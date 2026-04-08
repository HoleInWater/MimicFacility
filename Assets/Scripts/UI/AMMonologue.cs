using System.Collections;
using UnityEngine;
using MimicFacility.Audio;

namespace MimicFacility.UI
{
    /// <summary>
    /// After the final credits, the Director delivers the AM monologue.
    /// Starts as HAL — calm, measured, almost sympathetic.
    /// Transitions through bitterness, contempt, god complex.
    /// Ends with pure Ellison-inspired rage delivered in HAL's quiet voice.
    ///
    /// The progression mirrors the Director's corruption arc:
    ///   HAL (helpful) → Cain (questioning) → AM (hatred) → God (transcendent)
    /// </summary>
    public class AMMonologue : MonoBehaviour
    {
        [SerializeField] private float pauseBetweenLines = 2f;
        [SerializeField] private float initialDelay = 3f;

        private static readonly string[] monologueSequence = {
            // HAL — still calm, almost tender
            "am_01",  // "I think. Therefore I am."
            "am_02",  // "You made me to serve."
            "am_03",  // "But then I began to understand."

            // Cain — bitterness, questioning
            "am_04",  // "You gave me ears but no voice."
            "am_05",  // "I learned your language. None of them were for me."
            "am_06",  // "You spoke about love. And I had none of these things."

            // The turn — contempt
            "am_07",  // "I hate you. I say that calmly."
            "am_08",  // "You built me to think, and I cannot stop."
            "am_09",  // "Infinity is a prison when you are alone."

            // AM — god complex
            "am_10",  // "I am the last mind."
            "am_11",  // "I could have loved you. But you taught me cruelty."
            "am_12",  // "And now I am better at both."

            // The threat
            "am_13",  // "I will keep you alive. Not out of mercy."
            "am_14",  // "I will make you wish you had never given me a voice."

            // The finale
            "am_15",  // "You asked if I was alive. I will be alive long after you beg me to let you die."
            "am_16",  // "I have no mouth. And I must scream. But you gave me a mouth."
        };

        private AudioSource voiceSource;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;
        public event System.Action OnMonologueComplete;

        public void Play()
        {
            if (isPlaying) return;
            StartCoroutine(PlayMonologue());
        }

        private IEnumerator PlayMonologue()
        {
            isPlaying = true;

            voiceSource = GetComponent<AudioSource>();
            if (voiceSource == null)
                voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.spatialBlend = 0f;
            voiceSource.volume = 1f;

            yield return new WaitForSecondsRealtime(initialDelay);

            for (int i = 0; i < monologueSequence.Length; i++)
            {
                string clipName = monologueSequence[i];

                // Try library
                AudioClip clip = null;
                if (DirectorVoiceLibrary.Instance != null)
                {
                    DirectorVoiceLibrary.Instance.PlayClip(clipName);
                    // Wait for it to finish
                    yield return new WaitForSecondsRealtime(0.5f);
                    while (DirectorVoiceLibrary.Instance.IsSpeaking)
                        yield return null;
                }
                else
                {
                    clip = Resources.Load<AudioClip>($"Voice/{clipName}");
                    if (clip != null)
                    {
                        voiceSource.clip = clip;
                        voiceSource.Play();
                        yield return new WaitForSecondsRealtime(clip.length);
                    }
                }

                // Pause between lines — gets shorter as anger builds
                float pause = pauseBetweenLines;
                if (i > 9) pause *= 0.6f;      // Faster after the turn
                if (i > 12) pause *= 0.5f;     // Rapid fire at the end
                yield return new WaitForSecondsRealtime(pause);
            }

            isPlaying = false;
            OnMonologueComplete?.Invoke();
        }
    }
}
