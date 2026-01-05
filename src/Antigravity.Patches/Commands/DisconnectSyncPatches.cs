using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using System.Collections.Generic;

namespace Antigravity.Patches.Commands
{
    /// <summary>
    /// Patches for DisconnectTool synchronization.
    /// DisconnectTool is used to cut wire/pipe connections.
    /// </summary>
    public static class DisconnectSyncPatches
    {
        private static List<DisconnectCommand> _pendingDisconnects = new List<DisconnectCommand>();

        /// <summary>
        /// Patch for DisconnectTool.OnDragComplete - capture the action using RunOnRegion callback
        /// We need to intercept the DisconnectCellsAction calls
        /// </summary>
        public static class DisconnectTool_OnDragComplete_Patch
        {
            public static void Prefix(DisconnectTool __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                // Clear pending list before drag operation
                _pendingDisconnects.Clear();
            }

            public static void Postfix(DisconnectTool __instance)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                // Send all pending disconnects
                SendPendingDisconnects();
            }
        }

        /// <summary>
        /// Capture each disconnect action as it happens during the drag
        /// This is called from a Harmony transpiler or manual interception
        /// </summary>
        public static void CaptureDisconnect(int cell, int removeConnections, string filterLayer)
        {
            if (!MultiplayerState.IsMultiplayerSession) return;
            if (CommandManager.IsExecutingRemoteCommand) return;

            var cmd = new DisconnectCommand
            {
                Cell = cell,
                RemoveConnections = removeConnections,
                FilterLayer = filterLayer
            };
            _pendingDisconnects.Add(cmd);
        }

        private static void SendPendingDisconnects()
        {
            if (_pendingDisconnects.Count == 0) return;

            Debug.Log($"[Antigravity] SEND: Disconnect {_pendingDisconnects.Count} connections");
            
            foreach (var cmd in _pendingDisconnects)
            {
                CommandManager.SendCommand(cmd);
            }
            
            _pendingDisconnects.Clear();
        }

        /// <summary>
        /// Alternative approach: Patch the actual disconnect action method
        /// DisconnectTool calls DisconnectCellsAction for each cell
        /// </summary>
        [HarmonyPatch(typeof(DisconnectTool), "DisconnectCellsAction")]
        public static class DisconnectTool_DisconnectCellsAction_Patch
        {
            public static void Postfix(int cell, GameObject objectOnCell, UtilityConnections removeConnections)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsGameLoaded) return;
                if (CommandManager.IsExecutingRemoteCommand) return;

                // Get the filter layer from the object
                string filterLayer = GetFilterLayerFromObject(objectOnCell);
                
                var cmd = new DisconnectCommand
                {
                    Cell = cell,
                    RemoveConnections = (int)removeConnections,
                    FilterLayer = filterLayer
                };
                
                Debug.Log($"[Antigravity] SEND: DisconnectUtility cell={cell} connections={(int)removeConnections} layer={filterLayer}");
                CommandManager.SendCommand(cmd);
            }

            private static string GetFilterLayerFromObject(GameObject go)
            {
                if (go == null) return "ALL";

                var building = go.GetComponent<Building>();
                if (building == null) return "ALL";

                var def = building.Def;
                if (def == null) return "ALL";

                // Check ObjectLayer to determine filter
                var layer = def.ObjectLayer;
                
                if (layer == ObjectLayer.Wire) return "WIRES";
                if (layer == ObjectLayer.LiquidConduit) return "LIQUIDCONDUIT";
                if (layer == ObjectLayer.GasConduit) return "GASCONDUIT";
                if (layer == ObjectLayer.SolidConduit) return "SOLIDCONDUIT";
                if (layer == ObjectLayer.LogicWire) return "LOGIC";
                
                return "ALL";
            }
        }
    }
}
