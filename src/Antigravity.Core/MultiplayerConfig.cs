using UnityEngine;

namespace Antigravity.Core
{
    /// <summary>
    /// Configuration settings for Antigravity multiplayer.
    /// </summary>
    public static class MultiplayerConfig
    {
        /// <summary>
        /// Whether to use local networking (LiteNetLib) instead of Steam P2P.
        /// Set to true for local testing on the same PC.
        /// </summary>
        public static bool UseLocalNetworking { get; set; } = false;

        /// <summary>
        /// Default port for local networking.
        /// </summary>
        public static int LocalPort { get; set; } = 7777;

        /// <summary>
        /// Default local address.
        /// </summary>
        public static string LocalAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// Enable debug overlay for testing.
        /// </summary>
        public static bool ShowDebugOverlay { get; set; } = false;

        /// <summary>
        /// Enable verbose logging for network events.
        /// </summary>
        public static bool VerboseLogging { get; set; } = true;

        /// <summary>
        /// Load configuration (can be extended to load from file).
        /// </summary>
        public static void Load()
        {
            // Check for command line argument to enable local mode
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--local-multiplayer" || args[i] == "-lm")
                {
                    UseLocalNetworking = true;
                    Debug.Log("[Antigravity] Local networking mode enabled via command line");
                }
                else if (args[i] == "--local-port" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int port))
                    {
                        LocalPort = port;
                    }
                }
                else if (args[i] == "--debug-overlay")
                {
                    ShowDebugOverlay = true;
                }
            }
        }

        /// <summary>
        /// Get connection string for current mode.
        /// </summary>
        public static string GetConnectionString()
        {
            return UseLocalNetworking 
                ? $"{LocalAddress}:{LocalPort}" 
                : "Steam Lobby";
        }
    }
}
