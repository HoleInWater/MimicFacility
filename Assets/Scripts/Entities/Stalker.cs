using UnityEngine;
using UnityEngine.AI;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;

namespace MimicFacility.Entities
{
    public class Stalker : NetworkBehaviour
    {
        [Header("Stalker Settings")]
        [SerializeField] private float followDistance = 12f;
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float freezeCheckInterval = 0.2f;
        [SerializeField] private float viewAngleThreshold = 0.4f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 25f;
        [SerializeField] private float attackCooldown = 3f;
        [SerializeField] private float darknessSpeedBonus = 1.5f;

        [Header("Visual")]
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private Material frozenMaterial;
        [SerializeField] private Material activeMaterial;

        [Header("Audio")]
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioClip breathingClip;
        [SerializeField] private AudioClip freezeClip;
        [SerializeField] private AudioClip attackClip;

        private NavMeshAgent agent;
        private Transform targetPlayer;
        private bool isFrozen;
        private float lastAttackTime;
        private float freezeCheckTimer;

        [SyncVar(hook = nameof(OnFrozenChanged))]
        private bool syncFrozen;

        public override void OnStartServer()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
            }
            agent.speed = moveSpeed;
            agent.stoppingDistance = minDistance;
            agent.angularSpeed = 360f;
        }

        [Server]
        private void Update()
        {
            if (!isServer) return;

            freezeCheckTimer -= Time.deltaTime;
            if (freezeCheckTimer <= 0f)
            {
                freezeCheckTimer = freezeCheckInterval;
                UpdateFreezeState();
            }

            if (targetPlayer == null)
            {
                FindNearestPlayer();
                return;
            }

            float distToTarget = Vector3.Distance(transform.position, targetPlayer.position);

            if (isFrozen)
            {
                agent.isStopped = true;
                return;
            }

            agent.isStopped = false;

            bool inDarkness = !IsInLitArea();
            agent.speed = inDarkness ? moveSpeed + darknessSpeedBonus : moveSpeed;

            if (distToTarget > minDistance)
            {
                Vector3 dirToPlayer = (targetPlayer.position - transform.position).normalized;
                Vector3 followPos = targetPlayer.position - dirToPlayer * followDistance * 0.5f;
                agent.SetDestination(followPos);
            }

            if (distToTarget <= attackRange && Time.time - lastAttackTime > attackCooldown)
            {
                Attack();
            }
        }

        [Server]
        private void UpdateFreezeState()
        {
            bool anyoneWatching = false;

            foreach (var player in FindObjectsOfType<PlayerMovement>())
            {
                Camera cam = player.playerCamera;
                if (cam == null) continue;

                Vector3 dirToStalker = (transform.position - cam.transform.position).normalized;
                float dot = Vector3.Dot(cam.transform.forward, dirToStalker);

                if (dot > viewAngleThreshold)
                {
                    float dist = Vector3.Distance(cam.transform.position, transform.position);
                    if (!Physics.Raycast(cam.transform.position, dirToStalker, dist, ~0, QueryTriggerInteraction.Ignore))
                    {
                        anyoneWatching = true;
                        break;
                    }
                }
            }

            if (anyoneWatching != isFrozen)
            {
                isFrozen = anyoneWatching;
                syncFrozen = isFrozen;
            }
        }

        [Server]
        private void Attack()
        {
            lastAttackTime = Time.time;

            var playerState = targetPlayer.GetComponent<MimicPlayerState>();
            if (playerState != null)
            {
                playerState.TakeDamage(attackDamage);
            }

            RpcPlayAttackEffect();
        }

        [ClientRpc]
        private void RpcPlayAttackEffect()
        {
            if (ambientSource != null && attackClip != null)
                ambientSource.PlayOneShot(attackClip);
        }

        private void OnFrozenChanged(bool oldVal, bool newVal)
        {
            if (bodyRenderers == null) return;

            Material mat = newVal ? frozenMaterial : activeMaterial;
            if (mat == null) return;

            foreach (var r in bodyRenderers)
            {
                if (r != null) r.material = mat;
            }

            if (newVal && ambientSource != null && freezeClip != null)
                ambientSource.PlayOneShot(freezeClip);
        }

        [Server]
        private void FindNearestPlayer()
        {
            float closest = float.MaxValue;
            Transform best = null;

            foreach (var player in FindObjectsOfType<PlayerMovement>())
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist < closest)
                {
                    closest = dist;
                    best = player.transform;
                }
            }

            targetPlayer = best;
        }

        private bool IsInLitArea()
        {
            foreach (var light in FindObjectsOfType<Light>())
            {
                if (!light.enabled) continue;
                float dist = Vector3.Distance(transform.position, light.transform.position);
                if (dist < light.range)
                    return true;
            }
            return false;
        }

        public bool IsFrozen => isFrozen;
    }
}
