using System;
using UnityEngine;

namespace Antigravity.Core.Network
{
    /// <summary>
    /// Manages the active network backend and provides a unified API.
    /// Use Local mode for testing without Steam, Steam mode for production.
    /// </summary>
    public static class NetworkBackendManager
    {
        /// <summary>
        /// The currently active backend.
        /// </summary>
        public static INetworkBackend Active { get; private set; }

        /// <summary>
        /// Whether we're using local (LiteNetLib) or Steam networking.
        /// </summary>
        public static bool IsLocalMode { get; private set; }

        /// <summary>
        /// Shortcut to check if connected.
        /// </summary>
        public static bool IsConnected => Active?.IsConnected ?? false;

        /// <summary>
        /// Shortcut to check if we are the host.
        /// </summary>
        public static bool IsHost => Active?.IsHost ?? false;

        /// <summary>
        /// Local player ID.
        /// </summary>
        public static PlayerId LocalPlayerId => Active?.LocalPlayerId ?? PlayerId.Invalid;

        /// <summary>
        /// Host player ID.
        /// </summary>
        public static PlayerId HostPlayerId => Active?.HostPlayerId ?? PlayerId.Invalid;

        // Events forwarded from active backend
        public static event EventHandler OnConnected;
        public static event EventHandler OnDisconnected;
        public static event EventHandler<PlayerEventArgs> OnPlayerJoined;
        public static event EventHandler<PlayerEventArgs> OnPlayerLeft;
        public static event EventHandler<DataReceivedEventArgs> OnDataReceived;

        private static bool _initialized = false;

        /// <summary>
        /// Initialize with Steam backend (default for production).
        /// </summary>
        public static bool InitializeSteam()
        {
            return Initialize(new SteamNetworkBackend());
        }

        /// <summary>
        /// Initialize with LiteNetLib backend (for local testing).
        /// </summary>
        public static bool InitializeLocal()
        {
            return Initialize(new LiteNetLibBackend());
        }

        /// <summary>
        /// Initialize with a specific backend.
        /// </summary>
        public static bool Initialize(INetworkBackend backend)
        {
            if (_initialized && Active != null)
            {
                // Shutdown existing backend
                UnsubscribeFromBackend(Active);
                Active.Shutdown();
            }

            Active = backend;
            IsLocalMode = backend.BackendType == NetworkBackendType.LiteNetLib;

            if (!backend.Initialize())
            {
                Debug.LogError($"[Antigravity] Failed to initialize {backend.BackendType} backend");
                return false;
            }

            SubscribeToBackend(backend);
            _initialized = true;

            Debug.Log($"[Antigravity] Network backend initialized: {backend.BackendType}");
            return true;
        }

        /// <summary>
        /// Host a game using the active backend.
        /// </summary>
        public static string HostGame(int maxPlayers = 4)
        {
            if (Active == null)
            {
                Debug.LogError("[Antigravity] No network backend initialized!");
                return null;
            }
            return Active.HostGame(maxPlayers);
        }

        /// <summary>
        /// Join a game using the active backend.
        /// For Steam: pass lobby ID. For Local: pass IP:Port.
        /// </summary>
        public static void JoinGame(string connectionInfo)
        {
            if (Active == null)
            {
                Debug.LogError("[Antigravity] No network backend initialized!");
                return;
            }
            Active.JoinGame(connectionInfo);
        }

        /// <summary>
        /// Disconnect from the current session.
        /// </summary>
        public static void Disconnect()
        {
            Active?.Disconnect();
        }

        /// <summary>
        /// Send data to all connected players.
        /// </summary>
        public static bool SendToAll(byte[] data, SendReliability reliability = SendReliability.Reliable)
        {
            return Active?.SendToAll(data, reliability) ?? false;
        }

        /// <summary>
        /// Send data to a specific player.
        /// </summary>
        public static bool SendTo(PlayerId target, byte[] data, SendReliability reliability = SendReliability.Reliable)
        {
            return Active?.SendTo(target, data, reliability) ?? false;
        }

        /// <summary>
        /// Get player display name.
        /// </summary>
        public static string GetPlayerName(PlayerId playerId)
        {
            return Active?.GetPlayerName(playerId) ?? $"Unknown ({playerId.Value})";
        }

        /// <summary>
        /// Process network events. Call every frame.
        /// </summary>
        public static void Update()
        {
            Active?.Update();
        }

        /// <summary>
        /// Shutdown the network backend.
        /// </summary>
        public static void Shutdown()
        {
            if (Active != null)
            {
                UnsubscribeFromBackend(Active);
                Active.Shutdown();
                Active = null;
            }
            _initialized = false;
        }

        private static void SubscribeToBackend(INetworkBackend backend)
        {
            backend.OnConnected += HandleConnected;
            backend.OnDisconnected += HandleDisconnected;
            backend.OnPlayerJoined += HandlePlayerJoined;
            backend.OnPlayerLeft += HandlePlayerLeft;
            backend.OnDataReceived += HandleDataReceived;
        }

        private static void UnsubscribeFromBackend(INetworkBackend backend)
        {
            backend.OnConnected -= HandleConnected;
            backend.OnDisconnected -= HandleDisconnected;
            backend.OnPlayerJoined -= HandlePlayerJoined;
            backend.OnPlayerLeft -= HandlePlayerLeft;
            backend.OnDataReceived -= HandleDataReceived;
        }

        private static void HandleConnected(object sender, EventArgs e) => OnConnected?.Invoke(sender, e);
        private static void HandleDisconnected(object sender, EventArgs e) => OnDisconnected?.Invoke(sender, e);
        private static void HandlePlayerJoined(object sender, PlayerEventArgs e) => OnPlayerJoined?.Invoke(sender, e);
        private static void HandlePlayerLeft(object sender, PlayerEventArgs e) => OnPlayerLeft?.Invoke(sender, e);
        private static void HandleDataReceived(object sender, DataReceivedEventArgs e) => OnDataReceived?.Invoke(sender, e);
    }
}
