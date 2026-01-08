using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using Antigravity.Core.Network;

namespace Antigravity.Core.Sync
{
    /// <summary>
    /// Manages partial synchronization of world state categories.
    /// Handles verification, detection of mismatches, and category-based resync.
    /// </summary>
    public static class PartialSyncManager
    {
        private const int SYNC_CHECK_INTERVAL_SECONDS = 60;
        private const int CHUNK_SIZE = 64 * 1024; // 64KB chunks

        private static float _lastSyncCheck = 0;
        private static WorldStateChecksums _lastHostChecksums;
        private static WorldStateChecksums _lastLocalChecksums;
        private static Dictionary<ulong, WorldSyncCategory> _clientMismatches = new();

        /// <summary>
        /// Event fired when sync mismatch is detected
        /// </summary>
        public static event Action<WorldSyncCategory> OnSyncMismatchDetected;

        /// <summary>
        /// Event fired when partial resync completes
        /// </summary>
        public static event Action<WorldSyncCategory> OnPartialResyncComplete;

        /// <summary>
        /// Initialize the partial sync manager
        /// </summary>
        public static void Initialize()
        {
            _lastSyncCheck = 0;
            _lastHostChecksums = null;
            _lastLocalChecksums = null;
            _clientMismatches.Clear();
            Debug.Log("[Antigravity] PartialSyncManager initialized.");
        }

        /// <summary>
        /// Called each game update to check if sync verification is needed
        /// </summary>
        public static void Update()
        {
            if (!SyncEngine.IsRunning) return;
            if (!NetworkManager.IsConnected) return;

            float now = Time.realtimeSinceStartup;
            if (now - _lastSyncCheck < SYNC_CHECK_INTERVAL_SECONDS) return;

            _lastSyncCheck = now;

            if (NetworkManager.IsHost)
            {
                PerformSyncCheck();
            }
        }

        #region Host Side

        /// <summary>
        /// Host performs sync check by calculating and broadcasting checksums
        /// </summary>
        public static void PerformSyncCheck()
        {
            if (!NetworkManager.IsHost) return;

            try
            {
                _lastHostChecksums = WorldStateHasher.CalculateAllChecksums();

                var message = new SyncCheckMessage
                {
                    GameTick = _lastHostChecksums.GameTick,
                    Timestamp = _lastHostChecksums.Timestamp,
                    PickupablesChecksum = _lastHostChecksums.PickupablesChecksum,
                    PickupablesCount = _lastHostChecksums.PickupablesCount,
                    BuildingsChecksum = _lastHostChecksums.BuildingsChecksum,
                    BuildingsCount = _lastHostChecksums.BuildingsCount,
                    DuplicantsChecksum = _lastHostChecksums.DuplicantsChecksum,
                    ConduitsChecksum = _lastHostChecksums.ConduitsChecksum,
                    GasesChecksum = _lastHostChecksums.GasesChecksum,
                    LiquidsChecksum = _lastHostChecksums.LiquidsChecksum
                };

                GameSession.SendToAllClients(MessageType.SyncCheck, message);
                Debug.Log($"[Antigravity] Sync check sent: Pickupables={_lastHostChecksums.PickupablesCount}, Buildings={_lastHostChecksums.BuildingsCount}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Error performing sync check: {ex.Message}");
            }
        }

        /// <summary>
        /// Host receives sync response from client
        /// </summary>
        public static void HandleSyncResponse(ulong clientId, SyncResponseMessage response)
        {
            if (!NetworkManager.IsHost) return;

            if (response.MismatchedCategories != WorldSyncCategory.All - WorldSyncCategory.All) // Has mismatches
            {
                _clientMismatches[clientId] = response.MismatchedCategories;
                Debug.LogWarning($"[Antigravity] Client {clientId} has sync mismatches: {response.MismatchedCategories}");

                // Send category data for each mismatched category
                foreach (WorldSyncCategory category in Enum.GetValues(typeof(WorldSyncCategory)))
                {
                    if (category == WorldSyncCategory.All) continue;
                    if ((response.MismatchedCategories & category) != 0)
                    {
                        SendCategoryData(clientId, category);
                    }
                }
            }
            else
            {
                _clientMismatches.Remove(clientId);
                Debug.Log($"[Antigravity] Client {clientId} is in sync.");
            }
        }

        /// <summary>
        /// Send category data to a specific client
        /// </summary>
        public static void SendCategoryData(ulong clientId, WorldSyncCategory category)
        {
            try
            {
                byte[] data = GetCategoryData(category);
                if (data == null || data.Length == 0) return;

                // Compress the data
                byte[] compressed = CompressData(data);

                if (compressed.Length <= CHUNK_SIZE)
                {
                    // Send in single message
                    var message = new SyncCategoryDataMessage
                    {
                        Category = category,
                        GameTick = GameClock.Instance?.GetCycle() ?? 0,
                        Data = compressed,
                        IsCompressed = true,
                        ChunkIndex = 0,
                        TotalChunks = 1
                    };

                    GameSession.SendToClient(clientId, MessageType.SyncCategoryData, message);
                }
                else
                {
                    // Send in chunks
                    int totalChunks = (compressed.Length + CHUNK_SIZE - 1) / CHUNK_SIZE;
                    
                    for (int i = 0; i < totalChunks; i++)
                    {
                        int offset = i * CHUNK_SIZE;
                        int length = Math.Min(CHUNK_SIZE, compressed.Length - offset);
                        byte[] chunk = new byte[length];
                        Array.Copy(compressed, offset, chunk, 0, length);

                        var chunkMessage = new SyncCategoryDataMessage
                        {
                            Category = category,
                            GameTick = GameClock.Instance?.GetCycle() ?? 0,
                            Data = chunk,
                            IsCompressed = true,
                            ChunkIndex = i,
                            TotalChunks = totalChunks
                        };

                        GameSession.SendToClient(clientId, MessageType.SyncCategoryChunk, chunkMessage);
                    }
                }

                Debug.Log($"[Antigravity] Sent {category} data to client {clientId}: {compressed.Length} bytes");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Error sending category {category} data: {ex.Message}");
            }
        }

        private static byte[] GetCategoryData(WorldSyncCategory category)
        {
            return category switch
            {
                WorldSyncCategory.Pickupables => WorldStateHasher.SerializePickupables(),
                WorldSyncCategory.Buildings => WorldStateHasher.SerializeBuildings(),
                WorldSyncCategory.Gases => WorldStateHasher.SerializeGasRegion(),
                WorldSyncCategory.Liquids => WorldStateHasher.SerializeLiquidRegion(),
                _ => null
            };
        }

        #endregion

        #region Client Side

        /// <summary>
        /// Client receives sync check from host
        /// </summary>
        public static void HandleSyncCheck(SyncCheckMessage hostChecksums)
        {
            if (NetworkManager.IsHost) return;

            try
            {
                // Calculate local checksums
                _lastLocalChecksums = WorldStateHasher.CalculateAllChecksums();

                // Convert message to WorldStateChecksums for comparison
                var hostState = new WorldStateChecksums
                {
                    GameTick = hostChecksums.GameTick,
                    Timestamp = hostChecksums.Timestamp,
                    PickupablesChecksum = hostChecksums.PickupablesChecksum,
                    PickupablesCount = hostChecksums.PickupablesCount,
                    BuildingsChecksum = hostChecksums.BuildingsChecksum,
                    BuildingsCount = hostChecksums.BuildingsCount,
                    DuplicantsChecksum = hostChecksums.DuplicantsChecksum,
                    ConduitsChecksum = hostChecksums.ConduitsChecksum,
                    GasesChecksum = hostChecksums.GasesChecksum,
                    LiquidsChecksum = hostChecksums.LiquidsChecksum
                };

                // Find mismatches
                WorldSyncCategory mismatched = _lastLocalChecksums.GetMismatchedCategories(hostState);

                // Send response
                var response = new SyncResponseMessage
                {
                    GameTick = _lastLocalChecksums.GameTick,
                    MismatchedCategories = mismatched
                };

                GameSession.SendToHost(MessageType.SyncResponse, response);

                if (mismatched != 0)
                {
                    Debug.LogWarning($"[Antigravity] Detected sync mismatch: {mismatched}");
                    OnSyncMismatchDetected?.Invoke(mismatched);
                    SyncEngine.ReportSyncError($"Category mismatch: {mismatched}");
                }
                else
                {
                    Debug.Log("[Antigravity] Sync check passed - all categories match.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Error handling sync check: {ex.Message}");
            }
        }

        /// <summary>
        /// Client receives category data for resync
        /// </summary>
        public static void HandleCategoryData(SyncCategoryDataMessage data)
        {
            if (NetworkManager.IsHost) return;

            try
            {
                byte[] decompressed = data.IsCompressed 
                    ? DecompressData(data.Data) 
                    : data.Data;

                ApplyCategoryData(data.Category, decompressed);

                Debug.Log($"[Antigravity] Applied {data.Category} resync data.");
                OnPartialResyncComplete?.Invoke(data.Category);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Error applying category {data.Category} data: {ex.Message}");
            }
        }

        private static void ApplyCategoryData(WorldSyncCategory category, byte[] data)
        {
            switch (category)
            {
                case WorldSyncCategory.Pickupables:
                    ApplyPickupablesData(Encoding.UTF8.GetString(data));
                    break;
                case WorldSyncCategory.Buildings:
                    ApplyBuildingsData(Encoding.UTF8.GetString(data));
                    break;
                case WorldSyncCategory.Gases:
                case WorldSyncCategory.Liquids:
                    WorldStateHasher.ApplyElementData(data);
                    break;
            }
        }

        private static void ApplyPickupablesData(string json)
        {
            // TODO: Implement pickupables resync
            // This would involve:
            // 1. Parsing the JSON
            // 2. Comparing with local pickupables
            // 3. Spawning missing items / removing extra items
            Debug.Log($"[Antigravity] Pickupables resync data received (not yet applied - {json.Length} bytes)");
        }

        private static void ApplyBuildingsData(string json)
        {
            // TODO: Implement buildings resync
            Debug.Log($"[Antigravity] Buildings resync data received (not yet applied - {json.Length} bytes)");
        }

        #endregion

        #region Compression Helpers

        private static byte[] CompressData(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Fastest))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] DecompressData(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            {
                gzip.CopyTo(output);
            }
            return output.ToArray();
        }

        #endregion
    }

    #region Network Messages

    /// <summary>
    /// Host sends this to all clients for sync verification
    /// </summary>
    [Serializable]
    public class SyncCheckMessage
    {
        public int GameTick;
        public long Timestamp;
        public long PickupablesChecksum;
        public int PickupablesCount;
        public long BuildingsChecksum;
        public int BuildingsCount;
        public long DuplicantsChecksum;
        public long ConduitsChecksum;
        public long GasesChecksum;
        public long LiquidsChecksum;
    }

    /// <summary>
    /// Client responds with mismatched categories
    /// </summary>
    [Serializable]
    public class SyncResponseMessage
    {
        public int GameTick;
        public WorldSyncCategory MismatchedCategories;
    }

    /// <summary>
    /// Category data for partial resync
    /// </summary>
    [Serializable]
    public class SyncCategoryDataMessage
    {
        public WorldSyncCategory Category;
        public int GameTick;
        public byte[] Data;
        public bool IsCompressed;
        public int ChunkIndex;
        public int TotalChunks;
    }

    #endregion
}
