using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Clear synchronization.
    /// Intercepts ClearTool.OnDragTool and syncs clear commands to other players.
    /// </summary>
    public static class ClearSyncPatches
    {
        private static List<int> _pendingClearCells = new List<int>();
        private static int _currentPriority = 5;

        /// <summary>
        /// Patch for ClearTool.OnDragTool
        /// </summary>
        public static class ClearTool_OnDragTool_Patch
        {
            public static void Postfix(int cell)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                if (!_pendingClearCells.Contains(cell))
                {
                    _pendingClearCells.Add(cell);
                }
            }
        }

        /// <summary>
        /// Send pending clear cells - called from DragTool_OnLeftClickUp_Patch
        /// </summary>
        public static void SendClearCells()
        {
            if (_pendingClearCells.Count > 0)
            {
                Debug.Log($"[Antigravity] SEND: Clear {_pendingClearCells.Count} cells");

                var cmd = new ClearCommand
                {
                    Cells = _pendingClearCells.ToArray(),
                    Priority = _currentPriority
                };
                CommandManager.SendCommand(cmd);

                _pendingClearCells.Clear();
            }
        }
    }
}
