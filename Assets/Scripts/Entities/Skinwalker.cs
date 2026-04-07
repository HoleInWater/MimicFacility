using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;
using MimicFacility.AI.Voice;

namespace MimicFacility.Entities
{
    public class Skinwalker : NetworkBehaviour
    {
        [Header("Transformation")]
        [SerializeField] private float transformDuration = 3f;
        [SerializeField] private float deathProximityRange = 5f;

        [Header("Behavior")]
        [SerializeField] private float patrolSpeed = 3f;
        [SerializeField] private float mimicAccuracy = 0.9f;
        [SerializeField] private float voiceReplayInterval = 12f;
        [SerializeField] private float behaviorMimicRadius = 2f;

        [Header("Detection")]
        [SerializeField] private float scannerIntegrity = 0.55f;

        [Header("Audio")]
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioClip transformSound;

        [SyncVar(hook = nameof(OnTransformedChanged))]
        private bool isTransformed;

        [SyncVar] private string assumedPlayerName;
        [SyncVar] private int assumedSubjectNumber;

        private NavMeshAgent agent;
        private List<string> stolenPhrases = new List<string>();
        private List<Vector3> recordedPath = new List<Vector3>();
        private int pathPlaybackIndex;
        private float nextVoiceTime;
        private float transformTimer;
        private bool isTransforming;
        private SkinnedMeshRenderer[] meshRenderers;

        public string AssumedName => assumedPlayerName;
        public float ScannerIntegrity => scannerIntegrity;

        public override void OnStartServer()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
                agent = gameObject.AddComponent<NavMeshAgent>();

            agent.speed = patrolSpeed;
            meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        [Server]
        private void Update()
        {
            if (!isServer) return;

            if (isTransforming)
            {
                transformTimer -= Time.deltaTime;
                if (transformTimer <= 0f)
                {
                    CompleteTransformation();
                }
                return;
            }

            if (!isTransformed)
            {
                ScanForDeadPlayers();
                return;
            }

            ReplayBehavior();

            if (Time.time >= nextVoiceTime && stolenPhrases.Count > 0)
            {
                ReplayVoice();
                nextVoiceTime = Time.time + voiceReplayInterval + Random.Range(-3f, 3f);
            }
        }

        [Server]
        private void ScanForDeadPlayers()
        {
            foreach (var playerState in FindObjectsOfType<MimicPlayerState>())
            {
                if (!playerState.IsAlive && !playerState.IsConverted)
                {
                    float dist = Vector3.Distance(transform.position, playerState.transform.position);
                    if (dist <= deathProximityRange)
                    {
                        BeginTransformation(playerState);
                        return;
                    }
                }
            }

            if (!agent.hasPath || agent.remainingDistance < 1f)
            {
                Vector3 randomPos = transform.position + Random.insideUnitSphere * 15f;
                randomPos.y = transform.position.y;
                if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }

        [Server]
        private void BeginTransformation(MimicPlayerState deadPlayer)
        {
            isTransforming = true;
            transformTimer = transformDuration;
            agent.isStopped = true;

            assumedPlayerName = deadPlayer.DisplayName;
            assumedSubjectNumber = deadPlayer.SubjectNumber;

            var voiceSystem = FindObjectOfType<VoiceLearningSystem>();
            if (voiceSystem != null)
            {
                string playerId = deadPlayer.connectionToClient?.connectionId.ToString() ?? "0";
                var phrases = voiceSystem.GetMostFrequentPhrases(playerId, 10);
                stolenPhrases.AddRange(phrases);
            }

            deadPlayer.MarkConverted();

            RpcPlayTransformEffect(transform.position);
        }

        [Server]
        private void CompleteTransformation()
        {
            isTransforming = false;
            isTransformed = true;
            agent.isStopped = false;
            agent.speed = patrolSpeed;

            nextVoiceTime = Time.time + Random.Range(5f, voiceReplayInterval);
        }

        [Server]
        private void ReplayBehavior()
        {
            if (recordedPath.Count > 0)
            {
                if (pathPlaybackIndex < recordedPath.Count)
                {
                    Vector3 target = recordedPath[pathPlaybackIndex];
                    agent.SetDestination(target);

                    if (agent.remainingDistance < behaviorMimicRadius)
                        pathPlaybackIndex++;
                }
                else
                {
                    pathPlaybackIndex = 0;
                }
            }
            else
            {
                foreach (var player in FindObjectsOfType<PlayerMovement>())
                {
                    float dist = Vector3.Distance(transform.position, player.transform.position);
                    if (dist > 8f && dist < 20f)
                    {
                        Vector3 offset = (transform.position - player.transform.position).normalized * 8f;
                        Vector3 followPos = player.transform.position + offset;
                        agent.SetDestination(followPos);
                        break;
                    }
                }
            }
        }

        [Server]
        private void ReplayVoice()
        {
            if (stolenPhrases.Count == 0) return;

            string phrase = stolenPhrases[Random.Range(0, stolenPhrases.Count)];

            var voiceCloneClient = FindObjectOfType<VoiceCloneClient>();
            if (voiceCloneClient != null)
            {
                var request = new VoiceCloneRequest
                {
                    text = phrase,
                    speakerReferenceId = assumedPlayerName,
                    temperature = 0.8f,
                    exaggerationFactor = mimicAccuracy
                };

                voiceCloneClient.SendCloneRequest(request, response =>
                {
                    if (response.success)
                    {
                        AudioClip clip = voiceCloneClient.CreateAudioClip(response);
                        RpcPlayVoice(phrase);
                    }
                });
            }
            else
            {
                RpcPlayVoice(phrase);
            }
        }

        [ClientRpc]
        private void RpcPlayVoice(string phrase)
        {
            if (voiceSource != null)
            {
                Debug.Log($"[Skinwalker] Speaking as {assumedPlayerName}: \"{phrase}\"");
            }
        }

        [ClientRpc]
        private void RpcPlayTransformEffect(Vector3 position)
        {
            if (voiceSource != null && transformSound != null)
                AudioSource.PlayClipAtPoint(transformSound, position);
        }

        private void OnTransformedChanged(bool oldVal, bool newVal)
        {
            if (newVal)
            {
                Debug.Log($"[Skinwalker] Now impersonating {assumedPlayerName}");
            }
        }

        [Server]
        public void RecordPlayerPath(Vector3 position)
        {
            recordedPath.Add(position);
            if (recordedPath.Count > 100)
                recordedPath.RemoveAt(0);
        }
    }
}
