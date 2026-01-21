# Script para atualizar a versão do mod Antigravity
# Uso: .\update-version.ps1 [major|minor|patch] ou .\update-version.ps1 <versao>

param(
    [Parameter(Position=0)]
    [string]$VersionArg = "patch"
)

$modInfoPath = "$PSScriptRoot\mod_info.yaml"
$distModInfoPath = "$PSScriptRoot\dist\Antigravity\mod_info.yaml"

# Ler versão atual
$content = Get-Content $modInfoPath -Raw
if ($content -match 'version:\s*(\d+)\.(\d+)\.(\d+)') {
    $major = [int]$Matches[1]
    $minor = [int]$Matches[2]
    $patch = [int]$Matches[3]
    $currentVersion = "$major.$minor.$patch"
    
    Write-Host "Versao atual: $currentVersion" -ForegroundColor Cyan
    
    # Determinar nova versão
    switch ($VersionArg.ToLower()) {
        "major" {
            $major++
            $minor = 0
            $patch = 0
        }
        "minor" {
            $minor++
            $patch = 0
        }
        "patch" {
            $patch++
        }
        default {
            # Tentar usar como versão direta (ex: "1.2.3")
            if ($VersionArg -match '^(\d+)\.(\d+)\.(\d+)$') {
                $major = [int]$Matches[1]
                $minor = [int]$Matches[2]
                $patch = [int]$Matches[3]
            } else {
                Write-Host "Uso: .\update-version.ps1 [major|minor|patch] ou .\update-version.ps1 <X.Y.Z>" -ForegroundColor Yellow
                exit 1
            }
        }
    }
    
    $newVersion = "$major.$minor.$patch"
    
    # Atualizar arquivo principal
    $newContent = $content -replace 'version:\s*\d+\.\d+\.\d+', "version: $newVersion"
    Set-Content $modInfoPath -Value $newContent -NoNewline
    
    # Atualizar arquivo em dist se existir
    if (Test-Path $distModInfoPath) {
        $distContent = Get-Content $distModInfoPath -Raw
        $distContent = $distContent -replace 'version:\s*\d+\.\d+\.\d+', "version: $newVersion"
        Set-Content $distModInfoPath -Value $distContent -NoNewline
    }
    
    Write-Host "Versao atualizada: $currentVersion -> $newVersion" -ForegroundColor Green
    Write-Host ""
    Write-Host "Arquivos atualizados:" -ForegroundColor White
    Write-Host "  - $modInfoPath" -ForegroundColor Gray
    if (Test-Path $distModInfoPath) {
        Write-Host "  - $distModInfoPath" -ForegroundColor Gray
    }
    
    # Mostrar próximos passos
    Write-Host ""
    Write-Host "Proximos passos:" -ForegroundColor Yellow
    Write-Host "  1. dotnet build -c Release" -ForegroundColor Gray
    Write-Host "  2. git add . && git commit -m 'v$newVersion'" -ForegroundColor Gray
    Write-Host "  3. git tag v$newVersion && git push --tags" -ForegroundColor Gray
    
} else {
    Write-Host "Erro: Nao foi possivel encontrar a versao em $modInfoPath" -ForegroundColor Red
    exit 1
}
