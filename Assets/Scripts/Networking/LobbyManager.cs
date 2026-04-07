using System;
using System.Collections;
using UnityEngine;
using Mirror;
using MimicFacility.Core;

namespace MimicFacility.Networking
{
    [Serializable]
    public struct LobbyPlayerData
    {
        public int connectionId;
        public string playerName;
        public bool isReady;
        public int subjectNumber;
        public int skinSelection;
    }

    public class LobbyManager : NetworkBehaviour
    {
        public event Action OnLobbyUpdated;
        public event Action<float> OnCountdownStarted;
        public event Action OnCountdownCancelled;
        public event Action OnGameStarting;

        [SerializeField] private int minPlayersToStart = 2;
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private float countdownDuration = 5f;

        private readonly SyncList<LobbyPlayerData> _lobbyPlayers = new SyncList<LobbyPlayerData>();
        public SyncList<LobbyPlayerData> LobbyPlayers => _lobbyPlayers;

        [SyncVar]
        private bool isCountingDown;
        public bool IsCountingDown => isCountingDown;

        [SyncVar]
        private float countdownTimer;
        public float CountdownTimer => countdownTimer;

        [SyncVar]
        private int hostConnectionId = -1;

        private Coroutine _countdownCoroutine;

        public override void OnStartClient()
        {
            _lobbyPlayers.Callback += OnLobbyPlayersChanged;
        }

        public override void OnStopClient()
        {
            _lobbyPlayers.Callback -= OnLobbyPlayersChanged;
        }

        [Server]
        public void AddPlayer(int connectionId, string playerName, int subjectNumber)
        {
            if (_lobbyPlayers.Count >= maxPlayers) return;

            foreach (var p in _lobbyPlayers)
            {
                if (p.connectionId == connectionId) return;
            }

            var data = new LobbyPlayerData
            {
                connectionId = connectionId,
                playerName = playerName,
                isReady = false,
                subjectNumber = subjectNumber,
                skinSelection = 0
            };

            _lobbyPlayers.Add(data);

            if (_lobbyPlayers.Count == 1)
                hostConnectionId = connectionId;

            RpcUpdateLobbyUI();
        }

        [Server]
        public void RemovePlayer(int connectionId)
        {
            for (int i = _lobbyPlayers.Count - 1; i >= 0; i--)
            {
                if (_lobbyPlayers[i].connectionId == connectionId)
                {
                    _lobbyPlayers.RemoveAt(i);
                    break;
                }
            }

            if (connectionId == hostConnectionId)
                PromoteNewHost();

            if (isCountingDown && !AllPlayersReady())
                CancelCountdown();

            RpcUpdateLobbyUI();
        }

        [Server]
        private void PromoteNewHost()
        {
            if (_lobbyPlayers.Count > 0)
                hostConnectionId = _lobbyPlayers[0].connectionId;
            else
                hostConnectionId = -1;
        }

        public bool IsHost(int connectionId) => connectionId == hostConnectionId;

        [Command(requiresAuthority = false)]
        public void CmdSetReady(int connectionId, bool ready)
        {
            for (int i = 0; i < _lobbyPlayers.Count; i++)
            {
                if (_lobbyPlayers[i].connectionId == connectionId)
                {
                    var data = _lobbyPlayers[i];
                    data.isReady = ready;
                    _lobbyPlayers[i] = data;
                    break;
                }
            }

            if (AllPlayersReady() && _lobbyPlayers.Count >= minPlayersToStart)
                StartCountdown();
            else if (isCountingDown && !AllPlayersReady())
                CancelCountdown();

            RpcUpdateLobbyUI();
        }

        [Command(requiresAuthority = false)]
        public void CmdChangeSkin(int connectionId, int skinIndex)
        {
            for (int i = 0; i < _lobbyPlayers.Count; i++)
            {
                if (_lobbyPlayers[i].connectionId == connectionId)
                {
                    var data = _lobbyPlayers[i];
                    data.skinSelection = skinIndex;
                    _lobbyPlayers[i] = data;
                    break;
                }
            }

            RpcUpdateLobbyUI();
        }

        [Command(requiresAuthority = false)]
        public void CmdStartGame(int connectionId)
        {
            if (connectionId != hostConnectionId) return;
            if (_lobbyPlayers.Count < minPlayersToStart) return;
            if (!AllPlayersReady()) return;

            LaunchGame();
        }

        public bool AllPlayersReady()
        {
            if (_lobbyPlayers.Count == 0) return false;

            foreach (var player in _lobbyPlayers)
            {
                if (!player.isReady) return false;
            }
            return true;
        }

        [Server]
        private void StartCountdown()
        {
            if (isCountingDown) return;

            isCountingDown = true;
            countdownTimer = countdownDuration;
            _countdownCoroutine = StartCoroutine(CountdownRoutine());
            RpcCountdownStarted(countdownDuration);
        }

        [Server]
        private void CancelCountdown()
        {
            if (!isCountingDown) return;

            isCountingDown = false;
            countdownTimer = 0f;

            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            RpcCountdownCancelled();
        }

        [Server]
        private IEnumerator CountdownRoutine()
        {
            countdownTimer = countdownDuration;

            while (countdownTimer > 0f)
            {
                yield return new WaitForSeconds(1f);
                countdownTimer -= 1f;

                if (!AllPlayersReady())
                {
                    CancelCountdown();
                    yield break;
                }
            }

            LaunchGame();
        }

        [Server]
        private void LaunchGame()
        {
            isCountingDown = false;

            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            RpcGameStarting();

            if (GameManager.Instance != null)
                GameManager.Instance.StartGame();
        }

        [ClientRpc]
        private void RpcUpdateLobbyUI()
        {
            OnLobbyUpdated?.Invoke();
        }

        [ClientRpc]
        private void RpcCountdownStarted(float duration)
        {
            OnCountdownStarted?.Invoke(duration);
        }

        [ClientRpc]
        private void RpcCountdownCancelled()
        {
            OnCountdownCancelled?.Invoke();
        }

        [ClientRpc]
        private void RpcGameStarting()
        {
            OnGameStarting?.Invoke();
        }

        private void OnLobbyPlayersChanged(SyncList<LobbyPlayerData>.Operation op, int index, LobbyPlayerData oldItem, LobbyPlayerData newItem)
        {
            OnLobbyUpdated?.Invoke();
        }

        public LobbyPlayerData? GetPlayerData(int connectionId)
        {
            foreach (var p in _lobbyPlayers)
            {
                if (p.connectionId == connectionId)
                    return p;
            }
            return null;
        }

        public int PlayerCount => _lobbyPlayers.Count;
    }
}
