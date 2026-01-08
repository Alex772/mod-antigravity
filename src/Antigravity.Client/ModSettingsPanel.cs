using UnityEngine;
using UnityEngine.UI;

namespace Antigravity.Client
{
    /// <summary>
    /// Mod settings panel - accessed via chat settings button.
    /// </summary>
    public class ModSettingsPanel : MonoBehaviour
    {
        private static ModSettingsPanel _instance;
        public static ModSettingsPanel Instance => _instance;
        
        private GameObject _panelObj;
        private bool _isVisible = false;
        
        // Settings
        public static bool ShowCoordinates = true;
        
        void Start()
        {
            _instance = this;
            CreatePanel();
            Hide();
            Debug.Log("[Antigravity] ModSettingsPanel started");
        }
        
        private void CreatePanel()
        {
            var canvas = FindGameCanvas();
            if (canvas == null)
            {
                Invoke(nameof(CreatePanel), 1f);
                return;
            }
            
            // Main panel
            _panelObj = new GameObject("ModSettingsPanel");
            _panelObj.transform.SetParent(canvas.transform, false);
            
            var rect = _panelObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(350, 250);
            
            var bg = _panelObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
            
            // Header
            CreateHeader();
            
            // Content
            CreateContent();
            
            Debug.Log("[Antigravity] ModSettingsPanel created");
        }
        
        private void CreateHeader()
        {
            var header = new GameObject("Header");
            header.transform.SetParent(_panelObj.transform, false);
            
            var rect = header.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 35);
            rect.anchoredPosition = Vector2.zero;
            
            var bg = header.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.45f, 0.65f, 1f);
            
            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(header.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(15, 0);
            titleRect.offsetMax = new Vector2(-40, 0);
            
            var title = titleGo.AddComponent<Text>();
            title.text = "⚙ Antigravity Settings";
            title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.fontSize = 16;
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleLeft;
            
            // Close button
            var closeBtn = CreateButton(header.transform, "✕", 28, 28, new Vector2(-5, 0));
            closeBtn.onClick.AddListener(Hide);
        }
        
        private void CreateContent()
        {
            var content = new GameObject("Content");
            content.transform.SetParent(_panelObj.transform, false);
            
            var rect = content.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(15, 15);
            rect.offsetMax = new Vector2(-15, -45);
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 10;
            
            // Coordinates toggle
            CreateToggleSetting(content.transform, "Show Cursor Coordinates", ShowCoordinates, (val) => {
                ShowCoordinates = val;
                if (CursorCoordsOverlay.Instance != null)
                {
                    CursorCoordsOverlay.Instance.SetVisible(val);
                }
            });
            
            // Divider
            CreateDivider(content.transform);
            
            // Info label
            CreateLabel(content.transform, "More settings coming soon...", new Color(0.5f, 0.5f, 0.6f));
        }
        
        private void CreateToggleSetting(Transform parent, string label, bool defaultValue, System.Action<bool> onChange)
        {
            var row = new GameObject("ToggleRow");
            row.transform.SetParent(parent, false);
            
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, 30);
            
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 10;
            
            // Toggle
            var toggleGo = new GameObject("Toggle");
            toggleGo.transform.SetParent(row.transform, false);
            
            var toggleImg = toggleGo.AddComponent<Image>();
            toggleImg.color = defaultValue ? new Color(0.3f, 0.7f, 0.3f) : new Color(0.3f, 0.3f, 0.3f);
            
            var toggleLe = toggleGo.AddComponent<LayoutElement>();
            toggleLe.minWidth = 40;
            toggleLe.minHeight = 22;
            
            // Toggle button behavior
            var toggleBtn = toggleGo.AddComponent<Button>();
            toggleBtn.targetGraphic = toggleImg;
            
            bool currentValue = defaultValue;
            toggleBtn.onClick.AddListener(() => {
                currentValue = !currentValue;
                toggleImg.color = currentValue ? new Color(0.3f, 0.7f, 0.3f) : new Color(0.3f, 0.3f, 0.3f);
                onChange?.Invoke(currentValue);
            });
            
            // Toggle text
            var toggleTextGo = new GameObject("ToggleText");
            toggleTextGo.transform.SetParent(toggleGo.transform, false);
            var toggleTextRect = toggleTextGo.AddComponent<RectTransform>();
            toggleTextRect.anchorMin = Vector2.zero;
            toggleTextRect.anchorMax = Vector2.one;
            toggleTextRect.offsetMin = Vector2.zero;
            toggleTextRect.offsetMax = Vector2.zero;
            
            var toggleText = toggleTextGo.AddComponent<Text>();
            toggleText.text = defaultValue ? "ON" : "OFF";
            toggleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            toggleText.fontSize = 11;
            toggleText.fontStyle = FontStyle.Bold;
            toggleText.color = Color.white;
            toggleText.alignment = TextAnchor.MiddleCenter;
            
            toggleBtn.onClick.AddListener(() => {
                toggleText.text = currentValue ? "ON" : "OFF";
            });
            
            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelText = labelGo.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 13;
            labelText.color = Color.white;
            
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1;
        }
        
        private void CreateDivider(Transform parent)
        {
            var divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);
            
            var img = divider.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.4f, 0.5f);
            
            var le = divider.AddComponent<LayoutElement>();
            le.minHeight = 1;
            le.flexibleWidth = 1;
        }
        
        private void CreateLabel(Transform parent, string text, Color color)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(parent, false);
            
            var labelText = labelGo.AddComponent<Text>();
            labelText.text = text;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 12;
            labelText.fontStyle = FontStyle.Italic;
            labelText.color = color;
            
            var le = labelGo.AddComponent<LayoutElement>();
            le.minHeight = 20;
        }
        
        private Button CreateButton(Transform parent, string text, float width, float height, Vector2 position)
        {
            var btnGo = new GameObject("Button");
            btnGo.transform.SetParent(parent, false);
            
            var rect = btnGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, height);
            
            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.4f, 0.2f, 0.2f, 1f);
            
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            
            var btnText = textGo.AddComponent<Text>();
            btnText.text = text;
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontSize = 14;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            
            return btn;
        }
        
        private Canvas FindGameCanvas()
        {
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                    return c;
            }
            return canvases.Length > 0 ? canvases[0] : null;
        }
        
        public void Toggle()
        {
            if (_isVisible) Hide();
            else Show();
        }
        
        public void Show()
        {
            _isVisible = true;
            if (_panelObj != null) _panelObj.SetActive(true);
        }
        
        public void Hide()
        {
            _isVisible = false;
            if (_panelObj != null) _panelObj.SetActive(false);
        }
        
        void OnDestroy()
        {
            if (_panelObj != null) Destroy(_panelObj);
        }
    }
}
