using UnityEngine;
using System.Reflection;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for Speed commands: SetGameSpeed with state-based sync.
    /// 
    /// ONI Speed Values (used directly without conversion):
    ///   0 = slow (1x) - SpeedControlScreen.normalSpeed
    ///   1 = medium (2x) - SpeedControlScreen.fastSpeed
    ///   2 = fast (3x) - SpeedControlScreen.ultraSpeed
    ///   
    /// This handler uses STATE-BASED sync (syncs IsPaused + Speed together)
    /// instead of action-based sync (separate Pause/Unpause commands).
    /// This avoids pauseCount stack desync issues.
    /// </summary>
    public static partial class CommandManager
    {
        // Cached reflection fields for performance
        private static FieldInfo _pauseCountField;
        private static FieldInfo _speedField;
        
        /// <summary>
        /// Execute a speed/pause state sync command.
        /// Uses reflection to directly set pauseCount and speed, avoiding stack issues.
        /// </summary>
        private static void ExecuteSetGameSpeedInline(SpeedCommand speedCmd)
        {
            if (speedCmd == null) return;
            if (SpeedControlScreen.Instance == null)
            {
                Debug.LogWarning("[Antigravity] SpeedControlScreen.Instance is null, cannot set speed");
                return;
            }
            
            int targetSpeed = speedCmd.Speed;
            bool targetPaused = speedCmd.IsPaused;
            
            // Validate speed is in valid range (0, 1, 2)
            if (targetSpeed < 0 || targetSpeed > 2)
            {
                Debug.LogWarning($"[Antigravity] Invalid speed value: {targetSpeed}, clamping to valid range");
                targetSpeed = Mathf.Clamp(targetSpeed, 0, 2);
            }
            
            Debug.Log($"[Antigravity] EXECUTE: SpeedState Speed={targetSpeed} IsPaused={targetPaused}");
            
            // Cache reflection fields
            if (_pauseCountField == null)
            {
                _pauseCountField = typeof(SpeedControlScreen).GetField("pauseCount", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (_speedField == null)
            {
                _speedField = typeof(SpeedControlScreen).GetField("speed", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }
            
            var screen = SpeedControlScreen.Instance;
            
            // Get current state
            int currentPauseCount = (int)_pauseCountField.GetValue(screen);
            bool currentlyPaused = currentPauseCount > 0;
            
            // Set speed field directly
            _speedField.SetValue(screen, targetSpeed);
            
            // Handle pause state
            if (targetPaused && !currentlyPaused)
            {
                // Need to pause - set pauseCount to 1
                _pauseCountField.SetValue(screen, 1);
                Time.timeScale = 0f;
                Debug.Log("[Antigravity] State sync: Paused");
            }
            else if (!targetPaused && currentlyPaused)
            {
                // Need to unpause - set pauseCount to 0
                _pauseCountField.SetValue(screen, 0);
                Time.timeScale = GetTimeScaleForSpeed(targetSpeed);
                Debug.Log($"[Antigravity] State sync: Unpaused, timeScale={Time.timeScale}");
            }
            else if (!targetPaused)
            {
                // Not paused, just update speed
                Time.timeScale = GetTimeScaleForSpeed(targetSpeed);
            }
            
            // Update UI buttons
            screen.SetSpeed(targetSpeed);
        }
        
        /// <summary>
        /// Get the correct Time.timeScale value for a speed index.
        /// </summary>
        private static float GetTimeScaleForSpeed(int speed)
        {
            if (SpeedControlScreen.Instance == null) return 1f;
            
            switch (speed)
            {
                case 0: return SpeedControlScreen.Instance.normalSpeed;
                case 1: return SpeedControlScreen.Instance.fastSpeed;
                case 2: return SpeedControlScreen.Instance.ultraSpeed;
                default: return 1f;
            }
        }
        
        /// <summary>
        /// Execute pause command (legacy - redirects to state sync).
        /// </summary>
        private static void ExecutePauseInline()
        {
            if (SpeedControlScreen.Instance == null) return;
            
            int currentSpeed = SpeedControlScreen.Instance.GetSpeed();
            ExecuteSetGameSpeedInline(new SpeedCommand { Speed = currentSpeed, IsPaused = true });
        }
        
        /// <summary>
        /// Execute unpause command (legacy - redirects to state sync).
        /// </summary>
        private static void ExecuteUnpauseInline()
        {
            if (SpeedControlScreen.Instance == null) return;
            
            int currentSpeed = SpeedControlScreen.Instance.GetSpeed();
            ExecuteSetGameSpeedInline(new SpeedCommand { Speed = currentSpeed, IsPaused = false });
        }
    }
}
