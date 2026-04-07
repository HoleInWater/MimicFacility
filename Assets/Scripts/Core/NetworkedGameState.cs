using System;
using UnityEngine;
using Mirror;

namespace MimicFacility.Core
{
    public class NetworkedGameState : NetworkBehaviour
    {
        public event Action OnWinConditionMet;
        public event Action<string> OnLoseConditionMet;
        public event Action OnStateUpdated;

        [SyncVar(hook = nameof(OnActiveMimicCountChanged))]
        private int activeMimicCount;
        public int ActiveMimicCount => activeMimicCount;

        [SyncVar(hook = nameof(OnContainedMimicCountChanged))]
        private int containedMimicCount;
        public int ContainedMimicCount => containedMimicCount;

        [SyncVar(hook = nameof(OnMiscontainmentCountChanged))]
        private int miscontainmentCount;
        public int MiscontainmentCount => miscontainmentCount;

        [SyncVar(hook = nameof(OnCurrentRoundChanged))]
        private int currentRound;
        public int CurrentRound => currentRound;

        [SyncVar(hook = nameof(OnDiagnosticTasksChanged))]
        private int diagnosticTasksCompleted;
        public int DiagnosticTasksCompleted => diagnosticTasksCompleted;

        [SyncVar]
        private int requiredTasksForExtraction = 3;
        public int RequiredTasksForExtraction => requiredTasksForExtraction;

        [SerializeField] private int maxMiscontainments = 3;

        private readonly SyncDictionary<int, bool> playerAliveStatus = new SyncDictionary<int, bool>();
        public SyncDictionary<int, bool> PlayerAliveStatus => playerAliveStatus;

        private bool _gameResolved;

        public override void OnStartServer()
        {
            ResetState();
        }

        public override void OnStartClient()
        {
            playerAliveStatus.Callback += OnPlayerAliveStatusChanged;
        }

        [Server]
        public void ResetState()
        {
            activeMimicCount = 0;
            containedMimicCount = 0;
            miscontainmentCount = 0;
            currentRound = 0;
            diagnosticTasksCompleted = 0;
            _gameResolved = false;
            playerAliveStatus.Clear();
        }

        [Server]
        public void SetCurrentRound(int round)
        {
            currentRound = round;
        }

        [Server]
        public void SetRequiredTasks(int count)
        {
            requiredTasksForExtraction = Mathf.Max(1, count);
        }

        [Server]
        public void AddActiveMimic()
        {
            activeMimicCount++;
            BroadcastStateUpdate();
        }

        [Server]
        public void RemoveActiveMimic()
        {
            activeMimicCount = Mathf.Max(0, activeMimicCount - 1);
            BroadcastStateUpdate();
        }

        [Server]
        public void IncrementContained()
        {
            containedMimicCount++;
            RemoveActiveMimic();
            CheckWinCondition();
            BroadcastStateUpdate();
        }

        [Server]
        public void IncrementMiscontainment()
        {
            miscontainmentCount++;
            CheckLoseCondition();
            BroadcastStateUpdate();
        }

        [Server]
        public void SetPlayerAlive(int connectionId, bool alive)
        {
            playerAliveStatus[connectionId] = alive;

            if (!alive)
                CheckLoseCondition();

            BroadcastStateUpdate();
        }

        [Server]
        public void RegisterPlayer(int connectionId)
        {
            playerAliveStatus[connectionId] = true;
        }

        [Server]
        public void UnregisterPlayer(int connectionId)
        {
            playerAliveStatus.Remove(connectionId);
            CheckLoseCondition();
        }

        [Server]
        public void CompleteDiagnosticTask()
        {
            diagnosticTasksCompleted++;
            CheckWinCondition();
            BroadcastStateUpdate();
        }

        [Server]
        public bool CheckWinCondition()
        {
            if (_gameResolved) return false;
            if (diagnosticTasksCompleted < requiredTasksForExtraction) return false;

            bool allAliveInExtraction = true;
            foreach (var kvp in playerAliveStatus)
            {
                if (!kvp.Value) continue;

                var playerState = FindPlayerState(kvp.Key);
                if (playerState == null || playerState.CurrentZone != "ExtractionZone")
                {
                    allAliveInExtraction = false;
                    break;
                }
            }

            if (!allAliveInExtraction) return false;

            _gameResolved = true;
            OnWinConditionMet?.Invoke();
            RpcNotifyWin();
            GameManager.Instance?.EndGame("Win_Extraction");
            return true;
        }

        [Server]
        public bool CheckLoseCondition()
        {
            if (_gameResolved) return false;

            if (miscontainmentCount >= maxMiscontainments)
            {
                _gameResolved = true;
                string reason = "Lose_TooManyMiscontainments";
                OnLoseConditionMet?.Invoke(reason);
                RpcNotifyLose(reason);
                GameManager.Instance?.EndGame(reason);
                return true;
            }

            bool anyAlive = false;
            foreach (var kvp in playerAliveStatus)
            {
                if (kvp.Value)
                {
                    var ps = FindPlayerState(kvp.Key);
                    if (ps == null || !ps.IsConverted)
                    {
                        anyAlive = true;
                        break;
                    }
                }
            }

            if (!anyAlive && playerAliveStatus.Count > 0)
            {
                _gameResolved = true;
                string reason = "Lose_AllPlayersDead";
                OnLoseConditionMet?.Invoke(reason);
                RpcNotifyLose(reason);
                GameManager.Instance?.EndGame(reason);
                return true;
            }

            return false;
        }

        private MimicPlayerState FindPlayerState(int connectionId)
        {
            foreach (var ps in FindObjectsOfType<MimicPlayerState>())
            {
                if (ps.connectionToClient != null && ps.connectionToClient.connectionId == connectionId)
                    return ps;
            }
            return null;
        }

        [ClientRpc]
        private void BroadcastStateUpdate()
        {
            OnStateUpdated?.Invoke();
        }

        [ClientRpc]
        private void RpcNotifyWin()
        {
            OnWinConditionMet?.Invoke();
        }

        [ClientRpc]
        private void RpcNotifyLose(string reason)
        {
            OnLoseConditionMet?.Invoke(reason);
        }

        private void OnActiveMimicCountChanged(int oldVal, int newVal) => OnStateUpdated?.Invoke();
        private void OnContainedMimicCountChanged(int oldVal, int newVal) => OnStateUpdated?.Invoke();
        private void OnMiscontainmentCountChanged(int oldVal, int newVal) => OnStateUpdated?.Invoke();
        private void OnCurrentRoundChanged(int oldVal, int newVal) => OnStateUpdated?.Invoke();
        private void OnDiagnosticTasksChanged(int oldVal, int newVal) => OnStateUpdated?.Invoke();
        private void OnPlayerAliveStatusChanged(SyncIDictionary<int, bool>.Operation op, int key, bool item) => OnStateUpdated?.Invoke();
    }
}
