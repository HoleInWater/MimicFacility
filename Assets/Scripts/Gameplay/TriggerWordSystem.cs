using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Entities;
using MimicFacility.AI.Voice;
using MimicFacility.Audio;
using MimicFacility.Characters;

namespace MimicFacility.Gameplay
{
    /// <summary>
    /// When a player speaks a trigger word near a mimic, the mimic reproduces.
    /// This is the core tension mechanic — your own speech patterns become the enemy.
    /// The words you say most often are the words that will split them in two.
    /// </summary>
    public class TriggerWordSystem : NetworkBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float mimicEarshotRange = 15f;
        [SerializeField] private float reproductionDelay = 2f;
        [SerializeField] private float reproductionCooldown = 30f;
        [SerializeField] private int warningRoundThreshold = 2;

        [Header("Effects")]
        [SerializeField] private GameObject reproductionParticlePrefab;
        [SerializeField] private GameObject mimicPrefab;

        [Header("References")]
        [SerializeField] private VoiceLearningSystem voiceLearningSystem;
        [SerializeField] private RoundManager roundManager;

        // Trigger words per player — the words that will cause reproduction
        private readonly Dictionary<string, List<string>> triggerWords = new Dictionary<string, List<string>>();

        // Cooldown tracking — prevents spam reproduction from the same player
        private readonly Dictionary<string, float> lastReproductionTime = new Dictionary<string, float>();

        // Active reproduction coroutines — prevents double-triggering on the same mimic
        private readonly HashSet<uint> reproducingMimics = new HashSet<uint>();

        public override void OnStartServer()
        {
            if (voiceLearningSystem == null)
                voiceLearningSystem = FindObjectOfType<VoiceLearningSystem>();
            if (roundManager == null)
                roundManager = FindObjectOfType<RoundManager>();
        }

        /// <summary>
        /// Register trigger words for a player. Called by VoiceLearningSystem
        /// after it selects trigger words based on speech patterns.
        /// </summary>
        [Server]
        public void SetTriggerWords(string playerId, List<string> words)
        {
            if (string.IsNullOrEmpty(playerId) || words == null) return;
            triggerWords[playerId] = new List<string>(words);
            Debug.Log($"[TriggerWord] Set {words.Count} trigger words for {playerId}: {string.Join(", ", words)}");
        }

        /// <summary>
        /// Returns the trigger words assigned to this player.
        /// </summary>
        public List<string> GetTriggerWords(string playerId)
        {
            if (triggerWords.TryGetValue(playerId, out var words))
                return new List<string>(words);
            return new List<string>();
        }

        /// <summary>
        /// Scans spoken text for any trigger word belonging to this player.
        /// If a trigger word is detected and a mimic is within earshot, reproduction begins.
        /// </summary>
        [Server]
        public void CheckForTriggerWord(string playerId, string spokenText)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(spokenText)) return;
            if (!triggerWords.ContainsKey(playerId)) return;

            string lower = spokenText.ToLowerInvariant();
            string matchedWord = null;

            foreach (string trigger in triggerWords[playerId])
            {
                if (lower.Contains(trigger))
                {
                    matchedWord = trigger;
                    break;
                }
            }

            if (matchedWord == null)
            {
                // Check for near-miss warning (first syllable match)
                CheckTriggerWordWarning(playerId, lower);
                return;
            }

            // Cooldown check
            if (lastReproductionTime.TryGetValue(playerId, out float lastTime))
            {
                if (Time.time - lastTime < reproductionCooldown) return;
            }

            // Find the speaking player's position
            Vector3? speakerPosition = GetPlayerPosition(playerId);
            if (!speakerPosition.HasValue) return;

            // Find nearest mimic within earshot
            MimicBase nearestMimic = FindNearestMimicInRange(speakerPosition.Value, mimicEarshotRange);
            if (nearestMimic == null) return;
            if (reproducingMimics.Contains(nearestMimic.netId)) return;

            // Trigger reproduction
            lastReproductionTime[playerId] = Time.time;
            StartCoroutine(ReproductionSequence(nearestMimic, playerId, matchedWord));
        }

        /// <summary>
        /// Checks if the spoken text nearly matches a trigger word (first syllable).
        /// Flashes a warning on the player's HUD, but only after the warning round threshold.
        /// </summary>
        [Server]
        private void CheckTriggerWordWarning(string playerId, string spokenLower)
        {
            if (roundManager == null) return;
            if (roundManager.CurrentRound < warningRoundThreshold) return;
            if (!triggerWords.ContainsKey(playerId)) return;

            foreach (string trigger in triggerWords[playerId])
            {
                if (trigger.Length < 3) continue;

                // Extract first syllable approximation (first 3 chars or up to first vowel cluster)
                string prefix = GetFirstSyllable(trigger);
                if (string.IsNullOrEmpty(prefix)) continue;

                // Check if any word in the spoken text starts with this prefix
                string[] words = spokenLower.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' },
                    StringSplitOptions.RemoveEmptyEntries);

                foreach (string word in words)
                {
                    if (word.StartsWith(prefix) && word != trigger)
                    {
                        // Near miss — warn the player
                        NetworkIdentity playerIdentity = FindPlayerIdentity(playerId);
                        if (playerIdentity != null)
                            TargetTriggerWordWarning(playerIdentity.connectionToClient, trigger);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Extracts a rough first-syllable approximation from a word.
        /// Takes characters up to and including the first vowel group.
        /// </summary>
        private string GetFirstSyllable(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;

            const string vowels = "aeiou";
            bool hitVowel = false;
            int end = 0;

            for (int i = 0; i < word.Length; i++)
            {
                end = i + 1;
                if (vowels.IndexOf(word[i]) >= 0)
                {
                    hitVowel = true;
                }
                else if (hitVowel)
                {
                    // Hit a consonant after vowels — end of first syllable
                    break;
                }
            }

            // Minimum 2 characters for a meaningful prefix
            return end >= 2 ? word.Substring(0, end) : null;
        }

        [TargetRpc]
        private void TargetTriggerWordWarning(NetworkConnection target, string triggerWord)
        {
            // Brief flash of the trigger word on the player's HUD
            var hud = FindObjectOfType<UI.HUDManager>();
            if (hud != null)
                hud.ShowNotification($"<color=#FF4444>{triggerWord.ToUpperInvariant()}</color>", 0.8f);
        }

        /// <summary>
        /// The reproduction sequence: mimic enters Reproducing state, waits,
        /// then splits into two. The new mimic inherits the triggering player's voice.
        /// </summary>
        [Server]
        private IEnumerator ReproductionSequence(MimicBase parentMimic, string playerId, string triggerWord)
        {
            if (parentMimic == null) yield break;

            uint parentId = parentMimic.netId;
            reproducingMimics.Add(parentId);

            // Enter reproducing state
            parentMimic.SetState(EMimicState.Reproducing);

            Debug.Log($"[TriggerWord] Mimic {parentId} reproducing — triggered by '{triggerWord}' from player {playerId}");

            yield return new WaitForSeconds(reproductionDelay);

            // Parent may have been destroyed during the delay
            if (parentMimic == null)
            {
                reproducingMimics.Remove(parentId);
                yield break;
            }

            // Spawn the new mimic
            Vector3 spawnPosition = parentMimic.transform.position + parentMimic.transform.right * 1.5f;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPosition, out UnityEngine.AI.NavMeshHit hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
                spawnPosition = hit.position;

            if (mimicPrefab == null)
            {
                // Fallback: try to use the same prefab as the parent via its AI controller
                var parentController = parentMimic.GetComponent<MimicAIController>();
                if (parentController != null)
                {
                    parentController.SpawnMimic();
                    PostReproductionEffects(parentMimic, parentId, playerId, triggerWord, parentMimic.transform.position + parentMimic.transform.right * 1.5f);
                    yield break;
                }

                Debug.LogWarning("[TriggerWord] No mimic prefab assigned and no fallback available.");
                reproducingMimics.Remove(parentId);
                yield break;
            }

            GameObject newMimicObj = Instantiate(mimicPrefab, spawnPosition, Quaternion.identity);
            NetworkServer.Spawn(newMimicObj);

            PostReproductionEffects(parentMimic, parentId, playerId, triggerWord, spawnPosition);

            // Inherit the triggering player's voice data
            MimicBase newMimic = newMimicObj.GetComponent<MimicBase>();
            if (newMimic != null)
            {
                VoiceProfile inheritedProfile = BuildVoiceProfile(playerId);
                newMimic.SetVoiceProfile(inheritedProfile);

                // Register with the hive mind
                if (MimicHiveMind.Instance != null)
                    MimicHiveMind.Instance.RegisterMimic(newMimic);
            }

            // Update game state
            var gameState = GameManager.Instance?.GameState;
            gameState?.AddActiveMimic();
        }

        [Server]
        private void PostReproductionEffects(MimicBase parentMimic, uint parentId, string playerId, string triggerWord, Vector3 spawnPosition)
        {
            reproducingMimics.Remove(parentId);

            // Particle burst at the reproduction site
            RpcReproductionEffect(spawnPosition);

            // Fire event bus
            EventBus.MimicReproduced(triggerWord, playerId);

            // Director commentary
            EventBus.DirectorSpoke("Interesting. You created another one.");

            Debug.Log($"[TriggerWord] Reproduction complete at {spawnPosition}. Trigger: '{triggerWord}', Source player: {playerId}");
        }

        [ClientRpc]
        private void RpcReproductionEffect(Vector3 position)
        {
            if (reproductionParticlePrefab != null)
            {
                GameObject particles = Instantiate(reproductionParticlePrefab, position, Quaternion.identity);
                var ps = particles.GetComponent<ParticleSystem>();
                if (ps != null)
                    ps.Play();

                // Auto-destroy after particle lifetime
                Destroy(particles, 5f);
            }

            Debug.Log($"[TriggerWord] Reproduction visual at {position}");
        }

        /// <summary>
        /// Builds a voice profile for the new mimic from the triggering player's recorded data.
        /// </summary>
        private VoiceProfile BuildVoiceProfile(string playerId)
        {
            var profile = new VoiceProfile
            {
                playerId = playerId,
                captureTimestamp = Time.time
            };

            if (voiceLearningSystem != null)
            {
                var phrases = voiceLearningSystem.GetPlayerPhrases(playerId);
                foreach (var phrase in phrases)
                    profile.capturedPhrases.Add(phrase.text);
            }

            // Also pull from hive mind shared data
            if (MimicHiveMind.Instance != null)
            {
                var sharedPhrases = MimicHiveMind.Instance.GetSharedPhrases(playerId);
                foreach (string phrase in sharedPhrases)
                {
                    if (!profile.capturedPhrases.Contains(phrase))
                        profile.capturedPhrases.Add(phrase);
                }
            }

            return profile;
        }

        /// <summary>
        /// Finds the nearest mimic within range of the given position.
        /// </summary>
        private MimicBase FindNearestMimicInRange(Vector3 position, float range)
        {
            if (MimicHiveMind.Instance == null) return null;

            MimicBase nearest = null;
            float nearestDist = range;

            foreach (var mimic in MimicHiveMind.Instance.AllMimics)
            {
                if (mimic == null) continue;
                if (mimic.CurrentState == EMimicState.Reproducing) continue;

                float dist = Vector3.Distance(position, mimic.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = mimic;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Resolves a player ID to their world position.
        /// </summary>
        private Vector3? GetPlayerPosition(string playerId)
        {
            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                var identity = player.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                string id = identity.connectionToClient != null
                    ? identity.connectionToClient.connectionId.ToString()
                    : identity.netId.ToString();

                if (id == playerId)
                    return player.transform.position;
            }
            return null;
        }

        /// <summary>
        /// Resolves a player ID to their NetworkIdentity for targeted RPCs.
        /// </summary>
        private NetworkIdentity FindPlayerIdentity(string playerId)
        {
            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                var identity = player.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                string id = identity.connectionToClient != null
                    ? identity.connectionToClient.connectionId.ToString()
                    : identity.netId.ToString();

                if (id == playerId)
                    return identity;
            }
            return null;
        }

        /// <summary>
        /// Removes all trigger word data for a disconnected player.
        /// </summary>
        [Server]
        public void ClearPlayer(string playerId)
        {
            triggerWords.Remove(playerId);
            lastReproductionTime.Remove(playerId);
        }
    }
}
