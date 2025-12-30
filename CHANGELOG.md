# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Utility Build Sync** - Wires, pipes, conduits sync correctly with proper connections
  - New `UtilityBuildCommand` with path-based synchronization
  - Two-step execution: register connections then place buildings
  - Works for power wires, liquid/gas pipes, solid conduits, logic wires
- **Tool Sync Commands** - Full synchronization for:
  - Mop tool (`MopCommand`)
  - Clear tool (`ClearCommand`)
  - Harvest tool (`HarvestCommand`)
  - Disinfect tool (`DisinfectCommand`)
  - Capture tool (`CaptureCommand`)
  - Prioritize tool (`BulkPriorityCommand`)
- **Door State Sync** - Door open/close/auto states synchronize (`DoorStateCommand`)
- **Storage Filter Sync** - Storage bin filters synchronize (`StorageFilterCommand`)
- **Building Settings Sync** - Building settings panels synchronize
- **Steam P2P Networking** - Replaced IP-based connection with Steam lobby system
  - No need for IP addresses or port forwarding
  - NAT traversal handled automatically by Steam
- **Multiplayer Menu Button** - Added "MULTIPLAYER" button to main menu
- **Lobby System** - Full lobby UI with:
  - HOST GAME - Create Steam lobby
  - JOIN GAME - Join via lobby code
  - COPY CODE - One-click copy lobby code to clipboard
  - Player list with names
- **Start Game Flow** - Host can select game mode:
  - NEW COLONY - Opens ONI's new game screen
  - LOAD SAVE - Opens ONI's save selection screen
  - Back to lobby
- **Game Integration** - Full integration with ONI's game flow:
  - Detects when save is loaded
  - Reads save data (ready for sync)
  - Detects when world is ready (Game.OnSpawn)
- **MultiplayerState** - Global state tracking for multiplayer sessions
- **Network Messages** - Message types for game sync (GameStarting, WorldData, etc)
- **Message Serialization** - JSON + GZip compression for network messages
- **Waiting State** - Clients see "Waiting for host..." message
- **Deploy Scripts** - Added `deploy.bat` and `create_package.bat`
- **Documentation** - Added MULTIPLAYER_DESIGN.md, TESTING_GUIDE.md, DEPLOY_MANUAL.md

### Changed
- Replaced MessagePack with Newtonsoft.Json for serialization (ONI compatibility)
- Updated MainMenuPatch to use reflection for accessing private ONI methods
- Improved UI layout and styling

### Fixed
- Fixed utility builds not syncing correctly (isolated segments)
- Fixed lobby screen not appearing after multiple opens
- Fixed UI state not resetting when reopening lobby
- Fixed waiting text visibility control


## [0.1.0-alpha] - 2024-12-21

### Added
- Initial project structure
- Core networking layer using LiteNetLib
- Command dispatcher system
- Sync engine with hard sync support
- Basic client and server managers
- Build and deploy scripts
- Unit test framework
- Steam P2P lobby system (working)
- Multiplayer UI in main menu

### Status
- ✅ Steam lobby creation working
- ✅ Lobby code sharing working
- ✅ Start game UI flow working
- ⏳ Game synchronization (Phase 2)
- ⏳ Command sync (Phase 3)

---

## Version History

| Version | Date | Status |
|---------|------|--------|
| 0.1.0-alpha | 2024-12-21 | In Development |
