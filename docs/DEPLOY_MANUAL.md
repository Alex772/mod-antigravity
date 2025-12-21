# Deploy Manual do Mod Antigravity

Este documento explica como fazer o deploy do mod manualmente para o Oxygen Not Included.

## 📁 Caminhos Importantes

| Item | Caminho |
|------|---------|
| **Projeto** | `d:\Desenvolvimento\ONI\mod antigravity` |
| **Build Output** | `d:\Desenvolvimento\ONI\mod antigravity\bin\Debug` |
| **Pasta de Mods Dev** | `C:\Users\Saikai\OneDrive\Documentos\Klei\OxygenNotIncluded\mods\Dev\Antigravity` |

---

## 🔨 Passo 1: Compilar o Projeto

Abra o PowerShell ou Terminal na pasta do projeto e execute:

```powershell
cd "d:\Desenvolvimento\ONI\mod antigravity"
dotnet build Antigravity.sln --configuration Debug
```

Se a compilação for bem-sucedida, você verá:
```
Construir êxito(s) em X.Xs
```

---

## 📦 Passo 2: Copiar Arquivos para a Pasta de Mods

### Arquivos DLL Necessários

Copie os seguintes arquivos de `bin\Debug\` para a pasta de mods:

| Arquivo | Descrição |
|---------|-----------|
| `Antigravity.dll` | Mod principal |
| `Antigravity.Core.dll` | Biblioteca core |
| `Antigravity.Client.dll` | Lógica do cliente |
| `Antigravity.Server.dll` | Lógica do servidor |
| `Antigravity.Patches.dll` | Patches Harmony |
| `LiteNetLib.dll` | Biblioteca de networking |

### Arquivos de Metadados

Copie da raiz do projeto:

| Arquivo | Descrição |
|---------|-----------|
| `mod.yaml` | Metadados do mod |
| `mod_info.yaml` | Informações de versão |

### Pasta de Assets

Copie a pasta `assets\` inteira para a pasta de mods.

---

## 🖥️ Comandos de Deploy (PowerShell)

### Opção 1: Copiar Tudo de Uma Vez

```powershell
# Defina os caminhos
$origem = "d:\Desenvolvimento\ONI\mod antigravity"
$destino = "C:\Users\Saikai\OneDrive\Documentos\Klei\OxygenNotIncluded\mods\Dev\Antigravity"

# Limpe a pasta de destino (opcional)
Remove-Item "$destino\*" -Recurse -Force -ErrorAction SilentlyContinue

# Copie as DLLs
Copy-Item "$origem\bin\Debug\Antigravity.dll" $destino -Force
Copy-Item "$origem\bin\Debug\Antigravity.Core.dll" $destino -Force
Copy-Item "$origem\bin\Debug\Antigravity.Patches.dll" $destino -Force
Copy-Item "$origem\bin\Debug\Antigravity.Client.dll" $destino -Force
Copy-Item "$origem\bin\Debug\Antigravity.Server.dll" $destino -Force
Copy-Item "$origem\bin\Debug\LiteNetLib.dll" $destino -Force

# Copie os metadados
Copy-Item "$origem\mod.yaml" $destino -Force
Copy-Item "$origem\mod_info.yaml" $destino -Force

# Copie os assets
Copy-Item "$origem\assets" $destino -Recurse -Force

Write-Host "Deploy concluído!" -ForegroundColor Green
```

### Opção 2: Usando o Explorador de Arquivos

1. Abra o **Explorador de Arquivos**
2. Navegue até `d:\Desenvolvimento\ONI\mod antigravity\bin\Debug`
3. Selecione os arquivos `.dll` listados acima
4. Copie (Ctrl+C)
5. Navegue até `C:\Users\Saikai\OneDrive\Documentos\Klei\OxygenNotIncluded\mods\Dev\Antigravity`
6. Cole (Ctrl+V)
7. Volte para `d:\Desenvolvimento\ONI\mod antigravity`
8. Copie `mod.yaml`, `mod_info.yaml` e a pasta `assets` para o mesmo destino

---

## 🎮 Passo 3: Testar no Jogo

1. **Inicie o Oxygen Not Included**
2. Vá para o **menu de Mods**
3. Ative **"Antigravity Multiplayer"**
4. **Reinicie o jogo** se solicitado
5. Verifique se o botão **"MULTIPLAYER"** aparece no menu principal

---

## 📋 Verificar Logs (se houver erros)

O log do ONI fica em:
```
%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log
```

Para visualizar erros do Antigravity:
```powershell
Get-Content "$env:USERPROFILE\AppData\LocalLow\Klei\Oxygen Not Included\Player.log" | Select-String "Antigravity"
```

---

## 📁 Estrutura Final da Pasta de Mods

Após o deploy, a pasta deve conter:

```
C:\Users\Saikai\OneDrive\Documentos\Klei\OxygenNotIncluded\mods\Dev\Antigravity\
├── assets\
│   ├── configs\
│   │   └── default_config.json
│   └── translations\
│       ├── en.po
│       └── pt-BR.po
├── Antigravity.dll
├── Antigravity.Core.dll
├── Antigravity.Client.dll
├── Antigravity.Server.dll
├── Antigravity.Patches.dll
├── LiteNetLib.dll
├── mod.yaml
└── mod_info.yaml
```

---

## 🔄 Script de Deploy Rápido

Você também pode usar o script pronto em `tools\scripts\deploy-local.ps1`:

```powershell
cd "d:\Desenvolvimento\ONI\mod antigravity"
.\tools\scripts\deploy-local.ps1 -Target Dev
```

---

## ⚠️ Problemas Comuns

### "Arquivo sendo usado por outro processo"
- Feche o ONI antes de fazer o deploy

### "Mod não aparece na lista"
- Verifique se `mod.yaml` foi copiado
- Verifique se os arquivos estão na pasta correta

### "Erro ao carregar o mod"
- Verifique o log em `Player.log`
- Procure por mensagens com "[Antigravity]"
