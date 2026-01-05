@echo off
echo ==========================================
echo   Antigravity Mod - Deploy Script
echo ==========================================
echo.

:: Definir caminhos
set PROJETO=d:\Desenvolvimento\ONI\mod antigravity
set BUILD_DIR=%PROJETO%\bin\Debug
set DESTINO=C:\Users\Saikai\OneDrive\Documentos\Klei\OxygenNotIncluded\mods\Dev\Antigravity

:: Ir para a pasta do projeto
cd /d "%PROJETO%"

:: Compilar (DEBUG para desenvolvimento - inclui botao LOCAL TEST e hotkeys F10/F11)
echo [1/3] Compilando em modo DEBUG...
dotnet build Antigravity.sln --configuration Debug --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo ERRO: Falha na compilacao!
    pause
    exit /b 1
)
echo       Compilacao OK!
echo.

:: Criar pasta de destino se nao existir
if not exist "%DESTINO%" (
    mkdir "%DESTINO%"
    echo       Pasta de destino criada.
)

:: Copiar DLLs
echo [2/3] Copiando arquivos...
copy /Y "%BUILD_DIR%\Antigravity.dll" "%DESTINO%\" >nul
copy /Y "%BUILD_DIR%\Antigravity.Core.dll" "%DESTINO%\" >nul
copy /Y "%BUILD_DIR%\Antigravity.Patches.dll" "%DESTINO%\" >nul
copy /Y "%BUILD_DIR%\Antigravity.Client.dll" "%DESTINO%\" >nul
copy /Y "%BUILD_DIR%\Antigravity.Server.dll" "%DESTINO%\" >nul
copy /Y "%BUILD_DIR%\LiteNetLib.dll" "%DESTINO%\" >nul

:: Copiar metadados
:: Para builds DEBUG, modificar o titulo do mod para facilitar identificação
echo title: "Antigravity Multiplayer [DEV]" > "%DESTINO%\mod.yaml"
echo description: "Play Oxygen Not Included with your friends! This mod adds multiplayer support allowing multiple players to control the same colony. [DEBUG BUILD - LOCAL TEST ENABLED]" >> "%DESTINO%\mod.yaml"
echo staticID: "Antigravity.Multiplayer" >> "%DESTINO%\mod.yaml"
copy /Y "%PROJETO%\mod_info.yaml" "%DESTINO%\" >nul

:: Copiar assets
xcopy /Y /E /I "%PROJETO%\assets" "%DESTINO%\assets" >nul

echo       Arquivos copiados!
echo.

:: Listar arquivos copiados
echo [3/3] Arquivos no destino:
echo.
dir /B "%DESTINO%"
echo.

echo ==========================================
echo   Deploy concluido com sucesso!
echo ==========================================
echo.
echo Destino: %DESTINO%
echo.
echo Inicie o ONI e ative o mod.
echo.
pause
