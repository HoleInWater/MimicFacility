using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MimicFacility.Gameplay;

namespace MimicFacility.UI
{
    public class AccusationUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject accusationPanel;
        [SerializeField] private GameObject deliberationPanel;
        [SerializeField] private GameObject votingPanel;
        [SerializeField] private GameObject resultPanel;

        [Header("Deliberation")]
        [SerializeField] private TextMeshProUGUI deliberationText;
        [SerializeField] private TextMeshProUGUI deliberationTimerText;

        [Header("Voting")]
        [SerializeField] private TextMeshProUGUI votingTimerText;
        [SerializeField] private Button containButton;
        [SerializeField] private Button releaseButton;
        [SerializeField] private TextMeshProUGUI containButtonText;
        [SerializeField] private TextMeshProUGUI releaseButtonText;

        [Header("Result")]
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Image resultBackground;

        [Header("Accusation Button")]
        [SerializeField] private Button accuseButton;
        [SerializeField] private TextMeshProUGUI accuseCooldownText;

        [Header("Settings")]
        [SerializeField] private float resultDisplayDuration = 5f;
        [SerializeField] private float slideInDuration = 0.3f;

        [Header("Colors")]
        [SerializeField] private Color mimicCaughtColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
        [SerializeField] private Color falsePositiveColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color releasedColor = new Color(0.8f, 0.8f, 0.2f, 0.9f);

        private AccusationManager accusationManager;
        private Coroutine timerCoroutine;
        private Coroutine resultCoroutine;
        private Coroutine cooldownCoroutine;
        private bool hasVoted;

        private void Start()
        {
            accusationManager = FindObjectOfType<AccusationManager>();
            if (accusationManager != null)
            {
                accusationManager.OnAccusationStarted += OnAccusationStarted;
                accusationManager.OnVotingStarted += OnVotingStarted;
                accusationManager.OnAccusationResolved += OnAccusationResolved;
            }

            containButton.onClick.AddListener(OnContainClicked);
            releaseButton.onClick.AddListener(OnReleaseClicked);

            HideAll();
        }

        private void OnDestroy()
        {
            if (accusationManager != null)
            {
                accusationManager.OnAccusationStarted -= OnAccusationStarted;
                accusationManager.OnVotingStarted -= OnVotingStarted;
                accusationManager.OnAccusationResolved -= OnAccusationResolved;
            }
        }

        private void OnAccusationStarted(string accuserName, string accusedName)
        {
            ShowDeliberation(accuserName, accusedName, 15f);
        }

        private void OnVotingStarted()
        {
            ShowVoting(10f);
        }

        private void OnAccusationResolved(AccusationRecord record)
        {
            ShowResult(record.result, record.wasMimic);
        }

        public void ShowDeliberation(string accuserName, string accusedName, float timeRemaining)
        {
            HideAll();
            accusationPanel.SetActive(true);
            deliberationPanel.SetActive(true);

            if (deliberationText != null)
                deliberationText.text = $"Subject {accuserName} accuses Subject {accusedName}";

            hasVoted = false;
            StartSlideIn(deliberationPanel);
            StartTimer(deliberationTimerText, timeRemaining);
        }

        public void ShowVoting(float timeRemaining)
        {
            deliberationPanel.SetActive(false);
            votingPanel.SetActive(true);

            containButton.interactable = true;
            releaseButton.interactable = true;
            hasVoted = false;

            StartSlideIn(votingPanel);
            StartTimer(votingTimerText, timeRemaining);
        }

        public void OnContainClicked()
        {
            if (hasVoted) return;
            hasVoted = true;
            DisableVotingButtons();

            if (accusationManager != null)
                accusationManager.CmdCastVote(EAccusationVote.Contain);
        }

        public void OnReleaseClicked()
        {
            if (hasVoted) return;
            hasVoted = true;
            DisableVotingButtons();

            if (accusationManager != null)
                accusationManager.CmdCastVote(EAccusationVote.Release);
        }

        public void ShowResult(EAccusationResult result, bool wasMimic)
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }

            deliberationPanel.SetActive(false);
            votingPanel.SetActive(false);
            resultPanel.SetActive(true);

            switch (result)
            {
                case EAccusationResult.MimicContained:
                    resultText.text = "MIMIC CONTAINED\nThreat neutralized.";
                    resultBackground.color = mimicCaughtColor;
                    break;
                case EAccusationResult.FalsePositive:
                    resultText.text = "FALSE POSITIVE\nAn innocent crew member has been contained.";
                    resultBackground.color = falsePositiveColor;
                    break;
                case EAccusationResult.MimicReleased:
                    resultText.text = "SUSPECT RELEASED\nThe mimic walks free among you.";
                    resultBackground.color = releasedColor;
                    break;
                case EAccusationResult.RealReleased:
                    resultText.text = "SUSPECT RELEASED\nCrew member cleared.";
                    resultBackground.color = releasedColor;
                    break;
            }

            StartSlideIn(resultPanel);

            if (resultCoroutine != null) StopCoroutine(resultCoroutine);
            resultCoroutine = StartCoroutine(HideResultAfterDelay());
        }

        private IEnumerator HideResultAfterDelay()
        {
            yield return new WaitForSeconds(resultDisplayDuration);
            HideAll();
        }

        public void ShowAccusationCooldown(float remaining)
        {
            if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = StartCoroutine(CooldownCoroutine(remaining));
        }

        private IEnumerator CooldownCoroutine(float remaining)
        {
            if (accuseButton != null) accuseButton.interactable = false;

            while (remaining > 0f)
            {
                if (accuseCooldownText != null)
                    accuseCooldownText.text = $"Accuse ({remaining:F0}s)";
                remaining -= Time.deltaTime;
                yield return null;
            }

            if (accuseButton != null) accuseButton.interactable = true;
            if (accuseCooldownText != null) accuseCooldownText.text = "Accuse";
        }

        private void DisableVotingButtons()
        {
            containButton.interactable = false;
            releaseButton.interactable = false;
        }

        private void StartTimer(TextMeshProUGUI timerText, float duration)
        {
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(TimerCoroutine(timerText, duration));
        }

        private IEnumerator TimerCoroutine(TextMeshProUGUI timerText, float duration)
        {
            float remaining = duration;
            while (remaining > 0f)
            {
                if (timerText != null)
                    timerText.text = Mathf.CeilToInt(remaining).ToString();
                remaining -= Time.deltaTime;
                yield return null;
            }
            if (timerText != null) timerText.text = "0";
        }

        private void StartSlideIn(GameObject panel)
        {
            StartCoroutine(SlideInCoroutine(panel));
        }

        private IEnumerator SlideInCoroutine(GameObject panel)
        {
            var rt = panel.GetComponent<RectTransform>();
            if (rt == null) yield break;

            Vector2 targetPos = rt.anchoredPosition;
            Vector2 startPos = targetPos + Vector2.down * 200f;
            rt.anchoredPosition = startPos;

            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / slideInDuration);
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }
            rt.anchoredPosition = targetPos;
        }

        private void HideAll()
        {
            if (accusationPanel != null) accusationPanel.SetActive(false);
            if (deliberationPanel != null) deliberationPanel.SetActive(false);
            if (votingPanel != null) votingPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
        }
    }
}
