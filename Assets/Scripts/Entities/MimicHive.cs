using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.AI.Voice;
using MimicFacility.Core;

namespace MimicFacility.Entities
{
    public class MimicHive : MimicBase
    {
        [Header("Hive Settings")]
        [SerializeField] private SphereCollider denialZone;
        [SerializeField] private MeshRenderer hiveMass;
        [SerializeField] private float growthRate = 0.15f;
        [SerializeField] private float maxRadius = 5f;
        [SerializeField] private float sporeDamagePerSecond = 5f;
        [SerializeField] private float sporeExposurePerSecond = 8f;
        [SerializeField] private float multiVoiceInterval = 12f;
        [SerializeField] private VoiceCloneClient voiceCloneClient;

        public override float MoveSpeed => 0f;
        public override float DetectionRange => 20f;
        public override float AttackRange => 0f;

        private readonly Dictionary<string, VoiceProfile> absorbedVoices = new Dictionary<string, VoiceProfile>();
        private float _currentRadius;
        private float _activeGrowthMultiplier = 1f;
        private Vector3 _growthDirection = Vector3.zero;
        private Coroutine _voicePlaybackLoop;

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (agent != null)
                agent.enabled = false;

            if (denialZone != null)
            {
                denialZone.isTrigger = true;
                denialZone.radius = 0.5f;
                _currentRadius = denialZone.radius;
            }

            _voicePlaybackLoop = StartCoroutine(MultiVoicePlaybackLoop());
        }

        private void Update()
        {
            if (!isServer) return;

            ExpandDenialZone();
            UpdateGrowthDirection();
            UpdateVisuals();
        }

        [Server]
        private void ExpandDenialZone()
        {
            if (denialZone == null) return;

            float rate = growthRate * _activeGrowthMultiplier * Time.deltaTime;
            _currentRadius = Mathf.Min(_currentRadius + rate, maxRadius);

            Vector3 center = denialZone.center;
            if (_growthDirection.sqrMagnitude > 0.01f)
                center += _growthDirection.normalized * rate * 0.3f;

            denialZone.center = center;
            denialZone.radius = _currentRadius;
        }

        [Server]
        private void UpdateGrowthDirection()
        {
            var players = FindObjectsOfType<PlayerCharacter>();
            if (players.Length == 0)
            {
                _growthDirection = Vector3.zero;
                return;
            }

            Vector3 clusterCenter = Vector3.zero;
            int count = 0;

            foreach (var player in players)
            {
                var state = player.GetComponent<MimicPlayerState>();
                if (state != null && !state.IsAlive) continue;

                clusterCenter += player.transform.position;
                count++;
            }

            if (count > 0)
                _growthDirection = (clusterCenter / count) - transform.position;
            else
                _growthDirection = Vector3.zero;
        }

        private void UpdateVisuals()
        {
            if (hiveMass != null)
            {
                float scale = 0.5f + (_currentRadius / maxRadius) * 2f;
                hiveMass.transform.localScale = Vector3.one * scale;
            }
        }

        public void AbsorbVoiceProfile(string playerId, VoiceProfile profile)
        {
            if (string.IsNullOrEmpty(playerId) || profile == null) return;

            if (absorbedVoices.ContainsKey(playerId))
            {
                var existing = absorbedVoices[playerId];
                existing.capturedPhrases.AddRange(profile.capturedPhrases);
            }
            else
            {
                absorbedVoices[playerId] = profile;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!isServer) return;

            var player = other.GetComponent<PlayerCharacter>();
            if (player == null) return;

            var state = player.GetComponent<MimicPlayerState>();
            if (state == null || !state.IsAlive) return;

            state.TakeDamage(sporeDamagePerSecond * Time.deltaTime);
            state.AddSporeExposure(sporeExposurePerSecond * Time.deltaTime);
        }

        protected override void OnStateChanged(EMimicState oldState, EMimicState newState)
        {
            base.OnStateChanged(oldState, newState);

            switch (newState)
            {
                case EMimicState.Idle:
                    _activeGrowthMultiplier = 1f;
                    break;
                case EMimicState.Attacking:
                    _activeGrowthMultiplier = 2f;
                    break;
                default:
                    _activeGrowthMultiplier = 1f;
                    break;
            }
        }

        private IEnumerator MultiVoicePlaybackLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(multiVoiceInterval);

                if (absorbedVoices.Count == 0) continue;

                int playCount = Mathf.Min(absorbedVoices.Count, 3);
                int played = 0;

                foreach (var kvp in absorbedVoices)
                {
                    if (played >= playCount) break;

                    VoiceProfile profile = kvp.Value;
                    if (!profile.HasData) continue;

                    string phrase = profile.capturedPhrases[
                        UnityEngine.Random.Range(0, profile.capturedPhrases.Count)];

                    PlayVoice(kvp.Key, phrase);
                    played++;

                    yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 1f));
                }
            }
        }

        [Server]
        private void PlayVoice(string speakerId, string text)
        {
            if (voiceCloneClient != null)
            {
                var request = new VoiceCloneRequest
                {
                    text = text,
                    speakerReferenceId = speakerId,
                    temperature = 0.9f,
                    exaggerationFactor = 1.3f
                };
                voiceCloneClient.SendCloneRequest(request, _ => { });
            }

            RpcPlayHiveVoice(text);
        }

        [ClientRpc]
        private void RpcPlayHiveVoice(string text)
        {
            if (voiceAudio != null && voiceAudio.clip != null)
                voiceAudio.Play();
        }

        private void OnDestroy()
        {
            if (_voicePlaybackLoop != null)
                StopCoroutine(_voicePlaybackLoop);
        }
    }
}
