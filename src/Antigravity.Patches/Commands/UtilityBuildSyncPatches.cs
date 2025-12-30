using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Utility Build synchronization (wires, pipes, logic wires, etc.).
    /// BaseUtilityBuildTool uses BuildPath() instead of OnDragTool for creating utilities.
    /// </summary>
    public static class UtilityBuildSyncPatches
    {
        /// <summary>
        /// Patch for BaseUtilityBuildTool.BuildPath - intercepts when utility paths are built.
        /// Captures the entire path and sends it as a single UtilityBuildCommand.
        /// </summary>
        public static class BaseUtilityBuildTool_BuildPath_Patch
        {
            public static void Prefix(BaseUtilityBuildTool __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    // Get path and def fields via reflection
                    var pathField = AccessTools.Field(typeof(BaseUtilityBuildTool), "path");
                    var defField = AccessTools.Field(typeof(BaseUtilityBuildTool), "def");
                    var elementsField = AccessTools.Field(typeof(BaseUtilityBuildTool), "selectedElements");
                    var facadeField = AccessTools.Field(typeof(BaseUtilityBuildTool), "facadeID");

                    var def = defField?.GetValue(__instance) as BuildingDef;
                    if (def == null) return;

                    var pathList = pathField?.GetValue(__instance) as System.Collections.IList;
                    if (pathList == null || pathList.Count == 0) return;

                    var elements = elementsField?.GetValue(__instance) as IList<Tag>;
                    var facade = facadeField?.GetValue(__instance) as string;

                    // Convert elements to string array
                    string[] elementStrings = null;
                    if (elements != null)
                    {
                        elementStrings = new string[elements.Count];
                        for (int i = 0; i < elements.Count; i++)
                        {
                            elementStrings[i] = elements[i].ToString();
                        }
                    }

                    // Get cells from path using reflection on PathNode struct
                    var pathCells = new List<int>();
                    foreach (var node in pathList)
                    {
                        var cellField = node.GetType().GetField("cell");
                        if (cellField != null)
                        {
                            int cell = (int)cellField.GetValue(node);
                            pathCells.Add(cell);
                        }
                    }

                    if (pathCells.Count == 0) return;

                    Debug.Log($"[Antigravity] SEND: UtilityBuild {def.PrefabID} with {pathCells.Count} cells");

                    // Send a single UtilityBuildCommand with the entire path
                    var cmd = new UtilityBuildCommand
                    {
                        BuildingDefId = def.PrefabID,
                        PathCells = pathCells.ToArray(),
                        SelectedElements = elementStrings,
                        FacadeId = facade
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send utility build: {ex.Message}");
                }
            }
        }
    }
}
