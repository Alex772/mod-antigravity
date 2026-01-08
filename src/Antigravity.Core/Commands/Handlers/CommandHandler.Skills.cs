using UnityEngine;
using System.Collections.Generic;

namespace Antigravity.Core.Commands.Handlers
{
    /// <summary>
    /// Handler for skills sync commands
    /// </summary>
    public static partial class CommandHandler
    {
        public static void ExecuteSkillsSyncCommand(SkillsSyncCommand cmd)
        {
            if (cmd == null) return;
            
            Debug.Log($"[Antigravity] EXECUTE: SkillsSync action={cmd.Action} dupe={cmd.DuplicantName} skill={cmd.SkillId}");
            
            try
            {
                // Find the duplicant by name
                MinionResume targetResume = FindMinionResumeByName(cmd.DuplicantName);
                if (targetResume == null)
                {
                    Debug.LogWarning($"[Antigravity] SkillsSync: Duplicant not found: {cmd.DuplicantName}");
                    return;
                }
                
                // Validate skill exists (but don't need the object since we use Traverse with string ID)
                if (Db.Get().Skills.TryGet(cmd.SkillId) == null)
                {
                    Debug.LogWarning($"[Antigravity] SkillsSync: Skill not found: {cmd.SkillId}");
                    return;
                }
                
                switch (cmd.Action)
                {
                    case SkillAction.Master:
                        // MasterSkill takes string ID
                        HarmonyLib.Traverse.Create(targetResume).Method("MasterSkill", cmd.SkillId).GetValue();
                        Debug.Log($"[Antigravity] SkillsSync: {cmd.DuplicantName} mastered {cmd.SkillId}");
                        break;
                        
                    case SkillAction.Unmaster:
                        HarmonyLib.Traverse.Create(targetResume).Method("UnmasterSkill", cmd.SkillId).GetValue();
                        Debug.Log($"[Antigravity] SkillsSync: {cmd.DuplicantName} unmastered {cmd.SkillId}");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] SkillsSync failed: {ex.Message}");
            }
        }
        
        private static MinionResume FindMinionResumeByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            
            // Search through all MinionResume components
            foreach (var minion in Components.MinionResumes.Items)
            {
                if (minion != null && minion.GetProperName() == name)
                {
                    return minion;
                }
            }
            
            return null;
        }
    }
}
