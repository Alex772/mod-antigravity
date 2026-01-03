using HarmonyLib;
using Antigravity.Core.Network;
using Antigravity.Core.Sync;
using Antigravity.Core.Commands;
using UnityEngine;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches to control Brain behavior in multiplayer.
    /// 
    /// CURRENT STATUS: DISABLED
    /// The blocking approach caused Duplicants to freeze on clients.
    /// For now, we rely on position sync to correct any drift.
    /// A proper implementation would need a flag in DuplicantSyncManager
    /// to track when we're forcing a chore from a remote command.
    /// </summary>
    public static class BrainSyncPatches
    {
        // Flag to allow chore assignment during forced sync
        public static bool IsApplyingRemoteChore = false;

        /// <summary>
        /// Patch ChoreConsumer.FindNextChore - CURRENTLY ALLOWS ALL
        /// </summary>
        [HarmonyPatch(typeof(ChoreConsumer), "FindNextChore")]
        public static class ChoreConsumer_FindNextChore_Patch
        {
            public static bool Prefix(ChoreConsumer __instance, ref bool __result)
            {
                // Allow normal behavior for now
                // Position sync will correct major drifts
                return true;
            }
        }

        /// <summary>
        /// Patch ChoreDriver.SetChore - CURRENTLY ALLOWS ALL
        /// We'll rely on position sync instead of blocking.
        /// </summary>
        [HarmonyPatch(typeof(ChoreDriver), "SetChore")]
        public static class ChoreDriver_SetChore_Client_Patch
        {
            public static bool Prefix(ChoreDriver __instance, Chore.Precondition.Context context)
            {
                // Allow all chore assignments for now
                // The position sync will keep things in line
                
                // Log when client makes independent decisions (for debugging)
                if (MultiplayerState.IsMultiplayerSession && 
                    MultiplayerState.IsGameLoaded && 
                    !MultiplayerState.IsHost &&
                    !CommandManager.IsExecutingRemoteCommand &&
                    !IsApplyingRemoteChore)
                {
                    // This is an independent client decision - log for debugging
                    // But allow it to prevent freezing
                }

                return true; // Allow all for now
            }
        }
    }
}
