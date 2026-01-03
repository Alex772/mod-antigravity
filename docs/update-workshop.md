# Atualizar Mod na Steam Workshop

> **Estratégia atual:** Mod privado na Workshop para testers receberem updates automáticos.

## Fluxo Rápido de Update

```
1. Faça suas correções no código
2. Rode: create_package.bat
3. Abra o Uploader → Edit → Publish!
4. Testers reiniciam o ONI para receber a atualização
```

## Passo a Passo Detalhado

### 1. Faça suas correções no código
Edite os arquivos em `src/` conforme necessário.

### 2. (Opcional) Atualize o CHANGELOG.md
Adicione notas sobre o que mudou - ajuda a comunicar com testers.

### 3. Compile para dist
// turbo
```powershell
cd "d:\Desenvolvimento\ONI\mod antigravity"
.\create_package.bat
```

### 4. Abra o Uploader do ONI
Caminho: `C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncludedUploader`

### 5. Publique a atualização
- Selecione "Antigravity - Multiplayer Mod" na lista
- Clique em **Edit**
- Clique em **Publish!**
- Aguarde o upload completar

### 6. Avise os testers
Peça para eles:
1. Fecharem o ONI (se estiver aberto)
2. Reiniciarem o Steam (ou esperarem sync automático)
3. Abrirem o ONI novamente

## Dicas

- **Updates são automáticos** - Testers inscritos recebem a nova versão ao abrir o jogo
- **Mod privado** - Só quem tem o link pode se inscrever
- **Link do mod:** https://steamcommunity.com/sharedfiles/filedetails/?id=3635632392