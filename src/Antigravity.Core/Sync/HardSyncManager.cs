using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using UnityEngine;

namespace Antigravity.Core.Sync
{
    /// <summary>
    /// Manages hard synchronization - full game state sync via save data.
    /// This corrects any accumulated desynchronization by reloading the game state.
    /// </summary>
    public class HardSyncManager
    {
        private static HardSyncManager _instance;
        public static HardSyncManager Instance => _instance ??= new HardSyncManager();
        
        private int _lastSyncCycle = -1;
        private bool _syncInProgress = false;
        private float _pendingSyncDelay = 0f;
        
        // Settings
        public bool EnableDailySync { get; set; } = true;
        public bool EnableSaveSync { get; set; } = true;
        
        private HardSyncManager() { }
        
        #region Host Side - Trigger Sync
        
        /// <summary>
        /// Called when a new game day starts (Host only).
        /// </summary>
        public void OnNewDay(int cycle)
        {
            if (!MultiplayerState.IsHost) return;
            if (!MultiplayerState.IsMultiplayerSession) return;
            if (!EnableDailySync) return;
            
            // Only sync once per cycle
            if (cycle <= _lastSyncCycle) return;
            _lastSyncCycle = cycle;
            
            Debug.Log($"[Antigravity] HardSync: New day detected (cycle {cycle}), triggering hard sync...");
            
            // Delay slightly to let game state settle
            _pendingSyncDelay = 1.0f;
        }
        
        /// <summary>
        /// Called when host saves the game.
        /// </summary>
        public void OnManualSave()
        {
            if (!MultiplayerState.IsHost) return;
            if (!EnableSaveSync) return;
            
            Debug.Log("[Antigravity] HardSync: Manual save detected, triggering hard sync...");
            TriggerHardSync(HardSyncReason.ManualSave);
        }
        
        /// <summary>
        /// Update loop - check for pending syncs.
        /// </summary>
        public void Update()
        {
            if (!MultiplayerState.IsHost) return;
            if (_syncInProgress) return;
            
            if (_pendingSyncDelay > 0)
            {
                _pendingSyncDelay -= Time.deltaTime;
                if (_pendingSyncDelay <= 0)
                {
                    TriggerHardSync(HardSyncReason.NewDay);
                }
            }
        }
        
        /// <summary>
        /// Trigger a hard sync to all clients.
        /// </summary>
        public void TriggerHardSync(HardSyncReason reason)
        {
            if (!MultiplayerState.IsHost) return;
            if (_syncInProgress) return;
            
            _syncInProgress = true;
            
            try
            {
                Debug.Log($"[Antigravity] HardSync: Starting sync (reason: {reason})...");
                
                // Get current save data
                byte[] saveData = GetCurrentSaveData();
                if (saveData == null || saveData.Length == 0)
                {
                    Debug.LogError("[Antigravity] HardSync: Failed to get save data");
                    return;
                }
                
                // Compress the data
                byte[] compressedData = CompressData(saveData);
                string base64Data = Convert.ToBase64String(compressedData);
                string dataHash = ComputeHash(compressedData);
                
                Debug.Log($"[Antigravity] HardSync: Save data {saveData.Length} bytes -> compressed {compressedData.Length} bytes");
                
                // Get current game time
                int cycle = GameClock.Instance?.GetCycle() ?? 0;
                float timeOfDay = GameClock.Instance?.GetCurrentCycleAsPercentage() ?? 0f;
                
                // Send command
                var cmd = new HardSyncCommand
                {
                    Reason = reason,
                    SaveData = base64Data,
                    Cycle = cycle,
                    TimeOfDay = timeOfDay,
                    DataHash = dataHash
                };
                
                CommandManager.SendCommand(cmd);
                Debug.Log($"[Antigravity] HardSync: Sent {base64Data.Length} chars to clients");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] HardSync: Failed to trigger sync: {ex.Message}");
            }
            finally
            {
                _syncInProgress = false;
            }
        }
        
        #endregion
        
        #region Client Side - Apply Sync
        
        /// <summary>
        /// Apply hard sync data received from host (Client only).
        /// </summary>
        public void ApplyHardSync(HardSyncCommand cmd)
        {
            if (MultiplayerState.IsHost) return;
            if (cmd == null || string.IsNullOrEmpty(cmd.SaveData)) return;
            
            Debug.Log($"[Antigravity] HardSync: Received sync (reason: {cmd.Reason}, cycle: {cmd.Cycle})");
            
            try
            {
                // Decode and decompress
                byte[] compressedData = Convert.FromBase64String(cmd.SaveData);
                
                // Verify hash
                string computedHash = ComputeHash(compressedData);
                if (computedHash != cmd.DataHash)
                {
                    Debug.LogError("[Antigravity] HardSync: Data hash mismatch! Sync aborted.");
                    return;
                }
                
                byte[] saveData = DecompressData(compressedData);
                Debug.Log($"[Antigravity] HardSync: Decompressed {compressedData.Length} -> {saveData.Length} bytes");
                
                // Write to temp file and load
                string tempPath = Path.Combine(Path.GetTempPath(), "antigravity_hardsync.sav");
                File.WriteAllBytes(tempPath, saveData);
                
                // Load the save
                // Note: This will reload the entire game state
                LoadSaveFile(tempPath);
                
                Debug.Log("[Antigravity] HardSync: Sync applied successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] HardSync: Failed to apply sync: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Save Data Helpers
        
        private byte[] GetCurrentSaveData()
        {
            try
            {
                // Create a temporary save
                string tempPath = Path.Combine(Path.GetTempPath(), "antigravity_hostsync.sav");
                
                // Use SaveLoader to save current state
                if (SaveLoader.Instance != null)
                {
                    SaveLoader.Instance.Save(tempPath, false, false);
                    
                    if (File.Exists(tempPath))
                    {
                        return File.ReadAllBytes(tempPath);
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] HardSync: GetCurrentSaveData failed: {ex.Message}");
                return null;
            }
        }
        
        private void LoadSaveFile(string path)
        {
            try
            {
                if (SaveLoader.Instance != null && File.Exists(path))
                {
                    // Queue the load for next frame to avoid issues
                    SaveLoader.Instance.Load(path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] HardSync: LoadSaveFile failed: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Compression Helpers
        
        private byte[] CompressData(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }
        
        private byte[] DecompressData(byte[] compressedData)
        {
            using (var input = new MemoryStream(compressedData))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }
        
        private string ComputeHash(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }
        
        #endregion
        
        /// <summary>
        /// Reset state when leaving multiplayer.
        /// </summary>
        public void Reset()
        {
            _lastSyncCycle = -1;
            _syncInProgress = false;
            _pendingSyncDelay = 0f;
        }
    }
}
