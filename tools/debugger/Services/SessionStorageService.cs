using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Antigravity.Debugger.Models;

namespace Antigravity.Debugger.Services;

public class SessionStorageService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    
    private readonly string _sessionsPath;
    
    public SessionStorageService()
    {
        var appDir = AppContext.BaseDirectory;
        _sessionsPath = Path.Combine(appDir, "data", "sessions");
        Directory.CreateDirectory(_sessionsPath);
    }
    
    public void SaveSession(Session session)
    {
        session.EndTime = SystemDateTime.Now;
        var fileName = $"session_{session.StartTime:yyyy-MM-dd_HH-mm-ss}.json";
        var filePath = Path.Combine(_sessionsPath, fileName);
        var json = JsonSerializer.Serialize(session, _jsonOptions);
        File.WriteAllText(filePath, json);
    }
    
    public Session? LoadSession(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Session>(json, _jsonOptions);
    }
    
    public List<string> GetSavedSessions() => Directory.GetFiles(_sessionsPath, "session_*.json").OrderByDescending(f => f).ToList();
    public string GetSessionsPath() => _sessionsPath;
}
