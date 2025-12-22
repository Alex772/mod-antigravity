using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Network;
using System.IO;

namespace Antigravity.Patches.Game
{
    /// <summary>
    /// Patches for game loading to support multiplayer synchronization.
    /// </summary>
    public static class GameLoadingPatches
    {
        private static bool _isMultiplayerLoad = false;
        private static byte[] _pendingWorldData = null;

        /// <summary>
        /// Set up world data to load (for clients).
        /// </summary>
        public static void SetPendingWorldData(byte[] worldData)
        {
            _pendingWorldData = worldData;
            _isMultiplayerLoad = true;
            Debug.Log($"[Antigravity] Pending world data set: {worldData.Length} bytes");
        }

        /// <summary>
        /// Clear pending world data.
        /// </summary>
        public static void ClearPendingWorldData()
        {
            _pendingWorldData = null;
            _isMultiplayerLoad = false;
        }

        /// <summary>
        /// Check if this is a multiplayer load.
        /// </summary>
        public static bool IsMultiplayerLoad => _isMultiplayerLoad;

        /// <summary>
        /// Get pending world data.
        /// </summary>
        public static byte[] GetPendingWorldData() => _pendingWorldData;

        /// <summary>
        /// Get the temp file path for multiplayer save.
        /// </summary>
        public static string GetMultiplayerTempSavePath()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "Antigravity");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            return Path.Combine(tempDir, "multiplayer_sync.sav");
        }

        /// <summary>
        /// Write pending world data to temp file and return path.
        /// </summary>
        public static string WritePendingWorldDataToFile()
        {
            if (_pendingWorldData == null)
            {
                Debug.LogError("[Antigravity] No pending world data to write!");
                return null;
            }

            string path = GetMultiplayerTempSavePath();
            try
            {
                File.WriteAllBytes(path, _pendingWorldData);
                Debug.Log($"[Antigravity] World data written to: {path}");
                return path;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to write world data: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Patch to detect when a game save is loaded.
    /// Note: Applied manually by PatchManager.
    /// </summary>
    public static class SaveLoader_Load_Patch
    {
        /// <summary>
        /// Called after a save is loaded.
        /// </summary>
        public static void Postfix(string filename)
        {
            Debug.Log($"[Antigravity] Save loaded: {filename}");

            // Get colony name from filename
            string colonyName = Path.GetFileNameWithoutExtension(filename);

            // Update multiplayer state
            if (MultiplayerState.IsMultiplayerSession)
            {
                MultiplayerState.OnGameLoaded(colonyName);
            }

            // If host and in multiplayer session, we need to sync this save to clients
            if (MultiplayerState.IsMultiplayerSession && MultiplayerState.IsHost && SteamNetworkManager.IsConnected)
            {
                Debug.Log("[Antigravity] Host loaded save in multiplayer mode, preparing to sync with clients...");
                OnHostSaveLoaded(filename, colonyName);
            }
        }

        private static void OnHostSaveLoaded(string filename, string colonyName)
        {
            try
            {
                // Read the save file that was just loaded
                if (File.Exists(filename))
                {
                    byte[] saveData = File.ReadAllBytes(filename);
                    Debug.Log($"[Antigravity] Read save data: {saveData.Length} bytes");

                    // Count connected players (excluding self)
                    int connectedCount = SteamNetworkManager.ConnectedPlayers.Count;
                    Debug.Log($"[Antigravity] Connected players count: {connectedCount}");
                    MultiplayerState.TotalPlayers = connectedCount + 1; // +1 for host

                    // Sync if there are any other players connected
                    if (connectedCount > 0)
                    {
                        Debug.Log($"[Antigravity] Syncing to {connectedCount} connected players...");
                        // Start the game session sync
                        GameSession.HostStartGame(saveData, colonyName, true);
                    }
                    else
                    {
                        Debug.Log("[Antigravity] No other players connected, skipping sync.");
                        MultiplayerState.OnSyncComplete();
                    }
                }
                else
                {
                    Debug.LogError($"[Antigravity] Save file not found: {filename}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Error preparing save sync: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Patch to detect when game world is ready (after loading completes).
    /// Note: Applied manually by PatchManager.
    /// </summary>
    public static class Game_OnSpawn_Patch
    {
        public static void Postfix(global::Game __instance)
        {
            Debug.Log("[Antigravity] Game.OnSpawn called - world is ready");

            if (MultiplayerState.IsMultiplayerSession)
            {
                // The game world is now fully loaded and ready
                Debug.Log("[Antigravity] Multiplayer game world ready!");
                
                // If host, pause the game until all clients are synced
                if (MultiplayerState.IsHost)
                {
                    Debug.Log("[Antigravity] Host: Pausing game for sync...");
                    // SpeedControlScreen.Instance can be used to control speed
                }
            }
        }
    }


    // Note: SpeedControlScreen.OnChange patch removed - method doesn't exist in current ONI version
    // Speed sync will be implemented differently when needed
}
