#Requires -Version 5.1
<#
.SYNOPSIS
  Cursor stop hook — runs export.ps1 after each agent prompt completes.
#>
$ErrorActionPreference = 'Stop'

# Hooks receive JSON on stdin; consume it so the process does not block.
$null = [Console]::In.ReadToEnd()

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$exportScript = Join-Path $projectRoot 'export.ps1'

if (-not (Test-Path $exportScript)) {
    Write-Error "export.ps1 not found at $exportScript"
    exit 1
}

Write-Host '[pinmo-hook] Running export.ps1 after agent completion...' -ForegroundColor Cyan

& $exportScript
exit $LASTEXITCODE
