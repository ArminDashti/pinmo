#Requires -Version 5.1
<#
.SYNOPSIS
  Cursor stop hook — runs export.ps1 after each agent prompt completes.
#>
$ErrorActionPreference = 'Stop'

$stdin = [Console]::In.ReadToEnd()
$payload = $null
if ($stdin) {
    try {
        $payload = $stdin | ConvertFrom-Json
    }
    catch {
        Write-Warning "[pinmo-hook] Could not parse hook JSON; continuing with export."
    }
}

$status = if ($payload -and $payload.status) { [string]$payload.status } else { 'completed' }
if ($status -ne 'completed') {
    Write-Host "[pinmo-hook] Agent status was '$status'; skipping export.ps1." -ForegroundColor DarkGray
    Write-Output '{}'
    exit 0
}

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$exportScript = Join-Path $projectRoot 'export.ps1'

if (-not (Test-Path $exportScript)) {
    Write-Error "export.ps1 not found at $exportScript"
    Write-Output '{}'
    exit 0
}

Write-Host '[pinmo-hook] Running export.ps1 after agent completion...' -ForegroundColor Cyan

& $exportScript
if ($LASTEXITCODE -ne 0) {
    Write-Warning "[pinmo-hook] export.ps1 exited with code $LASTEXITCODE."
}

Write-Output '{}'
exit 0
