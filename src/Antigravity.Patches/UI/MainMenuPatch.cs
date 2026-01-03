using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Antigravity.Core;

namespace Antigravity.Patches.UI
{
    /// <summary>
    /// Extension methods for MainMenu to add custom buttons.
    /// </summary>
    public static class MainMenuExtensions
    {
        private const int DefaultButtonFontSize = 22;

        /// <summary>
        /// Adds a button to the main menu using reflection to access private members.
        /// </summary>
        public static void AddButton(MainMenu menu, string text, System.Action action, bool highlight = false)
        {
            try
            {
                // Get the ButtonInfo type and its constructor via reflection
                var buttonInfoType = typeof(MainMenu).GetNestedType("ButtonInfo", BindingFlags.NonPublic | BindingFlags.Public);
                
                if (buttonInfoType == null)
                {
                    Debug.LogError("[Antigravity] Could not find MainMenu.ButtonInfo type");
                    return;
                }

                // Get button style
                var styleField = highlight 
                    ? typeof(MainMenu).GetField("topButtonStyle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    : typeof(MainMenu).GetField("normalButtonStyle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                object buttonStyle = null;
                if (styleField != null)
                {
                    buttonStyle = styleField.GetValue(menu);
                }

                // Create ButtonInfo instance
                var constructor = buttonInfoType.GetConstructor(new[] { 
                    typeof(LocString), 
                    typeof(System.Action), 
                    typeof(int), 
                    buttonStyle?.GetType() ?? typeof(object)
                });

                object buttonInfo = null;

                if (constructor != null)
                {
                    buttonInfo = constructor.Invoke(new object[] { 
                        new LocString(text), 
                        action, 
                        DefaultButtonFontSize, 
                        buttonStyle 
                    });
                }
                else
                {
                    // Try with 3 parameter constructor
                    constructor = buttonInfoType.GetConstructor(new[] { 
                        typeof(LocString), 
                        typeof(System.Action), 
                        typeof(int)
                    });
                    
                    if (constructor != null)
                    {
                        buttonInfo = constructor.Invoke(new object[] { 
                            new LocString(text), 
                            action, 
                            DefaultButtonFontSize
                        });
                    }
                }

                if (buttonInfo == null)
                {
                    Debug.LogError("[Antigravity] Could not create ButtonInfo instance");
                    return;
                }

                // Call MakeButton
                var makeButtonMethod = typeof(MainMenu).GetMethod("MakeButton", 
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (makeButtonMethod != null)
                {
                    makeButtonMethod.Invoke(menu, new object[] { buttonInfo });
                    Debug.Log($"[Antigravity] Button '{text}' added via MakeButton");
                }
                else
                {
                    Debug.LogError("[Antigravity] Could not find MakeButton method");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to add button: {ex.Message}");
                Debug.LogError(ex.StackTrace);
            }
        }
    }

    /// <summary>
    /// Patches the main menu to add Multiplayer buttons.
    /// Note: This patch is applied manually by PatchManager, not via attribute.
    /// </summary>
    public static class MainMenuPatch
    {
        /// <summary>
        /// Postfix patch to add Multiplayer buttons after MainMenu spawns.
        /// </summary>
        public static void Postfix(MainMenu __instance)
        {
            try
            {
                // Load config (check for command line args)
                MultiplayerConfig.Load();

                // Main multiplayer button (Steam) - always visible in all builds
                MainMenuExtensions.AddButton(__instance, "MULTIPLAYER", OnMultiplayerClick, highlight: true);
                
#if DEBUG
                // Local test button - ONLY visible in DEBUG builds (not in production)
                MainMenuExtensions.AddButton(__instance, "🔧 LOCAL TEST (DEV)", OnLocalTestClick, highlight: false);

                // Ensure hotkey handler exists (debug only)
                EnsureHotkeyHandler();
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to add multiplayer button: {ex.Message}");
                Debug.LogError(ex.StackTrace);
            }
        }

        private static void OnMultiplayerClick()
        {
            Debug.Log("[Antigravity] Multiplayer button clicked!");
            MultiplayerConfig.UseLocalNetworking = false;
            Client.MultiplayerLobbyScreen.Show();
        }

#if DEBUG
        private static void OnLocalTestClick()
        {
            Debug.Log("[Antigravity] Local Test button clicked!");
            MultiplayerConfig.UseLocalNetworking = true;
            Client.LocalTestLobbyScreen.Show();
        }

        private static void EnsureHotkeyHandler()
        {
            if (GameObject.Find("AntigravityHotkeyHandler") == null)
            {
                var handler = new GameObject("AntigravityHotkeyHandler");
                handler.AddComponent<MultiplayerHotkeyHandler>();
                Object.DontDestroyOnLoad(handler);
            }
        }
#endif
    }

#if DEBUG
    /// <summary>
    /// Handles global hotkeys for multiplayer features.
    /// DEBUG ONLY - Not included in production builds.
    /// F11 = Open Local Test lobby (for quick testing)
    /// F10 = Open Steam Multiplayer lobby
    /// </summary>
    public class MultiplayerHotkeyHandler : MonoBehaviour
    {
        private void Update()
        {
            // F11 = Open Local Test Mode (for development)
            if (Input.GetKeyDown(KeyCode.F11))
            {
                Debug.Log("[Antigravity] F11 pressed - Opening Local Test Mode");
                MultiplayerConfig.UseLocalNetworking = true;
                Client.LocalTestLobbyScreen.Show();
            }

            // F10 = Open Steam Multiplayer
            if (Input.GetKeyDown(KeyCode.F10))
            {
                Debug.Log("[Antigravity] F10 pressed - Opening Steam Multiplayer");
                MultiplayerConfig.UseLocalNetworking = false;
                Client.MultiplayerLobbyScreen.Show();
            }
        }
    }
#endif
}
