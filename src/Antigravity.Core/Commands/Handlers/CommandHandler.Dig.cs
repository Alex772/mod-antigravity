namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for Dig and CancelDig command execution
    /// </summary>
    public static partial class CommandManager
    {
        private static void ExecuteDigCommand(DigCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[Antigravity] DigCommand has no cells");
                return;
            }

            UnityEngine.Debug.Log($"[Antigravity] EXECUTE: Dig {cmd.Cells.Length} cells");

            int successCount = 0;
            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (Grid.IsValidCell(cell))
                    {
                        // Use DigTool.PlaceDig to create the dig marker (same as game does)
                        var result = DigTool.PlaceDig(cell, successCount);
                        if (result != null)
                        {
                            successCount++;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[Antigravity] Failed to place dig at cell {cell}: {ex.Message}");
                }
            }

            UnityEngine.Debug.Log($"[Antigravity] Placed {successCount}/{cmd.Cells.Length} dig markers");
        }

        private static void ExecuteCancelDigCommand(DigCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            UnityEngine.Debug.Log($"[Antigravity] EXECUTE: CancelDig {cmd.Cells.Length} cells");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    // Cancel all cancellable objects in the cell (same as CancelTool)
                    for (int layer = 0; layer < 45; layer++)
                    {
                        UnityEngine.GameObject obj = Grid.Objects[cell, layer];
                        if (obj != null)
                        {
                            // Trigger cancel event (2127324410)
                            obj.Trigger(2127324410);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[Antigravity] Cancel failed at cell {cell}: {ex.Message}");
                }
            }
        }
    }
}
