using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Capture synchronization.
    /// CaptureTool uses OnDragComplete with area selection instead of OnDragTool.
    /// </summary>
    public static class CaptureSyncPatches
    {
        private static int _currentPriorityClass = 0;
        private static int _currentPriorityValue = 5;

        /// <summary>
        /// Patch for CaptureTool.OnDragComplete
        /// </summary>
        public static class CaptureTool_OnDragComplete_Patch
        {
            public static void Postfix(CaptureTool __instance, Vector3 downPos, Vector3 upPos)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                // Get area bounds (same as CaptureTool does)
                Vector2 min = Vector2.Min(downPos, upPos);
                Vector2 max = Vector2.Max(downPos, upPos);

                // Get current priority from ToolMenu if available
                try
                {
                    if (ToolMenu.Instance?.PriorityScreen != null)
                    {
                        var priority = ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority();
                        _currentPriorityClass = (int)priority.priority_class;
                        _currentPriorityValue = priority.priority_value;
                    }
                }
                catch { }

                Debug.Log($"[Antigravity] SEND: Capture area ({min.x},{min.y})-({max.x},{max.y})");

                var cmd = new CaptureCommand
                {
                    MinX = min.x,
                    MinY = min.y,
                    MaxX = max.x,
                    MaxY = max.y,
                    Mark = true,
                    PriorityClass = _currentPriorityClass,
                    PriorityValue = _currentPriorityValue
                };
                CommandManager.SendCommand(cmd);
            }
        }
    }
}
