# Antigravity Multiplayer - Design Document

## 📋 Visão Geral

Este documento descreve o fluxo completo do modo multiplayer para o mod Antigravity, desde a conexão até o gameplay sincronizado.

---

## 🎯 Objetivos

1. Permitir que 2-4 jogadores joguem na mesma colônia
2. Sincronizar comandos em tempo real
3. Usar Steam P2P para conexão (sem IP, sem port forwarding)
4. Manter o jogo estável e sem dessincronização

---

## 🔄 Fluxo Completo

### Diagrama de Estados

```
┌─────────────────────────────────────────────────────────────────┐
│                         MENU PRINCIPAL                          │
│                              ↓                                  │
│                    [Botão MULTIPLAYER]                          │
└─────────────────────────────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────┐
│                      TELA DE LOBBY                              │
│  ┌─────────────────┐         ┌─────────────────┐                │
│  │   HOST GAME     │    ou   │   JOIN GAME     │                │
│  └────────┬────────┘         └────────┬────────┘                │
│           ↓                           ↓                         │
│    Cria lobby Steam           Entra no lobby                    │
│    Gera código                via código                        │
│           ↓                           ↓                         │
│  ┌────────────────────────────────────────────┐                 │
│  │            LOBBY ATIVO                      │                │
│  │  - Lista de jogadores                       │                │
│  │  - Código para compartilhar                 │                │
│  │  - [START GAME] (só host)                   │                │
│  │  - [LEAVE LOBBY]                            │                │
│  └────────────────────────────────────────────┘                 │
└─────────────────────────────────────────────────────────────────┘
                               ↓
                    Host clica [START GAME]
                               ↓
┌─────────────────────────────────────────────────────────────────┐
│                   SELEÇÃO DE JOGO (Host)                        │
│  ┌─────────────────┐         ┌─────────────────┐                │
│  │   NOVO JOGO     │    ou   │  CARREGAR SAVE  │                │
│  └────────┬────────┘         └────────┬────────┘                │
│           ↓                           ↓                         │
│    Configurar mundo           Selecionar arquivo                │
│    e colônia                  de save                           │
└─────────────────────────────────────────────────────────────────┘
                               ↓
                    Host confirma seleção
                               ↓
┌─────────────────────────────────────────────────────────────────┐
│                   CARREGAMENTO SINCRONIZADO                     │
│                                                                 │
│  Host:                          Clientes:                       │
│  1. Carrega o mundo             1. Recebem notificação          │
│  2. Pausa o jogo                2. Mostram tela de loading      │
│  3. Envia estado inicial        3. Recebem estado do mundo      │
│  4. Aguarda confirmação         4. Carregam mundo               │
│  5. Despausa quando todos OK    5. Confirmam que estão prontos  │
└─────────────────────────────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────┐
│                      GAMEPLAY MULTIPLAYER                       │
│                                                                 │
│  - Comandos sincronizados em tempo real                         │
│  - Cursores visíveis de outros jogadores                        │
│  - Chat integrado                                               │
│  - Indicadores de ações                                         │
│  - Pause sincronizado                                           │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📊 Estados do Sistema

| Estado | Host | Cliente | Descrição |
|--------|------|---------|-----------|
| `Disconnected` | ✓ | ✓ | Não conectado a nenhum lobby |
| `InLobby` | ✓ | ✓ | No lobby, aguardando início |
| `LoadingGame` | ✓ | ✓ | Carregando o mundo |
| `Syncing` | ✓ | ✓ | Sincronizando estado inicial |
| `Playing` | ✓ | ✓ | Jogando ativamente |
| `Paused` | ✓ | ✓ | Jogo pausado |

---

## 🎮 Detalhes do Fluxo

### 1. HOST GAME

```csharp
// Quando host clica "HOST GAME":
1. SteamNetworkManager.HostGame(maxPlayers: 4)
2. Steam cria lobby e retorna código
3. UI mostra código + botão COPY CODE
4. UI mostra botão START GAME (desabilitado até ter 2+ jogadores, ou habilitado para teste solo)
5. Host aguarda jogadores entrarem
```

### 2. JOIN GAME

```csharp
// Quando cliente cola código e clica "JOIN":
1. SteamNetworkManager.JoinByCode(code)
2. Steam conecta ao lobby
3. Cliente aparece na lista de jogadores do host
4. Cliente vê lista de jogadores e aguarda host iniciar
```

### 3. START GAME (Host Only)

```csharp
// Quando host clica "START GAME":
1. Fecha tela do lobby
2. Mostra opções:
   a) "New Colony" → Abre tela de configuração de mundo
   b) "Load Save" → Abre seletor de saves
3. Host seleciona/configura
4. Host confirma → Envia mensagem "GAME_STARTING" para todos
```

### 4. LOADING (Todos)

```csharp
// Mensagem GAME_STARTING recebida:
// Host:
1. Carrega o mundo/save
2. Pausa simulação
3. Serializa estado do mundo
4. Envia "WORLD_DATA" para cada cliente

// Clientes:
1. Mostram tela "Connecting to host..."
2. Recebem "GAME_STARTING" → Mostram "Loading world..."
3. Recebem "WORLD_DATA" → Deserializam e carregam mundo
4. Enviam "READY" para host
```

### 5. SYNC CHECK

```csharp
// Host recebe READY de todos:
1. Verifica se todos estão prontos
2. Envia "GAME_START" com tick inicial
3. Despausa simulação

// Clientes:
1. Recebem "GAME_START"
2. Sincronizam tick
3. Despausam simulação
4. Gameplay começa!
```

---

## 📡 Protocolo de Mensagens

### Tipos de Mensagem

| Tipo | Direção | Descrição |
|------|---------|-----------|
| `LOBBY_UPDATE` | Host → All | Atualização da lista de jogadores |
| `GAME_STARTING` | Host → All | Host está iniciando o jogo |
| `WORLD_DATA` | Host → Client | Dados do mundo (save) |
| `PLAYER_READY` | Client → Host | Cliente está pronto |
| `GAME_START` | Host → All | Começar gameplay |
| `COMMAND` | Any → Host | Comando de jogo (dig, build, etc) |
| `COMMAND_BROADCAST` | Host → All | Comando validado para todos executarem |
| `SYNC_CHECK` | Host → All | Verificação de sincronização |
| `SYNC_RESPONSE` | Client → Host | Checksum do estado |
| `PAUSE` | Any → All | Pausar jogo |
| `UNPAUSE` | Any → All | Despausar jogo |
| `CHAT` | Any → All | Mensagem de chat |
| `CURSOR_UPDATE` | Any → All | Posição do cursor |

### Estrutura das Mensagens

```csharp
[Serializable]
public class NetworkMessage
{
    public MessageType Type;      // Tipo da mensagem
    public ulong SenderSteamId;   // Quem enviou
    public long Timestamp;        // Tick do jogo
    public byte[] Payload;        // Dados serializados
}

public enum MessageType
{
    LobbyUpdate,
    GameStarting,
    WorldData,
    PlayerReady,
    GameStart,
    Command,
    CommandBroadcast,
    SyncCheck,
    SyncResponse,
    Pause,
    Unpause,
    Chat,
    CursorUpdate
}
```

---

## 🔧 Sincronização de Comandos

### Modelo: Lockstep Determinístico

Todos os jogadores executam os mesmos comandos no mesmo tick.

```
┌─────────────────────────────────────────────────────────────────┐
│  Jogador A         │  Rede (Host)      │  Jogador B            │
├────────────────────┼───────────────────┼───────────────────────┤
│  1. Clica "Dig"    │                   │                       │
│  2. Envia COMMAND  │→ Recebe comando   │                       │
│                    │  Valida comando   │                       │
│                    │  Broadcast        │→ Recebe broadcast     │
│  3. Executa dig    │  (tick 1000)      │  3. Executa dig       │
│                    │                   │     (tick 1000)       │
├────────────────────┼───────────────────┼───────────────────────┤
│                    │                   │  1. Clica "Build"     │
│                    │← Recebe comando   │  2. Envia COMMAND     │
│                    │  Valida comando   │                       │
│  3. Executa build  │← Broadcast        │  3. Executa build     │
│     (tick 1005)    │  (tick 1005)      │     (tick 1005)       │
└────────────────────┴───────────────────┴───────────────────────┘
```

### Comandos Sincronizados

| Categoria | Comandos |
|-----------|----------|
| **Construção** | Build, Cancel, Deconstruct |
| **Escavação** | Dig, Cancel Dig |
| **Prioridades** | Set Priority |
| **Duplicantes** | Assign Job, Set Schedule |
| **Pesquisa** | Queue Research, Cancel Research |
| **Errands** | Move To, Fetch, Deliver |
| **UI** | Pause, Speed Change |

---

## 🖥️ UI Multiplayer In-Game

### Elementos Visuais

```
┌─────────────────────────────────────────────────────────────────┐
│ [2 Players Connected]  [Ping: 45ms]              [💬 Chat]     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│                        GAME WORLD                               │
│                                                                 │
│     🔵 ← Cursor do Jogador 1 (Você)                             │
│                                                                 │
│                              🟢 ← Cursor do Jogador 2           │
│                                                                 │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ Chat:                                                           │
│ [Jogador2]: Vou cavar à direita                                 │
│ [Você]: Ok, vou construir geradores                             │
└─────────────────────────────────────────────────────────────────┘
```

### Overlay Multiplayer

- **Indicador de conexão**: Número de jogadores + ping
- **Cursores coloridos**: Cada jogador tem uma cor
- **Indicadores de ação**: Mostra o que cada jogador está fazendo
- **Chat box**: Comunicação em tempo real
- **Player list**: Lista de jogadores com status

---

## 🗂️ Arquitetura de Código

### Estrutura de Pastas

```
src/
├── Antigravity.Core/
│   ├── Network/
│   │   ├── SteamNetworkManager.cs    ✅ (Existe)
│   │   ├── NetworkMessage.cs         ⬜ (Criar)
│   │   ├── MessageSerializer.cs      ⬜ (Criar)
│   │   └── MessageHandler.cs         ⬜ (Criar)
│   ├── Sync/
│   │   ├── SyncEngine.cs             ✅ (Existe)
│   │   ├── CommandQueue.cs           ⬜ (Criar)
│   │   ├── WorldSerializer.cs        ⬜ (Criar)
│   │   └── SyncValidator.cs          ⬜ (Criar)
│   └── Commands/
│       ├── ICommand.cs               ✅ (Existe)
│       ├── CommandDispatcher.cs      ✅ (Existe)
│       ├── BuildCommand.cs           ⬜ (Criar)
│       ├── DigCommand.cs             ⬜ (Criar)
│       └── ...                       ⬜ (Criar)
│
├── Antigravity.Client/
│   ├── MultiplayerLobbyScreen.cs     ✅ (Existe)
│   ├── MultiplayerHUD.cs             ⬜ (Criar)
│   ├── PlayerCursors.cs              ⬜ (Criar)
│   └── ChatOverlay.cs                ⬜ (Criar)
│
├── Antigravity.Patches/
│   ├── UI/
│   │   └── MainMenuPatch.cs          ✅ (Existe)
│   ├── Game/
│   │   ├── BuildToolPatch.cs         ⬜ (Criar)
│   │   ├── DigToolPatch.cs           ⬜ (Criar)
│   │   └── ...                       ⬜ (Criar)
│   └── Sim/
│       └── SimTickPatch.cs           ⬜ (Criar)
│
└── Antigravity.Server/
    ├── ServerManager.cs              ✅ (Existe)
    ├── PlayerSession.cs              ⬜ (Criar)
    └── GameSession.cs                ⬜ (Criar)
```

---

## 📅 Plano de Implementação

### Fase 1: Lobby → Jogo (Próxima)
- [ ] Botão START GAME no lobby
- [ ] Tela de seleção (novo/carregar)
- [ ] Mensagem GAME_STARTING
- [ ] Loading sincronizado básico

### Fase 2: Sincronização Inicial
- [ ] Serialização do mundo
- [ ] Envio de WORLD_DATA
- [ ] Carregamento no cliente
- [ ] Handshake de início

### Fase 3: Comandos Básicos
- [ ] Patch para BuildTool
- [ ] Patch para DigTool
- [ ] Sistema de broadcast de comandos
- [ ] Execução sincronizada

### Fase 4: UI In-Game
- [ ] HUD multiplayer
- [ ] Cursores de jogadores
- [ ] Chat básico

### Fase 5: Polish
- [ ] Reconexão
- [ ] Tratamento de erros
- [ ] Verificação de desync
- [ ] Otimização de rede

---

## ⚠️ Desafios Técnicos

### 1. Determinismo
ONI usa simulação física que pode não ser 100% determinística. Soluções:
- Sincronizar estado periodicamente
- Detectar desync e corrigir

### 2. Tamanho do Save
Saves podem ser grandes (10-50MB). Soluções:
- Compressão (GZip)
- Envio em chunks
- Delta sync (só diferenças)

### 3. Latência
Comandos devem parecer responsivos. Soluções:
- Predição local
- Buffer de comandos
- Rollback se necessário

### 4. Mods de Terceiros
Outros mods podem causar desync. Soluções:
- Validar mods instalados
- Avisar sobre incompatibilidades

---

## 📝 Notas de Implementação

### Prioridade Alta
1. Fluxo Lobby → Jogo funcionando
2. Ambos jogadores no mesmo mundo
3. Comandos básicos sincronizados

### Prioridade Média
1. Chat
2. Cursores
3. Reconexão

### Prioridade Baixa
1. Mais de 2 jogadores
2. Permissões avançadas
3. Modo espectador

---

*Documento criado em: 21/12/2024*
*Versão: 1.0*
