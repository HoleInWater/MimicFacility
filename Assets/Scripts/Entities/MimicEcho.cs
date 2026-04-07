using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.AI.Voice;
using MimicFacility.Core;

namespace MimicFacility.Entities
{
    [Serializable]
    public class EchoPhrase
    {
        public string text;
        public string originalSpeakerId;
        public float captureTimestamp;
    }

    public class MimicEcho : MimicBase
    {
        [Header("Echo Settings")]
        [SerializeField] private float playbackRadius = 15f;
        [SerializeField] private float basePlaybackInterval = 8f;
        [SerializeField] private float driftSpeed = 0.5f;
        [SerializeField] private VoiceCloneClient voiceCloneClient;

        public override float DetectionRange => 25f;
        public override float MoveSpeed => 2f;
        public override float AttackRange => 0f;

        private readonly List<EchoPhrase> storedPhrases = new List<EchoPhrase>();
        private Coroutine _echoLoop;
        private Vector3 _driftTarget;
        private float _driftTimer;

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (agent != null)
                agent.enabled = false;

            SetInvisible();
            _echoLoop = StartCoroutine(EchoLoop());
        }

        private void SetInvisible()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                r.enabled = false;
        }

        public void AbsorbPhrase(string playerId, string text)
        {
            storedPhrases.Add(new EchoPhrase
            {
                text = text,
                originalSpeakerId = playerId,
                captureTimestamp = Time.time
            });
        }

        private void Update()
        {
            if (!isServer) return;
            DriftTowardPlayers();
        }

        [Server]
        private void DriftTowardPlayers()
        {
            _driftTimer -= Time.deltaTime;

            if (_driftTimer <= 0f)
            {
                _driftTimer = 5f;
                _driftTarget = FindPlayerClusterCenter();
            }

            if (_driftTarget != Vector3.zero)
            {
                Vector3 direction = (_driftTarget - transform.position).normalized;
                transform.position += direction * driftSpeed * Time.deltaTime;
            }
        }

        private Vector3 FindPlayerClusterCenter()
        {
            var players = FindObjectsOfType<PlayerCharacter>();
            if (players.Length == 0) return transform.position;

            Vector3 center = Vector3.zero;
            int count = 0;

            foreach (var player in players)
            {
                var state = player.GetComponent<MimicPlayerState>();
                if (state != null && !state.IsAlive) continue;

                center += player.transform.position;
                count++;
            }

            return count > 0 ? center / count : transform.position;
        }

        private IEnumerator EchoLoop()
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 6f));

            while (true)
            {
                float interval = basePlaybackInterval + UnityEngine.Random.Range(-2f, 4f);
                yield return new WaitForSeconds(interval);

                if (storedPhrases.Count == 0) continue;

                var nearbyPlayers = GetPlayersInRange(playbackRadius);
                if (nearbyPlayers.Count == 0) continue;

                EchoPhrase phrase = SelectPhrase(nearbyPlayers);
                if (phrase != null)
                    PlayPhrase(phrase);
            }
        }

        private List<PlayerCharacter> GetPlayersInRange(float radius)
        {
            var result = new List<PlayerCharacter>();
            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                var state = player.GetComponent<MimicPlayerState>();
                if (state != null && !state.IsAlive) continue;

                if (Vector3.Distance(transform.position, player.transform.position) <= radius)
                    result.Add(player);
            }
            return result;
        }

        private EchoPhrase SelectPhrase(List<PlayerCharacter> nearbyPlayers)
        {
            PlayerCharacter closest = null;
            float closestDist = float.MaxValue;

            foreach (var player in nearbyPlayers)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = player;
                }
            }

            if (closest != null)
            {
                string targetId = closest.netId.ToString();
                var matching = storedPhrases.FindAll(p => p.originalSpeakerId == targetId);
                if (matching.Count > 0)
                    return matching[UnityEngine.Random.Range(0, matching.Count)];
            }

            return storedPhrases[UnityEngine.Random.Range(0, storedPhrases.Count)];
        }

        [Server]
        private void PlayPhrase(EchoPhrase phrase)
        {
            if (voiceCloneClient != null)
            {
                var request = new VoiceCloneRequest
                {
                    text = phrase.text,
                    speakerReferenceId = phrase.originalSpeakerId,
                    temperature = 0.8f
                };

                voiceCloneClient.SendCloneRequest(request, response =>
                {
                    if (response.success)
                    {
                        AudioClip clip = voiceCloneClient.CreateAudioClip(response);
                        if (clip != null)
                            RpcPlayAudio(phrase.text);
                    }
                });
            }
            else
            {
                RpcPlayAudio(phrase.text);
            }
        }

        [ClientRpc]
        private void RpcPlayAudio(string phraseText)
        {
            if (voiceAudio != null && voiceAudio.clip != null)
                voiceAudio.Play();
        }

        private void OnDestroy()
        {
            if (_echoLoop != null)
                StopCoroutine(_echoLoop);
        }
    }
}
