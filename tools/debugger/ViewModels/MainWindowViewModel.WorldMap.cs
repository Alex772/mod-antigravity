using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Antigravity.Debugger.Models;

namespace Antigravity.Debugger.ViewModels;

/// <summary>
/// Detailed item info for display
/// </summary>
public record ItemInfo(string PrefabId, float Mass, int ElementId);

/// <summary>
/// Element cell data for gas/liquid visualization
/// </summary>
public record ElementCellData(int ElementId, float Mass, float Temperature, bool IsGas);

/// <summary>
/// Filter item for selection - must implement INotifyPropertyChanged for binding
/// </summary>
public class FilterItem : System.ComponentModel.INotifyPropertyChanged
{
    private string _name = "";
    private bool _isSelected = true;
    private int _count = 0;
    
    public string Name 
    { 
        get => _name; 
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }
    
    public bool IsSelected 
    { 
        get => _isSelected; 
        set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
    }
    
    public int Count 
    { 
        get => _count; 
        set { _count = value; OnPropertyChanged(nameof(Count)); }
    }
    
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}

/// <summary>
/// World Map tab functionality - renders world item data as a bitmap
/// </summary>
public partial class MainWindowViewModel
{
    // Store cell data for lookup
    private Dictionary<int, int> _cellItemCounts = new();
    private Dictionary<int, List<ItemInfo>> _cellItems = new(); // Detailed item info
    private List<ItemInfo> _allItems = new(); // All items for filtering
    
    // Store element (gas/liquid) data for visualization
    private Dictionary<int, ElementCellData> _gasCells = new();
    private Dictionary<int, ElementCellData> _liquidCells = new();
    private int _elementChangesReceived = 0;
    
    // Map refresh throttling
    private bool _mapRefreshPending = false;
    private long _lastMapRefresh = 0;
    private const int MAP_REFRESH_THROTTLE_MS = 500; // Minimum ms between refreshes
    
    /// <summary>
    /// Request a map refresh with throttling to avoid excessive re-renders
    /// </summary>
    private void RequestMapRefresh()
    {
        long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (now - _lastMapRefresh < MAP_REFRESH_THROTTLE_MS)
        {
            // Schedule a delayed refresh if not already pending
            if (!_mapRefreshPending)
            {
                _mapRefreshPending = true;
                Task.Delay(MAP_REFRESH_THROTTLE_MS).ContinueWith(_ =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        _mapRefreshPending = false;
                        ApplyFilterAndRender();
                    });
                });
            }
            return;
        }
        
        _lastMapRefresh = now;
        ApplyFilterAndRender();
    }
    
    #region World Map Commands
    
    [RelayCommand]
    private void RefreshWorldMap()
    {
        WorldMapStatus = "Refreshing...";
        ApplyFilterAndRender();
        WorldMapLastUpdate = System.DateTime.Now.ToString("HH:mm:ss");
    }
    
    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = 1.0;
    }
    
    [RelayCommand]
    private void FitToView()
    {
        double fitZoom = Math.Min(600.0 / (_worldWidth * 2), 500.0 / (_worldHeight * 2));
        ZoomLevel = Math.Max(0.5, Math.Min(4.0, fitZoom));
    }
    
    [RelayCommand]
    private void ApplyFilter()
    {
        ApplyFilterAndRender();
    }
    
    [RelayCommand]
    private void SelectAllFilters()
    {
        foreach (var filter in ItemFilters)
            filter.IsSelected = true;
        ApplyFilterAndRender();
    }
    
    [RelayCommand]
    private void ClearAllFilters()
    {
        foreach (var filter in ItemFilters)
            filter.IsSelected = false;
        ApplyFilterAndRender();
    }
    
    private void ApplyFilterAndRender()
    {
        // Get selected item types
        var selectedTypes = new HashSet<string>();
        foreach (var filter in ItemFilters)
        {
            if (filter.IsSelected)
                selectedTypes.Add(filter.Name);
        }
        
        System.Diagnostics.Debug.WriteLine($"[Filter] Selected {selectedTypes.Count} types, _cellItems has {_cellItems.Count} cells");
        
        // If no filters loaded yet, just render everything
        if (ItemFilters.Count == 0 || _cellItems.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Filter] No filters or no cellItems, rendering all from _cellItemCounts ({_cellItemCounts.Count})");
            RenderWorldMapBitmap(_cellItemCounts);
            WorldMapStatus = $"Showing all: {TotalItemsCount} items";
            return;
        }
        
        // If all selected, show everything
        bool showAll = selectedTypes.Count == ItemFilters.Count;
        
        // Build filtered cell counts
        var filteredCounts = new Dictionary<int, int>();
        int totalFiltered = 0;
        
        foreach (var kvp in _cellItems)
        {
            int cellId = kvp.Key;
            int count = 0;
            foreach (var item in kvp.Value)
            {
                if (showAll || selectedTypes.Contains(item.PrefabId))
                    count++;
            }
            if (count > 0)
            {
                filteredCounts[cellId] = count;
                totalFiltered += count;
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"[Filter] Filtered result: {filteredCounts.Count} cells, {totalFiltered} items");
        
        RenderWorldMapBitmap(filteredCounts);
        WorldMapStatus = showAll 
            ? $"Showing all: {TotalItemsCount} items" 
            : $"Filtered: {totalFiltered} items ({selectedTypes.Count} types)";
    }
    
    private void UpdateItemFilters()
    {
        // Count items by PrefabId
        var itemCounts = new Dictionary<string, int>();
        foreach (var item in _allItems)
        {
            itemCounts.TryGetValue(item.PrefabId, out int count);
            itemCounts[item.PrefabId] = count + 1;
        }
        
        ItemFilters.Clear();
        foreach (var kvp in itemCounts.OrderByDescending(x => x.Value))
        {
            ItemFilters.Add(new FilterItem 
            { 
                Name = kvp.Key, 
                Count = kvp.Value, 
                IsSelected = true 
            });
        }
    }
    
    /// <summary>
    /// Called from code-behind when Ctrl+Scroll is detected
    /// </summary>
    public void AdjustZoom(double delta)
    {
        double newZoom = ZoomLevel + (delta * 0.1);
        ZoomLevel = Math.Clamp(newZoom, 0.5, 4.0);
    }
    
    /// <summary>
    /// Called from code-behind when user clicks on the map
    /// </summary>
    public void SelectCellAtPosition(double x, double y)
    {
        // The bitmap is at 4x scale, so divide by 4 to get cell coordinates
        int scale = 4;
        int cellX = (int)(x / scale);
        int cellY = (int)(y / scale);
        
        // Flip Y back (display is flipped from world coords)
        int worldY = _worldHeight - cellY - 1;
        int cellId = worldY * _worldWidth + cellX;
        
        System.Diagnostics.Debug.WriteLine($"[WorldMap] Click at ({x:F1}, {y:F1}) -> cell ({cellX}, {worldY}), cellId={cellId}, hasData={_cellItemCounts.ContainsKey(cellId)}");
        
        if (cellX < 0 || cellX >= _worldWidth || worldY < 0 || worldY >= _worldHeight)
        {
            SelectedCellInfo = "Out of bounds";
            return;
        }
        
        SelectedCellX = cellX;
        SelectedCellY = worldY;
        
        // Collect all cell data
        var cellData = new Views.CellData();
        
        // Get gas data
        if (_gasCells.TryGetValue(cellId, out var gasData))
        {
            cellData.Gas = new Views.ElementInfo
            {
                ElementId = gasData.ElementId,
                Mass = gasData.Mass,
                Temperature = gasData.Temperature
            };
        }
        
        // Get liquid data
        if (_liquidCells.TryGetValue(cellId, out var liquidData))
        {
            cellData.Liquid = new Views.ElementInfo
            {
                ElementId = liquidData.ElementId,
                Mass = liquidData.Mass,
                Temperature = liquidData.Temperature
            };
        }
        
        // Get item data
        cellData.Items = new System.Collections.Generic.List<string>();
        if (_cellItems.TryGetValue(cellId, out var items))
        {
            foreach (var item in items)
                cellData.Items.Add($"{item.PrefabId}: {item.Mass:F1} kg");
        }
        else if (_cellItemDetails.TryGetValue(cellId, out var names))
        {
            foreach (var name in names)
                cellData.Items.Add(name);
        }
        
        // Update summary display
        int itemCount = cellData.Items?.Count ?? 0;
        SelectedCellItemCount = itemCount;
        
        string gasStr = cellData.Gas != null ? $"Gas: {cellData.Gas.Mass:F1}kg" : "";
        string liqStr = cellData.Liquid != null ? $"Liq: {cellData.Liquid.Mass:F1}kg" : "";
        string itemStr = itemCount > 0 ? $"{itemCount} item(s)" : "";
        
        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(gasStr)) parts.Add(gasStr);
        if (!string.IsNullOrEmpty(liqStr)) parts.Add(liqStr);
        if (!string.IsNullOrEmpty(itemStr)) parts.Add(itemStr);
        
        SelectedCellInfo = parts.Count > 0 
            ? $"Cell ({cellX}, {worldY}) - " + string.Join(", ", parts)
            : $"Cell ({cellX}, {worldY}) - Empty";
        
        // Update items list
        SelectedCellItems.Clear();
        foreach (var item in cellData.Items)
            SelectedCellItems.Add($"• {item}");
        
        // Store data for window opening
        _lastSelectedCellData = (cellX, worldY, cellId, cellData);
    }
    
    private (int X, int Y, int Id, Views.CellData Data)? _lastSelectedCellData;
    
    /// <summary>
    /// Get the last selected cell data for opening details window
    /// </summary>
    public (int X, int Y, int Id, Views.CellData Data)? GetLastSelectedCellData() => _lastSelectedCellData;
    
    /// <summary>
    /// Called from code-behind when mouse moves over the map
    /// </summary>
    public void UpdateHoverInfo(double x, double y)
    {
        int scale = 4;
        int cellX = (int)(x / scale);
        int cellY = (int)(y / scale);
        int worldY = _worldHeight - cellY - 1;
        
        if (cellX < 0 || cellX >= _worldWidth || worldY < 0 || worldY >= _worldHeight)
        {
            HoveredCellInfo = "";
            return;
        }
        
        int cellId = worldY * _worldWidth + cellX;
        
        if (_cellItemCounts.TryGetValue(cellId, out int itemCount) && itemCount > 0)
        {
            HoveredCellInfo = $"({cellX}, {worldY}): {itemCount} items";
        }
        else
        {
            HoveredCellInfo = $"({cellX}, {worldY})";
        }
    }
    
    private void UpdateCanvasSize()
    {
        CanvasWidth = _worldWidth * CellSize;
        CanvasHeight = _worldHeight * CellSize;
    }
    
    private void InitializeWorldMapGrid(int width, int height)
    {
        _worldWidth = width;
        _worldHeight = height;
        WorldSizeDisplay = $"{width} x {height}";
        UpdateCanvasSize();
    }
    
    #endregion
    
    #region Bitmap Rendering
    
    private void RenderWorldMapBitmap(Dictionary<int, int> cellItemCounts)
    {
        int width = _worldWidth;
        int height = _worldHeight;
        
        int scale = 4;  // 4x scale for larger cells
        int bmpWidth = width * scale;
        int bmpHeight = height * scale;
        
        var bitmap = new WriteableBitmap(
            new PixelSize(bmpWidth, bmpHeight),
            new Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        
        // Check layer visibility
        bool showItems = MapLayers.FirstOrDefault(l => l.Type == MapLayerType.Items)?.IsVisible ?? true;
        bool showDuplicants = MapLayers.FirstOrDefault(l => l.Type == MapLayerType.Duplicants)?.IsVisible ?? true;
        bool showGases = MapLayers.FirstOrDefault(l => l.Type == MapLayerType.Gases)?.IsVisible ?? true;
        bool showLiquids = MapLayers.FirstOrDefault(l => l.Type == MapLayerType.Liquids)?.IsVisible ?? true;
        
        using (var fb = bitmap.Lock())
        {
            unsafe
            {
                var ptr = (uint*)fb.Address;
                
                // Background
                uint bgColor = 0xFF1b1111;
                for (int i = 0; i < bmpWidth * bmpHeight; i++)
                {
                    ptr[i] = bgColor;
                }
                
                // Layer 1: Items (if visible)
                if (showItems)
                {
                    foreach (var kvp in cellItemCounts)
                    {
                        int cellId = kvp.Key;
                        int itemCount = kvp.Value;
                        
                        int cellY = cellId / width;
                        int cellX = cellId % width;
                        
                        if (cellX >= width || cellY >= height) continue;
                        
                        int displayY = height - cellY - 1;
                        
                        uint color = itemCount switch
                        {
                            1 => 0xFFfa89b4,      // Light pink
                            <= 5 => 0xFFa1e3a6,   // Green
                            <= 10 => 0xFFafe2f9,  // Yellow
                            _ => 0xFFa88bf3       // Purple
                        };
                        
                        for (int dy = 0; dy < scale; dy++)
                        {
                            for (int dx = 0; dx < scale; dx++)
                            {
                                int px = cellX * scale + dx;
                                int py = displayY * scale + dy;
                                if (px >= 0 && px < bmpWidth && py >= 0 && py < bmpHeight)
                                {
                                    ptr[py * bmpWidth + px] = color;
                                }
                            }
                        }
                    }
                }
                
                // Layer 2: Gases (if visible) - render as transparent colored cells
                if (showGases && _gasCells.Count > 0)
                {
                    foreach (var kvp in _gasCells)
                    {
                        int cellId = kvp.Key;
                        var data = kvp.Value;
                        
                        int cellY = cellId / width;
                        int cellX = cellId % width;
                        
                        if (cellX >= width || cellY >= height) continue;
                        
                        int displayY = height - cellY - 1;
                        
                        // Color based on mass - more mass = more opaque cyan
                        byte alpha = (byte)Math.Min(255, 80 + (int)(data.Mass * 5));
                        uint gasColor = (uint)((alpha << 24) | 0x00FFFF); // Cyan with variable alpha
                        
                        for (int dy = 0; dy < scale; dy++)
                        {
                            for (int dx = 0; dx < scale; dx++)
                            {
                                int px = cellX * scale + dx;
                                int py = displayY * scale + dy;
                                if (px >= 0 && px < bmpWidth && py >= 0 && py < bmpHeight)
                                {
                                    ptr[py * bmpWidth + px] = gasColor;
                                }
                            }
                        }
                    }
                }
                
                // Layer 3: Liquids (if visible) - render as blue colored cells
                if (showLiquids && _liquidCells.Count > 0)
                {
                    foreach (var kvp in _liquidCells)
                    {
                        int cellId = kvp.Key;
                        var data = kvp.Value;
                        
                        int cellY = cellId / width;
                        int cellX = cellId % width;
                        
                        if (cellX >= width || cellY >= height) continue;
                        
                        int displayY = height - cellY - 1;
                        
                        // Color based on mass - more mass = more opaque blue
                        byte alpha = (byte)Math.Min(255, 100 + (int)(data.Mass * 2));
                        uint liquidColor = (uint)((alpha << 24) | 0xFF8800); // Blue with variable alpha (BGRA)
                        
                        for (int dy = 0; dy < scale; dy++)
                        {
                            for (int dx = 0; dx < scale; dx++)
                            {
                                int px = cellX * scale + dx;
                                int py = displayY * scale + dy;
                                if (px >= 0 && px < bmpWidth && py >= 0 && py < bmpHeight)
                                {
                                    ptr[py * bmpWidth + px] = liquidColor;
                                }
                            }
                        }
                    }
                }
                
                // Layer 2: Duplicants (if visible) - render as larger orange squares
                if (showDuplicants)
                {
                    uint dupColor = 0xFF47a3ff; // Orange (BGRA)
                    int dupScale = scale + 2; // Slightly larger than items
                    
                    foreach (var dup in Duplicants)
                    {
                        int cellX = dup.CellX;
                        int cellY = dup.CellY;
                        
                        if (cellX < 0 || cellX >= width || cellY < 0 || cellY >= height) continue;
                        
                        int displayY = height - cellY - 1;
                        
                        // Draw larger square for duplicant
                        for (int dy = -1; dy < dupScale; dy++)
                        {
                            for (int dx = -1; dx < dupScale; dx++)
                            {
                                int px = cellX * scale + dx;
                                int py = displayY * scale + dy;
                                if (px >= 0 && px < bmpWidth && py >= 0 && py < bmpHeight)
                                {
                                    ptr[py * bmpWidth + px] = dupColor;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        WorldMapBitmap = bitmap;
        CanvasWidth = bmpWidth;
        CanvasHeight = bmpHeight;
        
        // Update layer counts
        var itemsLayer = MapLayers.FirstOrDefault(l => l.Type == MapLayerType.Items);
        var dupsLayer = MapLayers.FirstOrDefault(l => l.Type == MapLayerType.Duplicants);
        var gasLayer = MapLayers.FirstOrDefault(l => l.Type == MapLayerType.Gases);
        var liquidLayer = MapLayers.FirstOrDefault(l => l.Type == MapLayerType.Liquids);
        
        if (itemsLayer != null) itemsLayer.ItemCount = cellItemCounts.Values.Sum();
        if (dupsLayer != null) dupsLayer.ItemCount = Duplicants.Count;
        if (gasLayer != null) gasLayer.ItemCount = _gasCells.Count;
        if (liquidLayer != null) liquidLayer.ItemCount = _liquidCells.Count;
    }
    
    #endregion
    
    #region Item Sync Processing
    
    private void ProcessItemSyncCommand(CapturedPacket packet)
    {
        if (packet.PayloadJson == null) return;
        
        try
        {
            var json = packet.PayloadJson;
            
            int worldWidth = 256, worldHeight = 384;
            
            var widthMatch = Regex.Match(json, "\"WorldWidth\"\\s*:\\s*(\\d+)");
            if (widthMatch.Success) int.TryParse(widthMatch.Groups[1].Value, out worldWidth);
            
            var heightMatch = Regex.Match(json, "\"WorldHeight\"\\s*:\\s*(\\d+)");
            if (heightMatch.Success) int.TryParse(heightMatch.Groups[1].Value, out worldHeight);
            
            // First, parse all Cell values with simple regex (always works)
            var simpleMatches = Regex.Matches(json, "\"Cell\"\\s*:\\s*(\\d+)");
            
            var cellItemCounts = new Dictionary<int, int>();
            var cellItemDetails = new Dictionary<int, List<string>>();
            var cellItems = new Dictionary<int, List<ItemInfo>>();
            
            foreach (Match match in simpleMatches)
            {
                if (int.TryParse(match.Groups[1].Value, out int cell))
                {
                    cellItemCounts.TryGetValue(cell, out int count);
                    cellItemCounts[cell] = count + 1;
                    
                    if (!cellItemDetails.ContainsKey(cell))
                        cellItemDetails[cell] = new List<string>();
                    if (!cellItems.ContainsKey(cell))
                        cellItems[cell] = new List<ItemInfo>();
                }
            }
            
            // Try to extract full item data with Mass
            try
            {
                // Match items with Cell, PrefabId, Mass, ElementId (any order within braces)
                var itemPattern = new Regex(@"\{[^{}]*\}", RegexOptions.Compiled);
                var itemMatches = itemPattern.Matches(json);
                
                foreach (Match itemMatch in itemMatches)
                {
                    var itemJson = itemMatch.Value;
                    
                    var cellMatch = Regex.Match(itemJson, "\"Cell\"\\s*:\\s*(\\d+)");
                    var prefabMatch = Regex.Match(itemJson, "\"PrefabId\"\\s*:\\s*\"([^\"]+)\"");
                    var massMatch = Regex.Match(itemJson, "\"Mass\"\\s*:\\s*([\\d.]+)");
                    var elementMatch = Regex.Match(itemJson, "\"ElementId\"\\s*:\\s*(\\d+)");
                    
                    if (cellMatch.Success && prefabMatch.Success)
                    {
                        int cell = int.Parse(cellMatch.Groups[1].Value);
                        string prefabId = prefabMatch.Groups[1].Value;
                        float mass = massMatch.Success ? float.Parse(massMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0f;
                        int elementId = elementMatch.Success ? int.Parse(elementMatch.Groups[1].Value) : 0;
                        
                        if (cellItems.ContainsKey(cell))
                        {
                            cellItems[cell].Add(new ItemInfo(prefabId, mass, elementId));
                        }
                        if (cellItemDetails.ContainsKey(cell))
                        {
                            cellItemDetails[cell].Add(prefabId);
                        }
                    }
                }
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[WorldMap] Parsing details failed: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[WorldMap] Parsed {cellItemCounts.Count} cells from {simpleMatches.Count} items");
            
            int totalItems = 0;
            foreach (var count in cellItemCounts.Values) totalItems += count;
            int uniqueCells = cellItemCounts.Count;
            
            // Collect all items for filtering
            var allItems = new List<ItemInfo>();
            foreach (var list in cellItems.Values)
                allItems.AddRange(list);
            
            Dispatcher.UIThread.Post(() =>
            {
                _worldWidth = worldWidth;
                _worldHeight = worldHeight;
                WorldSizeDisplay = $"{worldWidth} x {worldHeight}";
                
                // Store for later lookup
                _cellItemCounts = cellItemCounts;
                _cellItemDetails = cellItemDetails;
                _cellItems = cellItems;
                _allItems = allItems;
                
                // Update filter list
                UpdateItemFilters();
                
                // Render with all items
                ApplyFilterAndRender();
                
                TotalItemsCount = totalItems;
                CellsWithItemsCount = uniqueCells;
                WorldMapLastUpdate = System.DateTime.Now.ToString("HH:mm:ss");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing ItemSync: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Element Sync Processing
    
    /// <summary>
    /// Process ElementChange command to update gas/liquid visualization
    /// </summary>
    public void ProcessElementChangeCommand(CapturedPacket packet)
    {
        if (packet.PayloadJson == null) return;
        
        try
        {
            var json = packet.PayloadJson;
            
            Console.WriteLine($"[WorldMap] Processing ElementChange packet, JSON length: {json.Length}");
            
            // Parse Changes array - handle nested arrays properly
            // Look for "Changes":[ and find matching ]
            int changesStart = json.IndexOf("\"Changes\"");
            if (changesStart < 0)
            {
                Console.WriteLine("[WorldMap] No 'Changes' field found in JSON");
                return;
            }
            
            int arrayStart = json.IndexOf('[', changesStart);
            if (arrayStart < 0) return;
            
            // Find matching bracket
            int depth = 1;
            int arrayEnd = arrayStart + 1;
            while (arrayEnd < json.Length && depth > 0)
            {
                if (json[arrayEnd] == '[') depth++;
                else if (json[arrayEnd] == ']') depth--;
                arrayEnd++;
            }
            
            var changesJson = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 2);
            
            // Match individual element changes (objects inside the array)
            var elementPattern = new Regex(@"\{[^{}]*\}", RegexOptions.Compiled);
            var elementMatches = elementPattern.Matches(changesJson);
            
            Console.WriteLine($"[WorldMap] Found {elementMatches.Count} element change entries");
            
            int gasCount = 0, liquidCount = 0;
            
            foreach (Match elemMatch in elementMatches)
            {
                var elemJson = elemMatch.Value;
                
                var cellMatch = Regex.Match(elemJson, @"""Cell""\s*:\s*(\d+)");
                var elementIdMatch = Regex.Match(elemJson, @"""ElementId""\s*:\s*(-?\d+)");
                var massMatch = Regex.Match(elemJson, @"""Mass""\s*:\s*([\d.]+)");
                var tempMatch = Regex.Match(elemJson, @"""Temperature""\s*:\s*([\d.]+)");
                var changeTypeMatch = Regex.Match(elemJson, @"""ChangeType""\s*:\s*(\d+)");
                var isGasMatch = Regex.Match(elemJson, @"""IsGas""\s*:\s*(true|false)", RegexOptions.IgnoreCase);
                
                if (!cellMatch.Success || !elementIdMatch.Success) continue;
                
                int cell = int.Parse(cellMatch.Groups[1].Value);
                int elementId = int.Parse(elementIdMatch.Groups[1].Value);
                float mass = massMatch.Success ? float.Parse(massMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0f;
                float temp = tempMatch.Success ? float.Parse(tempMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0f;
                int changeType = changeTypeMatch.Success ? int.Parse(changeTypeMatch.Groups[1].Value) : 0;
                
                // Parse IsGas and IsLiquid from JSON
                var isLiquidMatch = Regex.Match(elemJson, @"""IsLiquid""\s*:\s*(true|false)", RegexOptions.IgnoreCase);
                bool isGas = isGasMatch.Success && isGasMatch.Groups[1].Value.ToLower() == "true";
                bool isLiquid = isLiquidMatch.Success && isLiquidMatch.Groups[1].Value.ToLower() == "true";
                
                // Fallback to heuristic if neither field is present
                if (!isGasMatch.Success && !isLiquidMatch.Success)
                {
                    isGas = IsGasElement(elementId);
                    isLiquid = !isGas;
                }
                
                var data = new ElementCellData(elementId, mass, temp, isGas);
                
                if (changeType == 1) // Remove
                {
                    _gasCells.Remove(cell);
                    _liquidCells.Remove(cell);
                }
                else if (mass > 0.01f)
                {
                    if (isGas)
                    {
                        _gasCells[cell] = data;
                        gasCount++;
                    }
                    else if (isLiquid)
                    {
                        _liquidCells[cell] = data;
                        liquidCount++;
                    }
                }
            }
            
            _elementChangesReceived += elementMatches.Count;
            
            Console.WriteLine($"[WorldMap] Processed: {gasCount} gas cells, {liquidCount} liquid cells. Total stored: gas={_gasCells.Count}, liquid={_liquidCells.Count}");
            
            if (gasCount > 0 || liquidCount > 0)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    RequestMapRefresh();
                    WorldMapLastUpdate = System.DateTime.Now.ToString("HH:mm:ss");
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WorldMap] Error processing ElementChange: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Determine if an element ID is a gas (rough heuristic based on common ONI elements)
    /// </summary>
    private static bool IsGasElement(int elementId)
    {
        // Common gas element IDs in ONI (SimHashes)
        // This is a simplified check - in reality we'd need the full element table
        return elementId switch
        {
            1851740614 => true,   // Oxygen
            -1324664829 => true,  // CarbonDioxide  
            -858712596 => true,   // Hydrogen
            -1554872654 => true,  // ChlorineGas
            1887387588 => true,   // Methane
            -1528777920 => true,  // Steam
            -1908044868 => true,  // ContaminatedOxygen
            -1374542243 => true,  // SourGas
            -1858722091 => true,  // Helium
            -1406916018 => true,  // Propane
            _ => elementId < 0    // Fallback: negative IDs tend to be gases
        };
    }
    
    #endregion
}
