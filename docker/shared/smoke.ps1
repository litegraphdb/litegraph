<#
.SYNOPSIS
  Smoke validation shared by every LiteGraph Docker deployment.

.DESCRIPTION
  Called by docker/<deployment>/smoke.ps1 from that deployment's directory with -Deployment set.
  Checks every published service, the health endpoints, a full vector write and search round trip,
  chat endpoint management, and deployment-specific behavior:

    single-node-sqlite      vector search through HnswLite
    single-node-postgresql  pgvector extension present, vectors stored as pgvector
    multi-node              three ready nodes, requests spread across nodes, writes visible through
                            every node, identical search results on every node, Clutch healthy,
                            PostgreSQL role separation between LiteGraph and Clutch

  Host ports default to the documented values and follow the same LITEGRAPH_*_HOST_PORT environment
  variables as the compose files.  Exits non-zero on the first failure.  Loopback is always 127.0.0.1.
#>
param(
    [ValidateSet("single-node-sqlite", "single-node-postgresql", "multi-node")]
    [string] $Deployment = "single-node-postgresql",
    [string] $RestBase = "http://127.0.0.1:" + $(if ($env:LITEGRAPH_REST_HOST_PORT) { $env:LITEGRAPH_REST_HOST_PORT } else { "8701" }),
    [string] $McpBase = "http://127.0.0.1:" + $(if ($env:LITEGRAPH_MCP_HOST_PORT) { $env:LITEGRAPH_MCP_HOST_PORT } else { "8702" }),
    [string] $McpMetricsBase = "http://127.0.0.1:" + $(if ($env:LITEGRAPH_MCP_METRICS_HOST_PORT) { $env:LITEGRAPH_MCP_METRICS_HOST_PORT } else { "8705" }),
    [string] $UiBase = "http://127.0.0.1:" + $(if ($env:LITEGRAPH_UI_HOST_PORT) { $env:LITEGRAPH_UI_HOST_PORT } else { "3001" }),
    [string] $PrometheusBase = "http://127.0.0.1:" + $(if ($env:LITEGRAPH_PROMETHEUS_HOST_PORT) { $env:LITEGRAPH_PROMETHEUS_HOST_PORT } else { "9090" }),
    [string] $GrafanaBase = "http://127.0.0.1:" + $(if ($env:LITEGRAPH_GRAFANA_HOST_PORT) { $env:LITEGRAPH_GRAFANA_HOST_PORT } else { "3000" }),
    [string] $LokiBase = "http://127.0.0.1:" + $(if ($env:LITEGRAPH_LOKI_HOST_PORT) { $env:LITEGRAPH_LOKI_HOST_PORT } else { "3100" }),
    [string] $AdminBearerToken = "litegraphadmin",
    [string] $TenantGuid = "00000000-0000-0000-0000-000000000000",
    [string] $UserEmail = "default@user.com",
    [string] $UserPassword = "password",
    [int] $TimeoutSeconds = 15,
    [int] $LlmTimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"
$script:Passed = 0

function Write-Pass([string] $name, [string] $detail) {
    $script:Passed++
    Write-Host ("PASS {0,-44} {1}" -f $name, $detail)
}

function Write-Fail([string] $name, [string] $detail) {
    Write-Host ("FAIL {0,-44} {1}" -f $name, $detail)
}

function Invoke-Api {
    param(
        [string] $Name,
        [string] $Method = "GET",
        [string] $Uri,
        [hashtable] $Headers = @{},
        [object] $Body = $null,
        [int[]] $Expected = @(200),
        [int] $TimeoutSec = $TimeoutSeconds
    )

    $status = $null
    $content = $null
    $node = $null
    $request = @{ UseBasicParsing = $true; Method = $Method; Uri = $Uri; Headers = $Headers; TimeoutSec = $TimeoutSec }
    if ($null -ne $Body) {
        $request["Body"] = if ($Body -is [string]) { $Body } else { $Body | ConvertTo-Json -Depth 10 -Compress }
        $request["ContentType"] = "application/json"
    }

    try {
        $response = Invoke-WebRequest @request
        $status = [int] $response.StatusCode
        $content = $response.Content
        $node = $response.Headers["x-litegraph-node"]
    }
    catch {
        $resp = $_.Exception.Response
        if ($null -eq $resp) {
            Write-Fail $Name "$Method $Uri"
            throw
        }
        $status = [int] $resp.StatusCode
        try {
            $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $content = $reader.ReadToEnd()
            $reader.Dispose()
        }
        catch {
            $content = $_.ErrorDetails.Message
        }
    }

    if ($node -is [array]) { $node = $node[0] }

    if ($Expected -notcontains $status) {
        Write-Fail $Name "$Method $Uri -> HTTP $status"
        throw "$Name returned HTTP $status, expected $($Expected -join ', '): $content"
    }

    Write-Pass $Name ("$Method $Uri -> $status" + $(if ($node) { " ($node)" } else { "" }))

    $json = $null
    if (-not [string]::IsNullOrWhiteSpace($content)) {
        $trimmed = $content.Trim()
        if ($trimmed.StartsWith("{") -or $trimmed.StartsWith("[")) { $json = $trimmed | ConvertFrom-Json }
    }

    return [pscustomobject]@{ Status = $status; Content = $content; Node = $node; Json = $json }
}

function Wait-Ready([string] $Name, [string] $Uri, [int] $Seconds = 90) {
    $deadline = (Get-Date).AddSeconds($Seconds)
    while ($true) {
        try {
            $r = Invoke-WebRequest -UseBasicParsing -Uri $Uri -TimeoutSec $TimeoutSeconds
            if ([int] $r.StatusCode -eq 200) { Write-Pass $Name "GET $Uri -> 200"; return }
        }
        catch { }
        if ((Get-Date) -gt $deadline) {
            Write-Fail $Name "GET $Uri not ready after $Seconds seconds"
            throw "$Name was not ready after $Seconds seconds"
        }
        Start-Sleep -Seconds 2
    }
}

function Assert-True([bool] $condition, [string] $name, [string] $detail) {
    if (-not $condition) {
        Write-Fail $name $detail
        throw "$name failed: $detail"
    }
    Write-Pass $name $detail
}

function Invoke-NativeExpectFailure([string[]] $dockerArgs) {
    # Runs a docker command whose failure is the expected outcome.  Windows PowerShell 5.1 turns
    # native stderr into terminating errors under ErrorActionPreference=Stop, so relax it here.
    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & docker @dockerArgs 2>&1 | ForEach-Object { "$_" }
        $code = $LASTEXITCODE
        # The expected failure must not leak: a GitHub Actions pwsh step exits with $LASTEXITCODE.
        $global:LASTEXITCODE = 0
    }
    finally {
        $ErrorActionPreference = $previous
    }
    $first = (($output | Out-String).Trim() -split "`r?`n")[0]
    return [pscustomobject]@{ ExitCode = $code; FirstLine = $first }
}

function Invoke-Compose([string[]] $composeArgs) {
    $output = & docker compose @composeArgs 2>&1
    if ($LASTEXITCODE -ne 0) { throw "docker compose $($composeArgs -join ' ') failed: $output" }
    return ($output | Out-String).Trim()
}

$admin = @{ Authorization = "Bearer $AdminBearerToken" }
$tenantBase = "$RestBase/v1.0/tenants/$TenantGuid"

Write-Host "LiteGraph Docker smoke validation ($Deployment)"
Write-Host ""
Invoke-Compose @("ps") | Write-Host
Write-Host ""

#
# Services
#

Invoke-Api -Name "REST root" -Uri $RestBase | Out-Null
Invoke-Api -Name "REST liveness" -Uri "$RestBase/v1.0/health/live" | Out-Null
$ready = Invoke-Api -Name "REST readiness" -Uri "$RestBase/v1.0/health/ready"
Assert-True ($ready.Json.Checks.Database -eq $true) "Readiness database check" "Database=$($ready.Json.Checks.Database)"
$metricsText = (Invoke-WebRequest -UseBasicParsing -Uri "$RestBase/metrics" -TimeoutSec $TimeoutSeconds).Content
Assert-True ($metricsText -match 'litegraph_node_info\{node_id="[^"]+"') "REST metrics include node identity" (($metricsText -split "`n" | Where-Object { $_ -like 'litegraph_node_info*' } | Select-Object -First 1))
Invoke-Api -Name "REST tenants (admin)" -Uri "$RestBase/v1.0/tenants" -Headers $admin | Out-Null
Invoke-Api -Name "REST settings (admin)" -Uri "$RestBase/v1.0/settings" -Headers $admin | Out-Null
Invoke-Api -Name "REST rejects missing credentials" -Uri "$RestBase/v1.0/tenants" -Expected @(401) | Out-Null
Invoke-Api -Name "MCP root" -Uri $McpBase | Out-Null
Invoke-Api -Name "MCP metrics" -Uri "$McpMetricsBase/metrics" | Out-Null
Invoke-Api -Name "Dashboard" -Uri $UiBase | Out-Null
Wait-Ready "Prometheus ready" "$PrometheusBase/-/ready"
Wait-Ready "Loki ready" "$LokiBase/ready"
Wait-Ready "Grafana health" "$GrafanaBase/api/health"
$grafanaAuth = @{ Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin")) }
foreach ($uid in @("litegraph-overview", "litegraph-cluster")) {
    $dash = $null
    for ($i = 0; $i -lt 20 -and -not $dash; $i++) {
        try { $dash = Invoke-RestMethod -Uri "$GrafanaBase/api/dashboards/uid/$uid" -Headers $grafanaAuth -TimeoutSec $TimeoutSeconds } catch { Start-Sleep -Seconds 2 }
    }
    $hasNodeVar = $dash -and (@($dash.dashboard.templating.list | Where-Object { $_.name -eq "node" }).Count -eq 1)
    Assert-True $hasNodeVar "Grafana dashboard $uid provisioned with a node variable" $(if ($dash) { $dash.dashboard.title } else { "missing" })
}

#
# Vector write and search round trip
#

Write-Host ""
$dims = 8
$rng = New-Object System.Random 17
$vectors = @()
for ($i = 0; $i -lt 50; $i++) {
    $v = @()
    for ($d = 0; $d -lt $dims; $d++) { $v += [math]::Round(($rng.NextDouble() * 2 - 1), 6) }
    $vectors += ,$v
}

$graph = (Invoke-Api -Name "Create smoke graph" -Method PUT -Uri "$tenantBase/graphs" -Headers $admin -Body @{ Name = "smoke-" + [guid]::NewGuid().ToString("N").Substring(0, 8) }).Json
$graphBase = "$tenantBase/graphs/$($graph.GUID)"

try {
    $nodes = @()
    for ($i = 0; $i -lt $vectors.Count; $i++) {
        $nodes += @{
            Name    = "smoke-n$i"
            Vectors = @(@{ TenantGUID = $TenantGuid; GraphGUID = $graph.GUID; Model = "smoke"; Dimensionality = $dims; Content = "smoke-n$i"; Vectors = $vectors[$i] })
        }
    }
    Invoke-Api -Name "Create 50 nodes with vectors" -Method PUT -Uri "$graphBase/nodes/bulk" -Headers $admin -Body (ConvertTo-Json -InputObject $nodes -Depth 10 -Compress) | Out-Null

    Invoke-Api -Name "Enable vector index" -Method PUT -Uri "$graphBase/vectorindex/enable" -Headers $admin -Body @{ VectorIndexType = "HnswRam"; VectorDimensionality = $dims; VectorIndexM = 16; VectorIndexEf = 64; VectorIndexEfConstruction = 64 } | Out-Null

    $target = 11
    $search = @{ GraphGUID = $graph.GUID; Domain = "Node"; SearchType = "CosineSimilarity"; TopK = 5; MinimumScore = 0; Embeddings = $vectors[$target] }
    $result = (Invoke-Api -Name "Vector search" -Method POST -Uri "$graphBase/vectors/search" -Headers $admin -Body $search).Json
    $objects = if ($result.Objects) { $result.Objects } else { $result }
    Assert-True ($objects.Count -gt 0 -and $objects[0].Node.Name -eq "smoke-n$target") "Vector search exact match first" ("top: " + (($objects | Select-Object -First 3 | ForEach-Object { $_.Node.Name }) -join ", "))

    $stats = (Invoke-Api -Name "Vector index statistics" -Uri "$graphBase/vectorindex/stats" -Headers $admin).Json
    if ($Deployment -ne "single-node-sqlite") {
        Assert-True ($stats.IsLoaded -eq $true -and $stats.IndexFile -eq "idx_vectors_hnsw_cosine_$dims") "pgvector HNSW index built" "IndexFile=$($stats.IndexFile) IsLoaded=$($stats.IsLoaded) VectorCount=$($stats.VectorCount)"
    }
    else {
        Assert-True ($stats.VectorCount -ge 50) "HnswLite index populated" "VectorCount=$($stats.VectorCount)"
    }

    if ($Deployment -eq "multi-node") {
        Write-Host ""

        #
        # Distribution across nodes through the load balancer
        #

        $seen = @{}
        for ($i = 0; $i -lt 30; $i++) {
            $r = Invoke-WebRequest -UseBasicParsing -Uri "$RestBase/v1.0/health/ready" -TimeoutSec $TimeoutSeconds
            $n = $r.Headers["x-litegraph-node"]
            if ($n -is [array]) { $n = $n[0] }
            $seen[$n] = 1 + [int] $seen[$n]
        }
        Assert-True ($seen.Keys.Count -ge 3) "Load balancer spreads requests to every node" (($seen.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join " ")

        #
        # Every node, addressed directly inside the compose network, is ready, sees the same
        # write, and returns the same search results.
        #

        $searchJson = $search | ConvertTo-Json -Depth 10 -Compress
        $firstNames = @()
        foreach ($svc in @("litegraph-1", "litegraph-2", "litegraph-3")) {
            $readyJson = Invoke-Compose @("exec", "-T", $svc, "curl", "-s", "-f", "http://127.0.0.1:8701/v1.0/health/ready") | ConvertFrom-Json
            Assert-True ($readyJson.Status -eq "Healthy" -and $readyJson.Checks.Clutch -eq $true -and $readyJson.Checks.Redis -eq $true) "$svc ready with Clutch and Redis" "node=$($readyJson.NodeId) cluster=$($readyJson.ClusterName)"

            $graphJson = Invoke-Compose @("exec", "-T", $svc, "curl", "-s", "-f", "-H", "Authorization: Bearer $AdminBearerToken", "http://127.0.0.1:8701/v1.0/tenants/$TenantGuid/graphs/$($graph.GUID)") | ConvertFrom-Json
            Assert-True ($graphJson.GUID -eq $graph.GUID) "$svc reads the graph written via LB" $graphJson.Name

            $searchRaw = $searchJson | & docker compose exec -T $svc curl -s -f -X POST -H "Authorization: Bearer $AdminBearerToken" -H "Content-Type: application/json" --data-binary "@-" "http://127.0.0.1:8701/v1.0/tenants/$TenantGuid/graphs/$($graph.GUID)/vectors/search"
            if ($LASTEXITCODE -ne 0) { throw "search through $svc failed: $searchRaw" }
            $searchOut = ($searchRaw | Out-String) | ConvertFrom-Json
            $objs = if ($searchOut.Objects) { $searchOut.Objects } else { $searchOut }
            $names = ($objs | ForEach-Object { $_.Node.Name }) -join ","
            $firstNames += $names
            Assert-True ($objs[0].Node.Name -eq "smoke-n$target") "$svc search returns the exact match" $names
        }
        Assert-True (($firstNames | Select-Object -Unique).Count -eq 1) "Every node returns identical results" $firstNames[0]

        #
        # Node registry and shared settings
        #

        $nodes = Invoke-RestMethod -Uri "$RestBase/v1.0/cluster/nodes" -Headers $admin -TimeoutSec $TimeoutSeconds
        $listed = ($nodes.Nodes | ForEach-Object { "$($_.NodeId)=$($_.State)" }) -join " "
        Assert-True ($nodes.ClusterEnabled -and $nodes.RegistryAvailable -eq $true) "Node registry available" "cluster=$($nodes.ClusterName) answered by $($nodes.AnsweredBy)"
        foreach ($svc in @("litegraph-1", "litegraph-2", "litegraph-3")) {
            $entry = $nodes.Nodes | Where-Object { $_.NodeId -eq $svc }
            Assert-True ($entry -and $entry.State -eq "Healthy" -and $entry.HeartbeatAgeMs -lt 15000) "$svc registered and healthy" $listed
        }

        $settingsBodies = @()
        foreach ($svc in @("litegraph-1", "litegraph-2", "litegraph-3")) {
            $settingsBodies += (Invoke-Compose @("exec", "-T", $svc, "curl", "-s", "-f", "-H", "Authorization: Bearer $AdminBearerToken", "http://127.0.0.1:8701/v1.0/settings") | Out-String).Trim()
        }
        $settingsNodeIds = ($settingsBodies | ForEach-Object { ($_ | ConvertFrom-Json).Cluster.NodeId } | Where-Object { $_ }) -join ","
        Assert-True (($settingsBodies | Select-Object -Unique).Count -eq 1) "Every node returns the same settings" "$($settingsBodies[0].Length) bytes"
        Assert-True ([string]::IsNullOrEmpty($settingsNodeIds)) "Settings do not carry a node identifier" $(if ($settingsNodeIds) { $settingsNodeIds } else { "none" })

        #
        # Switchboard, when its profile is running
        #

        $switchboardId = (& docker compose ps -q switchboard 2>$null | Out-String).Trim()
        if ($switchboardId) {
            $sbPort = if ($env:LITEGRAPH_SWITCHBOARD_HOST_PORT) { $env:LITEGRAPH_SWITCHBOARD_HOST_PORT } else { "8711" }
            $sbSeen = @{}
            for ($i = 0; $i -lt 30; $i++) {
                $r = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:$sbPort/v1.0/tenants/$TenantGuid/graphs/$($graph.GUID)" -Headers $admin -TimeoutSec $TimeoutSeconds
                $n = $r.Headers["x-litegraph-node"]
                if ($n -is [array]) { $n = $n[0] }
                $sbSeen[$n] = 1 + [int] $sbSeen[$n]
            }
            Assert-True ($sbSeen.Keys.Count -ge 3) "Switchboard spreads requests to every node" (($sbSeen.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join " ")
        }

        #
        # Clutch and PostgreSQL
        #

        $clutch = Invoke-Compose @("exec", "-T", "clutch-lb", "curl", "-s", "-f", "http://127.0.0.1:8090/v1.0/api/health") | ConvertFrom-Json
        Assert-True ($clutch.status -eq "healthy") "Clutch healthy through clutch-lb" "node=$($clutch.node)"

        $pong = (Invoke-Compose @("exec", "-T", "redis", "redis-cli", "ping") | Out-String).Trim()
        Assert-True ($pong -eq "PONG") "Redis answers" $pong

        $locks = Invoke-RestMethod -Uri "$RestBase/v1.0/cluster/locks" -Headers $admin -TimeoutSec $TimeoutSeconds
        Assert-True ($locks.ClusterEnabled -and $locks.LockServiceAvailable -eq $true) "Cluster locks listed from Clutch" "$(@($locks.Locks).Count) held"
        $jobs = Invoke-RestMethod -Uri "$RestBase/v1.0/cluster/jobs" -Headers $admin -TimeoutSec $TimeoutSeconds
        Assert-True ($jobs.ClusterEnabled -and $jobs.RegistryAvailable -eq $true) "Cluster job runs listed from Redis" (($jobs.Jobs | ForEach-Object { "$($_.Job)@$($_.NodeId)" }) -join ",")

        # Request history records the handling node and, through the trusted load balancer, the client's own address.
        $history = Invoke-RestMethod -Uri "$RestBase/v1.0/requesthistory?max-keys=20" -Headers $admin -TimeoutSec $TimeoutSeconds
        $recent = @($history.Objects | Where-Object { $_.NodeId })
        Assert-True ($recent.Count -gt 0) "Request history records the handling node" (($recent | Select-Object -First 3 | ForEach-Object { $_.NodeId }) -join ",")
        $lbAddress = (& docker inspect (& docker compose ps -q litegraph-lb) --format "{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}" | Out-String).Trim()
        $sources = @($history.Objects | ForEach-Object { $_.SourceIp } | Sort-Object -Unique)
        Assert-True ($sources.Count -gt 0 -and -not ($sources -contains $lbAddress)) "Request history records the client address, not the load balancer" "sources=$($sources -join ',') lb=$lbAddress"
        $firstNode = $recent[0].NodeId
        $filtered = Invoke-RestMethod -Uri "$RestBase/v1.0/requesthistory?max-keys=20&nodeId=$firstNode" -Headers $admin -TimeoutSec $TimeoutSeconds
        Assert-True (@($filtered.Objects).Count -gt 0 -and @($filtered.Objects | Where-Object { $_.NodeId -ne $firstNode }).Count -eq 0) "Request history filters by node" $firstNode

        #
        # Prometheus scrapes every node under its node label, and every node reports Healthy
        #

        $upNodes = @()
        $healthy = 0
        for ($i = 0; $i -lt 30; $i++) {
            $up = Invoke-RestMethod -Uri ("$PrometheusBase/api/v1/query?query=" + [uri]::EscapeDataString('up{job="litegraph"} == 1')) -TimeoutSec $TimeoutSeconds
            $upNodes = @($up.data.result | ForEach-Object { $_.metric.node } | Sort-Object)
            $state = Invoke-RestMethod -Uri ("$PrometheusBase/api/v1/query?query=" + [uri]::EscapeDataString('sum(litegraph_node_state{state="Healthy"})')) -TimeoutSec $TimeoutSeconds
            $healthy = if ($state.data.result.Count -gt 0) { [int] $state.data.result[0].value[1] } else { 0 }
            if ($upNodes.Count -eq 3 -and $healthy -eq 3) { break }
            Start-Sleep -Seconds 2
        }
        Assert-True ($upNodes.Count -eq 3) "Prometheus scrapes every node by node label" ($upNodes -join ",")
        Assert-True ($healthy -eq 3) "Every node reports Healthy in Prometheus" "healthy=$healthy"

        $ext = Invoke-Compose @("exec", "-T", "postgresql", "psql", "-U", "postgres", "-d", "litegraph", "-tAc", "SELECT extversion FROM pg_extension WHERE extname = 'vector'")
        Assert-True (-not [string]::IsNullOrWhiteSpace($ext)) "pgvector extension in litegraph database" "version $ext"

        $super = Invoke-Compose @("exec", "-T", "postgresql", "psql", "-U", "postgres", "-d", "litegraph", "-tAc", "SELECT rolsuper FROM pg_roles WHERE rolname = 'litegraph'")
        Assert-True ($super.Trim() -eq "f") "litegraph role is not a superuser" "rolsuper=$($super.Trim())"

        $cross = Invoke-NativeExpectFailure @("compose", "exec", "-T", "postgresql", "sh", "-c", "PGPASSWORD=`$CLUTCH_DB_PASSWORD psql -h 127.0.0.1 -U clutch -d litegraph -tAc 'SELECT 1'")
        Assert-True ($cross.ExitCode -ne 0) "clutch role cannot connect to litegraph database" $cross.FirstLine
        $cross = Invoke-NativeExpectFailure @("compose", "exec", "-T", "postgresql", "sh", "-c", "PGPASSWORD=`$LITEGRAPH_DB_PASSWORD psql -h 127.0.0.1 -U litegraph -d clutch -tAc 'SELECT 1'")
        Assert-True ($cross.ExitCode -ne 0) "litegraph role cannot connect to clutch database" $cross.FirstLine
    }
    elseif ($Deployment -eq "single-node-postgresql") {
        $ext = Invoke-Compose @("exec", "-T", "postgresql", "psql", "-U", "litegraph", "-d", "litegraph", "-tAc", "SELECT extversion FROM pg_extension WHERE extname = 'vector'")
        Assert-True (-not [string]::IsNullOrWhiteSpace($ext)) "pgvector extension in litegraph database" "version $ext"
        $type = Invoke-Compose @("exec", "-T", "postgresql", "psql", "-U", "litegraph", "-d", "litegraph", "-tAc", "SELECT udt_name FROM information_schema.columns WHERE table_name = 'vectors' AND column_name = 'embeddings'")
        Assert-True ($type.Trim() -eq "vector") "vectors.embeddings stored as pgvector" "type=$($type.Trim())"
    }
}
finally {
    Invoke-Api -Name "Delete smoke graph" -Method DELETE -Uri "$($graphBase)?force" -Headers $admin -Expected @(200, 204) | Out-Null
}

#
# Chat endpoint management
#

Write-Host ""
$chatBase = "$tenantBase/chat"
$chatSettings = (Invoke-Api -Name "Chat settings read" -Uri "$chatBase/settings" -Headers $admin).Json
Assert-True ($chatSettings.EnableChat -eq $true) "Chat enabled" "EnableChat=$($chatSettings.EnableChat)"

$endpoint = (Invoke-Api -Name "Chat endpoint create" -Method PUT -Uri "$chatBase/endpoints" -Headers $admin -Body @{ Name = "Smoke Test Ollama"; EndpointType = "Completion"; Provider = "Ollama"; Endpoint = "http://host.docker.internal:11434"; Model = "llama3.1:8b"; HealthCheckEnabled = $false }).Json
Invoke-Api -Name "Chat endpoint read" -Uri "$chatBase/endpoints/$($endpoint.GUID)" -Headers $admin | Out-Null
$health = Invoke-Api -Name "Chat endpoint health list" -Uri "$chatBase/endpoints/health" -Headers $admin
Assert-True ($null -ne $health.Json -and $null -ne $health.Json.PSObject.Properties["Objects"]) "Chat endpoint health returns an enumeration" "TotalRecords=$($health.Json.TotalRecords)"
Invoke-Api -Name "Chat endpoint delete" -Method DELETE -Uri "$chatBase/endpoints/$($endpoint.GUID)" -Headers $admin | Out-Null
Invoke-Api -Name "Chat endpoint rejects VoyageAI completion" -Method PUT -Uri "$chatBase/endpoints" -Headers $admin -Body @{ Name = "Smoke Invalid VoyageAI"; EndpointType = "Completion"; Provider = "VoyageAI"; Endpoint = "https://api.voyageai.com/v1"; Model = "voyage-3"; HealthCheckEnabled = $false } -Expected @(400) | Out-Null

#
# Optional live LLM completion: set LITEGRAPH_SMOKE_LLM_ENDPOINT (OpenAI-compatible base URL)
# and optionally LITEGRAPH_SMOKE_LLM_MODEL.
#

$llmEndpointUrl = $env:LITEGRAPH_SMOKE_LLM_ENDPOINT
if (-not [string]::IsNullOrEmpty($llmEndpointUrl)) {
    Write-Host ""
    $llmModel = if ([string]::IsNullOrEmpty($env:LITEGRAPH_SMOKE_LLM_MODEL)) { "gpt-4o-mini" } else { $env:LITEGRAPH_SMOKE_LLM_MODEL }
    $llm = (Invoke-Api -Name "Chat LLM endpoint create" -Method PUT -Uri "$chatBase/endpoints" -Headers $admin -Body @{ Name = "Smoke Live LLM"; EndpointType = "Completion"; Provider = "OpenAI"; Endpoint = $llmEndpointUrl; Model = $llmModel; HealthCheckEnabled = $false }).Json
    try {
        $userHeaders = @{ "x-email" = $UserEmail; "x-password" = $UserPassword; "x-tenant-guid" = $TenantGuid }
        $completion = (Invoke-Api -Name "Chat completion" -Method POST -Uri "$chatBase/completions" -Headers $userHeaders -Body @{ Message = "Reply with the single word OK."; Stream = $false; CompletionEndpointGUID = $llm.GUID; EnableTools = $false; EnableRag = $false } -TimeoutSec $LlmTimeoutSeconds).Json
        Assert-True (-not [string]::IsNullOrEmpty($completion.Message)) "Chat completion returned text" ""
        if ($completion.ThreadGUID) { Invoke-Api -Name "Chat thread cleanup" -Method DELETE -Uri "$chatBase/threads/$($completion.ThreadGUID)" -Headers $userHeaders | Out-Null }
    }
    finally {
        Invoke-Api -Name "Chat LLM endpoint cleanup" -Method DELETE -Uri "$chatBase/endpoints/$($llm.GUID)" -Headers $admin | Out-Null
    }
}

Write-Host ""
Write-Host "Smoke validation passed: $($script:Passed) checks ($Deployment)."
