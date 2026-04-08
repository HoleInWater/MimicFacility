using System;
using UnityEngine;

namespace MimicFacility.Gameplay
{
    public enum GameEventType
    {
        EntitySpotted,
        PlayerDeath,
        PlayerConverted,
        MimicContained,
        Miscontainment,
        TaskCompleted,
        RoundStarted,
        RoundEnded,
        DirectorSpoke,
        ExtractionAvailable,
        GameOver,
        GameWin
    }

    public static class EventBus
    {
        public static event Action<string, string> OnEntitySpotted;
        public static event Action<string> OnPlayerDeath;
        public static event Action<string> OnPlayerConverted;
        public static event Action<string, string> OnMimicContained;
        public static event Action<string> OnMiscontainment;
        public static event Action<string, string> OnTaskCompleted;
        public static event Action<int> OnRoundStarted;
        public static event Action<int> OnRoundEnded;
        public static event Action<string> OnDirectorSpoke;
        public static event Action OnExtractionAvailable;
        public static event Action<string> OnGameOver;
        public static event Action OnGameWin;
        public static event Action<string, string> OnMimicReproduced;

        public static void EntitySpotted(string entityType, string location)
        {
            OnEntitySpotted?.Invoke(entityType, location);
        }

        public static void PlayerDeath(string playerName)
        {
            OnPlayerDeath?.Invoke(playerName);
        }

        public static void PlayerConverted(string playerName)
        {
            OnPlayerConverted?.Invoke(playerName);
        }

        public static void MimicContained(string playerName, string mimicType)
        {
            OnMimicContained?.Invoke(playerName, mimicType);
        }

        public static void Miscontainment(string playerName)
        {
            OnMiscontainment?.Invoke(playerName);
        }

        public static void TaskCompleted(string taskName, string playerName)
        {
            OnTaskCompleted?.Invoke(taskName, playerName);
        }

        public static void RoundStarted(int roundNumber)
        {
            OnRoundStarted?.Invoke(roundNumber);
        }

        public static void RoundEnded(int roundNumber)
        {
            OnRoundEnded?.Invoke(roundNumber);
        }

        public static void DirectorSpoke(string message)
        {
            OnDirectorSpoke?.Invoke(message);
        }

        public static void ExtractionAvailable()
        {
            OnExtractionAvailable?.Invoke();
        }

        public static void GameOver(string reason)
        {
            OnGameOver?.Invoke(reason);
        }

        public static void GameWin()
        {
            OnGameWin?.Invoke();
        }

        public static void MimicReproduced(string triggerWord, string playerId)
        {
            OnMimicReproduced?.Invoke(triggerWord, playerId);
        }

        public static void Clear()
        {
            OnEntitySpotted = null;
            OnPlayerDeath = null;
            OnPlayerConverted = null;
            OnMimicContained = null;
            OnMiscontainment = null;
            OnTaskCompleted = null;
            OnRoundStarted = null;
            OnRoundEnded = null;
            OnDirectorSpoke = null;
            OnExtractionAvailable = null;
            OnGameOver = null;
            OnGameWin = null;
            OnMimicReproduced = null;
        }
    }
}
