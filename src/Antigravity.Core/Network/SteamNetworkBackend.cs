using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace Antigravity.Core.Network
{
    /// <summary>
    /// Steam P2P implementation of INetworkBackend.
    /// Wraps existing SteamNetworkManager functionality.
    /// </summary>
    public class SteamNetworkBackend : INetworkBackend
    {
        public NetworkBackendType BackendType => NetworkBackendType.Steam;
        public bool IsInitialized => SteamNetworkManager.IsInitialized;
        public bool IsHost => SteamNetworkManager.IsHost;
        public bool IsConnected => SteamNetworkManager.IsConnected;
        
        public PlayerId LocalPlayerId => PlayerId.FromSteam(SteamNetworkManager.LocalSteamId.m_SteamID);
        public PlayerId HostPlayerId => PlayerId.FromSteam(SteamNetworkManager.HostSteamId.m_SteamID);

        private readonly List<PlayerId> _connectedPlayers = new List<PlayerId>();
        public IReadOnlyList<PlayerId> ConnectedPlayers => _connectedPlayers;

        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;
        public event EventHandler<PlayerEventArgs> OnPlayerJoined;
        public event EventHandler<PlayerEventArgs> OnPlayerLeft;
        public event EventHandler<DataReceivedEventArgs> OnDataReceived;

        private bool _eventsSubscribed = false;

        public bool Initialize()
        {
            if (!SteamNetworkManager.Initialize())
                return false;

            SubscribeToEvents();
            return true;
        }

        public string HostGame(int maxPlayers = 4)
        {
            SteamNetworkManager.HostGame(maxPlayers);
            // Code is returned async via OnLobbyCreated callback
            return "pending";
        }

        public void JoinGame(string connectionInfo)
        {
            SteamNetworkManager.JoinByCode(connectionInfo);
        }

        public void Disconnect()
        {
            SteamNetworkManager.Disconnect();
            _connectedPlayers.Clear();
        }

        public bool SendToAll(byte[] data, SendReliability reliability = SendReliability.Reliable)
        {
            var sendType = reliability == SendReliability.Reliable 
                ? EP2PSend.k_EP2PSendReliable 
                : EP2PSend.k_EP2PSendUnreliable;
            return SteamNetworkManager.SendToAll(data, sendType);
        }

        public bool SendTo(PlayerId target, byte[] data, SendReliability reliability = SendReliability.Reliable)
        {
            var sendType = reliability == SendReliability.Reliable 
                ? EP2PSend.k_EP2PSendReliable 
                : EP2PSend.k_EP2PSendUnreliable;
            return SteamNetworkManager.SendTo(target.AsSteamId, data, sendType);
        }

        public string GetPlayerName(PlayerId playerId)
        {
            return SteamNetworkManager.GetPlayerName(playerId.AsSteamId);
        }

        public void Update()
        {
            SteamNetworkManager.Update();
        }

        public void Shutdown()
        {
            UnsubscribeFromEvents();
            Disconnect();
        }

        private void SubscribeToEvents()
        {
            if (_eventsSubscribed) return;
            
            SteamNetworkManager.OnConnected += HandleConnected;
            SteamNetworkManager.OnDisconnected += HandleDisconnected;
            SteamNetworkManager.OnPlayerJoined += HandlePlayerJoined;
            SteamNetworkManager.OnPlayerLeft += HandlePlayerLeft;
            SteamNetworkManager.OnDataReceived += HandleDataReceived;
            
            _eventsSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_eventsSubscribed) return;
            
            SteamNetworkManager.OnConnected -= HandleConnected;
            SteamNetworkManager.OnDisconnected -= HandleDisconnected;
            SteamNetworkManager.OnPlayerJoined -= HandlePlayerJoined;
            SteamNetworkManager.OnPlayerLeft -= HandlePlayerLeft;
            SteamNetworkManager.OnDataReceived -= HandleDataReceived;
            
            _eventsSubscribed = false;
        }

        private void HandleConnected()
        {
            OnConnected?.Invoke(this, EventArgs.Empty);
        }

        private void HandleDisconnected()
        {
            _connectedPlayers.Clear();
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }

        private void HandlePlayerJoined(CSteamID steamId)
        {
            var playerId = PlayerId.FromSteam(steamId.m_SteamID);
            if (!_connectedPlayers.Contains(playerId))
            {
                _connectedPlayers.Add(playerId);
            }
            OnPlayerJoined?.Invoke(this, new PlayerEventArgs(playerId, GetPlayerName(playerId)));
        }

        private void HandlePlayerLeft(CSteamID steamId)
        {
            var playerId = PlayerId.FromSteam(steamId.m_SteamID);
            _connectedPlayers.Remove(playerId);
            OnPlayerLeft?.Invoke(this, new PlayerEventArgs(playerId));
        }

        private void HandleDataReceived(CSteamID sender, byte[] data)
        {
            var playerId = PlayerId.FromSteam(sender.m_SteamID);
            OnDataReceived?.Invoke(this, new DataReceivedEventArgs(playerId, data));
        }
    }
}
