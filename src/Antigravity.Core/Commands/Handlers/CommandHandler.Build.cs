using System.Collections.Generic;
using UnityEngine;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for Build and Deconstruct command execution
    /// </summary>
    public static partial class CommandManager
    {
        private static void ExecuteBuildCommand(BuildCommand cmd)
        {
            if (cmd == null || string.IsNullOrEmpty(cmd.BuildingDefId)) return;

            Debug.Log($"[Antigravity] EXECUTE: Build {cmd.BuildingDefId} at cell {cmd.Cell}");

            try
            {
                var def = Assets.GetBuildingDef(cmd.BuildingDefId);
                if (def == null)
                {
                    Debug.LogWarning($"[Antigravity] BuildingDef not found: {cmd.BuildingDefId}");
                    return;
                }

                // Convert string elements back to Tags
                List<Tag> elements = new List<Tag>();
                if (cmd.SelectedElements != null)
                {
                    foreach (string elem in cmd.SelectedElements)
                    {
                        elements.Add(new Tag(elem));
                    }
                }

                // Get position from cell
                Vector3 pos = Grid.CellToPosCBC(cmd.Cell, Grid.SceneLayer.Building);
                Orientation orientation = (Orientation)cmd.Orientation;

                // Try to place the building blueprint
                GameObject result = def.TryPlace(null, pos, orientation, elements, cmd.FacadeId);
                
                if (result != null)
                {
                    Debug.Log($"[Antigravity] Build placed successfully: {cmd.BuildingDefId}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Build failed: {ex.Message}");
            }
        }

        private static void ExecuteDeconstructCommand(DeconstructCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: Deconstruct {cmd.Cells.Length} cells, filters={string.Join(",", cmd.ActiveFilterLayers ?? new string[] { "ALL" })}");

            // Check if "ALL" filter is active
            bool filterAll = cmd.ActiveFilterLayers == null || 
                             cmd.ActiveFilterLayers.Length == 0 ||
                             System.Array.Exists(cmd.ActiveFilterLayers, f => f.ToUpper() == "ALL");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;
                    
                    // Check each layer (same as DeconstructTool.DeconstructCell)
                    for (int layer = 0; layer < 45; layer++)
                    {
                        GameObject obj = Grid.Objects[cell, layer];
                        if (obj == null) continue;

                        // Get filter layer for this object
                        string objFilterLayer = GetFilterLayerFromGameObject(obj);

                        // Check if this object matches active filters
                        bool shouldDeconstruct = filterAll || IsLayerActive(objFilterLayer, cmd.ActiveFilterLayers);

                        if (shouldDeconstruct)
                        {
                            // Trigger deconstruct event (-790448070)
                            obj.Trigger(-790448070);
                            
                            // Set priority if applicable
                            Prioritizable prioritizable = obj.GetComponent<Prioritizable>();
                            if (prioritizable != null && ToolMenu.Instance?.PriorityScreen != null)
                            {
                                prioritizable.SetMasterPriority(ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority());
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Deconstruct failed at cell {cell}: {ex.Message}");
                }
            }
        }
    }
}
