# ==========================================
#   Setup Test Instance - ONI Local Testing
#   Versao PowerShell (mais robusto que batch)
# ==========================================

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Setup Test Instance - ONI Local Testing" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Configuracoes
$ONI_STEAM = "C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded"
$ONI_TEST = "D:\ONI_Test_Client"
$MODS_ORIGINAL = "$env:USERPROFILE\Documents\Klei\OxygenNotIncluded\mods"
$MODS_TEST = "D:\ONI_Test_Client_Mods"

# Verificar se ONI existe
Write-Host "Verificando instalacao do ONI..." -ForegroundColor Yellow
if (-not (Test-Path "$ONI_STEAM\OxygenNotIncluded.exe")) {
    Write-Host ""
    Write-Host "ERRO: ONI nao encontrado em:" -ForegroundColor Red
    Write-Host "  $ONI_STEAM" -ForegroundColor Red
    Write-Host ""
    Write-Host "Por favor, edite este script com o caminho correto."
    Read-Host "Pressione Enter para sair"
    exit 1
}
Write-Host "  OK: ONI encontrado!" -ForegroundColor Green

# Verificar se ja existe copia
if (Test-Path $ONI_TEST) {
    Write-Host ""
    Write-Host "AVISO: Ja existe uma copia em $ONI_TEST" -ForegroundColor Yellow
    Write-Host ""
    $confirm = Read-Host "Deseja substituir? [S/N]"
    if ($confirm -ne "S" -and $confirm -ne "s") {
        Write-Host "Operacao cancelada."
        exit 0
    }
    Write-Host "Removendo copia antiga..."
    Remove-Item -Path $ONI_TEST -Recurse -Force -ErrorAction SilentlyContinue
}

# Copiar jogo
Write-Host ""
Write-Host "[1/3] Copiando o jogo (isso pode levar alguns minutos)..." -ForegroundColor Cyan
Write-Host "      De: $ONI_STEAM"
Write-Host "      Para: $ONI_TEST"
Write-Host ""

try {
    Copy-Item -Path $ONI_STEAM -Destination $ONI_TEST -Recurse -Force
    Write-Host "      Jogo copiado!" -ForegroundColor Green
}
catch {
    Write-Host "ERRO: Falha ao copiar o jogo!" -ForegroundColor Red
    Write-Host $_.Exception.Message
    Read-Host "Pressione Enter para sair"
    exit 1
}

# Copiar mods
Write-Host ""
Write-Host "[2/3] Copiando mods..." -ForegroundColor Cyan

if (Test-Path $MODS_TEST) {
    Remove-Item -Path $MODS_TEST -Recurse -Force -ErrorAction SilentlyContinue
}

try {
    Copy-Item -Path $MODS_ORIGINAL -Destination $MODS_TEST -Recurse -Force
    Write-Host "      Mods copiados!" -ForegroundColor Green
}
catch {
    Write-Host "AVISO: Falha ao copiar mods. Pode ser necessario sincronizar manualmente." -ForegroundColor Yellow
}

# Configurando
Write-Host ""
Write-Host "[3/3] Configuracao concluida!" -ForegroundColor Green

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Setup Concluido com Sucesso!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Instancia de teste criada em:"
Write-Host "  $ONI_TEST" -ForegroundColor White
Write-Host ""
Write-Host "Pasta de mods de teste:"
Write-Host "  $MODS_TEST" -ForegroundColor White
Write-Host ""
Write-Host "Proximos passos:" -ForegroundColor Yellow
Write-Host "  1. Execute start_host.bat (Steam)"
Write-Host "  2. Execute start_client.bat (Instancia de teste)"
Write-Host "  3. Use F11 para abrir o Local Test em ambos"
Write-Host ""
Read-Host "Pressione Enter para fechar"
