# 🚀 ONI Antigravity Multiplayer Mod - Arquitetura

## 📋 Visão Geral

Este documento descreve a arquitetura do mod multiplayer **Antigravity** para Oxygen Not Included.

### Estratégia de Sincronização

Baseado na análise do mod existente (oni_multiplayer), nossa estratégia será:

1. **Input Sync**: Sincronizar apenas inputs dos jogadores, não o estado completo do mundo
2. **Determinismo**: Assumir que a simulação roda igual em máquinas diferentes
3. **Hard Sync**: Sincronização completa periódica (a cada dia do jogo) via save file
4. **Soft Sync**: Sincronização incremental de áreas pequenas (16x16) para áreas críticas

```
┌─────────────────────────────────────────────────────────────┐
│                    ARQUITETURA DO MOD                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   ┌─────────────┐     ┌─────────────┐     ┌─────────────┐   │
│   │   CLIENT    │◄───►│   NETWORK   │◄───►│   SERVER    │   │
│   │   LAYER     │     │   LAYER     │     │   LAYER     │   │
│   └──────┬──────┘     └─────────────┘     └──────┬──────┘   │
│          │                                        │          │
│   ┌──────▼──────┐                         ┌──────▼──────┐   │
│   │    INPUT    │                         │    GAME     │   │
│   │   HANDLER   │                         │   STATE     │   │
│   └──────┬──────┘                         └──────┬──────┘   │
│          │                                        │          │
│   ┌──────▼──────┐                         ┌──────▼──────┐   │
│   │   HARMONY   │                         │    SYNC     │   │
│   │   PATCHES   │                         │   ENGINE    │   │
│   └─────────────┘                         └─────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎯 Fase 1: Controle Compartilhado

### Funcionalidades Planejadas

| Prioridade | Feature | Descrição |
|------------|---------|-----------|
| P0 | Lobby System | Criar/Entrar em jogos multiplayer |
| P0 | Input Sync | Sincronizar comandos básicos |
| P1 | UI Sync | Sincronizar menus e configurações da colônia |
| P1 | Tool Sync | Sincronizar ferramentas (dig, build, etc) |
| P2 | Player Cursors | Mostrar cursores de outros jogadores |
| P2 | Chat System | Sistema de chat in-game |
| P3 | Hard Sync | Sincronização completa periódica |

### Componentes Principais

1. **Network Manager**: Gerencia conexões P2P ou Cliente-Servidor
2. **Command Dispatcher**: Captura e distribui comandos dos jogadores
3. **State Synchronizer**: Mantém estados sincronizados
4. **Harmony Patches**: Intercepta e modifica comportamentos do jogo

---

## 🔮 Fase 2: Colônias Separadas (Futuro)

### Funcionalidades Futuras

- Cada jogador com seu asteroide
- Sistema de troca de recursos
- Missões cooperativas
- Duplicants visitantes

---

## 📁 Referência de Estrutura

Ver `PROJECT_STRUCTURE.md` para detalhes da organização de arquivos.

---

## 🔧 Tecnologias

| Componente | Tecnologia | Versão |
|------------|------------|--------|
| Linguagem | C# | .NET 4.7.2 |
| Patcher | HarmonyLib | 2.x |
| Networking | LiteNetLib | Latest |
| Serialização | MessagePack | Latest |
| Build | MSBuild | Latest |
| Testes | NUnit | 3.x |

---

## 📝 Notas Importantes

1. **Determinismo**: O jogo DEVE rodar exatamente igual em todas as máquinas
2. **Latência**: Comandos são bufferizados e aplicados no próximo tick
3. **Reconciliação**: Em caso de divergência, o servidor é a fonte da verdade
4. **Save Compatibility**: Saves devem funcionar com e sem o mod
