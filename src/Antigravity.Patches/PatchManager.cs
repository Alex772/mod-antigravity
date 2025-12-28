using HarmonyLib;

namespace Antigravity.Patches
{
    /// <summary>
    /// Manages all Harmony patches for the mod.
    /// </summary>
    public static class PatchManager
    {
        /// <summary>
        /// Whether patches have been applied.
        /// </summary>
        public static bool PatchesApplied { get; private set; }

        /// <summary>
        /// Apply all patches.
        /// </summary>
        /// <param name="harmony">Harmony instance to use.</param>
        public static void ApplyPatches(Harmony harmony)
        {
            if (PatchesApplied)
            {
                UnityEngine.Debug.LogWarning("[Antigravity] Patches already applied!");
                return;
            }

            try
            {
                // Apply UI patches
                ApplyUIPatches(harmony);

                // Apply game patches
                ApplyGamePatches(harmony);

                // Apply simulation patches
                ApplySimulationPatches(harmony);

                PatchesApplied = true;
                UnityEngine.Debug.Log("[Antigravity] All patches applied successfully.");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[Antigravity] Failed to apply patches: {ex.Message}");
                UnityEngine.Debug.LogError(ex.StackTrace);
            }
        }

        private static void ApplyUIPatches(Harmony harmony)
        {
            // Main menu patch - adds Multiplayer button
            harmony.Patch(
                AccessTools.Method(typeof(MainMenu), "OnSpawn"),
                postfix: new HarmonyMethod(typeof(UI.MainMenuPatch), "Postfix")
            );
            UnityEngine.Debug.Log("[Antigravity] UI patches applied.");
        }

        private static void ApplyGamePatches(Harmony harmony)
        {
            // Patch SaveLoader.Load to detect when a save is loaded
            var saveLoaderLoadMethod = AccessTools.Method(typeof(SaveLoader), "Load", new System.Type[] { typeof(string) });
            if (saveLoaderLoadMethod != null)
            {
                harmony.Patch(
                    saveLoaderLoadMethod,
                    postfix: new HarmonyMethod(typeof(Game.SaveLoader_Load_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] SaveLoader.Load patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] SaveLoader.Load method not found - save sync may not work.");
            }

            // Patch Game.OnSpawn to detect when world is ready
            var gameOnSpawnMethod = AccessTools.Method(typeof(global::Game), "OnSpawn");
            if (gameOnSpawnMethod != null)
            {
                harmony.Patch(
                    gameOnSpawnMethod,
                    postfix: new HarmonyMethod(typeof(Game.Game_OnSpawn_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] Game.OnSpawn patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] Game.OnSpawn method not found.");
            }
            
            UnityEngine.Debug.Log("[Antigravity] Game patches applied.");

            // Apply sync patches
            ApplySpeedSyncPatches(harmony);
            ApplyDigSyncPatches(harmony);
            ApplyCancelSyncPatches(harmony);
            ApplyBuildSyncPatches(harmony);
            ApplyDeconstructSyncPatches(harmony);
        }

        private static void ApplySpeedSyncPatches(Harmony harmony)
        {
            // Try to find Pause method - may have different signature
            var pauseMethod = AccessTools.Method(typeof(SpeedControlScreen), "Pause", new System.Type[] { typeof(bool) });
            if (pauseMethod == null)
            {
                // Try without parameters
                pauseMethod = AccessTools.Method(typeof(SpeedControlScreen), "Pause");
                UnityEngine.Debug.Log("[Antigravity] Trying Pause() without parameters...");
            }
            
            if (pauseMethod != null)
            {
                harmony.Patch(
                    pauseMethod,
                    postfix: new HarmonyMethod(typeof(Commands.SpeedSyncPatches.SpeedControlScreen_Pause_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] SpeedControlScreen.Pause patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] SpeedControlScreen.Pause method NOT FOUND! Pause sync will not work.");
            }

            // Patch SpeedControlScreen.Unpause
            var unpauseMethod = AccessTools.Method(typeof(SpeedControlScreen), "Unpause", new System.Type[] { typeof(bool) });
            if (unpauseMethod == null)
            {
                unpauseMethod = AccessTools.Method(typeof(SpeedControlScreen), "Unpause");
            }
            
            if (unpauseMethod != null)
            {
                harmony.Patch(
                    unpauseMethod,
                    postfix: new HarmonyMethod(typeof(Commands.SpeedSyncPatches.SpeedControlScreen_Unpause_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] SpeedControlScreen.Unpause patch applied.");
            }

            // Patch SpeedControlScreen.SetSpeed
            var setSpeedMethod = AccessTools.Method(typeof(SpeedControlScreen), "SetSpeed", new System.Type[] { typeof(int) });
            if (setSpeedMethod != null)
            {
                harmony.Patch(
                    setSpeedMethod,
                    postfix: new HarmonyMethod(typeof(Commands.SpeedSyncPatches.SpeedControlScreen_SetSpeed_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] SpeedControlScreen.SetSpeed patch applied.");
            }

            UnityEngine.Debug.Log("[Antigravity] Speed sync patches applied.");
        }

        private static void ApplyDigSyncPatches(Harmony harmony)
        {
            // Patch DigTool.OnDragTool
            var onDragToolMethod = AccessTools.Method(typeof(DigTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.DigSyncPatches.DigTool_OnDragTool_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] DigTool.OnDragTool patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] DigTool.OnDragTool method NOT FOUND!");
            }

            // Patch DragTool.OnLeftClickUp to detect when drag ends
            var onLeftClickUpMethod = AccessTools.Method(typeof(DragTool), "OnLeftClickUp");
            if (onLeftClickUpMethod != null)
            {
                harmony.Patch(
                    onLeftClickUpMethod,
                    postfix: new HarmonyMethod(typeof(Commands.DigSyncPatches.DragTool_OnLeftClickUp_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] DragTool.OnLeftClickUp patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] DragTool.OnLeftClickUp method NOT FOUND!");
            }

            UnityEngine.Debug.Log("[Antigravity] Dig sync patches applied.");
        }

        private static void ApplyCancelSyncPatches(Harmony harmony)
        {
            // Patch CancelTool.OnDragTool  
            var onDragToolMethod = AccessTools.Method(typeof(CancelTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.CancelSyncPatches.CancelTool_OnDragTool_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] CancelTool.OnDragTool patch applied.");
            }

            // Note: OnLeftClickUp is patched via DragTool already for Dig
            // The CancelTool postfix handler checks if instance is CancelTool
            UnityEngine.Debug.Log("[Antigravity] Cancel sync patches applied.");
        }

        private static void ApplyBuildSyncPatches(Harmony harmony)
        {
            // Patch BuildTool.OnDragTool
            var onDragToolMethod = AccessTools.Method(typeof(BuildTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.BuildSyncPatches.BuildTool_OnDragTool_Patch), "Postfix")
                );
            }

            // Patch DragTool.OnLeftClickUp for build (already patched, but will call BuildTool handler)
            UnityEngine.Debug.Log("[Antigravity] Build sync patches applied.");
        }

        private static void ApplyDeconstructSyncPatches(Harmony harmony)
        {
            // Patch DeconstructTool.OnDragTool
            var onDragToolMethod = AccessTools.Method(typeof(DeconstructTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.DeconstructSyncPatches.DeconstructTool_OnDragTool_Patch), "Postfix")
                );
            }

            UnityEngine.Debug.Log("[Antigravity] Deconstruct sync patches applied.");
        }

        private static void ApplySimulationPatches(Harmony harmony)
        {
            // Simulation patches will be applied here
            // harmony.PatchAll(typeof(SimTickPatch));
        }

        /// <summary>
        /// Remove all patches.
        /// </summary>
        /// <param name="harmony">Harmony instance to use.</param>
        public static void RemovePatches(Harmony harmony)
        {
            harmony.UnpatchAll(harmony.Id);
            PatchesApplied = false;
            UnityEngine.Debug.Log("[Antigravity] All patches removed.");
        }
    }
}
