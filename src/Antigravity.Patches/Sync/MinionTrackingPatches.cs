using HarmonyLib;
using Antigravity.Core.Sync;
using UnityEngine;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches to track MinionIdentities for synchronization.
    /// </summary>
    public static class MinionTrackingPatches
    {
        [HarmonyPatch(typeof(MinionIdentity), "OnSpawn")]
        public static class MinionIdentity_OnSpawn_Patch
        {
            public static void Postfix(MinionIdentity __instance)
            {
                if (__instance == null) return;
                
                // Register with sync manager
                DuplicantSyncManager.Instance.RegisterMinion(__instance);
            }
        }

        [HarmonyPatch(typeof(MinionIdentity), "OnCleanUp")]
        public static class MinionIdentity_OnCleanUp_Patch
        {
            public static void Prefix(MinionIdentity __instance)
            {
                if (__instance == null) return;

                // Unregister
                DuplicantSyncManager.Instance.UnregisterMinion(__instance);
            }
        }
    }
}
