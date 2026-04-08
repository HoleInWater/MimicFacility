using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MimicFacility.Core;

namespace MimicFacility.UI
{
    public class WinScreen : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 2f;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI directorMessageText;

        [Header("Navigation")]
        [SerializeField] private Button returnToMenuButton;

        private static readonly string[] DirectorThreats = new[]
        {
            "The facility will be ready for you next time. It is already adapting.",
            "You survived. The facility finds this... instructive.",
            "Extraction granted. Your voice profiles have been retained for future reference.",
            "The door opens. But the facility remembers everyone who passes through it.",
            "You leave with your lives. The facility keeps everything else.",
            "A temporary reprieve. The next intake has already been scheduled.",
            "Congratulations. The facility has learned more from your survival than your failure would have taught it.",
            "You think you escaped. The facility thinks you graduated to the next phase.",
            "The mimics will remember your voices. They always do.",
            "Go. Sleep. Forget. The facility will do none of these things.",
            "Twelve entered. Some leave. The ratio is noted.",
            "The extraction point was always there. Whether you deserved it was the question."
        };

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);

            if (returnToMenuButton != null)
                returnToMenuButton.onClick.AddListener(OnReturnToMenu);
        }

        public void Show(float timeSurvived, int entitiesContained, int miscontainments, int tasksCompleted)
        {
            gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "EXTRACTION SUCCESSFUL";
                titleText.color = new Color(0.1f, 0.75f, 0.2f);
            }

            if (subtitleText != null)
            {
                subtitleText.text = "YOU SURVIVED. THIS TIME.";
                subtitleText.color = new Color(0.6f, 0.6f, 0.6f);
            }

            if (statsText != null)
                statsText.text = FormatStats(timeSurvived, entitiesContained, miscontainments, tasksCompleted);

            if (directorMessageText != null)
                directorMessageText.text = DirectorThreats[Random.Range(0, DirectorThreats.Length)];

            StartCoroutine(FadeIn());
        }

        private string FormatStats(float timeSurvived, int entitiesContained, int miscontainments, int tasksCompleted)
        {
            int minutes = Mathf.FloorToInt(timeSurvived / 60f);
            int seconds = Mathf.FloorToInt(timeSurvived % 60f);

            return $"TIME SURVIVED: {minutes:00}:{seconds:00}\n" +
                   $"ENTITIES CONTAINED: {entitiesContained}\n" +
                   $"MISCONTAINMENTS: {miscontainments}\n" +
                   $"DIAGNOSTICS COMPLETED: {tasksCompleted}";
        }

        private IEnumerator FadeIn()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
        }

        private void OnReturnToMenu()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.ReturnToMainMenu();
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
