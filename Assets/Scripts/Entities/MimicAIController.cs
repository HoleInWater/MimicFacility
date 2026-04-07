using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MimicFacility.Characters;
using MimicFacility.Core;
using MimicFacility.AI.Voice;

namespace MimicFacility.Entities
{
    public class MimicAIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MimicBase mimicBase;

        [Header("Patrol")]
        [SerializeField] private float patrolRadius = 15f;
        [SerializeField] private float patrolWaitTime = 3f;

        [Header("Stalking")]
        [SerializeField] private float stalkMinDistance = 8f;
        [SerializeField] private float stalkMaxDistance = 12f;
        [SerializeField] private int coverRayCount = 12;
        [SerializeField] private float coverCheckDistance = 10f;

        [Header("Impersonation")]
        [SerializeField] private float impersonateRange = 15f;

        [Header("Combat")]
        [SerializeField] private float healthFleeThreshold = 0.3f;
        [SerializeField] private int playerCountFleeThreshold = 3;

        [Header("Reproduction")]
        [SerializeField] private GameObject mimicPrefab;

        private MimicStateMachine _stateMachine;
        private NavMeshAgent _agent;
        private VoiceLearningSystem _voiceSystem;
        private float _patrolWaitTimer;
        private bool _reachedPatrolPoint;
        private PlayerCharacter _detectedPlayer;

        public MimicBase MimicBase => mimicBase;
        public NavMeshAgent Agent => _agent;

        private void Awake()
        {
            if (mimicBase == null)
                mimicBase = GetComponent<MimicBase>();

            _agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            _voiceSystem = FindObjectOfType<VoiceLearningSystem>();
            InitializeStateMachine();
            InvokeRepeating(nameof(UpdateAI), 0.5f, 0.5f);
        }

        private void InitializeStateMachine()
        {
            _stateMachine = new MimicStateMachine();

            var patrol = new PatrolState();
            var stalk = new StalkState();
            var impersonate = new ImpersonateState();
            var attack = new AttackState();
            var flee = new FleeState();
            var reproduce = new ReproduceState();

            _stateMachine.AddTransition(EMimicState.Patrol, EMimicState.Stalking,
                () => mimicBase.GetNearestPlayer() != null);

            _stateMachine.AddTransition(EMimicState.Stalking, EMimicState.Impersonating,
                () => IsCloseEnoughToImpersonate() && !IsTargetLooking());

            _stateMachine.AddTransition(EMimicState.Impersonating, EMimicState.Attacking,
                () => IsTargetTooClose() || IsTargetLooking());

            _stateMachine.AddTransition(EMimicState.Attacking, EMimicState.Fleeing,
                () => mimicBase.GetHealthPercent() < healthFleeThreshold
                    || mimicBase.GetNearbyPlayerCount(mimicBase.DetectionRange) >= playerCountFleeThreshold);

            _stateMachine.AddTransition(EMimicState.Fleeing, EMimicState.Patrol,
                () => mimicBase.GetNearestPlayer() == null);

            _stateMachine.RegisterState(EMimicState.Patrol, patrol);
            _stateMachine.RegisterState(EMimicState.Stalking, stalk);
            _stateMachine.RegisterState(EMimicState.Impersonating, impersonate);
            _stateMachine.RegisterState(EMimicState.Attacking, attack);
            _stateMachine.RegisterState(EMimicState.Fleeing, flee);
            _stateMachine.RegisterState(EMimicState.Reproducing, reproduce);

            _stateMachine.ChangeState(patrol, this);
            mimicBase.SetState(EMimicState.Patrol);
        }

        private void UpdateAI()
        {
            if (mimicBase == null) return;

            CheckTriggerWordReproduction();
            _stateMachine.Evaluate(this);
            _stateMachine.Execute(this);
        }

        private void CheckTriggerWordReproduction()
        {
            if (_voiceSystem == null || mimicBase.CurrentState == EMimicState.Reproducing) return;

            PlayerCharacter target = mimicBase.GetTargetPlayer();
            if (target == null) return;

            string playerId = target.netId.ToString();
            var phrases = _voiceSystem.GetRecentPhrases(playerId, 5f);

            foreach (var phrase in phrases)
            {
                string triggerWord = _voiceSystem.CheckTriggerWord(playerId, phrase.text);
                if (triggerWord != null)
                {
                    var reproduceState = _stateMachine.GetState(EMimicState.Reproducing);
                    if (reproduceState != null)
                    {
                        _stateMachine.ChangeState(reproduceState, this);
                        mimicBase.SetState(EMimicState.Reproducing);
                    }
                    return;
                }
            }
        }

        public void OnPlayerDetected(PlayerCharacter player)
        {
            if (_detectedPlayer == null)
            {
                _detectedPlayer = player;
                mimicBase.SetTarget(player.netId);
            }
        }

        private bool IsCloseEnoughToImpersonate()
        {
            PlayerCharacter target = mimicBase.GetTargetPlayer();
            if (target == null) return false;
            float dist = Vector3.Distance(transform.position, target.transform.position);
            return dist <= impersonateRange;
        }

        private bool IsTargetLooking()
        {
            PlayerCharacter target = mimicBase.GetTargetPlayer();
            if (target == null) return false;

            Vector3 toMimic = (transform.position - target.transform.position).normalized;
            float dot = Vector3.Dot(target.transform.forward, toMimic);
            return dot > 0.7f;
        }

        private bool IsTargetTooClose()
        {
            PlayerCharacter target = mimicBase.GetTargetPlayer();
            if (target == null) return false;
            return Vector3.Distance(transform.position, target.transform.position) < stalkMinDistance * 0.5f;
        }

        public Vector3 GetPatrolPoint()
        {
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
            randomDir.y = 0f;
            randomDir += transform.position;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
                return hit.position;

            return transform.position;
        }

        public Vector3 GetStalkPosition(PlayerCharacter target)
        {
            if (target == null) return transform.position;

            Vector3 behindPlayer = target.transform.position - target.transform.forward * stalkMaxDistance;
            behindPlayer.y = target.transform.position.y;

            Vector3 coverPos = FindCoverPosition(target.transform.position);
            if (coverPos != Vector3.zero)
                return coverPos;

            if (NavMesh.SamplePosition(behindPlayer, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                return hit.position;

            return transform.position;
        }

        public Vector3 FindCoverPosition(Vector3 from)
        {
            float angleStep = 360f / coverRayCount;
            Vector3 bestCover = Vector3.zero;
            float bestScore = float.MinValue;

            for (int i = 0; i < coverRayCount; i++)
            {
                float angle = i * angleStep;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 checkPos = transform.position + dir * coverCheckDistance;

                if (Physics.Raycast(transform.position, dir, out RaycastHit hit, coverCheckDistance))
                {
                    Vector3 coverPoint = hit.point - dir * 1f;

                    if (!NavMesh.SamplePosition(coverPoint, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                        continue;

                    float distFromThreat = Vector3.Distance(navHit.position, from);
                    float distFromSelf = Vector3.Distance(navHit.position, transform.position);

                    bool hiddenFromThreat = Physics.Linecast(navHit.position + Vector3.up, from + Vector3.up);

                    float score = distFromThreat * 0.5f - distFromSelf * 0.3f;
                    if (hiddenFromThreat) score += 10f;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCover = navHit.position;
                    }
                }
            }

            return bestCover;
        }

        public Vector3 GetFleePoint()
        {
            PlayerCharacter nearest = mimicBase.GetNearestPlayer();
            if (nearest == null) return GetPatrolPoint();

            Vector3 awayDir = (transform.position - nearest.transform.position).normalized;
            Vector3 fleeTarget = transform.position + awayDir * patrolRadius;

            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
                return hit.position;

            return GetPatrolPoint();
        }

        public void SpawnMimic()
        {
            if (mimicPrefab == null) return;

            Vector3 spawnPos = transform.position + transform.right * 1.5f;
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                spawnPos = hit.position;

            GameObject instance = Instantiate(mimicPrefab, spawnPos, Quaternion.identity);
            Mirror.NetworkServer.Spawn(instance);

            var gameState = GameManager.Instance?.GameState;
            gameState?.AddActiveMimic();
        }
    }
}
