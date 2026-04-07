using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.Core;

namespace MimicFacility.Characters
{
    public class MimicCrawler : MimicBase
    {
        [Header("Crawler Settings")]
        [SerializeField] private bool startOnCeiling;
        [SerializeField] private float dropAttackRange = 5f;
        [SerializeField] private float dropAttackDamage = 40f;
        [SerializeField] private float ceilingCheckDistance = 20f;
        [SerializeField] private Transform[] ceilingWaypoints;
        [SerializeField] private float ceilingMoveSpeed = 3f;
        [SerializeField] private CapsuleCollider capsuleCollider;

        public override float DetectionRange => 15f;
        public override float MoveSpeed => 4.5f;
        public override float AttackRange => 2f;

        private bool _isOnCeiling;
        private int _currentWaypointIndex;
        private Vector3 _ceilingPosition;
        private bool _isDropping;

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (capsuleCollider != null)
            {
                capsuleCollider.radius = 0.3f;
                capsuleCollider.height = 0.8f;
            }

            if (startOnCeiling)
                MountCeiling();
        }

        [Server]
        private void MountCeiling()
        {
            if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, ceilingCheckDistance))
            {
                _ceilingPosition = hit.point - Vector3.up * 0.4f;
                transform.position = _ceilingPosition;
                transform.rotation = Quaternion.Euler(180f, transform.eulerAngles.y, 0f);

                _isOnCeiling = true;

                if (agent != null)
                    agent.enabled = false;
            }
        }

        [Server]
        private void DetachFromCeiling()
        {
            _isOnCeiling = false;
            _isDropping = true;
            transform.rotation = Quaternion.identity;
        }

        private void Update()
        {
            if (!isServer) return;

            if (_isOnCeiling && !_isDropping)
            {
                CheckForDropAttack();

                if (CurrentState == EMimicState.Patrol)
                    CeilingPatrol();
            }

            if (_isDropping)
                ProcessDrop();
        }

        [Server]
        private void CheckForDropAttack()
        {
            PlayerCharacter nearest = GetNearestPlayer();
            if (nearest == null) return;

            Vector3 horizontalDist = nearest.transform.position - transform.position;
            horizontalDist.y = 0f;

            if (horizontalDist.magnitude <= dropAttackRange &&
                nearest.transform.position.y < transform.position.y)
            {
                SetState(EMimicState.Attacking);
                DetachFromCeiling();
            }
        }

        [Server]
        private void ProcessDrop()
        {
            transform.position += Vector3.down * (MoveSpeed * 2f) * Time.deltaTime;

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 0.5f))
            {
                _isDropping = false;
                transform.position = hit.point;

                if (agent != null)
                {
                    agent.enabled = true;
                    agent.speed = MoveSpeed;
                }

                var player = hit.collider.GetComponent<PlayerCharacter>();
                if (player != null)
                {
                    var state = player.GetComponent<MimicPlayerState>();
                    if (state != null)
                        state.TakeDamage(dropAttackDamage);
                }
            }
        }

        [Server]
        private void CeilingPatrol()
        {
            if (ceilingWaypoints == null || ceilingWaypoints.Length == 0) return;

            Transform target = ceilingWaypoints[_currentWaypointIndex];
            if (target == null) return;

            Vector3 targetPos = target.position;
            if (Physics.Raycast(targetPos, Vector3.up, out RaycastHit hit, ceilingCheckDistance))
                targetPos = hit.point - Vector3.up * 0.4f;

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                ceilingMoveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetPos) < 0.3f)
                _currentWaypointIndex = (_currentWaypointIndex + 1) % ceilingWaypoints.Length;
        }

        protected override void OnStateChanged(EMimicState oldState, EMimicState newState)
        {
            base.OnStateChanged(oldState, newState);

            switch (newState)
            {
                case EMimicState.Stalking:
                    if (!_isOnCeiling)
                        MountCeiling();
                    break;
                case EMimicState.Attacking:
                    if (_isOnCeiling)
                        DetachFromCeiling();
                    break;
                case EMimicState.Patrol:
                    _currentWaypointIndex = 0;
                    break;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isServer || !_isDropping) return;

            var player = collision.collider.GetComponent<PlayerCharacter>();
            if (player != null)
            {
                var state = player.GetComponent<MimicPlayerState>();
                if (state != null)
                    state.TakeDamage(dropAttackDamage);
            }

            _isDropping = false;
        }
    }
}
