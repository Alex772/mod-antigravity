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
    
    #endregion
}
