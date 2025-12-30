using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for Door state synchronization.
    /// Intercepts Door.QueueStateChange to sync door controls between players.
    /// </summary>
    public static class DoorSyncPatches
    {
        /// <summary>
        /// Patch for Door.QueueStateChange - called when player changes door state
        /// </summary>
        public static class Door_QueueStateChange_Patch
        {
            public static void Postfix(Door __instance, Door.ControlState nextState)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                int cell = Grid.PosToCell(__instance.transform.GetPosition());
                
                Debug.Log($"[Antigravity] SEND: DoorState cell={cell} state={(int)nextState}");

                var cmd = new DoorStateCommand
                {
                    Cell = cell,
                    ControlState = (int)nextState
                };
                CommandManager.SendCommand(cmd);
            }
        }

        /// <summary>
        /// Patch for Door.OnCopySettings - sync when settings are copied to a door
        /// </summary>
        public static class Door_OnCopySettings_Patch
        {
            public static void Postfix(Door __instance, object data)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                // After copy settings, sync the door state
                int cell = Grid.PosToCell(__instance.transform.GetPosition());
                
                // Get current state from the door
                try
                {
                    var controller = __instance.GetComponent<Door.Controller.Instance>();
                    if (controller != null)
                    {
                        // Get requested state - this may require reflection
                        // For now, we'll sync the visible state
                    }
                }
                catch { }
            }
        }
    }
}
