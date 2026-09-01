param(
    [string] $RestBase = "http://localhost:8701",
    [string] $McpBase = "http://localhost:8702",
    [string] $McpMetricsBase = "http://localhost:8705",
    [string] $UiBase = "http://localhost:3001",
    [string] $PrometheusBase = "http://localhost:9090",
    [string] $GrafanaBase = "http://localhost:3000",
    [string] $LokiBase = "http://localhost:3100",
    [string] $AdminBearerToken = "litegraphadmin",
    [string] $TenantGuid = "00000000-0000-0000-0000-000000000000",
    [string] $UserEmail = "default@user.com",
    [string] $UserPassword = "password",
    [int] $TimeoutSeconds = 10,
    [int] $LlmTimeoutSeconds = 120
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

function Invoke-SmokeApiRequest {
    param(
        [string] $Name,
        [string] $Method = "GET",
        [string] $Uri,
        [hashtable] $Headers = @{},
        [string] $Body = $null,
        [int[]] $ExpectedStatusCodes = @(200),
        [int] $TimeoutSec = $TimeoutSeconds
    )

    $statusCode = $null
    $content = $null

    try {
        $requestArgs = @{
            UseBasicParsing = $true
            Method          = $Method
            Uri             = $Uri
            Headers         = $Headers
            TimeoutSec      = $TimeoutSec
        }
        if (-not [string]::IsNullOrEmpty($Body)) {
            $requestArgs["Body"] = $Body
            $requestArgs["ContentType"] = "application/json"
        }

        $response = Invoke-WebRequest @requestArgs
        $statusCode = [int] $response.StatusCode
        $content = $response.Content
    }
    catch [System.Net.WebException] {
        if ($null -ne $_.Exception.Response) {
            $statusCode = [int] $_.Exception.Response.StatusCode
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $content = $reader.ReadToEnd()
            $reader.Dispose()
        }
        else {
            Write-Host ("FAIL {0,-30} {1} {2}" -f $Name, $Method, $Uri)
            throw
        }
    }

    if ($ExpectedStatusCodes -notcontains $statusCode) {
        Write-Host ("FAIL {0,-30} {1} {2}" -f $Name, $Method, $Uri)
        throw "$Name returned HTTP $statusCode, expected $($ExpectedStatusCodes -join ", ")"
    }

    Write-Host ("PASS {0,-30} {1} {2}" -f $Name, $Method, $Uri)
    return $content
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
    Invoke-SmokeRequest -Name "REST settings (system admin)" -Uri "$RestBase/v1.0/settings" -Headers @{ Authorization = "Bearer $AdminBearerToken" } | Out-Null
    Invoke-SmokeRequest -Name "MCP root" -Uri $McpBase | Out-Null
    Invoke-SmokeRequest -Name "MCP metrics" -Uri "$McpMetricsBase/metrics" | Out-Null
    Invoke-SmokeRequest -Name "UI root" -Uri $UiBase | Out-Null
    Invoke-SmokeRequest -Name "Prometheus ready" -Uri "$PrometheusBase/-/ready" | Out-Null
    Invoke-SmokeRequest -Name "Loki ready" -Uri "$LokiBase/ready" | Out-Null
    Invoke-SmokeRequest -Name "Grafana health" -Uri "$GrafanaBase/api/health" | Out-Null

    #
    # Chat API probes.
    # Endpoint CRUD and settings use the admin break-glass bearer token; chat
    # completions require a user principal (x-email / x-password / x-tenant-guid)
    # because the break-glass token is rejected for completion routes with 400.
    #

    Write-Host ""
    $adminHeaders = @{ Authorization = "Bearer $AdminBearerToken" }
    $chatBase = "$RestBase/v1.0/tenants/$TenantGuid/chat"

    $settingsContent = Invoke-SmokeApiRequest -Name "Chat settings read" -Uri "$chatBase/settings" -Headers $adminHeaders
    $chatSettings = $settingsContent | ConvertFrom-Json
    if ($chatSettings.EnableChat -ne $true) {
        throw "Chat settings returned EnableChat=$($chatSettings.EnableChat), expected true"
    }
    Write-Host ("PASS {0,-30} EnableChat=true" -f "Chat settings enabled")

    $endpointBody = @{
        Name               = "Smoke Test Ollama"
        EndpointType       = "Completion"
        Provider           = "Ollama"
        Endpoint           = "http://host.docker.internal:11434"
        Model              = "llama3.1:8b"
        HealthCheckEnabled = $false
    } | ConvertTo-Json

    $createdContent = Invoke-SmokeApiRequest -Name "Chat endpoint create" -Method PUT -Uri "$chatBase/endpoints" -Headers $adminHeaders -Body $endpointBody
    $createdEndpoint = $createdContent | ConvertFrom-Json
    if ([string]::IsNullOrEmpty($createdEndpoint.GUID)) {
        throw "Chat endpoint create did not return a GUID"
    }

    Invoke-SmokeApiRequest -Name "Chat endpoint read" -Uri "$chatBase/endpoints/$($createdEndpoint.GUID)" -Headers $adminHeaders | Out-Null

    $healthContent = Invoke-SmokeApiRequest -Name "Chat endpoint health list" -Uri "$chatBase/endpoints/health" -Headers $adminHeaders
    if ($null -eq $healthContent -or -not $healthContent.Trim().StartsWith("[")) {
        throw "Chat endpoint health list did not return a JSON array"
    }

    Invoke-SmokeApiRequest -Name "Chat endpoint delete" -Method DELETE -Uri "$chatBase/endpoints/$($createdEndpoint.GUID)" -Headers $adminHeaders | Out-Null

    # Negative probe: VoyageAI is embedding-only, so a Completion endpoint must be rejected.
    $invalidEndpointBody = @{
        Name               = "Smoke Invalid VoyageAI"
        EndpointType       = "Completion"
        Provider           = "VoyageAI"
        Endpoint           = "https://api.voyageai.com/v1"
        Model              = "voyage-3"
        HealthCheckEnabled = $false
    } | ConvertTo-Json

    Invoke-SmokeApiRequest -Name "Chat endpoint reject VoyageAI" -Method PUT -Uri "$chatBase/endpoints" -Headers $adminHeaders -Body $invalidEndpointBody -ExpectedStatusCodes @(400) | Out-Null

    #
    # Optional live LLM completion probe.
    # Set LITEGRAPH_SMOKE_LLM_ENDPOINT to an OpenAI-compatible base URL (and
    # optionally LITEGRAPH_SMOKE_LLM_MODEL) to run one non-streaming completion.
    #

    $llmEndpointUrl = $env:LITEGRAPH_SMOKE_LLM_ENDPOINT
    if (-not [string]::IsNullOrEmpty($llmEndpointUrl)) {
        Write-Host ""
        Write-Host "Optional live LLM completion probe against $llmEndpointUrl"

        $llmModel = $env:LITEGRAPH_SMOKE_LLM_MODEL
        if ([string]::IsNullOrEmpty($llmModel)) {
            $llmModel = "gpt-4o-mini"
        }

        $llmEndpointBody = @{
            Name               = "Smoke Live LLM"
            EndpointType       = "Completion"
            Provider           = "OpenAI"
            Endpoint           = $llmEndpointUrl
            Model              = $llmModel
            HealthCheckEnabled = $false
        } | ConvertTo-Json

        $llmCreatedContent = Invoke-SmokeApiRequest -Name "Chat LLM endpoint create" -Method PUT -Uri "$chatBase/endpoints" -Headers $adminHeaders -Body $llmEndpointBody
        $llmEndpoint = $llmCreatedContent | ConvertFrom-Json

        try {
            $userHeaders = @{
                "x-email"       = $UserEmail
                "x-password"    = $UserPassword
                "x-tenant-guid" = $TenantGuid
            }

            $completionBody = @{
                Message                = "Reply with the single word OK."
                Stream                 = $false
                CompletionEndpointGUID = $llmEndpoint.GUID
                EnableTools            = $false
                EnableRag              = $false
            } | ConvertTo-Json

            $completionContent = Invoke-SmokeApiRequest -Name "Chat completion" -Method POST -Uri "$chatBase/completions" -Headers $userHeaders -Body $completionBody -TimeoutSec $LlmTimeoutSeconds
            $completion = $completionContent | ConvertFrom-Json
            if ([string]::IsNullOrEmpty($completion.Message)) {
                throw "Chat completion returned an empty message"
            }

            if (-not [string]::IsNullOrEmpty($completion.ThreadGUID)) {
                Invoke-SmokeApiRequest -Name "Chat thread cleanup" -Method DELETE -Uri "$chatBase/threads/$($completion.ThreadGUID)" -Headers $userHeaders | Out-Null
            }
        }
        finally {
            Invoke-SmokeApiRequest -Name "Chat LLM endpoint cleanup" -Method DELETE -Uri "$chatBase/endpoints/$($llmEndpoint.GUID)" -Headers $adminHeaders | Out-Null
        }
    }

    Write-Host ""
    Write-Host "Docker smoke validation passed."
}
finally {
    Pop-Location
}
