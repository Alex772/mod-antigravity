namespace Antigravity.Core
{
    /// <summary>
    /// Core constants used throughout the mod.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Mod identifier.
        /// </summary>
        public const string ModId = "Antigravity.Multiplayer";

        /// <summary>
        /// Network protocol version. Increment when protocol changes.
        /// </summary>
        public const int ProtocolVersion = 1;

        /// <summary>
        /// Default network port.
        /// </summary>
        public const int DefaultPort = 7777;

        /// <summary>
        /// Maximum packet size in bytes.
        /// </summary>
        public const int MaxPacketSize = 65536;

        /// <summary>
        /// Heartbeat interval in milliseconds.
        /// </summary>
        public const int HeartbeatInterval = 1000;

        /// <summary>
        /// Connection timeout in milliseconds.
        /// </summary>
        public const int ConnectionTimeout = 10000;

        /// <summary>
        /// Maximum reconnection attempts.
        /// </summary>
        public const int MaxReconnectAttempts = 5;

        /// <summary>
        /// Ticks per game day (approximately).
        /// </summary>
        public const int TicksPerDay = 600;
    }
}
