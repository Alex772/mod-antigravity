using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Speed/Pause synchronization.
    /// 
    /// Uses STATE-BASED sync: sends IsPaused + Speed together on any change.
    /// This avoids pauseCount stack desync issues.
    /// 
    /// ONI Speed Values:
    ///   0 = slow (1x) - normalSpeed
    ///   1 = medium (2x) - fastSpeed  
    ///   2 = fast (3x) - ultraSpeed
    /// </summary>
    public static class SpeedSyncPatches
    {
        // Flag to prevent recursive command sending
        private static bool _isSendingCommand = false;
        
        /// <summary>
        /// Send the current speed/pause state to all players.
        /// Called after any speed or pause change.
        /// </summary>
        private static void SendSpeedState()
        {
            if (!MultiplayerState.IsMultiplayerSession) return;
            if (!MultiplayerState.IsGameLoaded) return;
            if (CommandManager.IsExecutingRemoteCommand) return;
            if (_isSendingCommand) return;
            if (SpeedControlScreen.Instance == null) return;
            
            // Block unpause if host is waiting for players (ready check)
            if (MultiplayerState.IsHost && GameSession.IsWaitingForPlayers && 
                !SpeedControlScreen.Instance.IsPaused)
            {
                Debug.LogWarning("[Antigravity] BLOCKED: Cannot unpause while waiting for players!");
                // Force back to paused state
                SpeedControlScreen.Instance.Pause(playSound: false);
                return;
            }
            
            try
            {
                _isSendingCommand = true;
                
                var cmd = new SpeedCommand
                {
                    Speed = SpeedControlScreen.Instance.GetSpeed(),
                    IsPaused = SpeedControlScreen.Instance.IsPaused
                };
                
                Debug.Log($"[Antigravity] SEND: SpeedState Speed={cmd.Speed} IsPaused={cmd.IsPaused}");
                CommandManager.SendCommand(cmd);
            }
            finally
            {
                _isSendingCommand = false;
            }
        }
        
        /// <summary>
        /// Patch for SpeedControlScreen.Pause
        /// Sends speed state after pause.
        /// </summary>
        [HarmonyPatch(typeof(SpeedControlScreen), nameof(SpeedControlScreen.Pause))]
        public static class SpeedControlScreen_Pause_Patch
        {
            public static void Postfix()
            {
                SendSpeedState();
            }
        }

        /// <summary>
        /// Patch for SpeedControlScreen.Unpause
        /// Sends speed state after unpause.
        /// </summary>
        [HarmonyPatch(typeof(SpeedControlScreen), nameof(SpeedControlScreen.Unpause))]
        public static class SpeedControlScreen_Unpause_Patch
        {
            public static void Postfix()
            {
                SendSpeedState();
            }
        }

        /// <summary>
        /// Patch for SpeedControlScreen.SetSpeed
        /// Sends speed state after speed change.
        /// </summary>
        [HarmonyPatch(typeof(SpeedControlScreen), "SetSpeed")]
        public static class SpeedControlScreen_SetSpeed_Patch
        {
            public static void Postfix()
            {
                SendSpeedState();
            }
        }
    }
}
