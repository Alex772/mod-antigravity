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
    /// Respects filter layers (Wires, Buildings, etc.)
    /// </summary>
    public static class DeconstructSyncPatches
    {
        private static List<int> _pendingDeconstructCells = new List<int>();
        private static List<string> _activeFilterLayers = new List<string>();

        /// <summary>
        /// Patch for DeconstructTool.OnDragTool
        /// </summary>
        public static class DeconstructTool_OnDragTool_Patch
        {
            public static void Postfix(FilteredDragTool __instance, int cell)
            {
                if (!(__instance is DeconstructTool)) return;
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
        /// Patch for DeconstructTool.OnActivateTool - capture current filter when tool is activated
        /// </summary>
        public static class DeconstructTool_OnActivateTool_Patch
        {
            public static void Postfix(FilteredDragTool __instance)
            {
                if (!(__instance is DeconstructTool)) return;
                
                // Clear previous filters
                _activeFilterLayers.Clear();
            }
        }

        /// <summary>
        /// Send pending deconstruct cells - called from DigSyncPatches.DragTool_OnLeftClickUp_Patch
        /// </summary>
        public static void SendDeconstructCells()
        {
            if (_pendingDeconstructCells.Count > 0)
            {
                // Capture current active filters from DeconstructTool
                var activeFilters = GetActiveFiltersFromTool();
                
                Debug.Log($"[Antigravity] SEND: Deconstruct {_pendingDeconstructCells.Count} cells, filters=[{string.Join(",", activeFilters)}]");
                
                var cmd = new DeconstructCommand
                {
                    Cells = _pendingDeconstructCells.ToArray(),
                    ActiveFilterLayers = activeFilters
                };
                CommandManager.SendCommand(cmd);

                _pendingDeconstructCells.Clear();
            }
        }

        /// <summary>
        /// Get active filter layers from the DeconstructTool instance.
        /// </summary>
        private static string[] GetActiveFiltersFromTool()
        {
            try
            {
                if (DeconstructTool.Instance == null) return new string[] { "ALL" };

                // Need to check which filters are active via reflection or public API
                // FilteredDragTool uses currentFilterTargets dictionary
                // We'll check common filter layers
                var activeFilters = new List<string>();
                
                // Check each known filter layer
                string[] filterLayers = new string[]
                {
                    "ALL", "WIRES", "LIQUIDCONDUIT", "GASCONDUIT", 
                    "SOLIDCONDUIT", "BUILDINGS", "LOGIC", "BACKWALL"
                };

                foreach (string filterLayer in filterLayers)
                {
                    try
                    {
                        // Use IsActiveLayer to check if this filter is on
                        if (DeconstructTool.Instance.IsActiveLayer(filterLayer))
                        {
                            activeFilters.Add(filterLayer.ToUpper());
                        }
                    }
                    catch { }
                }

                // If no specific filters active, default to ALL
                if (activeFilters.Count == 0 || activeFilters.Contains("ALL"))
                {
                    return new string[] { "ALL" };
                }

                return activeFilters.ToArray();
            }
            catch
            {
                return new string[] { "ALL" };
            }
        }
    }
}
