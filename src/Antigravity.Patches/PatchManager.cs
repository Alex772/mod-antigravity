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
            // Game patches will be applied here
            // harmony.PatchAll(typeof(BuildToolPatch));
            // harmony.PatchAll(typeof(DigToolPatch));
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
