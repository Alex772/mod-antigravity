using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for intercepting Dig tool commands.
    /// </summary>
    public static class DigToolPatches
    {
        /// <summary>
        /// Patch for DigTool.OnDragComplete to capture dig commands.
        /// </summary>
        public static class DigTool_OnDragComplete_Patch
        {
            public static void Postfix(int cell, int distFromOrigin)
            {
                // Only capture if in multiplayer and not executing a remote command
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                var digCommand = new DigCommand
                {
                    Cells = new int[] { cell }
                };

                CommandManager.SendCommand(digCommand);
            }
        }

        /// <summary>
        /// Patch for DigTool.OnLeftClickDown to capture single dig commands.
        /// </summary>
        public static class DigTool_OnLeftClickDown_Patch
        {
            public static void Postfix(DigTool __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                // Get cell under cursor
                int cell = Grid.PosToCell(PlayerController.GetCursorPos(KInputManager.GetMousePos()));
                if (!Grid.IsValidCell(cell)) return;

                var digCommand = new DigCommand
                {
                    Cells = new int[] { cell }
                };

                CommandManager.SendCommand(digCommand);
            }
        }
    }

    /// <summary>
    /// Patches for intercepting Build tool commands.
    /// Note: Simplified for now - BuildTool API varies between ONI versions.
    /// </summary>
    public static class BuildToolPatches
    {
        public static class BuildTool_OnLeftClickDown_Patch
        {
            public static void Postfix(BuildTool __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                // Get cell under cursor
                int cell = Grid.PosToCell(PlayerController.GetCursorPos(KInputManager.GetMousePos()));
                if (!Grid.IsValidCell(cell)) return;

                // For now, just log the build action
                // TODO: Extract building def from __instance when we know the correct API
                Debug.Log($"[Antigravity] Build tool used at cell {cell}");

                // Placeholder - would need to determine actual building def
                // var buildCommand = new BuildCommand { Cell = cell };
                // CommandManager.SendCommand(buildCommand);
            }
        }
    }
    // NOTE: Speed control patches are implemented in SpeedSyncPatches.cs
    // The SpeedControlPatches class was removed to avoid code duplication.
    // See: src/Antigravity.Patches/Commands/SpeedSyncPatches.cs


    /// <summary>
    /// Patches for deconstruct tool.
    /// </summary>
    public static class DeconstructToolPatches
    {
        public static class DeconstructTool_OnDragComplete_Patch
        {
            public static void Postfix(int cell)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                var cmd = new DeconstructCommand
                {
                    Cells = new int[] { cell }
                };
                CommandManager.SendCommand(cmd);
            }
        }
    }
}
