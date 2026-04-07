using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MimicFacility.AI.Voice
{
    [Serializable]
    public class VoicePhrase
    {
        public string text;
        public string speakerId;
        public float timestamp;
        public List<string> triggerWords = new List<string>();
    }

    public class VoiceLearningSystem : MonoBehaviour
    {
        private readonly Dictionary<string, List<VoicePhrase>> playerPhrases = new Dictionary<string, List<VoicePhrase>>();
        private readonly Dictionary<string, Dictionary<string, int>> wordCounts = new Dictionary<string, Dictionary<string, int>>();
        private readonly Dictionary<string, List<string>> playerTriggerWords = new Dictionary<string, List<string>>();

        private static readonly HashSet<string> StopWords = new HashSet<string>
        {
            "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "could",
            "should", "may", "might", "shall", "can", "to", "of", "in", "for",
            "on", "with", "at", "by", "from", "it", "this", "that", "and", "or",
            "but", "not", "no", "so", "if", "then", "than", "too", "very", "just",
            "about", "up", "out", "what", "which", "who", "whom", "how", "all",
            "each", "every", "both", "few", "more", "some", "any", "most", "other"
        };

        public void RecordPhrase(string playerId, string text)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(text)) return;

            if (!playerPhrases.ContainsKey(playerId))
                playerPhrases[playerId] = new List<VoicePhrase>();

            if (!wordCounts.ContainsKey(playerId))
                wordCounts[playerId] = new Dictionary<string, int>();

            var phrase = new VoicePhrase
            {
                text = text,
                speakerId = playerId,
                timestamp = Time.time
            };

            playerPhrases[playerId].Add(phrase);

            string[] words = text.ToLowerInvariant().Split(new[] { ' ', ',', '.', '!', '?', ';', ':' },
                StringSplitOptions.RemoveEmptyEntries);

            var counts = wordCounts[playerId];
            foreach (string word in words)
            {
                if (word.Length < 3 || StopWords.Contains(word)) continue;

                if (counts.ContainsKey(word))
                    counts[word]++;
                else
                    counts[word] = 1;
            }
        }

        public void SelectTriggerWordsForAllPlayers()
        {
            playerTriggerWords.Clear();

            var globalFrequency = new Dictionary<string, int>();
            foreach (var kvp in wordCounts)
            {
                foreach (var wc in kvp.Value)
                {
                    if (globalFrequency.ContainsKey(wc.Key))
                        globalFrequency[wc.Key] += wc.Value;
                    else
                        globalFrequency[wc.Key] = wc.Value;
                }
            }

            foreach (var kvp in wordCounts)
            {
                string playerId = kvp.Key;
                var counts = kvp.Value;

                var candidates = counts
                    .Where(wc => wc.Value >= 3)
                    .OrderByDescending(wc =>
                    {
                        float playerFreq = wc.Value;
                        float globalFreq = globalFrequency.ContainsKey(wc.Key) ? globalFrequency[wc.Key] : 1f;
                        return playerFreq / globalFreq;
                    })
                    .Take(3)
                    .Select(wc => wc.Key)
                    .ToList();

                playerTriggerWords[playerId] = candidates;
            }
        }

        public string CheckTriggerWord(string playerId, string spokenText)
        {
            if (!playerTriggerWords.ContainsKey(playerId)) return null;
            if (string.IsNullOrEmpty(spokenText)) return null;

            string lower = spokenText.ToLowerInvariant();
            foreach (string trigger in playerTriggerWords[playerId])
            {
                if (lower.Contains(trigger))
                    return trigger;
            }
            return null;
        }

        public List<VoicePhrase> GetPlayerPhrases(string playerId)
        {
            if (playerPhrases.ContainsKey(playerId))
                return new List<VoicePhrase>(playerPhrases[playerId]);
            return new List<VoicePhrase>();
        }

        public List<VoicePhrase> GetRecentPhrases(string playerId, float withinSeconds)
        {
            if (!playerPhrases.ContainsKey(playerId))
                return new List<VoicePhrase>();

            float cutoff = Time.time - withinSeconds;
            return playerPhrases[playerId]
                .Where(p => p.timestamp >= cutoff)
                .ToList();
        }

        public void ClearPlayer(string playerId)
        {
            playerPhrases.Remove(playerId);
            wordCounts.Remove(playerId);
            playerTriggerWords.Remove(playerId);
        }

        public bool HasVoiceData(string playerId)
        {
            return playerPhrases.ContainsKey(playerId) && playerPhrases[playerId].Count > 0;
        }

        public List<string> GetMostFrequentPhrases(string playerId, int count)
        {
            if (!playerPhrases.ContainsKey(playerId))
                return new List<string>();

            var phraseOccurrences = new Dictionary<string, int>();
            foreach (var phrase in playerPhrases[playerId])
            {
                string normalized = phrase.text.Trim().ToLowerInvariant();
                if (phraseOccurrences.ContainsKey(normalized))
                    phraseOccurrences[normalized]++;
                else
                    phraseOccurrences[normalized] = 1;
            }

            return phraseOccurrences
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }
}
