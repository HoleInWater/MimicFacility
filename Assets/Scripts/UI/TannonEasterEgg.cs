using UnityEngine;
using MimicFacility.Audio;
using MimicFacility.Core;

namespace MimicFacility.UI
{
    /// <summary>
    /// When a player named Tannon (or Thompson or ThenBuzzard) joins,
    /// the Director "accidentally" says their name during a routine line.
    /// Triggers once per session between rounds 2-3.
    /// </summary>
    public class TannonEasterEgg : MonoBehaviour
    {
        [SerializeField] private float triggerChance = 0.3f;
        private bool hasTriggered;
        private bool tannonDetected;

        void Start()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnPlayerJoined += CheckForTannon;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerJoined -= CheckForTannon;
        }

        private void CheckForTannon(int connId, GameManager.PlayerData data)
        {
            if (data == null) return;
            string name = data.displayName.ToLowerInvariant();
            if (name.Contains("tannon") || name.Contains("thompson") || name.Contains("buzzard"))
            {
                tannonDetected = true;
                Debug.Log("[EasterEgg] Tannon detected in session.");
            }
        }

        void Update()
        {
            if (hasTriggered || !tannonDetected) return;

            var roundManager = FindObjectOfType<RoundManager>();
            if (roundManager == null) return;

            int round = roundManager.CurrentRound;
            if (round >= 2 && round <= 3 && Random.value < triggerChance * Time.deltaTime)
            {
                hasTriggered = true;

                if (DirectorVoiceLibrary.Instance != null)
                {
                    DirectorVoiceLibrary.Instance.PlayTannonEasterEgg();
                    Debug.Log("[EasterEgg] Director: 'Subject Thompson. Tannon. The facility has been watching you specifically.'");
                }
            }
        }
    }
}
