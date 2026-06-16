#Requires -Version 5.1
<#
.SYNOPSIS
  Builds Pinmo and exports a Windows .exe to ./PinMo

.DESCRIPTION
  1. Publishes the ASP.NET Core API as a self-contained win-x64 app
  2. Packages the Electron UI with electron-builder
  3. Writes Pinmo.exe to ./PinMo

.EXAMPLE
  .\export.ps1
#>
param(
    [switch]$SkipNpmInstall
)

$ErrorActionPreference = 'Stop'

$Root = $PSScriptRoot
$ApiProject = Join-Path $Root 'src\Pinmo.Api\Pinmo.Api.csproj'
$PublishDir = Join-Path $Root 'publish\api'
$AppDir = Join-Path $Root 'app'
$ElectronOutDir = Join-Path $Root '.electron-out'
$OutputDir = Join-Path $Root 'PinMo'

function Write-Step([string]$Message) {
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

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

function Remove-DirectoryWithRetry([string]$Path, [int]$Attempts = 3) {
    if (-not (Test-Path $Path)) {
        return $true
    }

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            Remove-Item $Path -Recurse -Force -ErrorAction Stop
            return $true
        }
        catch {
            if ($attempt -eq $Attempts) {
                return $false
            }

            Start-Sleep -Seconds 2
        }
    }

    return $false
}

Write-Step 'Checking prerequisites'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK is required. Install it from https://dotnet.microsoft.com/download'
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw 'Node.js/npm is required. Install it from https://nodejs.org/'
}

if (-not (Test-Path $ApiProject)) {
    throw "API project not found: $ApiProject"
}

Write-Step 'Publishing .NET API (Release, win-x64, self-contained)'
if (Test-Path $PublishDir) {
    Remove-Item $PublishDir -Recurse -Force
}

dotnet publish $ApiProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $PublishDir `
    /p:PublishReadyToRun=true

if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed'
}

$apiExe = Join-Path $PublishDir 'Pinmo.Api.exe'
if (-not (Test-Path $apiExe)) {
    throw "Published API executable was not found: $apiExe"
}

Write-Step 'Stopping running Pinmo instances'
Stop-PinmoProcesses
Start-Sleep -Seconds 1

Write-Step 'Cleaning electron-builder output'
if (-not (Remove-DirectoryWithRetry $ElectronOutDir)) {
    throw "Could not clear electron build folder: $ElectronOutDir"
}

$staleUnpacked = Join-Path $OutputDir 'win-unpacked'
if ((Test-Path $staleUnpacked) -and -not (Remove-DirectoryWithRetry $staleUnpacked 1)) {
    Write-Host "Warning: could not remove stale $staleUnpacked (file may be locked). Continuing build in .electron-out." -ForegroundColor Yellow
}

Write-Step 'Preparing Electron build'
Push-Location $AppDir
try {
    if (-not $SkipNpmInstall) {
        npm install
        if ($LASTEXITCODE -ne 0) {
            throw 'npm install failed'
        }
    }

    Write-Step 'Building Windows portable executable'
    npm run build:win
    if ($LASTEXITCODE -ne 0) {
        throw 'electron-builder failed'
    }
}
finally {
    Pop-Location
}

$builtExe = Get-ChildItem $ElectronOutDir -Filter '*.exe' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $builtExe) {
    throw "Build finished but no .exe was found in $ElectronOutDir"
}

Write-Step 'Copying executable to PinMo'
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$destinationExe = Join-Path $OutputDir 'Pinmo.exe'
Copy-Item -Path $builtExe.FullName -Destination $destinationExe -Force
$builtExe = Get-Item $destinationExe

Write-Step 'Export complete'
Write-Host "Executable: $builtExe" -ForegroundColor Green
Write-Host "Folder:     $OutputDir" -ForegroundColor Green
Write-Host ''
Write-Host 'Run Pinmo.exe to start the desktop app.' -ForegroundColor Yellow
