# 🛠️ Guia de Configuração do Ambiente de Desenvolvimento

## Pré-requisitos

### Software Necessário

| Software | Versão | Link |
|----------|--------|------|
| Visual Studio 2022 | Community+ | [Download](https://visualstudio.microsoft.com/) |
| .NET Framework | 4.7.2 | Incluído no VS |
| Git | Latest | [Download](https://git-scm.com/) |
| Oxygen Not Included | Steam | [Store](https://store.steampowered.com/app/457140/) |
| dnSpy ou ILSpy | Latest | [dnSpy](https://github.com/dnSpy/dnSpy) |

### Workloads do Visual Studio

Instale os seguintes workloads:
- ✅ Desenvolvimento para desktop com .NET
- ✅ Desenvolvimento de jogos com Unity (opcional, mas útil)

---

## 📋 Instalação Passo a Passo

### 1. Clone o Repositório

```powershell
cd D:\Desenvolvimento\ONI
git clone <REPO_URL> "mod antigravity"
cd "mod antigravity"
```

### 2. Localize a Instalação do ONI

Por padrão, o ONI está instalado em:
```
C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded
```

Você precisará das DLLs em:
```
OxygenNotIncluded_Data\Managed\
```

### 3. Configure as Variáveis de Ambiente

Crie um arquivo `local.props` na raiz do projeto (não commitado):

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <ONIPath>C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded</ONIPath>
    <ONIModsPath>$(USERPROFILE)\Documents\Klei\OxygenNotIncluded\mods\Dev</ONIModsPath>
  </PropertyGroup>
</Project>
```

### 4. Restaure as Dependências

```powershell
# Via NuGet
nuget restore Antigravity.sln

# Ou via dotnet CLI
dotnet restore Antigravity.sln
```

### 5. Compile o Projeto

```powershell
# Via PowerShell script
.\tools\scripts\build.ps1

# Ou via Visual Studio
# Abra Antigravity.sln e pressione Ctrl+Shift+B
```

---

## 🔗 Referências de DLLs do ONI

O projeto precisa referenciar as seguintes DLLs:

### Obrigatórias (do ONI)
| DLL | Caminho |
|-----|---------|
| `Assembly-CSharp.dll` | `OxygenNotIncluded_Data\Managed\` |
| `Assembly-CSharp-firstpass.dll` | `OxygenNotIncluded_Data\Managed\` |
| `UnityEngine.dll` | `OxygenNotIncluded_Data\Managed\` |
| `UnityEngine.CoreModule.dll` | `OxygenNotIncluded_Data\Managed\` |
| `UnityEngine.UI.dll` | `OxygenNotIncluded_Data\Managed\` |
| `0Harmony.dll` | `OxygenNotIncluded_Data\Managed\` |

### Via NuGet
| Package | Versão |
|---------|--------|
| `LiteNetLib` | Latest |
| `MessagePack` | Latest |
| `NUnit` | 3.x (dev only) |

---

## 🧪 Configuração de Testes

### Executar Testes Unitários

```powershell
# Via script
.\tools\scripts\run-tests.ps1

# Ou via dotnet
dotnet test tests\Antigravity.Tests.Unit
```

### Configurar Debugging

1. No Visual Studio, vá em **Debug > Attach to Process**
2. Encontre `OxygenNotIncluded.exe`
3. Selecione e clique **Attach**

Ou configure auto-attach no `launchSettings.json`:
```json
{
  "profiles": {
    "ONI Debug": {
      "commandName": "Executable",
      "executablePath": "$(ONIPath)\\OxygenNotIncluded.exe",
      "workingDirectory": "$(ONIPath)"
    }
  }
}
```

---

## 🚀 Deploy para Testes

### Deploy Automático (Desenvolvimento)

```powershell
# Compila e copia para pasta de mods
.\tools\scripts\deploy-local.ps1
```

Isso copia os arquivos para:
```
%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Dev\Antigravity\
```

### Estrutura do Mod Instalado

```
Antigravity/
├── Antigravity.dll           # DLL principal
├── Antigravity.Core.dll      # Biblioteca core
├── LiteNetLib.dll            # Dependência de rede
├── MessagePack.dll           # Dependência de serialização
├── mod.yaml                  # Metadados do mod
└── mod_info.yaml             # Info de versão
```

---

## 📝 Checklist de Verificação

Antes de começar a desenvolver, verifique:

- [ ] Visual Studio 2022 instalado
- [ ] ONI instalado e funcionando
- [ ] Repositório clonado
- [ ] `local.props` configurado
- [ ] DLLs do ONI localizadas
- [ ] Dependências NuGet restauradas
- [ ] Projeto compila sem erros
- [ ] Testes unitários passam
- [ ] Mod carrega no jogo (menu de mods)

---

## 🐛 Problemas Comuns

### "Could not find Assembly-CSharp.dll"
- Verifique se o caminho no `local.props` está correto
- Certifique-se de que o ONI está instalado

### "HarmonyLib not found"
- O ONI já inclui Harmony 2.0 em `0Harmony.dll`
- Não instale via NuGet, use a DLL do jogo

### "Mod não aparece na lista"
- Verifique se `mod.yaml` está presente
- Check o log em `%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\`

### Erros de versão do .NET
- ONI usa .NET 4.7.2 (não .NET Core/5/6/7)
- Configure o projeto para `<TargetFramework>net472</TargetFramework>`

---

## 📚 Recursos Úteis

- [ONI Modding Wiki](https://github.com/Cairath/Oxygen-Not-Included-Modding/wiki)
- [Harmony Documentation](https://harmony.pardeike.net/articles/intro.html)
- [ONI Multiplayer Discord](https://discord.gg/3TQ97w8Qwq)
- [LiteNetLib Docs](https://github.com/RevenantX/LiteNetLib)
