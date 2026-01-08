using UnityEngine;

namespace Antigravity.Core.Commands.Handlers
{
    /// <summary>
    /// Handler for research sync commands
    /// </summary>
    public static partial class CommandHandler
    {
        public static void ExecuteResearchSyncCommand(ResearchSyncCommand cmd)
        {
            if (cmd == null) return;
            
            Debug.Log($"[Antigravity] EXECUTE: ResearchSync action={cmd.Action} techId={cmd.TechId}");
            
            try
            {
                var research = Research.Instance;
                if (research == null)
                {
                    Debug.LogWarning("[Antigravity] ResearchSync: Research.Instance is null");
                    return;
                }
                
                switch (cmd.Action)
                {
                    case ResearchAction.Select:
                        ExecuteResearchSelect(research, cmd.TechId);
                        break;
                        
                    case ResearchAction.Complete:
                        ExecuteResearchComplete(research, cmd.TechId);
                        break;
                        
                    case ResearchAction.Cancel:
                        ExecuteResearchCancel(research);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] ResearchSync failed: {ex.Message}");
            }
        }
        
        private static void ExecuteResearchSelect(Research research, string techId)
        {
            if (string.IsNullOrEmpty(techId)) return;
            
            var tech = Db.Get().Techs.TryGet(techId);
            if (tech != null)
            {
                research.SetActiveResearch(tech, true);
                Debug.Log($"[Antigravity] ResearchSync: Selected tech {techId}");
            }
            else
            {
                Debug.LogWarning($"[Antigravity] ResearchSync: Tech not found: {techId}");
            }
        }
        
        private static void ExecuteResearchComplete(Research research, string techId)
        {
            if (string.IsNullOrEmpty(techId)) return;
            
            var tech = Db.Get().Techs.TryGet(techId);
            if (tech != null)
            {
                var techInstance = research.Get(tech);
                if (techInstance != null && !techInstance.IsComplete())
                {
                    // Force complete the tech
                    techInstance.Purchased();
                    Debug.Log($"[Antigravity] ResearchSync: Completed tech {techId}");
                }
            }
            else
            {
                Debug.LogWarning($"[Antigravity] ResearchSync: Tech not found: {techId}");
            }
        }
        
        private static void ExecuteResearchCancel(Research research)
        {
            // CancelResearch requires Tech parameter - get active tech first
            try
            {
                // Simply clear the active research by setting it to null
                research.SetActiveResearch(null, true);
            }
            catch { }
            Debug.Log("[Antigravity] ResearchSync: Cancelled research");
        }
    }
}
