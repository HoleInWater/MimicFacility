using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;
using MimicFacility.Audio;

namespace MimicFacility.Gameplay
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(AudioSource))]
    public class ExtractionZone : NetworkBehaviour
    {
        [Header("Extraction Settings")]
        [SerializeField] private float countdownDuration = 10f;

        [Header("Visual")]
        [SerializeField] private Light zoneLight;
        [SerializeField] private Color baseLightColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private float baseLightIntensity = 1f;
        [SerializeField] private float maxLightIntensity = 4f;
        [SerializeField] private float pulseSpeed = 2f;

        [Header("Audio")]
        [SerializeField] private AudioSource alarmSource;
        [SerializeField] private AudioClip alarmClip;

        [Header("References")]
        [SerializeField] private NetworkedGameState gameState;
        [SerializeField] private DiagnosticTaskManager taskManager;

        [SyncVar(hook = nameof(OnCountdownChanged))]
        private float countdownTimer;

        [SyncVar(hook = nameof(OnExtractionActiveChanged))]
        private bool extractionActive;

        private readonly SyncList<int> playersInZone = new SyncList<int>();

        private Coroutine countdownCoroutine;
        private float pulseTimer;

        public float CountdownTimer => countdownTimer;
        public bool ExtractionActive => extractionActive;

        public override void OnStartServer()
        {
            countdownTimer = 0f;
            extractionActive = false;

            if (gameState == null)
                gameState = FindObjectOfType<NetworkedGameState>();
            if (taskManager == null)
                taskManager = FindObjectOfType<DiagnosticTaskManager>();

            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        public override void OnStartClient()
        {
            playersInZone.OnChange += OnPlayersInZoneChanged;
            UpdateVisuals();
        }

        private void Update()
        {
            UpdatePulse();
        }

        // ═══════════════════════════════════════════════════════════════
        // TRIGGER DETECTION
        // ═══════════════════════════════════════════════════════════════

        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            var playerState = other.GetComponent<MimicPlayerState>();
            if (playerState == null) return;
            if (playerState.connectionToClient == null) return;

            int connId = playerState.connectionToClient.connectionId;
            if (!playerState.IsAlive || playerState.IsConverted) return;
            if (playersInZone.Contains(connId)) return;

            playersInZone.Add(connId);
            playerState.SetZone("ExtractionZone");

            EvaluateExtractionConditions();
        }

        [ServerCallback]
        private void OnTriggerExit(Collider other)
        {
            var playerState = other.GetComponent<MimicPlayerState>();
            if (playerState == null) return;
            if (playerState.connectionToClient == null) return;

            int connId = playerState.connectionToClient.connectionId;
            if (!playersInZone.Contains(connId)) return;

            playersInZone.Remove(connId);
            playerState.SetZone("Unknown");

            if (extractionActive)
                CancelExtraction();
        }

        // ═══════════════════════════════════════════════════════════════
        // EXTRACTION LOGIC
        // ═══════════════════════════════════════════════════════════════

        [Server]
        private void EvaluateExtractionConditions()
        {
            if (extractionActive) return;
            if (!AreAllConditionsMet()) return;

            StartExtraction();
        }

        [Server]
        private bool AreAllConditionsMet()
        {
            if (gameState == null || taskManager == null) return false;

            if (!taskManager.AreAllRequiredTasksComplete) return false;

            foreach (var kvp in gameState.PlayerAliveStatus)
            {
                if (!kvp.Value) continue;

                var playerState = FindPlayerStateByConnection(kvp.Key);
                if (playerState == null) return false;
                if (playerState.IsConverted) continue;

                if (!playersInZone.Contains(kvp.Key))
                    return false;
            }

            return playersInZone.Count > 0;
        }

        [Server]
        private void StartExtraction()
        {
            extractionActive = true;
            countdownTimer = countdownDuration;

            if (countdownCoroutine != null)
                StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(RunCountdown());

            RpcExtractionStarted(countdownDuration);

            var director = FindObjectOfType<AI.Director.DirectorAI>();
            if (director != null)
                director.SpeakLine("You think you can leave?");
        }

        [Server]
        private void CancelExtraction()
        {
            if (!extractionActive) return;

            extractionActive = false;
            countdownTimer = 0f;

            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            RpcExtractionCancelled();
        }

        [Server]
        private IEnumerator RunCountdown()
        {
            while (countdownTimer > 0f)
            {
                yield return null;
                countdownTimer -= Time.deltaTime;

                if (!AreAllConditionsMet())
                {
                    CancelExtraction();
                    yield break;
                }
            }

            countdownTimer = 0f;
            CompleteExtraction();
        }

        [Server]
        private void CompleteExtraction()
        {
            extractionActive = false;

            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            if (gameState != null)
                gameState.CheckWinCondition();

            EventBus.GameWin();

            RpcExtractionComplete();

            var director = FindObjectOfType<AI.Director.DirectorAI>();
            if (director != null)
                director.SpeakLine("No... this isn't over. I'll remember all of you.");
        }

        // ═══════════════════════════════════════════════════════════════
        // CLIENT RPCs
        // ═══════════════════════════════════════════════════════════════

        [ClientRpc]
        private void RpcExtractionStarted(float duration)
        {
            if (alarmSource != null && alarmClip != null)
            {
                alarmSource.clip = alarmClip;
                alarmSource.loop = true;
                alarmSource.Play();
            }

            EventBus.ExtractionAvailable();
        }

        [ClientRpc]
        private void RpcExtractionCancelled()
        {
            if (alarmSource != null)
                alarmSource.Stop();
        }

        [ClientRpc]
        private void RpcExtractionComplete()
        {
            if (alarmSource != null)
                alarmSource.Stop();
        }

        // ═══════════════════════════════════════════════════════════════
        // VISUALS
        // ═══════════════════════════════════════════════════════════════

        private void UpdatePulse()
        {
            if (zoneLight == null) return;

            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;

            if (extractionActive)
            {
                float progress = 1f - Mathf.Clamp01(countdownTimer / countdownDuration);
                float intensityRange = Mathf.Lerp(baseLightIntensity, maxLightIntensity, progress);
                float pulseAmplitude = Mathf.Lerp(0.3f, 1f, progress);
                zoneLight.intensity = Mathf.Lerp(
                    intensityRange * (1f - pulseAmplitude),
                    intensityRange,
                    pulse
                );
                zoneLight.color = Color.Lerp(baseLightColor, Color.white, progress * 0.3f);
            }
            else
            {
                zoneLight.intensity = baseLightIntensity + pulse * 0.3f;
                zoneLight.color = baseLightColor;
            }
        }

        private void UpdateVisuals()
        {
            if (zoneLight != null)
            {
                zoneLight.color = baseLightColor;
                zoneLight.intensity = baseLightIntensity;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // SYNC HOOKS
        // ═══════════════════════════════════════════════════════════════

        private void OnCountdownChanged(float oldVal, float newVal) { }
        private void OnExtractionActiveChanged(bool oldVal, bool newVal) => UpdateVisuals();
        private void OnPlayersInZoneChanged(SyncList<int>.Operation op, int index, int oldItem, int newItem) { }

        // ═══════════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════════

        private MimicPlayerState FindPlayerStateByConnection(int connectionId)
        {
            foreach (var ps in FindObjectsOfType<MimicPlayerState>())
            {
                if (ps.connectionToClient != null && ps.connectionToClient.connectionId == connectionId)
                    return ps;
            }
            return null;
        }
    }
}
