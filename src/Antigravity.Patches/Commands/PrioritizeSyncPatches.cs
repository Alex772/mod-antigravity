using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for PrioritizeTool synchronization.
    /// Intercepts PrioritizeTool.OnDragTool and syncs priority commands to other players.
    /// </summary>
    public static class PrioritizeSyncPatches
    {
        private static List<int> _pendingPriorityCells = new List<int>();
        private static int _currentPriorityClass = 0;
        private static int _currentPriorityValue = 5;

        /// <summary>
        /// Patch for PrioritizeTool.OnDragTool
        /// </summary>
        public static class PrioritizeTool_OnDragTool_Patch
        {
            public static void Postfix(FilteredDragTool __instance, int cell)
            {
                if (!(__instance is PrioritizeTool)) return;
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                if (!_pendingPriorityCells.Contains(cell))
                {
                    _pendingPriorityCells.Add(cell);
                }

                // Update current priority from ToolMenu
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
            }
        }

        /// <summary>
        /// Send pending priority cells - called from DragTool_OnLeftClickUp_Patch
        /// </summary>
        public static void SendPriorityCells()
        {
            if (_pendingPriorityCells.Count > 0)
            {
                Debug.Log($"[Antigravity] SEND: BulkPriority {_pendingPriorityCells.Count} cells");

                var cmd = new BulkPriorityCommand
                {
                    Cells = _pendingPriorityCells.ToArray(),
                    PriorityClass = _currentPriorityClass,
                    PriorityValue = _currentPriorityValue
                };
                CommandManager.SendCommand(cmd);

                _pendingPriorityCells.Clear();
            }
        }
    }
}
