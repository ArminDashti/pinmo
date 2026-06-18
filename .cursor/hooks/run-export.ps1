#Requires -Version 5.1
<#
.SYNOPSIS
  Cursor stop hook — runs export.ps1, then launches Pinmo after each agent prompt completes.
#>
$ErrorActionPreference = 'Stop'

function Stop-PinmoProcesses {
    $processNames = @('Pinmo', 'Pinmo.Api')
    foreach ($name in $processNames) {
        Get-Process -Name $name -ErrorAction SilentlyContinue |
            Stop-Process -Force -ErrorAction SilentlyContinue
    }

    Get-CimInstance Win32_Process |
        Where-Object {
            $_.ExecutablePath -and (
                $_.ExecutablePath -like '*\pinmo\*' -or
                $_.ExecutablePath -like '*\PinMo\*'
            )
        } |
        ForEach-Object {
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }
}

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

Write-Host '[pinmo-hook] Closing all Pinmo instances before export...' -ForegroundColor Cyan
Stop-PinmoProcesses
Start-Sleep -Seconds 1

Write-Host '[pinmo-hook] Running export.ps1 after agent completion...' -ForegroundColor Cyan

& $exportScript
if ($LASTEXITCODE -ne 0) {
    Write-Warning "[pinmo-hook] export.ps1 exited with code $LASTEXITCODE."
    Write-Output '{}'
    exit 0
}

$appExe = Join-Path $projectRoot 'PinMo\Pinmo.exe'
if (-not (Test-Path $appExe)) {
    Write-Warning "[pinmo-hook] Pinmo.exe not found at $appExe; skipping launch."
    Write-Output '{}'
    exit 0
}

Write-Host '[pinmo-hook] Launching Pinmo...' -ForegroundColor Cyan
Start-Process -FilePath $appExe -WorkingDirectory (Split-Path $appExe -Parent)

Write-Output '{}'
exit 0
