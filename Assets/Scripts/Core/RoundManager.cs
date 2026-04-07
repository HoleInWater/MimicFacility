using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

namespace MimicFacility.Core
{
    public enum ERoundPhase
    {
        Briefing,
        Exploration,
        Escalation,
        Extraction,
        Complete
    }

    [Serializable]
    public class MimicSpawnEntry
    {
        public GameObject prefab;
        public string mimicType;
        [Range(1, 10)] public int dangerTier = 1;
        [Range(0f, 1f)] public float baseWeight = 1f;
    }

    public class RoundManager : NetworkBehaviour
    {
        public event Action<int> OnRoundStarted;
        public event Action<int> OnRoundEnded;
        public event Action<ERoundPhase> OnPhaseChanged;

        [SerializeField] private float briefingDuration = 15f;
        [SerializeField] private float round1Duration = 120f;
        [SerializeField] private float standardRoundDuration = 180f;
        [SerializeField] private float extractionDuration = 60f;
        [SerializeField] private float navMeshSampleRadius = 10f;
        [SerializeField] private List<MimicSpawnEntry> mimicPrefabs = new List<MimicSpawnEntry>();
        [SerializeField] private List<Transform> spawnAnchors = new List<Transform>();

        [SyncVar(hook = nameof(OnCurrentRoundChanged))]
        private int currentRound;
        public int CurrentRound => currentRound;

        [SyncVar(hook = nameof(OnCurrentPhaseChanged))]
        private ERoundPhase currentPhase;
        public ERoundPhase CurrentPhase => currentPhase;

        [SyncVar]
        private float roundTimeRemaining;
        public float RoundTimeRemaining => roundTimeRemaining;

        private Coroutine _roundTimerCoroutine;
        private readonly List<GameObject> _activeMimics = new List<GameObject>();

        public void BeginFirstRound()
        {
            if (!isServer) return;
            currentRound = 0;
            StartNextRound();
        }

        [Server]
        public void StartNextRound()
        {
            currentRound++;
            _activeMimics.RemoveAll(m => m == null);

            StartPhase(ERoundPhase.Briefing);
        }

        [Server]
        private void StartPhase(ERoundPhase phase)
        {
            currentPhase = phase;

            if (_roundTimerCoroutine != null)
                StopCoroutine(_roundTimerCoroutine);

            switch (phase)
            {
                case ERoundPhase.Briefing:
                    roundTimeRemaining = briefingDuration;
                    _roundTimerCoroutine = StartCoroutine(PhaseTimer(briefingDuration, () => StartPhase(ERoundPhase.Exploration)));
                    break;

                case ERoundPhase.Exploration:
                    float duration = currentRound == 1 ? round1Duration : standardRoundDuration;
                    roundTimeRemaining = duration;
                    OnRoundStarted?.Invoke(currentRound);
                    RpcNotifyRoundStarted(currentRound);
                    SpawnMimicsForRound();
                    _roundTimerCoroutine = StartCoroutine(PhaseTimer(duration, () => StartPhase(ERoundPhase.Escalation)));
                    break;

                case ERoundPhase.Escalation:
                    SpawnEscalationMimics();
                    roundTimeRemaining = 0f;
                    StartPhase(ERoundPhase.Extraction);
                    return;

                case ERoundPhase.Extraction:
                    roundTimeRemaining = extractionDuration;
                    _roundTimerCoroutine = StartCoroutine(PhaseTimer(extractionDuration, () => StartPhase(ERoundPhase.Complete)));
                    break;

                case ERoundPhase.Complete:
                    roundTimeRemaining = 0f;
                    OnRoundEnded?.Invoke(currentRound);
                    RpcNotifyRoundEnded(currentRound);
                    break;
            }

            RpcNotifyPhaseChanged(phase);
        }

        [Server]
        public void AdvancePhase()
        {
            switch (currentPhase)
            {
                case ERoundPhase.Briefing:
                    StartPhase(ERoundPhase.Exploration);
                    break;
                case ERoundPhase.Exploration:
                    StartPhase(ERoundPhase.Escalation);
                    break;
                case ERoundPhase.Escalation:
                    StartPhase(ERoundPhase.Extraction);
                    break;
                case ERoundPhase.Extraction:
                    StartPhase(ERoundPhase.Complete);
                    break;
                case ERoundPhase.Complete:
                    StartNextRound();
                    break;
            }
        }

        [Server]
        private IEnumerator PhaseTimer(float duration, Action onComplete)
        {
            roundTimeRemaining = duration;
            while (roundTimeRemaining > 0f)
            {
                yield return new WaitForSeconds(1f);
                roundTimeRemaining = Mathf.Max(0f, roundTimeRemaining - 1f);
            }
            onComplete?.Invoke();
        }

        [Server]
        private void SpawnMimicsForRound()
        {
            int count = GetMimicCountForRound(currentRound);
            for (int i = 0; i < count; i++)
            {
                SpawnMimic(false);
            }
        }

        [Server]
        private void SpawnEscalationMimics()
        {
            int extra = Mathf.CeilToInt(currentRound * 0.5f);
            for (int i = 0; i < extra; i++)
            {
                SpawnMimic(true);
            }
        }

        private int GetMimicCountForRound(int round)
        {
            return round switch
            {
                1 => 2,
                2 => 4,
                _ => 4 + (round - 2) * 2
            };
        }

        [Server]
        private void SpawnMimic(bool preferDangerous)
        {
            if (mimicPrefabs.Count == 0) return;

            MimicSpawnEntry selected = SelectMimicType(preferDangerous);
            if (selected == null || selected.prefab == null) return;

            Vector3 spawnPosition = FindSpawnPosition();

            GameObject mimic = Instantiate(selected.prefab, spawnPosition, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
            NetworkServer.Spawn(mimic);
            _activeMimics.Add(mimic);

            var gameState = GameManager.Instance?.GameState;
            if (gameState != null)
                gameState.AddActiveMimic();
        }

        private MimicSpawnEntry SelectMimicType(bool preferDangerous)
        {
            float totalWeight = 0f;
            foreach (var entry in mimicPrefabs)
            {
                float weight = entry.baseWeight;
                if (preferDangerous)
                    weight *= entry.dangerTier;
                else
                    weight *= Mathf.Max(1f, entry.dangerTier * (currentRound / 3f));
                totalWeight += weight;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float accumulated = 0f;

            foreach (var entry in mimicPrefabs)
            {
                float weight = entry.baseWeight;
                if (preferDangerous)
                    weight *= entry.dangerTier;
                else
                    weight *= Mathf.Max(1f, entry.dangerTier * (currentRound / 3f));

                accumulated += weight;
                if (roll <= accumulated)
                    return entry;
            }

            return mimicPrefabs[mimicPrefabs.Count - 1];
        }

        private Vector3 FindSpawnPosition()
        {
            if (spawnAnchors.Count > 0)
            {
                Transform anchor = spawnAnchors[UnityEngine.Random.Range(0, spawnAnchors.Count)];
                Vector3 offset = UnityEngine.Random.insideUnitSphere * navMeshSampleRadius;
                offset.y = 0f;
                Vector3 candidate = anchor.position + offset;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                    return hit.position;

                return anchor.position;
            }

            Vector3 fallback = UnityEngine.Random.insideUnitSphere * 20f;
            fallback.y = 0f;
            if (NavMesh.SamplePosition(fallback, out NavMeshHit fallbackHit, navMeshSampleRadius * 2f, NavMesh.AllAreas))
                return fallbackHit.position;

            return Vector3.zero;
        }

        [ClientRpc]
        private void RpcNotifyRoundStarted(int round)
        {
            OnRoundStarted?.Invoke(round);
        }

        [ClientRpc]
        private void RpcNotifyRoundEnded(int round)
        {
            OnRoundEnded?.Invoke(round);
        }

        [ClientRpc]
        private void RpcNotifyPhaseChanged(ERoundPhase phase)
        {
            OnPhaseChanged?.Invoke(phase);
        }

        private void OnCurrentRoundChanged(int oldValue, int newValue) { }
        private void OnCurrentPhaseChanged(ERoundPhase oldValue, ERoundPhase newValue) { }

        public int GetActiveMimicCount() => _activeMimics.FindAll(m => m != null).Count;
    }
}
