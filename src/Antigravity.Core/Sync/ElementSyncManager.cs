using System;
using System.Collections.Generic;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;

namespace Antigravity.Core.Sync
{
    /// <summary>
    /// Manages synchronization of element (gas/liquid) changes between host and clients.
    /// Uses delta-based approach to minimize bandwidth.
    /// </summary>
    public class ElementSyncManager
    {
        private static ElementSyncManager _instance;
        public static ElementSyncManager Instance => _instance ??= new ElementSyncManager();
        
        // Pending changes to broadcast (Host only)
        private readonly List<ElementDelta> _pendingChanges = new List<ElementDelta>();
        private readonly object _changeLock = new object();
        
        // Timing
        private float _lastBroadcastTime = 0f;
        private const float BROADCAST_INTERVAL = 0.5f; // Broadcast every 0.5 seconds
        private const int MAX_CHANGES_PER_BATCH = 500; // Limit changes per message
        
        // Stats
        private int _changesSentThisSecond = 0;
        private float _statsResetTime = 0f;
        
        private ElementSyncManager() { }
        
        #region Host Side - Tracking Changes
        
        /// <summary>
        /// Track an element change (called from Harmony patches on Host).
        /// </summary>
        public void TrackElementChange(int cell, int elementId, float mass, float temperature, ElementChangeType changeType)
        {
            if (!MultiplayerState.IsHost) return;
            if (!MultiplayerState.IsMultiplayerSession) return;
            if (!Grid.IsValidCell(cell)) return;
            
            // Determine element type from the element
            bool isGas = false;
            bool isLiquid = false;
            var element = ElementLoader.FindElementByHash((SimHashes)elementId);
            if (element != null)
            {
                isGas = element.IsGas;
                isLiquid = element.IsLiquid;
            }
            
            lock (_changeLock)
            {
                _pendingChanges.Add(new ElementDelta
                {
                    Cell = cell,
                    ElementId = elementId,
                    Mass = mass,
                    Temperature = temperature,
                    ChangeType = changeType,
                    IsGas = isGas,
                    IsLiquid = isLiquid
                });
            }
        }
        
        /// <summary>
        /// Track an element being added/modified at a cell.
        /// </summary>
        public void TrackElementAdd(int cell, SimHashes element, float mass, float temperature)
        {
            TrackElementChange(cell, (int)element, mass, temperature, ElementChangeType.Add);
        }
        
        /// <summary>
        /// Track an element being removed from a cell.
        /// </summary>
        public void TrackElementRemove(int cell, SimHashes previousElement)
        {
            TrackElementChange(cell, (int)previousElement, 0f, 0f, ElementChangeType.Remove);
        }
        
        /// <summary>
        /// Track an element being replaced.
        /// </summary>
        public void TrackElementReplace(int cell, SimHashes newElement, float mass, float temperature)
        {
            TrackElementChange(cell, (int)newElement, mass, temperature, ElementChangeType.Replace);
        }
        
        #endregion
        
        #region Host Side - Broadcasting
        
        /// <summary>
        /// Update method - call from game update loop.
        /// Broadcasts pending changes if interval has elapsed.
        /// </summary>
        public void Update()
        {
            if (!MultiplayerState.IsHost) return;
            if (!MultiplayerState.IsMultiplayerSession) return;
            
            float currentTime = Time.time;
            
            // Reset stats counter
            if (currentTime - _statsResetTime >= 1f)
            {
                _statsResetTime = currentTime;
                _changesSentThisSecond = 0;
            }
            
            // Check if it's time to broadcast pending changes
            if (currentTime - _lastBroadcastTime >= BROADCAST_INTERVAL)
            {
                _lastBroadcastTime = currentTime;
                BroadcastPendingChanges();
            }
            
            // Check if it's time to send full element snapshot
            CheckFullSnapshotTimer();
        }
        
        /// <summary>
        /// Broadcast all pending element changes to clients.
        /// </summary>
        private void BroadcastPendingChanges()
        {
            List<ElementDelta> changesToSend;
            
            lock (_changeLock)
            {
                if (_pendingChanges.Count == 0) return;
                
                // Take a batch
                int count = Math.Min(_pendingChanges.Count, MAX_CHANGES_PER_BATCH);
                changesToSend = new List<ElementDelta>(_pendingChanges.GetRange(0, count));
                _pendingChanges.RemoveRange(0, count);
            }
            
            if (changesToSend.Count == 0) return;
            
            try
            {
                var cmd = new ElementChangeCommand
                {
                    Changes = changesToSend
                };
                
                CommandManager.SendCommand(cmd);
                _changesSentThisSecond += changesToSend.Count;
                
                if (changesToSend.Count > 10)
                {
                    Debug.Log($"[Antigravity] ElementSync: Sent {changesToSend.Count} element changes");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] ElementSync: Error broadcasting changes: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Force immediate broadcast (e.g., on significant events).
        /// </summary>
        public void ForceFlush()
        {
            if (!MultiplayerState.IsHost) return;
            BroadcastPendingChanges();
        }
        
        // Full snapshot timer
        private float _lastFullSnapshotTime = 0f;
        private const float FULL_SNAPSHOT_INTERVAL = 10f; // Full resync every 10 seconds
        
        /// <summary>
        /// Check if it's time to send a full snapshot and do so.
        /// Call from Update() periodically.
        /// </summary>
        public void CheckFullSnapshotTimer()
        {
            if (!MultiplayerState.IsHost) return;
            if (!MultiplayerState.IsMultiplayerSession) return;
            
            float currentTime = Time.time;
            if (currentTime - _lastFullSnapshotTime >= FULL_SNAPSHOT_INTERVAL)
            {
                _lastFullSnapshotTime = currentTime;
                SendFullElementSnapshot();
            }
        }
        
        /// <summary>
        /// Send a full snapshot of all gases and liquids in the world.
        /// Used for initial sync or periodic resync.
        /// </summary>
        public void SendFullElementSnapshot()
        {
            if (!MultiplayerState.IsHost) return;
            if (!MultiplayerState.IsMultiplayerSession) return;
            if (Grid.CellCount == 0) return;
            
            try
            {
                var gasChanges = new List<ElementDelta>();
                var liquidChanges = new List<ElementDelta>();
                
                int cellCount = Grid.CellCount;
                float minMass = 0.1f;
                
                for (int cell = 0; cell < cellCount; cell++)
                {
                    if (!Grid.IsValidCell(cell)) continue;
                    
                    Element element = Grid.Element[cell];
                    if (element == null) continue;
                    
                    float mass = Grid.Mass[cell];
                    if (mass < minMass) continue;
                    
                    if (element.IsGas)
                    {
                        gasChanges.Add(new ElementDelta
                        {
                            Cell = cell,
                            ElementId = (int)element.id,
                            Mass = mass,
                            Temperature = Grid.Temperature[cell],
                            ChangeType = ElementChangeType.Replace,
                            IsGas = true,
                            IsLiquid = false
                        });
                    }
                    else if (element.IsLiquid)
                    {
                        liquidChanges.Add(new ElementDelta
                        {
                            Cell = cell,
                            ElementId = (int)element.id,
                            Mass = mass,
                            Temperature = Grid.Temperature[cell],
                            ChangeType = ElementChangeType.Replace,
                            IsGas = false,
                            IsLiquid = true
                        });
                    }
                }
                
                // Send in batches to avoid huge packets
                SendBatchedChanges(gasChanges, "gas");
                SendBatchedChanges(liquidChanges, "liquid");
                
                Debug.Log($"[Antigravity] ElementSync: Full snapshot sent - {gasChanges.Count} gas, {liquidChanges.Count} liquid cells");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] ElementSync: Error sending full snapshot: {ex.Message}");
            }
        }
        
        private void SendBatchedChanges(List<ElementDelta> changes, string type)
        {
            const int batchSize = 1000;
            
            for (int i = 0; i < changes.Count; i += batchSize)
            {
                int count = Math.Min(batchSize, changes.Count - i);
                var batch = changes.GetRange(i, count);
                
                var cmd = new ElementChangeCommand { Changes = batch };
                CommandManager.SendCommand(cmd);
            }
        }
        
        #endregion
        
        #region Client Side - Applying Changes
        
        /// <summary>
        /// Apply received element changes (called on Client).
        /// </summary>
        public void ApplyElementChanges(ElementChangeCommand cmd)
        {
            if (MultiplayerState.IsHost) return; // Host doesn't apply received changes
            if (cmd?.Changes == null || cmd.Changes.Count == 0) return;
            
            int applied = 0;
            int failed = 0;
            
            foreach (var delta in cmd.Changes)
            {
                try
                {
                    if (!Grid.IsValidCell(delta.Cell))
                    {
                        failed++;
                        continue;
                    }
                    
                    ApplySingleChange(delta);
                    applied++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Antigravity] ElementSync: Failed to apply change at cell {delta.Cell}: {ex.Message}");
                    failed++;
                }
            }
            
            if (applied > 10 || failed > 0)
            {
                Debug.Log($"[Antigravity] ElementSync: Applied {applied} changes, {failed} failed");
            }
        }
        
        /// <summary>
        /// Apply a single element change to the grid.
        /// </summary>
        private void ApplySingleChange(ElementDelta delta)
        {
            var element = ElementLoader.FindElementByHash((SimHashes)delta.ElementId);
            if (element == null)
            {
                Debug.LogWarning($"[Antigravity] ElementSync: Unknown element ID {delta.ElementId}");
                return;
            }
            
            switch (delta.ChangeType)
            {
                case ElementChangeType.Add:
                case ElementChangeType.Replace:
                    // Use SimMessages to properly update the cell
                    SimMessages.ReplaceElement(
                        gameCell: delta.Cell,
                        new_element: (SimHashes)delta.ElementId,
                        ev: null,
                        mass: delta.Mass,
                        temperature: delta.Temperature,
                        diseaseIdx: byte.MaxValue,
                        diseaseCount: 0
                    );
                    break;
                    
                case ElementChangeType.Remove:
                    // Replace with vacuum
                    SimMessages.ReplaceElement(
                        gameCell: delta.Cell,
                        new_element: SimHashes.Vacuum,
                        ev: null,
                        mass: 0f,
                        temperature: 0f,
                        diseaseIdx: byte.MaxValue,
                        diseaseCount: 0
                    );
                    break;
                    
                case ElementChangeType.Modify:
                    // For modifications, just use ReplaceElement with the new mass/temp
                    SimMessages.ReplaceElement(
                        gameCell: delta.Cell,
                        new_element: (SimHashes)delta.ElementId,
                        ev: null,
                        mass: delta.Mass,
                        temperature: delta.Temperature,
                        diseaseIdx: Grid.DiseaseIdx[delta.Cell],
                        diseaseCount: Grid.DiseaseCount[delta.Cell]
                    );
                    break;
            }
        }
        
        #endregion
        
        #region Stats
        
        /// <summary>
        /// Get pending change count (for debugging).
        /// </summary>
        public int PendingChangeCount
        {
            get
            {
                lock (_changeLock)
                {
                    return _pendingChanges.Count;
                }
            }
        }
        
        /// <summary>
        /// Get changes sent in the last second.
        /// </summary>
        public int ChangesSentPerSecond => _changesSentThisSecond;
        
        /// <summary>
        /// Clear all pending changes.
        /// </summary>
        public void Clear()
        {
            lock (_changeLock)
            {
                _pendingChanges.Clear();
            }
        }
        
        #endregion
    }
}
