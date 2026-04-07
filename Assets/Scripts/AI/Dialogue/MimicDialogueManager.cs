using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using MimicFacility.AI.Director;
using MimicFacility.AI.LLM;
using MimicFacility.AI.Voice;

namespace MimicFacility.AI.Dialogue
{
    [Serializable]
    public class MimicDialogueEntry
    {
        public int mimicInstanceId;
        public string targetPlayerId;
        public string dialogueText;
        public int priority;
        public float timestamp;
    }

    public class MimicDialogueManager : NetworkBehaviour
    {
        [SerializeField] private float globalCooldown = 20f;
        [SerializeField] private float perMimicCooldown = 45f;
        [SerializeField] private float evaluationInterval = 5f;
        [SerializeField] private float maxSpeakDistance = 15f;
        [SerializeField] private float lookDotThreshold = 0.7f;
        [SerializeField] private int maxSimultaneousSpeakers = 1;

        private readonly List<MimicDialogueEntry> dialogueQueue = new List<MimicDialogueEntry>();
        private readonly Dictionary<int, NetworkBehaviour> registeredMimics = new Dictionary<int, NetworkBehaviour>();
        private readonly Dictionary<int, float> mimicCooldowns = new Dictionary<int, float>();
        private float lastGlobalDialogueTime = -999f;
        private int activeSpeakers;

        private OllamaClient ollamaClient;
        private VoiceLearningSystem voiceLearning;
        private VoiceCloneClient voiceClone;

        public override void OnStartServer()
        {
            ollamaClient = FindObjectOfType<OllamaClient>();
            voiceLearning = FindObjectOfType<VoiceLearningSystem>();
            voiceClone = FindObjectOfType<VoiceCloneClient>();

            InvokeRepeating(nameof(EvaluateDialogueOpportunities), evaluationInterval, evaluationInterval);
        }

        public void RegisterMimic(NetworkBehaviour mimic)
        {
            if (mimic == null) return;
            int id = mimic.GetInstanceID();
            registeredMimics[id] = mimic;
            mimicCooldowns[id] = -999f;
        }

        public void UnregisterMimic(NetworkBehaviour mimic)
        {
            if (mimic == null) return;
            int id = mimic.GetInstanceID();
            registeredMimics.Remove(id);
            mimicCooldowns.Remove(id);
        }

        [Server]
        private void EvaluateDialogueOpportunities()
        {
            if (activeSpeakers >= maxSimultaneousSpeakers) return;
            if (Time.time - lastGlobalDialogueTime < globalCooldown) return;

            var players = FindObjectsOfType<NetworkIdentity>();

            foreach (var kvp in registeredMimics)
            {
                int mimicId = kvp.Key;
                var mimic = kvp.Value;
                if (mimic == null) continue;

                if (Time.time - GetMimicCooldown(mimicId) < perMimicCooldown) continue;

                foreach (var player in players)
                {
                    if (player == null || player.gameObject == mimic.gameObject) continue;

                    float dist = Vector3.Distance(mimic.transform.position, player.transform.position);
                    if (dist > maxSpeakDistance) continue;

                    Camera playerCam = player.GetComponentInChildren<Camera>();
                    if (playerCam != null && IsPlayerLookingAtMimic(playerCam.transform, mimic.transform))
                        continue;

                    string targetId = SelectImpersonationTarget(mimic);
                    if (string.IsNullOrEmpty(targetId)) continue;

                    int priority = CalculatePriority(dist);
                    var entry = new MimicDialogueEntry
                    {
                        mimicInstanceId = mimicId,
                        targetPlayerId = targetId,
                        priority = priority,
                        timestamp = Time.time
                    };

                    RequestDialogue(entry);
                    return;
                }
            }
        }

        public void RequestDialogue(MimicDialogueEntry entry)
        {
            if (Time.time - lastGlobalDialogueTime < globalCooldown) return;

            dialogueQueue.Add(entry);
            dialogueQueue.Sort((a, b) => b.priority.CompareTo(a.priority));

            ProcessNextDialogue();
        }

        [Server]
        private void ProcessNextDialogue()
        {
            if (dialogueQueue.Count == 0) return;
            if (activeSpeakers >= maxSimultaneousSpeakers) return;

            var entry = dialogueQueue[0];
            dialogueQueue.RemoveAt(0);

            if (!registeredMimics.ContainsKey(entry.mimicInstanceId)) return;
            var mimic = registeredMimics[entry.mimicInstanceId];
            if (mimic == null) return;

            List<string> capturedPhrases = new List<string>();
            if (voiceLearning != null)
                capturedPhrases = voiceLearning.GetMostFrequentPhrases(entry.targetPlayerId, 5);

            var ctx = new PromptBuilder.MimicContext
            {
                targetPlayerName = entry.targetPlayerId,
                capturedPhrases = capturedPhrases,
                witnessedBehaviors = "",
                situation = "Players are nearby. Say something casual to blend in."
            };

            var request = PromptBuilder.BuildMimicRequest(ctx);

            activeSpeakers++;
            lastGlobalDialogueTime = Time.time;
            mimicCooldowns[entry.mimicInstanceId] = Time.time;

            ollamaClient.SendRequest(request, response =>
            {
                activeSpeakers = Mathf.Max(0, activeSpeakers - 1);

                if (!response.success)
                {
                    Debug.LogWarning($"[MimicDialogue] LLM failed for mimic {entry.mimicInstanceId}: {response.errorMessage}");
                    return;
                }

                if (voiceClone != null && voiceLearning != null && voiceLearning.HasVoiceData(entry.targetPlayerId))
                {
                    var cloneReq = new VoiceCloneRequest
                    {
                        text = response.text,
                        speakerReferenceId = entry.targetPlayerId,
                        temperature = 0.7f,
                        exaggerationFactor = 1.0f
                    };

                    voiceClone.SendCloneRequest(cloneReq, cloneResponse =>
                    {
                        if (cloneResponse.success)
                        {
                            var clip = voiceClone.CreateAudioClip(cloneResponse);
                            PlayClipOnMimic(mimic, clip);
                        }
                    });
                }
            });
        }

        public string SelectImpersonationTarget(NetworkBehaviour mimic)
        {
            if (voiceLearning == null) return null;

            var players = FindObjectsOfType<NetworkIdentity>();
            string bestTarget = null;
            float bestScore = -1f;

            foreach (var player in players)
            {
                if (player == null || player.gameObject == mimic.gameObject) continue;

                string playerId = player.netId.ToString();
                if (!voiceLearning.HasVoiceData(playerId)) continue;

                float dist = Vector3.Distance(mimic.transform.position, player.transform.position);
                float distScore = 1f - Mathf.Clamp01(dist / maxSpeakDistance);

                var recentPhrases = voiceLearning.GetRecentPhrases(playerId, 120f);
                float recencyScore = Mathf.Clamp01(recentPhrases.Count / 5f);

                float totalScore = distScore * 0.4f + recencyScore * 0.6f;

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestTarget = playerId;
                }
            }

            return bestTarget;
        }

        public bool IsPlayerLookingAtMimic(Transform playerCamera, Transform mimic)
        {
            if (playerCamera == null || mimic == null) return false;

            Vector3 toMimic = (mimic.position - playerCamera.position).normalized;
            float dot = Vector3.Dot(playerCamera.forward, toMimic);
            return dot > lookDotThreshold;
        }

        private float GetMimicCooldown(int mimicId)
        {
            return mimicCooldowns.ContainsKey(mimicId) ? mimicCooldowns[mimicId] : -999f;
        }

        private int CalculatePriority(float distance)
        {
            return Mathf.RoundToInt((1f - Mathf.Clamp01(distance / maxSpeakDistance)) * 100f);
        }

        private void PlayClipOnMimic(NetworkBehaviour mimic, AudioClip clip)
        {
            if (mimic == null || clip == null) return;

            var audioSource = mimic.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = mimic.gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = maxSpeakDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
