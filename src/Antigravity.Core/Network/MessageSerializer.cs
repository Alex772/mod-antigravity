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
        /// </summary>
        public static NetworkMessage Deserialize(byte[] data)
        {
            try
            {
                using (var ms = new MemoryStream(data))
                using (var reader = new BinaryReader(ms))
                {
                    var message = new NetworkMessage
                    {
                        Type = (MessageType)reader.ReadByte(),
                        SenderSteamId = reader.ReadUInt64(),
                        Tick = reader.ReadInt64()
                    };

                    int payloadLength = reader.ReadInt32();
                    if (payloadLength > 0)
                    {
                        message.Payload = reader.ReadBytes(payloadLength);
                    }

                    return message;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to deserialize message: {ex.Message}");
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
