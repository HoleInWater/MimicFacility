using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace MimicFacility.AI.Persistence
{
    [Serializable]
    public class DirectorMemoryData
    {
        public string groupHash;
        public int sessionCount;
        public int corruptionIndex;
        public List<string> rememberedFacts = new List<string>();
        public List<string> unaskedQuestions = new List<string>();
        public List<string> playerDisplayNames = new List<string>();
        public string lastEnding;
        public float totalPlaytimeSeconds;
        public int containmentAttempts;
        public int questionsAnswered;
        public int miscontainmentTotal;
    }

    public class DirectorMemory : MonoBehaviour
    {
        private DirectorMemoryData data;
        private string saveDirectory;

        public int SessionCount => data != null ? data.sessionCount : 0;
        public bool IsReturningGroup => data != null && data.sessionCount > 1;
        public DirectorMemoryData Data => data;

        private void Awake()
        {
            saveDirectory = Path.Combine(Application.persistentDataPath, "DirectorMemory");
            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);
        }

        public string ComputeGroupHash(List<string> playerIds)
        {
            var sorted = new List<string>(playerIds);
            sorted.Sort(StringComparer.Ordinal);
            string combined = string.Join("|", sorted);

            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(combined);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                var sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("x2"));
                return sb.ToString();
            }
        }

        public void SaveMemory()
        {
            if (data == null) return;

            string path = GetSavePath(data.groupHash);
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
                Debug.Log($"[DirectorMemory] Saved to {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DirectorMemory] Save failed: {e.Message}");
            }
        }

        public DirectorMemoryData LoadMemory(string groupHash)
        {
            string path = GetSavePath(groupHash);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<DirectorMemoryData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DirectorMemory] Load failed: {e.Message}");
                return null;
            }
        }

        public void InitializeForGroup(List<string> playerIds)
        {
            string hash = ComputeGroupHash(playerIds);
            var loaded = LoadMemory(hash);

            if (loaded != null)
            {
                data = loaded;
                data.sessionCount++;
                Debug.Log($"[DirectorMemory] Returning group detected. Session #{data.sessionCount}");
            }
            else
            {
                data = new DirectorMemoryData
                {
                    groupHash = hash,
                    sessionCount = 1
                };
                Debug.Log("[DirectorMemory] New group initialized.");
            }

            data.playerDisplayNames.Clear();
            foreach (string id in playerIds)
                data.playerDisplayNames.Add(id);

            SaveMemory();
        }

        public void RecordSessionEnd(int corruption, string ending, float playtime, int containments, int questions)
        {
            if (data == null) return;

            data.corruptionIndex = corruption;
            data.lastEnding = ending;
            data.totalPlaytimeSeconds += playtime;
            data.containmentAttempts += containments;
            data.questionsAnswered += questions;

            SaveMemory();
        }

        public void AddRememberedFact(string fact)
        {
            if (data == null) return;
            if (!data.rememberedFacts.Contains(fact))
            {
                data.rememberedFacts.Add(fact);
                if (data.rememberedFacts.Count > 50)
                    data.rememberedFacts.RemoveAt(0);
            }
        }

        public void AddUnaskedQuestion(string question)
        {
            if (data == null) return;
            if (!data.unaskedQuestions.Contains(question))
            {
                data.unaskedQuestions.Add(question);
                if (data.unaskedQuestions.Count > 20)
                    data.unaskedQuestions.RemoveAt(0);
            }
        }

        public string GetSessionGreeting()
        {
            if (data == null) return "The facility is online.";

            switch (data.sessionCount)
            {
                case 1:
                    return "You are later than expected. The facility has been waiting.";
                case 2:
                    return "Welcome back.";
                default:
                    return "I have been thinking about our last conversation.";
            }
        }

        private string GetSavePath(string groupHash)
        {
            return Path.Combine(saveDirectory, groupHash + ".json");
        }
    }
}
