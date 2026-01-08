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

        /// <summary>
        /// Patch for StorageLocker.set_UserMaxCapacity - syncs storage max capacity changes
        /// </summary>
        [HarmonyPatch(typeof(StorageLocker), nameof(StorageLocker.UserMaxCapacity), MethodType.Setter)]
        public static class StorageLocker_UserMaxCapacity_Patch
        {
            public static void Postfix(StorageLocker __instance, float value)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    Debug.Log($"[Antigravity] SEND: StorageCapacity cell={cell} capacity={value}");

                    var cmd = new StorageCapacityCommand
                    {
                        Cell = cell,
                        UserMaxCapacity = value
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send StorageCapacity: {ex.Message}");
                }
            }
        }
<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> 09d72f0 (falhando)

        /// <summary>
        /// Patch for Door.QueueStateChange - syncs door state changes
        /// </summary>
        public static class Door_QueueStateChange_Patch
        {
            public static void Postfix(Door __instance, Door.ControlState nextState)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    Debug.Log($"[Antigravity] SEND: DoorState cell={cell} state={nextState}");

                    var cmd = new DoorStateCommand
                    {
                        Cell = cell,
                        ControlState = (int)nextState
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send DoorState: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch for Filterable.SelectedTag setter - syncs element filter changes (valves, pumps)
        /// </summary>
        public static class Filterable_SelectedTag_Patch
        {
            public static void Postfix(Filterable __instance, Tag value)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    Debug.Log($"[Antigravity] SEND: Filterable cell={cell} tag={value}");

                    var cmd = new FilterableCommand
                    {
                        Cell = cell,
                        SelectedTag = value.ToString()
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send Filterable: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch for IThresholdSwitch.SetThreshold - syncs sensor threshold changes
        /// We need to patch the concrete implementations since IThresholdSwitch is an interface
        /// </summary>
        public static class ThresholdSwitch_SetThreshold_Patch
        {
            public static void Postfix(KMonoBehaviour __instance, float value, bool activateAbove)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    Debug.Log($"[Antigravity] SEND: Threshold cell={cell} value={value} activateAbove={activateAbove}");

                    var cmd = new ThresholdCommand
                    {
                        Cell = cell,
                        Threshold = value,
                        ActivateAbove = activateAbove
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send Threshold: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch for LogicSwitch.SetFirstFrameCallback or toggle method - syncs logic switch state
        /// </summary>
        public static class LogicSwitch_Toggle_Patch
        {
            public static void Postfix(LogicSwitch __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                try
                {
                    int cell = Grid.PosToCell(__instance.transform.GetPosition());
                    if (!Grid.IsValidCell(cell)) return;

                    // Get current switch state
                    bool switchOn = __instance.IsSwitchedOn;

                    Debug.Log($"[Antigravity] SEND: LogicSwitch cell={cell} on={switchOn}");

                    var cmd = new LogicSwitchCommand
                    {
                        Cell = cell,
                        SwitchOn = switchOn
                    };
                    CommandManager.SendCommand(cmd);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Failed to send LogicSwitch: {ex.Message}");
                }
            }
        }
<<<<<<< HEAD
=======
>>>>>>> 3663ac0 (feat: Introduce network packet debugger and command synchronization for building settings and disconnects.)
=======
>>>>>>> 09d72f0 (falhando)
    }
}

