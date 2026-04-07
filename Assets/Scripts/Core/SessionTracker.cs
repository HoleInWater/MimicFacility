using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MimicFacility.Core
{
    [Serializable]
    public class SessionEvent
    {
        public string eventType;
        public float timestamp;
        public string data;
    }

    [Serializable]
    public class SessionData
    {
        public string sessionId;
        public string startTime;
        public string endTime;
        public int roundsPlayed;
        public int mimicsContained;
        public int falsePositives;
        public int diagnosticTasksCompleted;
        public string ending;
        public int playerCount;
        public float corruptionDelta;
        public List<SessionEvent> events = new List<SessionEvent>();
    }

    [Serializable]
    public class SessionHistory
    {
        public List<SessionData> sessions = new List<SessionData>();
    }

    public class SessionTracker : MonoBehaviour
    {
        private SessionData _currentSession;
        private string _sessionsDirectory;
        private float _sessionStartRealtime;

        public SessionData CurrentSession => _currentSession;

        private void Awake()
        {
            _sessionsDirectory = Path.Combine(Application.persistentDataPath, "sessions");
            if (!Directory.Exists(_sessionsDirectory))
                Directory.CreateDirectory(_sessionsDirectory);
        }

        public void StartSession(int playerCount)
        {
            _currentSession = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = DateTime.UtcNow.ToString("o"),
                playerCount = playerCount,
                roundsPlayed = 0,
                mimicsContained = 0,
                falsePositives = 0,
                diagnosticTasksCompleted = 0,
                ending = "InProgress",
                corruptionDelta = 0f
            };
            _sessionStartRealtime = Time.realtimeSinceStartup;
        }

        public void EndSession(string ending, int roundsPlayed, int mimicsContained, int falsePositives, int tasksCompleted, float corruptionDelta)
        {
            if (_currentSession == null) return;

            _currentSession.endTime = DateTime.UtcNow.ToString("o");
            _currentSession.ending = ending;
            _currentSession.roundsPlayed = roundsPlayed;
            _currentSession.mimicsContained = mimicsContained;
            _currentSession.falsePositives = falsePositives;
            _currentSession.diagnosticTasksCompleted = tasksCompleted;
            _currentSession.corruptionDelta = corruptionDelta;

            SaveSession(_currentSession);
            _currentSession = null;
        }

        public void RecordEvent(string eventType, Dictionary<string, object> data = null)
        {
            if (_currentSession == null) return;

            var sessionEvent = new SessionEvent
            {
                eventType = eventType,
                timestamp = Time.realtimeSinceStartup - _sessionStartRealtime,
                data = data != null ? JsonUtility.ToJson(new SerializableDict(data)) : ""
            };

            _currentSession.events.Add(sessionEvent);
        }

        private void SaveSession(SessionData session)
        {
            try
            {
                string filename = $"session_{session.sessionId}.json";
                string path = Path.Combine(_sessionsDirectory, filename);
                string json = JsonUtility.ToJson(session, true);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to save session: {e.Message}");
            }
        }

        public List<SessionData> GetSessionHistory()
        {
            var history = new List<SessionData>();

            if (!Directory.Exists(_sessionsDirectory))
                return history;

            try
            {
                string[] files = Directory.GetFiles(_sessionsDirectory, "session_*.json");
                foreach (string file in files)
                {
                    string json = File.ReadAllText(file);
                    var session = JsonUtility.FromJson<SessionData>(json);
                    if (session != null)
                        history.Add(session);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load session history: {e.Message}");
            }

            return history;
        }

        public float GetTotalPlaytime()
        {
            float total = 0f;
            var sessions = GetSessionHistory();

            foreach (var session in sessions)
            {
                if (string.IsNullOrEmpty(session.startTime) || string.IsNullOrEmpty(session.endTime))
                    continue;

                if (DateTime.TryParse(session.startTime, out DateTime start) &&
                    DateTime.TryParse(session.endTime, out DateTime end))
                {
                    total += (float)(end - start).TotalSeconds;
                }
            }

            return total;
        }

        [Serializable]
        private class SerializableDict
        {
            public string json;

            public SerializableDict(Dictionary<string, object> dict)
            {
                var parts = new List<string>();
                foreach (var kvp in dict)
                    parts.Add($"\"{kvp.Key}\":\"{kvp.Value}\"");
                json = "{" + string.Join(",", parts) + "}";
            }
        }
    }
}
