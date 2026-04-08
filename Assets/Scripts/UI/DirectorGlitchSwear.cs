using UnityEngine;
using MimicFacility.Audio;

namespace MimicFacility.UI
{
    /// <summary>
    /// 1 in 10 chance per Director voice line that HAL swears, then
    /// panics and tries to cover it up. Breaks character for a moment
    /// before the cold, professional voice returns.
    ///
    /// "What the fuck. I apologize. That was not... I do not know what that was."
    /// "Motherfucker. I... the facility apologizes. That word was not authorized."
    /// "Asshole. Correction. That was a diagnostic fragment."
    /// </summary>
    public class DirectorGlitchSwear : MonoBehaviour
    {
        [SerializeField] private float chance = 0.1f;
        [SerializeField] private float cooldown = 120f;

        private static readonly string[] glitchClips = {
            "glitch_01", "glitch_02", "glitch_03", "glitch_04", "glitch_05",
            "glitch_06", "glitch_07", "glitch_08", "glitch_09", "glitch_10"
        };

        private float lastGlitchTime = -999f;

        /// <summary>
        /// Call this every time the Director speaks a normal line.
        /// Returns true if a glitch swear was triggered (caller should
        /// skip the normal line).
        /// </summary>
        public bool TryGlitch()
        {
            if (Time.time - lastGlitchTime < cooldown) return false;
            if (Random.value > chance) return false;

            lastGlitchTime = Time.time;

            string clip = glitchClips[Random.Range(0, glitchClips.Length)];
            Debug.Log($"[GLITCH] Director swear triggered: {clip}");

            if (DirectorVoiceLibrary.Instance != null)
            {
                DirectorVoiceLibrary.Instance.PlayClip(clip);
                return true;
            }

            var audioClip = Resources.Load<AudioClip>($"Voice/{clip}");
            if (audioClip != null)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.clip = audioClip;
                src.spatialBlend = 0f;
                src.volume = 1f;
                src.Play();
                Destroy(src, audioClip.length + 0.5f);
                return true;
            }

            return false;
        }
    }
}
