using UnityEngine;
using Antigravity.Core.Network;
using Antigravity.Core.Commands;
using Antigravity.Core.Sync;
using System.IO;

namespace Antigravity.Client
{
    /// <summary>
    /// MonoBehaviour that handles multiplayer updates and world sync.
    /// This component stays active even when the lobby screen is hidden.
    /// </summary>
    public class MultiplayerUpdater : MonoBehaviour
    {
        private static MultiplayerUpdater _instance;
        private static GameObject _updaterObject;

        // Client loading state
        private bool _isWaitingForWorldData = false;
        private bool _isLoadingWorld = false;
        private string _loadingColonyName = "";

        /// <summary>
        /// Ensure the updater exists and is running.
        /// </summary>
        public static void EnsureExists()
        {
            if (_instance == null || _updaterObject == null)
            {
                _updaterObject = new GameObject("AntigravityMultiplayerUpdater");
                _instance = _updaterObject.AddComponent<MultiplayerUpdater>();
                
                // Add RemoteCursorManager for cursor sync
                _updaterObject.AddComponent<RemoteCursorManager>();
                
                // Add UI overlays
                _updaterObject.AddComponent<ChatOverlay>();
                _updaterObject.AddComponent<CursorCoordsOverlay>();
                _updaterObject.AddComponent<ModSettingsPanel>();
                _updaterObject.AddComponent<PingManager>();
                
                DontDestroyOnLoad(_updaterObject);
                Debug.Log("[Antigravity] MultiplayerUpdater created with UI overlays.");
            }
        }

        /// <summary>
        /// Get the instance.
        /// </summary>
        public static MultiplayerUpdater Instance => _instance;

        private void Awake()
        {
            _instance = this;

            // Subscribe to game session events
            GameSession.OnGameStarting += OnGameStarting;
            GameSession.OnWorldDataReceived += OnWorldDataReceived;
            GameSession.OnGameStarted += OnGameStarted;
            GameSession.OnAllPlayersReady += OnAllPlayersReady;

            // Initialize command manager for syncing commands
            CommandManager.Initialize();

            Debug.Log("[Antigravity] MultiplayerUpdater initialized.");
        }

        private void OnDestroy()
        {
            GameSession.OnGameStarting -= OnGameStarting;
            GameSession.OnWorldDataReceived -= OnWorldDataReceived;
            GameSession.OnGameStarted -= OnGameStarted;
            GameSession.OnAllPlayersReady -= OnAllPlayersReady;
        }

        // Checksum sync timer (Host only, every 5 seconds)
        private float _checksumTimer = 0f;
        private const float CHECKSUM_INTERVAL = 5f;

        // Position sync timer (Host only, every 0.5 seconds)
        private float _positionSyncTimer = 0f;
        private const float POSITION_SYNC_INTERVAL = 0.5f;
        
        // Item sync timer (Host only, every 10 seconds)
        private float _itemSyncTimer = 0f;
        private const float ITEM_SYNC_INTERVAL = 10f;

        // Time sync timer (Host only, every 5 seconds)
        private float _timeSyncTimer = 0f;
        private const float TIME_SYNC_INTERVAL = 5f;

        private void Update()
        {
            // CRITICAL: Always poll network messages, even when paused
            // This ensures pause/unpause commands are received
            if (SteamNetworkManager.IsConnected)
            {
                SteamNetworkManager.Update();
            }
            
            if (NetworkBackendManager.IsLocalMode && NetworkBackendManager.IsConnected)
            {
                NetworkBackendManager.Update();
            }

            // CRITICAL: Always process commands when connected
            // Pause/Unpause commands MUST be processed even when paused
            bool isConnected = SteamNetworkManager.IsConnected || 
                (NetworkBackendManager.IsLocalMode && NetworkBackendManager.IsConnected);
            
            if (isConnected)
            {
                CommandManager.ProcessPendingCommands();
            }

            // Host sync logic - only when game is loaded and NOT paused
            bool canRunHostSync = MultiplayerState.IsMultiplayerSession && 
                                  MultiplayerState.IsGameLoaded && 
                                  MultiplayerState.IsHost &&
                                  SpeedControlScreen.Instance != null &&
                                  !SpeedControlScreen.Instance.IsPaused;
            
            if (canRunHostSync)
            {
                // Advance SyncEngine tick counter for periodic sync checks
                SyncEngine.ProcessTick();
                // Position sync (every 2 seconds)
                _positionSyncTimer += Time.deltaTime;
                if (_positionSyncTimer >= POSITION_SYNC_INTERVAL)
                {
                    _positionSyncTimer = 0f;
                    Antigravity.Core.Sync.DuplicantSyncManager.Instance?.SendPositionSync();
                }

                // Checksum sync (every 5 seconds)
                _checksumTimer += Time.deltaTime;
                if (_checksumTimer >= CHECKSUM_INTERVAL)
                {
                    _checksumTimer = 0f;
                    Antigravity.Core.Sync.DuplicantSyncManager.Instance?.SendMinionChecksums();
                }
                
                // Item sync (every 10 seconds)
                _itemSyncTimer += Time.deltaTime;
                if (_itemSyncTimer >= ITEM_SYNC_INTERVAL)
                {
                    _itemSyncTimer = 0f;
                    Antigravity.Core.Sync.DuplicantSyncManager.Instance?.SendItemSync();
                }
                
                // Element sync (continuous delta broadcast)
                Antigravity.Core.Sync.ElementSyncManager.Instance?.Update();

                // Time sync (every 5 seconds) — keeps client GameClock and pauseCount in sync
                _timeSyncTimer += Time.deltaTime;
                if (_timeSyncTimer >= TIME_SYNC_INTERVAL)
                {
                    _timeSyncTimer = 0f;
                    SendTimeSync();
                }
            }
            
            // Client: Process pending chores when game is loaded (even if paused - to prepare for unpause)
            if (MultiplayerState.IsMultiplayerSession && MultiplayerState.IsGameLoaded && !MultiplayerState.IsHost)
            {
                Antigravity.Core.Sync.DuplicantSyncManager.Instance?.ProcessPendingChores();
            }
        }

        #region Game Session Events

        /// <summary>
        /// Called when the host starts the game (for clients).
        /// </summary>
        private void OnGameStarting()
        {
            if (SteamNetworkManager.IsHost) return;

            Debug.Log("[Antigravity] Client: Host is starting the game!");
            _isWaitingForWorldData = true;

            // Update UI to show loading state
            MultiplayerLobbyScreen.UpdateStatus("🌍 Host is loading the world...", Color.yellow);
        }

        /// <summary>
        /// Called when world data is received from host (for clients).
        /// </summary>
        private void OnWorldDataReceived(byte[] worldData)
        {
            if (SteamNetworkManager.IsHost) return;

            Debug.Log($"[Antigravity] Client: Received world data ({worldData.Length} bytes)");
            _isWaitingForWorldData = false;
            _isLoadingWorld = true;

            // Update UI
            MultiplayerLobbyScreen.UpdateStatus("📂 Loading world...", Color.cyan);

            // Save the world data to a temp file
            string tempPath = WriteTempSaveFile(worldData);

            if (string.IsNullOrEmpty(tempPath))
            {
                Debug.LogError("[Antigravity] Failed to write world data to temp file!");
                MultiplayerLobbyScreen.UpdateStatus("❌ Failed to load world!", Color.red);
                return;
            }

            // Hide the lobby screen
            MultiplayerLobbyScreen.Hide();

            // Load the save file
            Debug.Log($"[Antigravity] Client: Loading world from {tempPath}");
            
            try
            {
                // Use SaveLoader to load the save
                LoadWorld(tempPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to load world: {ex.Message}");
                MultiplayerLobbyScreen.UpdateStatus($"❌ Load failed: {ex.Message}", Color.red);
            }
        }

        /// <summary>
        /// Write world data to a temporary save file.
        /// </summary>
        private string WriteTempSaveFile(byte[] worldData)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "Antigravity");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                
                string tempPath = Path.Combine(tempDir, "multiplayer_sync.sav");
                File.WriteAllBytes(tempPath, worldData);
                Debug.Log($"[Antigravity] World data written to: {tempPath} ({worldData.Length} bytes)");
                return tempPath;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to write temp save: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load the world from a save file.
        /// </summary>
        private void LoadWorld(string savePath)
        {
            Debug.Log($"[Antigravity] Loading world from: {savePath}");

            // Mark that we're in multiplayer
            MultiplayerState.IsMultiplayerSession = true;
            MultiplayerState.IsHost = false;

            try
            {
                // Method 1: Try SaveLoader.Instance if available
                if (SaveLoader.Instance != null)
                {
                    SaveLoader.Instance.Load(savePath);
                    Debug.Log("[Antigravity] SaveLoader.Load called successfully");
                    return;
                }

                // Method 2: Use App.LoadScene which is how the game normally loads saves
                Debug.Log("[Antigravity] SaveLoader.Instance is null, using App.LoadScene...");
                
                // Set the save file that should be loaded
                SaveLoader.SetActiveSaveFilePath(savePath);
                
                // Load the game scene with the save
                App.LoadScene("backend");
                
                Debug.Log("[Antigravity] App.LoadScene called to load save");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Error during world load: {ex.Message}");
                Debug.LogError(ex.StackTrace);
                
                // Try alternative: direct scene load
                try
                {
                    Debug.Log("[Antigravity] Trying alternative load method...");
                    SaveLoader.SetActiveSaveFilePath(savePath);
                    App.LoadScene("backend");
                }
                catch (System.Exception ex2)
                {
                    Debug.LogError($"[Antigravity] Alternative load also failed: {ex2.Message}");
                    MultiplayerLobbyScreen.UpdateStatus("❌ Failed to load world!", Color.red);
                }
            }
        }

        /// <summary>
        /// Called when game has started (world is ready).
        /// </summary>
        private void OnGameStarted()
        {
            Debug.Log("[Antigravity] Game started in multiplayer mode!");
            _isLoadingWorld = false;

            // Initialize DuplicantSyncManager
            Antigravity.Core.Sync.DuplicantSyncManager.Instance.Initialize();

            // Initialize and start SyncEngine for periodic sync checks
            SyncEngine.Initialize();
            SyncEngine.Start();
            Debug.Log("[Antigravity] SyncEngine started.");

            // If host, send random seed to ensure deterministic behavior
            if (SteamNetworkManager.IsHost)
            {
                Debug.Log("[Antigravity] Host sending random seed to clients...");
                Antigravity.Core.Sync.DuplicantSyncManager.Instance.SendRandomSeed();
            }
            else
            {
                // If client, notify host that we're ready
                GameSession.ClientReady();
            }
        }

        /// <summary>
        /// Called when all players have finished loading (Host only).
        /// Automatically unpauses the game.
        /// </summary>
        private void OnAllPlayersReady()
        {
            if (!SteamNetworkManager.IsHost) return;
            
            Debug.Log("[Antigravity] All players ready! Auto-unpausing game...");
            
            // Small delay to ensure everything is synced, then unpause
            StartCoroutine(DelayedUnpause());
        }
        
        private System.Collections.IEnumerator DelayedUnpause()
        {
            // Wait a short moment for any pending syncs
            yield return new WaitForSeconds(0.5f);
            
            if (SpeedControlScreen.Instance != null && SpeedControlScreen.Instance.IsPaused)
            {
                Debug.Log("[Antigravity] Unpausing game after all players ready...");
                SpeedControlScreen.Instance.Unpause(playSound: true);
            }
        }

        #endregion

        /// <summary>
        /// Start waiting for world data (called when client sees host start game).
        /// </summary>
        public void StartWaitingForWorldData()
        {
            _isWaitingForWorldData = true;
            Debug.Log("[Antigravity] Client now waiting for world data...");
        }

        /// <summary>
        /// Check if client is waiting for world data.
        /// </summary>
        public bool IsWaitingForWorldData => _isWaitingForWorldData;

        /// <summary>
        /// Check if world is currently loading.
        /// </summary>
        public bool IsLoadingWorld => _isLoadingWorld;

        #region Time Sync (Fix 3)

        // Cached reflection for SpeedControlScreen.pauseCount (private field)
        private static System.Reflection.FieldInfo _pauseCountField;

        /// <summary>
        /// Sends the host's current time state (pauseCount, speed, GameClock time) to all clients.
        /// Called periodically by the host (every TIME_SYNC_INTERVAL seconds).
        /// </summary>
        private void SendTimeSync()
        {
            if (!MultiplayerState.IsHost) return;
            if (SpeedControlScreen.Instance == null) return;

            // Get pauseCount via reflection (it's a private field)
            int pauseCount = 0;
            if (_pauseCountField == null)
            {
                _pauseCountField = typeof(SpeedControlScreen).GetField("pauseCount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }
            if (_pauseCountField != null)
            {
                pauseCount = (int)_pauseCountField.GetValue(SpeedControlScreen.Instance);
            }

            // Get current speed
            int speed = SpeedControlScreen.Instance.IsPaused ? 0 : 
                (int)typeof(SpeedControlScreen).GetField("speed", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(SpeedControlScreen.Instance);

            // Get GameClock time
            float gameTime = 0f;
            int cycle = 0;
            float timeSinceStartOfCycle = 0f;
            if (GameClock.Instance != null)
            {
                gameTime = GameClock.Instance.GetTime();
                cycle = GameClock.Instance.GetCycle();
                timeSinceStartOfCycle = GameClock.Instance.GetTimeSinceStartOfCycle();
            }

            var cmd = new Antigravity.Core.Commands.TimeSyncCommand
            {
                PauseCount = pauseCount,
                Speed = speed,
                GameTime = gameTime,
                Cycle = cycle,
                TimeSinceStartOfCycle = timeSinceStartOfCycle
            };

            CommandManager.SendCommand(cmd);
        }

        #endregion
    }
}
