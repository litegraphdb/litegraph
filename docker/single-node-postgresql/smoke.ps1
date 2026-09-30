<#
.SYNOPSIS
  Smoke validation for the single-node PostgreSQL deployment.  Run from any directory after
  'docker compose up -d'; extra parameters pass through to ../shared/smoke.ps1.
#>
Push-Location $PSScriptRoot
try {
    & (Join-Path $PSScriptRoot "..\shared\smoke.ps1") -Deployment "single-node-postgresql" @args
    if (-not $?) { exit 1 }
}
finally {
    Pop-Location
}
