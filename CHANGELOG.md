# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.33] - 2026-01-21

### Added
- **Player Ready Sync** - Jogo só despausa quando todos os jogadores terminam de carregar
  - Host bloqueado de despausar até todos estarem prontos
  - Auto-unpause quando todos os clientes enviam `ClientReady`
- **Version Update Script** - Script `update-version.ps1` para atualizar versão facilmente
  - Suporta `major`, `minor`, `patch` ou versão específica
  - Workflow `/update-version` criado

### Fixed
- **Pause/Unpause Sync** - Comandos agora são processados mesmo quando jogo pausado
  - `MultiplayerUpdater.Update()` processa mensagens de rede sempre
  - Cliente recebe e executa comando de Unpause corretamente
- **ChoreStart Error** - Corrigido erro silencioso ao executar ChoreStart
  - Adicionado null checks em `DuplicantSyncManager.Instance`
  - Try-catch com stack trace para diagnóstico
- **Minion Registration** - Minions agora são registrados corretamente no cliente
  - Novo método `RefreshMinionList()` para re-escanear minions
  - Auto-retry quando minion não encontrado

## [0.0.31] - 2026-01-20

### Added
- **Duplicant Synchronization** - Sistema completo de sincronização de duplicantes
  - `DuplicantSyncManager` para rastrear e sincronizar minions
  - `PositionSyncCommand` com dados de posição, movimento e tarefa atual
  - `ChoreStartCommand` e `ChoreEndCommand` para sync de tarefas
  - Checksum verification para detectar desyncs
- **Element Sync** - Sincronização de mudanças no grid de elementos
  - `ElementSyncManager` com delta broadcasting
  - `ElementChangeCommand` para propagar mudanças
- **Schedule Sync** - Sincronização de cronogramas de duplicantes
  - `ScheduleSyncCommand` para propagar mudanças de schedule
- **Skills Sync** - Sincronização de habilidades aprendidas
  - `SkillsSyncCommand` para propagar skill points
- **Research Sync** - Sincronização de progresso de pesquisa
  - `ResearchSyncCommand` para propagar unlocks

### Changed
- Sync Engine agora roda a 60 FPS com position sync a cada 120 ticks (~2s)
- Patches refatorados para usar `CommandManager.IsExecutingRemoteCommand`

## [0.0.25] - 2026-01-15

### Added
- **Steam P2P Networking** - Conexão via Steam sem necessidade de IP
  - `SteamNetworkManager` para comunicação P2P
  - NAT traversal automático via Steam
- **Lobby System** - Sistema completo de lobby
  - Criar lobby como Host
  - Juntar via código de lobby
  - Lista de jogadores conectados
- **World Data Transfer** - Transferência de save game para clientes
  - Compressão GZip para dados do mundo
  - Envio em chunks de 64KB
  - Progress tracking durante download

### Changed
- Migrado de LiteNetLib para Steam P2P (produção)
- LiteNetLib mantido para modo local/debug

## [0.0.20] - 2026-01-10

### Added
- **Building Commands Sync** - Sincronização de construções
  - `BuildCommand` para colocar construções
  - `DeconstructCommand` para demolir
  - `UtilityBuildCommand` para fios/tubos com conexões
- **Tool Commands Sync** - Sincronização de ferramentas
  - `DigCommand` para escavação
  - `MopCommand` para limpar líquidos
  - `ClearCommand` para limpar debris
  - `HarvestCommand` para colheita
  - `DisinfectCommand` para desinfectar
  - `CaptureCommand` para capturar critters
- **Priority Commands** - Sincronização de prioridades
  - `PriorityCommand` para prioridade de errands
  - `BulkPriorityCommand` para prioritize tool
  - `BuildingPriorityCommand` para prioridade de building

### Changed
- Sistema de comandos refatorado para usar Harmony patches
- Cada comando tem patch Postfix que envia para rede

## [0.0.15] - 2026-01-05

### Added
- **Speed/Pause Sync** - Sincronização de velocidade do jogo
  - `SpeedCommand` para mudança de velocidade (1x, 2x, 3x)
  - `PauseGame` e `UnpauseGame` commands
  - Patches em `SpeedControlScreen`
- **Building Settings Sync** - Sincronização de configurações
  - `DoorStateCommand` para estado de portas
  - `StorageFilterCommand` para filtros de storage
  - `StorageCapacityCommand` para capacidade
  - `BuildingEnabledCommand` para enable/disable
  - `AssignableCommand` para assignments
  - `FilterableCommand` para element filters
  - `ThresholdCommand` para thresholds
  - `LogicSwitchCommand` para switches lógicos

## [0.0.10] - 2025-12-28

### Added
- **Command System** - Arquitetura de comandos para multiplayer
  - `CommandManager` para enviar/receber comandos
  - `GameCommand` base class com tipos diversos
  - Serialização JSON com compressão
  - Queue de comandos pendentes
- **Network Backend** - Suporte a múltiplos backends
  - `NetworkBackendManager` para abstração
  - `PlayerId` para identificação cross-backend
  - Heartbeat system para detectar disconnects
- **Chat System** - Chat em jogo
  - `ChatManager` para mensagens
  - `ChatOverlay` para UI de chat

## [0.0.5] - 2025-12-21

### Added
- **Multiplayer Menu** - Botão MULTIPLAYER no menu principal
  - `MainMenuPatch` para injetar botão
  - `MultiplayerLobbyScreen` para UI de lobby
- **Project Structure** - Estrutura base do projeto
  - `Antigravity.Core` - Lógica core
  - `Antigravity.Client` - UI e cliente
  - `Antigravity.Patches` - Harmony patches
  - `Antigravity.Mod` - Entry point
  - `Antigravity.Server` - (reservado)
- **Build System** - Scripts de build e deploy
  - `deploy.bat` para deploy local
  - `create_package.bat` para workshop

---

## Version History

| Version | Date | Status |
|---------|------|--------|
| 0.0.33 | 2026-01-21 | Current - Sync fixes |
| 0.0.31 | 2026-01-20 | Duplicant sync |
| 0.0.25 | 2026-01-15 | Steam P2P |
| 0.0.20 | 2026-01-10 | Building/Tool sync |
| 0.0.15 | 2026-01-05 | Speed/Settings sync |
| 0.0.10 | 2025-12-28 | Command system |
| 0.0.5 | 2025-12-21 | Initial structure |
