using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Assignable synchronization.
    /// Intercepts assignment changes on beds, toilets, mess tables, etc.
    /// </summary>
    public static class AssignableSyncPatches
    {
        /// <summary>
        /// Patch for Assignable.Assign - syncs when a duplicant is assigned to a building
        /// </summary>
        [HarmonyPatch(typeof(Assignable), nameof(Assignable.Assign), typeof(IAssignableIdentity))]
        public static class Assignable_Assign_Patch
        {
            public static void Postfix(Assignable __instance, IAssignableIdentity new_assignee)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    string minionName = null;
                    bool isPublic = false;

                    // Determine what kind of assignment this is
                    if (new_assignee == null)
                    {
                        // This shouldn't happen through Assign, but handle it
                        return;
                    }
                    else if (new_assignee is MinionAssignablesProxy proxy)
                    {
                        // Assigned to a specific duplicant
                        var minion = proxy.GetTargetGameObject()?.GetComponent<MinionIdentity>();
                        if (minion != null)
                        {
                            minionName = minion.GetProperName();
                        }
                    }
                    else if (new_assignee is MinionIdentity minion)
                    {
                        minionName = minion.GetProperName();
                    }
                    else if (new_assignee is AssignmentGroup group)
                    {
                        // Public assignment
                        isPublic = group.id == "public";
                        if (isPublic)
                        {
                            // We don't sync public assignments as they're the default
                            return;
                        }
                    }

                    if (string.IsNullOrEmpty(minionName) && !isPublic)
                    {
                        return;
                    }

                    Debug.Log($"[Antigravity] SEND: SetAssignable cell={cell} slot={__instance.slotID} minion={minionName}");

                    var cmd = new AssignableCommand
                    {
                        Cell = cell,
                        SlotId = __instance.slotID,
                        MinionName = minionName,
                        IsUnassign = false
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send Assignable assign: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch for Assignable.Unassign - syncs when a duplicant is unassigned from a building
        /// </summary>
        [HarmonyPatch(typeof(Assignable), nameof(Assignable.Unassign))]
        public static class Assignable_Unassign_Patch
        {
            public static void Postfix(Assignable __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    Debug.Log($"[Antigravity] SEND: SetAssignable Unassign cell={cell} slot={__instance.slotID}");

                    var cmd = new AssignableCommand
                    {
                        Cell = cell,
                        SlotId = __instance.slotID,
                        MinionName = null,
                        IsUnassign = true
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send Assignable unassign: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch for BuildingEnabledButton - syncs when a building is enabled/disabled
        /// </summary>
        [HarmonyPatch(typeof(BuildingEnabledButton), "OnMenuToggle")]
        public static class BuildingEnabledButton_OnMenuToggle_Patch
        {
            public static void Postfix(BuildingEnabledButton __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    // Get current state after toggle
                    bool isEnabled = __instance.IsEnabled;

                    Debug.Log($"[Antigravity] SEND: SetBuildingEnabled cell={cell} enabled={isEnabled}");

                    var cmd = new BuildingEnabledCommand
                    {
                        Cell = cell,
                        Enabled = isEnabled
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send BuildingEnabled: {ex.Message}");
                }
            }
        }
    }
}
