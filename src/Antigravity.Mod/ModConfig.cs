using System;
using System.IO;
using Newtonsoft.Json;

namespace Antigravity
{
    /// <summary>
    /// Configuration settings for the Antigravity mod.
    /// </summary>
    public class ModConfig
    {
        /// <summary>
        /// Path to the configuration file.
        /// </summary>
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Klei", "OxygenNotIncluded", "mods", "Antigravity", "config.json"
        );

        #region Network Settings

        /// <summary>
        /// Default port for multiplayer connections.
        /// </summary>
        public int DefaultPort { get; set; } = 7777;

        /// <summary>
        /// Maximum number of players allowed in a session.
        /// </summary>
        public int MaxPlayers { get; set; } = 4;

        /// <summary>
        /// Connection timeout in milliseconds.
        /// </summary>
        public int ConnectionTimeout { get; set; } = 10000;

        /// <summary>
        /// Enable UPnP for automatic port forwarding.
        /// </summary>
        public bool EnableUPnP { get; set; } = true;

        #endregion

        #region Sync Settings

        /// <summary>
        /// Interval for hard sync in game ticks (1 game day = ~600 ticks).
        /// </summary>
        public int HardSyncInterval { get; set; } = 600;

        /// <summary>
        /// Interval for soft sync in game ticks.
        /// </summary>
        public int SoftSyncInterval { get; set; } = 30;

        /// <summary>
        /// Enable debug sync information.
        /// </summary>
        public bool DebugSync { get; set; } = false;

        #endregion

        #region UI Settings

        /// <summary>
        /// Show player cursors on screen.
        /// </summary>
        public bool ShowPlayerCursors { get; set; } = true;

        /// <summary>
        /// Show connection status indicator.
        /// </summary>
        public bool ShowConnectionStatus { get; set; } = true;

        /// <summary>
        /// Enable in-game chat.
        /// </summary>
        public bool EnableChat { get; set; } = true;

        #endregion

        #region Debug Settings

        /// <summary>
        /// Enable verbose logging.
        /// </summary>
        public bool VerboseLogging { get; set; } = false;

        /// <summary>
        /// Log network packets.
        /// </summary>
        public bool LogPackets { get; set; } = false;

        #endregion

        /// <summary>
        /// Load configuration from file or create default.
        /// </summary>
        public static ModConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonConvert.DeserializeObject<ModConfig>(json) ?? new ModConfig();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to load config: {ex.Message}. Using defaults.");
            }

            var config = new ModConfig();
            config.Save();
            return config;
        }

        /// <summary>
        /// Save configuration to file.
        /// </summary>
        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to save config: {ex.Message}");
            }
        }
    }
}
