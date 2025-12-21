using Antigravity.Core.Network;

namespace Antigravity.Client
{
    /// <summary>
    /// Manages client-side operations for multiplayer.
    /// </summary>
    public static class ClientManager
    {
        /// <summary>
        /// Whether the client is initialized.
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// Current connection state.
        /// </summary>
        public static ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        /// <summary>
        /// Initialize the client manager.
        /// </summary>
        public static void Initialize()
        {
            // Subscribe to network events
            NetworkManager.OnConnected += OnConnected;
            NetworkManager.OnDisconnected += OnDisconnected;

            IsInitialized = true;
            UnityEngine.Debug.Log("[Antigravity.Client] Client manager initialized.");
        }

        /// <summary>
        /// Connect to a multiplayer session.
        /// </summary>
        /// <param name="address">Host address.</param>
        /// <param name="port">Host port.</param>
        public static void Connect(string address, int port)
        {
            if (State != ConnectionState.Disconnected)
            {
                UnityEngine.Debug.LogWarning("[Antigravity.Client] Already connected or connecting!");
                return;
            }

            State = ConnectionState.Connecting;
            NetworkManager.Connect(address, port);
        }

        /// <summary>
        /// Disconnect from the current session.
        /// </summary>
        public static void Disconnect()
        {
            NetworkManager.Disconnect();
            State = ConnectionState.Disconnected;
        }

        private static void OnConnected()
        {
            State = ConnectionState.Connected;
            UnityEngine.Debug.Log("[Antigravity.Client] Connected to server!");
        }

        private static void OnDisconnected()
        {
            State = ConnectionState.Disconnected;
            UnityEngine.Debug.Log("[Antigravity.Client] Disconnected from server.");
        }

        /// <summary>
        /// Shutdown the client manager.
        /// </summary>
        public static void Shutdown()
        {
            Disconnect();
            NetworkManager.OnConnected -= OnConnected;
            NetworkManager.OnDisconnected -= OnDisconnected;
            IsInitialized = false;
        }
    }

    /// <summary>
    /// Connection state enumeration.
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting
    }
}
