using Avalonia.Controls;
using Avalonia.Input;
using Antigravity.Debugger.ViewModels;

namespace Antigravity.Debugger.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Handle Ctrl+Scroll for map zoom
    /// </summary>
    private void OnMapPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.AdjustZoom(e.Delta.Y);
                e.Handled = true;
            }
        }
    }
    
    /// <summary>
    /// Handle mouse move for hover info
    /// </summary>
    private void OnMapPointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && sender is ScrollViewer scrollViewer)
        {
            var image = scrollViewer.Content as Image;
            if (image != null)
            {
                // GetPosition(image) returns position in image's transformed coords
                // The image has ScaleTransform, so coords are already visual coords
                // We need to convert back to original bitmap coords
                var pos = e.GetPosition(image);
                double bitmapX = pos.X / vm.ZoomLevel;
                double bitmapY = pos.Y / vm.ZoomLevel;
                vm.UpdateHoverInfo(bitmapX, bitmapY);
            }
        }
    }
    
    /// <summary>
    /// Handle click for cell selection
    /// </summary>
    private void OnMapPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && sender is ScrollViewer scrollViewer)
        {
            var image = scrollViewer.Content as Image;
            if (image != null)
            {
                var pos = e.GetPosition(image);
                double bitmapX = pos.X / vm.ZoomLevel;
                double bitmapY = pos.Y / vm.ZoomLevel;
                vm.SelectCellAtPosition(bitmapX, bitmapY);
                
                // Open details window on double-click
                var point = e.GetCurrentPoint(image);
                if (point.Properties.IsLeftButtonPressed && e.ClickCount == 2)
                {
                    OpenCellDetailsWindow(vm);
                }
            }
        }
    }
    
    /// <summary>
    /// Open the cell details window with selected cell data
    /// </summary>
    private void OpenCellDetailsWindow(MainWindowViewModel vm)
    {
        var cellData = vm.GetLastSelectedCellData();
        if (cellData == null) return;
        
        var window = new CellDetailsWindow();
        window.SetCellData(cellData.Value.X, cellData.Value.Y, cellData.Value.Id, cellData.Value.Data);
        window.Show(this);
    }
}
