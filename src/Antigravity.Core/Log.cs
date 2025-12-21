using System;

namespace Antigravity.Core
{
    /// <summary>
    /// Abstraction for logging to support testing without Unity.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Whether logging is enabled.
        /// </summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>
        /// Custom log handler. Set this before logging to redirect output.
        /// If not set, will try to use Unity's Debug.Log (only in game context).
        /// </summary>
        public static Action<string> Handler { get; set; }

        /// <summary>
        /// Log an informational message.
        /// </summary>
        public static void Info(string message)
        {
            if (!Enabled) return;

            if (Handler != null)
            {
                Handler(message);
            }
            else
            {
                LogToUnity(message, LogLevel.Info);
            }
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public static void Warning(string message)
        {
            if (!Enabled) return;

            if (Handler != null)
            {
                Handler($"[WARN] {message}");
            }
            else
            {
                LogToUnity(message, LogLevel.Warning);
            }
        }

        /// <summary>
        /// Log an error message.
        /// </summary>
        public static void Error(string message)
        {
            if (Handler != null)
            {
                Handler($"[ERROR] {message}");
            }
            else
            {
                LogToUnity(message, LogLevel.Error);
            }
        }

        private enum LogLevel { Info, Warning, Error }

        private static void LogToUnity(string message, LogLevel level)
        {
            // This method is called only if Handler is not set
            // In tests, Handler should be set to a non-Unity implementation
            try
            {
                switch (level)
                {
                    case LogLevel.Info:
                        UnityEngine.Debug.Log(message);
                        break;
                    case LogLevel.Warning:
                        UnityEngine.Debug.LogWarning(message);
                        break;
                    case LogLevel.Error:
                        UnityEngine.Debug.LogError(message);
                        break;
                }
            }
            catch
            {
                // Unity not available (e.g., in unit tests)
                // Silently ignore
            }
        }
    }
}
