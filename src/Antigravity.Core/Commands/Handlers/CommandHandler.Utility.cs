using System.Collections.Generic;
using UnityEngine;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for Utility build/disconnect commands (wires, pipes, etc.)
    /// </summary>
    public static partial class CommandManager
    {
        private static void ExecuteUtilityBuildCommand(UtilityBuildCommand cmd)
        {
            if (cmd?.PathCells == null || cmd.PathCells.Length == 0 || string.IsNullOrEmpty(cmd.BuildingDefId))
                return;

            Debug.Log($"[Antigravity] EXECUTE: UtilityBuild {cmd.BuildingDefId} with {cmd.PathCells.Length} cells");

            try
            {
                var def = Assets.GetBuildingDef(cmd.BuildingDefId);
                if (def == null)
                {
                    Debug.LogWarning($"[Antigravity] BuildingDef not found: {cmd.BuildingDefId}");
                    return;
                }

                // Convert string elements back to Tags
                var elements = new List<Tag>();
                if (cmd.SelectedElements != null)
                {
                    foreach (string elem in cmd.SelectedElements)
                    {
                        elements.Add(new Tag(elem));
                    }
                }

                // Get the utility network manager
                IUtilityNetworkMgr conduitMgr = null;
                try
                {
                    var networkComponent = def.BuildingComplete.GetComponent<IHaveUtilityNetworkMgr>();
                    if (networkComponent != null)
                    {
                        conduitMgr = networkComponent.GetNetworkManager();
                    }
                }
                catch { }

                // STEP 1: Add connections to conduit system BEFORE placing
                if (conduitMgr != null && cmd.PathCells.Length >= 2)
                {
                    for (int i = 1; i < cmd.PathCells.Length; i++)
                    {
                        int cell1 = cmd.PathCells[i - 1];
                        int cell2 = cmd.PathCells[i];
                        
                        if (!Grid.IsValidCell(cell1) || !Grid.IsValidCell(cell2)) continue;

                        UtilityConnections direction = UtilityConnectionsExtensions.DirectionFromToCell(cell1, cell2);
                        if (direction != (UtilityConnections)0)
                        {
                            UtilityConnections inverseDir = direction.InverseDirection();
                            conduitMgr.AddConnection(direction, cell1, false);
                            conduitMgr.AddConnection(inverseDir, cell2, false);
                        }
                    }
                }

                // STEP 2: Place buildings at each cell
                int placedCount = 0;
                for (int i = 0; i < cmd.PathCells.Length; i++)
                {
                    int cell = cmd.PathCells[i];
                    if (!Grid.IsValidCell(cell)) continue;

                    Vector3 pos = Grid.CellToPosCBC(cell, Grid.SceneLayer.Building);
                    GameObject existingObj = Grid.Objects[cell, (int)def.TileLayer];
                    
                    if (existingObj == null)
                    {
                        GameObject placedObj = def.TryPlace(null, pos, Orientation.Neutral, elements, cmd.FacadeId);
                        
                        if (placedObj != null)
                        {
                            placedCount++;
                            if (conduitMgr != null)
                            {
                                UtilityConnections connections = conduitMgr.GetConnections(cell, false);
                                var utilityItem = placedObj.GetComponent<KAnimGraphTileVisualizer>();
                                if (utilityItem != null)
                                {
                                    utilityItem.Connections = connections;
                                }
                                TileVisualizer.RefreshCell(cell, def.TileLayer, def.ReplacementLayer);
                            }
                        }
                    }
                    else
                    {
                        // Update connections on existing piece
                        if (conduitMgr != null)
                        {
                            var utilityItem = existingObj.GetComponent<KAnimGraphTileVisualizer>();
                            if (utilityItem != null)
                            {
                                UtilityConnections connections = utilityItem.Connections;
                                connections |= conduitMgr.GetConnections(cell, false);
                                utilityItem.UpdateConnections(connections);
                            }
                            TileVisualizer.RefreshCell(cell, def.TileLayer, def.ReplacementLayer);
                        }
                    }
                }

                Debug.Log($"[Antigravity] UtilityBuild completed: {placedCount}/{cmd.PathCells.Length} cells placed");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] UtilityBuild failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute a disconnect utility command (cut wire/pipe connections).
        /// </summary>
        private static void ExecuteDisconnectCommand(DisconnectCommand cmd)
        {
            if (cmd == null) return;
            if (!Grid.IsValidCell(cmd.Cell)) return;

            Debug.Log($"[Antigravity] EXECUTE: DisconnectUtility cell={cmd.Cell} connections={cmd.RemoveConnections} layer={cmd.FilterLayer}");

            try
            {
                var removeConnections = (UtilityConnections)cmd.RemoveConnections;
                
                // Search through layers for matching object
                for (int layer = 0; layer < 45; layer++)
                {
                    GameObject obj = Grid.Objects[cmd.Cell, layer];
                    if (obj == null) continue;

                    var building = obj.GetComponent<Building>();
                    if (building == null) continue;

                    // Check if this matches our target filter layer
                    string objFilterLayer = GetFilterLayerFromObjectLayer(building.Def.ObjectLayer);
                    if (objFilterLayer.ToUpper() != cmd.FilterLayer.ToUpper() && cmd.FilterLayer.ToUpper() != "ALL")
                        continue;

                    // Get network manager
                    var networkComponent = building.Def.BuildingComplete.GetComponent<IHaveUtilityNetworkMgr>();
                    if (networkComponent.IsNullOrDestroyed()) continue;

                    var networkMgr = networkComponent.GetNetworkManager();
                    if (networkMgr == null) continue;

                    // Get current connections and calculate new connections (remove specified)
                    UtilityConnections currentConnections = networkMgr.GetConnections(cmd.Cell, is_physical_building: false);
                    UtilityConnections newConnections = currentConnections & ~removeConnections;

                    // Update the visual tile
                    var tileVisualizer = obj.GetComponent<KAnimGraphTileVisualizer>();
                    if (tileVisualizer != null)
                    {
                        tileVisualizer.UpdateConnections(newConnections);
                        tileVisualizer.Refresh();
                    }

                    // Refresh the tile layer
                    TileVisualizer.RefreshCell(cmd.Cell, building.Def.TileLayer, building.Def.ReplacementLayer);
                    
                    // Force rebuild networks
                    networkMgr.ForceRebuildNetworks();
                    
                    Debug.Log($"[Antigravity] DisconnectUtility success: removed connections at cell {cmd.Cell}");
                    break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] DisconnectUtility failed at cell {cmd.Cell}: {ex.Message}");
            }
        }
    }
}
