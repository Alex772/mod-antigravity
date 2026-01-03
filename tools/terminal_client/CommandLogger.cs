global using SystemDateTime = System.DateTime;

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Antigravity.TerminalClient;

/// <summary>
/// Logs all network messages to a file for later analysis.
/// </summary>
public class CommandLogger
{
    private readonly List<LogEntry> _entries = new List<LogEntry>();
    private bool _isEnabled = true;
    private string _logFile;
    
    public bool IsEnabled => _isEnabled;
    public int EntryCount => _entries.Count;
    
    public CommandLogger()
    {
        _logFile = $"network_log_{SystemDateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
    }
    
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[LOG] Logging {(enabled ? "ENABLED" : "DISABLED")}");
        Console.ResetColor();
    }
    
    public void LogReceived(byte[] data, string parsed)
    {
        if (!_isEnabled) return;
        
        var entry = new LogEntry
        {
            Timestamp = SystemDateTime.Now,
            Direction = "RECV",
            RawData = data,
            ParsedMessage = parsed
        };
        _entries.Add(entry);
    }
    
    public void LogSent(byte[] data, string description)
    {
        if (!_isEnabled) return;
        
        var entry = new LogEntry
        {
            Timestamp = SystemDateTime.Now,
            Direction = "SENT",
            RawData = data,
            ParsedMessage = description
        };
        _entries.Add(entry);
    }
    
    public void LogEvent(string eventType, string details)
    {
        if (!_isEnabled) return;
        
        var entry = new LogEntry
        {
            Timestamp = SystemDateTime.Now,
            Direction = "EVENT",
            RawData = null,
            ParsedMessage = $"{eventType}: {details}"
        };
        _entries.Add(entry);
    }
    
    public void SaveToFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                    ANTIGRAVITY NETWORK LOG                                   ║");
        sb.AppendLine($"║  Generated: {SystemDateTime.Now:yyyy-MM-dd HH:mm:ss}                                              ║");
        sb.AppendLine($"║  Total Entries: {_entries.Count,5}                                                      ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        
        foreach (var entry in _entries)
        {
            sb.AppendLine($"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Direction}]");
            
            if (entry.RawData != null)
            {
                sb.AppendLine($"  Raw ({entry.RawData.Length} bytes): {BitConverter.ToString(entry.RawData).Replace("-", " ")}");
            }
            
            if (!string.IsNullOrEmpty(entry.ParsedMessage))
            {
                sb.AppendLine($"  Parsed: {entry.ParsedMessage}");
            }
            
            sb.AppendLine();
        }
        
        File.WriteAllText(_logFile, sb.ToString());
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[LOG] Saved {_entries.Count} entries to: {_logFile}");
        Console.ResetColor();
    }
    
    public void ShowStats()
    {
        int recv = 0, sent = 0, events = 0;
        long totalBytesRecv = 0, totalBytesSent = 0;
        
        foreach (var entry in _entries)
        {
            switch (entry.Direction)
            {
                case "RECV":
                    recv++;
                    if (entry.RawData != null) totalBytesRecv += entry.RawData.Length;
                    break;
                case "SENT":
                    sent++;
                    if (entry.RawData != null) totalBytesSent += entry.RawData.Length;
                    break;
                case "EVENT":
                    events++;
                    break;
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════╗");
        Console.WriteLine("║           LOG STATISTICS                  ║");
        Console.WriteLine("╠═══════════════════════════════════════════╣");
        Console.WriteLine($"║  Total Entries: {_entries.Count,10}              ║");
        Console.WriteLine($"║  Received:      {recv,10} ({FormatBytes(totalBytesRecv),10})  ║");
        Console.WriteLine($"║  Sent:          {sent,10} ({FormatBytes(totalBytesSent),10})  ║");
        Console.WriteLine($"║  Events:        {events,10}              ║");
        Console.WriteLine("╠═══════════════════════════════════════════╣");
        Console.WriteLine($"║  Logging: {(_isEnabled ? "ENABLED" : "DISABLED"),8}                    ║");
        Console.WriteLine($"║  Log File: {_logFile,-28}  ║");
        Console.WriteLine("╚═══════════════════════════════════════════╝");
        Console.WriteLine();
    }
    
    public void Clear()
    {
        _entries.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[LOG] Log cleared");
        Console.ResetColor();
    }
    
    private string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
    
    private class LogEntry
    {
        public SystemDateTime Timestamp { get; set; }
        public string Direction { get; set; }
        public byte[] RawData { get; set; }
        public string ParsedMessage { get; set; }
    }
}
