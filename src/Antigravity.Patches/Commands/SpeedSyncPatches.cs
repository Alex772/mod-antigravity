using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Speed/Pause synchronization.
    ///
    /// ONI Speed Values:
    ///   0 = slow (1x) - normalSpeed
    ///   1 = medium (2x) - fastSpeed
    ///   2 = fast (3x) - ultraSpeed
    ///
    /// We send these values directly without conversion.
    /// Pause/Unpause are handled as separate commands.
    /// 
    /// Edge Case Handling:
    /// - Duplicate pause/unpause commands are safely ignored (checked in handler)
    /// - Simultaneous actions from multiple clients are handled correctly
    /// - Ready Check blocking uses _prefixBlocked flag to prevent Postfix from sending
    /// </summary>
    public static class SpeedSyncPatches
    {
        /// <summary>
        /// Patch for SpeedControlScreen.Pause
        /// Sends PauseGame command to all players.
        /// </summary>
        public static class SpeedControlScreen_Pause_Patch
        {
            public static void Postfix()
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                Debug.Log("[Antigravity] SEND: PauseGame");
                CommandManager.SendCommand(new GameCommand(GameCommandType.PauseGame));
            }
        }

        /// <summary>
        /// Patch for SpeedControlScreen.Unpause
        /// Sends UnpauseGame command to all players.
        /// BLOCKS unpause if host is waiting for players to load.
        /// Uses _prefixBlocked flag to prevent Postfix sending when Prefix blocks.
        /// </summary>
        public static class SpeedControlScreen_Unpause_Patch
        {
            /// <summary>
            /// Flag to track if Prefix blocked the method.
            /// ThreadStatic ensures thread safety.
            /// </summary>
            [System.ThreadStatic]
            private static bool _prefixBlocked;
            
            /// <summary>
            /// Prefix: Block unpause if host is waiting for players to load.
            /// Sets _prefixBlocked flag when blocking.
            /// </summary>
            public static bool Prefix()
            {
                _prefixBlocked = false;
                
                if (!MultiplayerState.IsMultiplayerSession) return true;
                if (!MultiplayerState.IsHost) return true;

                // Block unpause if waiting for players
                if (GameSession.IsWaitingForPlayers)
                {
                    Debug.LogWarning("[Antigravity] BLOCKED: Cannot unpause while waiting for players to load!");
                    _prefixBlocked = true;
                    return false; // Block the original method
                }

                return true; // Allow original method to run
            }

            public static void Postfix()
            {
                // If Prefix blocked the method, don't send command
                if (_prefixBlocked)
                {
                    _prefixBlocked = false; // Reset flag
                    return;
                }
                
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                Debug.Log("[Antigravity] SEND: UnpauseGame");
                CommandManager.SendCommand(new GameCommand(GameCommandType.UnpauseGame));
            }
        }

        /// <summary>
        /// Patch for SpeedControlScreen.SetSpeed
        /// Sends speed command using ONI's native values (0, 1, 2).
        /// </summary>
        public static class SpeedControlScreen_SetSpeed_Patch
        {
            public static void Postfix(int Speed)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                // Send ONI's native speed value directly (0, 1, 2)
                Debug.Log($"[Antigravity] SEND: SetGameSpeed {Speed}");
                CommandManager.SendCommand(new SpeedCommand { Speed = Speed });
            }
        }
    }
}
