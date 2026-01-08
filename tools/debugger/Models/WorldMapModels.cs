using System;
using System.Collections.Generic;

namespace Antigravity.Debugger.Models;

/// <summary>
/// World map data for grid visualization
/// </summary>
public class WorldMapData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public Dictionary<int, CellData> Cells { get; set; } = new();
    
    public int CellToX(int cell) => cell % Width;
    public int CellToY(int cell) => cell / Width;
    public int XYToCell(int x, int y) => y * Width + x;
}

/// <summary>
/// Data for a single cell in the world
/// </summary>
public class CellData
{
    public int Cell { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public List<ItemInfo> Items { get; set; } = new();
    public int ItemCount => Items.Count;
    
    // Display
    public string CellColor => ItemCount switch
    {
        0 => "#1e1e2e",      // Empty - dark
        1 => "#89b4fa",      // 1 item - blue
        <= 5 => "#a6e3a1",   // Few - green
        <= 10 => "#f9e2af",  // Some - yellow
        _ => "#f38ba8"       // Many - red
    };
}

/// <summary>
/// Info about a single item
/// </summary>
public class ItemInfo
{
    public int Cell { get; set; }
    public string PrefabId { get; set; } = "";
    public float Mass { get; set; }
    public int ElementId { get; set; }
    public long Timestamp { get; set; }
    
    // Display
    public string DisplayTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).ToString("HH:mm:ss");
    public string MassDisplay => $"{Mass:F1}kg";
    public string Summary => $"{PrefabId} ({MassDisplay})";
}

/// <summary>
/// Item sync message received from game
/// </summary>
public class ItemSyncMessage
{
    public int WorldWidth { get; set; }
    public int WorldHeight { get; set; }
    public List<ItemSnapshot> Items { get; set; } = new();
}

/// <summary>
/// Snapshot of a single item from game
/// </summary>
public class ItemSnapshot
{
    public int Cell { get; set; }
    public string PrefabId { get; set; } = "";
    public float Mass { get; set; }
    public int ElementId { get; set; }
}
