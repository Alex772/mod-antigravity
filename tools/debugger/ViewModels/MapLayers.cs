using System.ComponentModel;

namespace Antigravity.Debugger.ViewModels;

/// <summary>
/// Types of map layers available
/// </summary>
public enum MapLayerType
{
    Items,
    Duplicants,
    Gases,
    Liquids
}

/// <summary>
/// Represents a toggleable map layer
/// </summary>
public class MapLayer : INotifyPropertyChanged
{
    private bool _isVisible = true;
    private int _itemCount = 0;
    
    public MapLayerType Type { get; set; }
    public string Name { get; set; } = "";
    public string ColorHex { get; set; } = "#FFFFFF";
    public uint Color { get; set; } = 0xFFFFFFFF;
    
    public bool IsVisible 
    { 
        get => _isVisible; 
        set { _isVisible = value; OnPropertyChanged(nameof(IsVisible)); }
    }
    
    public int ItemCount 
    { 
        get => _itemCount; 
        set { _itemCount = value; OnPropertyChanged(nameof(ItemCount)); }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
