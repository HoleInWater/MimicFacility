using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
using TMPro;

namespace MimicFacility.UI
{
    public class IntroSequenceController : MonoBehaviour
    {
        [Header("Audio")]
        public AudioClip mainThemeClip;
        public AudioSource musicSource;
        [Range(0f, 1f)] public float musicVolume = 1f;

        [Header("Phase 1 -- Black to Facility Exterior (0s)")]
        public float fadeInDuration = 7f;

        [Header("Phase 2 -- Studio Logo (~7s)")]
        public float logoStartTime = 7f;
        public float logoDuration = 5f;
        public float logoFadeSpeed = 1f;

        [Header("Phase 3 -- Corridor (~20s)")]
        public float corridorStartTime = 20f;

        [Header("Phase 4 -- Control Room (~38s)")]
        public float controlRoomStartTime = 38f;

        [Header("Phase 5 -- MIMIC Title (~52s)")]
        public float titleDropTime = 60f;
        public float titleDrawDuration = 3f;
        public float postTitleHold = 5f;

        [Header("Scene References")]
        public CanvasGroup blackOverlay;
        public GameObject facilityExteriorScene;
        public ParticleSystem sporeParticles;
        public ParticleSystem fogParticles;

        [Header("Logo")]
        public CanvasGroup studioLogoGroup;

        [Header("Scare")]
        public ScaryScreenFlash scaryScreen;
        public float scareTime = 18f;

        [Header("Corridor")]
        public GameObject corridorScene;

        [Header("Control Room")]
        public GameObject controlRoomScene;

        [Header("Camera")]
        public IntroCameraController cameraController;

        [Header("Title")]
        public CanvasGroup titleGroup;
        public TextMeshProUGUI titleText;

        [Header("Glitch Wipe")]
        public RectTransform glitchWipePanel;
        public float glitchWipeDuration = 0.8f;

        [Header("Credits")]
        public TextMeshProUGUI creditText;
        public CanvasGroup creditTextGroup;
        public float creditFadeTime = 1f;
        public float creditHoldTime = 3f;

        public List<CreditLine> creditLines = new List<CreditLine>
        {
            new CreditLine { time = 22f, text = "A HoleInWater Production" },
            new CreditLine { time = 26f, text = "Lead Manager — Garrett\nCo-Leader — Ezra" },
            new CreditLine { time = 31f, text = "Creative Director & Lead Developer\nLandon Adams" },
            new CreditLine { time = 36f, text = "Music by Malakai Probert" },
            new CreditLine { time = 40f, text = "Section Leaders\nDeegan  —  Lori" },
            new CreditLine { time = 44f, text = "Developer & QA — Tannon Thompson\nDavid  —  Nora" },
            new CreditLine { time = 49f, text = "\"Everything the AI did to you,\nit learned by watching you do it to each other.\"" },
            new CreditLine { time = 55f, text = "HoleInWater\nproudly presents" },
        };

        [Serializable]
        public class CreditLine
        {
            public float time;
            [TextArea] public string text;
        }

        [Header("Transition")]
        public string nextSceneName = "MainMenu";
        public bool allowSkip = true;

        private float sequenceTime;
        private bool sequenceComplete;
        private bool isSkipping;
        private bool phase2Triggered, phase3Triggered, phase4Triggered, phase5Triggered, scareFired;
        private int nextCreditIndex;
        private Coroutine activeCreditCoroutine;

        void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            SetAlpha(blackOverlay, 1f);
            SetAlpha(studioLogoGroup, 0f);
            SetAlpha(titleGroup, 0f);
            SetAlpha(creditTextGroup, 0f);

            if (facilityExteriorScene != null) facilityExteriorScene.SetActive(true);
            if (sporeParticles != null) sporeParticles.Play();
            if (fogParticles != null) fogParticles.Play();
            if (corridorScene != null) corridorScene.SetActive(false);
            if (controlRoomScene != null) controlRoomScene.SetActive(false);

            if (musicSource != null && mainThemeClip != null)
            {
                musicSource.clip = mainThemeClip;
                musicSource.volume = musicVolume;
                musicSource.loop = false;
                musicSource.Play();
            }

            sequenceTime = 0f;
            nextCreditIndex = 0;
        }

        void Update()
        {
            if (sequenceComplete) return;

            if (allowSkip && !isSkipping &&
                (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)))
            {
                SkipSequence();
                return;
            }

            sequenceTime += Time.deltaTime;

            // Phase 1: fade from black
            if (sequenceTime <= fadeInDuration && blackOverlay != null)
                blackOverlay.alpha = 1f - (sequenceTime / fadeInDuration);
            else if (blackOverlay != null && blackOverlay.alpha > 0.001f)
                blackOverlay.alpha = 0f;

            // Phase 2: studio logo
            if (!phase2Triggered && sequenceTime >= logoStartTime)
            {
                phase2Triggered = true;
                StartCoroutine(PlayLogo());
            }

            // Scare flash — monster face on black between logo and corridor
            if (!scareFired && sequenceTime >= scareTime && scaryScreen != null)
            {
                scareFired = true;
                scaryScreen.TriggerScare();
            }

            // Phase 3: corridor
            if (!phase3Triggered && sequenceTime >= corridorStartTime)
            {
                phase3Triggered = true;
                CutToCorridor();
            }

            // Phase 4: control room
            if (!phase4Triggered && sequenceTime >= controlRoomStartTime)
            {
                phase4Triggered = true;
                CutToControlRoom();
            }

            // Phase 5: MIMIC title
            if (!phase5Triggered && sequenceTime >= titleDropTime)
            {
                phase5Triggered = true;
                StartCoroutine(DropTitle());
            }

            TickCredits();
        }

        IEnumerator PlayLogo()
        {
            yield return Fade(studioLogoGroup, 0f, 1f, logoFadeSpeed);
            yield return new WaitForSeconds(logoDuration);
            yield return Fade(studioLogoGroup, 1f, 0f, logoFadeSpeed);
        }

        void CutToCorridor()
        {
            StartCoroutine(FadeTransition(() =>
            {
                if (facilityExteriorScene != null) facilityExteriorScene.SetActive(false);
                if (corridorScene != null) corridorScene.SetActive(true);
                if (cameraController != null)
                    cameraController.SetPhase(IntroCameraController.Phase.Corridor);
            }));
        }

        void CutToControlRoom()
        {
            StartCoroutine(FadeTransition(() =>
            {
                if (corridorScene != null) corridorScene.SetActive(false);
                if (controlRoomScene != null) controlRoomScene.SetActive(true);
                if (cameraController != null)
                    cameraController.SetPhase(IntroCameraController.Phase.ControlRoom);
            }));
        }

        IEnumerator DropTitle()
        {
            if (cameraController != null)
                cameraController.SetPhase(IntroCameraController.Phase.TitleHold);

            if (activeCreditCoroutine != null)
                StopCoroutine(activeCreditCoroutine);
            yield return Fade(creditTextGroup, creditTextGroup != null ? creditTextGroup.alpha : 0f, 0f, 0.3f);

            if (titleText != null)
            {
                titleText.text = "";
                StartCoroutine(TypewriterTitle("MIMIC", titleDrawDuration));
            }

            yield return Fade(titleGroup, 0f, 1f, titleDrawDuration);

            if (cameraController != null) cameraController.TriggerShake(3f);

            yield return new WaitForSeconds(postTitleHold);
            yield return GlitchWipe();
            TransitionOut();
        }

        IEnumerator TypewriterTitle(string text, float duration)
        {
            if (titleText == null) yield break;
            float perChar = duration / text.Length;
            titleText.text = "";
            for (int i = 0; i < text.Length; i++)
            {
                titleText.text += text[i];
                yield return new WaitForSeconds(perChar);
            }
        }

        IEnumerator GlitchWipe()
        {
            if (glitchWipePanel == null)
            {
                if (blackOverlay != null) blackOverlay.alpha = 1f;
                yield break;
            }

            float screenWidth = 1920f;
            glitchWipePanel.gameObject.SetActive(true);
            glitchWipePanel.anchoredPosition = new Vector2(-screenWidth * 1.5f, 0f);

            float elapsed = 0f;
            while (elapsed < glitchWipeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / glitchWipeDuration;
                float eased = 1f - (1f - t) * (1f - t);
                float x = Mathf.Lerp(-screenWidth * 1.5f, screenWidth * 0.5f, eased);

                // Glitch: random vertical offset during wipe
                float glitchY = Random.Range(-5f, 5f) * (1f - t);
                glitchWipePanel.anchoredPosition = new Vector2(x, glitchY);
                yield return null;
            }

            glitchWipePanel.anchoredPosition = Vector2.zero;
            yield return new WaitForSeconds(0.3f);
        }

        IEnumerator FadeTransition(Action swapScenes, float fadeDuration = 1f)
        {
            yield return Fade(blackOverlay, blackOverlay != null ? blackOverlay.alpha : 0f, 1f, fadeDuration);
            swapScenes?.Invoke();
            yield return new WaitForSeconds(0.2f);
            yield return Fade(blackOverlay, 1f, 0f, fadeDuration);
        }

        void TickCredits()
        {
            if (creditLines == null || nextCreditIndex >= creditLines.Count) return;
            while (nextCreditIndex < creditLines.Count && sequenceTime >= creditLines[nextCreditIndex].time)
            {
                string text = creditLines[nextCreditIndex].text;
                nextCreditIndex++;
                if (activeCreditCoroutine != null) StopCoroutine(activeCreditCoroutine);
                activeCreditCoroutine = StartCoroutine(ShowCreditLine(text));
            }
        }

        IEnumerator ShowCreditLine(string text)
        {
            if (creditText == null || creditTextGroup == null) yield break;
            if (creditTextGroup.alpha > 0.01f)
                yield return Fade(creditTextGroup, creditTextGroup.alpha, 0f, creditFadeTime * 0.4f);
            creditText.text = text;
            yield return Fade(creditTextGroup, 0f, 1f, creditFadeTime);
            yield return new WaitForSeconds(creditHoldTime);
            yield return Fade(creditTextGroup, 1f, 0f, creditFadeTime);
        }

        void TransitionOut()
        {
            if (sequenceComplete) return;
            sequenceComplete = true;
            StartCoroutine(FadeToBlackAndLoad());
        }

        void SkipSequence()
        {
            if (isSkipping) return;
            isSkipping = true;
            sequenceComplete = true;
            if (musicSource != null)
                StartCoroutine(FadeAudio(musicSource, 0f, 0.8f));
            StartCoroutine(FadeToBlackAndLoad());
        }

        IEnumerator FadeToBlackAndLoad()
        {
            if (blackOverlay != null)
                yield return Fade(blackOverlay, blackOverlay.alpha, 1f, 1.5f);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;
            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            group.alpha = to;
        }

        IEnumerator FadeAudio(AudioSource source, float target, float duration)
        {
            float start = source.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            source.volume = target;
        }

        void SetAlpha(CanvasGroup group, float a)
        {
            if (group != null) group.alpha = a;
        }
    }
}
