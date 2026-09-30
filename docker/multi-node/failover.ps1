<#
.SYNOPSIS
  Failover validation for the multi-node deployment.

.DESCRIPTION
  Sends continuous authenticated traffic through the load balancer while it:
    1. stops litegraph-2, checks the cluster keeps serving, and starts it again;
    2. stops each Clutch node in turn, checks reads, writes, and index builds keep working
       through the other one, starts it again, and checks every LiteGraph node has its lock
       connection back.  Stopping both in turn guarantees every node's lock connection is cut
       at least once, whichever Clutch node it was on;
    3. stops Redis, checks traffic continues with every node reporting Degraded, that a cluster
       restart request is refused with 503, and that every node reconnects when Redis returns;
    4. requests a cluster rolling restart and checks every node restarts, never more than one
       at a time, while traffic continues.
  Fails if more than MaxErrorPercent of requests fail during either phase, or if a stopped
  service does not return to healthy.  Run after 'docker compose up -d' and smoke.ps1.
#>
param(
    [string] $RestBase = "http://127.0.0.1:" + $(if ($env:LITEGRAPH_REST_HOST_PORT) { $env:LITEGRAPH_REST_HOST_PORT } else { "8701" }),
    [string] $AdminBearerToken = "litegraphadmin",
    [string] $TenantGuid = "00000000-0000-0000-0000-000000000000",
    [double] $MaxErrorPercent = 2.0,
    [int] $PhaseSeconds = 25,
    [int] $RestartTimeoutSeconds = 240
)

$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot

function Start-Traffic([int] $seconds, [string] $stopFile = "") {
    Start-Job -ArgumentList $RestBase, $AdminBearerToken, $TenantGuid, $seconds, $stopFile -ScriptBlock {
        param($base, $token, $tenant, $seconds, $stopFile)
        $ok = 0; $fail = 0; $nodes = @{}
        $deadline = (Get-Date).AddSeconds($seconds)
        while ((Get-Date) -lt $deadline -and -not ($stopFile -and (Test-Path $stopFile))) {
            try {
                $r = Invoke-WebRequest -UseBasicParsing -Uri "$base/v1.0/tenants/$tenant/graphs" -Headers @{ Authorization = "Bearer $token" } -TimeoutSec 10
                if ([int] $r.StatusCode -eq 200) {
                    $ok++
                    $n = $r.Headers["x-litegraph-node"]; if ($n -is [array]) { $n = $n[0] }
                    $nodes[$n] = 1 + [int] $nodes[$n]
                }
                else { $fail++ }
            }
            catch { $fail++ }
            Start-Sleep -Milliseconds 50
        }
        [pscustomobject]@{ Ok = $ok; Fail = $fail; Nodes = (($nodes.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join " ") }
    }
}

function Wait-Healthy([string] $service, [int] $seconds = 120) {
    $deadline = (Get-Date).AddSeconds($seconds)
    while ((Get-Date) -lt $deadline) {
        $id = (& docker compose ps -q $service | Out-String).Trim()
        if ($id) {
            $state = (& docker inspect $id --format "{{.State.Health.Status}}" | Out-String).Trim()
            if ($state -eq "healthy") { return }
        }
        Start-Sleep -Seconds 2
    }
    throw "$service did not return to healthy within $seconds seconds"
}

function Wait-ClutchConnected([string[]] $services, [int] $seconds = 60) {
    $deadline = (Get-Date).AddSeconds($seconds)
    foreach ($svc in $services) {
        while ($true) {
            $json = (& docker compose exec -T $svc curl -s http://127.0.0.1:8701/v1.0/health/ready | Out-String)
            try { $ready = $json | ConvertFrom-Json } catch { $ready = $null }
            if ($ready -and $ready.Checks.Clutch -eq $true) { break }
            if ((Get-Date) -gt $deadline) { throw "$svc did not reconnect to Clutch within $seconds seconds" }
            Start-Sleep -Seconds 2
        }
    }
}

function Get-ClusterNodes {
    Invoke-RestMethod -Uri "$RestBase/v1.0/cluster/nodes" -Headers @{ Authorization = "Bearer $AdminBearerToken" } -TimeoutSec 10
}

function Wait-NodesReady([string[]] $services, [string] $check, [bool] $expected, [int] $seconds = 60) {
    $deadline = (Get-Date).AddSeconds($seconds)
    foreach ($svc in $services) {
        while ($true) {
            $json = (& docker compose exec -T $svc curl -s http://127.0.0.1:8701/v1.0/health/ready | Out-String)
            try { $ready = $json | ConvertFrom-Json } catch { $ready = $null }
            if ($ready -and $ready.Checks.$check -eq $expected) { break }
            if ((Get-Date) -gt $deadline) { throw "$svc did not report $check=$expected within $seconds seconds" }
            Start-Sleep -Seconds 2
        }
    }
}

function Assert-Phase([string] $name, $result) {
    $total = $result.Ok + $result.Fail
    $pct = if ($total -gt 0) { [math]::Round(100.0 * $result.Fail / $total, 2) } else { 100 }
    $line = "{0} requests, {1} failed ({2}%), served by {3}" -f $total, $result.Fail, $pct, $result.Nodes
    if ($total -lt 20 -or $pct -gt $MaxErrorPercent) {
        Write-Host "FAIL $name  $line"
        throw "$name exceeded the error budget: $line"
    }
    Write-Host "PASS $name  $line"
}

try {
    Write-Host "LiteGraph failover validation"
    Write-Host ""

    #
    # Phase 1: lose a LiteGraph node
    #

    $job = Start-Traffic $PhaseSeconds
    Start-Sleep -Seconds 3
    Write-Host "stopping litegraph-2"
    & docker compose stop litegraph-2 | Out-Null
    Start-Sleep -Seconds 8
    Write-Host "starting litegraph-2"
    & docker compose start litegraph-2 | Out-Null
    $result = Receive-Job -Job $job -Wait -AutoRemoveJob
    Assert-Phase "Traffic while litegraph-2 stopped and restarted" $result
    Wait-Healthy "litegraph-2"
    Write-Host "PASS litegraph-2 healthy again"

    #
    # Phase 2: lose each Clutch node in turn.  Reads and writes need no lock; an index build needs one.
    #

    $admin = @{ Authorization = "Bearer $AdminBearerToken" }
    $nodes = @("litegraph-1", "litegraph-2", "litegraph-3")

    foreach ($clutch in @("clutch-1", "clutch-2")) {
        $job = Start-Traffic $PhaseSeconds
        Start-Sleep -Seconds 3
        Write-Host "stopping $clutch"
        & docker compose stop $clutch | Out-Null

        $graph = Invoke-RestMethod -Method PUT -Uri "$RestBase/v1.0/tenants/$TenantGuid/graphs" -Headers $admin -ContentType "application/json" -Body (@{ Name = "failover-" + [guid]::NewGuid().ToString("N").Substring(0, 8) } | ConvertTo-Json)
        Write-Host "PASS Write through the load balancer with $clutch stopped  graph $($graph.GUID)"
        Invoke-RestMethod -Method PUT -Uri "$RestBase/v1.0/tenants/$TenantGuid/graphs/$($graph.GUID)/vectorindex/enable" -Headers $admin -ContentType "application/json" -Body (@{ VectorIndexType = "HnswRam"; VectorDimensionality = 12 } | ConvertTo-Json) | Out-Null
        Write-Host "PASS Index build (Clutch lock) with $clutch stopped"
        Invoke-RestMethod -Method DELETE -Uri "$RestBase/v1.0/tenants/$TenantGuid/graphs/$($graph.GUID)?force" -Headers $admin | Out-Null

        Wait-ClutchConnected $nodes
        Write-Host "PASS Every LiteGraph node has a lock connection with $clutch stopped"

        Start-Sleep -Seconds 3
        Write-Host "starting $clutch"
        & docker compose start $clutch | Out-Null
        $result = Receive-Job -Job $job -Wait -AutoRemoveJob
        Assert-Phase "Traffic while $clutch stopped and restarted" $result
        Wait-Healthy $clutch
        Write-Host "PASS $clutch healthy again"
    }

    #
    # Phase 3: lose Redis.  Nodes keep serving and report Degraded; only coordination waits.
    #

    $job = Start-Traffic $PhaseSeconds
    Start-Sleep -Seconds 3
    Write-Host "stopping redis"
    & docker compose stop redis | Out-Null
    Wait-NodesReady $nodes "Redis" $false 30
    Write-Host "PASS Every LiteGraph node reports Redis unavailable and stays ready"

    $status = Get-ClusterNodes
    if ($status.RegistryAvailable -ne $false -or $status.Nodes.Count -ne 1) { throw "cluster/nodes without Redis should list only the answering node" }
    Write-Host "PASS Node list falls back to the answering node  $($status.AnsweredBy)"

    $refused = $null
    try { Invoke-RestMethod -Method POST -Uri "$RestBase/v1.0/cluster/restart" -Headers $admin -ContentType "application/json" -Body '{}' | Out-Null }
    catch { $refused = [int] $_.Exception.Response.StatusCode }
    if ($refused -ne 503) { throw "cluster restart without Redis returned $refused instead of 503" }
    Write-Host "PASS Cluster restart refused with 503 while Redis is down"

    Write-Host "starting redis"
    & docker compose start redis | Out-Null
    $result = Receive-Job -Job $job -Wait -AutoRemoveJob
    Assert-Phase "Traffic while redis stopped and restarted" $result
    Wait-Healthy "redis"
    Wait-NodesReady $nodes "Redis" $true 60
    Write-Host "PASS Every LiteGraph node reconnects to Redis"

    #
    # Phase 4: rolling restart.  Every node restarts, one at a time, while traffic continues.
    #

    $before = @{}
    foreach ($n in (Get-ClusterNodes).Nodes) { $before[$n.NodeId] = $n.StartedUtc }

    $stopFile = Join-Path ([System.IO.Path]::GetTempPath()) ("litegraph-failover-" + [guid]::NewGuid().ToString("N"))
    $job = Start-Traffic $RestartTimeoutSeconds $stopFile
    Start-Sleep -Seconds 3
    $restart = Invoke-RestMethod -Method POST -Uri "$RestBase/v1.0/cluster/restart" -Headers $admin -ContentType "application/json" -Body '{}'
    if (-not $restart.Rolling) { throw "cluster restart was not a rolling restart" }
    Write-Host "PASS Rolling restart requested  version $($restart.RestartVersion)"

    $maxDown = 0
    $order = @()
    $deadline = (Get-Date).AddSeconds($RestartTimeoutSeconds - 10)
    while ($true) {
        try { $status = Get-ClusterNodes } catch { Start-Sleep -Seconds 1; continue }
        $down = @($status.Nodes | Where-Object { $nodes -contains $_.NodeId -and $_.State -ne "Healthy" })
        if ($down.Count -gt $maxDown) { $maxDown = $down.Count }
        foreach ($n in $status.Nodes) {
            if ($nodes -contains $n.NodeId -and $n.StartedUtc -ne $before[$n.NodeId] -and $order -notcontains $n.NodeId -and $n.State -eq "Healthy") { $order += $n.NodeId }
        }
        if ($order.Count -eq $nodes.Count) { break }
        if ((Get-Date) -gt $deadline) { throw "rolling restart did not finish within $RestartTimeoutSeconds seconds; restarted: $($order -join ', ')" }
        Start-Sleep -Seconds 1
    }
    Write-Host "PASS Every node restarted  order $($order -join ', ')"
    if ($maxDown -gt 1) { throw "more than one node was out of service at once during the rolling restart ($maxDown)" }
    Write-Host "PASS Never more than one node out of service  max $maxDown"

    New-Item -ItemType File -Path $stopFile | Out-Null
    $result = Receive-Job -Job $job -Wait -AutoRemoveJob
    Remove-Item $stopFile -ErrorAction SilentlyContinue
    Assert-Phase "Traffic during the rolling restart" $result

    $status = Get-ClusterNodes
    if (@($status.Nodes | Where-Object { $nodes -contains $_.NodeId -and $_.RestartPending }).Count -ne 0) { throw "a node still reports a pending restart" }
    Write-Host "PASS No node reports a pending restart"

    foreach ($svc in $nodes) { Wait-Healthy $svc 30 }
    Write-Host "PASS All LiteGraph nodes healthy"

    Write-Host ""
    Write-Host "Failover validation passed."
}
finally {
    Get-Job | Remove-Job -Force -ErrorAction SilentlyContinue
    Pop-Location
}
