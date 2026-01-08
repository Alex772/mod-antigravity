using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Antigravity.Debugger.ViewModels;

/// <summary>
/// Converter to multiply value by a factor for Canvas positioning
/// </summary>
public class MultiplyConverter : IValueConverter
{
    public double Factor { get; set; } = 1.0;
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intVal)
            return intVal * Factor;
        if (value is double doubleVal)
            return doubleVal * Factor;
        return 0;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// ViewModel for a single cell in the world map
/// </summary>
public class WorldMapCellViewModel
{
    public int Cell { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int ItemCount { get; set; }
    
    // Pre-computed canvas positions (adjusted by cell size factor)
    public double CanvasLeft => X * 2.0;
    public double CanvasTop => Y * 2.0;
    
    // Visual properties
    public string CellColor => ItemCount switch
    {
        0 => "#1e1e2e",      // Empty - dark
        1 => "#89b4fa",      // 1 item - blue
        <= 5 => "#a6e3a1",   // Few - green
        <= 10 => "#f9e2af",  // Some - yellow
        _ => "#f38ba8"       // Many - red
    };
    
    public string ToolTip => ItemCount > 0 
        ? $"Cell ({X}, {Y})\n{ItemCount} items" 
        : $"Cell ({X}, {Y})\nEmpty";
}
