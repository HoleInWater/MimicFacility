using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using MimicFacility.Core;

namespace MimicFacility.Characters
{
    public class MimicSwarm : MimicBase
    {
        [Header("Swarm Settings")]
        [SerializeField] private float flockingRadius = 8f;
        [SerializeField] private float separationWeight = 1.5f;
        [SerializeField] private float alignmentWeight = 1f;
        [SerializeField] private float cohesionWeight = 1f;
        [SerializeField] private float separationDistance = 2f;
        [SerializeField] private int swarmAttackThreshold = 3;
        [SerializeField] private LayerMask swarmLayer;

        public override float MoveSpeed => 6f;
        public override float DetectionRange => 12f;
        public override float AttackRange => 1.5f;

        private Vector3 _flockVelocity;
        private static readonly Collider[] OverlapBuffer = new Collider[32];

        public override void OnStartServer()
        {
            base.OnStartServer();
            transform.localScale = Vector3.one * 0.4f;
        }

        private void Update()
        {
            if (!isServer) return;

            switch (CurrentState)
            {
                case EMimicState.Patrol:
                    ApplyFlocking();
                    break;
                case EMimicState.Attacking:
                    RushTarget();
                    break;
                case EMimicState.Fleeing:
                    Scatter();
                    break;
            }

            CheckSwarmAttack();
        }

        [Server]
        private void ApplyFlocking()
        {
            Vector3 flockDir = ComputeFlockingDirection();
            if (flockDir.sqrMagnitude > 0.01f && agent != null && agent.enabled)
            {
                Vector3 target = transform.position + flockDir.normalized * 3f;
                if (NavMesh.SamplePosition(target, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }

        private Vector3 ComputeFlockingDirection()
        {
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            int neighborCount = 0;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position, flockingRadius, OverlapBuffer, swarmLayer);

            for (int i = 0; i < count; i++)
            {
                var other = OverlapBuffer[i].GetComponent<MimicSwarm>();
                if (other == null || other == this) continue;

                Vector3 toOther = other.transform.position - transform.position;
                float dist = toOther.magnitude;

                if (dist < separationDistance && dist > 0.01f)
                    separation -= toOther / dist;

                if (other.agent != null && other.agent.enabled)
                    alignment += other.agent.velocity;

                cohesion += other.transform.position;
                neighborCount++;
            }

            if (neighborCount == 0) return Vector3.zero;

            alignment /= neighborCount;
            cohesion = (cohesion / neighborCount) - transform.position;

            return separation * separationWeight
                + alignment.normalized * alignmentWeight
                + cohesion.normalized * cohesionWeight;
        }

        [Server]
        private void RushTarget()
        {
            PlayerCharacter target = GetTargetPlayer();
            if (target == null || agent == null || !agent.enabled) return;

            agent.speed = MoveSpeed;
            agent.SetDestination(target.transform.position);

            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= AttackRange)
            {
                var state = target.GetComponent<MimicPlayerState>();
                if (state != null && Time.time - lastAttackTime >= 0.5f)
                {
                    state.TakeDamage(attackDamage);
                    lastAttackTime = Time.time;
                }
            }
        }

        [Server]
        private void Scatter()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, flockingRadius, OverlapBuffer, swarmLayer);

            Vector3 awayDir = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                var other = OverlapBuffer[i].GetComponent<MimicSwarm>();
                if (other == null || other == this) continue;
                awayDir += (transform.position - other.transform.position).normalized;
            }

            if (awayDir.sqrMagnitude < 0.01f)
                awayDir = Random.insideUnitSphere;

            awayDir.y = 0f;

            if (agent != null && agent.enabled)
            {
                Vector3 target = transform.position + awayDir.normalized * 8f;
                if (NavMesh.SamplePosition(target, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }

        [Server]
        private void CheckSwarmAttack()
        {
            if (CurrentState == EMimicState.Attacking) return;

            PlayerCharacter nearest = GetNearestPlayer();
            if (nearest == null) return;

            int nearbySwarm = 0;
            int count = Physics.OverlapSphereNonAlloc(
                nearest.transform.position, flockingRadius, OverlapBuffer, swarmLayer);

            for (int i = 0; i < count; i++)
            {
                if (OverlapBuffer[i].GetComponent<MimicSwarm>() != null)
                    nearbySwarm++;
            }

            if (nearbySwarm >= swarmAttackThreshold)
            {
                SetTarget(nearest.netId);
                SetState(EMimicState.Attacking);
            }
        }

        protected override void OnStateChanged(EMimicState oldState, EMimicState newState)
        {
            base.OnStateChanged(oldState, newState);

            if (agent != null)
                agent.speed = newState == EMimicState.Attacking ? MoveSpeed : MoveSpeed * 0.7f;
        }

        [Server]
        public static void SpawnCluster(GameObject prefab, int count, Vector3 center)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 3f;
                offset.y = 0f;
                Vector3 spawnPos = center + offset;

                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    spawnPos = hit.position;

                GameObject instance = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
                NetworkServer.Spawn(instance);
            }
        }
    }
}
