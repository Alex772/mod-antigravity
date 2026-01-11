using HarmonyLib;
using KMod;

namespace Antigravity
{
    /// <summary>
    /// Main entry point for the Antigravity Multiplayer Mod.
    /// This class is loaded by ONI's mod loader via Harmony.
    /// </summary>
    public class AntigravityMod : UserMod2
    {
        /// <summary>
        /// Harmony instance for patching game methods.
        /// </summary>
        public static Harmony HarmonyInstance { get; private set; }

        /// <summary>
        /// Mod configuration instance.
        /// </summary>
        public static ModConfig Config { get; private set; }

        /// <summary>
        /// Whether the mod is currently active.
        /// </summary>
        public static bool IsActive { get; private set; }

        /// <summary>
        /// Current mod version.
        /// </summary>
        public const string Version = "0.0.31-alpha";

        /// <summary>
        /// Called when the mod is loaded.
        /// </summary>
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            
            HarmonyInstance = harmony;
            IsActive = true;
            
            // Initialize configuration
            Config = new ModConfig();
            
            // Initialize logging
            Logger.Initialize();
            Logger.Log($"Antigravity Multiplayer Mod v{Version} loaded!");
            
            // Initialize core systems
            InitializeCore();
        }

        /// <summary>
        /// Initialize core mod systems.
        /// </summary>
        private void InitializeCore()
        {
            try
            {
                // Initialize network manager
                Core.Network.NetworkManager.Initialize();
                
                // Initialize command dispatcher
                Core.Commands.CommandDispatcher.Initialize();
                
                // Initialize sync engine
                Core.Sync.SyncEngine.Initialize();
                
                // Initialize game session manager
                Core.Network.GameSession.Initialize();
                
                // Apply game patches
                Patches.PatchManager.ApplyPatches(HarmonyInstance);
                
                // Register Steam join callback early (so invites work from friend list)
                Core.Network.SteamNetworkManager.EnsureJoinCallbackRegistered();
                
                Logger.Log("Core systems initialized successfully.");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Failed to initialize core systems: {ex.Message}");
                Logger.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Called when the game is shutting down.
        /// </summary>
        public override void OnAllModsLoaded(Harmony harmony, System.Collections.Generic.IReadOnlyList<Mod> mods)
        {
            base.OnAllModsLoaded(harmony, mods);
            Logger.Log($"All mods loaded. Antigravity ready!");
        }
    }
}
