using UnityEngine;
using System.Reflection;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Partial class for TimeSync command: applies host's pauseCount, speed, and GameClock time on client.
    /// 
    /// WHY: SpeedControlScreen.pauseCount is ref-counted. Any local UI that opens a menu calls
    /// Pause() incrementing it. If the client opens a building detail screen, pauseCount goes up,
    /// but the host doesn't know. This causes Time.timeScale to stay at 0 on the client while the
    /// host continues running, creating progressive time drift.
    /// 
    /// HOW: Host sends TimeSyncCommand every 5 seconds with its current pauseCount, speed, and 
    /// GameClock time. Client forces its local state to match using reflection for the private
    /// pauseCount field.
    /// </summary>
    public static partial class CommandManager
    {
        // Cached reflection for SpeedControlScreen.pauseCount (private field)
        private static FieldInfo _pauseCountField;

        /// <summary>
        /// Execute a TimeSync command (client-side only).
        /// Forces local pauseCount and speed to match host, and logs time drift.
        /// </summary>
        private static void ExecuteTimeSyncInline(TimeSyncCommand cmd)
        {
            if (cmd == null) return;

            // Only clients should apply time sync
            if (Network.MultiplayerState.IsHost) return;

            if (SpeedControlScreen.Instance == null)
            {
                Debug.LogWarning("[Antigravity] SpeedControlScreen.Instance is null, cannot apply time sync");
                return;
            }

            // --- 1. Force pauseCount to match host ---
            if (_pauseCountField == null)
            {
                _pauseCountField = typeof(SpeedControlScreen).GetField("pauseCount",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (_pauseCountField != null)
            {
                int currentPauseCount = (int)_pauseCountField.GetValue(SpeedControlScreen.Instance);
                if (currentPauseCount != cmd.PauseCount)
                {
                    Debug.Log($"[Antigravity] TimeSync: Correcting pauseCount {currentPauseCount} → {cmd.PauseCount}");
                    _pauseCountField.SetValue(SpeedControlScreen.Instance, cmd.PauseCount);
                }
            }
            else
            {
                Debug.LogWarning("[Antigravity] TimeSync: Could not find pauseCount field via reflection");
            }

            // --- 2. Force speed to match host ---
            SpeedControlScreen.Instance.SetSpeed(cmd.Speed);

            // --- 3. Update Time.timeScale based on host pause state ---
            if (cmd.PauseCount > 0)
            {
                Time.timeScale = 0f;
            }
            else
            {
                // Let SetSpeed handle the timeScale via OnChanged()
                // But ensure it's not stuck at 0
                if (Time.timeScale == 0f)
                {
                    // Trigger OnChanged to recalculate timeScale
                    SpeedControlScreen.Instance.SetSpeed(cmd.Speed);
                }
            }

            // --- 4. Log time drift for diagnostics ---
            if (GameClock.Instance != null)
            {
                float localTime = GameClock.Instance.GetTime();
                float drift = Mathf.Abs(localTime - cmd.GameTime);
                if (drift > 1.0f)
                {
                    Debug.LogWarning($"[Antigravity] TimeSync: Clock drift detected! Local={localTime:F1}s Host={cmd.GameTime:F1}s Drift={drift:F1}s");
                }
            }
        }
    }
}
