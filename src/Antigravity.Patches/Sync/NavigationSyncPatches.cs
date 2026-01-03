using HarmonyLib;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using UnityEngine;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches for synchronizing Duplicant navigation.
    /// Intercepts Navigator.GoTo to broadcast movement targets.
    /// </summary>
    public static class NavigationSyncPatches
    {
        [HarmonyPatch(typeof(Navigator), "GoTo", typeof(KMonoBehaviour), typeof(CellOffset[]), typeof(NavTactic))]
        public static class Navigator_GoTo_Patch
        {
            public static void Postfix(Navigator __instance, KMonoBehaviour target, CellOffset[] offsets, NavTactic tactic)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                
                // Host authority for now
                if (!MultiplayerState.IsHost) return;

                if (CommandManager.IsExecutingRemoteCommand) return;
                
                if (target == null) return;

                MinionIdentity minion = __instance.GetComponent<MinionIdentity>();
                if (minion == null) return;

                var cmd = new NavigateToCommand
                {
                    DuplicantId = minion.GetInstanceID(),
                    TargetCell = Grid.PosToCell(target),
                    TargetId = target.gameObject.GetInstanceID()
                };

                CommandManager.SendCommand(cmd);
            }
        }
    }
}
