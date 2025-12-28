using UnityEngine;
using UnityEngine.UI;
using Antigravity.Core.Network;
using Steamworks;

namespace Antigravity.Client
{
    /// <summary>
    /// The multiplayer lobby screen for hosting or joining games via Steam.
    /// </summary>
    public class MultiplayerLobbyScreen : MonoBehaviour
    {
        private static MultiplayerLobbyScreen _instance;
        private static GameObject _screenObject;

        // UI Elements
        private GameObject _panel;
        private Text _titleText;
        private Text _statusText;
        private InputField _codeInput;
        private Text _lobbyCodeText;
        private Text _playersListText;
        private Text _playersCountText;
        private GameObject _hostSection;
        private GameObject _joinSection;
        private GameObject _lobbySection;
        private GameObject _startGameButton;
        private GameObject _gameSelectionSection;
        private Text _waitingText;

        // State
        private bool _inLobby = false;
        private bool _isSelectingGame = false;

        /// <summary>
        /// Show the multiplayer lobby screen.
        /// </summary>
        public static void Show()
        {
            // If screen exists but was destroyed, recreate it
            if (_instance == null || _screenObject == null)
            {
                // Clean up any leftover references
                if (_screenObject != null)
                {
                    Object.Destroy(_screenObject);
                }
                _screenObject = null;
                _instance = null;
                
                CreateScreen();
            }
            
            if (_screenObject != null)
            {
                _screenObject.SetActive(true);
                
                // Reset state when showing
                _instance._isSelectingGame = false;
                _instance.RefreshUI();
            }

            // Ensure the updater is running
            MultiplayerUpdater.EnsureExists();

            Debug.Log("[Antigravity] Multiplayer lobby opened");
        }

        /// <summary>
        /// Hide the multiplayer lobby screen.
        /// </summary>
        public static new void Hide()
        {
            if (_screenObject != null)
            {
                _screenObject.SetActive(false);
            }
        }

        /// <summary>
        /// Update status text from external code.
        /// </summary>
        public static void UpdateStatus(string message, Color color)
        {
            if (_instance != null)
            {
                _instance.SetStatus(message, color);
            }
        }

        private static void CreateScreen()
        {
            _screenObject = new GameObject("MultiplayerLobbyScreen");
            Object.DontDestroyOnLoad(_screenObject);

            var canvas = _screenObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = _screenObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _screenObject.AddComponent<GraphicRaycaster>();

            _instance = _screenObject.AddComponent<MultiplayerLobbyScreen>();
            _instance.CreateUI();
            _instance.SetupEventHandlers();
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
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            SetCenteredRect(_panel, 650, 550);

            var outline = _panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.6f, 1f, 1f);
            outline.effectDistance = new Vector2(2, 2);

            // Title
            var titleObj = CreateText("Title", _panel.transform, "MULTIPLAYER (Steam P2P)", 28);
            SetAnchoredPosition(titleObj, 0, 230);
            _titleText = titleObj.GetComponent<Text>();
            _titleText.color = new Color(0.4f, 0.8f, 1f, 1f);

            // === HOST/JOIN Section (initial view) ===
            _hostSection = CreatePanel("HostJoinSection", _panel.transform);
            SetFullScreen(_hostSection);

            // Host button
            var hostBtn = CreateButton("HostButton", _hostSection.transform, "🎮  HOST GAME (Steam)", OnHostClick);
            SetAnchoredPosition(hostBtn, 0, 120);
            SetSize(hostBtn, 350, 55);

            // Separator
            var sepText = CreateText("Separator", _hostSection.transform, "─────  OR  ─────", 16);
            SetAnchoredPosition(sepText, 0, 50);
            sepText.GetComponent<Text>().color = new Color(0.5f, 0.5f, 0.5f, 1f);

            // Join section label
            var joinLabel = CreateText("JoinLabel", _hostSection.transform, "Join using a Steam Lobby Code:", 16);
            SetAnchoredPosition(joinLabel, 0, 0);

            // Code input
            _codeInput = CreateInputField("CodeInput", _hostSection.transform, "Enter lobby code (e.g. 123456789)");
            SetAnchoredPosition(_codeInput.gameObject, 0, -50);
            SetSize(_codeInput.gameObject, 350, 45);

            // Join button
            var joinBtn = CreateButton("JoinButton", _hostSection.transform, "🔗  JOIN GAME", OnJoinClick);
            SetAnchoredPosition(joinBtn, 0, -120);
            SetSize(joinBtn, 350, 55);

            // === LOBBY Section (when in lobby) ===
            _lobbySection = CreatePanel("LobbySection", _panel.transform);
            SetFullScreen(_lobbySection);
            _lobbySection.SetActive(false);

            // Lobby code display
            var codeLabel = CreateText("CodeLabel", _lobbySection.transform, "📋 Share this code with friends:", 18);
            SetAnchoredPosition(codeLabel, 0, 140);

            var codeObj = CreateText("LobbyCode", _lobbySection.transform, "", 28);
            SetAnchoredPosition(codeObj, 0, 95);
            _lobbyCodeText = codeObj.GetComponent<Text>();
            _lobbyCodeText.color = new Color(0.5f, 1f, 0.5f, 1f);

            // COPY CODE BUTTON
            var copyBtn = CreateButton("CopyCodeButton", _lobbySection.transform, "📋  COPY CODE", OnCopyCodeClick);
            SetAnchoredPosition(copyBtn, 0, 40);
            SetSize(copyBtn, 200, 40);
            var copyBtnImg = copyBtn.GetComponent<Image>();
            copyBtnImg.color = new Color(0.3f, 0.6f, 0.3f, 1f);

            // === Players Section with background panel ===
            var playersPanel = CreatePanel("PlayersPanel", _lobbySection.transform);
            var playersPanelImg = playersPanel.AddComponent<Image>();
            playersPanelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
            SetAnchoredPosition(playersPanel, 0, -40);
            SetSize(playersPanel, 400, 100);

            // Players count header
            var playersLabel = CreateText("PlayersLabel", playersPanel.transform, "👥 Players (0/4)", 18);
            var playersLabelRect = playersLabel.GetComponent<RectTransform>();
            playersLabelRect.anchorMin = new Vector2(0.5f, 1);
            playersLabelRect.anchorMax = new Vector2(0.5f, 1);
            playersLabelRect.anchoredPosition = new Vector2(0, -15);
            playersLabel.GetComponent<Text>().color = new Color(0.8f, 0.85f, 1f, 1f);
            _playersCountText = playersLabel.GetComponent<Text>();

            // Players list scroll area
            var playersObj = CreateText("PlayersList", playersPanel.transform, "", 16);
            var playersRect = playersObj.GetComponent<RectTransform>();
            playersRect.anchorMin = new Vector2(0, 0);
            playersRect.anchorMax = new Vector2(1, 1);
            playersRect.offsetMin = new Vector2(20, 10);
            playersRect.offsetMax = new Vector2(-20, -35);
            _playersListText = playersObj.GetComponent<Text>();
            _playersListText.alignment = TextAnchor.UpperLeft;

            // START GAME button (Host only - will be hidden for clients)
            _startGameButton = CreateButton("StartGameButton", _lobbySection.transform, "🚀  START GAME", OnStartGameClick);
            SetAnchoredPosition(_startGameButton, 0, -130);
            SetSize(_startGameButton, 280, 50);
            var startBtnImg = _startGameButton.GetComponent<Image>();
            startBtnImg.color = new Color(0.2f, 0.7f, 0.3f, 1f);
            _startGameButton.SetActive(false); // Will be shown only for host

            // Leave button
            var leaveBtn = CreateButton("LeaveButton", _lobbySection.transform, "❌  LEAVE LOBBY", OnLeaveClick);
            SetAnchoredPosition(leaveBtn, 0, -190);
            SetSize(leaveBtn, 200, 40);
            var leaveBtnImg = leaveBtn.GetComponent<Image>();
            leaveBtnImg.color = new Color(0.6f, 0.25f, 0.25f, 1f);

            // === Common elements ===

            // Status text
            var statusObj = CreateText("Status", _panel.transform, "", 14);
            SetAnchoredPosition(statusObj, 0, -200);
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

            // Steam info
            var steamInfo = CreateText("SteamInfo", _panel.transform, "✓ Connected via Steam (no IP needed)", 12);
            SetAnchoredPosition(steamInfo, 0, -240);
            steamInfo.GetComponent<Text>().color = new Color(0.4f, 0.8f, 0.4f, 1f);

            // === GAME SELECTION Section (shown when host clicks START) ===
            _gameSelectionSection = CreatePanel("GameSelectionSection", _panel.transform);
            SetFullScreen(_gameSelectionSection);
            _gameSelectionSection.SetActive(false);

            var selectLabel = CreateText("SelectLabel", _gameSelectionSection.transform, "Choose how to start:", 20);
            SetAnchoredPosition(selectLabel, 0, 120);

            // New Game button
            var newGameBtn = CreateButton("NewGameButton", _gameSelectionSection.transform, "🌍  NEW COLONY", OnNewGameClick);
            SetAnchoredPosition(newGameBtn, 0, 50);
            SetSize(newGameBtn, 300, 55);
            var newGameImg = newGameBtn.GetComponent<Image>();
            newGameImg.color = new Color(0.3f, 0.5f, 0.7f, 1f);

            // Load Save button
            var loadSaveBtn = CreateButton("LoadSaveButton", _gameSelectionSection.transform, "📂  LOAD SAVE", OnLoadSaveClick);
            SetAnchoredPosition(loadSaveBtn, 0, -20);
            SetSize(loadSaveBtn, 300, 55);
            var loadSaveImg = loadSaveBtn.GetComponent<Image>();
            loadSaveImg.color = new Color(0.5f, 0.4f, 0.6f, 1f);

            // Back button
            var backBtn = CreateButton("BackButton", _gameSelectionSection.transform, "← Back to Lobby", OnBackToLobbyClick);
            SetAnchoredPosition(backBtn, 0, -100);
            SetSize(backBtn, 200, 40);
            backBtn.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 1f);
            backBtn.GetComponentInChildren<Text>().fontSize = 14;

            // Waiting text (for clients)
            var waitingObj = CreateText("WaitingText", _panel.transform, "", 16);
            SetAnchoredPosition(waitingObj, 0, 0);
            _waitingText = waitingObj.GetComponent<Text>();
            _waitingText.color = new Color(0.8f, 0.8f, 0.3f, 1f);
            waitingObj.SetActive(false);

            Debug.Log("[Antigravity] Steam Lobby UI created");
        }

        private void SetupEventHandlers()
        {
            SteamNetworkManager.OnConnected += OnConnectedToLobby;
            SteamNetworkManager.OnDisconnected += OnDisconnectedFromLobby;
            SteamNetworkManager.OnPlayerJoined += OnPlayerJoinedLobby;
            SteamNetworkManager.OnPlayerLeft += OnPlayerLeftLobby;
        }

        private void OnHostClick()
        {
            Debug.Log("[Antigravity] Host button clicked");
            SetStatus("Creating Steam lobby...", Color.yellow);

            if (!SteamNetworkManager.Initialize())
            {
                SetStatus("Failed to initialize Steam!", Color.red);
                return;
            }

            SteamNetworkManager.HostGame(4);
        }

        private void OnJoinClick()
        {
            Debug.Log("[Antigravity] Join button clicked");

            string code = _codeInput.text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("Please enter a lobby code", Color.red);
                return;
            }

            SetStatus("Joining lobby...", Color.yellow);

            if (!SteamNetworkManager.Initialize())
            {
                SetStatus("Failed to initialize Steam!", Color.red);
                return;
            }

            SteamNetworkManager.JoinByCode(code);
        }

        private void OnLeaveClick()
        {
            SteamNetworkManager.Disconnect();
        }

        private void OnCopyCodeClick()
        {
            string code = SteamNetworkManager.GetLobbyCode();
            if (!string.IsNullOrEmpty(code))
            {
                // Copy to clipboard
                GUIUtility.systemCopyBuffer = code;
                SetStatus("✓ Code copied to clipboard!", new Color(0.5f, 1f, 0.5f, 1f));
                Debug.Log($"[Antigravity] Lobby code copied: {code}");
            }
            else
            {
                SetStatus("No code to copy", Color.yellow);
            }
        }

        private void OnStartGameClick()
        {
            Debug.Log("[Antigravity] START GAME clicked");
            
            if (!SteamNetworkManager.IsHost)
            {
                SetStatus("Only the host can start the game!", Color.red);
                return;
            }

            // Show game selection
            _isSelectingGame = true;
            RefreshUI();
        }

        private void OnNewGameClick()
        {
            Debug.Log("[Antigravity] NEW COLONY selected");
            SetStatus("Opening new game setup...", Color.yellow);

            // Mark that we're in multiplayer mode
            MultiplayerState.IsMultiplayerSession = true;
            MultiplayerState.IsHost = SteamNetworkManager.IsHost;

            // Notify clients that host is setting up a new game
            if (SteamNetworkManager.IsHost)
            {
                NotifyClientsGameStarting(false);
            }

            // Hide the multiplayer screen but keep Steam connection
            Hide();

            // Trigger ONI's new game flow
            try
            {
                // Try to access MainMenu and trigger NewGame
                if (MainMenu.Instance != null)
                {
                    // Use reflection to call the NewGame method or press the button
                    var newGameMethod = typeof(MainMenu).GetMethod("NewGame", 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.NonPublic);
                    
                    if (newGameMethod != null)
                    {
                        newGameMethod.Invoke(MainMenu.Instance, null);
                        Debug.Log("[Antigravity] NewGame triggered via reflection");
                    }
                    else
                    {
                        Debug.LogWarning("[Antigravity] NewGame method not found, trying alternative...");
                        // Alternative: simulate clicking the New Game button
                        TriggerNewGameViaUI();
                    }
                }
                else
                {
                    Debug.LogError("[Antigravity] MainMenu.Instance is null!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to trigger NewGame: {ex.Message}");
                SetStatus("Failed to open new game. Try from main menu.", Color.red);
            }
        }

        private void OnLoadSaveClick()
        {
            Debug.Log("[Antigravity] LOAD SAVE selected");
            SetStatus("Opening save selection...", Color.yellow);

            // Mark that we're in multiplayer mode
            MultiplayerState.IsMultiplayerSession = true;
            MultiplayerState.IsHost = SteamNetworkManager.IsHost;

            // Notify clients that host is selecting a save
            if (SteamNetworkManager.IsHost)
            {
                NotifyClientsGameStarting(true);
            }

            // Hide the multiplayer screen but keep Steam connection
            Hide();

            // Trigger ONI's load game flow
            try
            {
                // Try to access the load screen
                if (MainMenu.Instance != null)
                {
                    var loadGameMethod = typeof(MainMenu).GetMethod("LoadGame", 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.NonPublic);
                    
                    if (loadGameMethod != null)
                    {
                        loadGameMethod.Invoke(MainMenu.Instance, null);
                        Debug.Log("[Antigravity] LoadGame triggered via reflection");
                    }
                    else
                    {
                        Debug.LogWarning("[Antigravity] LoadGame method not found, trying alternative...");
                        TriggerLoadGameViaUI();
                    }
                }
                else
                {
                    Debug.LogError("[Antigravity] MainMenu.Instance is null!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Antigravity] Failed to trigger LoadGame: {ex.Message}");
                SetStatus("Failed to open load game. Try from main menu.", Color.red);
            }
        }

        private void TriggerNewGameViaUI()
        {
            // Alternative method: find and click the New Game button
            Debug.Log("[Antigravity] Attempting to trigger NewGame via UI...");
            
            // For now, just log - the user can click manually
            // The multiplayer state is set, so when game loads it will sync
            Debug.Log("[Antigravity] Please click 'New Game' in the main menu. Multiplayer sync is ready.");
        }

        private void TriggerLoadGameViaUI()
        {
            // Alternative method: find and click the Load Game button
            Debug.Log("[Antigravity] Attempting to trigger LoadGame via UI...");
            
            // For now, just log - the user can click manually
            // The multiplayer state is set, so when game loads it will sync
            Debug.Log("[Antigravity] Please click 'Load Game' in the main menu. Multiplayer sync is ready.");
        }

        private void NotifyClientsGameStarting(bool isLoadingSave)
        {
            // TODO: Send message to clients that host is starting game setup
            Debug.Log($"[Antigravity] Notifying clients: game starting (isLoadingSave={isLoadingSave})");
        }

        private void OnBackToLobbyClick()
        {
            Debug.Log("[Antigravity] Back to lobby clicked");
            _isSelectingGame = false;
            RefreshUI();
        }

        private void OnCloseClick()
        {
            if (_inLobby)
            {
                SteamNetworkManager.Disconnect();
            }
            Hide();
        }

        private void OnConnectedToLobby()
        {
            _inLobby = true;
            RefreshUI();
            SetStatus(SteamNetworkManager.IsHost ? "Lobby created! Waiting for players..." : "Connected to lobby!", Color.green);
        }

        private void OnDisconnectedFromLobby()
        {
            _inLobby = false;
            RefreshUI();
            SetStatus("Disconnected from lobby", Color.yellow);
        }

        private void OnPlayerJoinedLobby(CSteamID playerId)
        {
            string name = SteamNetworkManager.GetPlayerName(playerId);
            SetStatus($"{name} joined the lobby!", Color.green);
            RefreshPlayersList();
        }

        private void OnPlayerLeftLobby(CSteamID playerId)
        {
            string name = SteamNetworkManager.GetPlayerName(playerId);
            SetStatus($"{name} left the lobby", Color.yellow);
            RefreshPlayersList();
        }

        private void RefreshUI()
        {
            // Host/Join section (before lobby)
            if (_hostSection != null)
                _hostSection.SetActive(!_inLobby && !_isSelectingGame);
            
            // Lobby section (after joining/hosting)
            if (_lobbySection != null)
            {
                _lobbySection.SetActive(_inLobby && !_isSelectingGame);
                
                if (_inLobby && !_isSelectingGame)
                {
                    _lobbyCodeText.text = SteamNetworkManager.GetLobbyCode();
                    RefreshPlayersList();
                }
            }

            // Game selection section (host only, after clicking START)
            if (_gameSelectionSection != null)
            {
                _gameSelectionSection.SetActive(_isSelectingGame);
            }

            // START GAME button (only for host)
            if (_startGameButton != null)
            {
                _startGameButton.SetActive(_inLobby && !_isSelectingGame && SteamNetworkManager.IsHost);
            }

            // Title text
            if (_titleText != null)
            {
                if (_isSelectingGame)
                {
                    _titleText.text = "SELECT GAME MODE";
                }
                else if (_inLobby)
                {
                    _titleText.text = SteamNetworkManager.IsHost ? "HOSTING GAME" : "IN LOBBY";
                }
                else
                {
                    _titleText.text = "MULTIPLAYER (Steam P2P)";
                }
            }

            // Waiting text for clients
            if (_waitingText != null)
            {
                bool showWaiting = _inLobby && !SteamNetworkManager.IsHost && !_isSelectingGame;
                _waitingText.gameObject.SetActive(showWaiting);
                if (showWaiting)
                {
                    _waitingText.text = "⏳ Waiting for host to start the game...";
                }
            }
        }

        private void RefreshPlayersList()
        {
            if (_playersListText == null) return;

            var players = new System.Text.StringBuilder();
            int playerCount = 0;

            // Add host
            if (SteamNetworkManager.IsConnected)
            {
                string hostName = SteamNetworkManager.GetPlayerName(SteamNetworkManager.HostSteamId);
                bool isMe = SteamNetworkManager.HostSteamId == SteamNetworkManager.LocalSteamId;
                players.AppendLine($"👑 {hostName}{(isMe ? " (You)" : "")} <color=#FFD700>[HOST]</color>");
                playerCount++;
            }

            // Add other players
            foreach (var player in SteamNetworkManager.ConnectedPlayers)
            {
                if (player != SteamNetworkManager.HostSteamId)
                {
                    string name = SteamNetworkManager.GetPlayerName(player);
                    bool isMe = player == SteamNetworkManager.LocalSteamId;
                    players.AppendLine($"👤 {name}{(isMe ? " (You)" : "")}");
                    playerCount++;
                }
            }

            _playersListText.text = players.ToString();
            _playersListText.supportRichText = true;

            // Update player count header
            if (_playersCountText != null)
            {
                _playersCountText.text = $"👥 Players ({playerCount}/4)";
            }
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
            // Poll Steam network events
            if (SteamNetworkManager.IsConnected)
            {
                SteamNetworkManager.Update();
            }

            // Close on Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnCloseClick();
            }
        }

        private void OnDestroy()
        {
            SteamNetworkManager.OnConnected -= OnConnectedToLobby;
            SteamNetworkManager.OnDisconnected -= OnDisconnectedFromLobby;
            SteamNetworkManager.OnPlayerJoined -= OnPlayerJoinedLobby;
            SteamNetworkManager.OnPlayerLeft -= OnPlayerLeftLobby;
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
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
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
