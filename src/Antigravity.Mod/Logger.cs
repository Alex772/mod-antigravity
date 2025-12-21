using System;

namespace Antigravity
{
    /// <summary>
    /// Logging utility for the Antigravity mod.
    /// </summary>
    public static class Logger
    {
        private const string Prefix = "[Antigravity]";
        private static bool _initialized;

        /// <summary>
        /// Initialize the logger.
        /// </summary>
        public static void Initialize()
        {
            _initialized = true;
            Log("Logger initialized.");
        }

        /// <summary>
        /// Log an informational message.
        /// </summary>
        public static void Log(string message)
        {
            if (!_initialized) return;
            Debug.Log($"{Prefix} {message}");
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public static void LogWarning(string message)
        {
            if (!_initialized) return;
            Debug.LogWarning($"{Prefix} [WARN] {message}");
        }

        /// <summary>
        /// Log an error message.
        /// </summary>
        public static void LogError(string message)
        {
            // Always log errors, even if not initialized
            Debug.LogError($"{Prefix} [ERROR] {message}");
        }

        /// <summary>
        /// Log a debug message (only if verbose logging is enabled).
        /// </summary>
        public static void LogDebug(string message)
        {
            if (!_initialized) return;
            if (AntigravityMod.Config?.VerboseLogging != true) return;
            
            Debug.Log($"{Prefix} [DEBUG] {message}");
        }

        /// <summary>
        /// Log a network-related message (only if packet logging is enabled).
        /// </summary>
        public static void LogNetwork(string message)
        {
            if (!_initialized) return;
            if (AntigravityMod.Config?.LogPackets != true) return;
            
            Debug.Log($"{Prefix} [NET] {message}");
        }
    }
}
