using HarmonyLib;
using Antigravity.Core.Network;
using Antigravity.Core.Commands;
using UnityEngine;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Suspends MinionBrain on client to prevent autonomous chore searching.
    /// 
    /// WHY: In Phase 1 (host-authoritative), the host makes all chore decisions
    /// and broadcasts them to clients via ChoreStartCommand. If the client's Brain
    /// is also running, it calls ChoreConsumer.FindNextChore() which can crash with
    /// NullReferenceException when chores/targets are invalidated by sync.
    /// 
    /// HOW: Uses Brain.Suspend() which sets an internal boolean flag checked by
    /// IsRunning(). The BrainScheduler skips suspended brains without removing them
    /// from its list. This is safer than disabling the component (which would call
    /// OnCmpDisable → Stop → StopChore, killing any active chore).
    /// 
    /// SAFETY: Brain.Suspend() is a native ONI API used by the game itself.
    /// It can be reversed with Brain.Resume() if needed in Phase 2.
    /// 
    /// Addresses: Feedbacks 1, 5, 6 (NullRef crash, idle duplicants, StateMachine conflicts)
    /// </summary>
    public static class ClientBrainSuspendPatch
    {
        /// <summary>
        /// After Brain.OnSpawn(), suspend it on client.
        /// 
        /// Brain.OnSpawn() sets running=true and registers with Components.Brains.
        /// We suspend AFTER this to ensure the brain is properly initialized but
        /// won't execute UpdateBrain() / FindNextChore().
        /// </summary>
        [HarmonyPatch(typeof(Brain), "OnSpawn")]
        public static class Brain_OnSpawn_Patch
        {
            public static void Postfix(Brain __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (MultiplayerState.IsHost) return;

                __instance.Suspend("AntigravityClientMode");
                Debug.Log($"[Antigravity] Brain suspended for client mode: {__instance.name}");
            }
        }

        /// <summary>
        /// Safety guard: prevent ChoreConsumer.FindNextChore from running on client.
        /// 
        /// Even with Brain suspended, FindNextChore could be called from other code paths
        /// (e.g. ForceChoreOnClient temporarily enables it). This guard prevents the
        /// NullReferenceException crash by returning false (no chore found) instead of
        /// letting it search through potentially invalid chore lists.
        /// 
        /// Exception: if IsExecutingRemoteCommand is true, we allow the call because
        /// ForceChoreOnClient needs it for matching host chores.
        /// </summary>
        [HarmonyPatch(typeof(ChoreConsumer), "FindNextChore")]
        public static class ChoreConsumer_FindNextChore_Patch
        {
            public static bool Prefix(ChoreConsumer __instance, ref bool __result)
            {
                if (!MultiplayerState.IsMultiplayerSession) return true;
                if (MultiplayerState.IsHost) return true;

                // Allow FindNextChore when executing remote commands
                // (ForceChoreOnClient uses it to match host chores)
                if (CommandManager.IsExecutingRemoteCommand) return true;

                // Client: block autonomous chore searching
                __result = false;
                return false; // Skip original method
            }
        }
    }
}
