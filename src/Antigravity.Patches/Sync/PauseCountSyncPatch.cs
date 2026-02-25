using HarmonyLib;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using UnityEngine;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches for SpeedControlScreen to prevent local pauseCount divergence on client.
    /// 
    /// PROBLEM: In ONI, many UI screens call Pause() when they open and Unpause() 
    /// when they close (building details, research screen, skills screen, etc).
    /// In multiplayer, these local pauses on the client cause pauseCount to diverge
    /// from the host. Since Time.timeScale is driven by pauseCount, the client's
    /// game clock stops ticking while the host continues, creating progressive
    /// time drift.
    /// 
    /// SOLUTION: Block local Pause/Unpause calls on the client unless they come
    /// from a remote command (i.e., the host sent PauseGame/UnpauseGame).
    /// The host's pauseCount is periodically synced via TimeSyncCommand.
    /// 
    /// NOTE: This means the client player won't see the pause overlay when opening
    /// menus. The game continues running in the background. This is intentional —
    /// only the host controls pause state.
    /// 
    /// Addresses: Feedbacks 2, 3 (time divergence, ghost blocks from drift)
    /// </summary>
    public static class PauseCountSyncPatch
    {
        /// <summary>
        /// Block local Pause() on client.
        /// 
        /// We allow Pause() to run when:
        /// - Not in multiplayer (singleplayer is unchanged)
        /// - On the host (host controls pause)
        /// - When executing a remote command (host sent PauseGame)
        /// - When the game crashed (isCrashed parameter)
        /// </summary>
        [HarmonyPatch(typeof(SpeedControlScreen), "Pause")]
        public static class SpeedControlScreen_Pause_ClientBlock
        {
            public static bool Prefix(bool isCrashed)
            {
                if (!MultiplayerState.IsMultiplayerSession) return true;
                if (MultiplayerState.IsHost) return true;
                if (CommandManager.IsExecutingRemoteCommand) return true;

                // Always allow crash pauses (safety)
                if (isCrashed) return true;

                // Client: block local pause to prevent pauseCount divergence
                // The host will send pause state via TimeSyncCommand
                return false;
            }
        }

        /// <summary>
        /// Block local Unpause() on client.
        /// 
        /// Same logic as Pause: only allow unpauses from remote commands.
        /// </summary>
        [HarmonyPatch(typeof(SpeedControlScreen), "Unpause")]
        public static class SpeedControlScreen_Unpause_ClientBlock
        {
            public static bool Prefix()
            {
                if (!MultiplayerState.IsMultiplayerSession) return true;
                if (MultiplayerState.IsHost) return true;
                if (CommandManager.IsExecutingRemoteCommand) return true;

                // Client: block local unpause to prevent pauseCount divergence
                return false;
            }
        }
    }
}
