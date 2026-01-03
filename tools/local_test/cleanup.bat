@echo off
echo ==========================================
echo   Cleanup - Remover Instancia de Teste
echo ==========================================
echo.

set ONI_TEST=D:\ONI_Test_Client
set MODS_TEST=D:\ONI_Test_Client_Mods

echo AVISO: Isso vai remover os seguintes arquivos:
echo   - %ONI_TEST%
echo   - %MODS_TEST%
echo.
set /p CONFIRM="Tem certeza? (S/N): "

if /i not "%CONFIRM%"=="S" (
    echo Operacao cancelada.
    pause
    exit /b 0
)

echo.
echo Removendo arquivos...

if exist "%ONI_TEST%" (
    rmdir /s /q "%ONI_TEST%"
    echo   Jogo removido.
)

if exist "%MODS_TEST%" (
    rmdir /s /q "%MODS_TEST%"
    echo   Mods removidos.
)

echo.
echo Limpeza concluida!
echo Espaco em disco liberado.
echo.
pause
