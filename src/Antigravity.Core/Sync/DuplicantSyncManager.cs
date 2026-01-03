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

        private Dictionary<int, MinionIdentity> _trackedMinions = new Dictionary<int, MinionIdentity>();

        private DuplicantSyncManager() { }

        public void Initialize()
        {
            Debug.Log("[Antigravity] DuplicantSyncManager initialized.");
            _trackedMinions.Clear();
        }

        public void RegisterMinion(MinionIdentity minion)
        {
            if (minion == null) return;
            
            // We use instanceID as the unique identifier for now.
            // In a real P2P scenario ensuring these match across clients is tricky directly via instanceID,
            // but for now we assume loading the same save file might preserve some consistency, 
            // OR we will need a better mapping strategy (e.g. tracking by creation index).
            // For Phase 1/2 of sync, let's just track them.
            
            int id = minion.GetInstanceID();
            if (!_trackedMinions.ContainsKey(id))
            {
                _trackedMinions[id] = minion;
                Debug.Log($"[Antigravity] Tracked minion: {minion.name} (ID: {id})");
            }
        }

        public void UnregisterMinion(MinionIdentity minion)
        {
            if (minion == null) return;
            int id = minion.GetInstanceID();
            if (_trackedMinions.ContainsKey(id))
            {
                _trackedMinions.Remove(id);
            }
        }

        public MinionIdentity GetMinion(int id)
        {
            if (_trackedMinions.TryGetValue(id, out var minion))
            {
                return minion;
            }
            return null;
        }

        public void HandleChoreStart(ChoreStartCommand cmd)
        {
            MinionIdentity minion = GetMinion(cmd.DuplicantId);
            if (minion == null)
            {
                Debug.LogWarning($"[Antigravity] ChoreStart: Minion {cmd.DuplicantId} not found!");
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
        /// This overrides the local Brain's decision.
        /// </summary>
        private void ForceChoreOnClient(MinionIdentity minion, ChoreStartCommand cmd)
        {
            Debug.Log($"[Antigravity] CLIENT: Forcing chore {cmd.ChoreTypeId} on {minion.name}");

            // Get the ChoreDriver
            ChoreDriver choreDriver = minion.GetComponent<ChoreDriver>();
            if (choreDriver == null)
            {
                Debug.LogError($"[Antigravity] No ChoreDriver found on {minion.name}");
                return;
            }

            // Find the target chore by iterating through available chores
            // We need to find a chore that matches:
            // 1. The chore type ID
            // 2. The target cell (if specified)
            
            ChoreConsumer choreConsumer = minion.GetComponent<ChoreConsumer>();
            if (choreConsumer == null)
            {
                Debug.LogError($"[Antigravity] No ChoreConsumer found on {minion.name}");
                return;
            }

            // Get the ChoreType from the database
            ChoreType targetChoreType = null;
            foreach (var ct in Db.Get().ChoreTypes.resources)
            {
                if (ct.Id == cmd.ChoreTypeId)
                {
                    targetChoreType = ct;
                    break;
                }
            }

            if (targetChoreType == null)
            {
                Debug.LogWarning($"[Antigravity] ChoreType '{cmd.ChoreTypeId}' not found in database!");
                return;
            }

            // Try to find a matching chore from the precondition snapshot
            var snapshot = choreConsumer.GetLastSuccessfulPreconditionSnapshot();
            Chore.Precondition.Context? bestMatch = null;

            foreach (var ctx in snapshot.succeededContexts)
            {
                if (ctx.chore == null) continue;
                if (ctx.chore.choreType.Id != cmd.ChoreTypeId) continue;

                // Check if target cell matches (if specified)
                if (cmd.TargetCell > 0 && ctx.chore.target != null)
                {
                    int choreCell = Grid.PosToCell(ctx.chore.target.gameObject);
                    if (choreCell == cmd.TargetCell)
                    {
                        bestMatch = ctx;
                        break; // Exact match
                    }
                }
                else
                {
                    // No specific target, take first match
                    bestMatch = ctx;
                }
            }

            if (bestMatch.HasValue)
            {
                Debug.Log($"[Antigravity] CLIENT: Found matching chore, forcing assignment");
                choreDriver.SetChore(bestMatch.Value);
            }
            else
            {
                Debug.LogWarning($"[Antigravity] CLIENT: No matching chore found for {cmd.ChoreTypeId} at cell {cmd.TargetCell}. Will retry on next snapshot.");
                // Store pending chore to retry
                StorePendingChore(minion, cmd);
            }
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
            MinionIdentity minion = GetMinion(cmd.DuplicantId);
            if (minion == null) return;

            Debug.Log($"[Antigravity] Syncing ChoreEnd for {minion.name}");
        }

        public void HandleNavigateTo(NavigateToCommand cmd)
        {
            MinionIdentity minion = GetMinion(cmd.DuplicantId);
            if (minion == null) return;

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
            MinionIdentity minion = GetMinion(cmd.DuplicantId);
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
                if (currentChore != null)
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
            MinionIdentity minion = GetMinion(cmd.DuplicantId);
            if (minion == null)
            {
                Debug.LogWarning($"[Antigravity] Checksum verification failed: Minion {cmd.DuplicantId} not found.");
                return false;
            }

            long localChecksum = CalculateChecksum(minion);
            if (localChecksum != cmd.Checksum)
            {
                Debug.LogWarning($"[Antigravity] DESYNC detected for {minion.name}! Local={localChecksum}, Remote={cmd.Checksum}");
                // Request full state sync
                RequestFullStateSync(cmd.DuplicantId);
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
                    DuplicantId = kvp.Key,
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
        public void RequestFullStateSync(int duplicantId)
        {
            Debug.Log($"[Antigravity] Requesting full state sync for Duplicant {duplicantId}");
            // TODO: Send request to host, host will respond with DuplicantFullStateCommand
        }

        /// <summary>
        /// [HOST] Send full state for a Duplicant when a desync is detected.
        /// </summary>
        public void SendFullState(int duplicantId)
        {
            MinionIdentity minion = GetMinion(duplicantId);
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
                DuplicantId = duplicantId,
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
                
                int targetCell = -1;
                bool isMoving = false;
                if (navigator != null)
                {
                    isMoving = navigator.IsMoving();
                    // Get target cell if available
                    if (navigator.target != null)
                    {
                        targetCell = Grid.PosToCell(navigator.target);
                    }
                }

                var cmd = new Commands.PositionSyncCommand
                {
                    DuplicantId = kvp.Key,
                    PositionX = pos.x,
                    PositionY = pos.y,
                    PositionZ = pos.z,
                    CurrentCell = Grid.PosToCell(pos),
                    IsMoving = isMoving,
                    TargetCell = targetCell
                };

                Commands.CommandManager.SendCommand(cmd);
            }
        }

        /// <summary>
        /// Apply a position sync from Host.
        /// </summary>
        public void ApplyPositionSync(Commands.PositionSyncCommand cmd)
        {
            // Only clients should apply position sync
            if (Antigravity.Core.Network.MultiplayerState.IsHost) return;

            MinionIdentity minion = GetMinion(cmd.DuplicantId);
            if (minion == null) return;

            Vector3 hostPos = new Vector3(cmd.PositionX, cmd.PositionY, cmd.PositionZ);
            Vector3 localPos = minion.transform.position;

            // Check distance - only correct if drift is significant (> 1 cell)
            float distance = Vector3.Distance(hostPos, localPos);
            if (distance > 1.5f)
            {
                Debug.Log($"[Antigravity] Position correction for {minion.name}: drift={distance:F2}");
                minion.transform.SetPosition(hostPos);
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
    }
}
