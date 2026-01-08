using System;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Command for skills/jobs synchronization
    /// </summary>
    [Serializable]
    public class SkillsSyncCommand : GameCommand
    {
        /// <summary>
        /// Duplicant name (used to find the duplicant)
        /// </summary>
        public string DuplicantName { get; set; }
        
        /// <summary>
        /// Skill ID being mastered or unmastered
        /// </summary>
        public string SkillId { get; set; }
        
        /// <summary>
        /// Action type
        /// </summary>
        public SkillAction Action { get; set; }
        
        public SkillsSyncCommand() : base(GameCommandType.SkillsSync) { }
    }
    
    /// <summary>
    /// Skills action type
    /// </summary>
    public enum SkillAction : byte
    {
        /// <summary>
        /// Skill was mastered
        /// </summary>
        Master = 0,
        
        /// <summary>
        /// Skill was unmastered/reset
        /// </summary>
        Unmaster = 1
    }
}
