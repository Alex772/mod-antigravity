using System.Collections.Generic;
using Antigravity.Core.Network;

namespace Antigravity.Server
{
    /// <summary>
    /// Manages server-side operations for multiplayer.
    /// </summary>
    public static class ServerManager
    {
        /// <summary>
        /// Whether the server is running.
        /// </summary>
        public static bool IsRunning { get; private set; }

        /// <summary>
        /// Current session information.
        /// </summary>
        public static SessionInfo CurrentSession { get; private set; }

        /// <summary>
        /// Connected players.
        /// </summary>
        private static readonly Dictionary<int, PlayerInfo> Players = new Dictionary<int, PlayerInfo>();

        /// <summary>
        /// Start hosting a multiplayer session.
        /// </summary>
        /// <param name="port">Port to listen on.</param>
        /// <returns>Session ID for players to join.</returns>
        public static string StartServer(int port = 0)
        {
            if (IsRunning)
            {
                UnityEngine.Debug.LogWarning("[Antigravity.Server] Server already running!");
                return CurrentSession?.SessionId;
            }

            // Start network host
            string sessionId = NetworkManager.StartHost(port);

            // Create session info
            CurrentSession = new SessionInfo
            {
                SessionId = sessionId,
                HostName = "Player 1", // TODO: Get actual player name
                MaxPlayers = 4 // TODO: Get from config
            };

            // Add host as player 0
            Players[0] = new PlayerInfo
            {
                PlayerId = 0,
                PlayerName = CurrentSession.HostName,
                IsHost = true
            };

            // Subscribe to events
            NetworkManager.OnPlayerJoined += OnPlayerJoined;
            NetworkManager.OnPlayerLeft += OnPlayerLeft;

            IsRunning = true;
            UnityEngine.Debug.Log($"[Antigravity.Server] Server started. Session: {sessionId}");

            return sessionId;
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public static void StopServer()
        {
            if (!IsRunning) return;

            NetworkManager.OnPlayerJoined -= OnPlayerJoined;
            NetworkManager.OnPlayerLeft -= OnPlayerLeft;
            NetworkManager.Disconnect();

            Players.Clear();
            CurrentSession = null;
            IsRunning = false;

            UnityEngine.Debug.Log("[Antigravity.Server] Server stopped.");
        }

        private static void OnPlayerJoined(int peerId)
        {
            var playerInfo = new PlayerInfo
            {
                PlayerId = peerId,
                PlayerName = $"Player {peerId + 1}",
                IsHost = false
            };

            Players[peerId] = playerInfo;
            UnityEngine.Debug.Log($"[Antigravity.Server] Player joined: {playerInfo.PlayerName}");

            // TODO: Send current game state to new player
        }

        private static void OnPlayerLeft(int peerId)
        {
            if (Players.TryGetValue(peerId, out var playerInfo))
            {
                UnityEngine.Debug.Log($"[Antigravity.Server] Player left: {playerInfo.PlayerName}");
                Players.Remove(peerId);
            }
        }

        /// <summary>
        /// Get a player by ID.
        /// </summary>
        public static PlayerInfo GetPlayer(int playerId)
        {
            return Players.TryGetValue(playerId, out var player) ? player : null;
        }

        /// <summary>
        /// Get all connected players.
        /// </summary>
        public static IEnumerable<PlayerInfo> GetAllPlayers()
        {
            return Players.Values;
        }

        /// <summary>
        /// Get the number of connected players.
        /// </summary>
        public static int PlayerCount => Players.Count;
    }

    /// <summary>
    /// Information about a multiplayer session.
    /// </summary>
    public class SessionInfo
    {
        public string SessionId { get; set; }
        public string HostName { get; set; }
        public int MaxPlayers { get; set; }
    }

    /// <summary>
    /// Information about a connected player.
    /// </summary>
    public class PlayerInfo
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public bool IsHost { get; set; }
        public int Latency { get; set; }
    }
}
