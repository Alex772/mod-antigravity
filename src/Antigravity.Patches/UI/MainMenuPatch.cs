using HarmonyLib;
using System.Reflection;
using UnityEngine;

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
    /// Patches the main menu to add a Multiplayer button.
    /// </summary>
    [HarmonyPatch(typeof(MainMenu), "OnSpawn")]
    public static class MainMenuPatch
    {
        /// <summary>
        /// Postfix patch to add Multiplayer button after MainMenu spawns.
        /// </summary>
        public static void Postfix(MainMenu __instance)
        {
            try
            {
                MainMenuExtensions.AddButton(__instance, "MULTIPLAYER", OnMultiplayerClick, highlight: true);
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
            
            // Show the multiplayer lobby screen
            Client.MultiplayerLobbyScreen.Show();
        }
    }
}
