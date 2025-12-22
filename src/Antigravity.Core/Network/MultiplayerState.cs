using UnityEngine;

namespace Antigravity.Core.Network
{
    /// <summary>
    /// Static class to track multiplayer session state across the game.
    /// This persists even when UI screens are closed.
    /// </summary>
    public static class MultiplayerState
    {
        // Session state
        public static bool IsMultiplayerSession { get; set; } = false;
        public static bool IsHost { get; set; } = false;
        public static bool IsGameLoaded { get; set; } = false;
        public static bool IsSyncing { get; set; } = false;

        // Game state
        public static string CurrentColonyName { get; set; } = "";
        public static long CurrentTick { get; set; } = 0;

        // Sync state
        public static bool WorldDataSent { get; set; } = false;
        public static int PlayersReady { get; set; } = 0;
        public static int TotalPlayers { get; set; } = 0;

        /// <summary>
        /// Reset all multiplayer state.
        /// </summary>
        public static void Reset()
        {
            IsMultiplayerSession = false;
            IsHost = false;
            IsGameLoaded = false;
            IsSyncing = false;
            CurrentColonyName = "";
            CurrentTick = 0;
            WorldDataSent = false;
            PlayersReady = 0;
            TotalPlayers = 0;

            Debug.Log("[Antigravity] MultiplayerState reset.");
        }

        /// <summary>
        /// Initialize for a new multiplayer session.
        /// </summary>
        public static void InitializeSession(bool isHost)
        {
            IsMultiplayerSession = true;
            IsHost = isHost;
            IsGameLoaded = false;
            IsSyncing = false;
            WorldDataSent = false;
            PlayersReady = 0;
            TotalPlayers = SteamNetworkManager.ConnectedPlayers.Count;

            Debug.Log($"[Antigravity] MultiplayerState initialized. IsHost: {isHost}, Players: {TotalPlayers}");
        }

        /// <summary>
        /// Called when the game world is loaded.
        /// </summary>
        public static void OnGameLoaded(string colonyName)
        {
            IsGameLoaded = true;
            CurrentColonyName = colonyName;

            Debug.Log($"[Antigravity] Game loaded in multiplayer mode. Colony: {colonyName}");

            // If host, start syncing to clients
            if (IsHost && IsMultiplayerSession)
            {
                StartHostSync();
            }
        }

        /// <summary>
        /// [HOST] Start syncing world data to clients.
        /// </summary>
        private static void StartHostSync()
        {
            if (!IsHost || WorldDataSent) return;

            Debug.Log("[Antigravity] Host starting world sync...");
            IsSyncing = true;

            // The actual sync will be triggered by the SaveLoader patch
            // This just marks that we're ready to sync
        }

        /// <summary>
        /// [CLIENT] Called when world data is received from host.
        /// </summary>
        public static void OnWorldDataReceived()
        {
            Debug.Log("[Antigravity] World data received from host.");
            IsSyncing = true;
        }

        /// <summary>
        /// Called when sync is complete and game can start.
        /// </summary>
        public static void OnSyncComplete()
        {
            IsSyncing = false;
            Debug.Log("[Antigravity] Sync complete! Game starting...");
        }

        /// <summary>
        /// Get a status string for display.
        /// </summary>
        public static string GetStatusString()
        {
            if (!IsMultiplayerSession)
                return "Not in multiplayer";

            if (IsSyncing)
                return IsHost ? "Syncing world to clients..." : "Receiving world data...";

            if (IsGameLoaded)
                return $"Playing: {CurrentColonyName}";

            return IsHost ? "Setting up game..." : "Waiting for host...";
        }
    }
}
