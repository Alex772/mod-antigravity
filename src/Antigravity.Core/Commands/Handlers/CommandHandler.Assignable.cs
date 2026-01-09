using UnityEngine;
using System.Linq;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Command handlers for building assignment and enabled state.
    /// </summary>
    public static partial class CommandManager
    {
        /// <summary>
        /// Execute Assignable command - assign/unassign duplicant to building
        /// </summary>
        private static void ExecuteAssignableCommand(AssignableCommand cmd)
        {
            if (cmd == null) return;
            
            try
            {
                if (!Grid.IsValidCell(cmd.Cell))
                {
                    Debug.LogWarning($"[Antigravity] SetAssignable: Invalid cell {cmd.Cell}");
                    return;
                }

                // Find the Assignable component at this cell
                GameObject buildingGO = Grid.Objects[cmd.Cell, (int)ObjectLayer.Building];
                if (buildingGO == null)
                {
                    Debug.LogWarning($"[Antigravity] SetAssignable: No building at cell {cmd.Cell}");
                    return;
                }

                Assignable assignable = buildingGO.GetComponent<Assignable>();
                if (assignable == null)
                {
                    Debug.LogWarning($"[Antigravity] SetAssignable: Building has no Assignable component");
                    return;
                }

                if (cmd.IsUnassign)
                {
                    // Unassign
                    assignable.Unassign();
                    Debug.Log($"[Antigravity] EXECUTE: SetAssignable Unassign cell={cmd.Cell}");
                }
                else
                {
                    // Find the duplicant by name
                    if (string.IsNullOrEmpty(cmd.MinionName))
                    {
                        // If no name provided but not unassign, this is public assignment
                        if (assignable.canBePublic)
                        {
                            var publicGroup = Game.Instance.assignmentManager.assignment_groups["public"];
                            assignable.Assign(publicGroup);
                            Debug.Log($"[Antigravity] EXECUTE: SetAssignable Public cell={cmd.Cell}");
                        }
                        return;
                    }

                    // Find duplicant by name
                    MinionIdentity minion = Components.LiveMinionIdentities.Items
                        .FirstOrDefault(m => m.GetProperName() == cmd.MinionName);
                    
                    if (minion == null)
                    {
                        Debug.LogWarning($"[Antigravity] SetAssignable: Could not find duplicant '{cmd.MinionName}'");
                        return;
                    }

                    // Get the MinionAssignablesProxy for assignment
                    MinionAssignablesProxy proxy = minion.GetComponent<MinionAssignablesProxy>();
                    if (proxy == null)
                    {
                        proxy = minion.gameObject.GetComponent<MinionAssignablesProxy>();
                    }

                    if (proxy != null)
                    {
                        assignable.Assign(proxy);
                        Debug.Log($"[Antigravity] EXECUTE: SetAssignable cell={cmd.Cell} to '{cmd.MinionName}'");
                    }
                    else
                    {
                        Debug.LogWarning($"[Antigravity] SetAssignable: Could not get proxy for '{cmd.MinionName}'");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] SetAssignable failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute BuildingEnabled command - enable/disable building
        /// </summary>
        private static void ExecuteBuildingEnabledCommand(BuildingEnabledCommand cmd)
        {
            if (cmd == null) return;
            
            try
            {
                if (!Grid.IsValidCell(cmd.Cell))
                {
                    Debug.LogWarning($"[Antigravity] SetBuildingEnabled: Invalid cell {cmd.Cell}");
                    return;
                }

                // Find the building at this cell
                GameObject buildingGO = Grid.Objects[cmd.Cell, (int)ObjectLayer.Building];
                if (buildingGO == null)
                {
                    Debug.LogWarning($"[Antigravity] SetBuildingEnabled: No building at cell {cmd.Cell}");
                    return;
                }

                // Try to find BuildingEnabledButton component (used for enable/disable checkbox)
                BuildingEnabledButton enableButton = buildingGO.GetComponent<BuildingEnabledButton>();
                if (enableButton != null)
                {
                    // Use IsEnabled property which is public
                    enableButton.IsEnabled = cmd.Enabled;
                    Debug.Log($"[Antigravity] EXECUTE: SetBuildingEnabled cell={cmd.Cell} enabled={cmd.Enabled}");
                    return;
                }

                // Alternative: Use Operational component directly with the EnabledFlag
                Operational operational = buildingGO.GetComponent<Operational>();
                if (operational != null)
                {
                    operational.SetFlag(BuildingEnabledButton.EnabledFlag, cmd.Enabled);
                    Debug.Log($"[Antigravity] EXECUTE: SetBuildingEnabled (Operational) cell={cmd.Cell} enabled={cmd.Enabled}");
                    return;
                }

                Debug.LogWarning($"[Antigravity] SetBuildingEnabled: No enable control found at cell {cmd.Cell}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] SetBuildingEnabled failed: {ex.Message}");
            }
        }
    }
}
