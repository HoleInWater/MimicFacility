using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Mirror;
using MimicFacility.Core;

namespace MimicFacility.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject joinPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Main Buttons")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Join Panel")]
        [SerializeField] private TMP_InputField serverAddressInput;
        [SerializeField] private Button joinConfirmButton;
        [SerializeField] private Button joinBackButton;

        [Header("Settings Panel")]
        [SerializeField] private Button settingsBackButton;

        [Header("Version")]
        [SerializeField] private TextMeshProUGUI versionText;

        [Header("Audio")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private float musicVolume = 0.15f;

        [Header("Camera Drift")]
        [SerializeField] private float driftSpeed = 0.08f;
        [SerializeField] private float driftAmplitudeX = 0.3f;
        [SerializeField] private float driftAmplitudeY = 0.15f;
        [SerializeField] private float driftRotationAmount = 0.4f;

        [Header("Panel Transition")]
        [SerializeField] private float panelFadeDuration = 0.2f;

        private Camera mainCamera;
        private Vector3 cameraBasePosition;
        private Quaternion cameraBaseRotation;
        private GameObject activePanel;

        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraBasePosition = mainCamera.transform.position;
                cameraBaseRotation = mainCamera.transform.rotation;
            }

            SetupButtons();
            SetupAudio();
            SetupVersion();
            ShowPanel(mainPanel);
        }

        private void Update()
        {
            UpdateCameraDrift();
        }

        private void SetupButtons()
        {
            if (hostButton != null)
                hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton != null)
                joinButton.onClick.AddListener(OnJoinClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
            if (joinConfirmButton != null)
                joinConfirmButton.onClick.AddListener(OnJoinConfirm);
            if (joinBackButton != null)
                joinBackButton.onClick.AddListener(OnBackToMain);
            if (settingsBackButton != null)
                settingsBackButton.onClick.AddListener(OnBackToMain);
        }

        private void SetupAudio()
        {
            if (musicSource == null)
            {
                musicSource = GetComponentInChildren<AudioSource>();
                if (musicSource == null) return;
            }

            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;

            if (musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();

            var corruptor = musicSource.GetComponent<AudioCorruptor>();
            if (corruptor == null)
                musicSource.gameObject.AddComponent<AudioCorruptor>();
        }

        private void SetupVersion()
        {
            if (versionText == null) return;

            if (VersionInfo.Instance != null)
            {
                versionText.text = $"v{VersionInfo.Instance.GetDisplayVersion()}";
                return;
            }

            var versionFile = Resources.Load<TextAsset>("VERSION");
            if (versionFile != null)
            {
                versionText.text = $"v{versionFile.text.Trim()}";
                return;
            }

            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "VERSION");
            if (System.IO.File.Exists(path))
            {
                versionText.text = $"v{System.IO.File.ReadAllText(path).Trim()}";
                return;
            }

            versionText.text = $"v{Application.version}";
        }

        private void UpdateCameraDrift()
        {
            if (mainCamera == null) return;

            float t = Time.time;
            float offsetX = Mathf.Sin(t * driftSpeed) * driftAmplitudeX;
            float offsetY = Mathf.Sin(t * driftSpeed * 0.7f + 1.3f) * driftAmplitudeY;
            float rotZ = Mathf.Sin(t * driftSpeed * 0.5f + 2.7f) * driftRotationAmount;

            mainCamera.transform.position = cameraBasePosition + new Vector3(offsetX, offsetY, 0f);
            mainCamera.transform.rotation = cameraBaseRotation * Quaternion.Euler(
                Mathf.Sin(t * driftSpeed * 0.6f) * driftRotationAmount * 0.5f,
                rotZ * 0.3f,
                rotZ
            );
        }

        // ── Button Handlers ─────────────────────────────────────────────

        public void OnHostClicked()
        {
            var manager = NetworkManager.singleton;
            if (manager == null)
            {
                Debug.LogError("[MainMenu] NetworkManager not found in scene.");
                return;
            }

            manager.StartHost();
            SceneManager.LoadScene("Lobby");
        }

        public void OnJoinClicked()
        {
            ShowPanel(joinPanel);
        }

        public void OnSettingsClicked()
        {
            ShowPanel(settingsPanel);
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnJoinConfirm()
        {
            var manager = NetworkManager.singleton;
            if (manager == null)
            {
                Debug.LogError("[MainMenu] NetworkManager not found in scene.");
                return;
            }

            string address = serverAddressInput != null ? serverAddressInput.text.Trim() : "";
            if (string.IsNullOrEmpty(address))
                address = "localhost";

            manager.networkAddress = address;
            manager.StartClient();
        }

        public void OnBackToMain()
        {
            ShowPanel(mainPanel);
        }

        // ── Panel Transitions ───────────────────────────────────────────

        private void ShowPanel(GameObject panel)
        {
            if (panel == null || activePanel == panel) return;

            if (activePanel != null)
                StartCoroutine(FadePanel(activePanel, false));

            activePanel = panel;
            StartCoroutine(FadePanel(activePanel, true));
        }

        private IEnumerator FadePanel(GameObject panel, bool show)
        {
            var group = panel.GetComponent<CanvasGroup>();
            if (group == null)
                group = panel.AddComponent<CanvasGroup>();

            if (show)
            {
                panel.SetActive(true);
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                float elapsed = 0f;
                while (elapsed < panelFadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    group.alpha = Mathf.Clamp01(elapsed / panelFadeDuration);
                    yield return null;
                }

                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
            else
            {
                group.interactable = false;
                group.blocksRaycasts = false;

                float elapsed = 0f;
                while (elapsed < panelFadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    group.alpha = 1f - Mathf.Clamp01(elapsed / panelFadeDuration);
                    yield return null;
                }

                group.alpha = 0f;
                panel.SetActive(false);
            }
        }
    }
}
