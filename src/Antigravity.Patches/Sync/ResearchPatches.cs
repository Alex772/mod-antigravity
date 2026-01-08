using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches for research synchronization
    /// </summary>
    public static class ResearchPatches
    {
        /// <summary>
        /// Patch for Research.SetActiveResearch - when player selects a tech to research
        /// </summary>
        public static class Research_SetActiveResearch_Patch
        {
            public static void Postfix(Research __instance, Tech tech)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;
                if (tech == null) return;
                
                try
                {
                    Debug.Log($"[Antigravity] SEND: ResearchSync Select techId={tech.Id}");
                    
                    var cmd = new ResearchSyncCommand
                    {
                        TechId = tech.Id,
                        Action = ResearchAction.Select,
                        Points = 0
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] ResearchPatches Select failed: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Patch for TechInstance.Complete - when a tech is completed
        /// </summary>
        public static class TechInstance_Complete_Patch
        {
            public static void Postfix(TechInstance __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;
                
                try
                {
                    string techId = __instance?.tech?.Id;
                    if (string.IsNullOrEmpty(techId)) return;
                    
                    Debug.Log($"[Antigravity] SEND: ResearchSync Complete techId={techId}");
                    
                    var cmd = new ResearchSyncCommand
                    {
                        TechId = techId,
                        Action = ResearchAction.Complete,
                        Points = 0
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] ResearchPatches Complete failed: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Patch for Research.CancelResearch - when research is cancelled
        /// </summary>
        public static class Research_CancelResearch_Patch
        {
            public static void Postfix(Research __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;
                
                try
                {
                    Debug.Log("[Antigravity] SEND: ResearchSync Cancel");
                    
                    var cmd = new ResearchSyncCommand
                    {
                        TechId = "",
                        Action = ResearchAction.Cancel,
                        Points = 0
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] ResearchPatches Cancel failed: {ex.Message}");
                }
            }
        }
    }
}
