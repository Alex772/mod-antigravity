using System;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Command for research synchronization
    /// </summary>
    [Serializable]
    public class ResearchSyncCommand : GameCommand
    {
        /// <summary>
        /// Tech ID being researched or completed
        /// </summary>
        public string TechId { get; set; }
        
        /// <summary>
        /// Action type (select, complete, cancel)
        /// </summary>
        public ResearchAction Action { get; set; }
        
        /// <summary>
        /// Research points accumulated (for progress sync)
        /// </summary>
        public float Points { get; set; }
        
        public ResearchSyncCommand() : base(GameCommandType.ResearchSync) { }
    }
    
    /// <summary>
    /// Research action type
    /// </summary>
    public enum ResearchAction : byte
    {
        /// <summary>
        /// Player selected this tech to research
        /// </summary>
        Select = 0,
        
        /// <summary>
        /// Tech was completed
        /// </summary>
        Complete = 1,
        
        /// <summary>
        /// Research was cancelled
        /// </summary>
        Cancel = 2
    }
}
