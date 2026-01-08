using UnityEngine;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for Speed commands: SetGameSpeed, Pause, Unpause
    /// </summary>
    public static partial class CommandManager
    {
        // Track current speed for pause/unpause (default 1)
        private static int _currentSpeed = 1;
        
        private static void ExecuteSpeedCommand(SpeedCommand cmd)
        {
            if (cmd == null || SpeedControlScreen.Instance == null) return;

            switch (cmd.Speed)
            {
                case 0:
                    SpeedControlScreen.Instance.Pause(false);
                    break;
                case 1:
                case 2:
                case 3:
                    // First unpause if paused, then set speed
                    SpeedControlScreen.Instance.Unpause(false);
                    SpeedControlScreen.Instance.SetSpeed(cmd.Speed);
                    break;
            }
            Debug.Log($"[Antigravity] Executed speed change to {cmd.Speed}");
        }
        
        private static void ExecuteSetGameSpeedInline(SpeedCommand speedCmd)
        {
            if (speedCmd == null) return;
            
            // ONI speed values: 0=slow(1x), 1=medium(2x), 2=fast(3x)
            // Our command uses 1=slow, 2=medium, 3=fast
            // So we convert: our speed - 1 = ONI speed
            int oniSpeed = speedCmd.Speed - 1;
            if (oniSpeed >= 0 && oniSpeed <= 2)
            {
                _currentSpeed = oniSpeed;
                Debug.Log($"[Antigravity] EXECUTE: Speed {speedCmd.Speed} -> ONI speed index {oniSpeed}");
                
                if (SpeedControlScreen.Instance != null)
                {
                    SpeedControlScreen.Instance.SetSpeed(oniSpeed);
                }
            }
        }
        
        private static void ExecutePauseInline()
        {
            Debug.Log($"[Antigravity] EXECUTE: Pause");
            if (SpeedControlScreen.Instance != null)
            {
                // Save current speed before pausing
                _currentSpeed = SpeedControlScreen.Instance.GetSpeed();
                SpeedControlScreen.Instance.Pause(false); // false = no sound
            }
        }
        
        private static void ExecuteUnpauseInline()
        {
            Debug.Log($"[Antigravity] EXECUTE: Unpause");
            if (SpeedControlScreen.Instance != null)
            {
                SpeedControlScreen.Instance.Unpause(false); // false = no sound
            }
        }
    }
}
