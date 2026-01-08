using UnityEngine;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for helper methods used by other command handlers
    /// </summary>
    public static partial class CommandManager
    {
        /// <summary>
        /// Get the filter layer string for a game object (same logic as FilteredDragTool).
        /// </summary>
        private static string GetFilterLayerFromGameObject(GameObject obj)
        {
            try
            {
                // Check for building components
                var buildingComplete = obj.GetComponent<BuildingComplete>();
                if (buildingComplete != null)
                {
                    return GetFilterLayerFromObjectLayer(buildingComplete.Def.ObjectLayer);
                }

                var buildingUnderConstruction = obj.GetComponent<BuildingUnderConstruction>();
                if (buildingUnderConstruction != null)
                {
                    return GetFilterLayerFromObjectLayer(buildingUnderConstruction.Def.ObjectLayer);
                }

                // Other object types
                if (obj.GetComponent<Clearable>() != null || obj.GetComponent<Moppable>() != null)
                {
                    return "CLEANANDCLEAR";
                }
                if (obj.GetComponent<Diggable>() != null)
                {
                    return "DIGPLACER";
                }
            }
            catch { }
            return "DEFAULT";
        }

        /// <summary>
        /// Map ObjectLayer to filter layer string (same logic as FilteredDragTool).
        /// </summary>
        private static string GetFilterLayerFromObjectLayer(ObjectLayer layer)
        {
            switch (layer)
            {
                case ObjectLayer.Building:
                case ObjectLayer.Gantry:
                    return "BUILDINGS";
                case ObjectLayer.Wire:
                case ObjectLayer.WireConnectors:
                    return "WIRES";
                case ObjectLayer.LiquidConduit:
                case ObjectLayer.LiquidConduitConnection:
                    return "LIQUIDCONDUIT";
                case ObjectLayer.GasConduit:
                case ObjectLayer.GasConduitConnection:
                    return "GASCONDUIT";
                case ObjectLayer.SolidConduit:
                case ObjectLayer.SolidConduitConnection:
                    return "SOLIDCONDUIT";
                case ObjectLayer.FoundationTile:
                    return "TILES";
                case ObjectLayer.LogicGate:
                case ObjectLayer.LogicWire:
                    return "LOGIC";
                case ObjectLayer.Backwall:
                    return "BACKWALL";
                default:
                    return "DEFAULT";
            }
        }

        /// <summary>
        /// Check if a layer is in the list of active filter layers.
        /// </summary>
        private static bool IsLayerActive(string objFilterLayer, string[] activeFilters)
        {
            if (activeFilters == null || activeFilters.Length == 0) return true;
            
            string upperLayer = objFilterLayer.ToUpper();
            foreach (string filter in activeFilters)
            {
                if (filter.ToUpper() == upperLayer || filter.ToUpper() == "ALL")
                {
                    return true;
                }
            }
            return false;
        }

        private static ObjectLayer GetObjectLayerFromFilter(string filterLayer)
        {
            switch (filterLayer?.ToUpper())
            {
                case "WIRES": return ObjectLayer.Wire;
                case "LIQUIDCONDUIT": return ObjectLayer.LiquidConduit;
                case "GASCONDUIT": return ObjectLayer.GasConduit;
                case "SOLIDCONDUIT": return ObjectLayer.SolidConduit;
                case "LOGIC": return ObjectLayer.LogicWire;
                default: return ObjectLayer.Building;
            }
        }

        private static long GetCurrentTick()
        {
            try
            {
                if (GameClock.Instance != null)
                {
                    return (long)GameClock.Instance.GetTime();
                }
            }
            catch { }
            return 0;
        }
    }
}
