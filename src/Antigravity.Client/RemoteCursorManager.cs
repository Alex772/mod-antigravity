using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Antigravity.Core.Network;
using Steamworks;

namespace Antigravity.Client
{
    /// <summary>
    /// Manages remote player cursors - shows other players' mouse positions.
    /// </summary>
    public class RemoteCursorManager : MonoBehaviour
    {
        private static RemoteCursorManager _instance;
        public static RemoteCursorManager Instance => _instance;

        // Cursor update interval (50ms = 20 updates per second)
        private const float CURSOR_UPDATE_INTERVAL = 0.05f;
        private float _lastCursorSendTime;

        // Remote cursor visuals
        private Dictionary<ulong, RemoteCursor> _remoteCursors = new Dictionary<ulong, RemoteCursor>();

        // Colors for different players
        private static readonly Color HOST_COLOR = new Color(1f, 0.85f, 0.2f, 0.9f); // Gold
        private static readonly Color CLIENT_COLOR = new Color(0.3f, 0.8f, 1f, 0.9f); // Light blue

        private class RemoteCursor
        {
            public GameObject CursorObject;
            public Text NameText;
            public Image CursorImage;
            public Vector3 TargetPosition;
            public float LastUpdateTime;
        }

        private void Awake()
        {
            _instance = this;
        }

        private void Update()
        {
            if (!MultiplayerState.IsMultiplayerSession || !MultiplayerState.IsGameLoaded) return;

            // Send our cursor position periodically
            if (Time.time - _lastCursorSendTime >= CURSOR_UPDATE_INTERVAL)
            {
                SendCursorPosition();
                _lastCursorSendTime = Time.time;
            }

            // Update remote cursor positions (interpolation)
            foreach (var kvp in _remoteCursors)
            {
                var cursor = kvp.Value;
                if (cursor.CursorObject != null)
                {
                    // Smooth interpolation
                    cursor.CursorObject.transform.position = Vector3.Lerp(
                        cursor.CursorObject.transform.position,
                        cursor.TargetPosition,
                        Time.deltaTime * 15f
                    );

                    // Fade out if no update for a while
                    float timeSinceUpdate = Time.time - cursor.LastUpdateTime;
                    if (timeSinceUpdate > 2f)
                    {
                        float alpha = Mathf.Max(0, 1f - (timeSinceUpdate - 2f));
                        if (cursor.CursorImage != null)
                        {
                            var c = cursor.CursorImage.color;
                            c.a = alpha * 0.9f;
                            cursor.CursorImage.color = c;
                        }
                        if (cursor.NameText != null)
                        {
                            var c = cursor.NameText.color;
                            c.a = alpha;
                            cursor.NameText.color = c;
                        }
                    }
                }
            }
        }

        private void SendCursorPosition()
        {
            if (!SteamNetworkManager.IsConnected) return;

            // Get cursor world position
            Vector3 cursorPos = Camera.main != null ? 
                Camera.main.ScreenToWorldPoint(Input.mousePosition) : 
                Vector3.zero;
            
            int cellId = Grid.IsValidCell(Grid.PosToCell(cursorPos)) ? 
                Grid.PosToCell(cursorPos) : -1;

            var cursorData = new CursorPositionMessage
            {
                SteamId = SteamNetworkManager.LocalSteamId.m_SteamID,
                PlayerName = SteamFriends.GetPersonaName(),
                WorldX = cursorPos.x,
                WorldY = cursorPos.y,
                CellId = cellId
            };

            // Send to all other players
            var netMessage = MessageSerializer.CreateMessage(MessageType.CursorUpdate, cursorData, 
                SteamNetworkManager.LocalSteamId.m_SteamID, 0);
            byte[] data = MessageSerializer.Serialize(netMessage);
            SteamNetworkManager.SendToAll(data, Steamworks.EP2PSend.k_EP2PSendUnreliable); // Unreliable for cursor
        }

        /// <summary>
        /// Handle received cursor position from another player.
        /// </summary>
        public void OnCursorReceived(CursorPositionMessage message)
        {
            if (message == null) return;
            if (message.SteamId == SteamNetworkManager.LocalSteamId.m_SteamID) return; // Ignore own cursor

            // Get or create cursor visual
            if (!_remoteCursors.TryGetValue(message.SteamId, out var cursor))
            {
                cursor = CreateCursorVisual(message.SteamId, message.PlayerName);
                _remoteCursors[message.SteamId] = cursor;
            }

            // Update target position
            cursor.TargetPosition = new Vector3(message.WorldX, message.WorldY, -5f);
            cursor.LastUpdateTime = Time.time;

            // Reset alpha
            if (cursor.CursorImage != null)
            {
                var c = cursor.CursorImage.color;
                c.a = 0.9f;
                cursor.CursorImage.color = c;
            }
            if (cursor.NameText != null)
            {
                var c = cursor.NameText.color;
                c.a = 1f;
                cursor.NameText.color = c;
            }
        }

        private RemoteCursor CreateCursorVisual(ulong steamId, string playerName)
        {
            var cursorObj = new GameObject($"RemoteCursor_{steamId}");
            cursorObj.transform.SetParent(transform);

            // Cursor indicator (diamond shape made with UI)
            var indicator = new GameObject("Indicator");
            indicator.transform.SetParent(cursorObj.transform, false);
            
            var spriteRenderer = indicator.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateCursorSprite();
            spriteRenderer.sortingOrder = 1000;
            
            // Color based on if host or client
            bool isHost = new CSteamID(steamId) == SteamNetworkManager.HostSteamId;
            spriteRenderer.color = isHost ? HOST_COLOR : CLIENT_COLOR;
            indicator.transform.localScale = Vector3.one * 0.5f;

            // Player name text
            var nameObj = new GameObject("NameLabel");
            nameObj.transform.SetParent(cursorObj.transform, false);
            nameObj.transform.localPosition = new Vector3(0, 0.8f, 0);

            // Use TextMesh for world-space text
            var textMesh = nameObj.AddComponent<TextMesh>();
            textMesh.text = playerName;
            textMesh.fontSize = 24;
            textMesh.characterSize = 0.1f;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = isHost ? HOST_COLOR : CLIENT_COLOR;

            var meshRenderer = nameObj.GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = 1001;

            return new RemoteCursor
            {
                CursorObject = cursorObj,
                CursorImage = null, // Using SpriteRenderer instead
                NameText = null, // Using TextMesh instead
                TargetPosition = Vector3.zero,
                LastUpdateTime = Time.time
            };
        }

        private Sprite CreateCursorSprite()
        {
            // Create a simple diamond/pointer texture
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            // Clear
            var clear = new Color(0, 0, 0, 0);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    tex.SetPixel(x, y, clear);

            // Draw diamond shape
            int center = size / 2;
            for (int y = 0; y < size; y++)
            {
                int width = y < center ? y : size - 1 - y;
                for (int x = center - width; x <= center + width; x++)
                {
                    if (x >= 0 && x < size)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// Remove cursor when player leaves.
        /// </summary>
        public void RemovePlayerCursor(ulong steamId)
        {
            if (_remoteCursors.TryGetValue(steamId, out var cursor))
            {
                if (cursor.CursorObject != null)
                {
                    Destroy(cursor.CursorObject);
                }
                _remoteCursors.Remove(steamId);
            }
        }

        private void OnDestroy()
        {
            foreach (var cursor in _remoteCursors.Values)
            {
                if (cursor.CursorObject != null)
                {
                    Destroy(cursor.CursorObject);
                }
            }
            _remoteCursors.Clear();
        }
    }
}
