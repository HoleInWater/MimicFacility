using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MimicFacility.Core;

namespace MimicFacility.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image overlayImage;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitToMenuButton;

        [Header("Settings")]
        [SerializeField] private string menuSceneName = "MainMenu";
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.75f);

        private bool _isPaused;
        private CursorLockMode _previousLockMode;
        private bool _previousCursorVisible;

        private void Awake()
        {
            if (overlayImage != null)
                overlayImage.color = overlayColor;

            SetVisible(false);
        }

        private void OnEnable()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            if (quitToMenuButton != null)
                quitToMenuButton.onClick.AddListener(QuitToMenu);

            var input = FallbackInputManager.Instance;
            if (input != null)
                input.OnPausePressed += OnPauseInput;
        }

        private void OnDisable()
        {
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(Resume);
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettings);
            if (quitToMenuButton != null)
                quitToMenuButton.onClick.RemoveListener(QuitToMenu);

            var input = FallbackInputManager.Instance;
            if (input != null)
                input.OnPausePressed -= OnPauseInput;
        }

        private void Update()
        {
            if (FallbackInputManager.Instance != null)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                OnPauseInput();
        }

        private void OnPauseInput()
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }

        public void Pause()
        {
            if (_isPaused) return;

            _isPaused = true;
            _previousLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SetVisible(true);
        }

        public void Resume()
        {
            if (!_isPaused) return;

            _isPaused = false;

            Time.timeScale = 1f;
            Cursor.lockState = _previousLockMode;
            Cursor.visible = _previousCursorVisible;

            SetVisible(false);
        }

        private void OpenSettings()
        {
            // Placeholder for settings panel integration.
            // SettingsUI can be toggled here when wired up.
            Debug.Log("[PauseMenu] Settings requested.");
        }

        private void QuitToMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _isPaused = false;

            if (LoadingScreen.Instance != null)
                LoadingScreen.Instance.LoadScene(menuSceneName);
            else
                SceneManager.LoadScene(menuSceneName);
        }

        private void SetVisible(bool visible)
        {
            if (canvas != null)
                canvas.enabled = visible;
        }

        public bool IsPaused => _isPaused;
    }
}
