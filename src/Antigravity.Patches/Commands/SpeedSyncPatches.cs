using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Speed/Pause sync.
    /// ONI uses: speed 0=slow(1x), 1=medium(2x), 2=fast(3x)
    /// We send: speed 1=slow, 2=medium, 3=fast (adding 1 so 0 is never sent)
    /// </summary>
    public static class SpeedSyncPatches
    {
        /// <summary>
        /// Pause -> Send Pause command
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
        /// Unpause -> Send Unpause command
        /// </summary>
        public static class SpeedControlScreen_Unpause_Patch
        {
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
        /// SetSpeed -> Send Speed command
        /// ONI speed 0,1,2 -> We send 1,2,3
        /// </summary>
        public static class SpeedControlScreen_SetSpeed_Patch
        {
            public static void Postfix(int Speed)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;
                
                // Speed is ONI's value (0, 1, 2)
                // We add 1 to send (1, 2, 3) so 0 is never confused with pause
                int speedToSend = Speed + 1;

                Debug.Log($"[Antigravity] SEND: SetGameSpeed {speedToSend} (ONI speed index: {Speed})");
                CommandManager.SendCommand(new SpeedCommand { Speed = speedToSend });
            }
        }
    }
}
