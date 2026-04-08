using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MimicFacility.Core;
using MimicFacility.Audio;
using MimicFacility.AI.Weapons;

namespace MimicFacility.AI.Voice
{
    [Serializable]
    public class TranscriptEntry
    {
        public string playerId;
        public string text;
        public float timestamp;
        public Vector3 position;
        public List<string> nearbyPlayerIds = new List<string>();
    }

    [Serializable]
    public class PlayerVoiceProfile
    {
        public string playerId;
        public int totalPhrases;
        public List<string> triggerWords = new List<string>();
        public List<string> mostFrequentWords = new List<string>();
        public List<string> verbalTics = new List<string>();
        public float averagePhraseLength;
        public float speechFrequency;
    }

    public class VoiceRecordingManager : MonoBehaviour
    {
        [Header("Recording")]
        [SerializeField] private float round1Duration = 120f;
        [SerializeField] private float earshortRange = 25f;
        [SerializeField] private int triggerWordMin = 3;
        [SerializeField] private int triggerWordMax = 5;

        [Header("References")]
        [SerializeField] private VoiceLearningSystem voiceLearningSystem;
        [SerializeField] private PersonalWeaponSystem personalWeaponSystem;

        public bool IsRecording { get; private set; }
        public bool RecordingIndicator { get; private set; }
        public float TimeRemaining { get; private set; }
        public float RecordingProgress => round1Duration > 0f ? 1f - (TimeRemaining / round1Duration) : 1f;

        private readonly Dictionary<string, List<float[]>> capturedAudio = new Dictionary<string, List<float[]>>();
        private readonly Dictionary<string, List<TranscriptEntry>> transcripts = new Dictionary<string, List<TranscriptEntry>>();
        private readonly Dictionary<string, Dictionary<string, int>> wordFrequency = new Dictionary<string, Dictionary<string, int>>();
        private readonly Dictionary<string, List<string>> detectedTics = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, int> phraseCount = new Dictionary<string, int>();
        private readonly Dictionary<string, List<string>> selectedTriggerWords = new Dictionary<string, List<string>>();

        private VoiceChatManager voiceChatManager;
        private float recordingStartTime;
        private bool warningPlayed;

        private static readonly string[] KnownTics =
        {
            "um", "uh", "like", "you know", "basically", "literally",
            "honestly", "actually", "right", "okay so", "i mean",
            "sort of", "kind of", "whatever", "anyway"
        };

        private static readonly HashSet<string> StopWords = new HashSet<string>
        {
            "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "could",
            "should", "may", "might", "shall", "can", "to", "of", "in", "for",
            "on", "with", "at", "by", "from", "it", "this", "that", "and", "or",
            "but", "not", "no", "so", "if", "then", "than", "too", "very", "just",
            "about", "up", "out", "what", "which", "who", "whom", "how", "all",
            "each", "every", "both", "few", "more", "some", "any", "most", "other",
            "i", "me", "my", "we", "us", "our", "you", "your", "he", "she", "they",
            "them", "his", "her", "its", "go", "get", "got", "here", "there", "yeah",
            "yes", "hey", "oh"
        };

        void Awake()
        {
            if (voiceLearningSystem == null)
                voiceLearningSystem = FindObjectOfType<VoiceLearningSystem>();
            if (personalWeaponSystem == null)
                personalWeaponSystem = FindObjectOfType<PersonalWeaponSystem>();
        }

        void Update()
        {
            if (!IsRecording) return;

            TimeRemaining = Mathf.Max(0f, (recordingStartTime + round1Duration) - Time.time);

            if (TimeRemaining <= 0f)
            {
                EndRecordingPhase();
            }
        }

        /// <summary>
        /// Begin the recording phase. Called at the start of Round 1.
        /// </summary>
        public void BeginRecordingPhase()
        {
            if (IsRecording) return;

            IsRecording = true;
            RecordingIndicator = true;
            TimeRemaining = round1Duration;
            recordingStartTime = Time.time;
            warningPlayed = false;

            capturedAudio.Clear();
            transcripts.Clear();
            wordFrequency.Clear();
            detectedTics.Clear();
            phraseCount.Clear();
            selectedTriggerWords.Clear();

            SubscribeToVoiceEvents();
            DirectorWarning();

            Debug.Log("[VoiceRecording] Recording phase started. All voice data is being captured.");
        }

        /// <summary>
        /// At the START of Round 1, plays the Miranda warning via DirectorVoiceLibrary.
        /// "You have the right to remain silent. Anything you say can and will be used against you."
        /// </summary>
        public void DirectorWarning()
        {
            if (warningPlayed) return;
            warningPlayed = true;

            var voiceLibrary = DirectorVoiceLibrary.Instance;
            if (voiceLibrary != null)
            {
                voiceLibrary.PlayMiranda();
                Debug.Log("[VoiceRecording] Miranda warning: You have the right to remain silent. Anything you say can and will be used against you.");
            }
            else
            {
                Debug.LogWarning("[VoiceRecording] DirectorVoiceLibrary not found. Miranda warning skipped.");
            }
        }

        /// <summary>
        /// Called when Round 1 ends. Processes all captured data and feeds it to downstream systems.
        /// </summary>
        public void EndRecordingPhase()
        {
            if (!IsRecording) return;

            IsRecording = false;
            RecordingIndicator = false;
            TimeRemaining = 0f;

            UnsubscribeFromVoiceEvents();

            foreach (string playerId in capturedAudio.Keys)
            {
                SelectTriggerWords(playerId);
            }

            FeedDataToSystems();
            LogRecordingSummary();

            Debug.Log("[VoiceRecording] Recording phase ended. All player data processed.");
        }

        /// <summary>
        /// Process incoming raw voice audio for a player.
        /// </summary>
        public void CaptureVoiceData(string playerId, float[] audioSamples, Vector3 position)
        {
            if (!IsRecording) return;
            if (string.IsNullOrEmpty(playerId) || audioSamples == null || audioSamples.Length == 0) return;

            if (!capturedAudio.ContainsKey(playerId))
                capturedAudio[playerId] = new List<float[]>();

            float[] copy = new float[audioSamples.Length];
            Array.Copy(audioSamples, copy, audioSamples.Length);
            capturedAudio[playerId].Add(copy);
        }

        /// <summary>
        /// Record a transcript entry for a player. In production, STT would feed this.
        /// </summary>
        public void RecordTranscript(string playerId, string text, Vector3 position)
        {
            if (!IsRecording) return;
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(text)) return;

            List<string> nearbyIds = FindNearbyPlayerIds(playerId, position);

            var entry = new TranscriptEntry
            {
                playerId = playerId,
                text = text,
                timestamp = Time.time,
                position = position,
                nearbyPlayerIds = nearbyIds
            };

            if (!transcripts.ContainsKey(playerId))
                transcripts[playerId] = new List<TranscriptEntry>();
            transcripts[playerId].Add(entry);

            if (!phraseCount.ContainsKey(playerId))
                phraseCount[playerId] = 0;
            phraseCount[playerId]++;

            AnalyzeWords(playerId, text);
            DetectVerbalTics(playerId, text);

            if (voiceLearningSystem != null)
                voiceLearningSystem.RecordPhrase(playerId, text);
        }

        /// <summary>
        /// Returns a profile summary of all captured data for a player.
        /// </summary>
        public PlayerVoiceProfile GetPlayerProfile(string playerId)
        {
            var profile = new PlayerVoiceProfile { playerId = playerId };

            if (phraseCount.ContainsKey(playerId))
                profile.totalPhrases = phraseCount[playerId];

            if (selectedTriggerWords.ContainsKey(playerId))
                profile.triggerWords = new List<string>(selectedTriggerWords[playerId]);

            if (wordFrequency.ContainsKey(playerId))
            {
                profile.mostFrequentWords = wordFrequency[playerId]
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(10)
                    .Select(kvp => kvp.Key)
                    .ToList();
            }

            if (detectedTics.ContainsKey(playerId))
                profile.verbalTics = new List<string>(detectedTics[playerId]);

            if (transcripts.ContainsKey(playerId) && transcripts[playerId].Count > 0)
            {
                float totalLength = 0f;
                foreach (var t in transcripts[playerId])
                    totalLength += t.text.Length;
                profile.averagePhraseLength = totalLength / transcripts[playerId].Count;

                float elapsed = Time.time - recordingStartTime;
                if (elapsed > 0f)
                    profile.speechFrequency = transcripts[playerId].Count / (elapsed / 60f);
            }

            return profile;
        }

        /// <summary>
        /// Get all captured audio segments for a player.
        /// </summary>
        public List<float[]> GetCapturedAudio(string playerId)
        {
            if (capturedAudio.ContainsKey(playerId))
                return new List<float[]>(capturedAudio[playerId]);
            return new List<float[]>();
        }

        /// <summary>
        /// Get all transcript entries for a player.
        /// </summary>
        public List<TranscriptEntry> GetTranscripts(string playerId)
        {
            if (transcripts.ContainsKey(playerId))
                return new List<TranscriptEntry>(transcripts[playerId]);
            return new List<TranscriptEntry>();
        }

        /// <summary>
        /// Get all tracked player IDs.
        /// </summary>
        public List<string> GetTrackedPlayerIds()
        {
            var ids = new HashSet<string>(capturedAudio.Keys);
            foreach (string id in transcripts.Keys)
                ids.Add(id);
            return ids.ToList();
        }

        private void SubscribeToVoiceEvents()
        {
            voiceChatManager = FindObjectOfType<VoiceChatManager>();
            if (voiceChatManager != null)
            {
                voiceChatManager.OnVoiceReceived += HandleVoiceReceived;
            }
            else
            {
                Debug.LogWarning("[VoiceRecording] No VoiceChatManager found. Voice capture will rely on manual input.");
            }
        }

        private void UnsubscribeFromVoiceEvents()
        {
            if (voiceChatManager != null)
            {
                voiceChatManager.OnVoiceReceived -= HandleVoiceReceived;
            }
        }

        private void HandleVoiceReceived(byte[] compressedAudio, int senderId)
        {
            if (!IsRecording) return;

            string playerId = senderId.ToString();
            float[] samples = VoiceChatManager.DecompressAudio(compressedAudio);
            CaptureVoiceData(playerId, samples, Vector3.zero);

            // Placeholder: actual STT transcription would convert samples to text here.
            // For now, audio is stored raw for VoiceLearningSystem / VoiceCloneClient to use.
        }

        private void AnalyzeWords(string playerId, string text)
        {
            if (!wordFrequency.ContainsKey(playerId))
                wordFrequency[playerId] = new Dictionary<string, int>();

            var counts = wordFrequency[playerId];
            string[] words = text.ToLowerInvariant().Split(
                new[] { ' ', ',', '.', '!', '?', ';', ':', '\'', '"', '-', '(', ')' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                if (word.Length < 3 || StopWords.Contains(word)) continue;

                if (counts.ContainsKey(word))
                    counts[word]++;
                else
                    counts[word] = 1;
            }
        }

        private void DetectVerbalTics(string playerId, string text)
        {
            if (!detectedTics.ContainsKey(playerId))
                detectedTics[playerId] = new List<string>();

            string lower = text.ToLowerInvariant();
            foreach (string tic in KnownTics)
            {
                if (lower.Contains(tic) && !detectedTics[playerId].Contains(tic))
                {
                    detectedTics[playerId].Add(tic);
                }
            }
        }

        private void SelectTriggerWords(string playerId)
        {
            if (!wordFrequency.ContainsKey(playerId))
            {
                selectedTriggerWords[playerId] = new List<string>();
                return;
            }

            var globalFrequency = new Dictionary<string, int>();
            foreach (var kvp in wordFrequency)
            {
                foreach (var wc in kvp.Value)
                {
                    if (globalFrequency.ContainsKey(wc.Key))
                        globalFrequency[wc.Key] += wc.Value;
                    else
                        globalFrequency[wc.Key] = wc.Value;
                }
            }

            int targetCount = Mathf.Clamp(
                wordFrequency[playerId].Count(wc => wc.Value >= 2),
                triggerWordMin,
                triggerWordMax);

            var triggers = wordFrequency[playerId]
                .Where(wc => wc.Value >= 2)
                .OrderByDescending(wc =>
                {
                    float playerFreq = wc.Value;
                    float globalFreq = globalFrequency.ContainsKey(wc.Key) ? globalFrequency[wc.Key] : 1f;
                    return playerFreq / globalFreq;
                })
                .Take(targetCount)
                .Select(wc => wc.Key)
                .ToList();

            selectedTriggerWords[playerId] = triggers;
        }

        private void FeedDataToSystems()
        {
            if (voiceLearningSystem != null)
            {
                voiceLearningSystem.SelectTriggerWordsForAllPlayers();
            }

            if (personalWeaponSystem != null)
            {
                foreach (string playerId in transcripts.Keys)
                {
                    foreach (var entry in transcripts[playerId])
                    {
                        personalWeaponSystem.RecordPhrase(playerId, entry.text);
                    }
                }
            }
        }

        private void LogRecordingSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== RECORDING PHASE SUMMARY ===");

            foreach (string playerId in GetTrackedPlayerIds())
            {
                var profile = GetPlayerProfile(playerId);
                sb.AppendLine($"  Player: {playerId}");
                sb.AppendLine($"    Phrases: {profile.totalPhrases}");
                sb.AppendLine($"    Speech rate: {profile.speechFrequency:F1} phrases/min");
                sb.AppendLine($"    Avg phrase length: {profile.averagePhraseLength:F0} chars");

                if (profile.triggerWords.Count > 0)
                    sb.AppendLine($"    Trigger words: {string.Join(", ", profile.triggerWords)}");

                if (profile.mostFrequentWords.Count > 0)
                    sb.AppendLine($"    Top words: {string.Join(", ", profile.mostFrequentWords.Take(5))}");

                if (profile.verbalTics.Count > 0)
                    sb.AppendLine($"    Verbal tics: {string.Join(", ", profile.verbalTics)}");

                int audioSegments = capturedAudio.ContainsKey(playerId) ? capturedAudio[playerId].Count : 0;
                sb.AppendLine($"    Audio segments: {audioSegments}");
            }

            sb.AppendLine("===============================");
            Debug.Log(sb.ToString());
        }

        private List<string> FindNearbyPlayerIds(string speakerId, Vector3 speakerPosition)
        {
            var nearby = new List<string>();
            var players = FindObjectsOfType<MimicPlayerState>();

            foreach (var player in players)
            {
                string otherId = player.PlayerId;
                if (otherId == speakerId) continue;

                float distance = Vector3.Distance(speakerPosition, player.transform.position);
                if (distance <= earshortRange)
                {
                    nearby.Add(otherId);
                }
            }

            return nearby;
        }

        void OnDestroy()
        {
            UnsubscribeFromVoiceEvents();
        }
    }
}
