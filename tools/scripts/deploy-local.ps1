# Antigravity Deploy Script
# Deploys the mod to the ONI mods folder for testing
# Usage: .\deploy-local.ps1 [-Target Dev|Local]

param(
    [ValidateSet("Dev", "Local")]
    [string]$Target = "Dev"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Antigravity Deploy Script" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Build first
Write-Host "Building project..." -ForegroundColor Yellow
& "$ScriptDir\build.ps1" -Configuration Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed, cannot deploy!" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Determine target directory
# Use OneDrive path if exists, otherwise fallback to standard Documents
$oniModsBase = "C:\Users\Saikai\OneDrive\Documentos\Klei\OxygenNotIncluded\mods"
if (-not (Test-Path $oniModsBase)) {
    $oniModsBase = Join-Path $env:USERPROFILE "Documents\Klei\OxygenNotIncluded\mods"
}
$targetDir = Join-Path $oniModsBase "$Target\Antigravity"

Write-Host "Deploy Target: $targetDir" -ForegroundColor Yellow
Write-Host ""

# Create target directory if it doesn't exist
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Write-Host "Created target directory." -ForegroundColor Gray
}

# Copy DLLs
$binDir = Join-Path $RootDir "bin\Debug"
$dlls = @(
    "Antigravity.dll",
    "Antigravity.Core.dll",
    "Antigravity.Patches.dll",
    "Antigravity.Client.dll",
    "Antigravity.Server.dll",
    "LiteNetLib.dll"
    # Note: Newtonsoft.Json is already included in ONI, no need to copy
)

Write-Host "Copying files..." -ForegroundColor Yellow

foreach ($dll in $dlls) {
    $source = Join-Path $binDir $dll
    if (Test-Path $source) {
        Copy-Item -Path $source -Destination $targetDir -Force
        Write-Host "  Copied: $dll" -ForegroundColor Gray
    }
    else {
        Write-Host "  Skipped (not found): $dll" -ForegroundColor DarkGray
    }
}

# Copy mod metadata
$metaFiles = @("mod.yaml", "mod_info.yaml")

foreach ($file in $metaFiles) {
    $source = Join-Path $RootDir $file
    if (Test-Path $source) {
        Copy-Item -Path $source -Destination $targetDir -Force
        Write-Host "  Copied: $file" -ForegroundColor Gray
    }
}

# Copy assets if they exist
$assetsSource = Join-Path $RootDir "assets"
if (Test-Path $assetsSource) {
    $assetsTarget = Join-Path $targetDir "assets"
    Copy-Item -Path $assetsSource -Destination $targetDir -Recurse -Force
    Write-Host "  Copied: assets folder" -ForegroundColor Gray
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "  Deploy Successful!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "Mod installed to: $targetDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Start ONI and enable the mod in the Mods menu." -ForegroundColor Yellow
