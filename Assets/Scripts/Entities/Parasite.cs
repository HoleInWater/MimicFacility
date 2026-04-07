using System.Collections;
using UnityEngine;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;

namespace MimicFacility.Entities
{
    public class Parasite : NetworkBehaviour
    {
        [Header("Infection Settings")]
        [SerializeField] private float attachRange = 3f;
        [SerializeField] private float infectionRate = 0.02f;
        [SerializeField] private float maxInfection = 1f;
        [SerializeField] private float detectionThreshold = 0.5f;
        [SerializeField] private float voiceDistortionStart = 0.3f;

        [Header("Movement")]
        [SerializeField] private float crawlSpeed = 1.5f;
        [SerializeField] private float seekRange = 15f;

        [Header("Audio")]
        [SerializeField] private AudioSource parasiteAudio;
        [SerializeField] private AudioClip attachSound;
        [SerializeField] private AudioClip heartbeatClip;
        [SerializeField] private float heartbeatBaseRate = 1f;

        [SyncVar] private bool isAttached;
        [SyncVar] private uint hostNetId;
        [SyncVar] private float infectionLevel;

        private Transform hostPlayer;
        private MimicPlayerState hostState;
        private float heartbeatTimer;

        public bool IsAttached => isAttached;
        public float InfectionLevel => infectionLevel;

        [Server]
        private void Update()
        {
            if (!isServer) return;

            if (isAttached)
            {
                UpdateInfection();
                FollowHost();
            }
            else
            {
                SeekHost();
            }
        }

        [Server]
        private void SeekHost()
        {
            float closest = float.MaxValue;
            Transform best = null;

            foreach (var player in FindObjectsOfType<PlayerMovement>())
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist < closest && dist < seekRange)
                {
                    bool alreadyInfected = false;
                    foreach (var otherParasite in FindObjectsOfType<Parasite>())
                    {
                        if (otherParasite != this && otherParasite.isAttached &&
                            otherParasite.hostPlayer == player.transform)
                        {
                            alreadyInfected = true;
                            break;
                        }
                    }

                    if (!alreadyInfected)
                    {
                        closest = dist;
                        best = player.transform;
                    }
                }
            }

            if (best != null)
            {
                Vector3 dir = (best.position - transform.position).normalized;
                transform.position += dir * crawlSpeed * Time.deltaTime;

                if (Vector3.Distance(transform.position, best.position) <= attachRange)
                {
                    AttachToHost(best);
                }
            }
        }

        [Server]
        private void AttachToHost(Transform player)
        {
            hostPlayer = player;
            hostState = player.GetComponent<MimicPlayerState>();

            var netId = player.GetComponent<NetworkIdentity>();
            if (netId != null)
                hostNetId = netId.netId;

            isAttached = true;
            infectionLevel = 0f;

            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.enabled = false;

            GetComponent<Collider>().enabled = false;

            RpcPlayAttachEffect();
        }

        [Server]
        private void UpdateInfection()
        {
            if (hostPlayer == null)
            {
                Detach();
                return;
            }

            infectionLevel += infectionRate * Time.deltaTime;
            infectionLevel = Mathf.Min(infectionLevel, maxInfection);

            if (infectionLevel >= voiceDistortionStart)
            {
                float distortionAmount = (infectionLevel - voiceDistortionStart) / (maxInfection - voiceDistortionStart);
                RpcApplyVoiceDistortion(hostNetId, distortionAmount);
            }

            if (infectionLevel >= detectionThreshold)
            {
                if (hostState != null)
                    hostState.AddSporeExposure(infectionRate * 0.5f * Time.deltaTime);
            }
        }

        [Server]
        private void FollowHost()
        {
            if (hostPlayer == null) return;
            transform.position = hostPlayer.position + Vector3.up * 0.8f + hostPlayer.forward * -0.3f;
        }

        [ClientRpc]
        private void RpcApplyVoiceDistortion(uint targetNetId, float amount)
        {
            if (NetworkClient.connection == null) return;
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer == null) return;

            if (localPlayer.netId == targetNetId) return;

            if (NetworkClient.spawned.TryGetValue(targetNetId, out var targetIdentity))
            {
                var audioSources = targetIdentity.GetComponentsInChildren<AudioSource>();
                foreach (var source in audioSources)
                {
                    source.pitch = Mathf.Lerp(1f, 0.7f, amount);
                }
            }
        }

        [ClientRpc]
        private void RpcPlayAttachEffect()
        {
            if (parasiteAudio != null && attachSound != null)
                parasiteAudio.PlayOneShot(attachSound);
        }

        [Server]
        public void Detach()
        {
            isAttached = false;
            hostPlayer = null;
            hostState = null;
            infectionLevel = 0f;

            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.enabled = true;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            RpcClearVoiceDistortion(hostNetId);
        }

        [ClientRpc]
        private void RpcClearVoiceDistortion(uint targetNetId)
        {
            if (NetworkClient.spawned.TryGetValue(targetNetId, out var targetIdentity))
            {
                var audioSources = targetIdentity.GetComponentsInChildren<AudioSource>();
                foreach (var source in audioSources)
                {
                    source.pitch = 1f;
                }
            }
        }

        [Server]
        public void OnScannerDetected()
        {
            if (isAttached)
                Detach();

            NetworkServer.Destroy(gameObject);
        }
    }
}
