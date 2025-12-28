using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Deconstruct synchronization.
    /// DeconstructTool uses Trigger(-790448070) to mark buildings for deconstruction.
    /// </summary>
    public static class DeconstructSyncPatches
    {
        private static List<int> _pendingDeconstructCells = new List<int>();

        /// <summary>
        /// Patch for DeconstructTool.OnDragTool
        /// </summary>
        public static class DeconstructTool_OnDragTool_Patch
        {
            public static void Postfix(int cell)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                if (!_pendingDeconstructCells.Contains(cell))
                {
                    _pendingDeconstructCells.Add(cell);
                }
            }
        }

        /// <summary>
        /// Send pending deconstruct cells - called from DigSyncPatches.DragTool_OnLeftClickUp_Patch
        /// </summary>
        public static void SendDeconstructCells()
        {
            if (_pendingDeconstructCells.Count > 0)
            {
                Debug.Log($"[Antigravity] SEND: Deconstruct {_pendingDeconstructCells.Count} cells");
                
                var cmd = new DeconstructCommand
                {
                    Cells = _pendingDeconstructCells.ToArray()
                };
                CommandManager.SendCommand(cmd);

                _pendingDeconstructCells.Clear();
            }
        }
    }
}
