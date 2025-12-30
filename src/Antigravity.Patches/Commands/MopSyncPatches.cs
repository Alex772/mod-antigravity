using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Mop synchronization.
    /// Intercepts MopTool.OnDragTool and syncs mop commands to other players.
    /// </summary>
    public static class MopSyncPatches
    {
        private static List<int> _pendingMopCells = new List<int>();
        private static int _currentPriority = 5;

        /// <summary>
        /// Patch for MopTool.OnDragTool
        /// </summary>
        public static class MopTool_OnDragTool_Patch
        {
            public static void Postfix(int cell)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                if (!_pendingMopCells.Contains(cell))
                {
                    _pendingMopCells.Add(cell);
                }
            }
        }

        /// <summary>
        /// Send pending mop cells - called from DragTool_OnLeftClickUp_Patch
        /// </summary>
        public static void SendMopCells()
        {
            if (_pendingMopCells.Count > 0)
            {
                Debug.Log($"[Antigravity] SEND: Mop {_pendingMopCells.Count} cells");

                var cmd = new MopCommand
                {
                    Cells = _pendingMopCells.ToArray(),
                    Priority = _currentPriority
                };
                CommandManager.SendCommand(cmd);

                _pendingMopCells.Clear();
            }
        }
    }
}
