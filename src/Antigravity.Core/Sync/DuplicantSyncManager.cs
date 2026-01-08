using System.Collections.Generic;
using UnityEngine;
using Antigravity.Core.Commands;

namespace Antigravity.Core.Sync
{
    /// <summary>
    /// Manages synchronization of Duplicants (Minions).
    /// Tracks identities, maps IDs, and handles chore/navigation sync.
    /// </summary>
    public class DuplicantSyncManager
    {
        private static DuplicantSyncManager _instance;
        public static DuplicantSyncManager Instance => _instance ?? (_instance = new DuplicantSyncManager());

        // Use Duplicant NAME as key (consistent across Host/Client)
        private Dictionary<string, MinionIdentity> _trackedMinions = new Dictionary<string, MinionIdentity>();

        private DuplicantSyncManager() { }

        public void Initialize()
        {
            Debug.Log("[Antigravity] DuplicantSyncManager initialized.");
            _trackedMinions.Clear();
        }

        /// <summary>
        /// Get a unique, consistent identifier for a Duplicant.
        /// Uses the Duplicant's name which is the same on Host and Client.
        /// </summary>
        public static string GetDuplicantKey(MinionIdentity minion)
        {
            if (minion == null) return null;
            return minion.name; // Name is consistent across Host/Client
        }

        public void RegisterMinion(MinionIdentity minion)
        {
            if (minion == null) return;
            
            string key = GetDuplicantKey(minion);
            if (string.IsNullOrEmpty(key)) return;

            if (!_trackedMinions.ContainsKey(key))
            {
                _trackedMinions[key] = minion;
                Debug.Log($"[Antigravity] Tracked minion: {key}");
            }
        }

        public void UnregisterMinion(MinionIdentity minion)
        {
            if (minion == null) return;
            string key = GetDuplicantKey(minion);
            if (!string.IsNullOrEmpty(key) && _trackedMinions.ContainsKey(key))
            {
                _trackedMinions.Remove(key);
            }
        }

        public MinionIdentity GetMinion(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_trackedMinions.TryGetValue(key, out var minion))
            {
                return minion;
            }
            return null;
        }

        // Legacy method for compatibility - converts int to string lookup
        public MinionIdentity GetMinion(int legacyId)
        {
            Debug.LogWarning($"[Antigravity] GetMinion(int) called with {legacyId} - this is deprecated!");
            return null;
        }

        public void HandleChoreStart(ChoreStartCommand cmd)
        {
            MinionIdentity minion = GetMinion(cmd.DuplicantName);
            if (minion == null)
            {
                Debug.LogWarning($"[Antigravity] ChoreStart: Minion {cmd.DuplicantName} not found!");
                return;
            }

            // On client, we need to force the chore that the host assigned
            if (!Antigravity.Core.Network.MultiplayerState.IsHost)
            {
                ForceChoreOnClient(minion, cmd);
            }
            else
            {
                Debug.Log($"[Antigravity] ChoreStart for {minion.name}: {cmd.ChoreTypeId} (Host, no force needed)");
            }
        }

        /// <summary>
        /// Force a specific chore on a client Duplicant.
        /// Uses improved matching with TargetPrefabId, with navigation fallback.
        /// </summary>
        private void ForceChoreOnClient(MinionIdentity minion, ChoreStartCommand cmd)
        {
            Debug.Log($"[Antigravity] CLIENT: Forcing chore {cmd.ChoreTypeId} on {minion.name} at cell {cmd.TargetCell}");

            // Temporarily enable ChoreConsumer for chore assignment
            ChoreConsumer consumer = minion.GetComponent<ChoreConsumer>();
            bool wasEnabled = consumer != null && consumer.enabled;
            if (consumer != null && !wasEnabled)
            {
                consumer.enabled = true;
            }

            try
            {
                // Step 1: Try to find and assign a matching chore from preconditions
                if (TryAssignChoreFromPreconditions(minion, cmd))
                {
                    return; // Success!
                }

                // Step 2: Try to find target by cell + prefab and navigate there
                if (TryNavigateToChoreTarget(minion, cmd))
                {
                    Debug.Log($"[Antigravity] CLIENT: Chore not found, navigating to target instead");
                    return;
                }

                // Step 3: Fallback - just move to the cell
                if (cmd.TargetCell > 0 && Grid.IsValidCell(cmd.TargetCell))
                {
                    var navigator = minion.GetComponent<Navigator>();
                    if (navigator != null)
                    {
                        navigator.GoTo(cmd.TargetCell);
                        Debug.Log($"[Antigravity] CLIENT: Fallback - navigating {minion.name} to cell {cmd.TargetCell}");
                        return;
                    }
                }

                // Store for retry if all else fails
                Debug.LogWarning($"[Antigravity] CLIENT: Could not force chore {cmd.ChoreTypeId}, storing for retry");
                StorePendingChore(minion, cmd);
            }
            finally
            {
                // Re-disable ChoreConsumer if it was disabled before
                if (consumer != null && !wasEnabled)
                {
                    consumer.enabled = false;
                }
            }
        }

        /// <summary>
        /// Try to find and assign a matching chore.
        /// </summary>
        private bool TryAssignChoreFromPreconditions(MinionIdentity minion, ChoreStartCommand cmd)
        {
            ChoreDriver choreDriver = minion.GetComponent<ChoreDriver>();
            ChoreConsumer choreConsumer = minion.GetComponent<ChoreConsumer>();
            
            if (choreDriver == null || choreConsumer == null) return false;

            // First try the preconditions snapshot if it has data
            var snapshot = choreConsumer.GetLastSuccessfulPreconditionSnapshot();
            if (snapshot.succeededContexts != null && snapshot.succeededContexts.Count > 0)
            {
                foreach (var ctx in snapshot.succeededContexts)
                {
                    if (ctx.chore == null || ctx.chore.choreType == null) continue;
                    if (ctx.chore.choreType.Id != cmd.ChoreTypeId) continue;

                    // Check if target matches
                    if (cmd.TargetCell > 0 && ctx.chore.target != null)
                    {
                        int choreCell = Grid.PosToCell(ctx.chore.target.gameObject);
                        if (choreCell == cmd.TargetCell)
                        {
                            Debug.Log($"[Antigravity] CLIENT: Found matching chore from preconditions, assigning");
                            choreDriver.SetChore(ctx);
                            return true;
                        }
                    }
                    else if (cmd.TargetCell <= 0)
                    {
                        // No target cell - match by type only
                        Debug.Log($"[Antigravity] CLIENT: Found matching chore by type, assigning");
                        choreDriver.SetChore(ctx);
                        return true;
                    }
                }
            }
            
            // Try to find a new chore using ChoreConsumer
            try
            {
                // Force refresh to find available chores
                Chore.Precondition.Context outContext = default;
                if (choreConsumer.FindNextChore(ref outContext))
                {
                    if (outContext.chore != null && outContext.chore.choreType != null)
                    {
                        if (outContext.chore.choreType.Id == cmd.ChoreTypeId)
                        {
                            Debug.Log($"[Antigravity] CLIENT: Found matching chore via FindNextChore, assigning");
                            choreDriver.SetChore(outContext);
                            return true;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Antigravity] FindNextChore failed: {ex.Message}");
            }

            Debug.Log($"[Antigravity] CLIENT: No matching chore found for {cmd.ChoreTypeId}");
            return false;
        }

        /// <summary>
        /// Navigate to the chore target location.
        /// </summary>
        private bool TryNavigateToChoreTarget(MinionIdentity minion, ChoreStartCommand cmd)
        {
            // Need a valid target cell
            if (cmd.TargetCell <= 0 || !Grid.IsValidCell(cmd.TargetCell)) return false;

            var navigator = minion.GetComponent<Navigator>();
            if (navigator == null) return false;

            // Find object at target cell matching prefab (if specified)
            GameObject targetObject = null;
            if (!string.IsNullOrEmpty(cmd.TargetPrefabId))
            {
                targetObject = FindObjectAtCell(cmd.TargetCell, cmd.TargetPrefabId);
            }

            if (targetObject != null)
            {
                // Navigate to the object's cell
                int cellToNavigate = Grid.PosToCell(targetObject.transform.position);
                navigator.GoTo(cellToNavigate);
                return true;
            }
            else
            {
                // Just navigate to the target cell
                navigator.GoTo(cmd.TargetCell);
                return true;
            }
        }

        /// <summary>
        /// Find a GameObject at a specific cell with a matching prefab ID.
        /// </summary>
        private GameObject FindObjectAtCell(int cell, string prefabId)
        {
            // Check for any KPrefabIDs at this cell
            var objectLayer = Grid.ObjectLayers[(int)ObjectLayer.Building];
            if (objectLayer.TryGetValue(cell, out var go) && go != null)
            {
                var kpid = go.GetComponent<KPrefabID>();
                if (kpid != null && kpid.PrefabTag.ToString() == prefabId)
                {
                    return go;
                }
            }

            // Also check pickupables layer
            objectLayer = Grid.ObjectLayers[(int)ObjectLayer.Pickupables];
            if (objectLayer.TryGetValue(cell, out go) && go != null)
            {
                var kpid = go.GetComponent<KPrefabID>();
                if (kpid != null && kpid.PrefabTag.ToString() == prefabId)
                {
                    return go;
                }
            }

            return null;
        }

        // Pending chores that couldn't be assigned immediately
        private Dictionary<int, ChoreStartCommand> _pendingChores = new Dictionary<int, ChoreStartCommand>();

        private void StorePendingChore(MinionIdentity minion, ChoreStartCommand cmd)
        {
            int id = minion.GetInstanceID();
            _pendingChores[id] = cmd;
        }

        /// <summary>
        /// Try to apply pending chores. Call this periodically.
        /// </summary>
        public void ProcessPendingChores()
        {
            if (_pendingChores.Count == 0) return;

            var toRemove = new System.Collections.Generic.List<int>();

            foreach (var kvp in _pendingChores)
            {
                MinionIdentity minion = GetMinion(kvp.Key);
                if (minion != null)
                {
                    ForceChoreOnClient(minion, kvp.Value);
                    // Remove after one attempt to avoid infinite loops
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var id in toRemove)
            {
                _pendingChores.Remove(id);
            }
        }

        public void HandleChoreEnd(ChoreEndCommand cmd)
        {
            MinionIdentity minion = GetMinion(cmd.DuplicantName);
            if (minion == null) return;

            Debug.Log($"[Antigravity] Syncing ChoreEnd for {minion.name}");
        }

        public void HandleNavigateTo(NavigateToCommand cmd)
        {
            MinionIdentity minion = GetMinion(cmd.DuplicantName);
            if (minion == null) return;

            // Host should not process navigation syncs from clients
            if (Antigravity.Core.Network.MultiplayerState.IsHost) return;

            Debug.Log($"[Antigravity] Syncing Navigation for {minion.name} -> {cmd.TargetCell}");
            
            // Logic to move the duplicant visually or logically
            var navigator = minion.GetComponent<Navigator>();
            if (navigator != null)
            {
                // Verify if we should override. 
                // Careful: overriding local logic might cause conflicts if the brain is also running.
            }
        }

        public void ApplyFullState(DuplicantFullStateCommand cmd)
        {
            MinionIdentity minion = GetMinion(cmd.DuplicantName);
            if (minion == null) return;

            // Correct position
            Vector3 newPos = new Vector3(cmd.PositionX, cmd.PositionY, cmd.PositionZ);
            minion.transform.SetPosition(newPos);
            
            Debug.Log($"[Antigravity] Applied Full State to {minion.name}");
        }

        #region Checksum Verification

        /// <summary>
        /// Calculate a simple checksum for a Duplicant's state.
        /// Considers position (cell) and current chore type.
        /// </summary>
        public long CalculateChecksum(MinionIdentity minion)
        {
            if (minion == null) return 0;

            int cell = Grid.PosToCell(minion.transform.position);
            string choreId = "";

            var choreDriver = minion.GetComponent<ChoreDriver>();
            if (choreDriver != null)
            {
                var currentChore = choreDriver.GetCurrentChore();
                if (currentChore != null && currentChore.choreType != null)
                {
                    choreId = currentChore.choreType.Id;
                }
            }

            // Simple hash combining cell and chore
            long hash = cell;
            hash = hash * 31 + choreId.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Verify a received checksum against local state.
        /// Returns true if matched, false if desync detected.
        /// </summary>
        public bool VerifyChecksum(DuplicantChecksumCommand cmd)
        {
            MinionIdentity minion = GetMinion(cmd.DuplicantName);
            if (minion == null)
            {
                Debug.LogWarning($"[Antigravity] Checksum verification failed: Minion {cmd.DuplicantName} not found.");
                return false;
            }

            long localChecksum = CalculateChecksum(minion);
            if (localChecksum != cmd.Checksum)
            {
                Debug.LogWarning($"[Antigravity] DESYNC detected for {minion.name}! Local={localChecksum}, Remote={cmd.Checksum}");
                // Request full state sync
                RequestFullStateSync(cmd.DuplicantName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// [HOST] Send checksums for all tracked Duplicants.
        /// Call this periodically (e.g., every 5 seconds).
        /// </summary>
        public void SendMinionChecksums()
        {
            if (!Antigravity.Core.Network.MultiplayerState.IsHost) return;
            if (!Antigravity.Core.Network.MultiplayerState.IsMultiplayerSession) return;

            foreach (var kvp in _trackedMinions)
            {
                MinionIdentity minion = kvp.Value;
                if (minion == null) continue;

                var choreDriver = minion.GetComponent<ChoreDriver>();
                string choreId = "";
                if (choreDriver?.GetCurrentChore() != null)
                {
                    choreId = choreDriver.GetCurrentChore().choreType.Id;
                }

                var cmd = new DuplicantChecksumCommand
                {
                    DuplicantName = kvp.Key,
                    Checksum = CalculateChecksum(minion),
                    CurrentCell = Grid.PosToCell(minion.transform.position),
                    CurrentChore = choreId
                };

                Commands.CommandManager.SendCommand(cmd);
            }
        }

        /// <summary>
        /// Request a full state sync for a specific Duplicant.
        /// </summary>
        public void RequestFullStateSync(string duplicantName)
        {
            Debug.Log($"[Antigravity] Requesting full state sync for Duplicant {duplicantName}");
            // TODO: Send request to host, host will respond with DuplicantFullStateCommand
        }

        /// <summary>
        /// [HOST] Send full state for a Duplicant when a desync is detected.
        /// </summary>
        public void SendFullState(string duplicantName)
        {
            MinionIdentity minion = GetMinion(duplicantName);
            if (minion == null) return;

            Vector3 pos = minion.transform.position;
            var choreDriver = minion.GetComponent<ChoreDriver>();
            string choreId = "";
            int targetCell = -1;

            if (choreDriver?.GetCurrentChore() != null)
            {
                choreId = choreDriver.GetCurrentChore().choreType.Id;
                if (choreDriver.GetCurrentChore().target != null)
                {
                    targetCell = Grid.PosToCell(choreDriver.GetCurrentChore().target.gameObject);
                }
            }

            var cmd = new DuplicantFullStateCommand
            {
                DuplicantName = duplicantName,
                PositionX = pos.x,
                PositionY = pos.y,
                PositionZ = pos.z,
                CurrentCell = Grid.PosToCell(pos),
                CurrentChore = choreId,
                TargetCell = targetCell
            };

            Commands.CommandManager.SendCommand(cmd);
        }

        #endregion

        #region Position Sync

        /// <summary>
        /// [HOST] Send position updates for all tracked Duplicants.
        /// Call every 2 seconds for smooth sync.
        /// </summary>
        public void SendPositionSync()
        {
            if (!Antigravity.Core.Network.MultiplayerState.IsHost) return;
            if (!Antigravity.Core.Network.MultiplayerState.IsMultiplayerSession) return;

            foreach (var kvp in _trackedMinions)
            {
                MinionIdentity minion = kvp.Value;
                if (minion == null) continue;

                Vector3 pos = minion.transform.position;
                Navigator navigator = minion.GetComponent<Navigator>();
                
                // Movement data
                int targetCell = -1;
                bool isMoving = false;
                if (navigator != null)
                {
                    isMoving = navigator.IsMoving();
                    if (navigator.target != null)
                    {
                        targetCell = Grid.PosToCell(navigator.target);
                    }
                }
                
                // Chore data - extract from ChoreDriver
                string choreTypeId = null;
                string choreGroupId = null;
                int choreTargetCell = -1;
                string choreTargetPrefabId = null;
                
                ChoreDriver choreDriver = minion.GetComponent<ChoreDriver>();
                if (choreDriver != null && choreDriver.GetCurrentChore() != null)
                {
                    Chore currentChore = choreDriver.GetCurrentChore();
                    if (currentChore.choreType != null)
                    {
                        choreTypeId = currentChore.choreType.Id;
                        
                        // Get chore group
                        if (currentChore.choreType.groups != null && currentChore.choreType.groups.Length > 0)
                        {
                            choreGroupId = currentChore.choreType.groups[0]?.Id;
                        }
                    }
                    
                    // Get chore target
                    if (currentChore.target != null && currentChore.target.gameObject != null)
                    {
                        choreTargetCell = Grid.PosToCell(currentChore.target.gameObject);
                        
                        var kpid = currentChore.target.gameObject.GetComponent<KPrefabID>();
                        if (kpid != null)
                        {
                            choreTargetPrefabId = kpid.PrefabTag.ToString();
                        }
                    }
                }

                var cmd = new Commands.PositionSyncCommand
                {
                    DuplicantName = kvp.Key,
                    PositionX = pos.x,
                    PositionY = pos.y,
                    PositionZ = pos.z,
                    CurrentCell = Grid.PosToCell(pos),
                    IsMoving = isMoving,
                    TargetCell = targetCell,
                    CurrentChoreTypeId = choreTypeId,
                    CurrentChoreGroupId = choreGroupId,
                    ChoreTargetCell = choreTargetCell,
                    ChoreTargetPrefabId = choreTargetPrefabId
                };

                Commands.CommandManager.SendCommand(cmd);
            }
        }

        /// <summary>
        /// Apply a position sync from Host.
        /// Makes client Duplicants follow the Host's movements.
        /// </summary>
        public void ApplyPositionSync(Commands.PositionSyncCommand cmd)
        {
            // Only clients should apply position sync
            if (Antigravity.Core.Network.MultiplayerState.IsHost) return;

            MinionIdentity minion = GetMinion(cmd.DuplicantName);
            if (minion == null) return;

            Vector3 hostPos = new Vector3(cmd.PositionX, cmd.PositionY, cmd.PositionZ);
            Vector3 localPos = minion.transform.position;
            float distance = Vector3.Distance(hostPos, localPos);
            
            Navigator navigator = minion.GetComponent<Navigator>();
            
            // If host is moving and has a target, make client navigate there too
            if (cmd.IsMoving && cmd.TargetCell >= 0 && navigator != null)
            {
                int currentCell = Grid.PosToCell(localPos);
                
                // Only start new navigation if we're far from target or not already moving
                if (!navigator.IsMoving() || currentCell != cmd.CurrentCell)
                {
                    // Navigate to the same target as the host
                    navigator.GoTo(cmd.TargetCell);
                }
            }
            else if (!cmd.IsMoving && navigator != null && navigator.IsMoving())
            {
                // Host stopped, we should stop too
                navigator.Stop();
            }
            
            // Correct position if drift is too large (teleport as fallback)
            if (distance > 3f)
            {
                Debug.Log($"[Antigravity] Position teleport for {minion.name}: drift={distance:F2}");
                minion.transform.SetPosition(hostPos);
                
                // Also stop any navigation since we teleported
                if (navigator != null)
                {
                    navigator.Stop();
                }
            }
            else if (distance > 1f && !cmd.IsMoving)
            {
                // Small drift while stationary - gently correct
                minion.transform.SetPosition(hostPos);
            }
        }

        #endregion

        #region Item Sync

        /// <summary>
        /// [HOST] Send all pickupable items to clients for debugging/sync.
        /// Call periodically (e.g., every 10 seconds or on demand).
        /// </summary>
        public void SendItemSync()
        {
            if (!Antigravity.Core.Network.MultiplayerState.IsHost) return;
            if (!Antigravity.Core.Network.MultiplayerState.IsMultiplayerSession) return;

            try
            {
                var itemList = new System.Collections.Generic.List<Commands.ItemSyncData>();
                
                // Get world dimensions
                int worldWidth = Grid.WidthInCells;
                int worldHeight = Grid.HeightInCells;
                
                // Collect all pickupables
                var pickupables = Components.Pickupables?.Items;
                if (pickupables != null)
                {
                    foreach (var pickupable in pickupables)
                    {
                        if (pickupable == null || !pickupable.gameObject.activeInHierarchy) continue;
                        
                        var pos = pickupable.transform.position;
                        int cell = Grid.PosToCell(pos);
                        
                        var element = pickupable.GetComponent<PrimaryElement>();
                        int elementId = element != null ? (int)element.ElementID : 0;
                        float mass = element?.Mass ?? 0;
                        
                        var kpid = pickupable.GetComponent<KPrefabID>();
                        string prefabId = kpid?.PrefabTag.ToString() ?? "Unknown";
                        
                        itemList.Add(new Commands.ItemSyncData
                        {
                            Cell = cell,
                            PrefabId = prefabId,
                            Mass = mass,
                            ElementId = elementId
                        });
                    }
                }
                
                var cmd = new Commands.ItemSyncCommand
                {
                    WorldWidth = worldWidth,
                    WorldHeight = worldHeight,
                    Items = itemList
                };
                
                Commands.CommandManager.SendCommand(cmd);
                Debug.Log($"[Antigravity] ItemSync sent: {itemList.Count} items in {worldWidth}x{worldHeight} world");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Antigravity] Error in SendItemSync: {ex.Message}");
            }
        }

        #endregion

        #region Random Seed Sync

        private static int _syncedSeed = 0;

        /// <summary>
        /// [HOST] Send the current random seed to clients.
        /// Call at game start and periodically.
        /// </summary>
        public void SendRandomSeed()
        {
            if (!Antigravity.Core.Network.MultiplayerState.IsHost) return;

            int currentSeed = UnityEngine.Random.state.GetHashCode();
            
            var cmd = new Commands.RandomSeedSyncCommand
            {
                Seed = currentSeed,
                GameTick = GameClock.Instance != null ? (long)GameClock.Instance.GetTime() : 0
            };

            Commands.CommandManager.SendCommand(cmd);
        }

        /// <summary>
        /// Apply random seed from Host.
        /// </summary>
        public void ApplyRandomSeed(Commands.RandomSeedSyncCommand cmd)
        {
            if (Antigravity.Core.Network.MultiplayerState.IsHost) return;

            Debug.Log($"[Antigravity] Applying random seed: {cmd.Seed}");
            _syncedSeed = cmd.Seed;
            UnityEngine.Random.InitState(cmd.Seed);
        }

        #endregion

        #region Client Command Processing

        /// <summary>
        /// [HOST] Process a command request from a client.
        /// Validates and executes the request locally, results sync automatically via PositionSync.
        /// </summary>
        public void ProcessClientRequest(DuplicantCommandRequestCommand cmd)
        {
            if (!Antigravity.Core.Network.MultiplayerState.IsHost)
            {
                Debug.LogWarning("[Antigravity] ProcessClientRequest called on client - ignoring");
                return;
            }

            var minion = GetMinion(cmd.DuplicantName);
            if (minion == null)
            {
                Debug.LogWarning($"[Antigravity] Client request: Duplicant '{cmd.DuplicantName}' not found");
                return;
            }

            switch (cmd.RequestType)
            {
                case DuplicantRequestType.MoveTo:
                    HandleMoveToRequest(minion, cmd.TargetCell);
                    break;
                    
                case DuplicantRequestType.CancelChore:
                    HandleCancelChoreRequest(minion);
                    break;
                    
                case DuplicantRequestType.AssignChore:
                    // TODO: Implement chore assignment by type/target
                    Debug.Log($"[Antigravity] AssignChore request not yet implemented");
                    break;
                    
                default:
                    Debug.LogWarning($"[Antigravity] Unknown request type: {cmd.RequestType}");
                    break;
            }
        }

        /// <summary>
        /// [HOST] Force a Duplicant to move to a specific cell.
        /// </summary>
        private void HandleMoveToRequest(MinionIdentity minion, int targetCell)
        {
            if (targetCell < 0 || !Grid.IsValidCell(targetCell))
            {
                Debug.LogWarning($"[Antigravity] Invalid target cell: {targetCell}");
                return;
            }

            var navigator = minion.GetComponent<Navigator>();
            if (navigator == null)
            {
                Debug.LogError($"[Antigravity] No Navigator on {minion.name}");
                return;
            }

            // Cancel current activity safely
            var choreDriver = minion.GetComponent<ChoreDriver>();
            if (choreDriver != null)
            {
                var currentChore = choreDriver.GetCurrentChore();
                if (currentChore != null)
                {
                    try
                    {
                        currentChore.Cancel("Player move command");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[Antigravity] Failed to cancel chore on move: {ex.Message}");
                    }
                }
            }

            // Move to the target cell
            Vector3 targetPos = Grid.CellToPos(targetCell, CellAlignment.Bottom, Grid.SceneLayer.Move);
            navigator.GoTo(targetCell);
            
            Debug.Log($"[Antigravity] MoveTo request executed: {minion.name} -> cell {targetCell}");
        }

        /// <summary>
        /// [HOST] Cancel the current chore of a Duplicant.
        /// </summary>
        private void HandleCancelChoreRequest(MinionIdentity minion)
        {
            var choreDriver = minion.GetComponent<ChoreDriver>();
            if (choreDriver != null)
            {
                var currentChore = choreDriver.GetCurrentChore();
                if (currentChore != null)
                {
                    try
                    {
                        currentChore.Cancel("Player cancel command");
                        Debug.Log($"[Antigravity] Chore cancelled for {minion.name}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[Antigravity] Failed to cancel chore: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// [CLIENT/HOST] API to request a Duplicant move to a position.
        /// On Host, executes directly. On Client, sends to Host.
        /// </summary>
        public static void RequestMoveTo(string duplicantName, int targetCell)
        {
            var cmd = new DuplicantCommandRequestCommand
            {
                DuplicantName = duplicantName,
                RequestType = DuplicantRequestType.MoveTo,
                TargetCell = targetCell
            };

            if (Antigravity.Core.Network.MultiplayerState.IsHost)
            {
                // Host executes directly
                Instance.ProcessClientRequest(cmd);
            }
            else
            {
                // Client sends to Host
                Commands.CommandManager.SendCommand(cmd);
            }
        }

        /// <summary>
        /// [CLIENT/HOST] API to request cancellation of a Duplicant's current chore.
        /// </summary>
        public static void RequestCancelChore(string duplicantName)
        {
            var cmd = new DuplicantCommandRequestCommand
            {
                DuplicantName = duplicantName,
                RequestType = DuplicantRequestType.CancelChore
            };

            if (Antigravity.Core.Network.MultiplayerState.IsHost)
            {
                Instance.ProcessClientRequest(cmd);
            }
            else
            {
                Commands.CommandManager.SendCommand(cmd);
            }
        }

        #endregion
    }
}
