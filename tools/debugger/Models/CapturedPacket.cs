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
    public static readonly Dictionary<int, string> Names = new()
    {
        { 10, "Dig" }, { 11, "CancelDig" }, { 20, "Build" }, { 21, "CancelBuild" }, { 22, "UtilityBuild" },
        { 30, "Deconstruct" }, { 31, "CancelDeconstruct" }, { 40, "Mop" }, { 41, "CancelMop" },
        { 50, "Clear" }, { 51, "CancelClear" }, { 60, "Harvest" }, { 61, "CancelHarvest" },
        { 70, "Disinfect" }, { 71, "CancelDisinfect" }, { 80, "Capture" }, { 81, "CancelCapture" },
        { 85, "PauseGame" }, { 86, "UnpauseGame" }, { 87, "SetGameSpeed" }, { 88, "SetBuildingPriority" },
        { 89, "SetStorageFilter" }, { 90, "SetDoorState" }, { 91, "PositionSync" }, { 92, "NavigateTo" },
        { 93, "ChoreStart" }, { 94, "DuplicantChecksum" }
    };
    public static string GetName(int type) => Names.GetValueOrDefault(type, $"Cmd({type})");
}
