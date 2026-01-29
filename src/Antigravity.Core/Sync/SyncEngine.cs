using System;
using System.Collections.Generic;

namespace Antigravity.Core.Sync
{
    // Custom delegates for sync events
    public delegate void SyncEventHandler();
    public delegate void SyncErrorEventHandler(string error);

    /// <summary>
    /// Engine responsible for synchronizing game state between players.
    /// </summary>
    public static class SyncEngine
    {
        /// <summary>
        /// Current synchronization tick.
        /// </summary>
        public static int CurrentTick { get; private set; }

        /// <summary>
        /// Whether the sync engine is running.
        /// </summary>
        public static bool IsRunning { get; private set; }

        /// <summary>
        /// Number of sync errors detected.
        /// </summary>
        public static int SyncErrorCount { get; private set; }

        /// <summary>
        /// Last tick when a hard sync was performed.
        /// </summary>
        public static int LastHardSyncTick { get; private set; }

        /// <summary>
        /// Event fired before a hard sync.
        /// </summary>
        public static event SyncEventHandler OnBeforeHardSync;

        /// <summary>
        /// Event fired after a hard sync.
        /// </summary>
        public static event SyncEventHandler OnAfterHardSync;

        /// <summary>
        /// Event fired when a sync error is detected.
        /// </summary>
        public static event SyncErrorEventHandler OnSyncError;

        /// <summary>
        /// Initialize the sync engine.
        /// </summary>
        public static void Initialize()
        {
            CurrentTick = 0;
            SyncErrorCount = 0;
            LastHardSyncTick = 0;
            IsRunning = false;
            
            // Initialize partial sync manager
            PartialSyncManager.Initialize();
        }

        /// <summary>
        /// Start the sync engine.
        /// </summary>
        public static void Start()
        {
            IsRunning = true;
            CurrentTick = 0;
            Log.Info("[Antigravity] Sync engine started.");
        }

        /// <summary>
        /// Stop the sync engine.
        /// </summary>
        public static void Stop()
        {
            IsRunning = false;
            Log.Info("[Antigravity] Sync engine stopped.");
        }

        /// <summary>
        /// Process a game tick.
        /// </summary>
        public static void ProcessTick()
        {
            if (!IsRunning) return;

            CurrentTick++;

            // Process pending commands at this tick
            Commands.CommandDispatcher.ProcessPendingCommands();

            // Update partial sync manager (periodic verification)
            PartialSyncManager.Update();
            
            // HOST: Periodic position sync every ~0.5 seconds (30 ticks at 60 FPS)
            if (Network.MultiplayerState.IsHost && CurrentTick % 30 == 0)
            {
                DuplicantSyncManager.Instance?.SendPositionSync();
            }
            
            // HOST: Periodic item sync every ~10 seconds (600 ticks at 60 FPS)
            if (Network.MultiplayerState.IsHost && CurrentTick % 600 == 0)
            {
                DuplicantSyncManager.Instance?.SendItemSync();
            }

            // Check if we need a hard sync
            CheckHardSync();
        }

        /// <summary>
        /// Check if a hard sync is needed and perform it.
        /// </summary>
        private static void CheckHardSync()
        {
            // Hard sync every game day (or as configured)
            int hardSyncInterval = Constants.TicksPerDay;

            if (CurrentTick - LastHardSyncTick >= hardSyncInterval)
            {
                PerformHardSync();
            }
        }

        /// <summary>
        /// Perform a full state synchronization.
        /// Saves current game state and sends to all clients.
        /// </summary>
        public static void PerformHardSync()
        {
            if (!Network.NetworkManager.IsHost) return;
            if (_isPerformingHardSync) return; // Prevent re-entry
            
            _isPerformingHardSync = true;
            OnBeforeHardSync?.Invoke();

            try
            {
                Log.Info("[Antigravity] Starting hard sync...");
                
                // 1. Pause game
                if (SpeedControlScreen.Instance != null)
                {
                    SpeedControlScreen.Instance.Pause(playSound: false);
                }
                
                // 2. Save current game state
                string colonyName = SaveGame.Instance?.BaseName ?? "Colony";
                string savePath = SaveLoader.Instance?.Save(colonyName, isAutoSave: false, updateSavePointer: false);
                
                if (string.IsNullOrEmpty(savePath) || !System.IO.File.Exists(savePath))
                {
                    Log.Error("[Antigravity] Hard sync failed: Could not save game");
                    return;
                }
                
                // 3. Read save file bytes
                byte[] saveData = System.IO.File.ReadAllBytes(savePath);
                Log.Info($"[Antigravity] Hard sync save: {saveData.Length} bytes");
                
                // 4. Compress and send to all clients
                byte[] compressedData = Network.MessageSerializer.Compress(saveData);
                
                var startingMsg = new Network.GameStartingMessage
                {
                    IsLoadingSave = true,
                    ColonyName = colonyName,
                    TotalDataSize = compressedData.Length,
                    ChunkCount = CalculateChunkCount(compressedData.Length),
                    IsHardSync = true  // Mark as hard sync so clients know
                };
                
                // Send to all clients
                Network.GameSession.SendToAllClients(Network.MessageType.GameStarting, startingMsg);
                
                // Send world data in chunks
                SendHardSyncChunks(compressedData);
                
                LastHardSyncTick = CurrentTick;
                SyncErrorCount = 0;
                
                Log.Info($"[Antigravity] Hard sync sent at tick {CurrentTick}");
                
                // 5. Resume game after short delay (clients will reload and signal ready)
                // For now, just unpause - clients will catch up
                if (SpeedControlScreen.Instance != null)
                {
                    SpeedControlScreen.Instance.Unpause(playSound: false);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Antigravity] Hard sync failed: {ex.Message}");
                OnSyncError?.Invoke($"Hard sync failed: {ex.Message}");
            }
            finally
            {
                _isPerformingHardSync = false;
            }

            OnAfterHardSync?.Invoke();
        }
        
        private static bool _isPerformingHardSync = false;
        
        private static int CalculateChunkCount(int dataSize)
        {
            const int CHUNK_SIZE = 64 * 1024;
            return (dataSize + CHUNK_SIZE - 1) / CHUNK_SIZE;
        }
        
        private static void SendHardSyncChunks(byte[] compressedData)
        {
            const int CHUNK_SIZE = 64 * 1024;
            int chunkCount = CalculateChunkCount(compressedData.Length);
            
            for (int i = 0; i < chunkCount; i++)
            {
                int offset = i * CHUNK_SIZE;
                int length = Math.Min(CHUNK_SIZE, compressedData.Length - offset);
                byte[] chunkData = new byte[length];
                Buffer.BlockCopy(compressedData, offset, chunkData, 0, length);
                
                var chunk = new Network.WorldDataChunk
                {
                    ChunkIndex = i,
                    TotalChunks = chunkCount,
                    Data = chunkData
                };
                
                Network.GameSession.SendToAllClients(Network.MessageType.WorldDataChunk, chunk);
            }
        }

        /// <summary>
        /// Request a hard sync from the host.
        /// </summary>
        public static void RequestHardSync()
        {
            if (Network.NetworkManager.IsHost)
            {
                PerformHardSync();
            }
            else
            {
                // TODO: Send sync request to host
            }
        }

        /// <summary>
        /// Report a synchronization error.
        /// </summary>
        public static void ReportSyncError(string description)
        {
            SyncErrorCount++;
            Log.Warning($"[Antigravity] Sync error: {description}");
            OnSyncError?.Invoke(description);

            // If too many errors, request hard sync
            if (SyncErrorCount >= 10)
            {
                Log.Info("[Antigravity] Too many sync errors, requesting hard sync...");
                RequestHardSync();
            }
        }

        /// <summary>
        /// Calculate a checksum for the current game state.
        /// Used to detect desynchronization.
        /// </summary>
        public static long CalculateStateChecksum()
        {
            try
            {
                var checksums = WorldStateHasher.CalculateAllChecksums();
                
                // Combine all category checksums into one
                long combined = checksums.PickupablesChecksum;
                combined = combined * 31 + checksums.BuildingsChecksum;
                combined = combined * 31 + checksums.DuplicantsChecksum;
                combined = combined * 31 + checksums.ConduitsChecksum;
                
                return combined;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Verify that local state matches the expected checksum.
        /// </summary>
        public static bool VerifyChecksum(long expectedChecksum)
        {
            var localChecksum = CalculateStateChecksum();
            
            if (localChecksum != expectedChecksum)
            {
                ReportSyncError($"Checksum mismatch: local={localChecksum}, expected={expectedChecksum}");
                return false;
            }

            return true;
        }
    }
}
