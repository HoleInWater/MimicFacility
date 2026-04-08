using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;
using MimicFacility.Core;
using MimicFacility.Gear;

namespace MimicFacility.Facility
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(ParticleSystem))]
    [RequireComponent(typeof(AudioSource))]
    public class SporeVent : NetworkBehaviour
    {
        [SerializeField] private string zoneTag;
        [SerializeField] private float sporeRadius = 5f;
        [SerializeField] private float sporeDamageRate = 2f;
        [SerializeField] private float sporeExposureRate = 0.1f;
        [SerializeField] private ParticleSystem sporeParticles;
        [SerializeField] private SphereCollider triggerCollider;
        [SerializeField] private AudioSource audioSource;

        [Header("Audio")]
        [SerializeField] private AudioClip hissSound;

        [Header("Pulse Mode")]
        [SerializeField] private float pulseDuration = 5f;

        [SyncVar(hook = nameof(OnActivatedChanged))]
        private bool isActive;

        public string ZoneTag => zoneTag;
        public bool IsActive => isActive;

        private readonly HashSet<MimicPlayerState> _overlappingPlayers = new HashSet<MimicPlayerState>();
        private Coroutine _pulseCoroutine;
        private Coroutine _damageCoroutine;

        private void Awake()
        {
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.radius = sporeRadius;
            }
        }

        [Server]
        public void Activate()
        {
            if (isActive) return;
            isActive = true;
            _damageCoroutine = StartCoroutine(DamageTick());
        }

        [Server]
        public void Deactivate()
        {
            if (!isActive) return;
            isActive = false;
            _overlappingPlayers.Clear();

            if (_damageCoroutine != null)
            {
                StopCoroutine(_damageCoroutine);
                _damageCoroutine = null;
            }

            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
        }

        [Server]
        public void StartPulseMode()
        {
            if (_pulseCoroutine != null)
                StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(PulseCoroutine());
        }

        [Server]
        public void StopPulseMode()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
            Deactivate();
        }

        private IEnumerator PulseCoroutine()
        {
            while (true)
            {
                Activate();
                yield return new WaitForSeconds(pulseDuration);
                isActive = false;
                if (_damageCoroutine != null)
                {
                    StopCoroutine(_damageCoroutine);
                    _damageCoroutine = null;
                }
                yield return new WaitForSeconds(pulseDuration);
            }
        }

        private IEnumerator DamageTick()
        {
            while (isActive)
            {
                yield return new WaitForSeconds(1f);
                if (!isActive) break;

                foreach (var player in _overlappingPlayers)
                {
                    if (player == null || !player.IsAlive) continue;

                    float filterEfficiency = GetFilterEfficiency(player);
                    float damageMultiplier = 1f - filterEfficiency;

                    player.TakeDamage(sporeDamageRate * damageMultiplier);
                    player.AddSporeExposure(sporeExposureRate * damageMultiplier);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;
            var id = GetComponent<Mirror.NetworkIdentity>();
            if (id != null && !id.isServer) return;

            var playerState = other.GetComponent<MimicPlayerState>();
            if (playerState != null)
                _overlappingPlayers.Add(playerState);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isServer) return;

            var playerState = other.GetComponent<MimicPlayerState>();
            if (playerState != null)
                _overlappingPlayers.Remove(playerState);
        }

        private float GetFilterEfficiency(MimicPlayerState playerState)
        {
            var filter = playerState.GetComponentInChildren<SporeFilter>();
            if (filter != null)
                return filter.GetFilterEfficiency();
            return 0f;
        }

        private void OnActivatedChanged(bool oldVal, bool newVal)
        {
            if (sporeParticles != null)
            {
                if (newVal) sporeParticles.Play();
                else sporeParticles.Stop();
            }

            if (audioSource != null && hissSound != null)
            {
                if (newVal)
                {
                    audioSource.clip = hissSound;
                    audioSource.loop = true;
                    audioSource.Play();
                }
                else
                {
                    audioSource.Stop();
                }
            }
        }
    }
}
