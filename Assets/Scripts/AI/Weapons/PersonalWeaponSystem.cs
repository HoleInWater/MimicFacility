using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MimicFacility.AI.Weapons
{
    [Serializable]
    public class VoiceProfile
    {
        public List<string> topPhrases = new List<string>();
        public List<string> verbalFillers = new List<string>();
        public float speechRate;
        public float pitchBaseline;
        public float pitchUnderStress;
    }

    [Serializable]
    public class EmotionalEvent
    {
        public string eventType;
        public string context;
        public float timestamp;
        public string location;
    }

    [Serializable]
    public class EmotionalProfile
    {
        public List<EmotionalEvent> events = new List<EmotionalEvent>();
        public float panicFrequency;
    }

    public enum ESocialInteraction
    {
        Proximity,
        Deference,
        Disagreement,
        Support,
        Initiation
    }

    [Serializable]
    public class SocialMap
    {
        public string leaderPlayerId;
        public string[] trustedPairIds = new string[2];
        public string[] volatilePairIds = new string[2];
        public string isolatedPlayerId;
        public Dictionary<string, int> deferenceCount = new Dictionary<string, int>();
        public Dictionary<string, int> initiationCount = new Dictionary<string, int>();
    }

    public enum ESlipCategory
    {
        Prediction,
        PersonalDetail,
        FutureKnowledge,
        EmotionalLeak
    }

    [Serializable]
    public class VerbalSlip
    {
        public string phrase;
        public string context;
        public List<string> witnesses = new List<string>();
        public ESlipCategory category;
        public bool used;
    }

    public class PersonalWeaponSystem : MonoBehaviour
    {
        private readonly Dictionary<string, VoiceProfile> voiceProfiles = new Dictionary<string, VoiceProfile>();
        private readonly Dictionary<string, EmotionalProfile> emotionalProfiles = new Dictionary<string, EmotionalProfile>();
        private readonly Dictionary<string, Dictionary<string, int>> phraseFrequency = new Dictionary<string, Dictionary<string, int>>();
        private readonly List<VerbalSlip> slipQueue = new List<VerbalSlip>();
        private SocialMap socialMap = new SocialMap();
        private float lastSlipConsumeTime = -999f;
        private const float SlipCooldown = 300f;

        private readonly Dictionary<string, Dictionary<string, int>> socialInteractions = new Dictionary<string, Dictionary<string, int>>();
        private readonly Dictionary<string, Dictionary<string, int>> interactionCounts = new Dictionary<string, Dictionary<string, int>>();

        public void RecordPhrase(string playerId, string phrase)
        {
            if (!voiceProfiles.ContainsKey(playerId))
                voiceProfiles[playerId] = new VoiceProfile();

            if (!phraseFrequency.ContainsKey(playerId))
                phraseFrequency[playerId] = new Dictionary<string, int>();

            var profile = voiceProfiles[playerId];
            var freqs = phraseFrequency[playerId];

            string normalized = phrase.Trim().ToLowerInvariant();
            if (freqs.ContainsKey(normalized))
                freqs[normalized]++;
            else
                freqs[normalized] = 1;

            UpdateTopPhrases(playerId);
            DetectFillers(playerId, phrase);
        }

        public void RecordEmotionalEvent(string playerId, EmotionalEvent evt)
        {
            if (!emotionalProfiles.ContainsKey(playerId))
                emotionalProfiles[playerId] = new EmotionalProfile();

            var profile = emotionalProfiles[playerId];
            profile.events.Add(evt);

            if (profile.events.Count > 100)
                profile.events.RemoveAt(0);

            RecalculatePanicFrequency(playerId);
        }

        public void RecordSocialInteraction(string player1, string player2, ESocialInteraction type)
        {
            string pairKey = GetPairKey(player1, player2);
            string typeKey = type.ToString();

            if (!interactionCounts.ContainsKey(pairKey))
                interactionCounts[pairKey] = new Dictionary<string, int>();

            if (interactionCounts[pairKey].ContainsKey(typeKey))
                interactionCounts[pairKey][typeKey]++;
            else
                interactionCounts[pairKey][typeKey] = 1;

            if (type == ESocialInteraction.Deference)
            {
                if (!socialMap.deferenceCount.ContainsKey(player2))
                    socialMap.deferenceCount[player2] = 0;
                socialMap.deferenceCount[player2]++;
            }

            if (type == ESocialInteraction.Initiation)
            {
                if (!socialMap.initiationCount.ContainsKey(player1))
                    socialMap.initiationCount[player1] = 0;
                socialMap.initiationCount[player1]++;
            }
        }

        public void AddVerbalSlip(VerbalSlip slip)
        {
            slipQueue.Add(slip);
        }

        public VerbalSlip ConsumeNextSlip()
        {
            if (Time.time - lastSlipConsumeTime < SlipCooldown) return null;

            for (int i = 0; i < slipQueue.Count; i++)
            {
                if (!slipQueue[i].used)
                {
                    slipQueue[i].used = true;
                    lastSlipConsumeTime = Time.time;
                    return slipQueue[i];
                }
            }
            return null;
        }

        public void ComputeSocialMap()
        {
            string leader = null;
            int maxInitiation = 0;
            foreach (var kvp in socialMap.initiationCount)
            {
                if (kvp.Value > maxInitiation)
                {
                    maxInitiation = kvp.Value;
                    leader = kvp.Key;
                }
            }
            socialMap.leaderPlayerId = leader;

            string bestTrustPair = null;
            int maxSupport = 0;
            string worstPair = null;
            int maxDisagreement = 0;

            foreach (var kvp in interactionCounts)
            {
                int support = kvp.Value.ContainsKey("Support") ? kvp.Value["Support"] : 0;
                int disagreement = kvp.Value.ContainsKey("Disagreement") ? kvp.Value["Disagreement"] : 0;

                if (support > maxSupport)
                {
                    maxSupport = support;
                    bestTrustPair = kvp.Key;
                }
                if (disagreement > maxDisagreement)
                {
                    maxDisagreement = disagreement;
                    worstPair = kvp.Key;
                }
            }

            if (bestTrustPair != null)
            {
                string[] parts = bestTrustPair.Split('|');
                if (parts.Length == 2)
                {
                    socialMap.trustedPairIds[0] = parts[0];
                    socialMap.trustedPairIds[1] = parts[1];
                }
            }

            if (worstPair != null)
            {
                string[] parts = worstPair.Split('|');
                if (parts.Length == 2)
                {
                    socialMap.volatilePairIds[0] = parts[0];
                    socialMap.volatilePairIds[1] = parts[1];
                }
            }

            FindIsolatedPlayer();
        }

        public string GenerateSocialSummary()
        {
            ComputeSocialMap();
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(socialMap.leaderPlayerId))
                sb.Append($"The group leader appears to be {socialMap.leaderPlayerId}. ");

            if (!string.IsNullOrEmpty(socialMap.trustedPairIds[0]) && !string.IsNullOrEmpty(socialMap.trustedPairIds[1]))
                sb.Append($"{socialMap.trustedPairIds[0]} and {socialMap.trustedPairIds[1]} show high mutual trust. ");

            if (!string.IsNullOrEmpty(socialMap.volatilePairIds[0]) && !string.IsNullOrEmpty(socialMap.volatilePairIds[1]))
                sb.Append($"Tension detected between {socialMap.volatilePairIds[0]} and {socialMap.volatilePairIds[1]}. ");

            if (!string.IsNullOrEmpty(socialMap.isolatedPlayerId))
                sb.Append($"{socialMap.isolatedPlayerId} is socially isolated from the group.");

            return sb.ToString().Trim();
        }

        public string GenerateEmotionalSummary(string playerId)
        {
            if (!emotionalProfiles.ContainsKey(playerId))
                return "No emotional data recorded.";

            var profile = emotionalProfiles[playerId];
            var sb = new StringBuilder();

            sb.Append($"Panic frequency: {profile.panicFrequency:F2} events/min. ");

            if (profile.events.Count > 0)
            {
                var recent = profile.events[profile.events.Count - 1];
                sb.Append($"Last emotional event: {recent.eventType} at {recent.location}. ");
            }

            int totalEvents = profile.events.Count;
            sb.Append($"Total recorded events: {totalEvents}.");

            return sb.ToString();
        }

        public string GenerateVoiceSummary(string playerId)
        {
            if (!voiceProfiles.ContainsKey(playerId))
                return "No voice data recorded.";

            var profile = voiceProfiles[playerId];
            var sb = new StringBuilder();

            if (profile.topPhrases.Count > 0)
            {
                sb.Append("Top phrases: ");
                sb.Append(string.Join(", ", profile.topPhrases.Take(5)));
                sb.Append(". ");
            }

            if (profile.verbalFillers.Count > 0)
            {
                sb.Append("Verbal fillers: ");
                sb.Append(string.Join(", ", profile.verbalFillers));
                sb.Append(".");
            }

            return sb.ToString().Trim();
        }

        private void UpdateTopPhrases(string playerId)
        {
            if (!phraseFrequency.ContainsKey(playerId)) return;

            var sorted = phraseFrequency[playerId]
                .OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .Select(kvp => kvp.Key)
                .ToList();

            voiceProfiles[playerId].topPhrases = sorted;
        }

        private void DetectFillers(string playerId, string phrase)
        {
            string[] knownFillers = { "um", "uh", "like", "you know", "basically", "literally", "honestly", "actually" };
            string lower = phrase.ToLowerInvariant();
            var profile = voiceProfiles[playerId];

            foreach (string filler in knownFillers)
            {
                if (lower.Contains(filler) && !profile.verbalFillers.Contains(filler))
                {
                    profile.verbalFillers.Add(filler);
                }
            }
        }

        private void RecalculatePanicFrequency(string playerId)
        {
            var profile = emotionalProfiles[playerId];
            int panicCount = 0;
            float windowStart = Time.time - 300f;

            foreach (var evt in profile.events)
            {
                if (evt.timestamp >= windowStart &&
                    (evt.eventType == "Panic" || evt.eventType == "Scream" || evt.eventType == "Flee"))
                {
                    panicCount++;
                }
            }

            profile.panicFrequency = panicCount / 5f;
        }

        private string GetPairKey(string a, string b)
        {
            return string.Compare(a, b, StringComparison.Ordinal) < 0 ? $"{a}|{b}" : $"{b}|{a}";
        }

        private void FindIsolatedPlayer()
        {
            var totalInteractions = new Dictionary<string, int>();

            foreach (var kvp in interactionCounts)
            {
                string[] parts = kvp.Key.Split('|');
                if (parts.Length != 2) continue;

                int total = 0;
                foreach (var count in kvp.Value.Values)
                    total += count;

                foreach (string p in parts)
                {
                    if (!totalInteractions.ContainsKey(p))
                        totalInteractions[p] = 0;
                    totalInteractions[p] += total;
                }
            }

            string isolated = null;
            int minInteractions = int.MaxValue;

            foreach (var kvp in totalInteractions)
            {
                if (kvp.Value < minInteractions)
                {
                    minInteractions = kvp.Value;
                    isolated = kvp.Key;
                }
            }

            socialMap.isolatedPlayerId = isolated;
        }
    }
}
