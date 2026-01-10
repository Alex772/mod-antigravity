using UnityEngine;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Command handler for Ping command.
    /// </summary>
    public static partial class CommandManager
    {
        /// <summary>
        /// Execute Ping command - shows ping to the local player
        /// </summary>
        private static void ExecutePingCommand(PingCommand cmd)
        {
            if (cmd == null) return;
            
            try
            {
                Debug.Log($"[Antigravity] EXECUTE: Ping from {cmd.PlayerName} at cell {cmd.Cell} ({cmd.X:F1}, {cmd.Y:F1})");
                
                // Use reflection to call PingManager.ShowPing since it's in Client assembly
                // This avoids direct dependency from Core to Client
                var pingManagerType = System.Type.GetType("Antigravity.Client.PingManager, Antigravity.Client");
                if (pingManagerType != null)
                {
                    var instanceProp = pingManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var instance = instanceProp?.GetValue(null);
                    if (instance != null)
                    {
                        var showPingMethod = pingManagerType.GetMethod("ShowPing");
                        showPingMethod?.Invoke(instance, new object[] { cmd });
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] ExecutePingCommand failed: {ex.Message}");
            }
        }
    }
}
