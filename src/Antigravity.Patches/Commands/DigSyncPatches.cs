using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Dig synchronization.
    /// Intercepts DigTool.OnDragTool and syncs dig commands to other players.
    /// Also handles OnLeftClickUp for all DragTool types.
    /// </summary>
    public static class DigSyncPatches
    {
        // Buffer to collect cells during a single drag operation
        private static List<int> _pendingDigCells = new List<int>();

        /// <summary>
        /// Patch for DigTool.OnDragTool - intercepts each cell being marked for dig
        /// </summary>
        public static class DigTool_OnDragTool_Patch
        {
            public static void Postfix(int cell, int distFromOrigin)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                // Add cell to pending list
                if (!_pendingDigCells.Contains(cell))
                {
                    _pendingDigCells.Add(cell);
                }
            }
        }

        /// <summary>
        /// Patch for DragTool.OnLeftClickUp - called when drag ends
        /// Handles all tool types: Dig, Cancel, Build, Deconstruct
        /// </summary>
        public static class DragTool_OnLeftClickUp_Patch
        {
            public static void Postfix(DragTool __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                // Handle based on tool type
                if (__instance is DigTool)
                {
                    SendDigCells();
                }
                else if (__instance is CancelTool)
                {
                    CancelSyncPatches.SendCancelCells();
                }
                else if (__instance is BuildTool)
                {
                    BuildSyncPatches.SendBuildCells();
                }
                else if (__instance is DeconstructTool)
                {
                    DeconstructSyncPatches.SendDeconstructCells();
                }
                else if (__instance is MopTool)
                {
                    MopSyncPatches.SendMopCells();
                }
                else if (__instance is ClearTool)
                {
                    ClearSyncPatches.SendClearCells();
                }
                else if (__instance is HarvestTool)
                {
                    HarvestSyncPatches.SendHarvestCells();
                }
                else if (__instance is DisinfectTool)
                {
                    DisinfectSyncPatches.SendDisinfectCells();
                }
                else if (__instance is PrioritizeTool)
                {
                    PrioritizeSyncPatches.SendPriorityCells();
                }
            }
        }

        private static void SendDigCells()
        {
            if (_pendingDigCells.Count > 0)
            {
                Debug.Log($"[Antigravity] SEND: Dig {_pendingDigCells.Count} cells");
                
                var cmd = new DigCommand
                {
                    Cells = _pendingDigCells.ToArray()
                };
                CommandManager.SendCommand(cmd);

                _pendingDigCells.Clear();
            }
        }

        public static void AddPendingCell(int cell)
        {
            if (!_pendingDigCells.Contains(cell))
            {
                _pendingDigCells.Add(cell);
            }
        }
    }
}
