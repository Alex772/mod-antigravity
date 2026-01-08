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

                if (chore.choreType == null) return;
                
                // Some chores might not have a target (e.g. Idle)
                int targetCell = -1;
                int targetId = -1;
                string targetPrefabId = null;

                if (chore.target != null && chore.target.gameObject != null)
                {
                    targetCell = Grid.PosToCell(chore.target.gameObject);
                    targetId = chore.target.gameObject.GetInstanceID();
                    
                    // Get prefab ID for matching on client
                    var kpid = chore.target.gameObject.GetComponent<KPrefabID>();
                    if (kpid != null)
                    {
                        targetPrefabId = kpid.PrefabTag.ToString();
                    }
                }

                // Get target position
                Vector3 targetPos = chore.target?.gameObject?.transform.position ?? Vector3.zero;
                
                // Get chore group
                string choreGroupId = chore.choreType?.groups != null && chore.choreType.groups.Length > 0
                    ? chore.choreType.groups[0]?.Id
                    : null;

                // Create command with all details
                var cmd = new ChoreStartCommand
                {
                    DuplicantName = minion.name,
                    ChoreTypeId = chore.choreType.Id,
                    TargetCell = targetCell,
                    TargetId = targetId,
                    TargetPrefabId = targetPrefabId,
                    TargetPositionX = targetPos.x,
                    TargetPositionY = targetPos.y,
                    ChoreGroupId = choreGroupId
                };

                // Detailed logging of what we're sending
                Debug.Log($"[Antigravity] HOST sending ChoreStart:" +
                    $"\n  Duplicant: {cmd.DuplicantName}" +
                    $"\n  ChoreType: {cmd.ChoreTypeId}" +
                    $"\n  ChoreGroup: {cmd.ChoreGroupId ?? "null"}" +
                    $"\n  TargetCell: {cmd.TargetCell}" +
                    $"\n  TargetPrefabId: {cmd.TargetPrefabId ?? "null"}" +
                    $"\n  TargetPos: ({cmd.TargetPositionX:F1}, {cmd.TargetPositionY:F1})");

                // Send
                CommandManager.SendCommand(cmd);
            }
        }
    }
}
