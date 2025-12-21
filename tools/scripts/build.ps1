# Antigravity Build Script
# Usage: .\build.ps1 [-Configuration Debug|Release] [-Clean]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Antigravity Build Script" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Root Directory: $RootDir" -ForegroundColor Yellow
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    
    $binDir = Join-Path $RootDir "bin"
    $objDirs = Get-ChildItem -Path $RootDir -Recurse -Directory -Filter "obj"
    
    if (Test-Path $binDir) {
        Remove-Item -Path $binDir -Recurse -Force
        Write-Host "  Removed: $binDir" -ForegroundColor Gray
    }
    
    foreach ($objDir in $objDirs) {
        Remove-Item -Path $objDir.FullName -Recurse -Force
        Write-Host "  Removed: $($objDir.FullName)" -ForegroundColor Gray
    }
    
    Write-Host "Clean complete!" -ForegroundColor Green
    Write-Host ""
}

# Check for local.props
$localProps = Join-Path $RootDir "local.props"
if (-not (Test-Path $localProps)) {
    Write-Host "WARNING: local.props not found!" -ForegroundColor Yellow
    Write-Host "Copy local.props.example to local.props and configure paths." -ForegroundColor Yellow
    Write-Host ""
}

# Restore NuGet packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
$solutionPath = Join-Path $RootDir "Antigravity.sln"
dotnet restore $solutionPath --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: NuGet restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "NuGet restore complete!" -ForegroundColor Green
Write-Host ""

# Build solution
Write-Host "Building solution ($Configuration)..." -ForegroundColor Yellow
dotnet build $solutionPath --configuration $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "  Build Successful!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output: $(Join-Path $RootDir "bin\$Configuration")" -ForegroundColor Cyan
