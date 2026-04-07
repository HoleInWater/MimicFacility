using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Mirror;
using MimicFacility.Core;

namespace MimicFacility.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject hostPanel;
        [SerializeField] private GameObject joinPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Main Buttons")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Host Panel")]
        [SerializeField] private Button hostConfirmButton;
        [SerializeField] private Button hostBackButton;

        [Header("Join Panel")]
        [SerializeField] private TMP_InputField serverAddressInput;
        [SerializeField] private Button joinConfirmButton;
        [SerializeField] private Button joinBackButton;

        [Header("Credits")]
        [SerializeField] private ScrollRect creditsScroll;
        [SerializeField] private Button creditsBackButton;
        [SerializeField] private float creditsScrollSpeed = 30f;

        [Header("Version")]
        [SerializeField] private TextMeshProUGUI versionText;

        [Header("Transition")]
        [SerializeField] private float panelFadeDuration = 0.25f;

        private GameObject activePanel;
        private Coroutine creditsCoroutine;

        private void Start()
        {
            hostButton.onClick.AddListener(OnHostClicked);
            joinButton.onClick.AddListener(OnJoinClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            creditsButton.onClick.AddListener(OnCreditsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);

            hostConfirmButton.onClick.AddListener(OnHostConfirm);
            hostBackButton.onClick.AddListener(OnBackClicked);

            joinConfirmButton.onClick.AddListener(OnJoinConfirm);
            joinBackButton.onClick.AddListener(OnBackClicked);

            creditsBackButton.onClick.AddListener(OnBackClicked);

            if (versionText != null)
                versionText.text = $"v{Application.version}";

            ShowPanel(mainPanel);
        }

        public void OnHostClicked() => ShowPanel(hostPanel);
        public void OnJoinClicked() => ShowPanel(joinPanel);
        public void OnSettingsClicked() => ShowPanel(settingsPanel);

        public void OnCreditsClicked()
        {
            ShowPanel(creditsPanel);
            if (creditsScroll != null)
            {
                creditsScroll.verticalNormalizedPosition = 1f;
                if (creditsCoroutine != null) StopCoroutine(creditsCoroutine);
                creditsCoroutine = StartCoroutine(AutoScrollCredits());
            }
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnBackClicked()
        {
            if (creditsCoroutine != null)
            {
                StopCoroutine(creditsCoroutine);
                creditsCoroutine = null;
            }
            ShowPanel(mainPanel);
        }

        public void OnHostConfirm()
        {
            var manager = NetworkManager.singleton;
            if (manager == null)
            {
                Debug.LogError("NetworkManager not found.");
                return;
            }

            manager.StartHost();
            SceneManager.LoadScene("Lobby");
        }

        public void OnJoinConfirm()
        {
            var manager = NetworkManager.singleton;
            if (manager == null)
            {
                Debug.LogError("NetworkManager not found.");
                return;
            }

            string address = serverAddressInput != null ? serverAddressInput.text.Trim() : "";
            if (string.IsNullOrEmpty(address))
                address = "localhost";

            manager.networkAddress = address;
            manager.StartClient();
        }

        private void ShowPanel(GameObject panel)
        {
            if (activePanel == panel) return;

            if (activePanel != null)
                StartCoroutine(AnimatePanel(activePanel, false));

            activePanel = panel;

            if (activePanel != null)
                StartCoroutine(AnimatePanel(activePanel, true));
        }

        private IEnumerator AnimatePanel(GameObject panel, bool show)
        {
            var group = panel.GetComponent<CanvasGroup>();
            if (group == null) group = panel.AddComponent<CanvasGroup>();

            if (show)
            {
                panel.SetActive(true);
                group.alpha = 0f;
                group.interactable = false;

                float elapsed = 0f;
                while (elapsed < panelFadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    group.alpha = elapsed / panelFadeDuration;
                    yield return null;
                }

                group.alpha = 1f;
                group.interactable = true;
            }
            else
            {
                group.interactable = false;
                float elapsed = 0f;
                while (elapsed < panelFadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    group.alpha = 1f - (elapsed / panelFadeDuration);
                    yield return null;
                }

                group.alpha = 0f;
                panel.SetActive(false);
            }
        }

        private IEnumerator AutoScrollCredits()
        {
            while (creditsScroll.verticalNormalizedPosition > 0f)
            {
                float contentHeight = creditsScroll.content.rect.height;
                if (contentHeight > 0f)
                {
                    float step = (creditsScrollSpeed * Time.unscaledDeltaTime) / contentHeight;
                    creditsScroll.verticalNormalizedPosition -= step;
                }
                yield return null;
            }
            creditsScroll.verticalNormalizedPosition = 0f;
        }
    }
}
