using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MimicFacility.Lore;

namespace MimicFacility.UI
{
    public class TerminalUI : MonoBehaviour
    {
        public event Action OnTerminalClosed;

        [Header("Panel")]
        [SerializeField] private CanvasGroup terminalGroup;
        [SerializeField] private GameObject terminalPanel;

        [Header("Entry List")]
        [SerializeField] private ScrollRect entryList;
        [SerializeField] private GameObject entryButtonPrefab;

        [Header("Entry Content")]
        [SerializeField] private TextMeshProUGUI entryTitle;
        [SerializeField] private TextMeshProUGUI entryAuthor;
        [SerializeField] private TextMeshProUGUI entryClassification;
        [SerializeField] private TextMeshProUGUI entryContent;

        [Header("Effects")]
        [SerializeField] private AudioSource keyClickSource;
        [SerializeField] private AudioClip keyClickClip;
        [SerializeField] private AudioSource humSource;
        [SerializeField] private GameObject scanlineOverlay;

        [Header("Settings")]
        [SerializeField] private float typewriterCharDelay = 0.02f;
        [SerializeField] private float fadeInDuration = 0.4f;
        [SerializeField] private float redactedFlickerChance = 0.03f;

        [Header("Colors")]
        [SerializeField] private Color unreadHighlight = new Color(0.3f, 1f, 0.3f, 1f);
        [SerializeField] private Color readColor = new Color(0.0f, 0.6f, 0.0f, 1f);

        private List<LoreEntry> currentEntries;
        private Coroutine typewriterCoroutine;
        private bool isTypewriting;
        private string fullTypewriterText;
        private readonly List<GameObject> spawnedButtons = new List<GameObject>();

        private void Awake()
        {
            if (terminalPanel != null) terminalPanel.SetActive(false);
            if (scanlineOverlay != null) scanlineOverlay.SetActive(false);
        }

        public void OpenTerminal(List<LoreEntry> entries)
        {
            currentEntries = entries;
            terminalPanel.SetActive(true);

            if (scanlineOverlay != null) scanlineOverlay.SetActive(true);
            if (humSource != null && !humSource.isPlaying) humSource.Play();

            PopulateEntryList(entries);
            ClearContent();
            StartCoroutine(FadeIn());
        }

        public void CloseTerminal()
        {
            StopTypewriter();
            if (humSource != null) humSource.Stop();
            if (scanlineOverlay != null) scanlineOverlay.SetActive(false);

            terminalPanel.SetActive(false);
            ClearSpawnedButtons();
            OnTerminalClosed?.Invoke();
        }

        private IEnumerator FadeIn()
        {
            if (terminalGroup == null) yield break;

            terminalGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                terminalGroup.alpha = elapsed / fadeInDuration;
                yield return null;
            }
            terminalGroup.alpha = 1f;
        }

        public void PopulateEntryList(List<LoreEntry> entries)
        {
            ClearSpawnedButtons();

            if (entryList == null || entryButtonPrefab == null) return;

            foreach (var entry in entries)
            {
                GameObject btnObj = Instantiate(entryButtonPrefab, entryList.content);
                spawnedButtons.Add(btnObj);

                var text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    string prefix = entry.classification == ELoreClassification.REDACTED ? "[REDACTED] " : "";
                    text.text = $"{prefix}{entry.title}";

                    bool isRead = LoreDatabase.Instance != null && LoreDatabase.Instance.IsEntryRead(entry.entryId);
                    text.color = isRead ? readColor : unreadHighlight;
                }

                var button = btnObj.GetComponent<Button>();
                if (button != null)
                {
                    var capturedEntry = entry;
                    button.onClick.AddListener(() => OnEntrySelected(capturedEntry));
                }
            }
        }

        public void OnEntrySelected(LoreEntry entry)
        {
            if (entryTitle != null) entryTitle.text = entry.title;
            if (entryAuthor != null) entryAuthor.text = entry.author ?? "";
            if (entryClassification != null) entryClassification.text = $"[{entry.classification}]";

            string displayContent = entry.content;
            if (entry.isRedacted && entry.classification == ELoreClassification.REDACTED
                && string.IsNullOrEmpty(entry.redactedContent))
            {
                displayContent = RedactedTextEffect(entry.content);
            }

            StartTypewriter(displayContent);
            MarkAsRead(entry.entryId);
        }

        private void StartTypewriter(string text)
        {
            StopTypewriter();
            fullTypewriterText = text;
            typewriterCoroutine = StartCoroutine(TypewriterCoroutine(text));
        }

        private IEnumerator TypewriterCoroutine(string text)
        {
            isTypewriting = true;
            entryContent.text = "";

            for (int i = 0; i < text.Length; i++)
            {
                entryContent.text = text.Substring(0, i + 1);
                PlayKeyClick();
                yield return new WaitForSecondsRealtime(typewriterCharDelay);
            }

            isTypewriting = false;
        }

        public void SkipTypewriter()
        {
            if (!isTypewriting) return;

            StopTypewriter();
            if (entryContent != null && fullTypewriterText != null)
                entryContent.text = fullTypewriterText;
        }

        private void StopTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            isTypewriting = false;
        }

        public string RedactedTextEffect(string text)
        {
            char[] chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsLetterOrDigit(chars[i]))
                {
                    if (UnityEngine.Random.value > redactedFlickerChance)
                        chars[i] = '\u2588';
                }
            }
            return new string(chars);
        }

        private void PlayKeyClick()
        {
            if (keyClickSource != null && keyClickClip != null)
                keyClickSource.PlayOneShot(keyClickClip, 0.3f);
        }

        public void MarkAsRead(string entryId)
        {
            if (LoreDatabase.Instance != null)
                LoreDatabase.Instance.MarkEntryAsRead(entryId);

            RefreshButtonHighlights();
        }

        private void RefreshButtonHighlights()
        {
            if (currentEntries == null) return;

            for (int i = 0; i < spawnedButtons.Count && i < currentEntries.Count; i++)
            {
                var text = spawnedButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (text == null) continue;

                bool isRead = LoreDatabase.Instance != null
                    && LoreDatabase.Instance.IsEntryRead(currentEntries[i].entryId);
                text.color = isRead ? readColor : unreadHighlight;
            }
        }

        private void ClearContent()
        {
            if (entryTitle != null) entryTitle.text = "";
            if (entryAuthor != null) entryAuthor.text = "";
            if (entryClassification != null) entryClassification.text = "";
            if (entryContent != null) entryContent.text = "";
        }

        private void ClearSpawnedButtons()
        {
            foreach (var btn in spawnedButtons)
            {
                if (btn != null) Destroy(btn);
            }
            spawnedButtons.Clear();
        }

        private void Update()
        {
            if (isTypewriting && Input.GetMouseButtonDown(0))
                SkipTypewriter();
        }
    }
}
