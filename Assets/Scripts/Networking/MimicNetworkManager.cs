using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.Core;

namespace MimicFacility.Networking
{
    public class MimicNetworkManager : NetworkManager
    {
        public event Action OnClientConnected;
        public event Action OnClientDisconnected;
        public event Action<NetworkConnectionToClient> OnPlayerJoinedServer;
        public event Action<NetworkConnectionToClient> OnPlayerLeftServer;

        [Header("Mimic Prefabs")]
        [SerializeField] private List<GameObject> mimicTypePrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> gearTypePrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> facilityObjectPrefabs = new List<GameObject>();

        [Header("Server Settings")]
        [SerializeField] private GameObject roundManagerPrefab;
        [SerializeField] private GameObject gameStatePrefab;

        [Header("Network Diagnostics")]
        [SerializeField] private float diagnosticInterval = 5f;

        private int _nextSubjectNumber = 1;
        private readonly Dictionary<int, float> _playerPings = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _playerPacketLoss = new Dictionary<int, float>();
        private float _diagnosticTimer;

        public IReadOnlyDictionary<int, float> PlayerPings => _playerPings;

        public override void Awake()
        {
            base.Awake();
            maxConnections = 4;
        }

        public override void Start()
        {
            base.Start();
            RegisterSpawnPrefabs();
        }

        private void RegisterSpawnPrefabs()
        {
            RegisterPrefabList(mimicTypePrefabs);
            RegisterPrefabList(gearTypePrefabs);
            RegisterPrefabList(facilityObjectPrefabs);
        }

        private void RegisterPrefabList(List<GameObject> prefabs)
        {
            foreach (var prefab in prefabs)
            {
                if (prefab != null && prefab.GetComponent<NetworkIdentity>() != null)
                {
                    if (!spawnPrefabs.Contains(prefab))
                        spawnPrefabs.Add(prefab);
                }
            }
        }

        public void ConfigureTransport(bool useSteam)
        {
            var transport = GetComponent<Transport>();
            if (transport == null) return;

            if (transport is kcp2k.KcpTransport kcp)
            {
                kcp.NoDelay = true;
                kcp.Interval = 10;
                kcp.Timeout = 10000;
                kcp.SendWindowSize = 256;
                kcp.ReceiveWindowSize = 256;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _nextSubjectNumber = 1;
            SpawnServerSystems();
        }

        private void SpawnServerSystems()
        {
            if (roundManagerPrefab != null)
            {
                GameObject rm = Instantiate(roundManagerPrefab);
                NetworkServer.Spawn(rm);
            }

            if (gameStatePrefab != null)
            {
                GameObject gs = Instantiate(gameStatePrefab);
                NetworkServer.Spawn(gs);
            }
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (numPlayers >= maxConnections)
            {
                conn.Disconnect();
                return;
            }

            GameObject player = Instantiate(playerPrefab);
            NetworkServer.AddPlayerForConnection(conn, player);

            int subjectNumber = _nextSubjectNumber++;
            string playerName = $"Subject-{subjectNumber}";

            var playerState = player.GetComponent<MimicPlayerState>();
            if (playerState != null)
                playerState.Initialize(subjectNumber, playerName);

            if (GameManager.Instance != null)
                GameManager.Instance.RegisterPlayer(conn.connectionId, playerName);

            var gameState = FindObjectOfType<NetworkedGameState>();
            if (gameState != null)
                gameState.RegisterPlayer(conn.connectionId);

            _playerPings[conn.connectionId] = 0f;
            _playerPacketLoss[conn.connectionId] = 0f;

            OnPlayerJoinedServer?.Invoke(conn);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            int connId = conn.connectionId;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterPlayer(connId);

                if (GameManager.Instance.CurrentPhase == GameManager.EGamePhase.Playing)
                {
                    var gameState = FindObjectOfType<NetworkedGameState>();
                    if (gameState != null)
                        gameState.UnregisterPlayer(connId);
                }
            }

            _playerPings.Remove(connId);
            _playerPacketLoss.Remove(connId);

            OnPlayerLeftServer?.Invoke(conn);
            base.OnServerDisconnect(conn);
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            OnClientConnected?.Invoke();
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            OnClientDisconnected?.Invoke();

            if (GameManager.Instance != null)
                GameManager.Instance.TransitionToPhase(GameManager.EGamePhase.MainMenu);
        }

        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);
        }

        private void Update()
        {
            if (!NetworkServer.active) return;

            _diagnosticTimer += Time.deltaTime;
            if (_diagnosticTimer >= diagnosticInterval)
            {
                _diagnosticTimer = 0f;
                UpdateNetworkDiagnostics();
            }
        }

        private void UpdateNetworkDiagnostics()
        {
            foreach (var conn in NetworkServer.connections)
            {
                if (conn.Value == null) continue;

                int connId = conn.Key;
                double rtt = conn.Value.remoteTimeStamp > 0 ? NetworkTime.rtt : 0;
                _playerPings[connId] = (float)(rtt * 1000.0);
            }
        }

        public float GetPlayerPing(int connectionId)
        {
            return _playerPings.TryGetValue(connectionId, out float ping) ? ping : -1f;
        }

        public void HostGame()
        {
            ConfigureTransport(false);
            StartHost();

            if (GameManager.Instance != null)
                GameManager.Instance.TransitionToPhase(GameManager.EGamePhase.Lobby);
        }

        public void JoinGame(string address)
        {
            ConfigureTransport(false);
            networkAddress = address;
            StartClient();
        }

        public void LeaveGame()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
                StopHost();
            else if (NetworkClient.isConnected)
                StopClient();
            else if (NetworkServer.active)
                StopServer();

            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMainMenu();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            _nextSubjectNumber = 1;
            _playerPings.Clear();
            _playerPacketLoss.Clear();
        }
    }
}
