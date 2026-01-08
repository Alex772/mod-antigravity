using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Antigravity.Debugger.Models;

namespace Antigravity.Debugger.Services
{
    /// <summary>
    /// Parser for ONI save file format
    /// </summary>
    public static class SaveFileParser
    {
        /// <summary>
        /// Parse save data from compressed bytes received via network
        /// </summary>
        public static ParsedSaveData ParseFromCompressedData(byte[] compressedData)
        {
            var result = new ParsedSaveData
            {
                CompressedSize = compressedData.Length
            };

            try
            {
                // Decompress the data (zlib format)
                byte[] decompressed = DecompressZlib(compressedData);
                result.DecompressedSize = decompressed.Length;

                // Parse the save file content
                using var stream = new MemoryStream(decompressed);
                using var reader = new BinaryReader(stream);
                
                ParseSaveContent(reader, result);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Failed to parse: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Parse save data from raw bytes (already decompressed)
        /// </summary>
        public static ParsedSaveData ParseFromRawData(byte[] rawData)
        {
            var result = new ParsedSaveData
            {
                DecompressedSize = rawData.Length
            };

            try
            {
                using var stream = new MemoryStream(rawData);
                using var reader = new BinaryReader(stream);
                
                ParseSaveContent(reader, result);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Failed to parse: {ex.Message}";
            }

            return result;
        }

        private static void ParseSaveContent(BinaryReader reader, ParsedSaveData result)
        {
            // Read header
            result.Header = new SaveFileHeader
            {
                BuildVersion = reader.ReadUInt32(),
                HeaderSize = reader.ReadInt32(),
                HeaderVersion = reader.ReadUInt32(),
                Compression = reader.ReadInt32()
            };

            // Read GameInfo JSON
            if (result.Header.HeaderSize > 0 && result.Header.HeaderSize < 100000)
            {
                byte[] jsonBytes = reader.ReadBytes(result.Header.HeaderSize);
                string json = Encoding.UTF8.GetString(jsonBytes);
                
                // Parse JSON
                result.GameInfo = System.Text.Json.JsonSerializer.Deserialize<SaveGameInfo>(json);
            }

            // Try to read world dimensions by searching for patterns
            TryParseWorldDimensions(reader, result);
        }

        private static void TryParseWorldDimensions(BinaryReader reader, ParsedSaveData result)
        {
            try
            {
                // The save format has "world" string followed by SaveFileRoot
                // SaveFileRoot starts with WidthInCells (int) and HeightInCells (int)
                
                long startPos = reader.BaseStream.Position;
                byte[] remaining = reader.ReadBytes((int)Math.Min(10000, reader.BaseStream.Length - startPos));
                
                // Search for "world" marker
                string worldMarker = "world";
                byte[] marker = Encoding.UTF8.GetBytes(worldMarker);
                
                int markerIndex = FindBytes(remaining, marker);
                if (markerIndex >= 0)
                {
                    // Skip past the marker and the Klei string format
                    // Klei string format: int length + chars
                    int offset = markerIndex + worldMarker.Length;
                    
                    // Look for two consecutive integers that look like world dimensions
                    // Typical ONI world sizes: 256x384, 128x256, etc.
                    for (int i = offset; i < remaining.Length - 8; i++)
                    {
                        int val1 = BitConverter.ToInt32(remaining, i);
                        int val2 = BitConverter.ToInt32(remaining, i + 4);
                        
                        // Check if these look like valid world dimensions
                        if (val1 >= 64 && val1 <= 512 && val2 >= 64 && val2 <= 512)
                        {
                            result.WorldWidth = val1;
                            result.WorldHeight = val2;
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Ignore dimension parsing errors
            }
        }

        private static int FindBytes(byte[] source, byte[] pattern)
        {
            for (int i = 0; i <= source.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (source[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found) return i;
            }
            return -1;
        }

        private static byte[] DecompressZlib(byte[] compressedData)
        {
            // The mod uses GZipStream, not raw Zlib!
            try
            {
                using var inputStream = new MemoryStream(compressedData);
                using var outputStream = new MemoryStream();
                using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
                gzip.CopyTo(outputStream);
                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"GZip decompression failed: {ex.Message}", ex);
            }
        }
    }
}
