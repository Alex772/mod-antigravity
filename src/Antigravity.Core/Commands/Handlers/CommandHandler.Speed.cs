using UnityEngine;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for Speed commands: SetGameSpeed, PauseGame, UnpauseGame.
    ///
    /// ONI Speed Values (used directly without conversion):
    ///   0 = slow (1x) - SpeedControlScreen.normalSpeed
    ///   1 = medium (2x) - SpeedControlScreen.fastSpeed
    ///   2 = fast (3x) - SpeedControlScreen.ultraSpeed
    /// </summary>
    public static partial class CommandManager
    {
        /// <summary>
        /// Execute a speed change command.
        /// Receives ONI's native speed values (0, 1, 2) directly.
        /// </summary>
        private static void ExecuteSetGameSpeedInline(SpeedCommand speedCmd)
        {
            if (speedCmd == null) return;
            if (SpeedControlScreen.Instance == null)
            {
                Debug.LogWarning("[Antigravity] SpeedControlScreen.Instance is null, cannot set speed");
                return;
            }

            int speed = speedCmd.Speed;

            // Validate speed is in valid range (0, 1, 2)
            if (speed < 0 || speed > 2)
            {
                Debug.LogWarning($"[Antigravity] Invalid speed value: {speed}, clamping to valid range");
                speed = Mathf.Clamp(speed, 0, 2);
            }

            Debug.Log($"[Antigravity] EXECUTE: SetSpeed {speed}");
            SpeedControlScreen.Instance.SetSpeed(speed);
        }

        /// <summary>
        /// Execute pause command.
        /// Uses SpeedControlScreen.Pause with sound disabled.
        /// Only pauses if not already paused to avoid pauseCount stack issues.
        /// </summary>
        private static void ExecutePauseInline()
        {
            if (SpeedControlScreen.Instance == null)
            {
                Debug.LogWarning("[Antigravity] SpeedControlScreen.Instance is null, cannot pause");
                return;
            }

            // Check if already paused to avoid stacking pauseCount
            if (SpeedControlScreen.Instance.IsPaused)
            {
                Debug.Log("[Antigravity] EXECUTE: Pause (already paused, skipping)");
                return;
            }

            Debug.Log("[Antigravity] EXECUTE: Pause");
            SpeedControlScreen.Instance.Pause(playSound: false);
        }

        /// <summary>
        /// Execute unpause command.
        /// Uses SpeedControlScreen.Unpause with sound disabled.
        /// Only unpauses if currently paused.
        /// </summary>
        private static void ExecuteUnpauseInline()
        {
            if (SpeedControlScreen.Instance == null)
            {
                Debug.LogWarning("[Antigravity] SpeedControlScreen.Instance is null, cannot unpause");
                return;
            }

            // Check if actually paused
            if (!SpeedControlScreen.Instance.IsPaused)
            {
                Debug.Log("[Antigravity] EXECUTE: Unpause (not paused, skipping)");
                return;
            }

            Debug.Log("[Antigravity] EXECUTE: Unpause");
            SpeedControlScreen.Instance.Unpause(playSound: false);
        }
    }
}
