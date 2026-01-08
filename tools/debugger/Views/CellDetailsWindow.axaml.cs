using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Antigravity.Debugger.Views;

public partial class CellDetailsWindow : Window
{
    public CellDetailsWindow()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Initialize the window with cell data
    /// </summary>
    public void SetCellData(int cellX, int cellY, int cellId, CellData data)
    {
        CellCoordinates.Text = $"Cell ({cellX}, {cellY})";
        CellId.Text = $"Cell ID: {cellId}";
        
        // Item count
        ItemCount.Text = data.Items?.Count.ToString() ?? "0";
        
        // Gas info
        if (data.Gas != null)
        {
            GasInfo.Text = $"{data.Gas.Mass:F1} kg";
            GasSection.IsVisible = true;
            GasDetails.Text = $"Element ID: {data.Gas.ElementId}\n" +
                              $"Mass: {data.Gas.Mass:F2} kg\n" +
                              $"Temperature: {data.Gas.Temperature:F1} K ({data.Gas.Temperature - 273.15f:F1} °C)";
        }
        else
        {
            GasInfo.Text = "-";
            GasSection.IsVisible = false;
        }
        
        // Liquid info
        if (data.Liquid != null)
        {
            LiquidInfo.Text = $"{data.Liquid.Mass:F1} kg";
            LiquidSection.IsVisible = true;
            LiquidDetails.Text = $"Element ID: {data.Liquid.ElementId}\n" +
                                 $"Mass: {data.Liquid.Mass:F2} kg\n" +
                                 $"Temperature: {data.Liquid.Temperature:F1} K ({data.Liquid.Temperature - 273.15f:F1} °C)";
        }
        else
        {
            LiquidInfo.Text = "-";
            LiquidSection.IsVisible = false;
        }
        
        // Items
        if (data.Items != null && data.Items.Count > 0)
        {
            ItemsSection.IsVisible = true;
            ItemsList.ItemsSource = data.Items;
        }
        else
        {
            ItemsSection.IsVisible = false;
        }
        
        // Empty message if no data
        EmptyMessage.IsVisible = data.Gas == null && data.Liquid == null && (data.Items == null || data.Items.Count == 0);
    }
    
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// Cell data for display in CellDetailsWindow
/// </summary>
public class CellData
{
    public ElementInfo? Gas { get; set; }
    public ElementInfo? Liquid { get; set; }
    public List<string>? Items { get; set; }
}

/// <summary>
/// Element info (gas or liquid)
/// </summary>
public class ElementInfo
{
    public int ElementId { get; set; }
    public float Mass { get; set; }
    public float Temperature { get; set; }
}
