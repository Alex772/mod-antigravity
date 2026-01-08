using HarmonyLib;
using Antigravity.Core.Sync;
using Antigravity.Core.Network;
using UnityEngine;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches to track MinionIdentities for synchronization.
    /// On clients, also disables AI components to make Duplicants "puppets".
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
                
                // If we are a client in a multiplayer session, disable AI components
                // This makes the Duplicant a "puppet" controlled by the Host
                if (MultiplayerState.IsMultiplayerSession && !MultiplayerState.IsHost)
                {
                    DisableClientAI(__instance.gameObject);
                }
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

        /// <summary>
        /// Previously disabled AI components on client Duplicants.
        /// NOW: We let client AI run normally, relying on position sync for consistency.
        /// This approach is more stable since forcing chores proved unreliable.
        /// </summary>
        private static void DisableClientAI(GameObject go)
        {
            // Phase 1 Strategy: Let client AI run normally
            // Position sync every 2 seconds keeps duplicants in the same location
            // Client may do slightly different animations but will be in the same place
            
            // For future improvement: sync the actual ChoreDriver.currentChore state
            // but for now, this is more stable than trying to force chores
            
            Debug.Log($"[Antigravity] Client AI active for Duplicant: {go.name} (Phase 1 - position sync only)");
        }
        
        /// <summary>
        /// Re-enables AI components. Called if switching from client to singleplayer
        /// or if the Host disconnects.
        /// </summary>
        public static void ReenableAI(GameObject go)
        {
            if (go.TryGetComponent<ChoreDriver>(out var driver))
                driver.enabled = true;
            if (go.TryGetComponent<ChoreConsumer>(out var consumer))
                consumer.enabled = true;
            if (go.TryGetComponent<MinionBrain>(out var brain))
                brain.enabled = true;
            if (go.TryGetComponent<Sensors>(out var sensors))
                sensors.enabled = true;
                
            Debug.Log($"[Antigravity] AI re-enabled for Duplicant: {go.name}");
        }
        
        /// <summary>
        /// Prevents Brain.UpdateChores from running on clients where AI is disabled.
        /// This prevents NullReferenceException when ChoreConsumer is disabled.
        /// </summary>
        [HarmonyPatch(typeof(Brain), "UpdateChores")]
        public static class Brain_UpdateChores_Patch
        {
            public static bool Prefix(Brain __instance)
            {
                // If not in multiplayer or we're host, allow normal behavior
                if (!MultiplayerState.IsMultiplayerSession || MultiplayerState.IsHost)
                    return true;
                
                // On client, only allow if this brain's ChoreConsumer is enabled
                if (__instance.TryGetComponent<ChoreConsumer>(out var consumer))
                {
                    if (!consumer.enabled)
                    {
                        // Skip UpdateChores - puppet doesn't choose chores
                        return false;
                    }
                }
                
                return true;
            }
        }
    }
}
