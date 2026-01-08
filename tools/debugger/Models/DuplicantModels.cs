using System;
using System.Collections.Generic;

namespace Antigravity.Debugger.Models;

/// <summary>
/// Model for Duplicant information displayed in the debugger.
/// </summary>
public class DuplicantInfo
{
    public string Name { get; set; } = "";
    public int CellX { get; set; }
    public int CellY { get; set; }
    public string Position => $"({CellX}, {CellY})";
    public string CurrentChore { get; set; } = "Idle";
    public string ChoreTarget { get; set; } = "";
    public bool IsHost { get; set; }
    public string Status { get; set; } = "Synced";
    public long LastUpdate { get; set; }
    
    // Chore history - last N chores received
    public List<DuplicantChoreInfo> ChoreHistory { get; set; } = new();
    
    // For display
    public string DisplayName => IsHost ? $"👑 {Name}" : $"👤 {Name}";
    public string StatusColor => Status == "Synced" ? "#a6e3a1" : "#f38ba8";
}

/// <summary>
/// Detailed chore/task info
/// </summary>
public class DuplicantChoreInfo
{
    public string ChoreType { get; set; } = "";
    public string ChoreGroup { get; set; } = "";
    public int TargetCell { get; set; }
    public float TargetX { get; set; }
    public float TargetY { get; set; }
    public string TargetPrefabId { get; set; } = "";
    public string Priority { get; set; } = "";
    public long Timestamp { get; set; }
    
    // Display properties
    public string DisplayTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).ToString("HH:mm:ss");
    public string PositionDisplay => $"({TargetX:F0}, {TargetY:F0})";
    public string Summary => $"{ChoreType} at {PositionDisplay}";
}
