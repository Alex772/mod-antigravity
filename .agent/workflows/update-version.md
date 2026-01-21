---
description: Como atualizar a versão do mod Antigravity
---

# Atualizar Versão do Mod

Este workflow explica como atualizar a versão do mod de forma rápida.

## Comandos Disponíveis

### Incrementar versão patch (0.0.X)
```powershell
// turbo
.\update-version.ps1 patch
```

### Incrementar versão minor (0.X.0)
```powershell
// turbo
.\update-version.ps1 minor
```

### Incrementar versão major (X.0.0)
```powershell
// turbo
.\update-version.ps1 major
```

### Definir versão específica
```powershell
// turbo
.\update-version.ps1 1.0.0
```

## Workflow Completo de Release

1. Atualizar versão:
   ```powershell
   // turbo
   .\update-version.ps1 patch
   ```

2. Compilar em Release:
   ```powershell
   // turbo
   dotnet build -c Release
   ```

3. Commit e tag:
   ```powershell
   git add .
   git commit -m "Release v<NOVA_VERSAO>"
   git tag v<NOVA_VERSAO>
   git push && git push --tags
   ```

4. Atualizar Workshop (ver `/update-workshop`)
