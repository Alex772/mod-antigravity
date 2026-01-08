using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace Antigravity.Core.Network
{
    // Custom delegates for Steam events
    public delegate void SteamEventHandler();
    public delegate void SteamPlayerEventHandler(CSteamID playerId);
    public delegate void SteamDataEventHandler(CSteamID sender, byte[] data);

    /// <summary>
    /// Steam P2P networking manager for multiplayer connections.
    /// Uses Steam's relay servers for NAT traversal (no IP needed).
    /// </summary>
    public static class SteamNetworkManager
    {
        // Events
        public static event SteamEventHandler OnConnected;
        public static event SteamEventHandler OnDisconnected;
        public static event SteamPlayerEventHandler OnPlayerJoined;
        public static event SteamPlayerEventHandler OnPlayerLeft;
        public static event SteamDataEventHandler OnDataReceived;
        public static event System.Action<CSteamID> OnJoinInviteReceived;

        // State
        public static bool IsInitialized { get; private set; }
        public static bool IsHost { get; private set; }
        public static bool IsConnected { get; private set; }
        public static CSteamID HostSteamId { get; private set; }
        public static CSteamID LocalSteamId { get; private set; }

        // Connected players (excluding host)
        private static readonly List<CSteamID> _connectedPlayers = new List<CSteamID>();
        public static IReadOnlyList<CSteamID> ConnectedPlayers => _connectedPlayers;

        // Lobby
        private static CSteamID _currentLobby;
        private static Callback<LobbyCreated_t> _lobbyCreatedCallback;
        private static Callback<LobbyEnter_t> _lobbyEnteredCallback;
        private static Callback<GameLobbyJoinRequested_t> _lobbyJoinRequestedCallback;
        private static Callback<LobbyChatUpdate_t> _lobbyChatUpdateCallback;
        private static Callback<P2PSessionRequest_t> _p2pSessionRequestCallback;
        private static bool _joinCallbackRegistered = false;

        /// <summary>
        /// Register the Steam lobby join callback early so invites work even before opening multiplayer menu.
        /// Call this from mod initialization.
        /// </summary>
        public static void EnsureJoinCallbackRegistered()
        {
            if (_joinCallbackRegistered) return;
            
            try
            {
                if (!SteamManager.Initialized)
                {
                    Debug.Log("[Antigravity] Steam not ready yet, will register join callback later.");
                    return;
                }

                LocalSteamId = SteamUser.GetSteamID();
                _lobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
                _joinCallbackRegistered = true;
                Debug.Log("[Antigravity] Steam join invite callback registered early.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Antigravity] Failed to register early join callback: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize Steam networking.
        /// </summary>
        public static bool Initialize()
        {
            if (IsInitialized) return true;

            try
            {
                if (!SteamManager.Initialized)
                {
                    Debug.LogError("[Antigravity] Steam is not initialized!");
                    return false;
                }

                LocalSteamId = SteamUser.GetSteamID();
                Debug.Log($"[Antigravity] Steam user: {SteamFriends.GetPersonaName()} ({LocalSteamId})");

                // Setup callbacks
                _lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
                _lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
                _lobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
                _lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
                _p2pSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);

                IsInitialized = true;
                Debug.Log("[Antigravity] Steam networking initialized.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to initialize Steam networking: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Host a new multiplayer session via Steam lobby.
        /// </summary>
        public static void HostGame(int maxPlayers = 4)
        {
            if (!IsInitialized) Initialize();

            Debug.Log("[Antigravity] Creating Steam lobby...");
            
            // Create a friends-only lobby (can be changed to public)
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
        }

        /// <summary>
        /// Join a game using Steam Friend's lobby.
        /// Called when player clicks "Join Game" on a friend's game.
        /// </summary>
        public static void JoinLobby(CSteamID lobbyId)
        {
            if (!IsInitialized) Initialize();

            Debug.Log($"[Antigravity] Joining lobby: {lobbyId}");
            SteamMatchmaking.JoinLobby(lobbyId);
        }

        /// <summary>
        /// Join a game using the lobby code.
        /// </summary>
        public static void JoinByCode(string code)
        {
            if (!IsInitialized) Initialize();

            // Try to parse the code as a lobby ID
            if (ulong.TryParse(code, out ulong lobbyIdValue))
            {
                JoinLobby(new CSteamID(lobbyIdValue));
            }
            else
            {
                Debug.LogError($"[Antigravity] Invalid lobby code: {code}");
            }
        }

        /// <summary>
        /// Leave the current session.
        /// </summary>
        public static void Disconnect()
        {
            if (_currentLobby.IsValid())
            {
                SteamMatchmaking.LeaveLobby(_currentLobby);
                _currentLobby = CSteamID.Nil;
            }

            // Close P2P sessions
            foreach (var player in _connectedPlayers)
            {
                SteamNetworking.CloseP2PSessionWithUser(player);
            }
            _connectedPlayers.Clear();

            IsHost = false;
            IsConnected = false;
            HostSteamId = CSteamID.Nil;

            OnDisconnected?.Invoke();
            Debug.Log("[Antigravity] Disconnected from session.");
        }

        /// <summary>
        /// Send data to all connected players.
        /// </summary>
        public static bool SendToAll(byte[] data, EP2PSend sendType = EP2PSend.k_EP2PSendReliable)
        {
            bool allSuccess = true;
            foreach (var player in _connectedPlayers)
            {
                if (!SendTo(player, data, sendType))
                    allSuccess = false;
            }
            return allSuccess;
        }

        /// <summary>
        /// Send data to a specific player.
        /// </summary>
        public static bool SendTo(CSteamID target, byte[] data, EP2PSend sendType = EP2PSend.k_EP2PSendReliable)
        {
            return SteamNetworking.SendP2PPacket(target, data, (uint)data.Length, sendType);
        }

        /// <summary>
        /// Poll for incoming messages. Call this from Update().
        /// </summary>
        public static void Update()
        {
            if (!IsInitialized || !IsConnected) return;

            uint messageSize;
            while (SteamNetworking.IsP2PPacketAvailable(out messageSize))
            {
                byte[] buffer = new byte[messageSize];
                CSteamID sender;
                
                if (SteamNetworking.ReadP2PPacket(buffer, messageSize, out uint bytesRead, out sender))
                {
                    OnDataReceived?.Invoke(sender, buffer);
                }
            }
        }

        /// <summary>
        /// Get the lobby code for sharing with friends.
        /// </summary>
        public static string GetLobbyCode()
        {
            if (_currentLobby.IsValid())
            {
                return _currentLobby.m_SteamID.ToString();
            }
            return "";
        }

        /// <summary>
        /// Get Steam friend's display name.
        /// </summary>
        public static string GetPlayerName(CSteamID steamId)
        {
            return SteamFriends.GetFriendPersonaName(steamId);
        }

        #region Steam Callbacks

        private static void OnLobbyCreated(LobbyCreated_t result)
        {
            if (result.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogError($"[Antigravity] Failed to create lobby: {result.m_eResult}");
                return;
            }

            _currentLobby = new CSteamID(result.m_ulSteamIDLobby);
            HostSteamId = LocalSteamId;
            IsHost = true;
            IsConnected = true;

            // Set lobby data
            SteamMatchmaking.SetLobbyData(_currentLobby, "game", "Antigravity");
            SteamMatchmaking.SetLobbyData(_currentLobby, "host", SteamFriends.GetPersonaName());

            Debug.Log($"[Antigravity] Lobby created! Code: {_currentLobby.m_SteamID}");
            
            OnConnected?.Invoke();
        }

        private static void OnLobbyEntered(LobbyEnter_t result)
        {
            _currentLobby = new CSteamID(result.m_ulSteamIDLobby);
            
            // Get host Steam ID
            HostSteamId = SteamMatchmaking.GetLobbyOwner(_currentLobby);
            IsHost = (HostSteamId == LocalSteamId);
            IsConnected = true;

            if (!IsHost)
            {
                // Connect to host via P2P
                Debug.Log($"[Antigravity] Connecting to host: {GetPlayerName(HostSteamId)}");
                
                // Add host to connected players
                if (!_connectedPlayers.Contains(HostSteamId))
                {
                    _connectedPlayers.Add(HostSteamId);
                }

                // Send a "hello" packet to initiate P2P connection
                byte[] hello = System.Text.Encoding.UTF8.GetBytes("HELLO");
                SendTo(HostSteamId, hello);
            }

            Debug.Log($"[Antigravity] Joined lobby! Host: {GetPlayerName(HostSteamId)}, IsHost: {IsHost}");
            OnConnected?.Invoke();
        }

        private static void OnLobbyJoinRequested(GameLobbyJoinRequested_t result)
        {
            Debug.Log($"[Antigravity] Join request from Steam overlay for lobby: {result.m_steamIDLobby}");
            
            // Fire event to let UI layer show lobby screen
            OnJoinInviteReceived?.Invoke(result.m_steamIDLobby);
            
            // Join the lobby
            JoinLobby(result.m_steamIDLobby);
        }

        private static void OnLobbyChatUpdate(LobbyChatUpdate_t result)
        {
            var changedUser = new CSteamID(result.m_ulSteamIDUserChanged);
            var changeType = (EChatMemberStateChange)result.m_rgfChatMemberStateChange;

            if (changeType == EChatMemberStateChange.k_EChatMemberStateChangeEntered)
            {
                if (changedUser != LocalSteamId && !_connectedPlayers.Contains(changedUser))
                {
                    _connectedPlayers.Add(changedUser);
                    Debug.Log($"[Antigravity] Player joined: {GetPlayerName(changedUser)}");
                    OnPlayerJoined?.Invoke(changedUser);
                }
            }
            else if (changeType == EChatMemberStateChange.k_EChatMemberStateChangeLeft ||
                     changeType == EChatMemberStateChange.k_EChatMemberStateChangeDisconnected)
            {
                if (_connectedPlayers.Contains(changedUser))
                {
                    _connectedPlayers.Remove(changedUser);
                    SteamNetworking.CloseP2PSessionWithUser(changedUser);
                    Debug.Log($"[Antigravity] Player left: {GetPlayerName(changedUser)}");
                    OnPlayerLeft?.Invoke(changedUser);
                }
            }
        }

        private static void OnP2PSessionRequest(P2PSessionRequest_t result)
        {
            var sender = result.m_steamIDRemote;
            
            // Accept P2P connection from lobby members
            if (_currentLobby.IsValid())
            {
                SteamNetworking.AcceptP2PSessionWithUser(sender);
                
                if (!_connectedPlayers.Contains(sender))
                {
                    _connectedPlayers.Add(sender);
                    OnPlayerJoined?.Invoke(sender);
                }
                
                Debug.Log($"[Antigravity] Accepted P2P session with: {GetPlayerName(sender)}");
            }
        }

        #endregion
    }
}
