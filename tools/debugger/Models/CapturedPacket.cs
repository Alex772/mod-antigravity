using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Antigravity.Debugger.Models;

public class CapturedPacket
{
    public int Id { get; set; }
    public SystemDateTime Timestamp { get; set; }
    public PacketDirection Direction { get; set; }
    public byte[] RawData { get; set; } = Array.Empty<byte>();
    public string RawHex => BitConverter.ToString(RawData).Replace("-", " ");
    
    public int MessageType { get; set; }
    public string MessageTypeName { get; set; } = "Unknown";
    public ulong SenderId { get; set; }
    public long Tick { get; set; }
    public byte[] Payload { get; set; } = Array.Empty<byte>();
    public string PayloadJson { get; set; } = "";
    
    public int GameTick { get; set; }
    public int CommandType { get; set; }
    public string CommandTypeName => GameCommandTypes.GetName(CommandType);
    
    public string DirectionSymbol => Direction == PacketDirection.Received ? "←" : "→";
    public string TimeString => Timestamp.ToString("HH:mm:ss.fff");
    public string SizeString => RawData.Length < 1024 ? $"{RawData.Length} B" : $"{RawData.Length / 1024.0:F1} KB";
    public string DisplayName => CommandType > 0 ? CommandTypeName : MessageTypeName;
    
    public string PayloadJsonFormatted
    {
        get
        {
            if (string.IsNullOrEmpty(PayloadJson)) return "";
            try
            {
                using var doc = JsonDocument.Parse(PayloadJson);
                return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            }
            catch { return PayloadJson; }
        }
    }
}

public enum PacketDirection { Received, Sent }

public static class MessageTypes
{
    public static readonly Dictionary<int, string> Names = new()
    {
        { 1, "LobbyUpdate" }, { 10, "GameStarting" }, { 11, "WorldData" }, { 12, "WorldDataChunk" },
        { 13, "PlayerReady" }, { 14, "GameStart" }, { 20, "SyncCheck" }, { 21, "SyncResponse" },
        { 22, "SyncRequest" }, { 30, "Command" }, { 31, "CommandBroadcast" }, { 40, "Pause" },
        { 41, "Unpause" }, { 42, "SpeedChange" }, { 50, "Chat" }, { 51, "CursorUpdate" },
        { 60, "Ping" }, { 61, "Pong" }, { 62, "Error" }, { 63, "Disconnect" }
    };
    public static string GetName(int type) => Names.GetValueOrDefault(type, $"Unknown({type})");
}

public static class GameCommandTypes
{
    // Maps GameCommandType enum values to human-readable names
    // Must match GameCommandType in Antigravity.Core.Commands.GameCommands
    public static readonly Dictionary<int, string> Names = new()
    {
        // Building commands
        { 1, "Build" }, { 2, "CancelBuild" }, { 3, "Deconstruct" },
        // Digging commands
        { 10, "Dig" }, { 11, "CancelDig" },
        // Priority
        { 20, "SetPriority" },
        // Errand
        { 30, "SetErrand" }, { 31, "CancelErrand" },
        // Door
        { 40, "SetDoorState" },
        // Copy
        { 50, "CopySettings" },
        // Speed/Pause
        { 60, "SetGameSpeed" }, { 61, "PauseGame" }, { 62, "UnpauseGame" },
        // Save
        { 70, "SaveGame" },
        // Mop/Clear
        { 80, "Mop" }, { 81, "Clear" },
        // Harvest
        { 82, "Harvest" }, { 83, "CancelHarvest" },
        // Disinfect
        { 84, "Disinfect" },
        // Capture
        { 85, "Capture" }, { 86, "CancelCapture" },
        // Bulk Priority
        { 87, "SetBulkPriority" },
        // Building UI commands
        { 88, "SetBuildingPriority" }, { 89, "ToggleBuildingDisinfect" }, { 90, "SetStorageFilter" },
        // Utility
        { 91, "UtilityBuild" }, { 92, "DisconnectUtility" }, { 93, "SetStorageCapacity" },
        // Duplicant sync
        { 110, "ChoreStart" }, { 111, "ChoreEnd" }, { 112, "NavigateTo" },
        { 113, "DuplicantFullState" }, { 114, "DuplicantChecksum" }, { 115, "PositionSync" },
        { 116, "RandomSeedSync" }, { 117, "DuplicantCommandRequest" },
<<<<<<< HEAD
<<<<<<< HEAD
        // World sync
        { 118, "ItemSync" },
        { 119, "ElementChange" },
=======
>>>>>>> 3663ac0 (feat: Introduce network packet debugger and command synchronization for building settings and disconnects.)
=======
        // World sync
        { 118, "ItemSync" },
        { 119, "ElementChange" },
>>>>>>> 09d72f0 (falhando)
        // Generic
        { 100, "Custom" }
    };
    public static string GetName(int type) => Names.GetValueOrDefault(type, $"Cmd({type})");
}
