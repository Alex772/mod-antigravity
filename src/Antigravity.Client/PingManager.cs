using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Antigravity.Core.Commands;
using Antigravity.Core.Network;

namespace Antigravity.Client
{
    /// <summary>
    /// Manages multiplayer ping system.
    /// Ctrl+Click to ping, shows visual marker and clickable notification.
    /// </summary>
    public class PingManager : MonoBehaviour
    {
        private static PingManager _instance;
        public static PingManager Instance => _instance;
        
        // Active pings on screen
        private List<PingMarker> _activeMarkers = new List<PingMarker>();
        
        // Ping settings
        private const float PING_DURATION = 5f;
        private const float PING_MARKER_SIZE = 40f;
        
        // Colors for different players
        private static readonly Color[] PlayerColors = new Color[]
        {
            new Color(1f, 0.3f, 0.3f),    // Red
            new Color(0.3f, 0.8f, 1f),    // Cyan
            new Color(1f, 0.8f, 0.2f),    // Yellow
            new Color(0.5f, 1f, 0.5f),    // Green
            new Color(1f, 0.5f, 1f),      // Pink
        };
        
        void Start()
        {
            _instance = this;
            Debug.Log("[Antigravity] PingManager started");
        }
        
        void Update()
        {
            // Only process in multiplayer and when game is loaded
            if (!MultiplayerState.IsMultiplayerSession) return;
            if (!MultiplayerState.IsGameLoaded) return;
            
            // Detect Ctrl+Click
            if (Input.GetMouseButtonDown(0) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                SendPing();
            }
            
            // Update active markers (fade out)
            UpdateMarkers();
        }
        
        private void SendPing()
        {
            if (Camera.main == null) return;
            
            try
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                int cell = Grid.PosToCell(mousePos);
                
                if (!Grid.IsValidCell(cell)) return;
                
                string playerName = MultiplayerState.IsHost ? "Host" : "Client";
                
                var cmd = new PingCommand
                {
                    Cell = cell,
                    PlayerName = playerName,
                    X = mousePos.x,
                    Y = mousePos.y
                };
                
                CommandManager.SendCommand(cmd);
                
                // Also show locally
                ShowPing(cmd);
                
                Debug.Log($"[Antigravity] Ping sent at cell {cell} ({mousePos.x:F1}, {mousePos.y:F1})");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Antigravity] Failed to send ping: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Show a ping on the screen (called when receiving ping from other player)
        /// </summary>
        public void ShowPing(PingCommand cmd)
        {
            if (cmd == null) return;
            
            // Create visual marker
            var marker = CreatePingMarker(cmd);
            _activeMarkers.Add(marker);
            
            // Create notification
            CreatePingNotification(cmd);
        }
        
        private PingMarker CreatePingMarker(PingCommand cmd)
        {
            // Get canvas
            var canvas = GetCanvas();
            if (canvas == null) return null;
            
            // Create marker object
            var markerGO = new GameObject($"PingMarker_{cmd.Cell}");
            markerGO.transform.SetParent(canvas.transform, false);
            
            var rect = markerGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(PING_MARKER_SIZE, PING_MARKER_SIZE);
            
            // Background circle
            var image = markerGO.AddComponent<Image>();
            image.color = GetPlayerColor(cmd.PlayerName);
            image.sprite = CreateCircleSprite();
            
            // Add pulsing animation component
            var marker = markerGO.AddComponent<PingMarker>();
            marker.Initialize(cmd, PING_DURATION);
            
            return marker;
        }
        
        private void CreatePingNotification(PingCommand cmd)
        {
            try
            {
                int x = Grid.CellColumn(cmd.Cell);
                int y = Grid.CellRow(cmd.Cell);
                
                // Show in chat as well
                ChatManager.SendSystemMessage($"📍 {cmd.PlayerName} pinged at ({x}, {y})");
                
                // Store ping position for the callback
                Vector3 pingPos = new Vector3(cmd.X, cmd.Y, 0);
                
                // Create a game notification that can be clicked
                if (NotificationManager.Instance != null)
                {
                    var notification = new Notification(
                        $"📍 {cmd.PlayerName} pinged here",
                        NotificationType.Event,
                        (notifications, data) => $"Click to go to ping location ({x}, {y})",
                        null,
                        expires: true,
                        delay: 0f,
                        custom_click_callback: (data) => {
                            // Move camera to ping location
                            if (CameraController.Instance != null)
                            {
                                CameraController.Instance.SetTargetPos(pingPos, 8f, true);
                            }
                        },
                        custom_click_data: null,
                        click_focus: null,
                        volume_attenuation: true,
                        clear_on_click: true,
                        show_dismiss_button: true
                    );
                    
                    NotificationManager.Instance.AddNotification(notification);
                    Debug.Log($"[Antigravity] Created clickable notification for ping at ({x}, {y})");
                }
                
                // Store last ping for "go to ping" functionality
                _lastPingPosition = pingPos;
                _lastPingCell = cmd.Cell;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Antigravity] Failed to create ping notification: {ex.Message}");
            }
        }
        
        // Last ping position for navigation
        private Vector3 _lastPingPosition;
        private int _lastPingCell;
        
        /// <summary>
        /// Navigate camera to the last ping location
        /// </summary>
        public void GoToLastPing()
        {
            if (_lastPingCell > 0 && Grid.IsValidCell(_lastPingCell))
            {
                CameraController.Instance?.SetTargetPos(_lastPingPosition, 8f, true);
            }
        }
        
        private void UpdateMarkers()
        {
            // Update marker world positions and remove expired ones
            for (int i = _activeMarkers.Count - 1; i >= 0; i--)
            {
                var marker = _activeMarkers[i];
                if (marker == null || marker.IsExpired)
                {
                    if (marker != null) Destroy(marker.gameObject);
                    _activeMarkers.RemoveAt(i);
                }
                else
                {
                    marker.UpdatePosition();
                }
            }
        }
        
        private Canvas GetCanvas()
        {
            var existingCanvas = GameObject.Find("AntigravityChatCanvas");
            if (existingCanvas != null)
            {
                return existingCanvas.GetComponent<Canvas>();
            }
            return null;
        }
        
        private Color GetPlayerColor(string playerName)
        {
            int hash = playerName?.GetHashCode() ?? 0;
            return PlayerColors[Mathf.Abs(hash) % PlayerColors.Length];
        }
        
        private Sprite CreateCircleSprite()
        {
            // Create a simple circle texture
            int size = 64;
            var tex = new Texture2D(size, size);
            float center = size / 2f;
            float radius = size / 2f - 2;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist < radius - 2)
                    {
                        tex.SetPixel(x, y, new Color(1, 1, 1, 0.6f));
                    }
                    else if (dist < radius)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
    
    /// <summary>
    /// Visual ping marker component - handles position update and fading
    /// </summary>
    public class PingMarker : MonoBehaviour
    {
        private PingCommand _cmd;
        private float _duration;
        private float _timeRemaining;
        private Image _image;
        private RectTransform _rect;
        private Color _baseColor;
        
        public bool IsExpired => _timeRemaining <= 0;
        
        public void Initialize(PingCommand cmd, float duration)
        {
            _cmd = cmd;
            _duration = duration;
            _timeRemaining = duration;
            _image = GetComponent<Image>();
            _rect = GetComponent<RectTransform>();
            _baseColor = _image.color;
            
            UpdatePosition();
        }
        
        void Update()
        {
            _timeRemaining -= Time.deltaTime;
            
            // Fade out effect
            float alpha = Mathf.Clamp01(_timeRemaining / (_duration * 0.3f));
            _image.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha * _baseColor.a);
            
            // Pulsing effect
            float pulse = 1f + 0.2f * Mathf.Sin(Time.time * 5f);
            transform.localScale = Vector3.one * pulse;
        }
        
        public void UpdatePosition()
        {
            if (Camera.main == null || _cmd == null) return;
            
            Vector3 worldPos = new Vector3(_cmd.X, _cmd.Y, 0);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            
            // Convert to canvas position
            _rect.position = screenPos;
        }
    }
}
