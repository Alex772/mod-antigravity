using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches for schedule synchronization
    /// </summary>
    public static class SchedulePatches
    {
        /// <summary>
        /// Patch for Schedule.SetBlockType - when player changes a block
        /// </summary>
        public static class Schedule_SetBlockType_Patch
        {
            public static void Postfix(Schedule __instance, int idx, ScheduleBlockType blockType)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;
                
                try
                {
                    Debug.Log($"[Antigravity] SEND: ScheduleSync SetBlock schedule={__instance.name} idx={idx} type={blockType?.Id}");
                    
                    var cmd = new ScheduleSyncCommand
                    {
                        ScheduleName = __instance.name,
                        BlockIndex = idx,
                        BlockType = blockType?.Id,
                        Action = ScheduleAction.SetBlock
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] SchedulePatches SetBlockType failed: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Patch for ScheduleManager.AddSchedule - when new schedule is created
        /// </summary>
        public static class ScheduleManager_AddSchedule_Patch
        {
            public static void Postfix(ScheduleManager __instance, Schedule __result)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;
                if (__result == null) return;
                
                try
                {
                    Debug.Log($"[Antigravity] SEND: ScheduleSync Create schedule={__result.name}");
                    
                    var cmd = new ScheduleSyncCommand
                    {
                        ScheduleName = __result.name,
                        Action = ScheduleAction.Create
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] SchedulePatches AddSchedule failed: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Patch for ScheduleManager.DeleteSchedule - when schedule is deleted
        /// </summary>
        public static class ScheduleManager_DeleteSchedule_Patch
        {
            public static void Prefix(ScheduleManager __instance, Schedule schedule)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;
                if (schedule == null) return;
                
                try
                {
                    Debug.Log($"[Antigravity] SEND: ScheduleSync Delete schedule={schedule.name}");
                    
                    var cmd = new ScheduleSyncCommand
                    {
                        ScheduleName = schedule.name,
                        Action = ScheduleAction.Delete
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] SchedulePatches DeleteSchedule failed: {ex.Message}");
                }
            }
        }
    }
}
