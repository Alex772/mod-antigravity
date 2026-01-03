# 🔧 Ferramentas de Teste Local - Antigravity Multiplayer

Esta pasta contém scripts para testar o multiplayer localmente no **mesmo PC**, sem depender de outra pessoa.

> ⚠️ **Importante**: A funcionalidade de teste local só aparece em builds **DEBUG**. Em produção (Release), os usuários não terão acesso.

## 📋 Scripts Disponíveis

| Arquivo | Descrição |
|---------|-----------|
| `setup_test_instance.ps1` | **PowerShell** - Cria cópia do ONI (~3GB) |
| `start_host.bat` | Abre o ONI via Steam (será o Host) |
| `start_client.bat` | Abre a segunda instância (será o Client) |
| `sync_mods.bat` | Sincroniza mods após modificar código |
| `cleanup.bat` | Remove a cópia de teste (libera espaço) |

## 🚀 Guia Rápido

### Primeira vez (configuração inicial)
```powershell
# Execute no PowerShell:
powershell -ExecutionPolicy Bypass -File setup_test_instance.ps1
```

Isso cria:
- `D:\ONI_Test_Client` - Cópia do jogo
- `D:\ONI_Test_Client_Mods` - Cópia dos mods

### Para testar
```batch
1. Dê duplo-clique em: start_host.bat     (abre ONI pelo Steam)
2. Dê duplo-clique em: start_client.bat   (abre segunda instância)
3. No Host: F11 → clique "HOST"
4. No Client: F11 → digite 127.0.0.1:7777 → clique "JOIN"
5. No Host: clique "START GAME"
```

### Após modificar código
```batch
1. Execute: deploy.bat (na pasta raiz do mod)
2. Execute: sync_mods.bat (sincroniza para a segunda instância)
3. Reinicie ambas instâncias do ONI
```

## 🔄 Workflow Completo de Desenvolvimento

```
┌─────────────────────────────────────────────────────────┐
│  1. Modifique o código no VS Code                       │
├─────────────────────────────────────────────────────────┤
│  2. Execute: deploy.bat (raiz do projeto)               │
│     → Compila em DEBUG e copia para pasta de mods       │
├─────────────────────────────────────────────────────────┤
│  3. Execute: sync_mods.bat (esta pasta)                 │
│     → Sincroniza mods para a instância de teste         │
├─────────────────────────────────────────────────────────┤
│  4. Execute: start_host.bat + start_client.bat          │
│     → Abre duas instâncias do ONI                       │
├─────────────────────────────────────────────────────────┤
│  5. Teste o multiplayer localmente!                     │
│     Host: F11 → HOST                                    │
│     Client: F11 → 127.0.0.1:7777 → JOIN                 │
└─────────────────────────────────────────────────────────┘
```

## ⚠️ Requisitos

- **Espaço em disco**: ~3 GB (para a cópia do jogo)
- **Mod compilado em DEBUG**: Use `deploy.bat` (não `create_package.bat`)
- **Duas janelas de resolução menor**: Facilita visualizar lado a lado

## 🗑️ Limpeza

Para remover a instância de teste e liberar ~3GB:
```batch
cleanup.bat
```

## 🔧 Solução de Problemas

| Problema | Solução |
|----------|---------|
| Botão LOCAL TEST não aparece | Verifique se compilou em DEBUG |
| F11 não funciona | O hotkey só existe em DEBUG |
| Client não conecta | Verifique se o Host está escutando na porta 7777 |
| Mods não atualizaram | Execute `sync_mods.bat` e reinicie |
