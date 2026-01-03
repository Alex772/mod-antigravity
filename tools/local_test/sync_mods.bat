@echo off
echo ==========================================
echo   Sync Mods - Sincronizar para Teste
echo ==========================================
echo.

set MODS_ORIGINAL=%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods
set MODS_TEST=D:\ONI_Test_Client_Mods

:: Verificar se existe
if not exist "%MODS_TEST%" (
    echo ERRO: Pasta de mods de teste nao encontrada!
    echo.
    echo Execute primeiro: setup_test_instance.bat
    echo.
    pause
    exit /b 1
)

echo Sincronizando mods...
echo   De: %MODS_ORIGINAL%
echo   Para: %MODS_TEST%
echo.

:: Copiar apenas a pasta Dev (onde fica o mod em desenvolvimento)
xcopy "%MODS_ORIGINAL%\Dev" "%MODS_TEST%\Dev" /E /I /H /Y /Q

echo.
echo Mods sincronizados!
echo.
echo Reinicie a instancia de teste para aplicar as mudancas.
echo.
pause
