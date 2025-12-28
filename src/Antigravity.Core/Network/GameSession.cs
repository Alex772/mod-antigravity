using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace Antigravity.Core.Network
{
    // Delegates for GameSession events
    public delegate void GameSessionEventHandler();
    public delegate void WorldDataReceivedHandler(byte[] worldData);

    /// <summary>
    /// Manages the multiplayer game session state.
    /// Coordinates game loading, sync, and player management.
    /// </summary>
    public static class GameSession
    {
        // Events
        public static event GameSessionEventHandler OnGameStarting;
        public static event WorldDataReceivedHandler OnWorldDataReceived;
        public static event GameSessionEventHandler OnAllPlayersReady;
        public static event GameSessionEventHandler OnGameStarted;

        // State
        public static bool IsInGame { get; private set; }
        public static bool IsLoading { get; private set; }
        public static bool IsWaitingForPlayers { get; private set; }
        
        // World data for transfer
        private static byte[] _pendingWorldData;
        private static readonly Dictionary<ulong, bool> _playerReadyStatus = new Dictionary<ulong, bool>();

        // Chunk transfer state
        private static List<byte[]> _receivedChunks = new List<byte[]>();
        private static int _expectedChunks = 0;
        private static int _totalDataSize = 0;

        /// <summary>
        /// Initialize the game session handlers.
        /// </summary>
        public static void Initialize()
        {
            SteamNetworkManager.OnDataReceived += OnNetworkDataReceived;
            SteamNetworkManager.OnPlayerLeft += OnPlayerDisconnected;
            
            Debug.Log("[Antigravity] GameSession initialized.");
        }

        /// <summary>
        /// Cleanup handlers.
        /// </summary>
        public static void Shutdown()
        {
            SteamNetworkManager.OnDataReceived -= OnNetworkDataReceived;
            SteamNetworkManager.OnPlayerLeft -= OnPlayerDisconnected;
            
            Reset();
        }

        /// <summary>
        /// Reset session state.
        /// </summary>
        public static void Reset()
        {
            IsInGame = false;
            IsLoading = false;
            IsWaitingForPlayers = false;
            _pendingWorldData = null;
            _playerReadyStatus.Clear();
            _receivedChunks.Clear();
            _expectedChunks = 0;
            _totalDataSize = 0;
        }

        /// <summary>
        /// [HOST] Start the game with the given world data.
        /// </summary>
        public static void HostStartGame(byte[] worldData, string colonyName, bool isLoadingSave)
        {
            if (!SteamNetworkManager.IsHost)
            {
                Debug.LogError("[Antigravity] Only host can start the game!");
                return;
            }

            Debug.Log($"[Antigravity] Host starting game. World data size: {worldData.Length} bytes");

            _pendingWorldData = worldData;
            IsLoading = true;
            IsWaitingForPlayers = true;

            // Initialize ready status for all players
            _playerReadyStatus.Clear();
            foreach (var player in SteamNetworkManager.ConnectedPlayers)
            {
                _playerReadyStatus[player.m_SteamID] = false;
            }

            // Compress the world data
            byte[] compressedData = MessageSerializer.Compress(worldData);
            Debug.Log($"[Antigravity] Compressed world data: {compressedData.Length} bytes ({(compressedData.Length * 100 / worldData.Length)}%)");

            // Send game starting notification
            var startingMsg = new GameStartingMessage
            {
                IsLoadingSave = isLoadingSave,
                ColonyName = colonyName,
                TotalDataSize = compressedData.Length,
                ChunkCount = CalculateChunkCount(compressedData.Length)
            };

            SendToAllClients(MessageType.GameStarting, startingMsg);

            // Send world data in chunks
            SendWorldDataChunks(compressedData);

            OnGameStarting?.Invoke();
        }

        /// <summary>
        /// [CLIENT] Signal that we're ready to play.
        /// </summary>
        public static void ClientReady()
        {
            if (SteamNetworkManager.IsHost) return;

            Debug.Log("[Antigravity] Client sending ready signal.");

            // Create a simple message without payload
            var msg = new NetworkMessage
            {
                Type = MessageType.PlayerReady,
                SenderSteamId = SteamNetworkManager.LocalSteamId.m_SteamID,
                Tick = 0,
                Payload = System.Array.Empty<byte>()
            };

            SendToHost(msg);
        }

        /// <summary>
        /// [HOST] Check if all players are ready and start the game.
        /// </summary>
        public static void CheckAllPlayersReady()
        {
            if (!SteamNetworkManager.IsHost || !IsWaitingForPlayers) return;

            bool allReady = true;
            foreach (var kvp in _playerReadyStatus)
            {
                if (!kvp.Value)
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady)
            {
                Debug.Log("[Antigravity] All players ready! Starting game...");
                IsWaitingForPlayers = false;
                OnAllPlayersReady?.Invoke();
                
                // Send game start signal
                var startMsg = new GameStartMessage
                {
                    StartTick = GetCurrentGameTick(),
                    GameSpeed = 1
                };

                SendToAllClients(MessageType.GameStart, startMsg);
                
                // Start locally too
                StartGame();
            }
        }

        /// <summary>
        /// Start the game (called on all clients after sync).
        /// </summary>
        private static void StartGame()
        {
            IsLoading = false;
            IsInGame = true;

            Debug.Log("[Antigravity] Game started!");
            OnGameStarted?.Invoke();
        }

        #region Network Message Handling

        private static void OnNetworkDataReceived(CSteamID sender, byte[] data)
        {
            var message = MessageSerializer.Deserialize(data);
            if (message == null) return;

            switch (message.Type)
            {
                case MessageType.GameStarting:
                    HandleGameStarting(message);
                    break;

                case MessageType.WorldDataChunk:
                    HandleWorldDataChunk(message);
                    break;

                case MessageType.PlayerReady:
                    HandlePlayerReady(sender);
                    break;

                case MessageType.GameStart:
                    HandleGameStart(message);
                    break;

                case MessageType.CursorUpdate:
                    HandleCursorUpdate(message);
                    break;

                default:
                    // Other message types handled elsewhere
                    break;
            }
        }

        private static void HandleGameStarting(NetworkMessage message)
        {
            if (SteamNetworkManager.IsHost) return;

            var data = MessageSerializer.DeserializePayload<GameStartingMessage>(message.Payload);
            if (data == null) return;

            Debug.Log($"[Antigravity] Game starting! Colony: {data.ColonyName}, Size: {data.TotalDataSize} bytes, Chunks: {data.ChunkCount}");

            IsLoading = true;
            _expectedChunks = data.ChunkCount;
            _totalDataSize = data.TotalDataSize;
            _receivedChunks.Clear();

            OnGameStarting?.Invoke();
        }

        private static void HandleWorldDataChunk(NetworkMessage message)
        {
            if (SteamNetworkManager.IsHost) return;

            var chunk = MessageSerializer.DeserializePayload<WorldDataChunk>(message.Payload);
            if (chunk == null) return;

            Debug.Log($"[Antigravity] Received chunk {chunk.ChunkIndex + 1}/{chunk.TotalChunks}");

            // Store chunk at correct index
            while (_receivedChunks.Count <= chunk.ChunkIndex)
            {
                _receivedChunks.Add(null);
            }
            _receivedChunks[chunk.ChunkIndex] = chunk.Data;

            // Check if all chunks received
            if (_receivedChunks.Count == _expectedChunks && !_receivedChunks.Contains(null))
            {
                AssembleWorldData();
            }
        }

        private static void AssembleWorldData()
        {
            Debug.Log("[Antigravity] All chunks received, assembling world data...");

            // Combine all chunks
            int totalSize = 0;
            foreach (var chunk in _receivedChunks)
            {
                totalSize += chunk.Length;
            }

            byte[] compressedData = new byte[totalSize];
            int offset = 0;
            foreach (var chunk in _receivedChunks)
            {
                Buffer.BlockCopy(chunk, 0, compressedData, offset, chunk.Length);
                offset += chunk.Length;
            }

            // Decompress
            byte[] worldData = MessageSerializer.Decompress(compressedData);
            if (worldData == null)
            {
                Debug.LogError("[Antigravity] Failed to decompress world data!");
                return;
            }

            Debug.Log($"[Antigravity] World data assembled: {worldData.Length} bytes");

            _receivedChunks.Clear();
            
            OnWorldDataReceived?.Invoke(worldData);
        }

        private static void HandlePlayerReady(CSteamID sender)
        {
            if (!SteamNetworkManager.IsHost) return;

            Debug.Log($"[Antigravity] Player ready: {SteamNetworkManager.GetPlayerName(sender)}");

            _playerReadyStatus[sender.m_SteamID] = true;
            CheckAllPlayersReady();
        }

        private static void HandleGameStart(NetworkMessage message)
        {
            if (SteamNetworkManager.IsHost) return;

            var data = MessageSerializer.DeserializePayload<GameStartMessage>(message.Payload);
            if (data == null) return;

            Debug.Log($"[Antigravity] Game start received! Tick: {data.StartTick}");

            StartGame();
        }

        private static void HandleCursorUpdate(NetworkMessage message)
        {
            var cursorData = MessageSerializer.DeserializePayload<CursorPositionMessage>(message.Payload);
            if (cursorData == null) return;

            // Forward to RemoteCursorManager (in client assembly)
            // Using reflection since we can't directly reference the client assembly from core
            try
            {
                var managerType = System.Type.GetType("Antigravity.Client.RemoteCursorManager, Antigravity.Client");
                if (managerType != null)
                {
                    var instanceProperty = managerType.GetProperty("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    var instance = instanceProperty?.GetValue(null);
                    if (instance != null)
                    {
                        var method = managerType.GetMethod("OnCursorReceived");
                        method?.Invoke(instance, new object[] { cursorData });
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Antigravity] Cursor update handling failed: {ex.Message}");
            }
        }

        private static void OnPlayerDisconnected(CSteamID player)
        {
            _playerReadyStatus.Remove(player.m_SteamID);

            if (IsWaitingForPlayers && SteamNetworkManager.IsHost)
            {
                CheckAllPlayersReady();
            }
        }

        #endregion

        #region Helper Methods

        private static void SendToAllClients<T>(MessageType type, T payload)
        {
            var message = MessageSerializer.CreateMessage(
                type, 
                payload,
                SteamNetworkManager.LocalSteamId.m_SteamID
            );

            byte[] data = MessageSerializer.Serialize(message);
            SteamNetworkManager.SendToAll(data);
        }

        private static void SendToHost(NetworkMessage message)
        {
            byte[] data = MessageSerializer.Serialize(message);
            SteamNetworkManager.SendTo(SteamNetworkManager.HostSteamId, data);
        }

        private const int CHUNK_SIZE = 64 * 1024; // 64 KB chunks

        private static int CalculateChunkCount(int totalSize)
        {
            return (totalSize + CHUNK_SIZE - 1) / CHUNK_SIZE;
        }

        private static void SendWorldDataChunks(byte[] compressedData)
        {
            int chunkCount = CalculateChunkCount(compressedData.Length);
            
            Debug.Log($"[Antigravity] Sending world data in {chunkCount} chunks...");

            for (int i = 0; i < chunkCount; i++)
            {
                int offset = i * CHUNK_SIZE;
                int size = Math.Min(CHUNK_SIZE, compressedData.Length - offset);
                
                byte[] chunkData = new byte[size];
                Buffer.BlockCopy(compressedData, offset, chunkData, 0, size);

                var chunk = new WorldDataChunk
                {
                    ChunkIndex = i,
                    TotalChunks = chunkCount,
                    Data = chunkData
                };

                var message = MessageSerializer.CreateMessage(
                    MessageType.WorldDataChunk,
                    chunk,
                    SteamNetworkManager.LocalSteamId.m_SteamID
                );

                byte[] data = MessageSerializer.Serialize(message);
                SteamNetworkManager.SendToAll(data);

                Debug.Log($"[Antigravity] Sent chunk {i + 1}/{chunkCount} ({size} bytes)");
            }
        }

        private static long GetCurrentGameTick()
        {
            // Try to get the current game tick from ONI
            try
            {
                if (GameClock.Instance != null)
                {
                    return (long)GameClock.Instance.GetTime();
                }
            }
            catch { }

            return 0;
        }

        #endregion
    }
}
