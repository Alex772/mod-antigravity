using HarmonyLib;
using UnityEngine;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;
using Antigravity.Core.Sync;

namespace Antigravity.Patches.Sync
{
    /// <summary>
    /// Patches for Element/Gas/Liquid synchronization.
    /// Intercepts element-changing operations and tracks them for sync.
    /// Note: Only patches methods with single signatures to avoid ambiguous match errors.
    /// </summary>
    public static class ElementSyncPatches
    {
        /// <summary>
        /// Patch for SimMessages.ModifyCell - intercepts element changes
        /// This is called when elements are placed/modified via debug tools
        /// </summary>
        [HarmonyPatch(typeof(SimMessages), nameof(SimMessages.ModifyCell))]
        public static class SimMessages_ModifyCell_Patch
        {
            public static void Postfix(int gameCell, int elementIdx, float temperature, float mass)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsHost) return;
                if (CommandManager.IsExecutingRemoteCommand) return;
                
                try
                {
                    ElementSyncManager.Instance.TrackElementChange(
                        gameCell, 
                        elementIdx, 
                        mass, 
                        temperature, 
                        ElementChangeType.Replace
                    );
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[Antigravity] ElementSync: Error tracking ModifyCell: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Patch for SimMessages.Dig - intercepts dig operations that remove elements
        /// </summary>
        [HarmonyPatch(typeof(SimMessages), nameof(SimMessages.Dig))]
        public static class SimMessages_Dig_Patch
        {
            public static void Postfix(int gameCell)
            {
                if (!MultiplayerState.IsMultiplayerSession) return;
                if (!MultiplayerState.IsHost) return;
                if (CommandManager.IsExecutingRemoteCommand) return;
                
                try
                {
                    // When digging, element at cell is removed (becomes vacuum/background)
                    ElementSyncManager.Instance.TrackElementChange(
                        gameCell, 
                        (int)SimHashes.Vacuum, 
                        0f, 
                        0f, 
                        ElementChangeType.Remove
                    );
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[Antigravity] ElementSync: Error tracking Dig: {ex.Message}");
                }
            }
        }
        
        // Note: SimMessages.ReplaceElement, AddRemoveSubstance, EmitMass, and ConsumeMass 
        // have multiple overloads that cause "Ambiguous match" errors with Harmony.
        // These operations will be synced via the delta-based checksum/resync system instead.
    }
}
