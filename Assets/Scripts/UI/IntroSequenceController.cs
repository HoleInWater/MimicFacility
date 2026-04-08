using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
using TMPro;
using MimicFacility.Audio;

namespace MimicFacility.UI
{
    public class IntroSequenceController : MonoBehaviour
    {
        [Header("Audio")]
        public AudioClip mainThemeClip;
        public AudioSource musicSource;
        [Range(0f, 1f)] public float musicVolume = 1f;

        [Header("Phase 1 -- Black to Facility Exterior (0-5s)")]
        [Tooltip("Computer wakes up — warbling electronic tone. Black lifts to reveal facility.")]
        public float fadeInDuration = 5f;

        [Header("Phase 2 -- Studio Logo (~5s)")]
        [Tooltip("'Daisy...' — the voice begins. Crimson Blade logo over exterior.")]
        public float logoStartTime = 5f;
        public float logoDuration = 5f;
        public float logoFadeSpeed = 1.5f;

        [Header("Phase 3 -- Corridor (~22s)")]
        [Tooltip("'I'm half crazy' — cut to corridor as the melody becomes clear but alien.")]
        public float corridorStartTime = 22f;

        [Header("Phase 4 -- Control Room (~50s)")]
        [Tooltip("'It won't be a stylish marriage' — the computer tries to be human. Director's domain.")]
        public float controlRoomStartTime = 50f;

        [Header("Phase 5 -- INTAKE Title (~82s)")]
        [Tooltip("'Upon the seat' — building to finale. Title slams in.")]
        public float titleDropTime = 82f;
        public float titleDrawDuration = 2f;
        public float postTitleHold = 6f;

        [Header("Scene References")]
        public CanvasGroup blackOverlay;
        public GameObject facilityExteriorScene;
        public ParticleSystem sporeParticles;
        public ParticleSystem fogParticles;

        [Header("Logo")]
        public CanvasGroup studioLogoGroup;

        [Header("Scare")]
        public ScaryScreenFlash scaryScreen;
        public float scareTime = 20f;

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
            // Credits synced to Daisy Bell (1:51)
            // "Daisy, Daisy, give me your answer do"
            new CreditLine { time = 24f, text = "A Crimson Blade Interactive Production" },
            // "I'm half crazy, all for the love of you"
            new CreditLine { time = 30f, text = "Creative Director & Lead Developer\nLandon Adams" },
            // Melody becoming clear
            new CreditLine { time = 37f, text = "Lead Manager — Garrett\nCo-Leader — Ezra" },
            // "It won't be a stylish marriage"
            new CreditLine { time = 43f, text = "Music by Malakai Probert" },
            // "I can't afford a carriage"
            new CreditLine { time = 52f, text = "Section Leaders\nDeegan  —  Lori" },
            // Voice getting more warped
            new CreditLine { time = 58f, text = "Developer & QA — Tannon Thompson\nDavid  —  Nora" },
            // "But you'll look sweet"
            new CreditLine { time = 65f, text = "\"Everything the AI did to you,\nit learned by watching you do it to each other.\"" },
            // "Upon the seat" — building to finale
            new CreditLine { time = 75f, text = "Crimson Blade Interactive\nproudly presents" },
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

        [Header("Beat Sync")]
        [Tooltip("Lights in the scene to flicker with the music")]
        public Light[] sceneLights;
        public float beatDetectThreshold = 0.05f;
        public float beatCooldown = 0.3f;

        private float sequenceTime;
        private bool sequenceComplete;
        private bool isSkipping;
        private bool phase2Triggered, phase3Triggered, phase4Triggered, phase5Triggered, scareFired;
        private int nextCreditIndex;
        private Coroutine activeCreditCoroutine;
        private float lastBeatTime;
        private float[] spectrumData = new float[256];
        private float prevBass;
        private bool isTransitioning;

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

            // Debug: Right Alt plays a random HAL voice line
            if (Input.GetKeyDown(KeyCode.RightAlt))
            {
                PlayRandomHALLine();
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

            // Phase 5: INTAKE title
            if (!phase5Triggered && sequenceTime >= titleDropTime)
            {
                phase5Triggered = true;
                StartCoroutine(DropTitle());
            }

            TickCredits();
            TickBeatSync();
            TickRandomFlicker();
        }

        void TickBeatSync()
        {
            if (musicSource == null || !musicSource.isPlaying) return;

            musicSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

            // Bass energy — average of low frequency bins (0-8)
            float bass = 0f;
            for (int i = 0; i < 8; i++)
                bass += spectrumData[i];
            bass /= 8f;

            // Beat detection — spike in bass energy
            float delta = bass - prevBass;
            prevBass = bass;

            if (delta > beatDetectThreshold && Time.time - lastBeatTime > beatCooldown)
            {
                lastBeatTime = Time.time;
                OnBeat(bass * 10f);
            }

            // Ambient light pulse from overall energy
            float energy = 0f;
            for (int i = 0; i < 64; i++)
                energy += spectrumData[i];

            if (sceneLights != null)
            {
                foreach (var light in sceneLights)
                {
                    if (light == null) continue;
                    light.intensity = Mathf.Lerp(light.intensity, 0.5f + energy * 20f, Time.deltaTime * 8f);
                }
            }
        }

        void OnBeat(float intensity)
        {
            // Camera pulse
            if (cameraController != null)
                cameraController.TriggerBeatPulse();

            // Light flicker on beat
            if (sceneLights != null)
            {
                foreach (var light in sceneLights)
                {
                    if (light != null)
                        light.intensity = 2f + intensity;
                }
            }
        }

        private float nextFlickerTime;

        void TickRandomFlicker()
        {
            // Don't flicker during scene transitions or before corridor
            if (isTransitioning) return;
            if (sequenceTime < corridorStartTime + 3f || sequenceTime > titleDropTime - 3f) return;

            if (Time.time < nextFlickerTime) return;

            // Random interval — more frequent as we approach the title drop
            float distToTitle = titleDropTime - sequenceTime;
            float maxInterval = Mathf.Lerp(1f, 5f, distToTitle / 40f);
            nextFlickerTime = Time.time + Random.Range(0.5f, maxInterval);

            StartCoroutine(QuickFlicker());
        }

        IEnumerator QuickFlicker()
        {
            if (blackOverlay == null) yield break;

            int flicks = Random.Range(1, 4);
            for (int i = 0; i < flicks; i++)
            {
                blackOverlay.alpha = Random.Range(0.1f, 0.5f);
                yield return new WaitForSecondsRealtime(Random.Range(0.02f, 0.06f));
                blackOverlay.alpha = 0f;
                yield return new WaitForSecondsRealtime(Random.Range(0.03f, 0.1f));
            }

            // Occasional hard flash
            if (Random.value < 0.2f)
            {
                blackOverlay.alpha = 0.8f;
                yield return new WaitForSecondsRealtime(0.03f);
                blackOverlay.alpha = 0f;
            }
        }

        private static readonly string[] debugVoiceClips = {
            "miranda", "opening", "helpful_01", "helpful_02", "revealing_01",
            "revealing_03", "manipulative_01", "manipulative_02",
            "confrontational_01", "confrontational_03", "transcendent_01",
            "transcendent_03", "tannon_egg", "welcome_back"
        };
        private int debugClipIndex;

        void PlayRandomHALLine()
        {
            if (DirectorVoiceLibrary.Instance != null)
            {
                string clipName = debugVoiceClips[debugClipIndex % debugVoiceClips.Length];
                debugClipIndex++;
                bool played = DirectorVoiceLibrary.Instance.PlayClip(clipName);
                Debug.Log($"[Debug] HAL voice: {clipName} (played: {played})");
                return;
            }

            // Fallback: load directly
            string name = debugVoiceClips[debugClipIndex % debugVoiceClips.Length];
            debugClipIndex++;
            var clip = Resources.Load<AudioClip>($"Voice/{name}");
            if (clip != null)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.clip = clip;
                src.spatialBlend = 0f;
                src.volume = 0.9f;
                src.Play();
                Destroy(src, clip.length + 0.5f);
                Debug.Log($"[Debug] HAL voice (direct): {name}");
            }
            else
            {
                Debug.LogWarning($"[Debug] Voice clip not found: Voice/{name}");
            }
        }

        IEnumerator TannonEasterEggDelayed()
        {
            yield return new WaitForSecondsRealtime(3f);

            // Try the voice library first
            if (DirectorVoiceLibrary.Instance != null)
            {
                DirectorVoiceLibrary.Instance.PlayTannonEasterEgg();
                yield break;
            }

            // Fallback: load and play directly from Resources
            var clip = Resources.Load<AudioClip>("Voice/tannon_egg");
            if (clip != null && musicSource != null)
            {
                var tempSource = gameObject.AddComponent<AudioSource>();
                tempSource.clip = clip;
                tempSource.spatialBlend = 0f;
                tempSource.volume = 0.9f;
                tempSource.Play();
                Destroy(tempSource, clip.length + 0.5f);
            }
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

            // Kill credits
            if (activeCreditCoroutine != null)
                StopCoroutine(activeCreditCoroutine);
            yield return Fade(creditTextGroup, creditTextGroup != null ? creditTextGroup.alpha : 0f, 0f, 0.3f);

            // ── Stage 1: Sudden blackout ──────────────────────────────
            if (blackOverlay != null)
            {
                blackOverlay.alpha = 1f;
                yield return new WaitForSecondsRealtime(0.6f);
            }

            // ── Stage 2: Static burst — rapid black/white flicker ─────
            if (blackOverlay != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    blackOverlay.alpha = Random.Range(0.3f, 1f);
                    yield return new WaitForSecondsRealtime(0.04f);
                    blackOverlay.alpha = Random.Range(0f, 0.2f);
                    yield return new WaitForSecondsRealtime(0.03f);
                }
                blackOverlay.alpha = 1f;
                yield return new WaitForSecondsRealtime(0.15f);
            }

            // ── Stage 3: Title SLAMS in at full opacity ───────────────
            if (titleGroup != null) titleGroup.alpha = 1f;

            // Trigger the MimicTitleRenderer draw animation
            var titleRenderer = titleGroup != null
                ? titleGroup.GetComponentInChildren<MimicTitleRenderer>()
                : null;
            if (titleRenderer != null)
                titleRenderer.StartDrawing(titleDrawDuration);

            // Smash cut — black drops away instantly
            if (blackOverlay != null)
                blackOverlay.alpha = 0f;

            // Heavy camera shake on impact
            if (cameraController != null)
                cameraController.TriggerShake(8f);

            yield return new WaitForSecondsRealtime(0.1f);

            // ── Stage 4: Screen flicker — title strobes ───────────────
            if (titleGroup != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    titleGroup.alpha = 0f;
                    yield return new WaitForSecondsRealtime(0.05f);
                    titleGroup.alpha = 1f;
                    yield return new WaitForSecondsRealtime(Random.Range(0.08f, 0.2f));
                }
            }

            // ── Stage 5: Second camera shake (aftershock) ─────────────
            if (cameraController != null)
                cameraController.TriggerShake(4f);

            yield return new WaitForSecondsRealtime(0.5f);

            // ── Stage 6: Subtitle fades in slowly ─────────────────────
            // Find subtitle by name
            if (titleGroup != null)
            {
                var subtitle = titleGroup.transform.Find("Subtitle");
                if (subtitle != null)
                {
                    var subCG = subtitle.GetComponent<CanvasGroup>();
                    if (subCG == null) subCG = subtitle.gameObject.AddComponent<CanvasGroup>();
                    subCG.alpha = 0f;
                    yield return Fade(subCG, 0f, 1f, 2f);
                }
            }

            // ── Stage 7: Hold — let it breathe ────────────────────────
            yield return new WaitForSecondsRealtime(postTitleHold);

            // ── Stage 8: One final flicker before transition ──────────
            if (titleGroup != null)
            {
                titleGroup.alpha = 0f;
                yield return new WaitForSecondsRealtime(0.1f);
                titleGroup.alpha = 1f;
                yield return new WaitForSecondsRealtime(0.3f);
                titleGroup.alpha = 0f;
                yield return new WaitForSecondsRealtime(0.15f);
                titleGroup.alpha = 1f;
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // ── Stage 9: Glitch wipe out ──────────────────────────────
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
            isTransitioning = true;
            yield return Fade(blackOverlay, blackOverlay != null ? blackOverlay.alpha : 0f, 1f, fadeDuration);
            swapScenes?.Invoke();
            yield return new WaitForSeconds(0.2f);
            yield return Fade(blackOverlay, 1f, 0f, fadeDuration);
            isTransitioning = false;
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
