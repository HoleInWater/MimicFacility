using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace MimicFacility.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI subjectText;
        [SerializeField] private TextMeshProUGUI hexIdText;

        [Header("Timing")]
        [SerializeField] private float minimumDisplayTime = 3f;
        [SerializeField] private float statusCycleInterval = 0.8f;
        [SerializeField] private float progressSmoothing = 2f;

        private static readonly string[] FacilityStatusMessages =
        {
            "INITIALIZING VOICE CAPTURE...",
            "CALIBRATING NEURAL PATTERNS...",
            "MAPPING BEHAVIORAL SIGNATURES...",
            "ARCHIVING VOCAL PROFILES...",
            "SYNCHRONIZING ENTITY PROTOCOLS...",
            "LOADING FACILITY LAYOUT...",
            "PREPARING CONTAINMENT ZONES...",
            "ESTABLISHING DIRECTOR LINK...",
            "SCANNING BIOMETRIC DATA...",
            "INDEXING SUBJECT HISTORIES..."
        };

        private string _playerName = "UNKNOWN";
        private string _hexId;
        private float _displayedProgress;
        private bool _isLoading;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetPlayerName(string name)
        {
            _playerName = string.IsNullOrWhiteSpace(name) ? "UNKNOWN" : name.ToUpperInvariant();
        }

        public void LoadScene(string sceneName)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            _isLoading = true;
            _displayedProgress = 0f;

            GenerateHexId();
            SetVisible(true);
            UpdateSubjectDisplay();
            UpdateHexDisplay();

            if (titleText != null)
                titleText.text = "INTAKE";

            if (progressBarFill != null)
                progressBarFill.fillAmount = 0f;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            float elapsed = 0f;
            int statusIndex = Random.Range(0, FacilityStatusMessages.Length);
            float statusTimer = 0f;

            SetStatusMessage(FacilityStatusMessages[statusIndex]);

            while (elapsed < minimumDisplayTime || operation.progress < 0.9f)
            {
                elapsed += Time.unscaledDeltaTime;
                statusTimer += Time.unscaledDeltaTime;

                if (statusTimer >= statusCycleInterval)
                {
                    statusTimer = 0f;
                    statusIndex = (statusIndex + 1) % FacilityStatusMessages.Length;
                    SetStatusMessage(FacilityStatusMessages[statusIndex]);
                }

                float actualProgress = Mathf.Clamp01(operation.progress / 0.9f);
                float timeProgress = Mathf.Clamp01(elapsed / minimumDisplayTime);
                float targetProgress = Mathf.Min(actualProgress, timeProgress);
                targetProgress = Mathf.Max(targetProgress, timeProgress * 0.85f);

                _displayedProgress = Mathf.Lerp(_displayedProgress, targetProgress,
                    Time.unscaledDeltaTime * progressSmoothing);

                if (progressBarFill != null)
                    progressBarFill.fillAmount = _displayedProgress;

                yield return null;
            }

            // Fill to 100%
            float fillElapsed = 0f;
            float fillDuration = 0.3f;
            float startFill = _displayedProgress;

            while (fillElapsed < fillDuration)
            {
                fillElapsed += Time.unscaledDeltaTime;
                float t = fillElapsed / fillDuration;
                _displayedProgress = Mathf.Lerp(startFill, 1f, t);

                if (progressBarFill != null)
                    progressBarFill.fillAmount = _displayedProgress;

                yield return null;
            }

            if (progressBarFill != null)
                progressBarFill.fillAmount = 1f;

            SetStatusMessage("FACILITY ACCESS GRANTED");
            yield return new WaitForSecondsRealtime(0.4f);

            operation.allowSceneActivation = true;

            yield return new WaitUntil(() => operation.isDone);

            SetVisible(false);
            _isLoading = false;
        }

        private void GenerateHexId()
        {
            _hexId = Random.Range(0, 0xFFFFFF + 1).ToString("X6")
                     + "-"
                     + Random.Range(0, 0xFFFF + 1).ToString("X4");
        }

        private void SetStatusMessage(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void UpdateSubjectDisplay()
        {
            if (subjectText != null)
                subjectText.text = $"SUBJECT: {_playerName}";
        }

        private void UpdateHexDisplay()
        {
            if (hexIdText != null)
                hexIdText.text = $"ID: {_hexId}";
        }

        private void SetVisible(bool visible)
        {
            if (canvas != null)
                canvas.enabled = visible;
            else
                gameObject.SetActive(visible);
        }

        public bool IsLoading => _isLoading;
    }
}
