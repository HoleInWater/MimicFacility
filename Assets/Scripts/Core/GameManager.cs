using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MimicFacility.Core
{
    public class GameManager : MonoBehavior
    {
        public enum EGamePhase
        {
            MainMenu,
            Lobby,
            Loading,
            Playing,
            PostGame
        }
    
        public enum EDifficulty
        {
            Standard,
            Hard,
            Nightmare
        }
    
        [Serializable]
        public class PlayerData
        {
            public int connectionId;
            public string displayName;
            public int subjectNumber;
            public bool isAlive;
            public bool isConverted;
    
            public PlayerData(int connectionId, string displayName, int subjectNumber)
            {
                this.connectionId = connectionId;
                this.displayName = displayName;
                this.subjectNumber = subjectNumber;
                isAlive = true;
                isConverted = false;
            }
        }
        
        public static GameManager Instance { get; private set; }
    
        public event Action OnGameStarted;
        public event Action<string> OnGameEnded;
        public event Action<int, PlayerData> OnPlayerJoined;
        public event Action<int> OnPlayerLeft;
        public event Action<EGamePhase> OnPhaseChanged;
    
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private EDifficulty difficulty = EDifficulty.Standard;
        [SerializeField] private bool enableDeviceHorror = true;
    
            public int MaxPlayers => maxPlayers;
            public EDifficulty Difficulty => difficulty;
            public bool EnableDeviceHorror => enableDeviceHorror;
    
            private EGamePhase _currentPhase = EGamePhase.MainMenu;
            public EGamePhase CurrentPhase
            {
                get => _currentPhase;
                private set
                {
                    if (_currentPhase == value) return;
                    _currentPhase = value;
                    OnPhaseChanged?.Invoke(_currentPhase);
                }
            }
    
            private readonly Dictionary<int, PlayerData> _players = new Dictionary<int, PlayerData>();
            public IReadOnlyDictionary<int, PlayerData> Players => _players;
    
            public RoundManager RoundManager { get; private set; }
            public NetworkedGameState GameState { get; private set; }
    
            private bool _isPaused;
            public bool IsPaused => _isPaused;
    
            private int _nextSubjectNumber = 1;
    
            private void Awake()
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                Instance = this;
                DontDestroyOnLoad(gameObject);
    
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
    
            private void OnDestroy()
            {
                if (Instance == this)
                {
                    SceneManager.sceneLoaded -= OnSceneLoaded;
                    Instance = null;
                }
            }
    
            private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                RefreshSubsystemReferences();
    
                if (scene.name == "MainMenu")
                    CurrentPhase = EGamePhase.MainMenu;
            }
    
            private void RefreshSubsystemReferences()
            {
                RoundManager = FindObjectOfType<RoundManager>();
                GameState = FindObjectOfType<NetworkedGameState>();
            }
    
            public void SetDifficulty(EDifficulty newDifficulty)
            {
                difficulty = newDifficulty;
            }
    
            public void SetDeviceHorror(bool enabled)
            {
                enableDeviceHorror = enabled;
            }
    
            public void TransitionToPhase(EGamePhase phase)
            {
                CurrentPhase = phase;
            }
    
            public void StartGame()
            {
                if (CurrentPhase != EGamePhase.Lobby) return;
    
                CurrentPhase = EGamePhase.Loading;
                _isPaused = false;
    
                foreach (var kvp in _players)
                {
                    kvp.Value.isAlive = true;
                    kvp.Value.isConverted = false;
                }
    
                CurrentPhase = EGamePhase.Playing;
                OnGameStarted?.Invoke();
    
                if (RoundManager != null)
                    RoundManager.BeginFirstRound();
            }
    
            public void EndGame(string reason)
            {
                if (CurrentPhase != EGamePhase.Playing) return;
    
                CurrentPhase = EGamePhase.PostGame;
                _isPaused = false;
                Time.timeScale = 1f;
    
                OnGameEnded?.Invoke(reason);
            }
    
            public void PauseGame()
            {
                if (CurrentPhase != EGamePhase.Playing || _isPaused) return;
                _isPaused = true;
                Time.timeScale = 0f;
            }
    
            public void ResumeGame()
            {
                if (!_isPaused) return;
                _isPaused = false;
                Time.timeScale = 1f;
            }
    
            public PlayerData RegisterPlayer(int connectionId, string displayName)
            {
                if (_players.ContainsKey(connectionId)) return _players[connectionId];
                if (_players.Count >= maxPlayers) return null;
    
                int subjectNumber = _nextSubjectNumber++;
                if (_nextSubjectNumber > maxPlayers)
                    _nextSubjectNumber = 1;
    
                var data = new PlayerData(connectionId, displayName, subjectNumber);
                _players[connectionId] = data;
                OnPlayerJoined?.Invoke(connectionId, data);
                return data;
            }
    
            public void UnregisterPlayer(int connectionId)
            {
                if (!_players.ContainsKey(connectionId)) return;
                _players.Remove(connectionId);
                OnPlayerLeft?.Invoke(connectionId);
    
                if (_players.Count == 0 && CurrentPhase == EGamePhase.Playing)
                    EndGame("AllPlayersDisconnected");
            }
    
            public PlayerData GetPlayer(int connectionId)
            {
                _players.TryGetValue(connectionId, out var data);
                return data;
            }
    
            public int GetAlivePlayerCount()
            {
                int count = 0;
                foreach (var kvp in _players)
                {
                    if (kvp.Value.isAlive && !kvp.Value.isConverted)
                        count++;
                }
                return count;
            }
    
            public void ReturnToMainMenu()
            {
                _players.Clear();
                _nextSubjectNumber = 1;
                _isPaused = false;
                Time.timeScale = 1f;
                CurrentPhase = EGamePhase.MainMenu;
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
