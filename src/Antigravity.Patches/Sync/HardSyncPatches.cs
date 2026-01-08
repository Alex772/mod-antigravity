using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Network;
using Antigravity.Core.Sync;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches for hard synchronization - detects new day and manual saves.
    /// </summary>
    public static class HardSyncPatches
    {
        /// <summary>
        /// Patch for GameClock.OnNewDay - triggers hard sync at start of each day
        /// </summary>
        public static class GameClock_OnNewDay_Patch
        {
            public static void Postfix(int cycle)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsHost) return;
                
                try
                {
                    HardSyncManager.Instance.OnNewDay(cycle);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] HardSync OnNewDay failed: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Patch for SaveLoader.Save - triggers hard sync on manual save
        /// </summary>
        public static class SaveLoader_Save_Patch
        {
            public static void Postfix()
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsHost) return;
                
                try
                {
                    HardSyncManager.Instance.OnManualSave();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] HardSync OnManualSave failed: {ex.Message}");
                }
            }
        }
    }
}
