using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Harvest synchronization.
    /// Intercepts HarvestTool.OnDragTool and syncs harvest commands to other players.
    /// </summary>
    public static class HarvestSyncPatches
    {
        private static List<int> _pendingHarvestCells = new List<int>();
        private static bool _harvestWhenReady = true;
        private static int _currentPriority = 5;

        /// <summary>
        /// Patch for HarvestTool.OnDragTool
        /// </summary>
        public static class HarvestTool_OnDragTool_Patch
        {
            public static void Postfix(HarvestTool __instance, int cell)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                if (!_pendingHarvestCells.Contains(cell))
                {
                    _pendingHarvestCells.Add(cell);
                }
            }
        }

        /// <summary>
        /// Send pending harvest cells - called from DragTool_OnLeftClickUp_Patch
        /// </summary>
        public static void SendHarvestCells()
        {
            if (_pendingHarvestCells.Count > 0)
            {
                Debug.Log($"[Antigravity] SEND: Harvest {_pendingHarvestCells.Count} cells (ready={_harvestWhenReady})");

                var cmd = new HarvestCommand
                {
                    Cells = _pendingHarvestCells.ToArray(),
                    HarvestWhenReady = _harvestWhenReady,
                    Priority = _currentPriority
                };
                CommandManager.SendCommand(cmd);

                _pendingHarvestCells.Clear();
            }
        }

        /// <summary>
        /// Set whether we're marking for harvest or cancelling.
        /// </summary>
        public static void SetHarvestMode(bool harvestWhenReady)
        {
            _harvestWhenReady = harvestWhenReady;
        }
    }
}
