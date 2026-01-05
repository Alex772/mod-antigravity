using System;
using System.Collections.Generic;

namespace Antigravity.Core.Network
{
    /// <summary>
    /// Unified player identifier that works across Steam and LiteNetLib backends.
    /// </summary>
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        /// <summary>
        /// The raw ID value. For Steam, this is the CSteamID. For LiteNetLib, this is the peer ID.
        /// </summary>
        public readonly ulong Value;

        /// <summary>
        /// The backend type this ID belongs to.
        /// </summary>
        public readonly NetworkBackendType BackendType;

        public PlayerId(ulong value, NetworkBackendType backendType)
        {
            Value = value;
            BackendType = backendType;
        }

        /// <summary>
        /// Create from Steam ID.
        /// </summary>
        public static PlayerId FromSteam(ulong steamId) => new PlayerId(steamId, NetworkBackendType.Steam);

        /// <summary>
        /// Create from LiteNetLib peer ID.
        /// </summary>
        public static PlayerId FromLiteNetLib(int peerId) => new PlayerId((ulong)peerId, NetworkBackendType.LiteNetLib);

        /// <summary>
        /// Get as Steam CSteamID.
        /// </summary>
        public Steamworks.CSteamID AsSteamId => new Steamworks.CSteamID(Value);

        /// <summary>
        /// Get as LiteNetLib peer ID.
        /// </summary>
        public int AsPeerId => (int)Value;

        public bool IsValid => Value != 0;

        public static PlayerId Invalid => new PlayerId(0, NetworkBackendType.Steam);

        public bool Equals(PlayerId other) => Value == other.Value && BackendType == other.BackendType;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"{BackendType}:{Value}";

        public static bool operator ==(PlayerId left, PlayerId right) => left.Equals(right);
        public static bool operator !=(PlayerId left, PlayerId right) => !left.Equals(right);
    }

    /// <summary>
    /// Type of network backend being used.
    /// </summary>
    public enum NetworkBackendType
    {
        Steam,
        LiteNetLib
    }

    /// <summary>
    /// Reliability level for sending messages.
    /// </summary>
    public enum SendReliability
    {
        Reliable,
        Unreliable
    }

    /// <summary>
    /// Event args for player events.
    /// </summary>
    public class PlayerEventArgs : EventArgs
    {
        public PlayerId PlayerId { get; }
        public string PlayerName { get; }

        public PlayerEventArgs(PlayerId playerId, string playerName = null)
        {
            PlayerId = playerId;
            PlayerName = playerName;
        }
    }

    /// <summary>
    /// Event args for data received events.
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        public PlayerId Sender { get; }
        public byte[] Data { get; }

        public DataReceivedEventArgs(PlayerId sender, byte[] data)
        {
            Sender = sender;
            Data = data;
        }
    }

    /// <summary>
    /// Unified interface for network backends (Steam P2P or LiteNetLib).
    /// </summary>
    public interface INetworkBackend
    {
        /// <summary>
        /// Type of this backend.
        /// </summary>
        NetworkBackendType BackendType { get; }

        /// <summary>
        /// Whether the backend is initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Whether we are the host.
        /// </summary>
        bool IsHost { get; }

        /// <summary>
        /// Whether we are connected to a session.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Local player ID.
        /// </summary>
        PlayerId LocalPlayerId { get; }

        /// <summary>
        /// Host player ID.
        /// </summary>
        PlayerId HostPlayerId { get; }

        /// <summary>
        /// Connected players (excluding self).
        /// </summary>
        IReadOnlyList<PlayerId> ConnectedPlayers { get; }

        /// <summary>
        /// Events.
        /// </summary>
        event EventHandler OnConnected;
        event EventHandler OnDisconnected;
        event EventHandler<PlayerEventArgs> OnPlayerJoined;
        event EventHandler<PlayerEventArgs> OnPlayerLeft;
        event EventHandler<DataReceivedEventArgs> OnDataReceived;

        /// <summary>
        /// Initialize the backend.
        /// </summary>
        bool Initialize();

        /// <summary>
        /// Host a new multiplayer session.
        /// </summary>
        /// <param name="maxPlayers">Maximum players allowed.</param>
        /// <returns>Connection info for others to join (lobby ID or port).</returns>
        string HostGame(int maxPlayers = 4);

        /// <summary>
        /// Join an existing session.
        /// </summary>
        /// <param name="connectionInfo">Steam lobby ID or IP:port.</param>
        void JoinGame(string connectionInfo);

        /// <summary>
        /// Disconnect from the current session.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Send data to all connected players.
        /// </summary>
        bool SendToAll(byte[] data, SendReliability reliability = SendReliability.Reliable);

        /// <summary>
        /// Send data to all connected players except the specified one.
        /// </summary>
        bool SendToAllExcept(PlayerId except, byte[] data, SendReliability reliability = SendReliability.Reliable);

        /// <summary>
        /// Send data to a specific player.
        /// </summary>
        bool SendTo(PlayerId target, byte[] data, SendReliability reliability = SendReliability.Reliable);

        /// <summary>
        /// Get player display name.
        /// </summary>
        string GetPlayerName(PlayerId playerId);

        /// <summary>
        /// Process network events. Call every frame.
        /// </summary>
        void Update();

        /// <summary>
        /// Cleanup and shutdown.
        /// </summary>
        void Shutdown();
    }
}
