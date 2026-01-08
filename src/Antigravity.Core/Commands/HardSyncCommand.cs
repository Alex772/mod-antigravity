using System;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Command for hard synchronization - sends compressed save data
    /// </summary>
    [Serializable]
    public class HardSyncCommand : GameCommand
    {
        /// <summary>
        /// Reason for the hard sync (morning, manual save, etc.)
        /// </summary>
        public HardSyncReason Reason { get; set; }
        
        /// <summary>
        /// Compressed save data (gzip compressed bytes as base64)
        /// </summary>
        public string SaveData { get; set; }
        
        /// <summary>
        /// Current game cycle
        /// </summary>
        public int Cycle { get; set; }
        
        /// <summary>
        /// Current time of day (0-1)
        /// </summary>
        public float TimeOfDay { get; set; }
        
        /// <summary>
        /// Hash of the save data for verification
        /// </summary>
        public string DataHash { get; set; }
        
        public HardSyncCommand() : base(GameCommandType.HardSync) { }
    }
    
    /// <summary>
    /// Reason for triggering hard sync
    /// </summary>
    public enum HardSyncReason : byte
    {
        /// <summary>
        /// Automatic sync at start of new day
        /// </summary>
        NewDay = 0,
        
        /// <summary>
        /// Manual save triggered by host
        /// </summary>
        ManualSave = 1,
        
        /// <summary>
        /// Client requested resync (too much desync detected)
        /// </summary>
        ClientRequest = 2,
        
        /// <summary>
        /// Initial sync when client joins
        /// </summary>
        InitialJoin = 3
    }
}
