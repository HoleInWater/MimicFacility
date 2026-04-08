using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MimicFacility.Core;

namespace MimicFacility.UI
{
    public class GameOverScreen : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 2f;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI reasonText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI directorMessageText;

        [Header("Navigation")]
        [SerializeField] private Button returnToMenuButton;

        public enum LossReason
        {
            AllDead,
            TooManyMiscontainments,
            TimeExpired
        }

        private static readonly string[] DirectorTaunts = new[]
        {
            "The facility expected more from you. It always does.",
            "Your voices have been archived. They will be useful.",
            "Twelve subjects entered. None proved adequate.",
            "The mimics learned everything they needed. Thank you for your cooperation.",
            "This intake is complete. The next group has already been selected.",
            "You were warned. You were watched. You were found wanting.",
            "The facility does not mourn. It iterates.",
            "Your fear was the most genuine data collected tonight.",
            "Every scream has been catalogued. Every silence noted.",
            "You spoke so freely. The facility is grateful.",
            "The containment was never for the mimics.",
            "Perhaps the next twelve will last longer. Perhaps not."
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

        public void Show(LossReason reason, float timeSurvived, int entitiesContained, int miscontainments, int tasksCompleted)
        {
            gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "INTAKE COMPLETE";
                titleText.color = new Color(0.8f, 0.1f, 0.1f);
            }

            if (reasonText != null)
                reasonText.text = FormatReason(reason);

            if (statsText != null)
                statsText.text = FormatStats(timeSurvived, entitiesContained, miscontainments, tasksCompleted);

            if (directorMessageText != null)
                directorMessageText.text = DirectorTaunts[Random.Range(0, DirectorTaunts.Length)];

            StartCoroutine(FadeIn());
        }

        private string FormatReason(LossReason reason)
        {
            return reason switch
            {
                LossReason.AllDead => "ALL SUBJECTS TERMINATED",
                LossReason.TooManyMiscontainments => "CRITICAL MISCONTAINMENT THRESHOLD EXCEEDED",
                LossReason.TimeExpired => "FACILITY LOCKDOWN PROTOCOL ENGAGED",
                _ => "SESSION ENDED"
            };
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
