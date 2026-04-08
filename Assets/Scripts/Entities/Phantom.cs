using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using MimicFacility.Characters;

namespace MimicFacility.Entities
{
    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof(AudioSource))]
    public class Phantom : NetworkBehaviour
    {
        [Header("Sound Projection")]
        [SerializeField] private float projectionRange = 25f;
        [SerializeField] private float minSoundInterval = 8f;
        [SerializeField] private float maxSoundInterval = 20f;
        [SerializeField] private int maxSimultaneousSounds = 2;

        [Header("Sound Library")]
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private AudioClip[] doorClips;
        [SerializeField] private AudioClip[] radioStaticClips;
        [SerializeField] private AudioClip[] breathingClips;
        [SerializeField] private AudioClip[] whisperClips;

        [Header("Movement")]
        [SerializeField] private float driftSpeed = 1f;
        [SerializeField] private float avoidPlayerRadius = 10f;

        [Header("Visibility")]
        [SerializeField] private float flickerChance = 0.05f;
        [SerializeField] private float flickerDuration = 0.1f;

        private NavMeshAgent agent;
        private List<AudioSource> projectedSources = new List<AudioSource>();
        private float nextSoundTime;
        private Renderer[] renderers;

        [SyncVar] private bool isFlickering;

        public override void OnStartServer()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
                agent = gameObject.AddComponent<NavMeshAgent>();

            agent.speed = driftSpeed;
            agent.angularSpeed = 120f;

            renderers = GetComponentsInChildren<Renderer>();
            SetVisible(false);

            ScheduleNextSound();
        }

        [Server]
        private void Update()
        {
            if (!isServer) return;

            DriftAwayFromPlayers();

            if (Time.time >= nextSoundTime)
            {
                ProjectSound();
                ScheduleNextSound();
            }

            if (Random.value < flickerChance * Time.deltaTime)
            {
                StartCoroutine(Flicker());
            }
        }

        [Server]
        private void DriftAwayFromPlayers()
        {
            Vector3 avoidDir = Vector3.zero;
            int count = 0;

            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist < avoidPlayerRadius)
                {
                    avoidDir += (transform.position - player.transform.position).normalized;
                    count++;
                }
            }

            if (count > 0)
            {
                avoidDir /= count;
                Vector3 targetPos = transform.position + avoidDir * 5f;

                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
            else
            {
                if (!agent.hasPath || agent.remainingDistance < 1f)
                {
                    Vector3 randomDir = Random.insideUnitSphere * projectionRange * 0.5f;
                    randomDir.y = 0;
                    Vector3 target = transform.position + randomDir;

                    if (NavMesh.SamplePosition(target, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
            }
        }

        [Server]
        private void ProjectSound()
        {
            Transform targetPlayer = FindMostIsolatedPlayer();
            if (targetPlayer == null) return;

            Vector3 lureDirection = (transform.position - targetPlayer.position).normalized;
            Vector3 soundPosition = targetPlayer.position + lureDirection * Random.Range(5f, 15f);
            soundPosition.y = targetPlayer.position.y;

            AudioClip clip = PickRandomSound();
            if (clip == null) return;

            RpcPlayProjectedSound(soundPosition, GetSoundCategory(clip), Random.Range(0, 1000));
        }

        [ClientRpc]
        private void RpcPlayProjectedSound(Vector3 position, int category, int seed)
        {
            AudioClip clip = GetClipFromCategory(category, seed);
            if (clip == null) return;

            GameObject tempObj = new GameObject("PhantomSound");
            tempObj.transform.position = position;

            AudioSource source = tempObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1f;
            source.maxDistance = 20f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.volume = Random.Range(0.3f, 0.7f);
            source.Play();

            Destroy(tempObj, clip.length + 0.5f);
        }

        private Transform FindMostIsolatedPlayer()
        {
            Transform mostIsolated = null;
            float maxIsolation = 0f;

            var players = FindObjectsOfType<PlayerCharacter>();
            if (players.Length <= 1) return players.Length == 1 ? players[0].transform : null;

            foreach (var player in players)
            {
                float minDistToOther = float.MaxValue;
                foreach (var other in players)
                {
                    if (other == player) continue;
                    float dist = Vector3.Distance(player.transform.position, other.transform.position);
                    if (dist < minDistToOther)
                        minDistToOther = dist;
                }

                if (minDistToOther > maxIsolation)
                {
                    maxIsolation = minDistToOther;
                    mostIsolated = player.transform;
                }
            }

            return mostIsolated;
        }

        private AudioClip PickRandomSound()
        {
            List<AudioClip[]> pools = new List<AudioClip[]>();
            if (footstepClips != null && footstepClips.Length > 0) pools.Add(footstepClips);
            if (doorClips != null && doorClips.Length > 0) pools.Add(doorClips);
            if (radioStaticClips != null && radioStaticClips.Length > 0) pools.Add(radioStaticClips);
            if (breathingClips != null && breathingClips.Length > 0) pools.Add(breathingClips);
            if (whisperClips != null && whisperClips.Length > 0) pools.Add(whisperClips);

            if (pools.Count == 0) return null;

            var pool = pools[Random.Range(0, pools.Count)];
            return pool[Random.Range(0, pool.Length)];
        }

        private int GetSoundCategory(AudioClip clip)
        {
            if (System.Array.IndexOf(footstepClips ?? new AudioClip[0], clip) >= 0) return 0;
            if (System.Array.IndexOf(doorClips ?? new AudioClip[0], clip) >= 0) return 1;
            if (System.Array.IndexOf(radioStaticClips ?? new AudioClip[0], clip) >= 0) return 2;
            if (System.Array.IndexOf(breathingClips ?? new AudioClip[0], clip) >= 0) return 3;
            if (System.Array.IndexOf(whisperClips ?? new AudioClip[0], clip) >= 0) return 4;
            return 0;
        }

        private AudioClip GetClipFromCategory(int category, int seed)
        {
            AudioClip[] pool = category switch
            {
                0 => footstepClips,
                1 => doorClips,
                2 => radioStaticClips,
                3 => breathingClips,
                4 => whisperClips,
                _ => footstepClips
            };

            if (pool == null || pool.Length == 0) return null;
            return pool[seed % pool.Length];
        }

        private void ScheduleNextSound()
        {
            nextSoundTime = Time.time + Random.Range(minSoundInterval, maxSoundInterval);
        }

        private IEnumerator Flicker()
        {
            isFlickering = true;
            SetVisible(true);
            yield return new WaitForSeconds(flickerDuration);
            SetVisible(false);
            isFlickering = false;
        }

        private void SetVisible(bool visible)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = visible;
            }
        }
    }
}
