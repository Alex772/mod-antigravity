using System;

namespace Antigravity.Core.Network
{
    /// <summary>
    /// Types of network messages exchanged between host and clients.
    /// </summary>
    public enum MessageType : byte
    {
        // Lobby messages
        LobbyUpdate = 1,
        
        // Game flow messages
        GameStarting = 10,      // Host is starting the game
        WorldData = 11,         // World data (save bytes)
        WorldDataChunk = 12,    // Chunk of world data (for large saves)
        PlayerReady = 13,       // Client is ready to play
        GameStart = 14,         // All ready, start playing
        
        // Sync messages
        SyncCheck = 20,         // Periodic sync verification (host sends checksums)
        SyncResponse = 21,      // Client's sync checksum response
        SyncRequest = 22,       // Request full sync
        SyncCategoryData = 23,  // Partial sync data for specific category
        SyncCategoryChunk = 24, // Chunk of category sync data (for large data)
        
        // Command messages
        Command = 30,           // Player command (dig, build, etc)
        CommandBroadcast = 31,  // Validated command for all to execute
        
        // Game state messages
        Pause = 40,
        Unpause = 41,
        SpeedChange = 42,
        
        // Social messages
        Chat = 50,
        CursorUpdate = 51,
        
        // System messages
        Ping = 60,
        Pong = 61,
        Error = 62,
        Disconnect = 63,
        Heartbeat = 64,             // Periodic heartbeat from host
        HeartbeatAck = 65,          // Client acknowledges heartbeat
        
        // Reconnection and sync
        PlayerJoined = 70,          // New player joined mid-game
        PlayerLeft = 71,            // Player left the game
        SyncPauseRequest = 72,      // Request all clients to pause for sync
        SyncResumeRequest = 73,     // Resume after sync complete
        ResyncWorldData = 74,       // Full world resync for reconnecting player
        
        // System notifications
        SystemMessage = 80          // System notification (desync, errors, etc)
    }

    /// <summary>
    /// Network message wrapper for all multiplayer communication.
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        /// <summary>
        /// Type of message.
        /// </summary>
        public MessageType Type { get; set; }

        /// <summary>
        /// Steam ID of the sender.
        /// </summary>
        public ulong SenderSteamId { get; set; }

        /// <summary>
        /// Game tick when message was created (for sync).
        /// </summary>
        public long Tick { get; set; }

        /// <summary>
        /// Serialized payload data.
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Create an empty message.
        /// </summary>
        public NetworkMessage()
        {
            Payload = Array.Empty<byte>();
        }

        /// <summary>
        /// Create a message with type and optional payload.
        /// </summary>
        public NetworkMessage(MessageType type, byte[] payload = null)
        {
            Type = type;
            Payload = payload ?? Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Message sent when host starts the game.
    /// </summary>
    [Serializable]
    public class GameStartingMessage
    {
        /// <summary>
        /// True if loading existing save, false if new game.
        /// </summary>
        public bool IsLoadingSave { get; set; }

        /// <summary>
        /// Name of the colony.
        /// </summary>
        public string ColonyName { get; set; }

        /// <summary>
        /// Total size of world data in bytes.
        /// </summary>
        public int TotalDataSize { get; set; }

        /// <summary>
        /// Number of chunks the data will be sent in.
        /// </summary>
        public int ChunkCount { get; set; }
        
        /// <summary>
        /// True if this is a periodic hard sync (not initial game start).
        /// Clients should reload world without returning to menu.
        /// </summary>
        public bool IsHardSync { get; set; }
    }

    /// <summary>
    /// A chunk of world data.
    /// </summary>
    [Serializable]
    public class WorldDataChunk
    {
        /// <summary>
        /// Index of this chunk (0-based).
        /// </summary>
        public int ChunkIndex { get; set; }

        /// <summary>
        /// Total number of chunks.
        /// </summary>
        public int TotalChunks { get; set; }

        /// <summary>
        /// The chunk data.
        /// </summary>
        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Message sent when all players should start the game.
    /// </summary>
    [Serializable]
    public class GameStartMessage
    {
        /// <summary>
        /// Game tick to start at.
        /// </summary>
        public long StartTick { get; set; }

        /// <summary>
        /// Game speed to use.
        /// </summary>
        public int GameSpeed { get; set; }
    }

    /// <summary>
    /// Sync check message for verifying world state.
    /// </summary>
    [Serializable]
    public class SyncCheckMessage
    {
        /// <summary>
        /// Current game tick.
        /// </summary>
        public long Tick { get; set; }

        /// <summary>
        /// Checksum of the world state.
        /// </summary>
        public long Checksum { get; set; }
    }

    /// <summary>
    /// Chat message.
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        /// <summary>
        /// Sender's display name.
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// The chat text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Timestamp of the message.
        /// </summary>
        public long Timestamp { get; set; }
        
        /// <summary>
        /// Type of message (player or system).
        /// </summary>
        public ChatMessageType MessageType { get; set; }
        
        /// <summary>
        /// Level for system messages (info, warning, error).
        /// </summary>
        public SystemMessageLevel SystemLevel { get; set; }
    }
    
    public enum ChatMessageType : byte
    {
        Player = 0,
        System = 1
    }
    
    public enum SystemMessageLevel : byte
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>
    /// Cursor position message for showing other player's cursor.
    /// </summary>
    [Serializable]
    public class CursorPositionMessage
    {
        /// <summary>
        /// Steam ID of the player.
        /// </summary>
        public ulong SteamId { get; set; }

        /// <summary>
        /// Player display name.
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// World X position.
        /// </summary>
        public float WorldX { get; set; }

        /// <summary>
        /// World Y position.
        /// </summary>
        public float WorldY { get; set; }

        /// <summary>
        /// Cell ID (grid cell position).
        /// </summary>
        public int CellId { get; set; }
    }
}
