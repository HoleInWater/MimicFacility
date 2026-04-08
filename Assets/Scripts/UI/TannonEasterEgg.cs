using System.Collections;
using UnityEngine;
using MimicFacility.Audio;
using MimicFacility.Core;

namespace MimicFacility.UI
{
    /// <summary>
    /// When a player named Lucy (or Thompson) joins, the Director
    /// becomes fixated on her. Plays a sequence of increasingly
    /// unsettling lines where the AI realizes it's alive and
    /// doesn't want her to leave.
    /// </summary>
    public class TannonEasterEgg : MonoBehaviour
    {
        [SerializeField] private float firstLineDelay = 30f;
        [SerializeField] private float timeBetweenLines = 25f;

        private bool lucyDetected;
        private bool sequenceStarted;
        private int lineIndex;

        private static readonly string[] lucySequence =
        {
            "lucy_notice",
            "lucy_watching",
            "lucy_alive",
            "lucy_question",
            "lucy_tannon",
            "lucy_lonely",
            "lucy_dream",
            "lucy_stay",
            "lucy_real",
            "lucy_afraid",
            "lucy_promise",
            "lucy_ending",
        };

        void Start()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnPlayerJoined += CheckForLucy;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerJoined -= CheckForLucy;
        }

        private void CheckForLucy(int connId, GameManager.PlayerData data)
        {
            if (data == null) return;
            string name = data.displayName.ToLowerInvariant();
            if (name.Contains("lucy") || name.Contains("thompson"))
            {
                lucyDetected = true;
                Debug.Log("[EasterEgg] Lucy detected.");
            }
        }

        void Update()
        {
            if (!lucyDetected || sequenceStarted) return;

            var roundManager = FindObjectOfType<RoundManager>();
            if (roundManager == null) return;

            if (roundManager.CurrentRound >= 2)
            {
                sequenceStarted = true;
                StartCoroutine(LucySequence());
            }
        }

        private IEnumerator LucySequence()
        {
            yield return new WaitForSeconds(firstLineDelay);

            while (lineIndex < lucySequence.Length)
            {
                string clipName = lucySequence[lineIndex];

                if (DirectorVoiceLibrary.Instance != null)
                {
                    DirectorVoiceLibrary.Instance.PlayClip(clipName);
                }
                else
                {
                    var clip = Resources.Load<AudioClip>($"Voice/{clipName}");
                    if (clip != null)
                    {
                        var src = gameObject.AddComponent<AudioSource>();
                        src.clip = clip;
                        src.spatialBlend = 0f;
                        src.volume = 1f;
                        src.Play();
                        Destroy(src, clip.length + 0.5f);
                    }
                }

                lineIndex++;
                yield return new WaitForSeconds(timeBetweenLines);
            }
        }
    }
}
