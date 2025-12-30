using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;
using System.Linq;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Building Settings synchronization.
    /// Intercepts UI actions (sidebar) like priority changes, disinfect toggles, and storage filters.
    /// </summary>
    public static class BuildingSettingsSyncPatches
    {
        /// <summary>
        /// Patch for Prioritizable.SetMasterPriority - syncs priority changes via UI sidebar
        /// </summary>
        public static class Prioritizable_SetMasterPriority_Patch
        {
            public static void Postfix(Prioritizable __instance, PrioritySetting priority)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    Debug.Log($"[Antigravity] SEND: BuildingPriority cell={cell} class={priority.priority_class} value={priority.priority_value}");

                    var cmd = new BuildingPriorityCommand
                    {
                        Cell = cell,
                        PriorityClass = (int)priority.priority_class,
                        PriorityValue = priority.priority_value
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send BuildingPriority: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch for Disinfectable.MarkForDisinfect - syncs disinfect mark via UI
        /// </summary>
        public static class Disinfectable_MarkForDisinfect_Patch
        {
            public static void Postfix(Disinfectable __instance, bool force)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    Debug.Log($"[Antigravity] SEND: BuildingDisinfect cell={cell} mark=true");

                    var cmd = new BuildingDisinfectCommand
                    {
                        Cell = cell,
                        MarkForDisinfect = true
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send BuildingDisinfect mark: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch for Disinfectable cancel - syncs disinfect cancel via UI
        /// We patch OnCancel which is triggered by event 2127324410
        /// </summary>
        public static class Disinfectable_OnCancel_Patch
        {
            public static void Postfix(Disinfectable __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    Debug.Log($"[Antigravity] SEND: BuildingDisinfect cell={cell} mark=false");

                    var cmd = new BuildingDisinfectCommand
                    {
                        Cell = cell,
                        MarkForDisinfect = false
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send BuildingDisinfect cancel: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch for TreeFilterable.UpdateFilters - syncs storage filter changes via UI
        /// </summary>
        public static class TreeFilterable_UpdateFilters_Patch
        {
            public static void Postfix(TreeFilterable __instance, HashSet<Tag> filters)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    // Convert tags to string array
                    string[] tagStrings = filters.Select(t => t.ToString()).ToArray();

                    Debug.Log($"[Antigravity] SEND: StorageFilter cell={cell} tags={tagStrings.Length}");

                    var cmd = new StorageFilterCommand
                    {
                        Cell = cell,
                        AcceptedTags = tagStrings
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send StorageFilter: {ex.Message}");
                }
            }
        }
    }
}
