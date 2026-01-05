using System;
using System.Collections.Generic;
using System.Linq;

namespace Antigravity.Debugger.Models;

/// <summary>
/// Represents a saved command or group of commands that can be loaded and replayed later.
/// </summary>
public class SavedCommand
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public SystemDateTime CreatedAt { get; set; } = SystemDateTime.Now;
    public string CommandType { get; set; } = "";  // Main command type (Dig, Build, etc.)
    public int GameTick { get; set; }
    public bool IsGroup { get; set; } = false;  // True if this is a custom group
    public List<string> SourceCommandIds { get; set; } = new();  // IDs of source commands if grouped
    public List<SavedPacketData> Packets { get; set; } = new();
    
    public int PacketCount => Packets.Count;
    public string DisplayName => string.IsNullOrEmpty(Name) ? CommandType : Name;
    public int TotalBytes => Packets.Sum(p => p.RawData?.Length ?? 0);
    public string TotalBytesString => $"{TotalBytes} B";
    public string TypeIcon => IsGroup ? "📁" : "📄";
    public string CommandTypesList => Packets.Count > 0 
        ? string.Join(", ", Packets.Select(p => p.CommandTypeName).Distinct())
        : "";
}

/// <summary>
/// Minimal packet data needed for replay.
/// </summary>
public class SavedPacketData
{
    public int Order { get; set; }
    public string CommandTypeName { get; set; } = "";
    public byte[] RawData { get; set; } = Array.Empty<byte>();
    public string PayloadJson { get; set; } = "";
    public int GameTick { get; set; }
    
    public int Size => RawData?.Length ?? 0;
    public string SizeString => $"{Size} B";
}
