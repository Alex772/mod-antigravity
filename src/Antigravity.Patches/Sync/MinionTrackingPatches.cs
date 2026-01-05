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
        /// Disables AI decision-making components on client Duplicants.
        /// The Duplicant will be controlled remotely by the Host.
        /// </summary>
        private static void DisableClientAI(GameObject go)
        {
            // Disable chore decision components
            if (go.TryGetComponent<ChoreDriver>(out var driver))
            {
                driver.enabled = false;
            }
            
            if (go.TryGetComponent<ChoreConsumer>(out var consumer))
            {
                consumer.enabled = false;
            }
            
            // Disable AI brain
            if (go.TryGetComponent<MinionBrain>(out var brain))
            {
                brain.enabled = false;
            }
            
            // Disable sensors that trigger behaviors
            if (go.TryGetComponent<Sensors>(out var sensors))
            {
                sensors.enabled = false;
            }
            
            // Note: We do NOT disable Navigator - it's needed for movement animations
            // The Navigator will receive commands from the Host via sync
            
            // Note: We do NOT disable StateMachineController - it's needed for animations
            // But we do prevent it from making autonomous decisions by disabling MinionBrain
            
            Debug.Log($"[Antigravity] Client AI disabled for Duplicant: {go.name}");
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
    }
}
