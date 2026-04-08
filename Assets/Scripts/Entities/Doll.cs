using UnityEngine;
using Mirror;
using MimicFacility.Characters;
using MimicFacility.Core;

namespace MimicFacility.Entities
{
    /// <summary>
    /// The Doll — sits perfectly still. Stares at you.
    /// When you look away, it turns to face you.
    /// When you look back, it's closer. It never moves while watched.
    /// If it reaches you, it screams.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Doll : NetworkBehaviour
    {
        [Header("Behavior")]
        [SerializeField] private float teleportDistance = 2f;
        [SerializeField] private float minTeleportInterval = 3f;
        [SerializeField] private float maxTeleportInterval = 8f;
        [SerializeField] private float killRange = 1.5f;
        [SerializeField] private float detectionRange = 25f;
        [SerializeField] private float viewAngleThreshold = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioSource dollAudio;
        [SerializeField] private AudioClip screamClip;
        [SerializeField] private AudioClip gigglerClip;
        [SerializeField] private float ambientVolume = 0.1f;

        [Header("Visual")]
        [SerializeField] private float headTrackSpeed = 5f;

        private Transform targetPlayer;
        private float nextMoveTime;
        private bool isBeingWatched;
        private bool hasScreamed;

        [SyncVar] private Vector3 syncPosition;
        [SyncVar] private Quaternion syncRotation;

        public override void OnStartServer()
        {
            if (dollAudio == null) dollAudio = GetComponent<AudioSource>();
            dollAudio.spatialBlend = 1f;
            dollAudio.maxDistance = detectionRange;
            dollAudio.loop = false;
            dollAudio.playOnAwake = false;

            ScheduleNextMove();
        }

        [Server]
        private void Update()
        {
            if (!isServer) return;

            FindTarget();
            if (targetPlayer == null) return;

            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            if (dist > detectionRange) return;

            isBeingWatched = IsPlayerLooking();

            // Always face the player (head tracking)
            Vector3 lookDir = targetPlayer.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, headTrackSpeed * Time.deltaTime);
            }

            // Move when not watched
            if (!isBeingWatched && Time.time >= nextMoveTime)
            {
                TeleportCloser();
                ScheduleNextMove();
            }

            // Kill check
            if (dist <= killRange && !hasScreamed)
            {
                hasScreamed = true;
                RpcScream();

                var playerState = targetPlayer.GetComponent<MimicPlayerState>();
                if (playerState != null)
                    playerState.TakeDamage(50f);
            }

            syncPosition = transform.position;
            syncRotation = transform.rotation;
        }

        [Server]
        private void TeleportCloser()
        {
            if (targetPlayer == null) return;

            Vector3 dirToPlayer = (targetPlayer.position - transform.position).normalized;
            Vector3 newPos = transform.position + dirToPlayer * teleportDistance;
            newPos.y = transform.position.y;

            transform.position = newPos;

            // Random chance to giggle after teleporting
            if (Random.value < 0.3f)
                RpcGiggle();
        }

        [Server]
        private void FindTarget()
        {
            float closest = float.MaxValue;
            Transform best = null;

            foreach (var player in FindObjectsOfType<PlayerCharacter>())
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

        private bool IsPlayerLooking()
        {
            if (targetPlayer == null) return false;

            Camera cam = targetPlayer.GetComponentInChildren<Camera>();
            if (cam == null) return false;

            Vector3 dirToDoll = (transform.position - cam.transform.position).normalized;
            float dot = Vector3.Dot(cam.transform.forward, dirToDoll);

            if (dot < viewAngleThreshold) return false;

            float dist = Vector3.Distance(cam.transform.position, transform.position);
            if (Physics.Raycast(cam.transform.position, dirToDoll, dist, ~0, QueryTriggerInteraction.Ignore))
                return false;

            return true;
        }

        private void ScheduleNextMove()
        {
            nextMoveTime = Time.time + Random.Range(minTeleportInterval, maxTeleportInterval);
        }

        [ClientRpc]
        private void RpcScream()
        {
            if (dollAudio != null && screamClip != null)
            {
                dollAudio.volume = 1f;
                dollAudio.PlayOneShot(screamClip);
            }
        }

        [ClientRpc]
        private void RpcGiggle()
        {
            if (dollAudio != null && gigglerClip != null)
            {
                dollAudio.volume = ambientVolume;
                dollAudio.PlayOneShot(gigglerClip);
            }
        }

        private void LateUpdate()
        {
            // Client-side smooth position sync
            if (!isServer)
            {
                transform.position = Vector3.Lerp(transform.position, syncPosition, 10f * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, syncRotation, 10f * Time.deltaTime);
            }
        }
    }
}
