using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Antigravity.Debugger.Models;
using Antigravity.Debugger.Services;

namespace Antigravity.Debugger.ViewModels;

/// <summary>
/// Main ViewModel - contains properties and constructor.
/// Methods are split into partial classes:
/// - MainWindowViewModel.Packets.cs - Packet processing, replay, save
/// - MainWindowViewModel.GroupBuilder.cs - Group builder, JSON editing
/// - MainWindowViewModel.WorldData.cs - World data capture and parsing
/// - MainWindowViewModel.WorldMap.cs - World map rendering
/// - MainWindowViewModel.Duplicants.cs - Duplicant tracking
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    #region Private Fields
    
    private readonly NetworkService _networkService;
    private readonly CommandStorageService _commandStorage;
    private readonly DispatcherTimer _updateTimer;
    private readonly DispatcherTimer _groupingTimer;
    private Session _currentSession;
    private readonly List<CapturedPacket> _pendingPackets = new();
    private readonly object _pendingLock = new();
    
    // World Data storage
    private readonly Dictionary<int, byte[]> _worldDataChunks = new();
    private byte[]? _completeWorldData = null;
    private byte[]? _decompressedWorldData = null;
    private ParsedSaveData? _parsedSaveData = null;
    
    // World Map dimensions
    private int _worldWidth = 256;
    private int _worldHeight = 384;
    private Dictionary<int, List<string>> _cellItemDetails = new();
    
    #endregion
    
    #region Connection Properties
    
    [ObservableProperty] private string _hostAddress = "127.0.0.1";
    [ObservableProperty] private int _hostPort = 7777;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _connectionStatus = "Disconnected";
    [ObservableProperty] private string _statsText = "↓0 ↑0";
    
    #endregion
    
    #region Selection Properties
    
    [ObservableProperty] private CapturedPacket? _selectedPacket;
    [ObservableProperty] private PacketGroup? _selectedGroup;
    [ObservableProperty] private SavedCommand? _selectedSavedCommand;
    [ObservableProperty] private int _selectedTabIndex = 0;
    
    #endregion
    
    #region Group Builder Properties
    
    [ObservableProperty] private string _newGroupName = "";
    [ObservableProperty] private SavedPacketData? _selectedPacketInGroup;
    [ObservableProperty] private string _editingPacketJson = "";
    [ObservableProperty] private bool _isEditingPacket = false;
    [ObservableProperty] private string _jsonEditError = "";
    
    #endregion
    
    #region World Data Tab Properties
    
    [ObservableProperty] private int _totalPacketsReceived = 0;
    [ObservableProperty] private string _totalBytesReceived = "0 B";
    [ObservableProperty] private long _lastGameTick = 0;
    [ObservableProperty] private string _sessionStartTime = "-";
    [ObservableProperty] private bool _isReceivingWorldData = false;
    [ObservableProperty] private string _worldDataStatus = "Waiting for world data...";
    [ObservableProperty] private string _worldDataColonyName = "-";
    [ObservableProperty] private int _worldDataTotalSize = 0;
    [ObservableProperty] private int _worldDataReceivedSize = 0;
    [ObservableProperty] private int _worldDataChunksTotal = 0;
    [ObservableProperty] private int _worldDataChunksReceived = 0;
    [ObservableProperty] private string _worldDataProgress = "0%";
    [ObservableProperty] private bool _hasWorldData = false;
    [ObservableProperty] private string _worldDataCompressedSize = "-";
    [ObservableProperty] private string _worldDataDecompressedSize = "-";
    [ObservableProperty] private string _worldDataCompressionRatio = "-";
    [ObservableProperty] private string _worldDataPreview = "No data loaded. Connect to a host and load a save to capture world data.";
    
    // Parsed Save Info
    [ObservableProperty] private string _saveBuildVersion = "-";
    [ObservableProperty] private string _saveCycles = "-";
    [ObservableProperty] private string _saveDuplicants = "-";
    [ObservableProperty] private string _saveWorldSize = "-";
    [ObservableProperty] private string _saveClusterId = "-";
    [ObservableProperty] private string _saveTraits = "-";
    [ObservableProperty] private string _saveSandbox = "-";
    [ObservableProperty] private string _saveVersion = "-";
    
    #endregion
    
    #region Duplicants Tab Properties
    
    [ObservableProperty] private DuplicantInfo? _selectedDuplicant;
    [ObservableProperty] private string _duplicantDetailsName = "-";
    [ObservableProperty] private string _duplicantDetailsPosition = "-";
    [ObservableProperty] private string _duplicantDetailsChore = "-";
    [ObservableProperty] private string _duplicantDetailsStatus = "-";
    
    #endregion
    
    #region World Map Tab Properties
    
    [ObservableProperty] private string _worldMapStatus = "No data received";
    [ObservableProperty] private string _worldSizeDisplay = "- x -";
    [ObservableProperty] private int _totalItemsCount = 0;
    [ObservableProperty] private int _cellsWithItemsCount = 0;
    [ObservableProperty] private string _worldMapLastUpdate = "-";
    [ObservableProperty] private double _cellSize = 2.0;
    [ObservableProperty] private double _canvasWidth = 512;
    [ObservableProperty] private double _canvasHeight = 768;
    [ObservableProperty] private double _zoomLevel = 1.0;
    [ObservableProperty] private Avalonia.Media.Imaging.WriteableBitmap? _worldMapBitmap;
    
    // Cell selection and hover
    [ObservableProperty] private string _hoveredCellInfo = "";
    [ObservableProperty] private string _selectedCellInfo = "Click on a cell to see details";
    [ObservableProperty] private int _selectedCellX = -1;
    [ObservableProperty] private int _selectedCellY = -1;
    [ObservableProperty] private int _selectedCellItemCount = 0;
    [ObservableProperty] private ObservableCollection<string> _selectedCellItems = new();
    
    // Filters
    [ObservableProperty] private string _filterSearchText = "";
    
    #endregion
    
    #region Observable Collections
    
    public ObservableCollection<PacketGroup> PacketGroups { get; } = new();
    public ObservableCollection<CapturedPacket> Packets { get; } = new();
    public ObservableCollection<SavedCommand> SavedCommands { get; } = new();
    public ObservableCollection<SavedCommand> CommandsForGroup { get; } = new();
    public ObservableCollection<SavedPacketData> PacketsForGroup { get; } = new();
    public ObservableCollection<KeyValuePair<string, int>> CommandTypeStats { get; } = new();
    
    // Duplicants
    public ObservableCollection<DuplicantInfo> Duplicants { get; } = new();
    public ObservableCollection<DuplicantChoreInfo> DuplicantChores { get; } = new();
    
    // World Map
    public ObservableCollection<WorldMapCellViewModel> WorldMapCells { get; } = new();
    public ObservableCollection<FilterItem> ItemFilters { get; } = new();
    public ObservableCollection<MapLayer> MapLayers { get; } = new()
    {
        new MapLayer { Type = MapLayerType.Items, Name = "Items", ColorHex = "#89b4fa", Color = 0xFFfab489, IsVisible = true },
        new MapLayer { Type = MapLayerType.Duplicants, Name = "Duplicants", ColorHex = "#fab387", Color = 0xFF87b3fa, IsVisible = true },
        new MapLayer { Type = MapLayerType.Gases, Name = "Gases", ColorHex = "#00FFFF", Color = 0xFFFFFF00, IsVisible = true },
        new MapLayer { Type = MapLayerType.Liquids, Name = "Liquids", ColorHex = "#0088FF", Color = 0xFFFF8800, IsVisible = true }
    };
    
    #endregion
    
    #region Constructor
    
    public MainWindowViewModel()
    {
        _networkService = new NetworkService();
        _commandStorage = new CommandStorageService();
        _currentSession = new Session();
        
        // Connection events
        _networkService.Connected += (s, e) => Dispatcher.UIThread.Post(() => {
            IsConnected = true;
            ConnectionStatus = $"Connected to {_networkService.ConnectedAddress}";
            _currentSession = new Session { HostAddress = _networkService.ConnectedAddress ?? "" };
            SessionStartTime = System.DateTime.Now.ToString("HH:mm:ss");
            TotalPacketsReceived = 0;
            TotalBytesReceived = "0 B";
            LastGameTick = 0;
            CommandTypeStats.Clear();
        });
        
        _networkService.Disconnected += (s, reason) => Dispatcher.UIThread.Post(() => {
            IsConnected = false;
            ConnectionStatus = $"Disconnected: {reason}";
        });
        
        _networkService.PacketReceived += (s, packet) => Dispatcher.UIThread.Post(() => {
            AddPacket(packet);
            _currentSession.Stats.PacketsReceived++;
            _currentSession.Stats.BytesReceived += packet.RawData.Length;
            UpdateStats();
            UpdateWorldDataStats(packet);
        });
        
        _networkService.PacketSent += (s, packet) => Dispatcher.UIThread.Post(() => {
            AddPacket(packet);
            _currentSession.Stats.PacketsSent++;
            _currentSession.Stats.BytesSent += packet.RawData.Length;
            UpdateStats();
        });
        
        _networkService.Error += (s, error) => Dispatcher.UIThread.Post(() => ConnectionStatus = $"Error: {error}");
        
        // Update timers
        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _updateTimer.Tick += (s, e) => _networkService.Update();
        _updateTimer.Start();
        
        _groupingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _groupingTimer.Tick += (s, e) => ProcessPendingPackets();
        _groupingTimer.Start();
        
        // Load saved commands on startup
        LoadSavedCommands();
    }
    
<<<<<<< HEAD
    #endregion
=======
    private void AddPacket(CapturedPacket packet)
    {
        Packets.Insert(0, packet);
        _currentSession.Packets.Add(packet);
        lock (_pendingLock) { _pendingPackets.Add(packet); }
    }
    
    private void ProcessPendingPackets()
    {
        List<CapturedPacket> packetsToProcess;
        lock (_pendingLock)
        {
            if (_pendingPackets.Count == 0) return;
            packetsToProcess = new List<CapturedPacket>(_pendingPackets);
            _pendingPackets.Clear();
        }
        
        var groups = packetsToProcess.GroupBy(p => p.GameTick > 0 ? p.GameTick : p.TimeString.GetHashCode());
        foreach (var group in groups.OrderByDescending(g => g.Key))
            PacketGroups.Insert(0, PacketGroup.FromPackets(group));
    }
    
    private void LoadSavedCommands()
    {
        SavedCommands.Clear();
        foreach (var cmd in _commandStorage.LoadAllCommands())
            SavedCommands.Add(cmd);
    }
    
    [RelayCommand]
    private void ToggleConnection()
    {
        if (IsConnected) _networkService.Disconnect();
        else { ConnectionStatus = "Connecting..."; _networkService.Connect(HostAddress, HostPort); }
    }
    
    [RelayCommand]
    private void ReplaySelected()
    {
        if (SelectedPacket == null || !IsConnected) return;
        _networkService.SendRaw(SelectedPacket.RawData);
        ConnectionStatus = $"Replayed: {SelectedPacket.CommandTypeName}";
    }
    
    [RelayCommand]
    private void ReplayGroup()
    {
        if (SelectedGroup == null || !IsConnected) return;
        foreach (var packet in SelectedGroup.Packets.OrderBy(p => p.Id))
            _networkService.SendRaw(packet.RawData);
        ConnectionStatus = $"Replayed group: {SelectedGroup.MainCommandName} ({SelectedGroup.PacketCount} packets)";
    }
    
    [RelayCommand]
    private void SaveSelectedPacket()
    {
        if (SelectedPacket == null) return;
        _commandStorage.SavePacket(SelectedPacket);
        LoadSavedCommands();
        ConnectionStatus = $"Saved: {SelectedPacket.CommandTypeName}";
    }
    
    [RelayCommand]
    private void SaveSelectedGroup()
    {
        if (SelectedGroup == null) return;
        _commandStorage.SaveGroup(SelectedGroup);
        LoadSavedCommands();
        ConnectionStatus = $"Saved group: {SelectedGroup.MainCommandName} ({SelectedGroup.PacketCount} packets)";
    }
    
    [RelayCommand]
    private void ReplaySavedCommand()
    {
        if (SelectedSavedCommand == null || !IsConnected) return;
        foreach (var packet in SelectedSavedCommand.Packets.OrderBy(p => p.Order))
            _networkService.SendRaw(packet.RawData);
        ConnectionStatus = $"Replayed saved: {SelectedSavedCommand.DisplayName} ({SelectedSavedCommand.PacketCount} packets)";
    }
    
    [RelayCommand]
    private void DeleteSavedCommand()
    {
        if (SelectedSavedCommand == null) return;
        var files = _commandStorage.GetSavedCommands();
        var file = files.FirstOrDefault(f => f.Contains(SelectedSavedCommand.Id) || 
            f.Contains($"{SelectedSavedCommand.CommandType}_{SelectedSavedCommand.CreatedAt:yyyy-MM-dd_HH-mm-ss}"));
        if (file != null) _commandStorage.DeleteCommand(file);
        LoadSavedCommands();
        SelectedSavedCommand = null;
        ConnectionStatus = "Deleted saved command";
    }
    
    [RelayCommand]
    private void RefreshSavedCommands() => LoadSavedCommands();
    
    [RelayCommand]
    private void ClearPackets()
    {
        Packets.Clear();
        PacketGroups.Clear();
        _currentSession = new Session { HostAddress = _networkService.ConnectedAddress ?? "" };
        UpdateStats();
    }
    
    /// <summary>
    /// Add selected command's packets to the group builder.
    /// </summary>
    [RelayCommand]
    private void AddToGroupBuilder()
    {
        if (SelectedSavedCommand == null) return;
        
        // Add all packets from this command
        foreach (var packet in SelectedSavedCommand.Packets)
        {
            var newPacket = new SavedPacketData
            {
                Order = PacketsForGroup.Count,
                CommandTypeName = packet.CommandTypeName,
                RawData = packet.RawData,
                PayloadJson = packet.PayloadJson,
                GameTick = packet.GameTick
            };
            PacketsForGroup.Add(newPacket);
        }
        
        if (!CommandsForGroup.Contains(SelectedSavedCommand))
            CommandsForGroup.Add(SelectedSavedCommand);
            
        ConnectionStatus = $"Added {SelectedSavedCommand.PacketCount} packets ({PacketsForGroup.Count} total)";
    }
    
    /// <summary>
    /// Remove a specific packet from the group builder.
    /// </summary>
    [RelayCommand]
    private void RemovePacketFromGroup(SavedPacketData? packet)
    {
        if (packet != null && PacketsForGroup.Contains(packet))
        {
            PacketsForGroup.Remove(packet);
            ReorderPackets();
            ConnectionStatus = $"Removed packet ({PacketsForGroup.Count} remaining)";
        }
    }
    
    /// <summary>
    /// Move packet up in the list.
    /// </summary>
    [RelayCommand]
    private void MovePacketUp(SavedPacketData? packet)
    {
        if (packet == null) return;
        var index = PacketsForGroup.IndexOf(packet);
        if (index > 0)
        {
            PacketsForGroup.Move(index, index - 1);
            ReorderPackets();
        }
    }
    
    /// <summary>
    /// Move packet down in the list.
    /// </summary>
    [RelayCommand]
    private void MovePacketDown(SavedPacketData? packet)
    {
        if (packet == null) return;
        var index = PacketsForGroup.IndexOf(packet);
        if (index < PacketsForGroup.Count - 1)
        {
            PacketsForGroup.Move(index, index + 1);
            ReorderPackets();
        }
    }
    
    private void ReorderPackets()
    {
        for (int i = 0; i < PacketsForGroup.Count; i++)
            PacketsForGroup[i].Order = i;
    }
    
    /// <summary>
    /// Remove a command from the group builder list (and its packets).
    /// </summary>
    [RelayCommand]
    private void RemoveFromGroupBuilder(SavedCommand? command)
    {
        if (command != null && CommandsForGroup.Contains(command))
        {
            CommandsForGroup.Remove(command);
            ConnectionStatus = $"Removed from group ({CommandsForGroup.Count} commands)";
        }
    }
    
    /// <summary>
    /// Clear the group builder list.
    /// </summary>
    [RelayCommand]
    private void ClearGroupBuilder()
    {
        CommandsForGroup.Clear();
        PacketsForGroup.Clear();
        NewGroupName = "";
        SelectedPacketInGroup = null;
        ConnectionStatus = "Group builder cleared";
    }
    
    /// <summary>
    /// Create a new group from the packets in PacketsForGroup.
    /// </summary>
    [RelayCommand]
    private void CreateGroup()
    {
        if (PacketsForGroup.Count == 0) return;
        
        var newGroup = new SavedCommand
        {
            Name = string.IsNullOrWhiteSpace(NewGroupName) ? $"Custom Group ({PacketsForGroup.Count} packets)" : NewGroupName,
            CommandType = "Group",
            IsGroup = true,
            SourceCommandIds = CommandsForGroup.Select(c => c.Id).ToList(),
            GameTick = PacketsForGroup.First().GameTick,
            Packets = PacketsForGroup.Select((p, i) => new SavedPacketData
            {
                Order = i,
                CommandTypeName = p.CommandTypeName,
                RawData = p.RawData,
                PayloadJson = p.PayloadJson,
                GameTick = p.GameTick
            }).ToList()
        };
        
        _commandStorage.SaveCommand(newGroup);
        LoadSavedCommands();
        CommandsForGroup.Clear();
        PacketsForGroup.Clear();
        NewGroupName = "";
        SelectedPacketInGroup = null;
        ConnectionStatus = $"Created group: {newGroup.DisplayName} ({newGroup.PacketCount} packets)";
    }
    
    /// <summary>
    /// Start editing the selected packet's JSON.
    /// </summary>
    [RelayCommand]
    private void StartEditPacket(SavedPacketData? packet)
    {
        if (packet == null) return;
        SelectedPacketInGroup = packet;
        EditingPacketJson = FormatJson(packet.PayloadJson);
        IsEditingPacket = true;
        JsonEditError = "";
        ConnectionStatus = $"Editing: {packet.CommandTypeName}";
    }
    
    /// <summary>
    /// Apply the edited JSON to the selected packet.
    /// </summary>
    [RelayCommand]
    private void ApplyJsonChanges()
    {
        if (SelectedPacketInGroup == null || string.IsNullOrWhiteSpace(EditingPacketJson)) return;
        
        try
        {
            // Validate JSON
            var jsonDoc = System.Text.Json.JsonDocument.Parse(EditingPacketJson);
            var compactJson = System.Text.Json.JsonSerializer.Serialize(jsonDoc);
            
            // RawData format: [1b MessageType] + [8b SenderId] + [8b GameTick] + [4b PayloadLength] + [Payload]
            // Header = 21 bytes, we need to preserve the first 17 bytes and rebuild from PayloadLength
            var oldRawData = SelectedPacketInGroup.RawData;
            if (oldRawData.Length < 21)
            {
                JsonEditError = "Invalid packet format - too short";
                return;
            }
            
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(compactJson);
            
            // Build new RawData: preserve header (17 bytes) + new length (4 bytes) + new JSON
            var newRawData = new byte[17 + 4 + jsonBytes.Length];
            
            // Copy header (MessageType + SenderId + GameTick)
            Array.Copy(oldRawData, 0, newRawData, 0, 17);
            
            // Write new payload length
            BitConverter.GetBytes(jsonBytes.Length).CopyTo(newRawData, 17);
            
            // Write new JSON payload
            jsonBytes.CopyTo(newRawData, 21);
            
            // Update packet
            SelectedPacketInGroup.PayloadJson = compactJson;
            SelectedPacketInGroup.RawData = newRawData;
            
            // Refresh the list to show updated size
            var index = PacketsForGroup.IndexOf(SelectedPacketInGroup);
            if (index >= 0)
            {
                PacketsForGroup.RemoveAt(index);
                PacketsForGroup.Insert(index, SelectedPacketInGroup);
            }
            
            IsEditingPacket = false;
            JsonEditError = "";
            ConnectionStatus = $"Applied changes to {SelectedPacketInGroup.CommandTypeName} ({newRawData.Length} B)";
        }
        catch (System.Text.Json.JsonException ex)
        {
            JsonEditError = $"Invalid JSON: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Cancel editing and discard changes.
    /// </summary>
    [RelayCommand]
    private void CancelEditPacket()
    {
        IsEditingPacket = false;
        EditingPacketJson = "";
        JsonEditError = "";
        ConnectionStatus = "Edit cancelled";
    }
    
    private string FormatJson(string json)
    {
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(json);
            return System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }
    
    private void UpdateStats() => StatsText = $"↓{_currentSession.Stats.PacketsReceived} ↑{_currentSession.Stats.PacketsSent}";
>>>>>>> 3663ac0 (feat: Introduce network packet debugger and command synchronization for building settings and disconnects.)
}
