#if DEBUG
using UnityEngine;
using UnityEngine.UI;
using Antigravity.Core;
using Antigravity.Core.Network;

namespace Antigravity.Client
{
    /// <summary>
    /// Local test lobby for testing multiplayer without Steam on the same PC.
    /// Uses LiteNetLib for direct IP connections.
    /// </summary>
    public class LocalTestLobbyScreen : MonoBehaviour
    {
        private static LocalTestLobbyScreen _instance;
        private static GameObject _screenObject;

        // UI Elements
        private GameObject _panel;
        private Text _statusText;
        private Text _connectionInfoText;
        private Text _playersListText;
        private InputField _portInput;
        private InputField _addressInput;
        private GameObject _hostSection;
        private GameObject _joinSection;
        private GameObject _lobbySection;

        // State
        private bool _inLobby = false;

        /// <summary>
        /// Show the local test lobby screen.
        /// </summary>
        public static void Show()
        {
            if (_instance == null || _screenObject == null)
            {
                if (_screenObject != null)
                {
                    Object.Destroy(_screenObject);
                }
                CreateScreen();
            }

            if (_screenObject != null)
            {
                _screenObject.SetActive(true);
                _instance.RefreshUI();
            }

            MultiplayerUpdater.EnsureExists();
            Debug.Log("[Antigravity] Local test lobby opened");
        }

        /// <summary>
        /// Hide the local test lobby screen.
        /// </summary>
        public static new void Hide()
        {
            if (_screenObject != null)
            {
                _screenObject.SetActive(false);
            }
        }

        private static void CreateScreen()
        {
            _screenObject = new GameObject("LocalTestLobbyScreen");
            Object.DontDestroyOnLoad(_screenObject);

            var canvas = _screenObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = _screenObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _screenObject.AddComponent<GraphicRaycaster>();

            _instance = _screenObject.AddComponent<LocalTestLobbyScreen>();
            _instance.CreateUI();
        }

        private void CreateUI()
        {
            // Background overlay
            var overlay = CreatePanel("Overlay", transform);
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.85f);
            SetFullScreen(overlay);

            // Main panel
            _panel = CreatePanel("MainPanel", overlay.transform);
            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.15f, 0.15f, 1f); // Reddish tint for local mode
            SetCenteredRect(_panel, 600, 500);

            var outline = _panel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.6f, 0.4f, 1f);
            outline.effectDistance = new Vector2(2, 2);

            // Title
            var titleObj = CreateText("Title", _panel.transform, "🔧 LOCAL TEST MODE", 28);
            SetAnchoredPosition(titleObj, 0, 210);
            titleObj.GetComponent<Text>().color = new Color(1f, 0.7f, 0.4f, 1f);

            var subtitleObj = CreateText("Subtitle", _panel.transform, "(No Steam required - for testing on same PC)", 14);
            SetAnchoredPosition(subtitleObj, 0, 175);
            subtitleObj.GetComponent<Text>().color = new Color(0.7f, 0.7f, 0.7f, 1f);

            // === HOST/JOIN Section ===
            _hostSection = CreatePanel("HostJoinSection", _panel.transform);
            SetFullScreen(_hostSection);

            // Port label and input
            var portLabel = CreateText("PortLabel", _hostSection.transform, "Port:", 16);
            SetAnchoredPosition(portLabel, -100, 90);

            _portInput = CreateInputField("PortInput", _hostSection.transform, "7777");
            SetAnchoredPosition(_portInput.gameObject, 50, 90);
            SetSize(_portInput.gameObject, 150, 40);
            _portInput.text = MultiplayerConfig.LocalPort.ToString();

            // Host button
            var hostBtn = CreateButton("HostButton", _hostSection.transform, "🖥️  HOST (Instance 1)", OnHostClick);
            SetAnchoredPosition(hostBtn, 0, 20);
            SetSize(hostBtn, 350, 55);
            hostBtn.GetComponent<Image>().color = new Color(0.4f, 0.6f, 0.3f, 1f);

            // Separator
            var sepText = CreateText("Separator", _hostSection.transform, "─────  OR  ─────", 16);
            SetAnchoredPosition(sepText, 0, -40);
            sepText.GetComponent<Text>().color = new Color(0.5f, 0.5f, 0.5f, 1f);

            // Address input
            var addrLabel = CreateText("AddrLabel", _hostSection.transform, "Address:", 16);
            SetAnchoredPosition(addrLabel, -100, -90);

            _addressInput = CreateInputField("AddressInput", _hostSection.transform, "127.0.0.1:7777");
            SetAnchoredPosition(_addressInput.gameObject, 50, -90);
            SetSize(_addressInput.gameObject, 200, 40);
            _addressInput.text = $"{MultiplayerConfig.LocalAddress}:{MultiplayerConfig.LocalPort}";

            // Join button
            var joinBtn = CreateButton("JoinButton", _hostSection.transform, "🔗  JOIN (Instance 2)", OnJoinClick);
            SetAnchoredPosition(joinBtn, 0, -150);
            SetSize(joinBtn, 350, 55);

            // === LOBBY Section ===
            _lobbySection = CreatePanel("LobbySection", _panel.transform);
            SetFullScreen(_lobbySection);
            _lobbySection.SetActive(false);

            // Connection info
            var connLabel = CreateText("ConnLabel", _lobbySection.transform, "📡 Connection Info:", 18);
            SetAnchoredPosition(connLabel, 0, 100);

            var connObj = CreateText("ConnectionInfo", _lobbySection.transform, "", 22);
            SetAnchoredPosition(connObj, 0, 60);
            _connectionInfoText = connObj.GetComponent<Text>();
            _connectionInfoText.color = new Color(0.5f, 1f, 0.5f, 1f);

            // Players list
            var playersLabel = CreateText("PlayersLabel", _lobbySection.transform, "👥 Connected:", 18);
            SetAnchoredPosition(playersLabel, 0, 10);

            var playersObj = CreateText("PlayersList", _lobbySection.transform, "", 16);
            SetAnchoredPosition(playersObj, 0, -40);
            _playersListText = playersObj.GetComponent<Text>();

            // START button (host only)
            var startBtn = CreateButton("StartButton", _lobbySection.transform, "🚀  START GAME", OnStartGameClick);
            SetAnchoredPosition(startBtn, 0, -110);
            SetSize(startBtn, 250, 50);
            startBtn.GetComponent<Image>().color = new Color(0.3f, 0.7f, 0.3f, 1f);

            // Leave button
            var leaveBtn = CreateButton("LeaveButton", _lobbySection.transform, "❌  DISCONNECT", OnLeaveClick);
            SetAnchoredPosition(leaveBtn, 0, -170);
            SetSize(leaveBtn, 200, 40);
            leaveBtn.GetComponent<Image>().color = new Color(0.6f, 0.25f, 0.25f, 1f);

            // === Status text ===
            var statusObj = CreateText("Status", _panel.transform, "", 14);
            SetAnchoredPosition(statusObj, 0, -210);
            _statusText = statusObj.GetComponent<Text>();
            _statusText.color = Color.yellow;

            // Close button
            var closeBtn = CreateButton("CloseButton", _panel.transform, "✕", OnCloseClick);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-20, -20);
            closeRect.sizeDelta = new Vector2(40, 40);
            closeBtn.GetComponentInChildren<Text>().fontSize = 18;

            // Setup events
            SetupEventHandlers();

            Debug.Log("[Antigravity] Local Test Lobby UI created");
        }

        private void SetupEventHandlers()
        {
            NetworkBackendManager.OnConnected += (s, e) =>
            {
                _inLobby = true;
                RefreshUI();
                SetStatus(NetworkBackendManager.IsHost ? "Hosting! Waiting for connections..." : "Connected!", Color.green);
            };

            NetworkBackendManager.OnDisconnected += (s, e) =>
            {
                _inLobby = false;
                RefreshUI();
                SetStatus("Disconnected", Color.yellow);
            };

            NetworkBackendManager.OnPlayerJoined += (s, e) =>
            {
                SetStatus($"{e.PlayerName} joined!", Color.green);
                RefreshPlayersList();
            };

            NetworkBackendManager.OnPlayerLeft += (s, e) =>
            {
                SetStatus($"Player {e.PlayerId.Value} left", Color.yellow);
                RefreshPlayersList();
            };
        }

        private void OnHostClick()
        {
            Debug.Log("[Antigravity] Local Host button clicked");

            if (int.TryParse(_portInput.text, out int port))
            {
                MultiplayerConfig.LocalPort = port;
            }

            SetStatus("Starting local host...", Color.yellow);

            if (!NetworkBackendManager.InitializeLocal())
            {
                SetStatus("Failed to initialize LiteNetLib!", Color.red);
                return;
            }

            string result = NetworkBackendManager.HostGame(4);
            if (result != null)
            {
                _inLobby = true;
                RefreshUI();
            }
        }

        private void OnJoinClick()
        {
            Debug.Log("[Antigravity] Local Join button clicked");

            string address = _addressInput.text.Trim();
            if (string.IsNullOrEmpty(address))
            {
                SetStatus("Please enter an address", Color.red);
                return;
            }

            SetStatus($"Connecting to {address}...", Color.yellow);

            if (!NetworkBackendManager.InitializeLocal())
            {
                SetStatus("Failed to initialize LiteNetLib!", Color.red);
                return;
            }

            NetworkBackendManager.JoinGame(address);
        }

        private void OnLeaveClick()
        {
            NetworkBackendManager.Disconnect();
        }

        private void OnStartGameClick()
        {
            Debug.Log("[Antigravity] Local START GAME clicked");

            if (!NetworkBackendManager.IsHost)
            {
                SetStatus("Only the host can start the game!", Color.red);
                return;
            }

            // Show game choice dialog
            ShowGameChoiceDialog();
        }

        private GameObject _gameChoicePanel;

        private void ShowGameChoiceDialog()
        {
            // Create choice panel if it doesn't exist
            if (_gameChoicePanel == null)
            {
                _gameChoicePanel = CreatePanel("GameChoicePanel", _panel.transform);
                var panelImg = _gameChoicePanel.AddComponent<Image>();
                panelImg.color = new Color(0.15f, 0.15f, 0.2f, 0.98f);
                SetCenteredRect(_gameChoicePanel, 350, 200);
                _gameChoicePanel.AddComponent<Outline>().effectColor = new Color(0.7f, 0.5f, 0.3f, 1f);

                // Title
                var titleObj = CreateText("ChoiceTitle", _gameChoicePanel.transform, "🎮 Start Game", 22);
                SetAnchoredPosition(titleObj, 0, 60);
                titleObj.GetComponent<Text>().fontStyle = FontStyle.Bold;

                // New Game button
                var newGameBtn = CreateButton("NewGameBtn", _gameChoicePanel.transform, "🌍  New World", OnNewGameClick);
                SetAnchoredPosition(newGameBtn, 0, 10);
                SetSize(newGameBtn, 200, 45);
                newGameBtn.GetComponent<Image>().color = new Color(0.3f, 0.6f, 0.3f, 1f);

                // Load Save button
                var loadGameBtn = CreateButton("LoadGameBtn", _gameChoicePanel.transform, "📂  Load Save", OnLoadGameClick);
                SetAnchoredPosition(loadGameBtn, 0, -45);
                SetSize(loadGameBtn, 200, 45);
                loadGameBtn.GetComponent<Image>().color = new Color(0.3f, 0.4f, 0.6f, 1f);

                // Cancel button
                var cancelBtn = CreateButton("CancelBtn", _gameChoicePanel.transform, "✕", () => _gameChoicePanel.SetActive(false));
                var cancelRect = cancelBtn.GetComponent<RectTransform>();
                cancelRect.anchorMin = new Vector2(1, 1);
                cancelRect.anchorMax = new Vector2(1, 1);
                cancelRect.anchoredPosition = new Vector2(-10, -10);
                cancelRect.sizeDelta = new Vector2(30, 30);
            }

            _gameChoicePanel.SetActive(true);
        }

        private void OnNewGameClick()
        {
            Debug.Log("[Antigravity] New Game selected");
            
            // Mark multiplayer state
            MultiplayerState.IsMultiplayerSession = true;
            MultiplayerState.IsHost = true;

            if (_gameChoicePanel != null)
                _gameChoicePanel.SetActive(false);

            SetStatus("Starting new game...", Color.green);
            Hide();

            // Trigger main menu new game
            if (MainMenu.Instance != null)
            {
                var newGameMethod = typeof(MainMenu).GetMethod("NewGame",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                newGameMethod?.Invoke(MainMenu.Instance, null);
            }
        }

        private void OnLoadGameClick()
        {
            Debug.Log("[Antigravity] Load Game selected");
            
            // Mark multiplayer state
            MultiplayerState.IsMultiplayerSession = true;
            MultiplayerState.IsHost = true;

            if (_gameChoicePanel != null)
                _gameChoicePanel.SetActive(false);

            SetStatus("Opening load screen...", Color.green);
            Hide();

            // Trigger main menu load game
            if (MainMenu.Instance != null)
            {
                var loadGameMethod = typeof(MainMenu).GetMethod("LoadGame",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                loadGameMethod?.Invoke(MainMenu.Instance, null);
            }
        }

        private void OnCloseClick()
        {
            if (_inLobby)
            {
                NetworkBackendManager.Disconnect();
            }
            Hide();
        }

        private void RefreshUI()
        {
            if (_hostSection != null)
                _hostSection.SetActive(!_inLobby);

            if (_lobbySection != null)
            {
                _lobbySection.SetActive(_inLobby);

                if (_inLobby && _connectionInfoText != null)
                {
                    var backend = NetworkBackendManager.Active as LiteNetLibBackend;
                    if (backend != null)
                    {
                        _connectionInfoText.text = $"127.0.0.1:{backend.GetPort()}";
                    }
                    RefreshPlayersList();
                }
            }
        }

        private void RefreshPlayersList()
        {
            if (_playersListText == null) return;

            var players = new System.Text.StringBuilder();
            int count = 0;

            if (NetworkBackendManager.IsConnected)
            {
                players.AppendLine(NetworkBackendManager.IsHost 
                    ? "👑 You (Host)" 
                    : "👤 You (Client)");
                count++;

                foreach (var player in NetworkBackendManager.Active.ConnectedPlayers)
                {
                    string name = NetworkBackendManager.GetPlayerName(player);
                    players.AppendLine($"👤 {name}");
                    count++;
                }
            }

            _playersListText.text = $"({count} players)\n{players}";
        }

        private void SetStatus(string message, Color color)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
                _statusText.color = color;
            }
        }

        private void Update()
        {
            if (NetworkBackendManager.IsConnected)
            {
                NetworkBackendManager.Update();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnCloseClick();
            }
        }

        private void OnDestroy()
        {
            // Events are cleaned up automatically
        }

        #region UI Helpers

        private GameObject CreatePanel(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private GameObject CreateText(string name, Transform parent, string text, int fontSize)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 40);

            var textComp = obj.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComp.fontSize = fontSize;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;

            return obj;
        }

        private GameObject CreateButton(string name, Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);

            var image = obj.AddComponent<Image>();
            image.color = new Color(0.25f, 0.45f, 0.75f, 1f);

            var button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var colors = button.colors;
            colors.normalColor = new Color(0.25f, 0.45f, 0.75f, 1f);
            colors.highlightedColor = new Color(0.35f, 0.55f, 0.85f, 1f);
            colors.pressedColor = new Color(0.2f, 0.35f, 0.65f, 1f);
            button.colors = colors;

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

        private InputField CreateInputField(string name, Transform parent, string placeholder)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 40);

            var image = obj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f, 1f);

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
            phText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            phText.alignment = TextAnchor.MiddleCenter;

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

        private void SetFullScreen(GameObject obj)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }

        private void SetCenteredRect(GameObject obj, float width, float height)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private void SetAnchoredPosition(GameObject obj, float x, float y)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, y);
        }

        private void SetSize(GameObject obj, float width, float height)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
        }

        #endregion
    }
}
#endif
