using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using MimicFacility.Audio;

namespace MimicFacility.UI
{
    /// <summary>
    /// At the start of the game, asks the player to select their gender.
    /// Then the Director "guesses" their name from a list of the most common
    /// names for that gender (2010 US census). If it gets their name right,
    /// it's terrifying. If it gets it wrong, it's still unsettling because
    /// the AI tried.
    ///
    /// Top 10 boys: Jacob, Ethan, Michael, Alexander, William, Joshua, Daniel, James, Landon, Logan
    /// Top 10 girls: Isabella, Sophia, Emma, Olivia, Ava, Emily, Abigail, Madison, Chloe, Mia
    /// </summary>
    public class NameGuessSystem : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject genderPanel;
        [SerializeField] private Button maleButton;
        [SerializeField] private Button femaleButton;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private CanvasGroup panelGroup;

        [Header("Timing")]
        [SerializeField] private float pauseBeforeGuess = 2f;
        [SerializeField] private float pauseAfterGuess = 3f;

        private static readonly string[] boyNames = {
            "Jacob", "Ethan", "Michael", "Alexander", "William",
            "Joshua", "Daniel", "James", "Landon", "Logan"
        };

        private static readonly string[] girlNames = {
            "Isabella", "Sophia", "Emma", "Olivia", "Ava",
            "Emily", "Abigail", "Madison", "Chloe", "Mia"
        };

        private string guessedName;
        private bool hasGuessed;
        private Canvas canvas;

        public string GuessedName => guessedName;
        public bool HasGuessed => hasGuessed;

        public event System.Action<string> OnNameGuessed;

        void Awake()
        {
            if (genderPanel == null)
                CreateUI();
        }

        public void Show()
        {
            // Must have EventSystem for buttons to work
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Cursor must be visible and unlocked to click buttons
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (genderPanel != null)
                genderPanel.SetActive(true);

            Debug.Log("[NameGuess] Panel shown. Cursor visible. Click MALE or FEMALE.");
        }

        public void Hide()
        {
            if (genderPanel != null)
                genderPanel.SetActive(false);
        }

        private void OnMaleSelected()
        {
            string name = boyNames[Random.Range(0, boyNames.Length)];
            StartCoroutine(GuessSequence(name));
        }

        private void OnFemaleSelected()
        {
            string name = girlNames[Random.Range(0, girlNames.Length)];
            StartCoroutine(GuessSequence(name));
        }

        private System.Collections.IEnumerator GuessSequence(string name)
        {
            guessedName = name;

            if (maleButton != null) maleButton.interactable = false;
            if (femaleButton != null) femaleButton.interactable = false;

            if (promptText != null)
                promptText.text = "PROCESSING...";

            yield return new WaitForSecondsRealtime(pauseBeforeGuess);

            // Play the HAL voice line with their name
            var clip = Resources.Load<AudioClip>($"Voice/Names/name_{name.ToLower()}");
            if (clip != null)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.clip = clip;
                src.spatialBlend = 0f;
                src.volume = 1f;
                src.Play();
                Destroy(src, clip.length + 0.5f);
            }

            if (resultText != null)
            {
                resultText.gameObject.SetActive(true);
                resultText.text = $"SUBJECT IDENTIFIED:\n<size=40>{name.ToUpper()}</size>";
            }

            if (promptText != null)
                promptText.text = "The facility knows your name.\nIt has always known your name.";

            yield return new WaitForSecondsRealtime(pauseAfterGuess);

            hasGuessed = true;
            OnNameGuessed?.Invoke(name);

            // Fade out
            if (panelGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 1.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    panelGroup.alpha = 1f - (elapsed / 1.5f);
                    yield return null;
                }
            }

            Hide();
        }

        private void CreateUI()
        {
            var canvasObj = new GameObject("NameGuessCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            panelGroup = canvasObj.AddComponent<CanvasGroup>();

            genderPanel = canvasObj;

            // Dark background
            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasObj.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.95f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Prompt text
            var promptObj = new GameObject("Prompt");
            promptObj.transform.SetParent(canvasObj.transform, false);
            var pRT = promptObj.AddComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.1f, 0.6f);
            pRT.anchorMax = new Vector2(0.9f, 0.8f);
            pRT.sizeDelta = Vector2.zero;
            promptText = promptObj.AddComponent<TextMeshProUGUI>();
            promptText.text = "THE FACILITY REQUIRES CLASSIFICATION\n\n<size=20>Select your identifier.</size>";
            promptText.fontSize = 28;
            promptText.color = new Color(0.8f, 0.8f, 0.8f);
            promptText.alignment = TextAlignmentOptions.Center;

            // Male button
            var maleObj = new GameObject("MaleButton");
            maleObj.transform.SetParent(canvasObj.transform, false);
            var maleImg = maleObj.AddComponent<Image>();
            maleImg.color = new Color(0.12f, 0.12f, 0.15f);
            maleButton = maleObj.AddComponent<Button>();
            maleButton.onClick.AddListener(OnMaleSelected);
            var maleRT = maleObj.GetComponent<RectTransform>();
            maleRT.anchorMin = new Vector2(0.2f, 0.35f);
            maleRT.anchorMax = new Vector2(0.45f, 0.5f);
            maleRT.sizeDelta = Vector2.zero;

            var maleTxtObj = new GameObject("Text");
            maleTxtObj.transform.SetParent(maleObj.transform, false);
            var maleTxtRT = maleTxtObj.AddComponent<RectTransform>();
            maleTxtRT.anchorMin = Vector2.zero;
            maleTxtRT.anchorMax = Vector2.one;
            maleTxtRT.sizeDelta = Vector2.zero;
            var maleTMP = maleTxtObj.AddComponent<TextMeshProUGUI>();
            maleTMP.text = "MALE";
            maleTMP.fontSize = 24;
            maleTMP.color = Color.white;
            maleTMP.alignment = TextAlignmentOptions.Center;

            // Female button
            var femaleObj = new GameObject("FemaleButton");
            femaleObj.transform.SetParent(canvasObj.transform, false);
            var femaleImg = femaleObj.AddComponent<Image>();
            femaleImg.color = new Color(0.12f, 0.12f, 0.15f);
            femaleButton = femaleObj.AddComponent<Button>();
            femaleButton.onClick.AddListener(OnFemaleSelected);
            var femaleRT = femaleObj.GetComponent<RectTransform>();
            femaleRT.anchorMin = new Vector2(0.55f, 0.35f);
            femaleRT.anchorMax = new Vector2(0.8f, 0.5f);
            femaleRT.sizeDelta = Vector2.zero;

            var femaleTxtObj = new GameObject("Text");
            femaleTxtObj.transform.SetParent(femaleObj.transform, false);
            var femaleTxtRT = femaleTxtObj.AddComponent<RectTransform>();
            femaleTxtRT.anchorMin = Vector2.zero;
            femaleTxtRT.anchorMax = Vector2.one;
            femaleTxtRT.sizeDelta = Vector2.zero;
            var femaleTMP = femaleTxtObj.AddComponent<TextMeshProUGUI>();
            femaleTMP.text = "FEMALE";
            femaleTMP.fontSize = 24;
            femaleTMP.color = Color.white;
            femaleTMP.alignment = TextAlignmentOptions.Center;

            // Result text (hidden initially)
            var resultObj = new GameObject("Result");
            resultObj.transform.SetParent(canvasObj.transform, false);
            var rRT = resultObj.AddComponent<RectTransform>();
            rRT.anchorMin = new Vector2(0.1f, 0.15f);
            rRT.anchorMax = new Vector2(0.9f, 0.35f);
            rRT.sizeDelta = Vector2.zero;
            resultText = resultObj.AddComponent<TextMeshProUGUI>();
            resultText.text = "";
            resultText.fontSize = 24;
            resultText.color = new Color(0.9f, 0.15f, 0.1f);
            resultText.alignment = TextAlignmentOptions.Center;
            resultObj.SetActive(false);

            genderPanel.SetActive(false);
        }
    }
}
