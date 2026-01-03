@echo off
echo ==========================================
echo   Start Client - Segunda Instancia
echo ==========================================
echo.

set ONI_TEST=D:\ONI_Test_Client
set CLIENT_EXE=ONI_TestClient.exe

:: Verificar se existe o executavel renomeado
if not exist "%ONI_TEST%\%CLIENT_EXE%" (
    echo Criando executavel renomeado para evitar conflito com Steam...
    copy "%ONI_TEST%\OxygenNotIncluded.exe" "%ONI_TEST%\%CLIENT_EXE%" >nul
)

if not exist "%ONI_TEST%\%CLIENT_EXE%" (
    echo ERRO: Executavel nao encontrado!
    echo Execute primeiro: setup_test_instance.ps1
    pause
    exit /b 1
)

echo Abrindo segunda instancia do ONI...
echo.
echo O executavel foi renomeado para %CLIENT_EXE%
echo Isso evita que o Steam detecte como jogo duplicado.
echo.
echo Apos abrir:
echo   1. Pressione F11
echo   2. Clique em "HOST" ou "JOIN" conforme necessario
echo.

:: Iniciar com nome diferente
cd /d "%ONI_TEST%"
start "" "%ONI_TEST%\%CLIENT_EXE%"

echo.
echo Segunda instancia iniciando!
echo Agora voce pode abrir o ONI pelo Steam normalmente.
echo.
pause
