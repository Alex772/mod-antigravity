using System;
using System.Collections.Generic;
using UnityEngine;
using Antigravity.Core.Network;
using Newtonsoft.Json;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Manages sending and receiving game commands for multiplayer sync.
    /// </summary>
    public static class CommandManager
    {
        // Queue of commands waiting to be executed
        private static Queue<GameCommand> _pendingCommands = new Queue<GameCommand>();
        
        // Flag to prevent re-sending commands we just received
        private static bool _isExecutingRemoteCommand = false;
        
        // Track current speed for pause/unpause (default 1)
        private static int _currentSpeed = 1;
        
        // Events
        public static event Action<GameCommand> OnCommandReceived;
        public static event Action<GameCommand> OnCommandSent;

        /// <summary>
        /// Initialize the command manager.
        /// </summary>
        public static void Initialize()
        {
            SteamNetworkManager.OnDataReceived += OnNetworkDataReceived;
            Debug.Log("[Antigravity] CommandManager initialized.");
        }

        /// <summary>
        /// Check if we're currently executing a remote command (to prevent echo).
        /// </summary>
        public static bool IsExecutingRemoteCommand => _isExecutingRemoteCommand;

        /// <summary>
        /// Send a command to all other players.
        /// </summary>
        public static void SendCommand(GameCommand command)
        {
            // Don't send if we're executing a remote command (prevent echo)
            if (_isExecutingRemoteCommand) return;
            
            // Don't send if not connected
            if (!SteamNetworkManager.IsConnected) return;

            try
            {
                // Set sender info
                command.SenderSteamId = SteamNetworkManager.LocalSteamId.m_SteamID;
                command.GameTick = GetCurrentTick();

                // Create network message
                var message = MessageSerializer.CreateMessage(
                    MessageType.Command,
                    command,
                    command.SenderSteamId,
                    command.GameTick
                );

                byte[] data = MessageSerializer.Serialize(message);

                // Send to all other players
                if (SteamNetworkManager.IsHost)
                {
                    // Host sends to all clients
                    SteamNetworkManager.SendToAll(data);
                }
                else
                {
                    // Client sends to host only (host will relay)
                    SteamNetworkManager.SendTo(SteamNetworkManager.HostSteamId, data);
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
        /// Called when network data is received.
        /// </summary>
        private static void OnNetworkDataReceived(Steamworks.CSteamID sender, byte[] data)
        {
            var message = MessageSerializer.Deserialize(data);
            if (message == null) return;

            if (message.Type == MessageType.Command)
            {
                try
                {
                    // First get the base command to check the type
                    var baseCommand = MessageSerializer.DeserializePayload<GameCommand>(message.Payload);
                    if (baseCommand == null) return;

                    // Deserialize the correct specific type based on command type
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
                default:
                    return MessageSerializer.DeserializePayload<GameCommand>(payload);
            }
        }

        /// <summary>
        /// Handle a command received from another player.
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
        /// Relay a command from one client to all other clients.
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
        /// Execute a command locally.
        /// </summary>
        private static void ExecuteCommand(GameCommand command)
        {
            _isExecutingRemoteCommand = true;
            
            try
            {
                switch (command.Type)
                {
                    case GameCommandType.Dig:
                        ExecuteDigCommand(command as DigCommand);
                        break;
                    case GameCommandType.CancelDig:
                        ExecuteCancelDigCommand(command as DigCommand);
                        break;
                    case GameCommandType.Build:
                        ExecuteBuildCommand(command as BuildCommand);
                        break;
                    case GameCommandType.Deconstruct:
                        ExecuteDeconstructCommand(command as DeconstructCommand);
                        break;
                    case GameCommandType.SetPriority:
                        ExecutePriorityCommand(command as PriorityCommand);
                        break;
                    case GameCommandType.SetGameSpeed:
                        var speedCmd = command as SpeedCommand;
                        if (speedCmd != null)
                        {
                            // ONI speed values: 0=slow(1x), 1=medium(2x), 2=fast(3x)
                            // Our command uses 1=slow, 2=medium, 3=fast
                            // So we convert: our speed - 1 = ONI speed
                            int oniSpeed = speedCmd.Speed - 1;
                            if (oniSpeed >= 0 && oniSpeed <= 2)
                            {
                                _currentSpeed = oniSpeed;
                                Debug.Log($"[Antigravity] EXECUTE: Speed {speedCmd.Speed} -> ONI speed index {oniSpeed}");
                                
                                if (SpeedControlScreen.Instance != null)
                                {
                                    SpeedControlScreen.Instance.SetSpeed(oniSpeed);
                                }
                            }
                        }
                        break;
                    case GameCommandType.PauseGame:
                        Debug.Log($"[Antigravity] EXECUTE: Pause");
                        if (SpeedControlScreen.Instance != null)
                        {
                            // Save current speed before pausing
                            _currentSpeed = SpeedControlScreen.Instance.GetSpeed();
                            SpeedControlScreen.Instance.Pause(false); // false = no sound
                        }
                        break;
                    case GameCommandType.UnpauseGame:
                        Debug.Log($"[Antigravity] EXECUTE: Unpause");
                        if (SpeedControlScreen.Instance != null)
                        {
                            SpeedControlScreen.Instance.Unpause(false); // false = no sound
                        }
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

        #region Command Execution

        private static void ExecuteDigCommand(DigCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0)
            {
                Debug.LogWarning("[Antigravity] DigCommand has no cells");
                return;
            }

            Debug.Log($"[Antigravity] EXECUTE: Dig {cmd.Cells.Length} cells");

            int successCount = 0;
            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (Grid.IsValidCell(cell))
                    {
                        // Use DigTool.PlaceDig to create the dig marker (same as game does)
                        var result = DigTool.PlaceDig(cell, successCount);
                        if (result != null)
                        {
                            successCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to place dig at cell {cell}: {ex.Message}");
                }
            }

            Debug.Log($"[Antigravity] Placed {successCount}/{cmd.Cells.Length} dig markers");
        }

        private static void ExecuteCancelDigCommand(DigCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: CancelDig {cmd.Cells.Length} cells");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    // Cancel all cancellable objects in the cell (same as CancelTool)
                    for (int layer = 0; layer < 45; layer++)
                    {
                        GameObject obj = Grid.Objects[cell, layer];
                        if (obj != null)
                        {
                            // Trigger cancel event (2127324410)
                            obj.Trigger(2127324410);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity] Cancel failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecuteBuildCommand(BuildCommand cmd)
        {
            if (cmd == null || string.IsNullOrEmpty(cmd.BuildingDefId)) return;

            Debug.Log($"[Antigravity] EXECUTE: Build {cmd.BuildingDefId} at cell {cmd.Cell}");

            try
            {
                var def = Assets.GetBuildingDef(cmd.BuildingDefId);
                if (def == null)
                {
                    Debug.LogWarning($"[Antigravity] BuildingDef not found: {cmd.BuildingDefId}");
                    return;
                }

                // Convert string elements back to Tags
                System.Collections.Generic.List<Tag> elements = new System.Collections.Generic.List<Tag>();
                if (cmd.SelectedElements != null)
                {
                    foreach (string elem in cmd.SelectedElements)
                    {
                        elements.Add(new Tag(elem));
                    }
                }

                // Get position from cell
                Vector3 pos = Grid.CellToPosCBC(cmd.Cell, Grid.SceneLayer.Building);
                Orientation orientation = (Orientation)cmd.Orientation;

                // Try to place the building blueprint
                GameObject result = def.TryPlace(null, pos, orientation, elements, cmd.FacadeId);
                
                if (result != null)
                {
                    Debug.Log($"[Antigravity] Build placed successfully: {cmd.BuildingDefId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Build failed: {ex.Message}");
            }
        }

        private static void ExecuteDeconstructCommand(DeconstructCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: Deconstruct {cmd.Cells.Length} cells");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    // Mark all deconstructable objects in the cell (same as DeconstructTool)
                    for (int layer = 0; layer < 45; layer++)
                    {
                        GameObject obj = Grid.Objects[cell, layer];
                        if (obj != null)
                        {
                            // Trigger deconstruct event (-790448070)
                            obj.Trigger(-790448070);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity] Deconstruct failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecutePriorityCommand(PriorityCommand cmd)
        {
            if (cmd == null) return;
            Debug.Log($"[Antigravity] Executed priority {cmd.Priority} at cell {cmd.Cell}");
        }

        private static void ExecuteSpeedCommand(SpeedCommand cmd)
        {
            if (cmd == null || SpeedControlScreen.Instance == null) return;

            switch (cmd.Speed)
            {
                case 0:
                    SpeedControlScreen.Instance.Pause(false);
                    break;
                case 1:
                case 2:
                case 3:
                    // First unpause if paused, then set speed
                    SpeedControlScreen.Instance.Unpause(false);
                    SpeedControlScreen.Instance.SetSpeed(cmd.Speed);
                    break;
            }
            Debug.Log($"[Antigravity] Executed speed change to {cmd.Speed}");
        }

        #endregion

        private static long GetCurrentTick()
        {
            try
            {
                if (GameClock.Instance != null)
                {
                    return (long)GameClock.Instance.GetTime();
                }
            }
            catch { }
            return 0;
        }
    }
}
