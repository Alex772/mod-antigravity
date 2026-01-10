using UnityEngine;
using UnityEngine.UI;

namespace Antigravity.Client
{
    /// <summary>
    /// Always-visible cursor coordinates overlay.
    /// Shows current cell (X, Y) at mouse position.
    /// Can be toggled via ModSettingsPanel.
    /// </summary>
    public class CursorCoordsOverlay : MonoBehaviour
    {
        private static CursorCoordsOverlay _instance;
        public static CursorCoordsOverlay Instance => _instance;
        
        private GameObject _overlayObj;
        private Text _coordsText;
        private bool _isVisible = true;
        
        void Start()
        {
            _instance = this;
            CreateOverlay();
            // Read initial visibility from settings
            _isVisible = ModSettingsPanel.ShowCoordinates;
            Debug.Log("[Antigravity] CursorCoordsOverlay started");
        }
        
        void Update()
        {
            // Check if overlay was destroyed (parent canvas was destroyed on scene change)
            if (_overlayObj == null)
            {
                CreateOverlay();
                return;
            }
            
            if (!_isVisible || !_overlayObj.activeSelf) return;
            UpdateCoordinates();
        }
        
        private void CreateOverlay()
        {
            var canvas = FindGameCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[Antigravity] CursorCoordsOverlay: No canvas found, retrying...");
                Invoke(nameof(CreateOverlay), 1f);
                return;
            }
            
            _overlayObj = new GameObject("CursorCoordsOverlay");
            _overlayObj.transform.SetParent(canvas.transform, false);
            
            var rectTransform = _overlayObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(10, -10);
            rectTransform.sizeDelta = new Vector2(140, 28);
            
            // Background
            var bg = _overlayObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
            
            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(_overlayObj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 2);
            textRect.offsetMax = new Vector2(-8, -2);
            
            _coordsText = textObj.AddComponent<Text>();
            _coordsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _coordsText.fontSize = 13;
            _coordsText.fontStyle = FontStyle.Bold;
            _coordsText.color = new Color(0.8f, 0.9f, 1f);
            _coordsText.alignment = TextAnchor.MiddleLeft;
            _coordsText.text = "📍 Cell: --, --";
            
            Debug.Log("[Antigravity] CursorCoordsOverlay created");
        }
        
        private Canvas FindGameCanvas()
        {
            // Use the same dedicated canvas as ChatOverlay for consistency
            var existingCanvas = GameObject.Find("AntigravityChatCanvas");
            if (existingCanvas != null)
            {
                return existingCanvas.GetComponent<Canvas>();
            }
            
            // Create dedicated canvas if ChatOverlay hasn't created one yet
            Debug.Log("[Antigravity] CursorCoordsOverlay: Creating dedicated canvas");
            var canvasGo = new GameObject("AntigravityChatCanvas");
            DontDestroyOnLoad(canvasGo);
            
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            return canvas;
        }
        
        private void UpdateCoordinates()
        {
            if (_coordsText == null) return;
            
            try
            {
                if (Camera.main == null) return;
                
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                int cell = Grid.PosToCell(mousePos);
                
                if (Grid.IsValidCell(cell))
                {
                    int x = Grid.CellColumn(cell);
                    int y = Grid.CellRow(cell);
                    _coordsText.text = $"📍 Cell: {x}, {y}";
                }
                else
                {
                    _coordsText.text = "📍 Cell: --, --";
                }
            }
            catch
            {
                _coordsText.text = "📍 Cell: ?";
            }
        }
        
        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_overlayObj != null)
            {
                _overlayObj.SetActive(visible);
            }
        }
        
        void OnDestroy()
        {
            if (_overlayObj != null)
            {
                Destroy(_overlayObj);
            }
        }
    }
}
