using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Antigravity.Core.Telemetry
{
    /// <summary>
    /// Crash reporter for collecting error logs.
    /// Currently logs to file - remote webhook support can be added later.
    /// </summary>
    public static class CrashReporter
    {
        private static bool _initialized = false;
        private static string _logFilePath = null;
        private static int _maxReportsPerSession = 50;
        private static int _reportsSentThisSession = 0;
        
        /// <summary>
        /// Initialize the crash reporter.
        /// Call this on mod startup.
        /// </summary>
        public static void Initialize(string discordWebhookUrl = null)
        {
            if (_initialized) return;
            
            try
            {
                // Create log file in game's log directory
                string logDir = Path.Combine(Application.persistentDataPath, "Antigravity");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                _logFilePath = Path.Combine(logDir, "crash_reports.log");
                
                // Subscribe to Unity log events
                Application.logMessageReceived += OnLogMessage;
                
                _initialized = true;
                Debug.Log($"[Antigravity.CrashReporter] Initialized - logging to {_logFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Antigravity.CrashReporter] Failed to initialize: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Cleanup on mod unload.
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized) return;
            
            Application.logMessageReceived -= OnLogMessage;
            _initialized = false;
        }
        
        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            // Only report errors and exceptions
            if (type != LogType.Exception && type != LogType.Error) return;
            
            // Only report Antigravity-related errors
            if (!condition.Contains("Antigravity") && !stackTrace.Contains("Antigravity")) return;
            
            // Rate limit
            if (_reportsSentThisSession >= _maxReportsPerSession) return;
            
            try
            {
                string roleInfo = Network.MultiplayerState.IsMultiplayerSession 
                    ? (Network.MultiplayerState.IsHost ? "Host" : "Client") 
                    : "Singleplayer";
                    
                string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                var sb = new StringBuilder();
                sb.AppendLine($"=== CRASH REPORT ({timestamp}) ===");
                sb.AppendLine($"Version: {Constants.ModVersion}");
                sb.AppendLine($"Session: {roleInfo}");
                sb.AppendLine($"Error: {condition}");
                sb.AppendLine($"Stack Trace:");
                sb.AppendLine(stackTrace);
                sb.AppendLine();
                
                // Write to log file
                if (!string.IsNullOrEmpty(_logFilePath))
                {
                    File.AppendAllText(_logFilePath, sb.ToString());
                }
                
                _reportsSentThisSession++;
            }
            catch
            {
                // Silently fail - don't cause more errors while reporting errors
            }
        }
        
        /// <summary>
        /// Get the path to the crash log file.
        /// </summary>
        public static string GetLogFilePath()
        {
            return _logFilePath;
        }
        
        /// <summary>
        /// Manually report an error.
        /// </summary>
        public static void ReportError(string message, string details = null)
        {
            if (!_initialized) return;
            OnLogMessage(message, details ?? Environment.StackTrace, LogType.Error);
        }
    }
}
