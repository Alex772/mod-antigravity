using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Antigravity.Debugger.Models;

namespace Antigravity.Debugger.ViewModels;

/// <summary>
/// Group Builder functionality - create, edit, and manage custom packet groups
/// </summary>
public partial class MainWindowViewModel
{
    #region Group Builder
    
    /// <summary>
    /// Add selected command's packets to the group builder.
    /// </summary>
    [RelayCommand]
    private void AddToGroupBuilder()
    {
        if (SelectedSavedCommand == null) return;
        
        if (!CommandsForGroup.Contains(SelectedSavedCommand))
            CommandsForGroup.Add(SelectedSavedCommand);
        
        foreach (var packet in SelectedSavedCommand.Packets.OrderBy(p => p.Order))
        {
            var packetData = new SavedPacketData
            {
                Order = PacketsForGroup.Count,
                CommandTypeName = packet.CommandTypeName,
                RawData = packet.RawData,
                PayloadJson = packet.PayloadJson,
                GameTick = packet.GameTick
            };
            PacketsForGroup.Add(packetData);
        }
        
        ConnectionStatus = $"Added {SelectedSavedCommand.DisplayName} to group builder";
    }
    
    /// <summary>
    /// Remove a specific packet from the group builder.
    /// </summary>
    [RelayCommand]
    private void RemovePacketFromGroup(SavedPacketData? packet)
    {
        if (packet == null) return;
        PacketsForGroup.Remove(packet);
        
        // Reorder remaining packets
        for (int i = 0; i < PacketsForGroup.Count; i++)
            PacketsForGroup[i].Order = i;
        
        ConnectionStatus = $"Removed {packet.CommandTypeName} from group";
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
        if (index >= 0 && index < PacketsForGroup.Count - 1)
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
    private void RemoveFromGroupBuilder()
    {
        if (SelectedSavedCommand == null) return;
        CommandsForGroup.Remove(SelectedSavedCommand);
        var toRemove = PacketsForGroup.Where(p => 
            SelectedSavedCommand.Packets.Any(sp => sp.RawData.SequenceEqual(p.RawData))).ToList();
        foreach (var p in toRemove) PacketsForGroup.Remove(p);
        ReorderPackets();
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
    
    #endregion
    
    #region JSON Editing
    
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
    
    #endregion
}
