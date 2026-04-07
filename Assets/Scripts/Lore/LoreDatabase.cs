using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MimicFacility.Lore
{
    public class LoreDatabase : MonoBehaviour
    {
        public static LoreDatabase Instance { get; private set; }

        public event Action<string> OnEntryRead;

        private readonly Dictionary<string, List<LoreEntry>> entriesByTerminal = new Dictionary<string, List<LoreEntry>>();
        private readonly Dictionary<string, List<LoreEntry>> entriesByChannel = new Dictionary<string, List<LoreEntry>>();
        private readonly Dictionary<string, LoreEntry> entriesById = new Dictionary<string, LoreEntry>();
        private readonly HashSet<string> readEntries = new HashSet<string>();

        private List<LoreEntry> allEntries = new List<LoreEntry>();
        private const string READ_PROGRESS_KEY = "MimicFacility_LoreReadProgress";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Initialize()
        {
            allEntries.Clear();
            entriesByTerminal.Clear();
            entriesByChannel.Clear();
            entriesById.Clear();

            string json = null;
            var textAsset = Resources.Load<TextAsset>("Data/LoreEntries");
            if (textAsset != null)
            {
                json = textAsset.text;
            }
            else
            {
                string streamingPath = Path.Combine(Application.streamingAssetsPath, "Data", "LoreEntries.json");
                if (File.Exists(streamingPath))
                    json = File.ReadAllText(streamingPath);
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("LoreDatabase: No lore data found.");
                return;
            }

            var collection = JsonUtility.FromJson<LoreEntryCollection>(json);
            if (collection == null || collection.entries == null)
            {
                Debug.LogWarning("LoreDatabase: Failed to parse lore data.");
                return;
            }

            allEntries = collection.entries;
            IndexEntries();
            LoadReadProgress();
        }

        private void IndexEntries()
        {
            foreach (var entry in allEntries)
            {
                entriesById[entry.entryId] = entry;

                if (!string.IsNullOrEmpty(entry.terminalId))
                {
                    if (!entriesByTerminal.ContainsKey(entry.terminalId))
                        entriesByTerminal[entry.terminalId] = new List<LoreEntry>();
                    entriesByTerminal[entry.terminalId].Add(entry);
                }

                string channelKey = entry.channel.ToString();
                if (!entriesByChannel.ContainsKey(channelKey))
                    entriesByChannel[channelKey] = new List<LoreEntry>();
                entriesByChannel[channelKey].Add(entry);
            }
        }

        public List<LoreEntry> GetEntriesForTerminal(string terminalId, int corruptionLevel)
        {
            if (!entriesByTerminal.TryGetValue(terminalId, out var entries))
                return new List<LoreEntry>();

            var result = new List<LoreEntry>();
            foreach (var entry in entries)
            {
                if (corruptionLevel < entry.minCorruptionToReveal) continue;

                var copy = CloneEntry(entry);
                if (copy.isRedacted && corruptionLevel >= entry.minCorruptionToReveal
                    && !string.IsNullOrEmpty(copy.redactedContent))
                {
                    copy.content = copy.redactedContent;
                }
                result.Add(copy);
            }
            return result;
        }

        public List<LoreEntry> GetEnvironmentalLore(string zoneTag, int corruption)
        {
            string key = ELoreChannel.Environmental.ToString();
            if (!entriesByChannel.TryGetValue(key, out var entries))
                return new List<LoreEntry>();

            return entries
                .Where(e => e.zoneTag == zoneTag && corruption >= e.minCorruptionToReveal)
                .Select(CloneEntry)
                .ToList();
        }

        public List<LoreEntry> GetDirectorLore(int corruption)
        {
            string key = ELoreChannel.Director.ToString();
            if (!entriesByChannel.TryGetValue(key, out var entries))
                return new List<LoreEntry>();

            return entries
                .Where(e => corruption >= e.minCorruptionToReveal)
                .Select(e =>
                {
                    var copy = CloneEntry(e);
                    if (copy.isRedacted && !string.IsNullOrEmpty(copy.redactedContent))
                        copy.content = copy.redactedContent;
                    return copy;
                })
                .ToList();
        }

        public void MarkEntryAsRead(string entryId)
        {
            if (readEntries.Add(entryId))
            {
                SaveReadProgress();
                OnEntryRead?.Invoke(entryId);
            }
        }

        public bool IsEntryRead(string entryId) => readEntries.Contains(entryId);

        public int GetUnreadCount(string terminalId)
        {
            if (!entriesByTerminal.TryGetValue(terminalId, out var entries))
                return 0;
            return entries.Count(e => !readEntries.Contains(e.entryId));
        }

        public List<LoreEntry> GetAllEntries() => new List<LoreEntry>(allEntries);

        public void SaveReadProgress()
        {
            var data = new ReadProgressData { readIds = readEntries.ToList() };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(READ_PROGRESS_KEY, json);
            PlayerPrefs.Save();
        }

        public void LoadReadProgress()
        {
            string json = PlayerPrefs.GetString(READ_PROGRESS_KEY, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            var data = JsonUtility.FromJson<ReadProgressData>(json);
            if (data?.readIds == null) return;

            readEntries.Clear();
            foreach (string id in data.readIds)
                readEntries.Add(id);
        }

        private LoreEntry CloneEntry(LoreEntry src)
        {
            return new LoreEntry
            {
                entryId = src.entryId,
                terminalId = src.terminalId,
                title = src.title,
                content = src.content,
                author = src.author,
                classification = src.classification,
                channel = src.channel,
                minCorruptionToReveal = src.minCorruptionToReveal,
                isRedacted = src.isRedacted,
                redactedContent = src.redactedContent,
                zoneTag = src.zoneTag
            };
        }

        [Serializable]
        private class ReadProgressData
        {
            public List<string> readIds = new List<string>();
        }
    }
}
