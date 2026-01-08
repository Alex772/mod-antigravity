using UnityEngine;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for Tool commands: Mop, Clear, Harvest, Disinfect, Capture
    /// </summary>
    public static partial class CommandManager
    {
        private static void ExecuteMopCommand(MopCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: Mop {cmd.Cells.Length} cells");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;

                    // Check if instant build mode (same as MopTool)
                    if (DebugHandler.InstantBuildMode)
                    {
                        Moppable.MopCell(cell, 1000000f, null);
                        continue;
                    }

                    // Create mop placer (same as MopTool.OnDragTool)
                    GameObject placer = Assets.GetPrefab(new Tag("MopPlacer"));
                    if (placer != null)
                    {
                        GameObject obj = Util.KInstantiate(placer);
                        Grid.Objects[cell, 8] = obj;
                        Vector3 position = Grid.CellToPosCBC(cell, Grid.SceneLayer.Move);
                        position.z += -0.15f;
                        obj.transform.SetPosition(position);
                        obj.SetActive(true);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Mop failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecuteClearCommand(ClearCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: Clear {cmd.Cells.Length} cells");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;

                    // Get pickupables in cell (same as ClearTool.OnDragTool)
                    GameObject gameObject = Grid.Objects[cell, 3];
                    if (gameObject == null) continue;

                    var pickupable = gameObject.GetComponent<Pickupable>();
                    if (pickupable == null) continue;

                    ObjectLayerListItem objectLayerListItem = pickupable.objectLayerListItem;
                    while (objectLayerListItem != null)
                    {
                        GameObject obj = objectLayerListItem.gameObject;
                        Pickupable pickup = objectLayerListItem.pickupable;
                        objectLayerListItem = objectLayerListItem.nextItem;

                        if (obj != null && !pickup.KPrefabID.HasTag(GameTags.BaseMinion) && pickup.Clearable != null && pickup.Clearable.isClearable)
                        {
                            pickup.Clearable.MarkForClear();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Clear failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecuteHarvestCommand(HarvestCommand cmd, bool harvestWhenReady)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: Harvest {cmd.Cells.Length} cells (ready={harvestWhenReady})");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;

                    // Find HarvestDesignatables in cell (same as HarvestTool.OnDragTool)
                    foreach (HarvestDesignatable item in Components.HarvestDesignatables.Items)
                    {
                        OccupyArea area = item.area;
                        if (Grid.PosToCell(item) != cell && (area == null || !area.CheckIsOccupying(cell)))
                            continue;

                        if (harvestWhenReady)
                        {
                            item.SetHarvestWhenReady(true);
                        }
                        else
                        {
                            Harvestable component = item.GetComponent<Harvestable>();
                            if (component != null)
                            {
                                component.Trigger(2127324410); // Cancel harvest
                            }
                            item.SetHarvestWhenReady(false);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Harvest failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecuteDisinfectCommand(DisinfectCommand cmd)
        {
            if (cmd?.Cells == null || cmd.Cells.Length == 0) return;

            Debug.Log($"[Antigravity] EXECUTE: Disinfect {cmd.Cells.Length} cells");

            foreach (int cell in cmd.Cells)
            {
                try
                {
                    if (!Grid.IsValidCell(cell)) continue;

                    // Same as DisinfectTool.OnDragTool
                    for (int layer = 0; layer < 45; layer++)
                    {
                        GameObject obj = Grid.Objects[cell, layer];
                        if (obj != null)
                        {
                            Disinfectable component = obj.GetComponent<Disinfectable>();
                            if (component != null && component.GetComponent<PrimaryElement>().DiseaseCount > 0)
                            {
                                component.MarkForDisinfect();
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Antigravity] Disinfect failed at cell {cell}: {ex.Message}");
                }
            }
        }

        private static void ExecuteCaptureCommand(CaptureCommand cmd, bool mark)
        {
            if (cmd == null) return;

            Debug.Log($"[Antigravity] EXECUTE: Capture area ({cmd.MinX},{cmd.MinY})-({cmd.MaxX},{cmd.MaxY}) mark={mark}");

            try
            {
                Vector2 min = new Vector2(cmd.MinX, cmd.MinY);
                Vector2 max = new Vector2(cmd.MaxX, cmd.MaxY);

                // Same as CaptureTool.MarkForCapture
                foreach (Capturable item in Components.Capturables.Items)
                {
                    Vector2 vector = Grid.PosToXY(item.transform.GetPosition());
                    if (vector.x >= min.x && vector.x < max.x && vector.y >= min.y && vector.y < max.y)
                    {
                        if (item.allowCapture)
                        {
                            PrioritySetting priority = new PrioritySetting(
                                (PriorityScreen.PriorityClass)cmd.PriorityClass,
                                cmd.PriorityValue
                            );
                            item.MarkForCapture(mark, priority, true);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Capture failed: {ex.Message}");
            }
        }
    }
}
