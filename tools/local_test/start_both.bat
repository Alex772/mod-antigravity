@echo off
echo ==========================================
echo   Start Both Instances - Teste Completo
echo ==========================================
echo.
echo IMPORTANTE: Para testes locais, recomendamos:
echo   - Instance de TESTE (D:\) = HOST (inicia primeiro)
echo   - Instance do STEAM = CLIENT (conecta depois)
echo.
echo Isso evita problemas de verificacao do Steam.
echo.
pause

set ONI_TEST=D:\ONI_Test_Client

:: Verificar se existe
if not exist "%ONI_TEST%\OxygenNotIncluded.exe" (
    echo ERRO: Instancia de teste nao encontrada!
    echo Execute primeiro: setup_test_instance.ps1
    pause
    exit /b 1
)

echo.
echo [1/2] Iniciando instancia de TESTE como HOST...
cd /d "%ONI_TEST%"
start "" "%ONI_TEST%\OxygenNotIncluded.exe"

echo Aguardando 10 segundos para o jogo carregar...
timeout /t 10

echo.
echo [2/2] Agora abra o ONI pelo Steam manualmente!
echo.
echo Instrucoes:
echo   HOST (instancia de teste):
echo     - F11 ^> Clique em HOST
echo.
echo   CLIENT (Steam):
echo     - F11 ^> Digite 127.0.0.1:7777 ^> JOIN
echo.
pause
