using System;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Antigravity.Debugger.Models;

namespace Antigravity.Debugger.ViewModels;

/// <summary>
/// Duplicants tab functionality
/// </summary>
public partial class MainWindowViewModel
{
    #region Duplicants Tab
    
    partial void OnSelectedDuplicantChanged(DuplicantInfo? value)
    {
        if (value == null)
        {
            DuplicantDetailsName = "-";
            DuplicantDetailsPosition = "-";
            DuplicantDetailsChore = "-";
            DuplicantDetailsStatus = "-";
            DuplicantChores.Clear();
            return;
        }
        
        DuplicantDetailsName = value.DisplayName;
        DuplicantDetailsPosition = value.Position;
        DuplicantDetailsChore = string.IsNullOrEmpty(value.CurrentChore) ? "Idle" : value.CurrentChore;
        DuplicantDetailsStatus = value.Status;
        
        DuplicantChores.Clear();
        foreach (var chore in value.ChoreHistory)
        {
            DuplicantChores.Add(chore);
        }
    }
    
    /// <summary>
    /// Process duplicant-related commands from captured packets.
    /// </summary>
    private void ProcessDuplicantCommand(CapturedPacket packet)
    {
        if (packet.CommandTypeName == null) return;
        
        if (packet.CommandTypeName == "ChoreStart" || 
            packet.CommandTypeName == "PositionSync" ||
            packet.CommandTypeName == "DuplicantChecksum")
        {
            UpdateDuplicantFromPacket(packet);
        }
    }
    
    /// <summary>
    /// Update or add duplicant info from packet data.
    /// </summary>
    private void UpdateDuplicantFromPacket(CapturedPacket packet)
    {
        if (packet.PayloadJson == null) return;
        
        try
        {
            var json = packet.PayloadJson;
            string? dupName = null;
            string? choreType = null;
            string? choreGroup = null;
            string? targetPrefabId = null;
            int targetCell = 0;
            float targetX = 0, targetY = 0;
            int cellX = 0, cellY = 0;
            
            if (json.Contains("DuplicantName"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(json, "\"DuplicantName\"\\s*:\\s*\"([^\"]+)\"");
                if (match.Success) dupName = match.Groups[1].Value;
            }
            
            if (json.Contains("ChoreTypeId") || json.Contains("CurrentChoreTypeId"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(json, "\"(?:ChoreTypeId|CurrentChoreTypeId)\"\\s*:\\s*\"([^\"]+)\"");
                if (match.Success) choreType = match.Groups[1].Value;
            }
            
            if (json.Contains("ChoreGroupId") || json.Contains("CurrentChoreGroupId"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(json, "\"(?:ChoreGroupId|CurrentChoreGroupId)\"\\s*:\\s*\"([^\"]+)\"");
                if (match.Success) choreGroup = match.Groups[1].Value;
            }
            
            if (json.Contains("TargetPrefabId"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(json, "\"TargetPrefabId\"\\s*:\\s*\"([^\"]+)\"");
                if (match.Success) targetPrefabId = match.Groups[1].Value;
            }
            
            if (json.Contains("TargetPositionX"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(json, "\"TargetPositionX\"\\s*:\\s*([\\d.]+)");
                if (match.Success) float.TryParse(match.Groups[1].Value, out targetX);
            }
            
            if (json.Contains("TargetPositionY"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(json, "\"TargetPositionY\"\\s*:\\s*([\\d.]+)");
                if (match.Success) float.TryParse(match.Groups[1].Value, out targetY);
            }
            
            if (json.Contains("CurrentCell") || json.Contains("TargetCell"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(json, "\"(CurrentCell|TargetCell)\"\\s*:\\s*(\\d+)");
                if (match.Success)
                {
                    targetCell = int.Parse(match.Groups[2].Value);
                    cellX = targetCell % 256;
                    cellY = targetCell / 256;
                }
            }
            
            if (string.IsNullOrEmpty(dupName)) return;
            
            DuplicantChoreInfo? choreInfo = null;
            if (!string.IsNullOrEmpty(choreType) && packet.CommandTypeName == "ChoreStart")
            {
                choreInfo = new DuplicantChoreInfo
                {
                    ChoreType = choreType,
                    ChoreGroup = choreGroup ?? "",
                    TargetCell = targetCell,
                    TargetX = targetX,
                    TargetY = targetY,
                    TargetPrefabId = targetPrefabId ?? "",
                    Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                };
            }
            
            Dispatcher.UIThread.Post(() =>
            {
                var existing = Duplicants.FirstOrDefault(d => d.Name == dupName);
                if (existing != null)
                {
                    existing.CellX = cellX;
                    existing.CellY = cellY;
                    existing.CurrentChore = choreType ?? existing.CurrentChore;
                    existing.LastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    existing.Status = "Synced";
                    
                    if (choreInfo != null)
                    {
                        existing.ChoreHistory.Insert(0, choreInfo);
                        while (existing.ChoreHistory.Count > 20)
                            existing.ChoreHistory.RemoveAt(existing.ChoreHistory.Count - 1);
                    }
                    
                    var idx = Duplicants.IndexOf(existing);
                    if (idx >= 0)
                    {
                        Duplicants.Remove(existing);
                        Duplicants.Insert(idx, existing);
                    }
                }
                else
                {
                    var newDup = new DuplicantInfo
                    {
                        Name = dupName,
                        CellX = cellX,
                        CellY = cellY,
                        CurrentChore = choreType ?? "Idle",
                        LastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                        Status = "Synced"
                    };
                    
                    if (choreInfo != null)
                        newDup.ChoreHistory.Add(choreInfo);
                        
                    Duplicants.Add(newDup);
                }
                
                if (SelectedDuplicant?.Name == dupName)
                {
                    OnSelectedDuplicantChanged(SelectedDuplicant);
                }
                
                // Trigger map refresh if on World Map tab
                RequestMapRefresh();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing duplicant data: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void ClearDuplicants()
    {
        Duplicants.Clear();
        DuplicantChores.Clear();
        SelectedDuplicant = null;
    }
    
    #endregion
}
