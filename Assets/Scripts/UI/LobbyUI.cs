using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using MimicFacility.Networking;

namespace MimicFacility.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI playerCountText;

        [Header("Player List")]
        [SerializeField] private Transform playerListContent;
        [SerializeField] private GameObject playerEntryTemplate;

        [Header("Chat")]
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private ScrollRect chatScroll;
        [SerializeField] private Transform chatContent;
        [SerializeField] private GameObject chatMessageTemplate;

        [Header("Controls")]
        [SerializeField] private TMP_InputField displayNameInput;
        [SerializeField] private Button readyButton;
        [SerializeField] private TextMeshProUGUI readyButtonLabel;
        [SerializeField] private GameObject hostStartButton;
        [SerializeField] private Button hostStartButtonComponent;
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Visual References")]
        [SerializeField] private Light[] readyGlowLights;
        [SerializeField] private Renderer[] readyLightRenderers;

        private const int MaxPlayers = 12;

        private LobbyManager _lobby;
        private readonly List<GameObject> _playerEntries = new List<GameObject>();
        private bool _isReady;
        private Coroutine _countdownCoroutine;

        private static readonly Color ReadyOnColor = new Color(0.1f, 0.7f, 0.15f);
        private static readonly Color ReadyOffColor = new Color(0.6f, 0.08f, 0.08f);
        private static readonly Color AccentColor = new Color(0.8f, 0.12f, 0.08f);

        private void Start()
        {
            _lobby = FindObjectOfType<LobbyManager>();

            if (_lobby == null)
            {
                Debug.LogError("[LobbyUI] LobbyManager not found in scene.");
                SetStatusText("ERROR: NO LOBBY MANAGER DETECTED");
                return;
            }

            BindEvents();
            BindButtons();
            InitializeDisplayName();
            RefreshLobby();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void BindEvents()
        {
            if (_lobby == null) return;

            _lobby.OnLobbyUpdated += RefreshLobby;
            _lobby.OnCountdownStarted += OnCountdownStarted;
            _lobby.OnCountdownCancelled += OnCountdownCancelled;
            _lobby.OnGameStarting += OnGameStarting;
        }

        private void UnbindEvents()
        {
            if (_lobby == null) return;

            _lobby.OnLobbyUpdated -= RefreshLobby;
            _lobby.OnCountdownStarted -= OnCountdownStarted;
            _lobby.OnCountdownCancelled -= OnCountdownCancelled;
            _lobby.OnGameStarting -= OnGameStarting;
        }

        private void BindButtons()
        {
            if (readyButton != null)
                readyButton.onClick.AddListener(OnReadyToggle);

            if (hostStartButtonComponent != null)
                hostStartButtonComponent.onClick.AddListener(OnHostStartClicked);

            if (displayNameInput != null)
                displayNameInput.onEndEdit.AddListener(OnDisplayNameChanged);

            if (chatInput != null)
                chatInput.onSubmit.AddListener(OnChatSubmit);
        }

        private void InitializeDisplayName()
        {
            if (displayNameInput == null) return;

            string savedName = PlayerPrefs.GetString("MimicFacility_PlayerName", "");
            if (!string.IsNullOrEmpty(savedName))
                displayNameInput.text = savedName;
        }

        private void RefreshLobby()
        {
            if (_lobby == null) return;

            RefreshPlayerCount();
            RefreshPlayerList();
            RefreshHostControls();
            RefreshReadyButton();
            RefreshReadyIndicators();
            RefreshCountdown();
        }

        private void RefreshPlayerCount()
        {
            int count = _lobby.PlayerCount;

            if (playerCountText != null)
                playerCountText.text = $"{count}/{MaxPlayers} SUBJECTS";

            if (titleText != null)
            {
                titleText.text = count == 0
                    ? "WAITING FOR SUBJECTS"
                    : $"WAITING FOR SUBJECTS  [{count} PRESENT]";
            }
        }

        private void RefreshPlayerList()
        {
            // Clear existing entries
            foreach (var entry in _playerEntries)
            {
                if (entry != null)
                    Destroy(entry);
            }
            _playerEntries.Clear();

            if (playerEntryTemplate == null || playerListContent == null) return;

            for (int i = 0; i < _lobby.LobbyPlayers.Count; i++)
            {
                var player = _lobby.LobbyPlayers[i];
                var entry = Instantiate(playerEntryTemplate, playerListContent);
                entry.SetActive(true);
                entry.name = $"Player_{player.subjectNumber:D2}";

                PopulatePlayerEntry(entry, player);
                _playerEntries.Add(entry);
            }
        }

        private void PopulatePlayerEntry(GameObject entry, LobbyPlayerData player)
        {
            // Ready indicator dot
            var readyDot = entry.transform.Find("ReadyDot");
            if (readyDot != null)
            {
                var dotImg = readyDot.GetComponent<Image>();
                if (dotImg != null)
                    dotImg.color = player.isReady ? ReadyOnColor : ReadyOffColor;
            }

            // Subject number
            var numObj = entry.transform.Find("SubjectNumber");
            if (numObj != null)
            {
                var numTMP = numObj.GetComponent<TextMeshProUGUI>();
                if (numTMP != null)
                    numTMP.text = player.subjectNumber.ToString("D2");
            }

            // Player name
            var nameObj = entry.transform.Find("PlayerName");
            if (nameObj != null)
            {
                var nameTMP = nameObj.GetComponent<TextMeshProUGUI>();
                if (nameTMP != null)
                {
                    string displayText = string.IsNullOrEmpty(player.playerName)
                        ? $"SUBJECT {player.subjectNumber:D2}"
                        : player.playerName.ToUpper();

                    bool isLocal = IsLocalPlayer(player.connectionId);
                    nameTMP.text = isLocal ? $"> {displayText}" : displayText;
                    nameTMP.fontStyle = isLocal ? FontStyles.Bold : FontStyles.Normal;
                }
            }

            // Host crown indicator
            if (_lobby.IsHost(player.connectionId))
            {
                var hostTag = entry.transform.Find("HostTag");
                if (hostTag != null)
                    hostTag.gameObject.SetActive(true);
            }
        }

        private void RefreshHostControls()
        {
            if (hostStartButton == null) return;

            int localConnId = GetLocalConnectionId();
            bool isHost = _lobby.IsHost(localConnId);
            bool allReady = _lobby.AllPlayersReady();
            bool enoughPlayers = _lobby.PlayerCount >= 2;

            hostStartButton.SetActive(isHost);

            if (hostStartButtonComponent != null)
                hostStartButtonComponent.interactable = allReady && enoughPlayers && !_lobby.IsCountingDown;
        }

        private void RefreshReadyButton()
        {
            if (readyButtonLabel == null) return;

            readyButtonLabel.text = _isReady ? "NOT READY" : "READY";
            readyButtonLabel.color = _isReady ? ReadyOffColor : ReadyOnColor;

            if (readyButton != null)
                readyButton.interactable = !_lobby.IsCountingDown;
        }

        private void RefreshReadyIndicators()
        {
            if (readyGlowLights == null || readyLightRenderers == null) return;

            for (int i = 0; i < MaxPlayers; i++)
            {
                bool occupied = false;
                bool ready = false;

                for (int j = 0; j < _lobby.LobbyPlayers.Count; j++)
                {
                    if (_lobby.LobbyPlayers[j].subjectNumber == i + 1)
                    {
                        occupied = true;
                        ready = _lobby.LobbyPlayers[j].isReady;
                        break;
                    }
                }

                Color indicatorColor = occupied
                    ? (ready ? ReadyOnColor : ReadyOffColor)
                    : new Color(0.15f, 0.15f, 0.17f);

                if (i < readyGlowLights.Length && readyGlowLights[i] != null)
                {
                    readyGlowLights[i].color = indicatorColor;
                    readyGlowLights[i].intensity = occupied ? 0.5f : 0.05f;
                }

                if (i < readyLightRenderers.Length && readyLightRenderers[i] != null)
                {
                    var mat = readyLightRenderers[i].material;
                    mat.color = indicatorColor;
                    if (occupied && ready)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", indicatorColor * 0.3f);
                    }
                    else
                    {
                        mat.DisableKeyword("_EMISSION");
                    }
                }
            }
        }

        private void RefreshCountdown()
        {
            if (countdownText == null) return;

            if (_lobby.IsCountingDown)
            {
                float t = _lobby.CountdownTimer;
                countdownText.text = $"INTAKE IN {Mathf.CeilToInt(t)}";
                countdownText.gameObject.SetActive(true);
            }
            else
            {
                countdownText.gameObject.SetActive(false);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // BUTTON CALLBACKS
        // ════════════════════════════════════════════════════════════════════

        private void OnReadyToggle()
        {
            if (_lobby == null) return;

            _isReady = !_isReady;
            int connId = GetLocalConnectionId();
            _lobby.CmdSetReady(connId, _isReady);
            RefreshReadyButton();
        }

        private void OnHostStartClicked()
        {
            if (_lobby == null) return;

            int connId = GetLocalConnectionId();
            _lobby.CmdStartGame(connId);
        }

        private void OnDisplayNameChanged(string newName)
        {
            if (_lobby == null) return;

            string sanitized = SanitizeDisplayName(newName);
            if (displayNameInput != null && displayNameInput.text != sanitized)
                displayNameInput.text = sanitized;

            PlayerPrefs.SetString("MimicFacility_PlayerName", sanitized);
            PlayerPrefs.Save();

            int connId = GetLocalConnectionId();
            _lobby.CmdChangeDisplayName(connId, sanitized);
        }

        private void OnChatSubmit(string message)
        {
            if (_lobby == null) return;
            if (string.IsNullOrWhiteSpace(message)) return;

            string sanitized = message.Trim();
            if (sanitized.Length > 200)
                sanitized = sanitized.Substring(0, 200);

            int connId = GetLocalConnectionId();
            _lobby.CmdSendChatMessage(connId, sanitized);

            if (chatInput != null)
            {
                chatInput.text = "";
                chatInput.ActivateInputField();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // COUNTDOWN
        // ════════════════════════════════════════════════════════════════════

        private void OnCountdownStarted(float duration)
        {
            if (_countdownCoroutine != null)
                StopCoroutine(_countdownCoroutine);

            _countdownCoroutine = StartCoroutine(CountdownDisplay(duration));
            SetStatusText("ALL SUBJECTS READY  //  INITIATING INTAKE SEQUENCE");
        }

        private void OnCountdownCancelled()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            if (countdownText != null)
                countdownText.gameObject.SetActive(false);

            SetStatusText("COUNTDOWN ABORTED  //  AWAITING READY STATUS");
            RefreshHostControls();
            RefreshReadyButton();
        }

        private IEnumerator CountdownDisplay(float duration)
        {
            float remaining = duration;

            while (remaining > 0f)
            {
                if (countdownText != null)
                {
                    int seconds = Mathf.CeilToInt(remaining);
                    countdownText.text = $"INTAKE IN {seconds}";
                    countdownText.gameObject.SetActive(true);

                    // Pulse effect — scale text on each second tick
                    float frac = remaining - Mathf.Floor(remaining);
                    float scale = 1f + 0.15f * (1f - frac);
                    countdownText.transform.localScale = Vector3.one * scale;
                }

                remaining -= Time.deltaTime;
                yield return null;
            }

            if (countdownText != null)
            {
                countdownText.text = "INITIATING...";
                countdownText.transform.localScale = Vector3.one;
            }

            _countdownCoroutine = null;
        }

        private void OnGameStarting()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            SetStatusText("INTAKE SEQUENCE ACTIVATED  //  TRANSFERRING TO FACILITY");

            // Disable all interactive elements
            if (readyButton != null) readyButton.interactable = false;
            if (hostStartButtonComponent != null) hostStartButtonComponent.interactable = false;
            if (displayNameInput != null) displayNameInput.interactable = false;
            if (chatInput != null) chatInput.interactable = false;

            if (countdownText != null)
            {
                countdownText.text = "INTAKE AUTHORIZED";
                countdownText.gameObject.SetActive(true);
                countdownText.transform.localScale = Vector3.one;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // CHAT DISPLAY
        // ════════════════════════════════════════════════════════════════════

        public void DisplayChatMessage(string senderName, string message)
        {
            if (chatContent == null) return;

            var msgObj = new GameObject("ChatMsg");
            msgObj.transform.SetParent(chatContent, false);
            var rt = msgObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 20f);

            var le = msgObj.AddComponent<LayoutElement>();
            le.preferredHeight = 20f;
            le.flexibleWidth = 1f;

            var tmp = msgObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<color=#CC3333>{senderName}</color>: {message}";
            tmp.fontSize = 13;
            tmp.color = new Color(0.7f, 0.7f, 0.72f);
            tmp.richText = true;

            // Auto-scroll to bottom
            if (chatScroll != null)
                StartCoroutine(ScrollToBottom());

            // Limit chat history
            const int maxMessages = 50;
            while (chatContent.childCount > maxMessages)
                Destroy(chatContent.GetChild(0).gameObject);
        }

        private IEnumerator ScrollToBottom()
        {
            yield return null; // wait one frame for layout rebuild
            if (chatScroll != null)
                chatScroll.verticalNormalizedPosition = 0f;
        }

        // ════════════════════════════════════════════════════════════════════
        // UTILITIES
        // ════════════════════════════════════════════════════════════════════

        private int GetLocalConnectionId()
        {
            if (NetworkClient.connection != null)
                return NetworkClient.connection.connectionId;
            return -1;
        }

        private bool IsLocalPlayer(int connectionId)
        {
            return connectionId == GetLocalConnectionId();
        }

        private void SetStatusText(string text)
        {
            if (statusText != null)
                statusText.text = text;
        }

        private static string SanitizeDisplayName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            string trimmed = raw.Trim();
            if (trimmed.Length > 24)
                trimmed = trimmed.Substring(0, 24);

            // Strip angle brackets to prevent TMP rich text injection
            trimmed = trimmed.Replace("<", "").Replace(">", "");

            return trimmed;
        }

        private void Update()
        {
            // Continuously refresh countdown display from SyncVar
            if (_lobby != null && _lobby.IsCountingDown)
                RefreshCountdown();
        }
    }
}
