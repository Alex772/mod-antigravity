using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Antigravity.Debugger.Models;
using Antigravity.Debugger.Services;

namespace Antigravity.Debugger.ViewModels;

/// <summary>
/// World Data tab functionality - receive, parse, and display world save data
/// </summary>
public partial class MainWindowViewModel
{
    #region Stats and Processing
    
    /// <summary>
    /// Update World Data tab statistics when a packet is received
    /// </summary>
    private void UpdateWorldDataStats(CapturedPacket packet)
    {
        TotalPacketsReceived++;
        
        // Format bytes nicely
        var totalBytes = _currentSession.Stats.BytesReceived;
        TotalBytesReceived = totalBytes switch
        {
            > 1024 * 1024 => $"{totalBytes / (1024.0 * 1024.0):F2} MB",
            > 1024 => $"{totalBytes / 1024.0:F2} KB",
            _ => $"{totalBytes} B"
        };
        
        // Update last game tick
        if (packet.Tick > LastGameTick)
            LastGameTick = packet.Tick;
        
        // Process world data messages
        ProcessWorldDataMessage(packet);
        
        // Update command type stats if this is a game command (MessageType 30 = Command)
        if (packet.MessageType == 30 && !string.IsNullOrEmpty(packet.CommandTypeName))
        {
            var existing = CommandTypeStats.FirstOrDefault(x => x.Key == packet.CommandTypeName);
            if (existing.Key != null)
            {
                var index = CommandTypeStats.IndexOf(existing);
                CommandTypeStats[index] = new KeyValuePair<string, int>(existing.Key, existing.Value + 1);
            }
            else
            {
                CommandTypeStats.Add(new KeyValuePair<string, int>(packet.CommandTypeName, 1));
            }
        }
    }
    
    #endregion
    
    #region World Data Messages
    
    /// <summary>
    /// Process world data related messages (GameStarting, WorldDataChunk, etc.)
    /// </summary>
    private void ProcessWorldDataMessage(CapturedPacket packet)
    {
        try
        {
            // Log all message types for debugging
            Console.WriteLine($"[WorldData] Received MessageType={packet.MessageType} ({packet.MessageTypeName}), payload length={packet.PayloadJson?.Length ?? 0}");
            
            // MessageType 10 = GameStarting
            if (packet.MessageType == 10)
            {
                ProcessGameStartingMessage(packet);
            }
            // MessageType 12 = WorldDataChunk
            else if (packet.MessageType == 12)
            {
                ProcessWorldDataChunk(packet);
            }
            // MessageType 14 = GameStart
            else if (packet.MessageType == 14)
            {
                IsReceivingWorldData = false;
                if (_completeWorldData != null)
                {
                    WorldDataStatus = $"World data complete: {FormatBytes(_completeWorldData.Length)} (compressed)";
                }
                else
                {
                    WorldDataStatus = "Game started";
                }
            }
        }
        catch (System.Exception ex)
        {
            WorldDataStatus = $"Error: {ex.Message}";
        }
    }
    
    private void ProcessGameStartingMessage(CapturedPacket packet)
    {
        if (string.IsNullOrEmpty(packet.PayloadJson)) return;
        
        var json = System.Text.Json.JsonDocument.Parse(packet.PayloadJson);
        var root = json.RootElement;
        
        if (root.TryGetProperty("ColonyName", out var colonyName))
            WorldDataColonyName = colonyName.GetString() ?? "-";
        if (root.TryGetProperty("TotalDataSize", out var totalSize))
            WorldDataTotalSize = totalSize.GetInt32();
        if (root.TryGetProperty("ChunkCount", out var chunkCount))
            WorldDataChunksTotal = chunkCount.GetInt32();
        
        IsReceivingWorldData = true;
        WorldDataReceivedSize = 0;
        WorldDataChunksReceived = 0;
        WorldDataProgress = "0%";
        WorldDataStatus = $"Receiving world data: {WorldDataColonyName}";
        _worldDataChunks.Clear();
        _completeWorldData = null;
    }
    
    private void ProcessWorldDataChunk(CapturedPacket packet)
    {
        if (string.IsNullOrEmpty(packet.PayloadJson)) return;
        
        var json = System.Text.Json.JsonDocument.Parse(packet.PayloadJson);
        var root = json.RootElement;
        
        int chunkIndex = 0;
        int totalChunks = 0;
        string base64Data = "";
        
        if (root.TryGetProperty("ChunkIndex", out var idx))
            chunkIndex = idx.GetInt32();
        if (root.TryGetProperty("TotalChunks", out var total))
            totalChunks = total.GetInt32();
        if (root.TryGetProperty("Data", out var data))
            base64Data = data.GetString() ?? "";
        
        // Decode chunk data
        if (!string.IsNullOrEmpty(base64Data))
        {
            byte[] chunkData = Convert.FromBase64String(base64Data);
            _worldDataChunks[chunkIndex] = chunkData;
            WorldDataChunksReceived = _worldDataChunks.Count;
            WorldDataReceivedSize = _worldDataChunks.Values.Sum(c => c.Length);
            
            // Calculate progress
            if (totalChunks > 0)
            {
                int progress = (WorldDataChunksReceived * 100) / totalChunks;
                WorldDataProgress = $"{progress}%";
                WorldDataStatus = $"Receiving chunk {chunkIndex + 1}/{totalChunks}";
            }
            
            // Check if we have all chunks
            if (WorldDataChunksReceived == totalChunks)
            {
                AssembleWorldData();
            }
        }
    }
    
    #endregion
    
    #region Data Assembly
    
    /// <summary>
    /// Assemble all received chunks into complete world data
    /// </summary>
    private void AssembleWorldData()
    {
        if (_worldDataChunks.Count == 0) return;
        
        // Sort chunks by index and concatenate
        var ordered = _worldDataChunks.OrderBy(k => k.Key).ToList();
        int totalLength = ordered.Sum(x => x.Value.Length);
        _completeWorldData = new byte[totalLength];
        
        int offset = 0;
        foreach (var chunk in ordered)
        {
            Array.Copy(chunk.Value, 0, _completeWorldData, offset, chunk.Value.Length);
            offset += chunk.Value.Length;
        }
        
        IsReceivingWorldData = false;
        HasWorldData = true;
        WorldDataCompressedSize = FormatBytes(_completeWorldData.Length);
        WorldDataStatus = $"Complete: {FormatBytes(_completeWorldData.Length)} (compressed)";
        ConnectionStatus = $"Received complete world data: {WorldDataColonyName}";
        
        // Show hex preview of first 2KB
        UpdateWorldDataPreview();
    }
    
    private void UpdateWorldDataPreview()
    {
        if (_completeWorldData == null)
        {
            WorldDataPreview = "No data loaded.";
            return;
        }
        
        var data = _decompressedWorldData ?? _completeWorldData;
        var dataType = _decompressedWorldData != null ? "DECOMPRESSED" : "COMPRESSED";
        
        int previewSize = Math.Min(data.Length, 2048);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {dataType} DATA PREVIEW ({previewSize} of {data.Length} bytes) ===\n");
        
        // Show hex dump
        for (int i = 0; i < previewSize; i += 16)
        {
            sb.Append($"{i:X8}  ");
            
            // Hex bytes
            for (int j = 0; j < 16 && i + j < previewSize; j++)
            {
                sb.Append($"{data[i + j]:X2} ");
                if (j == 7) sb.Append(" ");
            }
            
            // Padding for incomplete lines
            for (int j = Math.Min(16, previewSize - i); j < 16; j++)
            {
                sb.Append("   ");
                if (j == 7) sb.Append(" ");
            }
            
            sb.Append(" |");
            
            // ASCII representation
            for (int j = 0; j < 16 && i + j < previewSize; j++)
            {
                char c = (char)data[i + j];
                sb.Append(c >= 32 && c < 127 ? c : '.');
            }
            
            sb.AppendLine("|");
        }
        
        WorldDataPreview = sb.ToString();
    }
    
    #endregion
    
    #region Decompression
    
    /// <summary>
    /// Decompress the world data using zlib
    /// </summary>
    [RelayCommand]
    private void DecompressWorldData()
    {
        if (_completeWorldData == null) return;
        
        try
        {
            WorldDataStatus = "Parsing save data...";
            
            // Use SaveFileParser to decompress and parse
            _parsedSaveData = SaveFileParser.ParseFromCompressedData(_completeWorldData);
            
            if (_parsedSaveData.DecompressedSize > 0)
            {
                // Store decompressed data for saving
                _decompressedWorldData = new byte[_parsedSaveData.DecompressedSize];
            }
            
            WorldDataDecompressedSize = FormatBytes(_parsedSaveData.DecompressedSize);
            WorldDataCompressedSize = FormatBytes(_parsedSaveData.CompressedSize);
            
            double ratio = _parsedSaveData.DecompressedSize > 0 
                ? (double)_parsedSaveData.CompressedSize / _parsedSaveData.DecompressedSize * 100 
                : 0;
            WorldDataCompressionRatio = $"{ratio:F1}%";
            
            // Populate parsed save info
            if (_parsedSaveData.IsValid)
            {
                PopulateParsedSaveInfo();
            }
            else
            {
                WorldDataStatus = $"Parse incomplete: {_parsedSaveData.ErrorMessage}";
                UpdateWorldDataPreview();
            }
        }
        catch (Exception ex)
        {
            WorldDataStatus = $"Parse failed: {ex.Message}";
            WorldDataPreview = $"Failed to parse save data:\n{ex.Message}";
        }
    }
    
    private void PopulateParsedSaveInfo()
    {
        var header = _parsedSaveData!.Header!;
        var info = _parsedSaveData.GameInfo!;
        
        SaveBuildVersion = header.BuildVersion.ToString();
        SaveVersion = $"{info.saveMajorVersion}.{info.saveMinorVersion}";
        SaveCycles = info.numberOfCycles.ToString();
        SaveDuplicants = info.numberOfDuplicants.ToString();
        SaveClusterId = info.clusterId ?? "-";
        SaveSandbox = info.sandboxEnabled ? "Yes" : "No";
        
        if (info.worldTraits != null && info.worldTraits.Length > 0)
        {
            var traitNames = info.worldTraits.Select(t => 
                System.IO.Path.GetFileName(t) ?? t).ToArray();
            SaveTraits = string.Join(", ", traitNames);
        }
        else
        {
            SaveTraits = "None";
        }
        
        if (_parsedSaveData.WorldWidth > 0 && _parsedSaveData.WorldHeight > 0)
        {
            int totalTiles = _parsedSaveData.WorldWidth * _parsedSaveData.WorldHeight;
            SaveWorldSize = $"{_parsedSaveData.WorldWidth}x{_parsedSaveData.WorldHeight} ({totalTiles:N0} tiles)";
        }
        
        WorldDataColonyName = info.baseName ?? WorldDataColonyName;
        WorldDataStatus = $"Parsed: {info.baseName} - Cycle {info.numberOfCycles}";
        
        // Update preview with parsed info
        UpdateParsedDataPreview();
    }
    
    private void UpdateParsedDataPreview()
    {
        if (_parsedSaveData == null || !_parsedSaveData.IsValid)
        {
            UpdateWorldDataPreview();
            return;
        }
        
        var header = _parsedSaveData.Header!;
        var info = _parsedSaveData.GameInfo!;
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                     SAVE FILE INFO                       ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Colony:          {info.baseName,-38} ║");
        sb.AppendLine($"║  Build:           {header.BuildVersion,-38} ║");
        sb.AppendLine($"║  Save Version:    {info.saveMajorVersion}.{info.saveMinorVersion,-36} ║");
        sb.AppendLine($"║  Cycles:          {info.numberOfCycles,-38} ║");
        sb.AppendLine($"║  Duplicants:      {info.numberOfDuplicants,-38} ║");
        sb.AppendLine($"║  Compressed:      {header.IsCompressed,-38} ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Cluster ID:      {TruncateString(info.clusterId ?? "-", 38),-38} ║");
        sb.AppendLine($"║  Sandbox:         {(info.sandboxEnabled ? "Enabled" : "Disabled"),-38} ║");
        
        if (_parsedSaveData.WorldWidth > 0)
        {
            sb.AppendLine($"║  World Size:      {_parsedSaveData.WorldWidth}x{_parsedSaveData.WorldHeight} tiles{"",-26} ║");
        }
        
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine("║  WORLD TRAITS:                                           ║");
        
        if (info.worldTraits != null && info.worldTraits.Length > 0)
        {
            foreach (var trait in info.worldTraits)
            {
                string traitName = System.IO.Path.GetFileName(trait) ?? trait;
                sb.AppendLine($"║    • {TruncateString(traitName, 52),-52} ║");
            }
        }
        else
        {
            sb.AppendLine("║    (No traits)                                           ║");
        }
        
        if (info.dlcIds != null && info.dlcIds.Count > 0)
        {
            sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  DLC:                                                    ║");
            foreach (var dlc in info.dlcIds)
            {
                if (!string.IsNullOrEmpty(dlc))
                    sb.AppendLine($"║    • {TruncateString(dlc, 52),-52} ║");
            }
        }
        
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Compressed Size:   {FormatBytes(_parsedSaveData.CompressedSize),-36} ║");
        sb.AppendLine($"║  Decompressed Size: {FormatBytes(_parsedSaveData.DecompressedSize),-36} ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════╝");
        
        WorldDataPreview = sb.ToString();
    }
    
    #endregion
    
    #region Save/Clear Commands
    
    private static string TruncateString(string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str)) return str ?? "";
        return str.Length <= maxLength ? str : str.Substring(0, maxLength - 3) + "...";
    }
    
    /// <summary>
    /// Save world data to file
    /// </summary>
    [RelayCommand]
    private void SaveWorldData()
    {
        if (_completeWorldData == null) return;
        
        try
        {
            var saveData = _decompressedWorldData ?? _completeWorldData;
            var suffix = _decompressedWorldData != null ? "" : "_compressed";
            var filename = $"{WorldDataColonyName.Replace(" ", "_")}{suffix}.sav";
            
            // Save to current directory
            var path = System.IO.Path.Combine(Environment.CurrentDirectory, filename);
            System.IO.File.WriteAllBytes(path, saveData);
            
            WorldDataStatus = $"Saved to: {filename}";
            ConnectionStatus = $"World data saved: {path}";
        }
        catch (Exception ex)
        {
            WorldDataStatus = $"Save failed: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Clear world data
    /// </summary>
    [RelayCommand]
    private void ClearWorldData()
    {
        _worldDataChunks.Clear();
        _completeWorldData = null;
        _decompressedWorldData = null;
        
        HasWorldData = false;
        WorldDataStatus = "Waiting for world data...";
        WorldDataColonyName = "-";
        WorldDataTotalSize = 0;
        WorldDataReceivedSize = 0;
        WorldDataChunksTotal = 0;
        WorldDataChunksReceived = 0;
        WorldDataProgress = "0%";
        WorldDataCompressedSize = "-";
        WorldDataDecompressedSize = "-";
        WorldDataCompressionRatio = "-";
        WorldDataPreview = "No data loaded. Connect to a host and load a save to capture world data.";
        
        ConnectionStatus = "World data cleared";
    }
    
    private static string FormatBytes(long bytes) => bytes switch
    {
        > 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F2} MB",
        > 1024 => $"{bytes / 1024.0:F2} KB",
        _ => $"{bytes} B"
    };
    
    /// <summary>
    /// Refresh World Data statistics by recalculating from all packet groups
    /// </summary>
    [RelayCommand]
    private void RefreshWorldData()
    {
        // Recalculate command type stats from all groups
        var stats = new Dictionary<string, int>();
        
        foreach (var group in PacketGroups)
        {
            foreach (var packet in group.Packets)
            {
                if (!string.IsNullOrEmpty(packet.CommandTypeName))
                {
                    if (stats.ContainsKey(packet.CommandTypeName))
                        stats[packet.CommandTypeName]++;
                    else
                        stats[packet.CommandTypeName] = 1;
                }
            }
        }
        
        // Sort by count descending and update observable collection
        CommandTypeStats.Clear();
        foreach (var kvp in stats.OrderByDescending(x => x.Value))
        {
            CommandTypeStats.Add(kvp);
        }
        
        ConnectionStatus = $"World data refreshed - {stats.Count} command types";
    }
    
    #endregion
}
