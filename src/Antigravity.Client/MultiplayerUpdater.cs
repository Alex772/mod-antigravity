using UnityEngine;
using Antigravity.Core.Network;
using Antigravity.Core.Commands;
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

            // Initialize command manager for syncing commands
            CommandManager.Initialize();

            Debug.Log("[Antigravity] MultiplayerUpdater initialized.");
        }

        private void OnDestroy()
        {
            GameSession.OnGameStarting -= OnGameStarting;
            GameSession.OnWorldDataReceived -= OnWorldDataReceived;
            GameSession.OnGameStarted -= OnGameStarted;
        }

        // Checksum sync timer (Host only, every 5 seconds)
        private float _checksumTimer = 0f;
        private const float CHECKSUM_INTERVAL = 5f;

        // Position sync timer (Host only, every 2 seconds)
        private float _positionSyncTimer = 0f;
        private const float POSITION_SYNC_INTERVAL = 2f;
        
        // Item sync timer (Host only, every 10 seconds)
        private float _itemSyncTimer = 0f;
        private const float ITEM_SYNC_INTERVAL = 10f;

        private void Update()
        {
            // Poll for network messages (Steam mode)
            if (SteamNetworkManager.IsConnected)
            {
                SteamNetworkManager.Update();
            }
            
            // Poll for network messages (Local mode - LiteNetLib)
            if (NetworkBackendManager.IsLocalMode && NetworkBackendManager.IsConnected)
            {
                NetworkBackendManager.Update();
            }

            // Process any pending commands from other players
            // In local mode (terminal client testing), always process commands
            bool shouldProcessCommands = (MultiplayerState.IsMultiplayerSession && MultiplayerState.IsGameLoaded)
                || (NetworkBackendManager.IsLocalMode && NetworkBackendManager.IsConnected);
            
            if (shouldProcessCommands)
            {
                CommandManager.ProcessPendingCommands();

                // Host sync logic
                if (MultiplayerState.IsHost)
                {
                    // Position sync (every 2 seconds)
                    _positionSyncTimer += Time.deltaTime;
                    if (_positionSyncTimer >= POSITION_SYNC_INTERVAL)
                    {
                        _positionSyncTimer = 0f;
                        Antigravity.Core.Sync.DuplicantSyncManager.Instance.SendPositionSync();
                    }

                    // Checksum sync (every 5 seconds)
                    _checksumTimer += Time.deltaTime;
                    if (_checksumTimer >= CHECKSUM_INTERVAL)
                    {
                        _checksumTimer = 0f;
                        Antigravity.Core.Sync.DuplicantSyncManager.Instance.SendMinionChecksums();
                    }
                    
                    // Item sync (every 10 seconds)
                    _itemSyncTimer += Time.deltaTime;
                    if (_itemSyncTimer >= ITEM_SYNC_INTERVAL)
                    {
                        _itemSyncTimer = 0f;
                        Antigravity.Core.Sync.DuplicantSyncManager.Instance.SendItemSync();
                    }
                    
                    // Element sync (continuous delta broadcast)
                    Antigravity.Core.Sync.ElementSyncManager.Instance.Update();
                }
                else
                {
                    // Client: Process any pending chores that couldn't be assigned immediately
                    Antigravity.Core.Sync.DuplicantSyncManager.Instance.ProcessPendingChores();
                }
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
    }
}
