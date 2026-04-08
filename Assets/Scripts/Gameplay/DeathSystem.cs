using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;
using MimicFacility.Effects;

namespace MimicFacility.Gameplay
{
    public class DeathSystem : NetworkBehaviour
    {
        public event Action<string> OnPlayerDied;
        public event Action<string> OnPlayerConverted;
        public event Action OnAllPlayersDead;

        [Header("Spectator")]
        [SerializeField] private float spectatorFlySpeed = 8f;
        [SerializeField] private float spectatorLookSensitivity = 2f;

        [Header("Death Camera")]
        [SerializeField] private float deathZoomOutDistance = 3f;
        [SerializeField] private float deathZoomOutDuration = 1.5f;
        [SerializeField] private float deathRotateSpeed = 15f;

        [Header("Game Over")]
        [SerializeField] private float allDeadGameOverDelay = 10f;

        [SyncVar(hook = nameof(OnDeadCountChanged))]
        private int deadPlayerCount;
        public int DeadPlayerCount => deadPlayerCount;

        private readonly List<MimicPlayerState> _trackedPlayers = new List<MimicPlayerState>();
        private int _spectatorTargetIndex;
        private bool _isSpectating;
        private bool _isFreeLook;
        private Camera _spectatorCamera;
        private float _spectatorPitch;
        private float _spectatorYaw;
        private Coroutine _deathCameraCoroutine;
        private Coroutine _gameOverTimerCoroutine;
        private PostProcessController _postProcess;

        public override void OnStartServer()
        {
            deadPlayerCount = 0;
        }

        public override void OnStartClient()
        {
            _postProcess = FindObjectOfType<PostProcessController>();
        }

        [Server]
        public void RegisterPlayer(MimicPlayerState player)
        {
            if (player == null || _trackedPlayers.Contains(player)) return;
            _trackedPlayers.Add(player);
        }

        [Server]
        public void UnregisterPlayer(MimicPlayerState player)
        {
            if (player == null) return;
            _trackedPlayers.Remove(player);
            RecalculateDeadCount();
        }

        [Server]
        public void OnPlayerDeath(MimicPlayerState player)
        {
            if (player == null) return;

            player.Die();
            RecalculateDeadCount();

            string playerName = player.DisplayName ?? "Unknown";
            RpcNotifyDeath(playerName);

            var playerCharacter = player.GetComponent<PlayerCharacter>();
            if (playerCharacter != null)
            {
                Vector3 deathPosition = player.transform.position;
                Quaternion deathRotation = player.transform.rotation;
                var conn = player.connectionToClient;
                if (conn != null)
                    TargetEnterDeathCamera(conn, deathPosition, deathRotation);
            }

            CheckAllPlayersDead();
        }

        [Server]
        public void OnPlayerConverted(MimicPlayerState player)
        {
            if (player == null) return;

            player.Convert();
            RecalculateDeadCount();

            string playerName = player.DisplayName ?? "Unknown";
            RpcNotifyConverted(playerName);

            var conn = player.connectionToClient;
            if (conn != null)
            {
                Vector3 position = player.transform.position;
                Quaternion rotation = player.transform.rotation;
                TargetEnterDeathCamera(conn, position, rotation);
            }

            CheckAllPlayersDead();
        }

        [Server]
        public void RevivePlayer(MimicPlayerState player, Vector3 position)
        {
            if (player == null) return;

            player.Revive(player.MaxHealth);
            player.transform.position = position;

            RecalculateDeadCount();

            var conn = player.connectionToClient;
            if (conn != null)
                TargetExitSpectator(conn, position);

            if (_gameOverTimerCoroutine != null)
            {
                StopCoroutine(_gameOverTimerCoroutine);
                _gameOverTimerCoroutine = null;
            }
        }

        [Server]
        private void RecalculateDeadCount()
        {
            int count = 0;
            for (int i = _trackedPlayers.Count - 1; i >= 0; i--)
            {
                if (_trackedPlayers[i] == null)
                {
                    _trackedPlayers.RemoveAt(i);
                    continue;
                }
                if (!_trackedPlayers[i].IsAlive || _trackedPlayers[i].IsConverted)
                    count++;
            }
            deadPlayerCount = count;
        }

        [Server]
        private void CheckAllPlayersDead()
        {
            if (_trackedPlayers.Count == 0) return;

            bool anyAlive = false;
            foreach (var player in _trackedPlayers)
            {
                if (player == null) continue;
                if (player.IsAlive && !player.IsConverted)
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
            {
                OnAllPlayersDead?.Invoke();

                if (_gameOverTimerCoroutine != null)
                    StopCoroutine(_gameOverTimerCoroutine);
                _gameOverTimerCoroutine = StartCoroutine(GameOverTimerCoroutine());
            }
        }

        [Server]
        private IEnumerator GameOverTimerCoroutine()
        {
            yield return new WaitForSeconds(allDeadGameOverDelay);

            bool stillAllDead = true;
            foreach (var player in _trackedPlayers)
            {
                if (player != null && player.IsAlive && !player.IsConverted)
                {
                    stillAllDead = false;
                    break;
                }
            }

            if (stillAllDead)
                GameManager.Instance?.EndGame("Lose_AllPlayersDead");

            _gameOverTimerCoroutine = null;
        }

        // --- Client RPCs ---

        [ClientRpc]
        private void RpcNotifyDeath(string playerName)
        {
            OnPlayerDied?.Invoke(playerName);

            var hud = FindObjectOfType<MimicFacility.UI.HUDManager>();
            if (hud != null)
                hud.ShowNotification($"Subject {playerName} has been eliminated.", 5f);
        }

        [ClientRpc]
        private void RpcNotifyConverted(string playerName)
        {
            OnPlayerConverted?.Invoke(playerName);

            var hud = FindObjectOfType<MimicFacility.UI.HUDManager>();
            if (hud != null)
                hud.ShowNotification($"Subject {playerName} has been compromised.", 5f);
        }

        // --- Targeted client commands for the dying player ---

        [TargetRpc]
        private void TargetEnterDeathCamera(NetworkConnection target, Vector3 deathPosition, Quaternion deathRotation)
        {
            if (_deathCameraCoroutine != null)
                StopCoroutine(_deathCameraCoroutine);
            _deathCameraCoroutine = StartCoroutine(DeathCameraSequence(deathPosition, deathRotation));
        }

        [TargetRpc]
        private void TargetExitSpectator(NetworkConnection target, Vector3 respawnPosition)
        {
            ExitSpectatorMode();
        }

        // --- Death Camera ---

        private IEnumerator DeathCameraSequence(Vector3 deathPosition, Quaternion deathRotation)
        {
            DisableLocalPlayerControls();

            if (_postProcess != null)
                _postProcess.LerpProfileWeight(1f, 0.5f);

            Camera localCamera = GetLocalPlayerCamera();
            if (localCamera == null)
                localCamera = CreateSpectatorCamera(deathPosition);

            Vector3 startPos = localCamera.transform.position;
            Vector3 endPos = deathPosition + Vector3.up * deathZoomOutDistance + deathRotation * Vector3.back * deathZoomOutDistance;

            float elapsed = 0f;
            while (elapsed < deathZoomOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / deathZoomOutDuration);

                localCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
                localCamera.transform.LookAt(deathPosition + Vector3.up * 0.5f);

                yield return null;
            }

            float rotateTime = 0f;
            float holdDuration = 3f;
            while (rotateTime < holdDuration)
            {
                rotateTime += Time.deltaTime;
                localCamera.transform.RotateAround(deathPosition + Vector3.up * 0.5f, Vector3.up, deathRotateSpeed * Time.deltaTime);
                localCamera.transform.LookAt(deathPosition + Vector3.up * 0.5f);
                yield return null;
            }

            _deathCameraCoroutine = null;
            EnterSpectatorMode(localCamera);
        }

        // --- Spectator Mode ---

        private void EnterSpectatorMode(Camera camera)
        {
            _isSpectating = true;
            _isFreeLook = false;
            _spectatorCamera = camera;
            _spectatorTargetIndex = 0;

            if (InputManager.Instance != null)
                InputManager.Instance.EnableSpectatorInput();

            SnapToAlivePlayer();
        }

        private void ExitSpectatorMode()
        {
            _isSpectating = false;
            _isFreeLook = false;

            if (_deathCameraCoroutine != null)
            {
                StopCoroutine(_deathCameraCoroutine);
                _deathCameraCoroutine = null;
            }

            if (_spectatorCamera != null && _spectatorCamera != GetLocalPlayerCamera())
            {
                Destroy(_spectatorCamera.gameObject);
                _spectatorCamera = null;
            }

            if (_postProcess != null)
                _postProcess.ResetToNormal();

            EnableLocalPlayerControls();

            if (InputManager.Instance != null)
                InputManager.Instance.EnableGameplayInput();
        }

        private void Update()
        {
            if (!_isSpectating) return;

            if (Input.GetMouseButtonDown(0))
                CycleSpectatorTarget(1);
            else if (Input.GetMouseButtonDown(1))
                CycleSpectatorTarget(-1);

            if (Input.GetKeyDown(KeyCode.Tab))
                _isFreeLook = !_isFreeLook;

            if (_isFreeLook)
                UpdateFreeLook();
            else
                UpdateFollowTarget();
        }

        private void UpdateFreeLook()
        {
            if (_spectatorCamera == null) return;

            float mouseX = Input.GetAxis("Mouse X") * spectatorLookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * spectatorLookSensitivity;

            _spectatorYaw += mouseX;
            _spectatorPitch -= mouseY;
            _spectatorPitch = Mathf.Clamp(_spectatorPitch, -89f, 89f);

            _spectatorCamera.transform.rotation = Quaternion.Euler(_spectatorPitch, _spectatorYaw, 0f);

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            float up = Input.GetKey(KeyCode.Space) ? 1f : (Input.GetKey(KeyCode.LeftControl) ? -1f : 0f);

            Vector3 move = _spectatorCamera.transform.right * h
                         + _spectatorCamera.transform.forward * v
                         + Vector3.up * up;
            move = Vector3.ClampMagnitude(move, 1f) * spectatorFlySpeed;

            _spectatorCamera.transform.position += move * Time.deltaTime;
        }

        private void UpdateFollowTarget()
        {
            if (_spectatorCamera == null) return;

            var alivePlayers = GetAlivePlayers();
            if (alivePlayers.Count == 0) return;

            int index = Mathf.Clamp(_spectatorTargetIndex, 0, alivePlayers.Count - 1);
            var target = alivePlayers[index];
            if (target == null) return;

            Vector3 desiredPos = target.transform.position + Vector3.up * 2f + target.transform.forward * -2.5f;
            _spectatorCamera.transform.position = Vector3.Lerp(
                _spectatorCamera.transform.position, desiredPos, Time.deltaTime * 5f);
            _spectatorCamera.transform.LookAt(target.transform.position + Vector3.up * 1f);
        }

        private void CycleSpectatorTarget(int direction)
        {
            var alivePlayers = GetAlivePlayers();
            if (alivePlayers.Count == 0) return;

            _spectatorTargetIndex += direction;
            if (_spectatorTargetIndex < 0)
                _spectatorTargetIndex = alivePlayers.Count - 1;
            else if (_spectatorTargetIndex >= alivePlayers.Count)
                _spectatorTargetIndex = 0;

            _isFreeLook = false;
        }

        private void SnapToAlivePlayer()
        {
            var alivePlayers = GetAlivePlayers();
            if (alivePlayers.Count == 0 || _spectatorCamera == null) return;

            _spectatorTargetIndex = 0;
            var target = alivePlayers[0];
            if (target != null)
            {
                _spectatorCamera.transform.position = target.transform.position + Vector3.up * 2f + Vector3.back * 2.5f;
                _spectatorCamera.transform.LookAt(target.transform.position + Vector3.up * 1f);
            }
        }

        // --- Helpers ---

        private List<MimicPlayerState> GetAlivePlayers()
        {
            var alive = new List<MimicPlayerState>();
            foreach (var player in _trackedPlayers)
            {
                if (player != null && player.IsAlive && !player.IsConverted)
                    alive.Add(player);
            }
            return alive;
        }

        private Camera GetLocalPlayerCamera()
        {
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer == null) return null;

            var playerChar = localPlayer.GetComponent<PlayerCharacter>();
            if (playerChar == null) return null;

            return playerChar.GetComponentInChildren<Camera>();
        }

        private Camera CreateSpectatorCamera(Vector3 position)
        {
            var cameraObj = new GameObject("SpectatorCamera");
            cameraObj.transform.position = position + Vector3.up * 1.5f;

            _spectatorCamera = cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            return _spectatorCamera;
        }

        private void DisableLocalPlayerControls()
        {
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer == null) return;

            var controller = localPlayer.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;

            var playerChar = localPlayer.GetComponent<PlayerCharacter>();
            if (playerChar != null)
                playerChar.enabled = false;
        }

        private void EnableLocalPlayerControls()
        {
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer == null) return;

            var controller = localPlayer.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = true;

            var playerChar = localPlayer.GetComponent<PlayerCharacter>();
            if (playerChar != null)
                playerChar.enabled = true;

            var playerCamera = localPlayer.GetComponentInChildren<Camera>();
            if (playerCamera != null)
                playerCamera.enabled = true;

            var audioListener = localPlayer.GetComponentInChildren<AudioListener>();
            if (audioListener != null)
                audioListener.enabled = true;
        }

        private void OnDeadCountChanged(int oldVal, int newVal) { }
    }
}
