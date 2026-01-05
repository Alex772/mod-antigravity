using HarmonyLib;
using UnityEngine;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches to prevent common crashes during multiplayer sync.
    /// </summary>
    public static class CrashFixPatches
    {
        /// <summary>
        /// Prevents crash/error when adding a null anim override.
        /// Trace: KAnimControllerBase.AddAnimOverrides (KAnimFile kanim_file, System.Single priority)
        /// </summary>
        [HarmonyPatch(typeof(KAnimControllerBase), "AddAnimOverrides")]
        public static class KAnimControllerBase_AddAnimOverrides_Patch
        {
            public static bool Prefix(KAnimControllerBase __instance, KAnimFile kanim_file, float priority)
            {
                if (kanim_file == null)
                {
                    // Log a warning but PREVENT the original method from running (which would log Error/Crash)
                    // We check if it's a Minion to be more specific, but this protection is good generally
                    if (__instance.GetComponent<MinionIdentity>() != null)
                    {
                        Debug.LogWarning($"[Antigravity] Crash averted: AddAnimOverrides ignored null file for {__instance.name}");
                    }
                    return false; // Skip original
                }
                return true; // Run original
            }
        }
    }
}
