using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using Database;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches for skills synchronization
    /// </summary>
    public static class SkillsPatches
    {
        /// <summary>
        /// Patch for MinionResume.MasterSkill - when player assigns a skill
        /// </summary>
        public static class MinionResume_MasterSkill_Patch
        {
            public static void Postfix(MinionResume __instance, Skill skill)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;
                if (skill == null) return;
                
                try
                {
                    string dupeName = __instance.GetProperName();
                    
                    Debug.Log($"[Antigravity] SEND: SkillsSync Master dupe={dupeName} skill={skill.Id}");
                    
                    var cmd = new SkillsSyncCommand
                    {
                        DuplicantName = dupeName,
                        SkillId = skill.Id,
                        Action = SkillAction.Master
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] SkillsPatches MasterSkill failed: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Patch for MinionResume.UnmasterSkill - when player removes a skill
        /// </summary>
        public static class MinionResume_UnmasterSkill_Patch
        {
            public static void Postfix(MinionResume __instance, Skill skill)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;
                if (skill == null) return;
                
                try
                {
                    string dupeName = __instance.GetProperName();
                    
                    Debug.Log($"[Antigravity] SEND: SkillsSync Unmaster dupe={dupeName} skill={skill.Id}");
                    
                    var cmd = new SkillsSyncCommand
                    {
                        DuplicantName = dupeName,
                        SkillId = skill.Id,
                        Action = SkillAction.Unmaster
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] SkillsPatches UnmasterSkill failed: {ex.Message}");
                }
            }
        }
    }
}
