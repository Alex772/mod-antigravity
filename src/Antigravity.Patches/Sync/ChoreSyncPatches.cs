using HarmonyLib;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using UnityEngine;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches for synchronizing Duplicant chores.
    /// Intercepts ChoreDriver.SetChore to broadcast intended actions.
    /// </summary>
    public static class ChoreSyncPatches
    {
        [HarmonyPatch(typeof(ChoreDriver), "SetChore")]
        public static class ChoreDriver_SetChore_Patch
        {
            public static void Postfix(ChoreDriver __instance, Chore.Precondition.Context context)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                
                // For Phase 1, only Host broadcasts chore decisions to avoid conflicts.
                // Clients will eventually be forced to follow via HandleChoreStart (in SyncManager).
                if (!MultiplayerState.IsHost) return;

                // Check if we are executing a remote command to avoid infinite loops
                if (CommandManager.IsExecutingRemoteCommand) return;

                Chore chore = context.chore;
                if (chore == null) return;

                MinionIdentity minion = __instance.GetComponent<MinionIdentity>();
                if (minion == null) return;

                // Create command
                var cmd = new ChoreStartCommand
                {
                    DuplicantId = minion.GetInstanceID(),
                    ChoreTypeId = chore.choreType.Id,
                    TargetCell = Grid.PosToCell(chore.target.gameObject),
                    TargetId = chore.target.gameObject.GetInstanceID()
                };

                // Send
                CommandManager.SendCommand(cmd);
            }
        }
    }
}
