using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Antigravity.Core.Network;

namespace Antigravity.Client
{
    /// <summary>
    /// Floating chat window - always visible with collapse option.
    /// Includes settings button for mod configuration.
    /// </summary>
    public class ChatOverlay : MonoBehaviour
    {
        private static ChatOverlay _instance;
        public static ChatOverlay Instance => _instance;
        
        // UI Elements
        private GameObject _windowObj;
        private GameObject _headerObj;
        private GameObject _contentObj;
        private GameObject _inputContainer;
        private GameObject _resizeHandle;
        private InputField _inputField;
        private Text _titleText;
        private Button _collapseBtn;
        private Button _settingsBtn;
        private List<GameObject> _messageObjects = new List<GameObject>();
        
        private bool _isCollapsed = false;
        private bool _isInputActive = false;
        private const int MAX_VISIBLE_MESSAGES = 10;
        private const float WINDOW_WIDTH = 420f;
        private const float WINDOW_HEIGHT = 320f;
        private const float COLLAPSED_HEIGHT = 36f;
        
        void Start()
        {
            _instance = this;
            CreateWindow();
            ChatManager.OnMessageReceived += OnNewMessage;
            Debug.Log("[Antigravity] ChatOverlay started");
        }
        
        void Update()
        {
            // Check if window was destroyed (parent canvas was destroyed on scene change)
            if (_windowObj == null)
            {
                CreateWindow();
                return;
            }
            
            // Handle input when chat field is focused
            if (_isInputActive && _inputField != null)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    SendMessage();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseInput();
                }
            }
        }
        
        private void CreateWindow()
        {
            // Get or create a canvas for the chat
            var gameCanvas = GetGameCanvas();
            
            // Main window container
            _windowObj = CreatePanel(gameCanvas.transform, "ChatWindow", WINDOW_WIDTH, WINDOW_HEIGHT);
            var windowRect = _windowObj.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0, 0);
            windowRect.anchorMax = new Vector2(0, 0);
            windowRect.pivot = new Vector2(0, 0);
            windowRect.anchoredPosition = new Vector2(10, 60);
            
            var windowBg = _windowObj.GetComponent<Image>();
            windowBg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
            
            // Header bar
            _headerObj = CreatePanel(_windowObj.transform, "Header", WINDOW_WIDTH, 28);
            var headerRect = _headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 28);
            
            var headerBg = _headerObj.GetComponent<Image>();
            headerBg.color = new Color(0.2f, 0.4f, 0.6f, 0.95f);
            
            // Title text
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_headerObj.transform, false);
            _titleText = titleObj.AddComponent<Text>();
            _titleText.text = "💬 Chat";
            _titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _titleText.fontSize = 18;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = Color.white;
            _titleText.alignment = TextAnchor.MiddleLeft;
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-70, 0);
            
            // Settings button
            _settingsBtn = CreateButton(_headerObj.transform, "⚙", 30, 30, new Vector2(-45, 0));
            _settingsBtn.onClick.AddListener(OpenSettings);
            
            // Collapse button
            _collapseBtn = CreateButton(_headerObj.transform, "−", 30, 30, new Vector2(-10, 0));
            _collapseBtn.onClick.AddListener(ToggleCollapse);
            
            // Make header draggable
            var dragHandler = _headerObj.AddComponent<DraggableWindow>();
            dragHandler.SetWindowTransform(_windowObj.GetComponent<RectTransform>());
            
            // Content area
            _contentObj = CreatePanel(_windowObj.transform, "Content", WINDOW_WIDTH, WINDOW_HEIGHT - 28 - 32);
            var contentRect = _contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.offsetMin = new Vector2(5, 32);
            contentRect.offsetMax = new Vector2(-5, -28);
            
            _contentObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);
            
            var vlg = _contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.LowerLeft;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 2;
            vlg.padding = new RectOffset(5, 5, 5, 5);
            
            // Input container
            _inputContainer = CreatePanel(_windowObj.transform, "InputContainer", WINDOW_WIDTH, 28);
            var inputRect = _inputContainer.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0);
            inputRect.pivot = new Vector2(0.5f, 0);
            inputRect.anchoredPosition = Vector2.zero;
            inputRect.sizeDelta = new Vector2(0, 28);
            
            _inputContainer.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            
            // Input field
            var inputGo = new GameObject("InputField");
            inputGo.transform.SetParent(_inputContainer.transform, false);
            var inputFieldRect = inputGo.AddComponent<RectTransform>();
            inputFieldRect.anchorMin = Vector2.zero;
            inputFieldRect.anchorMax = Vector2.one;
            inputFieldRect.offsetMin = new Vector2(5, 2);
            inputFieldRect.offsetMax = new Vector2(-5, -2);
            
            var inputBg = inputGo.AddComponent<Image>();
            inputBg.color = new Color(0.1f, 0.1f, 0.12f, 1f);
            
            _inputField = inputGo.AddComponent<InputField>();
            
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(inputGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);
            
            var inputText = textGo.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            inputText.fontSize = 14;
            inputText.color = Color.white;
            inputText.supportRichText = false;
            
            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(inputGo.transform, false);
            var phRect = placeholderGo.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(5, 0);
            phRect.offsetMax = new Vector2(-5, 0);
            
            var phText = placeholderGo.AddComponent<Text>();
            phText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            phText.fontSize = 14;
            phText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            phText.text = "Type message and press Enter...";
            phText.fontStyle = FontStyle.Italic;
            
            _inputField.textComponent = inputText;
            _inputField.placeholder = phText;
            
            // Track input focus and block game input when typing
            _inputField.onValueChanged.AddListener(_ => {
                _isInputActive = true;
                BlockGameInput(true);
            });
            _inputField.onEndEdit.AddListener(_ => {
                // Input ended (blur or enter)
                BlockGameInput(false);
            });
            
            // Resize handle (bottom-right corner)
            _resizeHandle = new GameObject("ResizeHandle");
            _resizeHandle.transform.SetParent(_windowObj.transform, false);
            var resizeRect = _resizeHandle.AddComponent<RectTransform>();
            resizeRect.anchorMin = new Vector2(1, 0);
            resizeRect.anchorMax = new Vector2(1, 0);
            resizeRect.pivot = new Vector2(1, 0);
            resizeRect.anchoredPosition = Vector2.zero;
            resizeRect.sizeDelta = new Vector2(20, 20);
            
            var resizeImg = _resizeHandle.AddComponent<Image>();
            resizeImg.color = new Color(0.4f, 0.5f, 0.7f, 0.8f);
            
            // Add diagonal lines to indicate resize (using text for simplicity)
            var resizeText = new GameObject("ResizeIcon");
            resizeText.transform.SetParent(_resizeHandle.transform, false);
            var resizeTextRect = resizeText.AddComponent<RectTransform>();
            resizeTextRect.anchorMin = Vector2.zero;
            resizeTextRect.anchorMax = Vector2.one;
            resizeTextRect.offsetMin = Vector2.zero;
            resizeTextRect.offsetMax = Vector2.zero;
            var resizeLabel = resizeText.AddComponent<Text>();
            resizeLabel.text = "⋱";
            resizeLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            resizeLabel.fontSize = 14;
            resizeLabel.color = Color.white;
            resizeLabel.alignment = TextAnchor.MiddleCenter;
            
            var resizer = _resizeHandle.AddComponent<ResizableWindow>();
            resizer.SetWindowTransform(_windowObj.GetComponent<RectTransform>());
            resizer.MinWidth = 300f;
            resizer.MinHeight = 200f;
            resizer.MaxWidth = 800f;
            resizer.MaxHeight = 600f;
            
            Debug.Log("[Antigravity] ChatOverlay window created");
        }
        
        private Canvas GetGameCanvas()
        {
            // Always create our own canvas to ensure it persists between scenes
            // Looking for existing Antigravity canvas first
            var existingCanvas = GameObject.Find("AntigravityChatCanvas");
            if (existingCanvas != null)
            {
                return existingCanvas.GetComponent<Canvas>();
            }
            
            // Create dedicated canvas
            Debug.Log("[Antigravity] ChatOverlay: Creating dedicated canvas");
            var canvasGo = new GameObject("AntigravityChatCanvas");
            DontDestroyOnLoad(canvasGo);
            
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Ensure it's on top
            
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGo.AddComponent<GraphicRaycaster>();
            
            return canvas;
        }
        
        private GameObject CreatePanel(Transform parent, string name, float width, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            go.AddComponent<Image>();
            return go;
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
            img.color = new Color(0.3f, 0.3f, 0.4f, 1f);
            
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var btnText = textGo.AddComponent<Text>();
            btnText.text = text;
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontSize = 14;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            
            return btn;
        }
        
        public void ToggleCollapse()
        {
            _isCollapsed = !_isCollapsed;
            
            if (_contentObj != null) _contentObj.SetActive(!_isCollapsed);
            if (_inputContainer != null) _inputContainer.SetActive(!_isCollapsed);
            if (_resizeHandle != null) _resizeHandle.SetActive(!_isCollapsed);
            
            var windowRect = _windowObj.GetComponent<RectTransform>();
            windowRect.sizeDelta = new Vector2(WINDOW_WIDTH, _isCollapsed ? COLLAPSED_HEIGHT : WINDOW_HEIGHT);
            
            // Clamp position to keep window on screen when expanding
            if (!_isCollapsed)
            {
                ClampWindowPosition();
            }
            
            // Update collapse button text
            var btnText = _collapseBtn.GetComponentInChildren<Text>();
            if (btnText != null) btnText.text = _isCollapsed ? "+" : "−";
        }
        
        /// <summary>
        /// Ensure window stays within screen bounds.
        /// </summary>
        private void ClampWindowPosition()
        {
            if (_windowObj == null) return;
            
            var rect = _windowObj.GetComponent<RectTransform>();
            var pos = rect.anchoredPosition;
            float height = _isCollapsed ? COLLAPSED_HEIGHT : WINDOW_HEIGHT;
            
            // Get screen size (assuming canvas scaler reference of 1920x1080)
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            // Clamp X: keep at least 50px visible on left/right
            pos.x = Mathf.Clamp(pos.x, 10, screenWidth - WINDOW_WIDTH - 10);
            
            // Clamp Y: keep at least header visible, don't go below 10px
            pos.y = Mathf.Clamp(pos.y, 10, screenHeight - height - 10);
            
            rect.anchoredPosition = pos;
        }
        
        private void OpenSettings()
        {
            // Open mod settings panel
            if (ModSettingsPanel.Instance != null)
            {
                ModSettingsPanel.Instance.Toggle();
            }
            else
            {
                Debug.Log("[Antigravity] Settings panel not available");
            }
        }
        
        private void SendMessage()
        {
            if (_inputField == null) return;
            
            string text = _inputField.text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                ChatManager.SendMessage(text);
            }
            
            _inputField.text = "";
            _isInputActive = false;
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }
        
        private void CloseInput()
        {
            if (_inputField != null)
            {
                _inputField.text = "";
            }
            _isInputActive = false;
            BlockGameInput(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }
        
        /// <summary>
        /// Block or unblock game keyboard input while typing in chat.
        /// </summary>
        private void BlockGameInput(bool block)
        {
            try
            {
                // Use PlayerController.AllowKeyboard to control game input via reflection
                if (PlayerController.Instance != null)
                {
                    // When block is true, disable keyboard; when false, enable it
                    var prop = typeof(PlayerController).GetProperty("AllowKeyboard");
                    if (prop != null)
                    {
                        prop.SetValue(PlayerController.Instance, !block);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Antigravity] BlockGameInput failed: {ex.Message}");
            }
        }

        
        private void OnNewMessage(ChatEntry entry)
        {
            if (_contentObj == null) return;
            
            var msgObj = new GameObject("Message");
            msgObj.transform.SetParent(_contentObj.transform, false);
            
            var text = msgObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 14;
            text.text = entry.DisplayText;
            
            // Color by type
            switch (entry.Type)
            {
                case ChatMessageType.System:
                    text.color = entry.Level switch
                    {
                        SystemMessageLevel.Warning => new Color(1f, 0.8f, 0.2f),
                        SystemMessageLevel.Error => new Color(1f, 0.4f, 0.4f),
                        _ => new Color(0.6f, 0.8f, 1f)
                    };
                    break;
                default:
                    text.color = Color.white;
                    break;
            }
            
            var le = msgObj.AddComponent<LayoutElement>();
            le.minHeight = 16;
            
            _messageObjects.Add(msgObj);
            
            while (_messageObjects.Count > MAX_VISIBLE_MESSAGES)
            {
                Destroy(_messageObjects[0]);
                _messageObjects.RemoveAt(0);
            }
        }
        
        void OnDestroy()
        {
            ChatManager.OnMessageReceived -= OnNewMessage;
            if (_windowObj != null) Destroy(_windowObj);
        }
    }
}
