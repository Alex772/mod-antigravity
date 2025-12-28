using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Build synchronization.
    /// BuildTool uses BuildingDef.TryPlace to create construction blueprints.
    /// </summary>
    public static class BuildSyncPatches
    {
        private static List<BuildData> _pendingBuilds = new List<BuildData>();

        private struct BuildData
        {
            public int Cell;
            public string BuildingDefId;
            public int Orientation;
            public string[] SelectedElements;
            public string FacadeId;
        }

        /// <summary>
        /// Patch for BuildTool.OnDragTool - intercepts build attempts
        /// </summary>
        public static class BuildTool_OnDragTool_Patch
        {
            public static void Postfix(BuildTool __instance, int cell)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                // Get current build info from the tool using reflection
                try
                {
                    var defField = AccessTools.Field(typeof(BuildTool), "def");
                    var orientationField = AccessTools.Field(typeof(BuildTool), "buildingOrientation");
                    var elementsField = AccessTools.Field(typeof(BuildTool), "selectedElements");
                    var facadeField = AccessTools.Field(typeof(BuildTool), "facadeID");

                    var def = defField?.GetValue(__instance) as BuildingDef;
                    if (def == null) return;

                    var orientation = (Orientation)orientationField.GetValue(__instance);
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

                    _pendingBuilds.Add(new BuildData
                    {
                        Cell = cell,
                        BuildingDefId = def.PrefabID,
                        Orientation = (int)orientation,
                        SelectedElements = elementStrings,
                        FacadeId = facade
                    });
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to capture build data: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Send pending builds - called from DigSyncPatches.DragTool_OnLeftClickUp_Patch
        /// </summary>
        public static void SendBuildCells()
        {
            if (_pendingBuilds.Count > 0)
            {
                Debug.Log($"[Antigravity] SEND: Build {_pendingBuilds.Count} items");
                
                foreach (var build in _pendingBuilds)
                {
                    var cmd = new BuildCommand
                    {
                        Cell = build.Cell,
                        BuildingDefId = build.BuildingDefId,
                        Orientation = build.Orientation,
                        SelectedElements = build.SelectedElements,
                        FacadeId = build.FacadeId
                    };
                    CommandManager.SendCommand(cmd);
                }

                _pendingBuilds.Clear();
            }
        }
    }
}
