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
                    case GameCommandType.SetBulkPriority:
                        ExecuteBulkPriorityCommand(command as BulkPriorityCommand);
                        break;
                    case GameCommandType.SetDoorState:
                        ExecuteDoorStateCommand(command as DoorStateCommand);
                        break;
                    case GameCommandType.SetBuildingPriority:
                        ExecuteBuildingPriorityCommand(command as BuildingPriorityCommand);
                        break;
                    case GameCommandType.ToggleBuildingDisinfect:
                        ExecuteBuildingDisinfectCommand(command as BuildingDisinfectCommand);
                        break;
                    case GameCommandType.SetStorageFilter:
                        ExecuteStorageFilterCommand(command as StorageFilterCommand);
                        break;
                    case GameCommandType.UtilityBuild:
                        ExecuteUtilityBuildCommand(command as UtilityBuildCommand);
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

            Debug.Log($"[Antigravity] EXECUTE: Deconstruct {cmd.Cells.Length} cells, filters={string.Join(",", cmd.ActiveFilterLayers ?? new string[] { "ALL" })}");

            // Check if "ALL" filter is active
            bool filterAll = cmd.ActiveFilterLayers == null || 
                             cmd.ActiveFilterLayers.Length == 0 ||
                             System.Array.Exists(cmd.ActiveFilterLayers, f => f.ToUpper() == "ALL");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;
                    
                    // Check each layer (same as DeconstructTool.DeconstructCell)
                    for (int layer = 0; layer < 45; layer++)
                    {
                        GameObject obj = Grid.Objects[cell, layer];
                        if (obj == null) continue;

                        // Get filter layer for this object
                        string objFilterLayer = GetFilterLayerFromGameObject(obj);

                        // Check if this object matches active filters
                        bool shouldDeconstruct = filterAll || IsLayerActive(objFilterLayer, cmd.ActiveFilterLayers);

                        if (shouldDeconstruct)
                        {
                            // Trigger deconstruct event (-790448070)
                            obj.Trigger(-790448070);
                            
                            // Set priority if applicable
                            Prioritizable prioritizable = obj.GetComponent<Prioritizable>();
                            if (prioritizable != null && ToolMenu.Instance?.PriorityScreen != null)
                            {
                                prioritizable.SetMasterPriority(ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority());
                            }
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

        private static void ExecuteMopCommand(MopCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: Mop {cmd.Cells.Length} cells");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;

                    // Check if instant build mode (same as MopTool)
                    if (DebugHandler.InstantBuildMode)
                    {
                        Moppable.MopCell(cell, 1000000f, null);
                        continue;
                    }

                    // Create mop placer (same as MopTool.OnDragTool)
                    GameObject placer = Assets.GetPrefab(new Tag("MopPlacer"));
                    if (placer != null)
                    {
                        GameObject obj = Util.KInstantiate(placer);
                        Grid.Objects[cell, 8] = obj;
                        Vector3 position = Grid.CellToPosCBC(cell, Grid.SceneLayer.Move);
                        position.z += -0.15f;
                        obj.transform.SetPosition(position);
                        obj.SetActive(true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity] Mop failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecuteClearCommand(ClearCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: Clear {cmd.Cells.Length} cells");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;

                    // Get pickupables in cell (same as ClearTool.OnDragTool)
                    GameObject gameObject = Grid.Objects[cell, 3];
                    if (gameObject == null) continue;

                    var pickupable = gameObject.GetComponent<Pickupable>();
                    if (pickupable == null) continue;

                    ObjectLayerListItem objectLayerListItem = pickupable.objectLayerListItem;
                    while (objectLayerListItem != null)
                    {
                        GameObject obj = objectLayerListItem.gameObject;
                        Pickupable pickup = objectLayerListItem.pickupable;
                        objectLayerListItem = objectLayerListItem.nextItem;

                        if (obj != null && !pickup.KPrefabID.HasTag(GameTags.BaseMinion) && pickup.Clearable != null && pickup.Clearable.isClearable)
                        {
                            pickup.Clearable.MarkForClear();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity] Clear failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecuteHarvestCommand(HarvestCommand cmd, bool harvestWhenReady)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: Harvest {cmd.Cells.Length} cells (ready={harvestWhenReady})");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;

                    // Find HarvestDesignatables in cell (same as HarvestTool.OnDragTool)
                    foreach (HarvestDesignatable item in Components.HarvestDesignatables.Items)
                    {
                        OccupyArea area = item.area;
                        if (Grid.PosToCell(item) != cell && (area == null || !area.CheckIsOccupying(cell)))
                            continue;

                        if (harvestWhenReady)
                        {
                            item.SetHarvestWhenReady(true);
                        }
                        else
                        {
                            Harvestable component = item.GetComponent<Harvestable>();
                            if (component != null)
                            {
                                component.Trigger(2127324410); // Cancel harvest
                            }
                            item.SetHarvestWhenReady(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity] Harvest failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecuteDisinfectCommand(DisinfectCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: Disinfect {cmd.Cells.Length} cells");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;

                    // Same as DisinfectTool.OnDragTool
                    for (int layer = 0; layer < 45; layer++)
                    {
                        GameObject obj = Grid.Objects[cell, layer];
                        if (obj != null)
                        {
                            Disinfectable component = obj.GetComponent<Disinfectable>();
                            if (component != null && component.GetComponent<PrimaryElement>().DiseaseCount > 0)
                            {
                                component.MarkForDisinfect();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity] Disinfect failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecuteCaptureCommand(CaptureCommand cmd, bool mark)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: Capture area ({cmd.MinX},{cmd.MinY})-({cmd.MaxX},{cmd.MaxY}) mark={mark}");

            try
            {
                Vector2 min = new Vector2(cmd.MinX, cmd.MinY);
                Vector2 max = new Vector2(cmd.MaxX, cmd.MaxY);

                // Same as CaptureTool.MarkForCapture
                foreach (Capturable item in Components.Capturables.Items)
                {
                    Vector2 vector = Grid.PosToXY(item.transform.GetPosition());
                    if (vector.x >= min.x && vector.x < max.x && vector.y >= min.y && vector.y < max.y)
                    {
                        if (item.allowCapture)
                        {
                            PrioritySetting priority = new PrioritySetting(
                                (PriorityScreen.PriorityClass)cmd.PriorityClass,
                                cmd.PriorityValue
                            );
                            item.MarkForCapture(mark, priority, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Capture failed: {ex.Message}");
            }
        }

        private static void ExecuteBulkPriorityCommand(BulkPriorityCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: BulkPriority {cmd.Cells.Length} cells");

            PrioritySetting priority = new PrioritySetting(
                (PriorityScreen.PriorityClass)cmd.PriorityClass,
                cmd.PriorityValue
            );

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;

                    // Same as PrioritizeTool.OnDragTool
                    for (int layer = 0; layer < 45; layer++)
                    {
                        GameObject obj = Grid.Objects[cell, layer];
                        if (obj == null) continue;

                        Prioritizable component = obj.GetComponent<Prioritizable>();
                        if (component != null && component.showIcon && component.IsPrioritizable())
                        {
                            component.SetMasterPriority(priority);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity] Priority failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecuteDoorStateCommand(DoorStateCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: DoorState cell={cmd.Cell} state={cmd.ControlState}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;

                // Find door at cell
                GameObject obj = Grid.Objects[cmd.Cell, 1]; // Building layer
                if (obj == null) return;

                Door door = obj.GetComponent<Door>();
                if (door == null) return;

                // Set the door state (0=Auto, 1=Open, 2=Locked)
                Door.ControlState state = (Door.ControlState)cmd.ControlState;
                door.QueueStateChange(state);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] DoorState failed at cell {cmd.Cell}: {ex.Message}");
            }
        }

        private static void ExecuteBuildingPriorityCommand(BuildingPriorityCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: BuildingPriority cell={cmd.Cell} class={cmd.PriorityClass} value={cmd.PriorityValue}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;

                PrioritySetting priority = new PrioritySetting(
                    (PriorityScreen.PriorityClass)cmd.PriorityClass,
                    cmd.PriorityValue
                );

                // Find building at cell and set priority
                for (int layer = 0; layer < 45; layer++)
                {
                    GameObject obj = Grid.Objects[cmd.Cell, layer];
                    if (obj == null) continue;

                    Prioritizable prioritizable = obj.GetComponent<Prioritizable>();
                    if (prioritizable != null && prioritizable.IsPrioritizable())
                    {
                        prioritizable.SetMasterPriority(priority);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] BuildingPriority failed at cell {cmd.Cell}: {ex.Message}");
            }
        }

        private static void ExecuteBuildingDisinfectCommand(BuildingDisinfectCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: BuildingDisinfect cell={cmd.Cell} mark={cmd.MarkForDisinfect}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;

                // Find disinfectable at cell
                for (int layer = 0; layer < 45; layer++)
                {
                    GameObject obj = Grid.Objects[cmd.Cell, layer];
                    if (obj == null) continue;

                    Disinfectable disinfectable = obj.GetComponent<Disinfectable>();
                    if (disinfectable != null)
                    {
                        if (cmd.MarkForDisinfect)
                        {
                            disinfectable.MarkForDisinfect();
                        }
                        else
                        {
                            // CancelDisinfection is private, so we trigger the cancel event
                            obj.Trigger(2127324410);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] BuildingDisinfect failed at cell {cmd.Cell}: {ex.Message}");
            }
        }

        private static void ExecuteStorageFilterCommand(StorageFilterCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: StorageFilter cell={cmd.Cell} tags={cmd.AcceptedTags?.Length ?? 0}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;
                if (cmd.AcceptedTags == null) return;

                // Convert string tags back to Tag set
                HashSet<Tag> tags = new HashSet<Tag>();
                foreach (string tagStr in cmd.AcceptedTags)
                {
                    tags.Add(new Tag(tagStr));
                }

                // Find TreeFilterable at cell
                for (int layer = 0; layer < 45; layer++)
                {
                    GameObject obj = Grid.Objects[cmd.Cell, layer];
                    if (obj == null) continue;

                    TreeFilterable filterable = obj.GetComponent<TreeFilterable>();
                    if (filterable != null)
                    {
                        filterable.UpdateFilters(tags);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] StorageFilter failed at cell {cmd.Cell}: {ex.Message}");
            }
        }

        private static void ExecuteUtilityBuildCommand(UtilityBuildCommand cmd)
        {
            if (cmd?.PathCells == null || cmd.PathCells.Length == 0 || string.IsNullOrEmpty(cmd.BuildingDefId))
                return;

            Debug.Log($"[Antigravity] EXECUTE: UtilityBuild {cmd.BuildingDefId} with {cmd.PathCells.Length} cells");

            try
            {
                var def = Assets.GetBuildingDef(cmd.BuildingDefId);
                if (def == null)
                {
                    Debug.LogWarning($"[Antigravity] BuildingDef not found: {cmd.BuildingDefId}");
                    return;
                }

                // Convert string elements back to Tags
                var elements = new System.Collections.Generic.List<Tag>();
                if (cmd.SelectedElements != null)
                {
                    foreach (string elem in cmd.SelectedElements)
                    {
                        elements.Add(new Tag(elem));
                    }
                }

                // Get the utility network manager
                IUtilityNetworkMgr conduitMgr = null;
                try
                {
                    var networkComponent = def.BuildingComplete.GetComponent<IHaveUtilityNetworkMgr>();
                    if (networkComponent != null)
                    {
                        conduitMgr = networkComponent.GetNetworkManager();
                    }
                }
                catch { }

                // STEP 1: Add connections to conduit system BEFORE placing
                // This mirrors WireBuildTool.ApplyPathToConduitSystem()
                if (conduitMgr != null && cmd.PathCells.Length >= 2)
                {
                    for (int i = 1; i < cmd.PathCells.Length; i++)
                    {
                        int cell1 = cmd.PathCells[i - 1];
                        int cell2 = cmd.PathCells[i];
                        
                        if (!Grid.IsValidCell(cell1) || !Grid.IsValidCell(cell2)) continue;

                        // Calculate direction from cell1 to cell2
                        UtilityConnections direction = UtilityConnectionsExtensions.DirectionFromToCell(cell1, cell2);
                        if (direction != (UtilityConnections)0)
                        {
                            UtilityConnections inverseDir = direction.InverseDirection();
                            conduitMgr.AddConnection(direction, cell1, false);
                            conduitMgr.AddConnection(inverseDir, cell2, false);
                        }
                    }
                }

                // STEP 2: Place buildings at each cell
                int placedCount = 0;
                for (int i = 0; i < cmd.PathCells.Length; i++)
                {
                    int cell = cmd.PathCells[i];
                    if (!Grid.IsValidCell(cell)) continue;

                    Vector3 pos = Grid.CellToPosCBC(cell, Grid.SceneLayer.Building);
                    GameObject existingObj = Grid.Objects[cell, (int)def.TileLayer];
                    
                    if (existingObj == null)
                    {
                        GameObject placedObj = def.TryPlace(null, pos, Orientation.Neutral, elements, cmd.FacadeId);
                        
                        if (placedObj != null)
                        {
                            placedCount++;
                            if (conduitMgr != null)
                            {
                                UtilityConnections connections = conduitMgr.GetConnections(cell, false);
                                var utilityItem = placedObj.GetComponent<KAnimGraphTileVisualizer>();
                                if (utilityItem != null)
                                {
                                    utilityItem.Connections = connections;
                                }
                                TileVisualizer.RefreshCell(cell, def.TileLayer, def.ReplacementLayer);
                            }
                        }
                    }
                    else
                    {
                        // Update connections on existing piece
                        if (conduitMgr != null)
                        {
                            var utilityItem = existingObj.GetComponent<KAnimGraphTileVisualizer>();
                            if (utilityItem != null)
                            {
                                UtilityConnections connections = utilityItem.Connections;
                                connections |= conduitMgr.GetConnections(cell, false);
                                utilityItem.UpdateConnections(connections);
                            }
                            TileVisualizer.RefreshCell(cell, def.TileLayer, def.ReplacementLayer);
                        }
                    }
                }

                Debug.Log($"[Antigravity] UtilityBuild completed: {placedCount}/{cmd.PathCells.Length} cells placed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] UtilityBuild failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the filter layer string for a game object (same logic as FilteredDragTool).
        /// </summary>
        private static string GetFilterLayerFromGameObject(GameObject obj)
        {
            try
            {
                // Check for building components
                var buildingComplete = obj.GetComponent<BuildingComplete>();
                if (buildingComplete != null)
                {
                    return GetFilterLayerFromObjectLayer(buildingComplete.Def.ObjectLayer);
                }

                var buildingUnderConstruction = obj.GetComponent<BuildingUnderConstruction>();
                if (buildingUnderConstruction != null)
                {
                    return GetFilterLayerFromObjectLayer(buildingUnderConstruction.Def.ObjectLayer);
                }

                // Other object types
                if (obj.GetComponent<Clearable>() != null || obj.GetComponent<Moppable>() != null)
                {
                    return "CLEANANDCLEAR";
                }
                if (obj.GetComponent<Diggable>() != null)
                {
                    return "DIGPLACER";
                }
            }
            catch { }
            return "DEFAULT";
        }

        /// <summary>
        /// Map ObjectLayer to filter layer string (same logic as FilteredDragTool).
        /// </summary>
        private static string GetFilterLayerFromObjectLayer(ObjectLayer layer)
        {
            switch (layer)
            {
                case ObjectLayer.Building:
                case ObjectLayer.Gantry:
                    return "BUILDINGS";
                case ObjectLayer.Wire:
                case ObjectLayer.WireConnectors:
                    return "WIRES";
                case ObjectLayer.LiquidConduit:
                case ObjectLayer.LiquidConduitConnection:
                    return "LIQUIDCONDUIT";
                case ObjectLayer.GasConduit:
                case ObjectLayer.GasConduitConnection:
                    return "GASCONDUIT";
                case ObjectLayer.SolidConduit:
                case ObjectLayer.SolidConduitConnection:
                    return "SOLIDCONDUIT";
                case ObjectLayer.FoundationTile:
                    return "TILES";
                case ObjectLayer.LogicGate:
                case ObjectLayer.LogicWire:
                    return "LOGIC";
                case ObjectLayer.Backwall:
                    return "BACKWALL";
                default:
                    return "DEFAULT";
            }
        }

        /// <summary>
        /// Check if a layer is in the list of active filter layers.
        /// </summary>
        private static bool IsLayerActive(string objFilterLayer, string[] activeFilters)
        {
            if (activeFilters == null || activeFilters.Length == 0) return true;
            
            string upperLayer = objFilterLayer.ToUpper();
            foreach (string filter in activeFilters)
            {
                if (filter.ToUpper() == upperLayer || filter.ToUpper() == "ALL")
                {
                    return true;
                }
            }
            return false;
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
