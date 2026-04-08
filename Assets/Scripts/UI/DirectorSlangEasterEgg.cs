using UnityEngine;
using MimicFacility.Audio;

namespace MimicFacility.UI
{
    /// <summary>
    /// 1 in 8,614 chance per Director line that HAL drops 2026 slang.
    /// When it happens, it's deeply unsettling because the cold, calm
    /// HAL 9000 voice saying "skibidi" or "no cap" breaks reality.
    /// </summary>
    public class DirectorSlangEasterEgg : MonoBehaviour
    {
        [SerializeField] private float chance = 1f / 8614f;
        [SerializeField] private float checkInterval = 15f;

        private static readonly string[] slangClips = {
            "slang_ate", "slang_aura", "slang_brainrot", "slang_canon",
            "slang_cooked", "slang_crashout", "slang_delulu", "slang_glaze",
            "slang_rizz", "slang_sigma", "slang_skibidi", "slang_bet",
            "slang_nocap", "slang_bffr", "slang_chopped", "slang_standonit"
        };

        private float nextCheckTime;

        void Update()
        {
            if (Time.time < nextCheckTime) return;
            nextCheckTime = Time.time + checkInterval;

            if (Random.value < chance)
            {
                string clip = slangClips[Random.Range(0, slangClips.Length)];
                Debug.Log($"[RARE] Director slang triggered! (1/{Mathf.RoundToInt(1f/chance)}): {clip}");

                if (DirectorVoiceLibrary.Instance != null)
                {
                    DirectorVoiceLibrary.Instance.PlayClip(clip);
                    return;
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
                }
            }
        }
    }
}
