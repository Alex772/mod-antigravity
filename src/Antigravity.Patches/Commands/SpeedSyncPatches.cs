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
        /// </summary>
        public static class SpeedControlScreen_Unpause_Patch
        {
            /// <summary>
            /// Prefix: Block unpause if host is waiting for players to load.
            /// </summary>
            public static bool Prefix()
            {
                if (!MultiplayerState.IsMultiplayerSession) return true;
                if (!MultiplayerState.IsHost) return true;
                
                // Block unpause if waiting for players
                if (GameSession.IsWaitingForPlayers)
                {
                    Debug.LogWarning("[Antigravity] BLOCKED: Cannot unpause while waiting for players to load!");
                    return false; // Block the original method
                }
                
                return true; // Allow original method to run
            }
            
            public static void Postfix()
            {
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

