using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Antigravity.Core.Network
{
    /// <summary>
    /// Handles serialization and deserialization of network messages.
    /// Uses JSON for structured data and GZip for compression.
    /// </summary>
    public static class MessageSerializer
    {
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// Serialize a NetworkMessage to bytes for sending.
        /// </summary>
        public static byte[] Serialize(NetworkMessage message)
        {
            try
            {
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    // Header
                    writer.Write((byte)message.Type);
                    writer.Write(message.SenderSteamId);
                    writer.Write(message.Tick);
                    
                    // Payload length and data
                    writer.Write(message.Payload?.Length ?? 0);
                    if (message.Payload != null && message.Payload.Length > 0)
                    {
                        writer.Write(message.Payload);
                    }

                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to serialize message: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserialize bytes into a NetworkMessage.
        /// Returns null if data is invalid or not in expected format.
        /// </summary>
        public static NetworkMessage Deserialize(byte[] data)
        {
            // Minimum message size: 1 (type) + 8 (steamId) + 8 (tick) + 4 (payloadLength) = 21 bytes
            const int MinMessageSize = 21;
            
            if (data == null || data.Length < MinMessageSize)
            {
                // Data too small, likely not our message format - silently ignore
                return null;
            }

            try
            {
                using (var ms = new MemoryStream(data))
                using (var reader = new BinaryReader(ms))
                {
                    byte typeValue = reader.ReadByte();
                    
                    // Validate message type is in valid range
                    if (!Enum.IsDefined(typeof(MessageType), typeValue))
                    {
                        // Unknown message type - not our format
                        return null;
                    }

                    var message = new NetworkMessage
                    {
                        Type = (MessageType)typeValue,
                        SenderSteamId = reader.ReadUInt64(),
                        Tick = reader.ReadInt64()
                    };

                    int payloadLength = reader.ReadInt32();
                    
                    // Sanity check payload length
                    if (payloadLength < 0 || payloadLength > 50 * 1024 * 1024) // Max 50MB
                    {
                        Debug.LogWarning($"[Antigravity] Invalid payload length: {payloadLength}");
                        return null;
                    }

                    if (payloadLength > 0)
                    {
                        // Check if there's enough data remaining
                        long remaining = ms.Length - ms.Position;
                        if (remaining < payloadLength)
                        {
                            Debug.LogWarning($"[Antigravity] Not enough data for payload: need {payloadLength}, have {remaining}");
                            return null;
                        }
                        message.Payload = reader.ReadBytes(payloadLength);
                    }

                    return message;
                }
            }
            catch (Exception ex)
            {
                // Only log if it's an actual error, not just invalid format
                if (data.Length >= MinMessageSize)
                {
                    Debug.LogWarning($"[Antigravity] Failed to deserialize message ({data.Length} bytes): {ex.Message}");
                }
                return null;
            }
        }

        /// <summary>
        /// Serialize an object to JSON bytes for use as message payload.
        /// </summary>
        public static byte[] SerializePayload<T>(T obj)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj, _jsonSettings);
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to serialize payload: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserialize JSON bytes into an object.
        /// </summary>
        public static T DeserializePayload<T>(byte[] data)
        {
            try
            {
                string json = Encoding.UTF8.GetString(data);
                return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to deserialize payload: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Compress data using GZip.
        /// </summary>
        public static byte[] Compress(byte[] data)
        {
            try
            {
                using (var output = new MemoryStream())
                {
                    using (var gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Fastest))
                    {
                        gzip.Write(data, 0, data.Length);
                    }
                    return output.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to compress data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Decompress GZip data.
        /// </summary>
        public static byte[] Decompress(byte[] compressedData)
        {
            try
            {
                using (var input = new MemoryStream(compressedData))
                using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    gzip.CopyTo(output);
                    return output.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to decompress data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a message with a serialized payload.
        /// </summary>
        public static NetworkMessage CreateMessage<T>(MessageType type, T payload, ulong senderSteamId = 0, long tick = 0)
        {
            return new NetworkMessage
            {
                Type = type,
                SenderSteamId = senderSteamId,
                Tick = tick,
                Payload = SerializePayload(payload)
            };
        }

        /// <summary>
        /// Create a simple message without payload.
        /// </summary>
        public static NetworkMessage CreateMessage(MessageType type, ulong senderSteamId = 0, long tick = 0)
        {
            return new NetworkMessage
            {
                Type = type,
                SenderSteamId = senderSteamId,
                Tick = tick,
                Payload = Array.Empty<byte>()
            };
        }
    }
}
