param(
    [string] $RestBase = "http://localhost:8701",
    [string] $McpBase = "http://localhost:8702",
    [string] $UiBase = "http://localhost:3001",
    [string] $PrometheusBase = "http://localhost:9090",
    [string] $GrafanaBase = "http://localhost:3000",
    [string] $AdminBearerToken = "litegraphadmin",
    [int] $TimeoutSeconds = 10
)

$ErrorActionPreference = "Stop"

function Invoke-SmokeRequest {
    param(
        [string] $Name,
        [string] $Uri,
        [hashtable] $Headers = @{},
        [int[]] $ExpectedStatusCodes = @(200)
    )

    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $Uri -Headers $Headers -TimeoutSec $TimeoutSeconds
        if ($ExpectedStatusCodes -notcontains [int] $response.StatusCode) {
            throw "$Name returned HTTP $($response.StatusCode), expected $($ExpectedStatusCodes -join ", ")"
        }

        Write-Host ("PASS {0,-30} {1}" -f $Name, $Uri)
        return $response
    }
    catch {
        Write-Host ("FAIL {0,-30} {1}" -f $Name, $Uri)
        throw
    }
}

Push-Location $PSScriptRoot
try {
    Write-Host "LiteGraph Docker smoke validation"
    Write-Host ""

    docker compose ps
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose ps failed"
    }

    Write-Host ""
    Invoke-SmokeRequest -Name "REST root" -Uri $RestBase | Out-Null
    Invoke-SmokeRequest -Name "REST metrics" -Uri "$RestBase/metrics" | Out-Null
    Invoke-SmokeRequest -Name "REST tenants auth" -Uri "$RestBase/v1.0/tenants" -Headers @{ Authorization = "Bearer $AdminBearerToken" } | Out-Null
    Invoke-SmokeRequest -Name "MCP root" -Uri $McpBase | Out-Null
    Invoke-SmokeRequest -Name "UI root" -Uri $UiBase | Out-Null
    Invoke-SmokeRequest -Name "Prometheus ready" -Uri "$PrometheusBase/-/ready" | Out-Null
    Invoke-SmokeRequest -Name "Grafana health" -Uri "$GrafanaBase/api/health" | Out-Null

    Write-Host ""
    Write-Host "Docker smoke validation passed."
}
finally {
    Pop-Location
}
