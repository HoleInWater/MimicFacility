using UnityEngine;
using UnityEngine.AI;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;

namespace MimicFacility.Entities
{
    public enum EFraudState
    {
        Idle,
        Mirroring,
        Waving,
        LockedOn,
        Pursuing
    }

    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class Fraud : NetworkBehaviour
    {
        [Header("Mirroring")]
        [SerializeField] private float mirrorRange = 15f;
        [SerializeField] private float mirrorDelay = 0.3f;
        [SerializeField] private float waveDistance = 10f;
        [SerializeField] private float waveDuration = 2.5f;

        [Header("Lock-On")]
        [SerializeField] private float lockOnDistance = 8f;
        [SerializeField] private float pursuitSpeed = 5f;
        [SerializeField] private float pursuitAcceleration = 8f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 30f;
        [SerializeField] private float attackCooldown = 2f;

        [Header("Body Copy")]
        [SerializeField] private float poseMimicSpeed = 5f;

        [Header("Audio")]
        [SerializeField] private AudioSource fraudAudio;
        [SerializeField] private AudioClip waveSound;
        [SerializeField] private AudioClip lockOnSound;
        [SerializeField] private AudioClip pursuitLoop;

        private NavMeshAgent agent;
        private Animator animator;
        private Transform targetPlayer;
        private Animator targetAnimator;

        [SyncVar(hook = nameof(OnStateChanged))]
        private EFraudState currentState = EFraudState.Idle;

        private float stateTimer;
        private float lastAttackTime;
        private Vector3[] recordedPositions;
        private Quaternion[] recordedRotations;
        private int recordIndex;
        private int playbackIndex;
        private const int BUFFER_SIZE = 30;

        public EFraudState CurrentState => currentState;

        public override void OnStartServer()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
                agent = gameObject.AddComponent<NavMeshAgent>();

            agent.speed = pursuitSpeed;
            agent.acceleration = pursuitAcceleration;
            agent.angularSpeed = 360f;

            animator = GetComponent<Animator>();

            recordedPositions = new Vector3[BUFFER_SIZE];
            recordedRotations = new Quaternion[BUFFER_SIZE];
        }

        [Server]
        private void Update()
        {
            if (!isServer) return;

            switch (currentState)
            {
                case EFraudState.Idle:
                    SearchForTarget();
                    break;
                case EFraudState.Mirroring:
                    MirrorTarget();
                    break;
                case EFraudState.Waving:
                    WaveAtTarget();
                    break;
                case EFraudState.LockedOn:
                    LockOnStare();
                    break;
                case EFraudState.Pursuing:
                    PursueTarget();
                    break;
            }
        }

        [Server]
        private void SearchForTarget()
        {
            float closest = float.MaxValue;
            Transform best = null;
            Animator bestAnim = null;

            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist < closest && dist < mirrorRange)
                {
                    closest = dist;
                    best = player.transform;
                    bestAnim = player.GetComponent<Animator>();
                }
            }

            if (best != null)
            {
                targetPlayer = best;
                targetAnimator = bestAnim;
                currentState = EFraudState.Mirroring;
                agent.isStopped = true;
                recordIndex = 0;
                playbackIndex = 0;
            }
        }

        [Server]
        private void MirrorTarget()
        {
            if (targetPlayer == null)
            {
                currentState = EFraudState.Idle;
                return;
            }

            float dist = Vector3.Distance(transform.position, targetPlayer.position);

            recordedPositions[recordIndex % BUFFER_SIZE] = targetPlayer.position;
            recordedRotations[recordIndex % BUFFER_SIZE] = targetPlayer.rotation;
            recordIndex++;

            int delayedIndex = Mathf.Max(0, recordIndex - Mathf.RoundToInt(mirrorDelay / Time.deltaTime));
            delayedIndex = delayedIndex % BUFFER_SIZE;

            if (animator != null && targetAnimator != null)
            {
                CopyAnimatorParameters();
            }

            Vector3 mirrorDir = (transform.position - targetPlayer.position).normalized;
            Vector3 mirrorPos = targetPlayer.position + mirrorDir * Mathf.Min(dist, waveDistance);

            if (NavMesh.SamplePosition(mirrorPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }

            Quaternion lookAtPlayer = Quaternion.LookRotation(targetPlayer.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAtPlayer, poseMimicSpeed * Time.deltaTime);

            if (dist <= waveDistance && recordIndex > BUFFER_SIZE)
            {
                currentState = EFraudState.Waving;
                stateTimer = waveDuration;
                agent.isStopped = true;
                RpcTriggerWave();
            }
        }

        [Server]
        private void CopyAnimatorParameters()
        {
            if (targetAnimator == null || animator == null) return;

            for (int i = 0; i < targetAnimator.parameterCount; i++)
            {
                var param = targetAnimator.GetParameter(i);
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        animator.SetBool(param.nameHash, targetAnimator.GetBool(param.nameHash));
                        break;
                    case AnimatorControllerParameterType.Float:
                        float current = animator.GetFloat(param.nameHash);
                        float target = targetAnimator.GetFloat(param.nameHash);
                        animator.SetFloat(param.nameHash, Mathf.Lerp(current, target, poseMimicSpeed * Time.deltaTime));
                        break;
                    case AnimatorControllerParameterType.Int:
                        animator.SetInteger(param.nameHash, targetAnimator.GetInteger(param.nameHash));
                        break;
                }
            }
        }

        [Server]
        private void WaveAtTarget()
        {
            if (targetPlayer == null)
            {
                currentState = EFraudState.Idle;
                return;
            }

            Quaternion lookAt = Quaternion.LookRotation(targetPlayer.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAt, poseMimicSpeed * Time.deltaTime);

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                currentState = EFraudState.LockedOn;
                stateTimer = 3f;
                RpcTriggerLockOn();
            }
        }

        [Server]
        private void LockOnStare()
        {
            if (targetPlayer == null)
            {
                currentState = EFraudState.Idle;
                return;
            }

            Quaternion lookAt = Quaternion.LookRotation(targetPlayer.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAt, 10f * Time.deltaTime);

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                currentState = EFraudState.Pursuing;
                agent.isStopped = false;
                agent.speed = pursuitSpeed;

                if (fraudAudio != null && pursuitLoop != null)
                {
                    fraudAudio.clip = pursuitLoop;
                    fraudAudio.loop = true;
                    fraudAudio.Play();
                }
            }
        }

        [Server]
        private void PursueTarget()
        {
            if (targetPlayer == null)
            {
                currentState = EFraudState.Idle;
                agent.isStopped = true;
                return;
            }

            agent.SetDestination(targetPlayer.position);

            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            if (dist <= attackRange && Time.time - lastAttackTime > attackCooldown)
            {
                lastAttackTime = Time.time;
                var playerState = targetPlayer.GetComponent<MimicPlayerState>();
                if (playerState != null)
                    playerState.TakeDamage(attackDamage);
            }

            if (dist > mirrorRange * 2f)
            {
                currentState = EFraudState.Idle;
                targetPlayer = null;
                agent.isStopped = true;
            }
        }

        [ClientRpc]
        private void RpcTriggerWave()
        {
            if (animator != null)
                animator.SetTrigger("Wave");

            if (fraudAudio != null && waveSound != null)
                fraudAudio.PlayOneShot(waveSound);
        }

        [ClientRpc]
        private void RpcTriggerLockOn()
        {
            if (fraudAudio != null && lockOnSound != null)
                fraudAudio.PlayOneShot(lockOnSound);
        }

        private void OnStateChanged(EFraudState oldState, EFraudState newState)
        {
            if (newState == EFraudState.Pursuing && fraudAudio != null && pursuitLoop != null)
            {
                fraudAudio.clip = pursuitLoop;
                fraudAudio.loop = true;
                fraudAudio.Play();
            }
            else if (newState != EFraudState.Pursuing && fraudAudio != null)
            {
                fraudAudio.Stop();
            }
        }
    }
}
