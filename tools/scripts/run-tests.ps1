# Antigravity Test Runner Script
# Usage: .\run-tests.ps1 [-Filter <pattern>] [-Verbose]

param(
    [string]$Filter = "",
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Antigravity Test Runner" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

$testProject = Join-Path $RootDir "tests\Antigravity.Tests.Unit\Antigravity.Tests.Unit.csproj"

$args = @(
    "test",
    $testProject,
    "--configuration", "Debug",
    "--logger", "console;verbosity=normal"
)

if ($Filter) {
    $args += "--filter"
    $args += $Filter
    Write-Host "Filter: $Filter" -ForegroundColor Yellow
}

if ($Verbose) {
    $args += "--verbosity"
    $args += "detailed"
}

Write-Host ""
Write-Host "Running tests..." -ForegroundColor Yellow
Write-Host ""

& dotnet @args

$exitCode = $LASTEXITCODE

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "======================================" -ForegroundColor Green
    Write-Host "  All Tests Passed!" -ForegroundColor Green
    Write-Host "======================================" -ForegroundColor Green
}
else {
    Write-Host "======================================" -ForegroundColor Red
    Write-Host "  Some Tests Failed!" -ForegroundColor Red
    Write-Host "======================================" -ForegroundColor Red
}

exit $exitCode
