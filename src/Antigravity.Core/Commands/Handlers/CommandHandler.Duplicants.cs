using UnityEngine;
using Antigravity.Core.Sync;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for Duplicant sync commands
    /// </summary>
    public static partial class CommandManager
    {
        private static void ExecuteChoreStartCommand(ChoreStartCommand cmd)
        {
            if (cmd == null) return;
            Debug.Log($"[Antigravity] EXECUTE: ChoreStart Dupe={cmd.DuplicantName} Chore={cmd.ChoreTypeId} at {cmd.TargetCell}");
            DuplicantSyncManager.Instance.HandleChoreStart(cmd);
        }

        private static void ExecuteChoreEndCommand(ChoreEndCommand cmd)
        {
            if (cmd == null) return;
            Debug.Log($"[Antigravity] EXECUTE: ChoreEnd Dupe={cmd.DuplicantName} Chore={cmd.ChoreTypeId}");
            DuplicantSyncManager.Instance.HandleChoreEnd(cmd);
        }

        private static void ExecuteNavigateToCommand(NavigateToCommand cmd)
        {
            if (cmd == null) return;
            Debug.Log($"[Antigravity] EXECUTE: NavigateTo Dupe={cmd.DuplicantName} to {cmd.TargetCell}");
            DuplicantSyncManager.Instance.HandleNavigateTo(cmd);
        }

        private static void ExecuteDuplicantChecksumCommand(DuplicantChecksumCommand cmd)
        {
            if (cmd == null) return;
            DuplicantSyncManager.Instance.VerifyChecksum(cmd);
        }

        private static void ExecuteDuplicantFullStateCommand(DuplicantFullStateCommand cmd)
        {
            if (cmd == null) return;
            Debug.Log($"[Antigravity] EXECUTE: FullStateSync Dupe={cmd.DuplicantName} at {cmd.CurrentCell}");
            DuplicantSyncManager.Instance.ApplyFullState(cmd);
        }

        private static void ExecutePositionSyncCommand(PositionSyncCommand cmd)
        {
            if (cmd == null) return;
            DuplicantSyncManager.Instance.ApplyPositionSync(cmd);
        }

        private static void ExecuteRandomSeedSyncCommand(RandomSeedSyncCommand cmd)
        {
            if (cmd == null) return;
            DuplicantSyncManager.Instance.ApplyRandomSeed(cmd);
        }

        /// <summary>
        /// Execute a DuplicantCommandRequest - only Host processes these.
        /// </summary>
        private static void ExecuteDuplicantCommandRequest(DuplicantCommandRequestCommand cmd)
        {
            if (cmd == null) return;
            
            // Only Host processes these requests
            if (!Network.MultiplayerState.IsHost)
            {
                Debug.LogWarning($"[Antigravity] Client received DuplicantCommandRequest - ignoring (should only go to Host)");
                return;
            }
            
            Debug.Log($"[Antigravity] Host processing DuplicantCommandRequest: {cmd.RequestType} for {cmd.DuplicantName}");
            DuplicantSyncManager.Instance.ProcessClientRequest(cmd);
        }
    }
}
