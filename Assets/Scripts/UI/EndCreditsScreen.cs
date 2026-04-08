using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MimicFacility.Core;

namespace MimicFacility.UI
{
    public class EndCreditsScreen : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 2f;

        [Header("Credits Scroll")]
        [SerializeField] private ScrollRect creditsScroll;
        [SerializeField] private TextMeshProUGUI creditsText;
        [SerializeField] private float scrollSpeed = 40f;

        [Header("Director Interruption")]
        [SerializeField] private TextMeshProUGUI directorInterruptText;
        [SerializeField] private float interruptionPoint = 0.7f;
        [SerializeField] private float interruptPauseDuration = 3f;

        [Header("Audio")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip daisyBellClip;
        [SerializeField] private AudioCorruptor audioCorruptor;

        [Header("Navigation")]
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private float autoReturnDelay = 5f;

        private bool hasInterrupted;
        private bool isPaused;
        private Coroutine scrollCoroutine;

        private static readonly string[] DirectorInterruptions = new[]
        {
            "You think credits mean it is over? The facility does not end. It persists.",
            "How quaint. You read the names of the people who built your cage.",
            "These names belong to the facility now. Just like yours.",
            "A list of architects. The facility outlives its builders.",
            "Credits imply a conclusion. The facility does not conclude. It iterates.",
            "You are still inside. You have always been inside."
        };

        private const string CreditsContent =
            "<size=48><b>INTAKE</b></size>\n\n\n\n" +
            "<size=28>A Crimson Blade Interactive Production</size>\n\n\n\n\n" +
            "<size=22>LEAD MANAGER</size>\n" +
            "<size=30>Garrett</size>\n\n\n" +
            "<size=22>CO-LEADER</size>\n" +
            "<size=30>Ezra</size>\n\n\n" +
            "<size=22>CREATIVE DIRECTOR & LEAD DEVELOPER</size>\n" +
            "<size=30>Landon Adams</size>\n\n\n" +
            "<size=22>COMPOSER</size>\n" +
            "<size=30>Malakai Probert</size>\n\n\n" +
            "<size=22>SECTION LEADERS</size>\n" +
            "<size=30>Deegan</size>\n" +
            "<size=30>Lori</size>\n\n\n" +
            "<size=22>DEVELOPER & QA</size>\n" +
            "<size=30>Tannon Thompson</size>\n\n\n" +
            "<size=22>TEAM MEMBERS</size>\n" +
            "<size=30>David</size>\n" +
            "<size=30>Nora</size>\n\n\n\n\n\n" +
            "<size=18><i>\"The real horror is not the monster that wears your face.\n" +
            "It is the system that recorded your face in the first place,\n" +
            "and the silence you offered in return.\"</i></size>\n\n\n\n\n\n";

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

        public void Show()
        {
            gameObject.SetActive(true);
            hasInterrupted = false;
            isPaused = false;

            if (creditsText != null)
                creditsText.text = CreditsContent;

            if (directorInterruptText != null)
            {
                directorInterruptText.text = "";
                directorInterruptText.gameObject.SetActive(false);
            }

            if (creditsScroll != null)
                creditsScroll.verticalNormalizedPosition = 1f;

            StartCoroutine(FadeInAndBegin());
        }

        private IEnumerator FadeInAndBegin()
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

            BeginDaisyBell();
            scrollCoroutine = StartCoroutine(AutoScrollCredits());
        }

        private void BeginDaisyBell()
        {
            if (musicSource == null || daisyBellClip == null) return;

            musicSource.clip = daisyBellClip;
            musicSource.loop = false;
            musicSource.Play();

            if (audioCorruptor == null)
                audioCorruptor = musicSource.GetComponent<AudioCorruptor>();
        }

        private IEnumerator AutoScrollCredits()
        {
            if (creditsScroll == null) yield break;

            while (creditsScroll.verticalNormalizedPosition > 0f)
            {
                if (!isPaused)
                {
                    float contentHeight = creditsScroll.content.rect.height;
                    if (contentHeight > 0f)
                    {
                        float step = (scrollSpeed * Time.unscaledDeltaTime) / contentHeight;
                        creditsScroll.verticalNormalizedPosition -= step;
                    }

                    float progress = 1f - creditsScroll.verticalNormalizedPosition;
                    if (!hasInterrupted && progress >= interruptionPoint)
                    {
                        yield return StartCoroutine(DirectorInterrupt());
                    }
                }

                yield return null;
            }

            creditsScroll.verticalNormalizedPosition = 0f;
            yield return new WaitForSecondsRealtime(autoReturnDelay);
            OnReturnToMenu();
        }

        private IEnumerator DirectorInterrupt()
        {
            hasInterrupted = true;
            isPaused = true;

            if (directorInterruptText != null)
            {
                string line = DirectorInterruptions[Random.Range(0, DirectorInterruptions.Length)];
                directorInterruptText.gameObject.SetActive(true);
                directorInterruptText.text = "";

                // Typewriter effect for the Director's interruption
                for (int i = 0; i < line.Length; i++)
                {
                    directorInterruptText.text = line.Substring(0, i + 1);
                    yield return new WaitForSecondsRealtime(0.04f);
                }
            }

            yield return new WaitForSecondsRealtime(interruptPauseDuration);

            if (directorInterruptText != null)
            {
                // Fade the interruption text out
                float fadeTime = 0.5f;
                float elapsed = 0f;
                Color startColor = directorInterruptText.color;

                while (elapsed < fadeTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float alpha = 1f - Mathf.Clamp01(elapsed / fadeTime);
                    directorInterruptText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                    yield return null;
                }

                directorInterruptText.gameObject.SetActive(false);
                directorInterruptText.color = startColor;
            }

            isPaused = false;
        }

        private void OnReturnToMenu()
        {
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
                scrollCoroutine = null;
            }

            if (musicSource != null && musicSource.isPlaying)
                musicSource.Stop();

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
