@echo off
echo ==========================================
echo   Antigravity Mod - Create Distribution
echo ==========================================
echo.

:: Definir caminhos
set PROJETO=d:\Desenvolvimento\ONI\mod antigravity
set BUILD_DIR=%PROJETO%\bin\Release
set DIST_DIR=%PROJETO%\dist
set MOD_NAME=Antigravity

:: Compilar primeiro (RELEASE para producao - exclui funcionalidades de debug)
echo [1/4] Compilando em modo RELEASE...
cd /d "%PROJETO%"
dotnet build Antigravity.sln --configuration Release --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo ERRO: Falha na compilacao!
    pause
    exit /b 1
)
echo       OK!

:: Criar pasta de distribuicao
echo [2/4] Preparando pasta...
if exist "%DIST_DIR%" rmdir /s /q "%DIST_DIR%"
mkdir "%DIST_DIR%\%MOD_NAME%"

:: Copiar arquivos
echo [3/4] Copiando arquivos...
copy /Y "%BUILD_DIR%\Antigravity.dll" "%DIST_DIR%\%MOD_NAME%\" >nul
copy /Y "%BUILD_DIR%\Antigravity.Core.dll" "%DIST_DIR%\%MOD_NAME%\" >nul
copy /Y "%BUILD_DIR%\Antigravity.Patches.dll" "%DIST_DIR%\%MOD_NAME%\" >nul
copy /Y "%BUILD_DIR%\Antigravity.Client.dll" "%DIST_DIR%\%MOD_NAME%\" >nul
copy /Y "%BUILD_DIR%\Antigravity.Server.dll" "%DIST_DIR%\%MOD_NAME%\" >nul
copy /Y "%BUILD_DIR%\LiteNetLib.dll" "%DIST_DIR%\%MOD_NAME%\" >nul
copy /Y "%PROJETO%\mod.yaml" "%DIST_DIR%\%MOD_NAME%\" >nul
copy /Y "%PROJETO%\mod_info.yaml" "%DIST_DIR%\%MOD_NAME%\" >nul
xcopy /Y /E /I "%PROJETO%\assets" "%DIST_DIR%\%MOD_NAME%\assets" >nul

:: Criar arquivo de instrucoes
echo ## Como Instalar o Mod Antigravity Multiplayer > "%DIST_DIR%\LEIA-ME.txt"
echo. >> "%DIST_DIR%\LEIA-ME.txt"
echo 1. Abra a pasta de mods do ONI: >> "%DIST_DIR%\LEIA-ME.txt"
echo    %%USERPROFILE%%\Documents\Klei\OxygenNotIncluded\mods\Local >> "%DIST_DIR%\LEIA-ME.txt"
echo. >> "%DIST_DIR%\LEIA-ME.txt"
echo 2. Copie a pasta "Antigravity" para dentro de "Local" >> "%DIST_DIR%\LEIA-ME.txt"
echo. >> "%DIST_DIR%\LEIA-ME.txt"
echo 3. Inicie o ONI e ative o mod no menu de Mods >> "%DIST_DIR%\LEIA-ME.txt"
echo. >> "%DIST_DIR%\LEIA-ME.txt"
echo 4. Clique em MULTIPLAYER no menu principal >> "%DIST_DIR%\LEIA-ME.txt"
echo. >> "%DIST_DIR%\LEIA-ME.txt"
echo Pronto! >> "%DIST_DIR%\LEIA-ME.txt"

:: Criar ZIP (usando PowerShell)
echo [4/4] Criando ZIP...
powershell -Command "Compress-Archive -Path '%DIST_DIR%\*' -DestinationPath '%DIST_DIR%\..\Antigravity_Mod.zip' -Force"

echo.
echo ==========================================
echo   Pacote criado com sucesso!
echo ==========================================
echo.
echo Arquivo: %PROJETO%\Antigravity_Mod.zip
echo.
echo Envie este ZIP para seus amigos!
echo Eles so precisam extrair na pasta de mods.
echo.
pause
