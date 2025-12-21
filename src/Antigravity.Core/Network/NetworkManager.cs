using System;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Antigravity.Core.Network
{
    // Custom delegates for events
    public delegate void NetworkEventHandler();
    public delegate void NetworkPlayerEventHandler(int playerId);
    public delegate void NetworkDataEventHandler(NetPeer peer, NetDataReader reader);

    /// <summary>
    /// Manages network connections for multiplayer sessions.
    /// </summary>
    public static class NetworkManager
    {
        /// <summary>
        /// The network manager instance (host or client).
        /// </summary>
        private static NetManager _netManager;

        /// <summary>
        /// Event listener for network events.
        /// </summary>
        private static EventBasedNetListener _listener;

        /// <summary>
        /// Whether we are the host of the session.
        /// </summary>
        public static bool IsHost { get; private set; }

        /// <summary>
        /// Whether we are connected to a session.
        /// </summary>
        public static bool IsConnected { get; private set; }

        /// <summary>
        /// Current session ID.
        /// </summary>
        public static string SessionId { get; private set; }

        /// <summary>
        /// Local player ID.
        /// </summary>
        public static int LocalPlayerId { get; private set; }

        /// <summary>
        /// Event fired when connected to a session.
        /// </summary>
        public static event NetworkEventHandler OnConnected;

        /// <summary>
        /// Event fired when disconnected from a session.
        /// </summary>
        public static event NetworkEventHandler OnDisconnected;

        /// <summary>
        /// Event fired when a player joins.
        /// </summary>
        public static event NetworkPlayerEventHandler OnPlayerJoined;

        /// <summary>
        /// Event fired when a player leaves.
        /// </summary>
        public static event NetworkPlayerEventHandler OnPlayerLeft;

        /// <summary>
        /// Event fired when data is received.
        /// </summary>
        public static event NetworkDataEventHandler OnDataReceived;

        /// <summary>
        /// Initialize the network manager.
        /// </summary>
        public static void Initialize()
        {
            _listener = new EventBasedNetListener();
            SetupEventListeners();
        }

        /// <summary>
        /// Set up network event listeners.
        /// </summary>
        private static void SetupEventListeners()
        {
            _listener.ConnectionRequestEvent += request =>
            {
                // Accept connections with matching protocol version
                if (_netManager.ConnectedPeersCount < Constants.DefaultPort) // TODO: Use maxPlayers config
                {
                    request.AcceptIfKey(Constants.ProtocolVersion.ToString());
                }
                else
                {
                    request.Reject();
                }
            };

            _listener.PeerConnectedEvent += peer =>
            {
                Log.Info($"[Antigravity] Peer connected: {peer.Address}");
                OnPlayerJoined?.Invoke(peer.Id);
            };

            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                Log.Info($"[Antigravity] Peer disconnected: {peer.Address}, Reason: {info.Reason}");
                OnPlayerLeft?.Invoke(peer.Id);
            };

            _listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
            {
                OnDataReceived?.Invoke(peer, reader);
                reader.Recycle();
            };
        }

        /// <summary>
        /// Start hosting a multiplayer session.
        /// </summary>
        /// <param name="port">Port to listen on.</param>
        /// <returns>Session ID for others to join.</returns>
        public static string StartHost(int port = 0)
        {
            if (port == 0) port = Constants.DefaultPort;

            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true,
                EnableStatistics = true
            };

            if (!_netManager.Start(port))
            {
                throw new Exception($"Failed to start host on port {port}");
            }

            IsHost = true;
            IsConnected = true;
            LocalPlayerId = 0; // Host is always player 0
            SessionId = GenerateSessionId();

            OnConnected?.Invoke();

            Log.Info($"[Antigravity] Started hosting on port {port}. Session ID: {SessionId}");
            return SessionId;
        }

        /// <summary>
        /// Connect to an existing multiplayer session.
        /// </summary>
        /// <param name="address">Host address.</param>
        /// <param name="port">Host port.</param>
        public static void Connect(string address, int port = 0)
        {
            if (port == 0) port = Constants.DefaultPort;

            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true
            };

            if (!_netManager.Start())
            {
                throw new Exception("Failed to start network client");
            }

            _netManager.Connect(address, port, Constants.ProtocolVersion.ToString());

            IsHost = false;
            Log.Info($"[Antigravity] Connecting to {address}:{port}...");
        }

        /// <summary>
        /// Send data to all connected peers.
        /// </summary>
        public static void SendToAll(byte[] data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            if (_netManager == null || !IsConnected) return;

            var writer = new NetDataWriter();
            writer.Put(data);

            foreach (var peer in _netManager.ConnectedPeerList)
            {
                peer.Send(writer, deliveryMethod);
            }
        }

        /// <summary>
        /// Send data to a specific peer.
        /// </summary>
        public static void SendTo(int peerId, byte[] data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            if (_netManager == null || !IsConnected) return;

            var peer = _netManager.GetPeerById(peerId);
            if (peer == null) return;

            var writer = new NetDataWriter();
            writer.Put(data);
            peer.Send(writer, deliveryMethod);
        }

        /// <summary>
        /// Process network events. Call this every frame.
        /// </summary>
        public static void Update()
        {
            _netManager?.PollEvents();
        }

        /// <summary>
        /// Disconnect from the current session.
        /// </summary>
        public static void Disconnect()
        {
            if (_netManager == null) return;

            _netManager.Stop();
            _netManager = null;

            IsHost = false;
            IsConnected = false;
            SessionId = null;

            OnDisconnected?.Invoke();

            Log.Info("[Antigravity] Disconnected from session.");
        }

        /// <summary>
        /// Generate a random session ID.
        /// </summary>
        private static string GenerateSessionId()
        {
            // Generate a 6-character alphanumeric code
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new char[6];
            
            for (int i = 0; i < 6; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }

            return new string(result);
        }

        /// <summary>
        /// Get network statistics.
        /// </summary>
        public static NetStatistics GetStatistics()
        {
            return _netManager?.Statistics;
        }

        /// <summary>
        /// Get the number of connected players.
        /// </summary>
        public static int ConnectedPlayerCount => (_netManager?.ConnectedPeersCount ?? 0) + (IsHost ? 1 : 0);
    }
}
