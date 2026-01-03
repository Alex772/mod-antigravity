@echo off
echo ==========================================
echo   Setup Test Instance - ONI Local Testing
echo ==========================================
echo.

:: Configuracoes - EDITE AQUI se necessario
:: Caminho do ONI instalado pelo Steam
set ONI_STEAM_DRIVE=C:
set ONI_STEAM_PATH=\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded

:: Caminhos de destino
set ONI_TEST_DRIVE=D:
set ONI_TEST_PATH=\ONI_Test_Client
set MODS_TEST_PATH=\ONI_Test_Client_Mods

:: Montar caminhos completos
set "ONI_STEAM=%ONI_STEAM_DRIVE%%ONI_STEAM_PATH%"
set "ONI_TEST=%ONI_TEST_DRIVE%%ONI_TEST_PATH%"
set "MODS_TEST=%ONI_TEST_DRIVE%%MODS_TEST_PATH%"
set "MODS_ORIGINAL=%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods"

:: Verificar se ONI existe
echo Verificando instalacao do ONI...
if not exist "%ONI_STEAM%\OxygenNotIncluded.exe" (
    echo.
    echo ERRO: ONI nao encontrado em:
    echo   %ONI_STEAM%
    echo.
    echo Por favor, edite este script com o caminho correto.
    pause
    exit /b 1
)
echo   OK: ONI encontrado!

:: Verificar se ja existe copia
if exist "%ONI_TEST%" (
    echo.
    echo AVISO: Ja existe uma copia em %ONI_TEST%
    echo.
    set /p CONFIRM="Deseja substituir? [S/N]: "
    if /i not "!CONFIRM!"=="S" (
        echo Operacao cancelada.
        pause
        exit /b 0
    )
    echo Removendo copia antiga...
    rmdir /s /q "%ONI_TEST%" 2>nul
)

:: Copiar jogo
echo.
echo [1/3] Copiando o jogo (isso pode levar alguns minutos)...
echo       De: %ONI_STEAM%
echo       Para: %ONI_TEST%
echo.

%ONI_TEST_DRIVE%
if not exist "%ONI_TEST%" mkdir "%ONI_TEST%"

xcopy "%ONI_STEAM%\*" "%ONI_TEST%\" /E /I /H /Y /Q
if errorlevel 1 (
    echo ERRO: Falha ao copiar o jogo!
    pause
    exit /b 1
)
echo       Jogo copiado!

:: Copiar mods
echo.
echo [2/3] Copiando mods...
if exist "%MODS_TEST%" rmdir /s /q "%MODS_TEST%" 2>nul
xcopy "%MODS_ORIGINAL%\*" "%MODS_TEST%\" /E /I /H /Y /Q
if errorlevel 1 (
    echo AVISO: Falha ao copiar mods. Pode ser necessario sincronizar manualmente.
) else (
    echo       Mods copiados!
)

:: Configurando
echo.
echo [3/3] Configuracao concluida!

echo.
echo ==========================================
echo   Setup Concluido com Sucesso!
echo ==========================================
echo.
echo Instancia de teste criada em:
echo   %ONI_TEST%
echo.
echo Pasta de mods de teste:
echo   %MODS_TEST%
echo.
echo Proximos passos:
echo   1. Execute start_host.bat (Steam)
echo   2. Execute start_client.bat (Instancia de teste)
echo   3. Use F11 para abrir o Local Test em ambos
echo.
pause
