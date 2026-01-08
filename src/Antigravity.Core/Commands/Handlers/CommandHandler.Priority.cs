using UnityEngine;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for Priority commands: SetPriority, BulkPriority, BuildingPriority
    /// </summary>
    public static partial class CommandManager
    {
        private static void ExecutePriorityCommand(PriorityCommand cmd)
        {
            if (cmd == null) return;
            Debug.Log($"[Antigravity] Executed priority {cmd.Priority} at cell {cmd.Cell}");
        }

        private static void ExecuteBulkPriorityCommand(BulkPriorityCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: BulkPriority {cmd.Cells.Length} cells");

            PrioritySetting priority = new PrioritySetting(
                (PriorityScreen.PriorityClass)cmd.PriorityClass,
                cmd.PriorityValue
            );

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;

                    // Same as PrioritizeTool.OnDragTool
                    for (int layer = 0; layer < 45; layer++)
                    {
                        GameObject obj = Grid.Objects[cell, layer];
                        if (obj == null) continue;

                        Prioritizable component = obj.GetComponent<Prioritizable>();
                        if (component != null && component.showIcon && component.IsPrioritizable())
                        {
                            component.SetMasterPriority(priority);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Priority failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecuteBuildingPriorityCommand(BuildingPriorityCommand cmd)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: BuildingPriority cell={cmd.Cell} class={cmd.PriorityClass} value={cmd.PriorityValue}");

            try
            {
                if (!Grid.IsValidCell(cmd.Cell)) return;

                PrioritySetting priority = new PrioritySetting(
                    (PriorityScreen.PriorityClass)cmd.PriorityClass,
                    cmd.PriorityValue
                );

                // Find building at cell and set priority
                for (int layer = 0; layer < 45; layer++)
                {
                    GameObject obj = Grid.Objects[cmd.Cell, layer];
                    if (obj == null) continue;

                    Prioritizable prioritizable = obj.GetComponent<Prioritizable>();
                    if (prioritizable != null && prioritizable.IsPrioritizable())
                    {
                        prioritizable.SetMasterPriority(priority);
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] BuildingPriority failed at cell {cmd.Cell}: {ex.Message}");
            }
        }
    }
}
