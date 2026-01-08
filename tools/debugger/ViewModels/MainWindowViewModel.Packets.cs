using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Antigravity.Debugger.Models;
using Antigravity.Debugger.Services;

namespace Antigravity.Debugger.ViewModels;

/// <summary>
/// Packet handling - AddPacket, ProcessPendingPackets, Replay, Save commands
/// </summary>
public partial class MainWindowViewModel
{
    #region Packet Processing
    
    private void AddPacket(CapturedPacket packet)
    {
        Packets.Insert(0, packet);
        _currentSession.Packets.Add(packet);
        lock (_pendingLock) { _pendingPackets.Add(packet); }
        
        // Process duplicant-related commands for Duplicants tab
        ProcessDuplicantCommand(packet);
        
        // Process ItemSync for World Map tab
        if (packet.CommandTypeName == "ItemSync")
        {
            ProcessItemSyncCommand(packet);
        }
        
        // Process ElementChange for gas/liquid layers
        if (packet.CommandTypeName == "ElementChange")
        {
            ProcessElementChangeCommand(packet);
        }
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
    
    #endregion
    
    #region Connection Commands
    
    [RelayCommand]
    private void ToggleConnection()
    {
        if (IsConnected) _networkService.Disconnect();
        else { ConnectionStatus = "Connecting..."; _networkService.Connect(HostAddress, HostPort); }
    }
    
    #endregion
    
    #region Replay Commands
    
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
    
    #endregion
    
    #region Save Commands
    
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
    
    #endregion
}
