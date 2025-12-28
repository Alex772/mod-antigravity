using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Cancel task synchronization.
    /// CancelTool uses Trigger(2127324410) to cancel tasks.
    /// </summary>
    public static class CancelSyncPatches
    {
        private static List<int> _pendingCancelCells = new List<int>();

        /// <summary>
        /// Patch for CancelTool.OnDragTool
        /// </summary>
        public static class CancelTool_OnDragTool_Patch
        {
            public static void Postfix(int cell)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                if (!_pendingCancelCells.Contains(cell))
                {
                    _pendingCancelCells.Add(cell);
                }
            }
        }

        /// <summary>
        /// Send pending cancel cells - called from DigSyncPatches.DragTool_OnLeftClickUp_Patch
        /// </summary>
        public static void SendCancelCells()
        {
            if (_pendingCancelCells.Count > 0)
            {
                Debug.Log($"[Antigravity] SEND: CancelDig {_pendingCancelCells.Count} cells");
                
                var cmd = new DigCommand
                {
                    Type = GameCommandType.CancelDig,
                    Cells = _pendingCancelCells.ToArray()
                };
                CommandManager.SendCommand(cmd);

                _pendingCancelCells.Clear();
            }
        }
    }
}
