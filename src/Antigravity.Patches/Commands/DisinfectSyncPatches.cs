using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Disinfect synchronization.
    /// Intercepts DisinfectTool.OnDragTool and syncs disinfect commands to other players.
    /// </summary>
    public static class DisinfectSyncPatches
    {
        private static List<int> _pendingDisinfectCells = new List<int>();

        /// <summary>
        /// Patch for DisinfectTool.OnDragTool
        /// </summary>
        public static class DisinfectTool_OnDragTool_Patch
        {
            public static void Postfix(int cell)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                if (!_pendingDisinfectCells.Contains(cell))
                {
                    _pendingDisinfectCells.Add(cell);
                }
            }
        }

        /// <summary>
        /// Send pending disinfect cells - called from DragTool_OnLeftClickUp_Patch
        /// </summary>
        public static void SendDisinfectCells()
        {
            if (_pendingDisinfectCells.Count > 0)
            {
                Debug.Log($"[Antigravity] SEND: Disinfect {_pendingDisinfectCells.Count} cells");

                var cmd = new DisinfectCommand
                {
                    Cells = _pendingDisinfectCells.ToArray()
                };
                CommandManager.SendCommand(cmd);

                _pendingDisinfectCells.Clear();
            }
        }
    }
}
