---
description: Como atualizar o mod na Steam Workshop
---

# Atualizar Mod na Steam Workshop

## Passo a Passo

### 1. Atualize a versão (opcional mas recomendado)
Edite `mod.yaml` e atualize a versão se necessário:
```yaml
title: "Antigravity Multiplayer"
description: "..."
staticID: "Antigravity.Multiplayer"
version: "0.1.1"  # Incremente aqui
```

### 2. Atualize o CHANGELOG.md
Adicione as notas da nova versão no topo do arquivo.

### 3. Compile e faça deploy para dist
// turbo
```powershell
cd "d:\Desenvolvimento\ONI\mod antigravity"
.\create_package.bat
```

### 4. Abra o Uploader do ONI
Caminho: `C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncludedUploader`
- Abra o executável `OxygenNotIncludedUploader.exe`

### 5. Selecione o mod e edite
- Selecione "Antigravity - Multiplayer Mod" na lista
- Clique em "Edit"
- Verifique se o caminho do Content está correto: `d:\Desenvolvimento\ONI\mod antigravity\dist\Antigravity`

### 6. Atualize a descrição (se necessário)
Se houver novas features, atualize a Description.

### 7. Publique!
- Clique em "Publish!"
- Aguarde o upload completar
- Os usuários inscritos receberão a atualização automaticamente!

## Notas

- **Não precisa de versão obrigatória** - A Steam Workshop não exige versionamento
- **Changelog é bom ter** - Ajuda os usuários a saber o que mudou
- **A imagem preview pode ser mantida** - Só atualize se quiser mudar
- **Updates são automáticos** - Usuários inscritos recebem a nova versão ao abrir o jogo
