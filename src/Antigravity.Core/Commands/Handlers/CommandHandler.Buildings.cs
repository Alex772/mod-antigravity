using System.Collections.Generic;
using UnityEngine;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for Building commands: Door, Disinfect toggle, Storage filter/capacity
    /// </summary>
    public static partial class CommandManager
    {
        private static void ExecuteDoorStateCommand(DoorStateCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: DoorState cell={cmd.Cell} state={cmd.ControlState}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;

                // Find door at cell
                GameObject obj = Grid.Objects[cmd.Cell, 1]; // Building layer
                if (obj == null) return;

                Door door = obj.GetComponent<Door>();
                if (door == null) return;

                // Set the door state (0=Auto, 1=Open, 2=Locked)
                Door.ControlState state = (Door.ControlState)cmd.ControlState;
                door.QueueStateChange(state);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] DoorState failed at cell {cmd.Cell}: {ex.Message}");
            }
        }

        private static void ExecuteBuildingDisinfectCommand(BuildingDisinfectCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: BuildingDisinfect cell={cmd.Cell} mark={cmd.MarkForDisinfect}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;

                // Find disinfectable at cell
                for (int layer = 0; layer < 45; layer++)
                {
                    GameObject obj = Grid.Objects[cmd.Cell, layer];
                    if (obj == null) continue;

                    Disinfectable disinfectable = obj.GetComponent<Disinfectable>();
                    if (disinfectable != null)
                    {
                        if (cmd.MarkForDisinfect)
                        {
                            disinfectable.MarkForDisinfect();
                        }
                        else
                        {
                            // CancelDisinfection is private, so we trigger the cancel event
                            obj.Trigger(2127324410);
                        }
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] BuildingDisinfect failed at cell {cmd.Cell}: {ex.Message}");
            }
        }

        private static void ExecuteStorageFilterCommand(StorageFilterCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: StorageFilter cell={cmd.Cell} tags={cmd.AcceptedTags?.Length ?? 0}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;
                if (cmd.AcceptedTags == null) return;

                // Convert string tags back to Tag set
                HashSet<Tag> tags = new HashSet<Tag>();
                foreach (string tagStr in cmd.AcceptedTags)
                {
                    tags.Add(new Tag(tagStr));
                }

                // Find TreeFilterable at cell
                for (int layer = 0; layer < 45; layer++)
                {
                    GameObject obj = Grid.Objects[cmd.Cell, layer];
                    if (obj == null) continue;

                    TreeFilterable filterable = obj.GetComponent<TreeFilterable>();
                    if (filterable != null)
                    {
                        filterable.UpdateFilters(tags);
                        
                        // Force refresh the UI if this object is currently selected
                        try
                        {
                            if (DetailsScreen.Instance != null && 
                                SelectTool.Instance != null && 
                                SelectTool.Instance.selected != null &&
                                SelectTool.Instance.selected.gameObject == obj)
                            {
                                DetailsScreen.Instance.Refresh(obj);
                            }
                        }
                        catch { }
                        
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] StorageFilter failed at cell {cmd.Cell}: {ex.Message}");
            }
        }

        private static void ExecuteStorageCapacityCommand(StorageCapacityCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: StorageCapacity cell={cmd.Cell} capacity={cmd.UserMaxCapacity}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;

                // Find IUserControlledCapacity at cell (StorageLocker, Refrigerator, etc.)
                for (int layer = 0; layer < 45; layer++)
                {
                    GameObject obj = Grid.Objects[cmd.Cell, layer];
                    if (obj == null) continue;

                    IUserControlledCapacity capacityControl = obj.GetComponent<IUserControlledCapacity>();
                    if (capacityControl != null)
                    {
                        capacityControl.UserMaxCapacity = cmd.UserMaxCapacity;
                        
                        // Force refresh the UI if this object is currently selected
                        try
                        {
                            if (DetailsScreen.Instance != null && 
                                SelectTool.Instance != null && 
                                SelectTool.Instance.selected != null &&
                                SelectTool.Instance.selected.gameObject == obj)
                            {
                                DetailsScreen.Instance.Refresh(obj);
                            }
                        }
                        catch { }
                        
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] StorageCapacity failed at cell {cmd.Cell}: {ex.Message}");
            }
        }

        private static void ExecuteFilterableCommand(FilterableCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: Filterable cell={cmd.Cell} tag={cmd.SelectedTag}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;

                // Find Filterable at cell (valves, pumps, etc.)
                for (int layer = 0; layer < 45; layer++)
                {
                    GameObject obj = Grid.Objects[cmd.Cell, layer];
                    if (obj == null) continue;

                    Filterable filterable = obj.GetComponent<Filterable>();
                    if (filterable != null)
                    {
                        Tag tag = new Tag(cmd.SelectedTag);
                        filterable.SelectedTag = tag;
                        
                        // Force refresh the UI if currently selected
                        RefreshDetailsScreenIfSelected(obj);
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Filterable failed at cell {cmd.Cell}: {ex.Message}");
            }
        }

        private static void ExecuteThresholdCommand(ThresholdCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: Threshold cell={cmd.Cell} value={cmd.Threshold} above={cmd.ActivateAbove}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;

                // Find IThresholdSwitch at cell
                for (int layer = 0; layer < 45; layer++)
                {
                    GameObject obj = Grid.Objects[cmd.Cell, layer];
                    if (obj == null) continue;

                    IThresholdSwitch threshold = obj.GetComponent<IThresholdSwitch>();
                    if (threshold != null)
                    {
                        threshold.Threshold = cmd.Threshold;
                        threshold.ActivateAboveThreshold = cmd.ActivateAbove;
                        
                        // Force refresh the UI if currently selected
                        RefreshDetailsScreenIfSelected(obj);
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Threshold failed at cell {cmd.Cell}: {ex.Message}");
            }
        }

        private static void ExecuteLogicSwitchCommand(LogicSwitchCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: LogicSwitch cell={cmd.Cell} on={cmd.SwitchOn}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;

                // Find LogicSwitch at cell
                for (int layer = 0; layer < 45; layer++)
                {
                    GameObject obj = Grid.Objects[cmd.Cell, layer];
                    if (obj == null) continue;

                    LogicSwitch logicSwitch = obj.GetComponent<LogicSwitch>();
                    if (logicSwitch != null)
                    {
                        // Toggle if needed - SetState is protected, use Traverse
                        if (logicSwitch.IsSwitchedOn != cmd.SwitchOn)
                        {
                            HarmonyLib.Traverse.Create(logicSwitch).Method("SetState", cmd.SwitchOn).GetValue();
                        }
                        
                        // Force refresh the UI if currently selected
                        RefreshDetailsScreenIfSelected(obj);
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] LogicSwitch failed at cell {cmd.Cell}: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper to refresh UI if the given object is currently selected
        /// </summary>
        private static void RefreshDetailsScreenIfSelected(GameObject obj)
        {
            try
            {
                if (DetailsScreen.Instance != null && 
                    SelectTool.Instance != null && 
                    SelectTool.Instance.selected != null &&
                    SelectTool.Instance.selected.gameObject == obj)
                {
                    DetailsScreen.Instance.Refresh(obj);
                }
            }
            catch { }
        }
    }
}

