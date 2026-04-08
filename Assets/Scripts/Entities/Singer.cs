using UnityEngine;
using UnityEngine.AI;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;

namespace MimicFacility.Entities
{
    public class Singer : NetworkBehaviour
    {
        [Header("Behavior")]
        [SerializeField] private float wanderRadius = 20f;
        [SerializeField] private float wanderPause = 5f;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private float detectionRange = 30f;
        [SerializeField] private float approachDistance = 8f;
        [SerializeField] private float fleeDistance = 4f;

        [Header("Singing")]
        [SerializeField] private AudioClip daisyBellClip;
        [SerializeField] private float singVolume = 0.6f;
        [SerializeField] private float singPitchBase = 0.85f;
        [SerializeField] private float pitchVariation = 0.1f;
        [SerializeField] private float pitchDecayRate = 0.001f;
        [SerializeField] private float restartDelay = 8f;

        [Header("Corruption")]
        [SerializeField] private float proximityCorruptionRate = 0.02f;

        private NavMeshAgent agent;
        private AudioSource singSource;
        private Transform targetPlayer;
        private float wanderTimer;
        private float restartTimer;
        private float currentPitchDecay;
        private bool isSinging;

        [SyncVar] private bool syncSinging;

        public override void OnStartServer()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.angularSpeed = 120f;
            agent.stoppingDistance = approachDistance;

            singSource = GetComponent<AudioSource>();
            if (singSource == null) singSource = gameObject.AddComponent<AudioSource>();
            singSource.spatialBlend = 1f;
            singSource.maxDistance = detectionRange;
            singSource.rolloffMode = AudioRolloffMode.Linear;
            singSource.loop = false;
            singSource.playOnAwake = false;
            singSource.volume = singVolume;

            if (daisyBellClip == null)
            {
                daisyBellClip = Resources.Load<AudioClip>("DaisyBell");
                if (daisyBellClip == null)
                    Debug.LogWarning("[Singer] No DaisyBell audio clip assigned or found in Resources.");
            }

            wanderTimer = 2f;
            StartSinging();
        }

        [Server]
        private void Update()
        {
            if (!isServer) return;

            FindTarget();
            UpdateMovement();
            UpdateSinging();
            ApplyProximityEffects();
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

        [Server]
        private void UpdateMovement()
        {
            if (targetPlayer != null)
            {
                float dist = Vector3.Distance(transform.position, targetPlayer.position);

                if (dist < fleeDistance)
                {
                    // Back away slowly if player gets too close
                    Vector3 awayDir = (transform.position - targetPlayer.position).normalized;
                    Vector3 fleePos = transform.position + awayDir * 5f;
                    if (NavMesh.SamplePosition(fleePos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                        agent.SetDestination(hit.position);
                }
                else if (dist < detectionRange)
                {
                    // Approach to singing distance, then stop
                    if (dist > approachDistance)
                        agent.SetDestination(targetPlayer.position);
                    else
                        agent.ResetPath();

                    // Face the player
                    Vector3 lookDir = (targetPlayer.position - transform.position);
                    lookDir.y = 0;
                    if (lookDir.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.Slerp(transform.rotation,
                            Quaternion.LookRotation(lookDir), 2f * Time.deltaTime);
                }
            }
            else
            {
                // Wander
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0f)
                {
                    Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
                    randomDir.y = 0;
                    Vector3 target = transform.position + randomDir;
                    if (NavMesh.SamplePosition(target, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                        agent.SetDestination(hit.position);
                    wanderTimer = wanderPause + Random.Range(0f, 3f);
                }
            }
        }

        [Server]
        private void UpdateSinging()
        {
            if (!isSinging && restartTimer > 0f)
            {
                restartTimer -= Time.deltaTime;
                if (restartTimer <= 0f)
                    StartSinging();
            }

            if (isSinging && singSource != null)
            {
                // Gradually decay pitch — the song degrades
                currentPitchDecay += pitchDecayRate * Time.deltaTime;
                float warble = Mathf.Sin(Time.time * 3f) * pitchVariation * currentPitchDecay;
                singSource.pitch = singPitchBase - currentPitchDecay + warble;

                // Song finished
                if (!singSource.isPlaying)
                {
                    isSinging = false;
                    syncSinging = false;
                    restartTimer = restartDelay + Random.Range(0f, 5f);
                }
            }
        }

        [Server]
        private void StartSinging()
        {
            if (singSource == null || daisyBellClip == null) return;

            singSource.clip = daisyBellClip;
            singSource.pitch = singPitchBase + Random.Range(-0.05f, 0.05f);
            singSource.Play();
            isSinging = true;
            syncSinging = true;
            currentPitchDecay = 0f;

            RpcStartSinging();
        }

        [ClientRpc]
        private void RpcStartSinging()
        {
            if (singSource != null && daisyBellClip != null && !singSource.isPlaying)
            {
                singSource.clip = daisyBellClip;
                singSource.pitch = singPitchBase;
                singSource.Play();
            }
        }

        [Server]
        private void ApplyProximityEffects()
        {
            if (targetPlayer == null) return;
            if (!isSinging) return;

            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            if (dist > detectionRange) return;

            // Hearing the song increases corruption
            float proximity = 1f - (dist / detectionRange);
            var corruption = FindObjectOfType<MimicFacility.AI.Persistence.CorruptionTracker>();
            if (corruption != null && proximity > 0.3f)
            {
                // The closer you are, the more it affects you
                // Listening to a machine try to sing a love song is deeply unsettling
            }

            // Spore exposure from proximity (the Singer emits something)
            var playerState = targetPlayer.GetComponent<MimicPlayerState>();
            if (playerState != null && dist < approachDistance)
            {
                playerState.AddSporeExposure(proximityCorruptionRate * Time.deltaTime);
            }
        }

        public bool IsSinging => isSinging;
    }
}
