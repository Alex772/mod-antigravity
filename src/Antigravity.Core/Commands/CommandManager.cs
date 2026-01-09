using System;
using System.Collections.Generic;
using UnityEngine;
using Antigravity.Core.Network;
using Newtonsoft.Json;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Manages sending and receiving game commands for multiplayer sync.
    /// Command execution methods are in separate partial class files under Handlers/.
    /// </summary>
    public static partial class CommandManager
    {
        // Queue of commands waiting to be executed
        private static Queue<GameCommand> _pendingCommands = new Queue<GameCommand>();
        
        // Flag to prevent re-sending commands we just received
        private static bool _isExecutingRemoteCommand = false;
        
        // Events
        public static event Action<GameCommand> OnCommandReceived;
        public static event Action<GameCommand> OnCommandSent;

        /// <summary>
        /// Initialize the command manager.
        /// </summary>
        public static void Initialize()
        {
            // Subscribe to Steam network events (for production)
            SteamNetworkManager.OnDataReceived += OnNetworkDataReceived;
            
            // Subscribe to NetworkBackendManager events (for local testing/debugger)
            NetworkBackendManager.OnDataReceived += OnLocalNetworkDataReceived;
            
            Debug.Log("[Antigravity] CommandManager initialized (Steam + Local mode).");
        }
        
        /// <summary>
        /// Check if we're currently executing a remote command (to prevent echo).
        /// </summary>
        public static bool IsExecutingRemoteCommand => _isExecutingRemoteCommand;

        #region Network Communication
        
        /// <summary>
        /// Handle data received via NetworkBackendManager (local mode).
        /// </summary>
        private static void OnLocalNetworkDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.Log($"[Antigravity] CommandManager.OnLocalNetworkDataReceived: {e.Data?.Length ?? 0} bytes from peer {e.Sender.Value}");
            
            if (e.Data == null || e.Data.Length == 0)
            {
                Debug.LogWarning("[Antigravity] Received empty data from local peer");
                return;
            }
            
            var message = MessageSerializer.Deserialize(e.Data);
            if (message == null) 
            {
                Debug.LogWarning($"[Antigravity] Failed to deserialize message from local peer");
                return;
            }

            Debug.Log($"[Antigravity] Deserialized message: Type={message.Type}, SenderId={message.SenderSteamId}");

            if (message.Type == MessageType.Command)
            {
                try
                {
                    var baseCommand = MessageSerializer.DeserializePayload<GameCommand>(message.Payload);
                    if (baseCommand == null) return;

                    GameCommand command = DeserializeCommand(baseCommand.Type, message.Payload);
                    if (command != null)
                    {
                        Debug.Log($"[Antigravity] Local command: {command.Type}");
                        HandleReceivedCommandLocal(command, e.Sender);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to deserialize local command: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Handle a command received from local network (terminal client/debugger).
        /// </summary>
        private static void HandleReceivedCommandLocal(GameCommand command, PlayerId sender)
        {
            Debug.Log($"[Antigravity] Processing local command: {command.Type} from player {sender.Value}");
            
            // If we're the host, relay to other clients (but not back to sender)
            if (NetworkBackendManager.IsHost && NetworkBackendManager.Active != null)
            {
                RelayCommandLocal(command, sender);
            }

            // Queue the command for execution
            _pendingCommands.Enqueue(command);
            OnCommandReceived?.Invoke(command);
        }
        
        /// <summary>
        /// Relay a command from one client to all other local clients (LiteNetLib).
        /// </summary>
        private static void RelayCommandLocal(GameCommand command, PlayerId originalSender)
        {
            var message = MessageSerializer.CreateMessage(
                MessageType.Command,
                command,
                command.SenderSteamId,
                command.GameTick
            );

            byte[] data = MessageSerializer.Serialize(message);
            
            // Send to all clients except the original sender
            NetworkBackendManager.SendToAllExcept(originalSender, data);
            Debug.Log($"[Antigravity] Relayed command {command.Type} to other clients (excluding {originalSender.Value})");
        }

        /// <summary>
        /// Send a command to all other players.
        /// </summary>
        public static void SendCommand(GameCommand command)
        {
            // Don't send if we're executing a remote command (prevent echo)
            if (_isExecutingRemoteCommand) return;
            
            // Check connection based on backend
            bool isConnected = NetworkBackendManager.IsLocalMode 
                ? NetworkBackendManager.IsConnected 
                : SteamNetworkManager.IsConnected;
            
            if (!isConnected) return;

            try
            {
                // Set sender info based on backend
                command.SenderSteamId = NetworkBackendManager.IsLocalMode 
                    ? (ulong)NetworkBackendManager.LocalPlayerId.Value 
                    : SteamNetworkManager.LocalSteamId.m_SteamID;
                command.GameTick = GetCurrentTick();

                // Create network message
                var message = MessageSerializer.CreateMessage(
                    MessageType.Command,
                    command,
                    command.SenderSteamId,
                    command.GameTick
                );

                byte[] data = MessageSerializer.Serialize(message);

                // Route to appropriate backend
                if (NetworkBackendManager.IsLocalMode && NetworkBackendManager.Active != null)
                {
                    // Local mode uses NetworkBackendManager (LiteNetLib)
                    if (NetworkBackendManager.IsHost)
                    {
                        NetworkBackendManager.SendToAll(data);
                        Debug.Log($"[Antigravity] Sent command via LiteNetLib: {command.Type}");
                    }
                    else
                    {
                        NetworkBackendManager.SendTo(NetworkBackendManager.HostPlayerId, data);
                        Debug.Log($"[Antigravity] Sent command to host via LiteNetLib: {command.Type}");
                    }
                }
                else
                {
                    // Steam mode uses SteamNetworkManager
                    if (SteamNetworkManager.IsHost)
                    {
                        SteamNetworkManager.SendToAll(data);
                    }
                    else
                    {
                        SteamNetworkManager.SendTo(SteamNetworkManager.HostSteamId, data);
                    }
                }

                OnCommandSent?.Invoke(command);
                Debug.Log($"[Antigravity] Sent command: {command.Type}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to send command: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when network data is received from Steam.
        /// </summary>
        private static void OnNetworkDataReceived(Steamworks.CSteamID sender, byte[] data)
        {
            var message = MessageSerializer.Deserialize(data);
            if (message == null) return;

            if (message.Type == MessageType.Command)
            {
                try
                {
                    var baseCommand = MessageSerializer.DeserializePayload<GameCommand>(message.Payload);
                    if (baseCommand == null) return;

                    GameCommand command = DeserializeCommand(baseCommand.Type, message.Payload);
                    if (command != null)
                    {
                        HandleReceivedCommand(command, sender);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to deserialize command: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle a command received from another player via Steam.
        /// </summary>
        private static void HandleReceivedCommand(GameCommand command, Steamworks.CSteamID sender)
        {
            Debug.Log($"[Antigravity] Received command: {command.Type} from {SteamNetworkManager.GetPlayerName(sender)}");

            // If we're host, relay the command to other clients
            if (SteamNetworkManager.IsHost && SteamNetworkManager.ConnectedPlayers.Count > 1)
            {
                RelayCommand(command, sender);
            }

            // Queue the command for execution
            _pendingCommands.Enqueue(command);
            OnCommandReceived?.Invoke(command);
        }

        /// <summary>
        /// Relay a command from one client to all other clients via Steam.
        /// </summary>
        private static void RelayCommand(GameCommand command, Steamworks.CSteamID originalSender)
        {
            var message = MessageSerializer.CreateMessage(
                MessageType.Command,
                command,
                command.SenderSteamId,
                command.GameTick
            );

            byte[] data = MessageSerializer.Serialize(message);

            foreach (var player in SteamNetworkManager.ConnectedPlayers)
            {
                // Don't send back to the original sender
                if (player.m_SteamID != originalSender.m_SteamID)
                {
                    SteamNetworkManager.SendTo(player, data);
                }
            }
        }
        
        #endregion

        #region Command Deserialization
        
        /// <summary>
        /// Deserialize a command to its specific type based on the command type.
        /// </summary>
        private static GameCommand DeserializeCommand(GameCommandType type, byte[] payload)
        {
            switch (type)
            {
                case GameCommandType.SetGameSpeed:
                case GameCommandType.PauseGame:
                case GameCommandType.UnpauseGame:
                    return MessageSerializer.DeserializePayload<SpeedCommand>(payload);
                case GameCommandType.Dig:
                case GameCommandType.CancelDig:
                    return MessageSerializer.DeserializePayload<DigCommand>(payload);
                case GameCommandType.Build:
                case GameCommandType.CancelBuild:
                    return MessageSerializer.DeserializePayload<BuildCommand>(payload);
                case GameCommandType.Deconstruct:
                    return MessageSerializer.DeserializePayload<DeconstructCommand>(payload);
                case GameCommandType.SetPriority:
                    return MessageSerializer.DeserializePayload<PriorityCommand>(payload);
                case GameCommandType.Mop:
                    return MessageSerializer.DeserializePayload<MopCommand>(payload);
                case GameCommandType.Clear:
                    return MessageSerializer.DeserializePayload<ClearCommand>(payload);
                case GameCommandType.Harvest:
                case GameCommandType.CancelHarvest:
                    return MessageSerializer.DeserializePayload<HarvestCommand>(payload);
                case GameCommandType.Disinfect:
                    return MessageSerializer.DeserializePayload<DisinfectCommand>(payload);
                case GameCommandType.Capture:
                case GameCommandType.CancelCapture:
                    return MessageSerializer.DeserializePayload<CaptureCommand>(payload);
                case GameCommandType.SetBulkPriority:
                    return MessageSerializer.DeserializePayload<BulkPriorityCommand>(payload);
                case GameCommandType.SetDoorState:
                    return MessageSerializer.DeserializePayload<DoorStateCommand>(payload);
                case GameCommandType.SetBuildingPriority:
                    return MessageSerializer.DeserializePayload<BuildingPriorityCommand>(payload);
                case GameCommandType.ToggleBuildingDisinfect:
                    return MessageSerializer.DeserializePayload<BuildingDisinfectCommand>(payload);
                case GameCommandType.SetStorageFilter:
                    return MessageSerializer.DeserializePayload<StorageFilterCommand>(payload);
                case GameCommandType.UtilityBuild:
                    return MessageSerializer.DeserializePayload<UtilityBuildCommand>(payload);
                case GameCommandType.DisconnectUtility:
                    return MessageSerializer.DeserializePayload<DisconnectCommand>(payload);
                case GameCommandType.SetStorageCapacity:
                    return MessageSerializer.DeserializePayload<StorageCapacityCommand>(payload);
                case GameCommandType.SetAssignable:
                    return MessageSerializer.DeserializePayload<AssignableCommand>(payload);
                case GameCommandType.SetBuildingEnabled:
                    return MessageSerializer.DeserializePayload<BuildingEnabledCommand>(payload);
                case GameCommandType.ChoreStart:
                    return MessageSerializer.DeserializePayload<ChoreStartCommand>(payload);
                case GameCommandType.ChoreEnd:
                    return MessageSerializer.DeserializePayload<ChoreEndCommand>(payload);
                case GameCommandType.NavigateTo:
                    return MessageSerializer.DeserializePayload<NavigateToCommand>(payload);
                case GameCommandType.DuplicantChecksum:
                    return MessageSerializer.DeserializePayload<DuplicantChecksumCommand>(payload);
                case GameCommandType.DuplicantFullState:
                    return MessageSerializer.DeserializePayload<DuplicantFullStateCommand>(payload);
                case GameCommandType.PositionSync:
                    return MessageSerializer.DeserializePayload<PositionSyncCommand>(payload);
                case GameCommandType.RandomSeedSync:
                    return MessageSerializer.DeserializePayload<RandomSeedSyncCommand>(payload);
                case GameCommandType.DuplicantCommandRequest:
                    return MessageSerializer.DeserializePayload<DuplicantCommandRequestCommand>(payload);
                case GameCommandType.ElementChange:
                    return MessageSerializer.DeserializePayload<ElementChangeCommand>(payload);
                case GameCommandType.SetFilterable:
                    return MessageSerializer.DeserializePayload<FilterableCommand>(payload);
                case GameCommandType.SetThreshold:
                    return MessageSerializer.DeserializePayload<ThresholdCommand>(payload);
                case GameCommandType.SetLogicSwitch:
                    return MessageSerializer.DeserializePayload<LogicSwitchCommand>(payload);
                case GameCommandType.HardSync:
                    return MessageSerializer.DeserializePayload<HardSyncCommand>(payload);
                case GameCommandType.ResearchSync:
                    return MessageSerializer.DeserializePayload<ResearchSyncCommand>(payload);
                case GameCommandType.SkillsSync:
                    return MessageSerializer.DeserializePayload<SkillsSyncCommand>(payload);
                case GameCommandType.ScheduleSync:
                    return MessageSerializer.DeserializePayload<ScheduleSyncCommand>(payload);
                default:
                    return MessageSerializer.DeserializePayload<GameCommand>(payload);
            }
        }
        
        #endregion

        #region Command Processing
        
        /// <summary>
        /// Process pending commands. Call this from Update.
        /// </summary>
        public static void ProcessPendingCommands()
        {
            while (_pendingCommands.Count > 0)
            {
                var command = _pendingCommands.Dequeue();
                ExecuteCommand(command);
            }
        }

        /// <summary>
        /// Execute a command locally. Dispatches to specific handlers in partial classes.
        /// </summary>
        private static void ExecuteCommand(GameCommand command)
        {
            _isExecutingRemoteCommand = true;
            
            try
            {
                switch (command.Type)
                {
                    // Dig commands
                    case GameCommandType.Dig:
                        ExecuteDigCommand(command as DigCommand);
                        break;
                    case GameCommandType.CancelDig:
                        ExecuteCancelDigCommand(command as DigCommand);
                        break;
                        
                    // Build commands
                    case GameCommandType.Build:
                        ExecuteBuildCommand(command as BuildCommand);
                        break;
                    case GameCommandType.Deconstruct:
                        ExecuteDeconstructCommand(command as DeconstructCommand);
                        break;
                    case GameCommandType.UtilityBuild:
                        ExecuteUtilityBuildCommand(command as UtilityBuildCommand);
                        break;
                    case GameCommandType.DisconnectUtility:
                        ExecuteDisconnectCommand(command as DisconnectCommand);
                        break;
                        
                    // Speed commands
                    case GameCommandType.SetGameSpeed:
                        ExecuteSetGameSpeedInline(command as SpeedCommand);
                        break;
                    case GameCommandType.PauseGame:
                        ExecutePauseInline();
                        break;
                    case GameCommandType.UnpauseGame:
                        ExecuteUnpauseInline();
                        break;
                        
                    // Tool commands
                    case GameCommandType.Mop:
                        ExecuteMopCommand(command as MopCommand);
                        break;
                    case GameCommandType.Clear:
                        ExecuteClearCommand(command as ClearCommand);
                        break;
                    case GameCommandType.Harvest:
                        ExecuteHarvestCommand(command as HarvestCommand, true);
                        break;
                    case GameCommandType.CancelHarvest:
                        ExecuteHarvestCommand(command as HarvestCommand, false);
                        break;
                    case GameCommandType.Disinfect:
                        ExecuteDisinfectCommand(command as DisinfectCommand);
                        break;
                    case GameCommandType.Capture:
                        ExecuteCaptureCommand(command as CaptureCommand, true);
                        break;
                    case GameCommandType.CancelCapture:
                        ExecuteCaptureCommand(command as CaptureCommand, false);
                        break;
                        
                    // Priority commands
                    case GameCommandType.SetPriority:
                        ExecutePriorityCommand(command as PriorityCommand);
                        break;
                    case GameCommandType.SetBulkPriority:
                        ExecuteBulkPriorityCommand(command as BulkPriorityCommand);
                        break;
                    case GameCommandType.SetBuildingPriority:
                        ExecuteBuildingPriorityCommand(command as BuildingPriorityCommand);
                        break;
                        
                    // Building settings commands
                    case GameCommandType.SetDoorState:
                        ExecuteDoorStateCommand(command as DoorStateCommand);
                        break;
                    case GameCommandType.ToggleBuildingDisinfect:
                        ExecuteBuildingDisinfectCommand(command as BuildingDisinfectCommand);
                        break;
                    case GameCommandType.SetStorageFilter:
                        ExecuteStorageFilterCommand(command as StorageFilterCommand);
                        break;
                    case GameCommandType.SetStorageCapacity:
                        ExecuteStorageCapacityCommand(command as StorageCapacityCommand);
                        break;
                    case GameCommandType.SetAssignable:
                        ExecuteAssignableCommand(command as AssignableCommand);
                        break;
                    case GameCommandType.SetBuildingEnabled:
                        ExecuteBuildingEnabledCommand(command as BuildingEnabledCommand);
                        break;
                        
                    // Duplicant sync commands
                    case GameCommandType.ChoreStart:
                        ExecuteChoreStartCommand(command as ChoreStartCommand);
                        break;
                    case GameCommandType.ChoreEnd:
                        ExecuteChoreEndCommand(command as ChoreEndCommand);
                        break;
                    case GameCommandType.NavigateTo:
                        ExecuteNavigateToCommand(command as NavigateToCommand);
                        break;
                    case GameCommandType.DuplicantChecksum:
                        ExecuteDuplicantChecksumCommand(command as DuplicantChecksumCommand);
                        break;
                    case GameCommandType.DuplicantFullState:
                        ExecuteDuplicantFullStateCommand(command as DuplicantFullStateCommand);
                        break;
                    case GameCommandType.PositionSync:
                        ExecutePositionSyncCommand(command as PositionSyncCommand);
                        break;
                    case GameCommandType.RandomSeedSync:
                        ExecuteRandomSeedSyncCommand(command as RandomSeedSyncCommand);
                        break;
                    case GameCommandType.DuplicantCommandRequest:
                        ExecuteDuplicantCommandRequest(command as DuplicantCommandRequestCommand);
                        break;
                    
                    // Element sync commands
                    case GameCommandType.ElementChange:
                        Sync.ElementSyncManager.Instance.ApplyElementChanges(command as ElementChangeCommand);
                        break;
                    
                    // Building config sync commands
                    case GameCommandType.SetFilterable:
                        ExecuteFilterableCommand(command as FilterableCommand);
                        break;
                    case GameCommandType.SetThreshold:
                        ExecuteThresholdCommand(command as ThresholdCommand);
                        break;
                    case GameCommandType.SetLogicSwitch:
                        ExecuteLogicSwitchCommand(command as LogicSwitchCommand);
                        break;
                    
                    // Hard sync
                    case GameCommandType.HardSync:
                        Sync.HardSyncManager.Instance.ApplyHardSync(command as HardSyncCommand);
                        break;
                    
                    // Research sync
                    case GameCommandType.ResearchSync:
                        Handlers.CommandHandler.ExecuteResearchSyncCommand(command as ResearchSyncCommand);
                        break;
                    
                    // Skills sync
                    case GameCommandType.SkillsSync:
                        Handlers.CommandHandler.ExecuteSkillsSyncCommand(command as SkillsSyncCommand);
                        break;
                    
                    // Schedule sync
                    case GameCommandType.ScheduleSync:
                        Handlers.CommandHandler.ExecuteScheduleSyncCommand(command as ScheduleSyncCommand);
                        break;
                        
                    default:
                        Debug.LogWarning($"[Antigravity] Unknown command type: {command.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to execute command {command.Type}: {ex.Message}");
            }
            finally
            {
                _isExecutingRemoteCommand = false;
            }
        }

        #endregion

        // Command handler implementations are in the Handlers/ folder as partial class files
    }
}
