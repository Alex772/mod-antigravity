using UnityEngine;
using UnityEngine.UI;

namespace Antigravity.Client.UI
{
    /// <summary>
    /// Factory class for creating common UI elements.
    /// Provides consistent styling across all mod UI screens.
    /// </summary>
    public static class UIFactory
    {
        #region Colors
        
        /// <summary>Default button color (blue)</summary>
        public static readonly Color ButtonNormalColor = new Color(0.25f, 0.45f, 0.75f, 1f);
        public static readonly Color ButtonHighlightColor = new Color(0.35f, 0.55f, 0.85f, 1f);
        public static readonly Color ButtonPressedColor = new Color(0.2f, 0.35f, 0.65f, 1f);
        
        /// <summary>Input field background color (dark gray)</summary>
        public static readonly Color InputBackgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        
        /// <summary>Placeholder text color</summary>
        public static readonly Color PlaceholderColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        #endregion
        
        #region Panel Creation
        
        /// <summary>
        /// Create an empty panel with RectTransform.
        /// </summary>
        public static GameObject CreatePanel(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }
        
        /// <summary>
        /// Create a panel with background image.
        /// </summary>
        public static GameObject CreatePanel(string name, Transform parent, Color backgroundColor)
        {
            var obj = CreatePanel(name, parent);
            var image = obj.AddComponent<Image>();
            image.color = backgroundColor;
            return obj;
        }
        
        #endregion
        
        #region Text Creation
        
        /// <summary>
        /// Create a text element with specified size and alignment.
        /// </summary>
        public static GameObject CreateText(string name, Transform parent, string text, int fontSize, 
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 40);

            var textComp = obj.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComp.fontSize = fontSize;
            textComp.alignment = alignment;
            textComp.color = Color.white;

            return obj;
        }
        
        /// <summary>
        /// Create a text element with custom size.
        /// </summary>
        public static GameObject CreateText(string name, Transform parent, string text, int fontSize, 
            float width, float height, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var obj = CreateText(name, parent, text, fontSize, alignment);
            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            return obj;
        }
        
        #endregion
        
        #region Button Creation
        
        /// <summary>
        /// Create a button with text label.
        /// </summary>
        public static GameObject CreateButton(string name, Transform parent, string text, 
            UnityEngine.Events.UnityAction onClick, float width = 200, float height = 50)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            var image = obj.AddComponent<Image>();
            image.color = ButtonNormalColor;

            var button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var colors = button.colors;
            colors.normalColor = ButtonNormalColor;
            colors.highlightedColor = ButtonHighlightColor;
            colors.pressedColor = ButtonPressedColor;
            button.colors = colors;

            // Create text child
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComp.fontSize = 18;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;

            return obj;
        }
        
        #endregion
        
        #region Input Field Creation
        
        /// <summary>
        /// Create an input field with placeholder text.
        /// </summary>
        public static InputField CreateInputField(string name, Transform parent, string placeholder,
            float width = 300, float height = 40)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            var image = obj.AddComponent<Image>();
            image.color = InputBackgroundColor;

            // Placeholder
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(obj.transform, false);
            var phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(10, 0);
            phRect.offsetMax = new Vector2(-10, 0);
            var phText = placeholderObj.AddComponent<Text>();
            phText.text = placeholder;
            phText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            phText.fontSize = 14;
            phText.color = PlaceholderColor;
            phText.alignment = TextAnchor.MiddleCenter;

            // Text content
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            var textComp = textObj.AddComponent<Text>();
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComp.fontSize = 16;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.supportRichText = false;

            var inputField = obj.AddComponent<InputField>();
            inputField.textComponent = textComp;
            inputField.placeholder = phText;

            return inputField;
        }
        
        #endregion
    }
}
