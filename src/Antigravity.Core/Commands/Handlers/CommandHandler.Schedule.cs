using UnityEngine;
using System.Linq;

namespace Antigravity.Core.Commands.Handlers
{
    /// <summary>
    /// Handler for schedule sync commands
    /// </summary>
    public static partial class CommandHandler
    {
        public static void ExecuteScheduleSyncCommand(ScheduleSyncCommand cmd)
        {
            if (cmd == null) return;
            
            Debug.Log($"[Antigravity] EXECUTE: ScheduleSync action={cmd.Action} schedule={cmd.ScheduleName} block={cmd.BlockIndex}");
            
            try
            {
                var scheduleManager = ScheduleManager.Instance;
                if (scheduleManager == null)
                {
                    Debug.LogWarning("[Antigravity] ScheduleSync: ScheduleManager.Instance is null");
                    return;
                }
                
                switch (cmd.Action)
                {
                    case ScheduleAction.SetBlock:
                        ExecuteScheduleSetBlock(scheduleManager, cmd);
                        break;
                        
                    case ScheduleAction.Create:
                        ExecuteScheduleCreate(scheduleManager, cmd);
                        break;
                        
                    case ScheduleAction.Delete:
                        ExecuteScheduleDelete(scheduleManager, cmd);
                        break;
                        
                    case ScheduleAction.Assign:
                        // TODO: Implement duplicant assignment
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] ScheduleSync failed: {ex.Message}");
            }
        }
        
        private static void ExecuteScheduleSetBlock(ScheduleManager manager, ScheduleSyncCommand cmd)
        {
            // Find the schedule by name
            var schedule = manager.GetSchedules().FirstOrDefault(s => s.name == cmd.ScheduleName);
            if (schedule == null)
            {
                Debug.LogWarning($"[Antigravity] ScheduleSync: Schedule not found: {cmd.ScheduleName}");
                return;
            }
            
            // Get the block type
            var blockType = Db.Get().ScheduleBlockTypes.TryGet(cmd.BlockType);
            if (blockType == null)
            {
                Debug.LogWarning($"[Antigravity] ScheduleSync: BlockType not found: {cmd.BlockType}");
                return;
            }
            
            // Set the block using Traverse since exact signature may vary
            try
            {
                HarmonyLib.Traverse.Create(schedule).Method("SetBlockType", cmd.BlockIndex, blockType).GetValue();
            }
            catch
            {
                // Alternative - set block directly via blocks list
                var blocks = schedule.GetBlocks();
                if (blocks != null && cmd.BlockIndex >= 0 && cmd.BlockIndex < blocks.Count)
                {
                    blocks[cmd.BlockIndex].GroupId = blockType.Id;
                }
            }
            Debug.Log($"[Antigravity] ScheduleSync: Set block {cmd.BlockIndex} to {cmd.BlockType} in {cmd.ScheduleName}");
        }
        
        private static void ExecuteScheduleCreate(ScheduleManager manager, ScheduleSyncCommand cmd)
        {
            // Add a new schedule (will use default blocks)
            manager.AddSchedule(null); // null creates a copy of default
            Debug.Log($"[Antigravity] ScheduleSync: Created new schedule");
        }
        
        private static void ExecuteScheduleDelete(ScheduleManager manager, ScheduleSyncCommand cmd)
        {
            // Find and delete the schedule
            var schedule = manager.GetSchedules().FirstOrDefault(s => s.name == cmd.ScheduleName);
            if (schedule != null)
            {
                manager.DeleteSchedule(schedule);
                Debug.Log($"[Antigravity] ScheduleSync: Deleted schedule {cmd.ScheduleName}");
            }
        }
    }
}
