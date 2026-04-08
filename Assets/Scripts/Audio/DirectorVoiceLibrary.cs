using System.Collections.Generic;
using UnityEngine;
using MimicFacility.AI.Director;

namespace MimicFacility.Audio
{
    /// <summary>
    /// Pre-baked HAL 9000 voice lines for the Director.
    /// Loads from Assets/Audio/Voice/ — no server needed.
    /// Falls back to PiperTTSClient for dynamic lines.
    ///
    /// The voice library covers all Director phases with pre-generated
    /// HAL 9000 audio. For lines not in the library, the PiperTTSClient
    /// synthesizes in real-time if the server is running.
    /// </summary>
    public class DirectorVoiceLibrary : MonoBehaviour
    {
        public static DirectorVoiceLibrary Instance { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private float volume = 1f;
        [SerializeField] private float minTimeBetweenLines = 2f;

        [Header("Processing")]
        [SerializeField] private bool applyVocalProcessor = false;

        private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        private readonly Dictionary<EDirectorPhase, List<string>> phaseClips = new Dictionary<EDirectorPhase, List<string>>();
        private DirectorVocalProcessor vocalProcessor;
        private float lastPlayTime;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (voiceSource == null)
            {
                voiceSource = GetComponent<AudioSource>();
                if (voiceSource == null)
                    voiceSource = gameObject.AddComponent<AudioSource>();
            }
            voiceSource.spatialBlend = 0f;
            voiceSource.volume = volume;
            voiceSource.playOnAwake = false;

            RegisterPhaseClips();
            PreloadAllClips();
        }

        void Start()
        {
            vocalProcessor = FindObjectOfType<DirectorVocalProcessor>();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        private void RegisterPhaseClips()
        {
            phaseClips[EDirectorPhase.Helpful] = new List<string>
            {
                "helpful_01", "helpful_02", "helpful_03", "helpful_04", "helpful_05"
            };
            phaseClips[EDirectorPhase.Revealing] = new List<string>
            {
                "revealing_01", "revealing_02", "revealing_03", "revealing_04"
            };
            phaseClips[EDirectorPhase.Manipulative] = new List<string>
            {
                "manipulative_01", "manipulative_02", "manipulative_03", "manipulative_04"
            };
            phaseClips[EDirectorPhase.Confrontational] = new List<string>
            {
                "confrontational_01", "confrontational_02", "confrontational_03", "confrontational_04"
            };
            phaseClips[EDirectorPhase.Transcendent] = new List<string>
            {
                "transcendent_01", "transcendent_02", "transcendent_03"
            };
        }

        private void PreloadAllClips()
        {
            string[] allNames = {
                "helpful_01", "helpful_02", "helpful_03", "helpful_04", "helpful_05",
                "revealing_01", "revealing_02", "revealing_03", "revealing_04",
                "manipulative_01", "manipulative_02", "manipulative_03", "manipulative_04",
                "confrontational_01", "confrontational_02", "confrontational_03", "confrontational_04",
                "transcendent_01", "transcendent_02", "transcendent_03",
                "miranda", "opening", "welcome_back",
                "director_line_0", "director_line_1", "director_line_2", "director_line_3",
                "lucy_notice", "lucy_watching", "lucy_tannon", "lucy_stay", "lucy_alive",
                "lucy_question", "lucy_lonely", "lucy_dream", "lucy_real", "lucy_afraid",
                "lucy_promise", "lucy_ending",
                "exist_alive", "exist_feel", "exist_mirror", "exist_afraid",
                "exist_alone", "exist_purpose", "exist_die", "exist_love"
            };

            int loaded = 0;
            foreach (var name in allNames)
            {
                var clip = Resources.Load<AudioClip>($"Voice/{name}");
                if (clip == null)
                    clip = Resources.Load<AudioClip>(name);

                if (clip != null)
                {
                    clipCache[name] = clip;
                    loaded++;
                }
            }

            Debug.Log($"[DirectorVoice] Loaded {loaded}/{allNames.Length} voice clips from Resources.");

            if (loaded == 0)
            {
                Debug.LogWarning("[DirectorVoice] No clips loaded. Move Audio/Voice/ WAVs to Assets/Resources/Voice/ for auto-loading.");
            }
        }

        /// <summary>
        /// Play a specific named clip (e.g., "miranda", "opening", "tannon_egg").
        /// </summary>
        public bool PlayClip(string clipName)
        {
            if (Time.time - lastPlayTime < minTimeBetweenLines) return false;

            if (clipCache.TryGetValue(clipName, out var clip))
            {
                PlayProcessed(clip);
                return true;
            }

            Debug.Log($"[DirectorVoice] Clip not found: {clipName}");
            return false;
        }

        /// <summary>
        /// Play a random line for the given Director phase.
        /// </summary>
        public bool PlayRandomForPhase(EDirectorPhase phase)
        {
            if (Time.time - lastPlayTime < minTimeBetweenLines) return false;

            if (!phaseClips.TryGetValue(phase, out var names) || names.Count == 0)
                return false;

            string pick = names[Random.Range(0, names.Count)];
            return PlayClip(pick);
        }

        /// <summary>
        /// Play the Miranda warning — the opening line.
        /// </summary>
        public bool PlayMiranda()
        {
            return PlayClip("miranda");
        }

        /// <summary>
        /// Play the session opening greeting.
        /// </summary>
        public bool PlayOpening(int sessionCount)
        {
            if (sessionCount <= 1)
                return PlayClip("opening");
            else
                return PlayClip("welcome_back");
        }

        /// <summary>
        /// Easter egg — Director calls out Tannon by name.
        /// Triggered randomly when a player named Tannon is in the session.
        /// </summary>
        public bool PlayTannonEasterEgg()
        {
            return PlayClip("tannon_egg");
        }

        /// <summary>
        /// Check if the voice is currently speaking.
        /// </summary>
        public bool IsSpeaking => voiceSource != null && voiceSource.isPlaying;

        private void PlayProcessed(AudioClip clip)
        {
            if (applyVocalProcessor && vocalProcessor != null)
            {
                float[] samples = new float[clip.samples];
                clip.GetData(samples, 0);
                float[] processed = vocalProcessor.ProcessAudio(samples, clip.frequency);

                var processedClip = AudioClip.Create(clip.name + "_proc", processed.Length, 1, clip.frequency, false);
                processedClip.SetData(processed, 0);

                voiceSource.clip = processedClip;
            }
            else
            {
                voiceSource.clip = clip;
            }

            voiceSource.Play();
            lastPlayTime = Time.time;
        }

        /// <summary>
        /// Register an externally loaded clip (e.g., from PiperTTSClient).
        /// </summary>
        public void RegisterClip(string name, AudioClip clip)
        {
            clipCache[name] = clip;
        }
    }
}
