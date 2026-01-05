using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Antigravity.Debugger.Models;

namespace Antigravity.Debugger.Services;

public class CommandStorageService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    
    private readonly string _commandsPath;
    
    public CommandStorageService()
    {
        var appDir = AppContext.BaseDirectory;
        _commandsPath = Path.Combine(appDir, "data", "commands");
        Directory.CreateDirectory(_commandsPath);
    }
    
    /// <summary>
    /// Save a single packet as a command.
    /// </summary>
    public string SavePacket(CapturedPacket packet, string name = "")
    {
        var saved = new SavedCommand
        {
            Name = name,
            CommandType = packet.CommandTypeName,
            GameTick = packet.GameTick,
            Packets = new List<SavedPacketData>
            {
                new SavedPacketData
                {
                    Order = 0,
                    CommandTypeName = packet.CommandTypeName,
                    RawData = packet.RawData,
                    PayloadJson = packet.PayloadJson
                }
            }
        };
        
        return SaveCommand(saved);
    }
    
    /// <summary>
    /// Save a group of packets as a command.
    /// </summary>
    public string SaveGroup(PacketGroup group, string name = "")
    {
        var saved = new SavedCommand
        {
            Name = name,
            CommandType = group.MainCommandName,
            GameTick = group.GameTick,
            Packets = group.Packets.OrderBy(p => p.Id).Select((p, i) => new SavedPacketData
            {
                Order = i,
                CommandTypeName = p.CommandTypeName,
                RawData = p.RawData,
                PayloadJson = p.PayloadJson
            }).ToList()
        };
        
        return SaveCommand(saved);
    }
    
    /// <summary>
    /// Save a command to disk.
    /// </summary>
    public string SaveCommand(SavedCommand command)
    {
        var fileName = $"{command.CommandType}_{command.CreatedAt:yyyy-MM-dd_HH-mm-ss}.json";
        var filePath = Path.Combine(_commandsPath, fileName);
        var json = JsonSerializer.Serialize(command, _jsonOptions);
        File.WriteAllText(filePath, json);
        return filePath;
    }
    
    /// <summary>
    /// Load a saved command from disk.
    /// </summary>
    public SavedCommand? LoadCommand(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SavedCommand>(json, _jsonOptions);
    }
    
    /// <summary>
    /// Get all saved command files.
    /// </summary>
    public List<string> GetSavedCommands()
    {
        return Directory.GetFiles(_commandsPath, "*.json")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .ToList();
    }
    
    /// <summary>
    /// Load all saved commands.
    /// </summary>
    public List<SavedCommand> LoadAllCommands()
    {
        var commands = new List<SavedCommand>();
        foreach (var file in GetSavedCommands())
        {
            var cmd = LoadCommand(file);
            if (cmd != null) commands.Add(cmd);
        }
        return commands;
    }
    
    /// <summary>
    /// Delete a saved command.
    /// </summary>
    public void DeleteCommand(string filePath)
    {
        if (File.Exists(filePath)) File.Delete(filePath);
    }
    
    public string GetCommandsPath() => _commandsPath;
}
