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
        /// </summary>
        public static void PerformHardSync()
        {
            if (!Network.NetworkManager.IsHost) return;

            OnBeforeHardSync?.Invoke();

            try
            {
                // TODO: Implement actual hard sync logic
                // 1. Pause game
                // 2. Save game state
                // 3. Send save to all clients
                // 4. Clients load save
                // 5. Resume game

                LastHardSyncTick = CurrentTick;
                SyncErrorCount = 0; // Reset error count after successful sync

                Log.Info($"[Antigravity] Hard sync performed at tick {CurrentTick}");
            }
            catch (Exception ex)
            {
                Log.Error($"[Antigravity] Hard sync failed: {ex.Message}");
                OnSyncError?.Invoke($"Hard sync failed: {ex.Message}");
            }

            OnAfterHardSync?.Invoke();
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
