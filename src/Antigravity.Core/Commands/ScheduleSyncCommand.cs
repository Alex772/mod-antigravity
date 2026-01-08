using System;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Command for schedule synchronization
    /// </summary>
    [Serializable]
    public class ScheduleSyncCommand : GameCommand
    {
        /// <summary>
        /// Schedule name/ID
        /// </summary>
        public string ScheduleName { get; set; }
        
        /// <summary>
        /// Block index (0-23 for 24 hour blocks)
        /// </summary>
        public int BlockIndex { get; set; }
        
        /// <summary>
        /// Schedule block type name
        /// </summary>
        public string BlockType { get; set; }
        
        /// <summary>
        /// Action type
        /// </summary>
        public ScheduleAction Action { get; set; }
        
        /// <summary>
        /// For duplicant assignment: duplicant name
        /// </summary>
        public string DuplicantName { get; set; }
        
        public ScheduleSyncCommand() : base(GameCommandType.ScheduleSync) { }
    }
    
    /// <summary>
    /// Schedule action type
    /// </summary>
    public enum ScheduleAction : byte
    {
        /// <summary>
        /// Changed a block type
        /// </summary>
        SetBlock = 0,
        
        /// <summary>
        /// Created new schedule
        /// </summary>
        Create = 1,
        
        /// <summary>
        /// Deleted schedule
        /// </summary>
        Delete = 2,
        
        /// <summary>
        /// Assigned duplicant to schedule
        /// </summary>
        Assign = 3
    }
}
