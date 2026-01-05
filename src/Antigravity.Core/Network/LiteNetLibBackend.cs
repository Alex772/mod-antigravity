using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Antigravity.Core.Network
{
    /// <summary>
    /// LiteNetLib implementation of INetworkBackend.
    /// For local testing without Steam dependencies.
    /// </summary>
    public class LiteNetLibBackend : INetworkBackend
    {
        public NetworkBackendType BackendType => NetworkBackendType.LiteNetLib;
        public bool IsInitialized { get; private set; }
        public bool IsHost { get; private set; }
        public bool IsConnected { get; private set; }
        
        public PlayerId LocalPlayerId { get; private set; }
        public PlayerId HostPlayerId { get; private set; }

        private readonly List<PlayerId> _connectedPlayers = new List<PlayerId>();
        public IReadOnlyList<PlayerId> ConnectedPlayers => _connectedPlayers;

        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;
        public event EventHandler<PlayerEventArgs> OnPlayerJoined;
        public event EventHandler<PlayerEventArgs> OnPlayerLeft;
        public event EventHandler<DataReceivedEventArgs> OnDataReceived;

        private NetManager _netManager;
        private EventBasedNetListener _listener;
        private readonly Dictionary<int, string> _playerNames = new Dictionary<int, string>();
        
        private const int DEFAULT_PORT = 7777;
        private const string CONNECTION_KEY = "Antigravity_v1";

        public bool Initialize()
        {
            if (IsInitialized) return true;

            try
            {
                _listener = new EventBasedNetListener();
                SetupEventListeners();
                IsInitialized = true;
                Debug.Log("[Antigravity] LiteNetLib backend initialized.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to initialize LiteNetLib: {ex.Message}");
                return false;
            }
        }

        private void SetupEventListeners()
        {
            _listener.ConnectionRequestEvent += request =>
            {
                Debug.Log($"[Antigravity] Connection request from {request.RemoteEndPoint}");
                request.AcceptIfKey(CONNECTION_KEY);
            };

            _listener.PeerConnectedEvent += peer =>
            {
                Debug.Log($"[Antigravity] Peer connected: {peer.Address}:{peer.Port} (ID: {peer.Id})");
                
                var playerId = PlayerId.FromLiteNetLib(peer.Id);
                _connectedPlayers.Add(playerId);
                _playerNames[peer.Id] = $"Player {peer.Id + 1}";
                
                if (!IsHost)
                {
                    // We're a client connecting to host
                    IsConnected = true;
                    HostPlayerId = playerId;
                    OnConnected?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    OnPlayerJoined?.Invoke(this, new PlayerEventArgs(playerId, _playerNames[peer.Id]));
                }
            };

            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                Debug.Log($"[Antigravity] Peer disconnected: {peer.Address}:{peer.Port}, Reason: {info.Reason}");
                
                var playerId = PlayerId.FromLiteNetLib(peer.Id);
                _connectedPlayers.Remove(playerId);
                _playerNames.Remove(peer.Id);
                
                OnPlayerLeft?.Invoke(this, new PlayerEventArgs(playerId));
            };

            _listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
            {
                try
                {
                    var playerId = PlayerId.FromLiteNetLib(peer.Id);
                    
                    // Read the length prefix (4 bytes) that was added by SendToAll/SendTo
                    int length = reader.GetInt();
                    if (length > 0 && length <= reader.AvailableBytes)
                    {
                        byte[] data = new byte[length];
                        reader.GetBytes(data, length);
                        
                        Debug.Log($"[Antigravity] LiteNetLib received {length} bytes from peer {peer.Id}");
                        OnDataReceived?.Invoke(this, new DataReceivedEventArgs(playerId, data));
                    }
                    else
                    {
                        Debug.LogWarning($"[Antigravity] Invalid packet length: {length}, available: {reader.AvailableBytes}");
                    }
                }
                finally
                {
                    reader.Recycle();
                }
            };

            _listener.NetworkErrorEvent += (endPoint, error) =>
            {
                Debug.LogError($"[Antigravity] Network error from {endPoint}: {error}");
            };
        }

        public string HostGame(int maxPlayers = 4)
        {
            if (!IsInitialized) Initialize();

            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true,
                EnableStatistics = true,
                BroadcastReceiveEnabled = true
            };

            if (!_netManager.Start(DEFAULT_PORT))
            {
                Debug.LogError($"[Antigravity] Failed to start LiteNetLib on port {DEFAULT_PORT}");
                return null;
            }

            IsHost = true;
            IsConnected = true;
            LocalPlayerId = PlayerId.FromLiteNetLib(0); // Host is always ID 0
            HostPlayerId = LocalPlayerId;
            _playerNames[0] = "Host";

            Debug.Log($"[Antigravity] LiteNetLib hosting on port {DEFAULT_PORT}");
            
            OnConnected?.Invoke(this, EventArgs.Empty);
            
            return $"127.0.0.1:{DEFAULT_PORT}";
        }

        public void JoinGame(string connectionInfo)
        {
            if (!IsInitialized) Initialize();

            // Parse connection info (format: IP:Port or just IP)
            string address = "127.0.0.1";
            int port = DEFAULT_PORT;

            if (!string.IsNullOrEmpty(connectionInfo))
            {
                var parts = connectionInfo.Split(':');
                address = parts[0];
                if (parts.Length > 1 && int.TryParse(parts[1], out int parsedPort))
                {
                    port = parsedPort;
                }
            }

            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true
            };

            if (!_netManager.Start())
            {
                Debug.LogError("[Antigravity] Failed to start LiteNetLib client");
                return;
            }

            IsHost = false;
            LocalPlayerId = PlayerId.FromLiteNetLib(-1); // Will be updated after connection
            
            Debug.Log($"[Antigravity] Connecting to {address}:{port}...");
            _netManager.Connect(address, port, CONNECTION_KEY);
        }

        public void Disconnect()
        {
            if (_netManager == null) return;

            _netManager.Stop();
            _netManager = null;
            
            IsHost = false;
            IsConnected = false;
            _connectedPlayers.Clear();
            _playerNames.Clear();

            OnDisconnected?.Invoke(this, EventArgs.Empty);
            Debug.Log("[Antigravity] LiteNetLib disconnected.");
        }

        public bool SendToAll(byte[] data, SendReliability reliability = SendReliability.Reliable)
        {
            if (_netManager == null || !IsConnected) return false;

            var deliveryMethod = reliability == SendReliability.Reliable 
                ? DeliveryMethod.ReliableOrdered 
                : DeliveryMethod.Unreliable;

            var writer = new NetDataWriter();
            writer.Put(data.Length);
            writer.Put(data);

            foreach (var peer in _netManager.ConnectedPeerList)
            {
                peer.Send(writer, deliveryMethod);
            }

            return true;
        }
        
        /// <summary>
        /// Send data to all connected players except the specified one.
        /// </summary>
        public bool SendToAllExcept(PlayerId except, byte[] data, SendReliability reliability = SendReliability.Reliable)
        {
            if (_netManager == null || !IsConnected) return false;

            var deliveryMethod = reliability == SendReliability.Reliable 
                ? DeliveryMethod.ReliableOrdered 
                : DeliveryMethod.Unreliable;

            var writer = new NetDataWriter();
            writer.Put(data.Length);
            writer.Put(data);

            int exceptPeerId = except.AsPeerId;
            int sentCount = 0;
            int skipped = 0;
            
            Debug.Log($"[Antigravity] SendToAllExcept: except.Value={except.Value}, except.AsPeerId={exceptPeerId}, total peers={_netManager.ConnectedPeerList.Count}");
            
            foreach (var peer in _netManager.ConnectedPeerList)
            {
                Debug.Log($"[Antigravity] Checking peer.Id={peer.Id} vs exceptPeerId={exceptPeerId}");
                if (peer.Id != exceptPeerId)
                {
                    peer.Send(writer, deliveryMethod);
                    sentCount++;
                    Debug.Log($"[Antigravity] Sent to peer {peer.Id}");
                }
                else
                {
                    skipped++;
                    Debug.Log($"[Antigravity] Skipped peer {peer.Id} (matches sender)");
                }
            }

            Debug.Log($"[Antigravity] SendToAllExcept complete: sent to {sentCount}, skipped {skipped}");
            return true;
        }

        public bool SendTo(PlayerId target, byte[] data, SendReliability reliability = SendReliability.Reliable)
        {
            if (_netManager == null || !IsConnected) return false;

            var peer = _netManager.GetPeerById(target.AsPeerId);
            if (peer == null) return false;

            var deliveryMethod = reliability == SendReliability.Reliable 
                ? DeliveryMethod.ReliableOrdered 
                : DeliveryMethod.Unreliable;

            var writer = new NetDataWriter();
            writer.Put(data.Length);
            writer.Put(data);

            peer.Send(writer, deliveryMethod);
            return true;
        }

        public string GetPlayerName(PlayerId playerId)
        {
            if (_playerNames.TryGetValue(playerId.AsPeerId, out string name))
            {
                return name;
            }
            return $"Player {playerId.Value}";
        }

        public void Update()
        {
            _netManager?.PollEvents();
        }

        public void Shutdown()
        {
            Disconnect();
            IsInitialized = false;
        }

        /// <summary>
        /// Set a custom player name (for display purposes).
        /// </summary>
        public void SetPlayerName(int peerId, string name)
        {
            _playerNames[peerId] = name;
        }

        /// <summary>
        /// Get the current port being used.
        /// </summary>
        public int GetPort() => _netManager?.LocalPort ?? DEFAULT_PORT;
    }
}
