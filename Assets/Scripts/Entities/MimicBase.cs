using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;

namespace MimicFacility.Entities
{
    public enum EMimicState
    {
        Idle,
        Patrol,
        Stalking,
        Impersonating,
        Attacking,
        Fleeing,
        Reproducing
    }

    [Serializable]
    public class VoiceProfile
    {
        public string playerId;
        public List<string> capturedPhrases = new List<string>();
        public float captureTimestamp;

        public bool HasData => capturedPhrases.Count > 0;
    }

    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class MimicBase : NetworkBehaviour
    {
        [Header("Components")]
        [SerializeField] protected NavMeshAgent agent;
        [SerializeField] protected AudioSource voiceAudio;
        [SerializeField] protected Collider mimicCollider;

        [Header("Combat")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float attackDamage = 25f;
        [SerializeField] protected float attackCooldown = 1.5f;

        [SyncVar(hook = nameof(OnStateChanged_Hook))]
        private EMimicState currentState = EMimicState.Idle;
        public EMimicState CurrentState => currentState;

        [SyncVar] private uint targetPlayerId;
        public uint TargetPlayerId => targetPlayerId;

        [SyncVar] private bool isIdentified;
        public bool IsIdentified => isIdentified;

        [SyncVar] private float health;

        protected VoiceProfile voiceProfile = new VoiceProfile();
        protected float lastAttackTime;

        public virtual float DetectionRange => 20f;
        public virtual float MoveSpeed => 3.5f;
        public virtual float AttackRange => 2f;

        public event Action<EMimicState, EMimicState> OnMimicStateChanged;

        public override void OnStartServer()
        {
            health = maxHealth;

            // Register with the hive mind — all mimics are one organism
            if (MimicHiveMind.Instance != null)
                MimicHiveMind.Instance.RegisterMimic(this);

            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = MoveSpeed;
                agent.angularSpeed = 360f;
                agent.acceleration = 8f;
                agent.stoppingDistance = 0.5f;
            }

            if (voiceAudio == null) voiceAudio = GetComponent<AudioSource>();
            if (voiceAudio != null)
            {
                voiceAudio.spatialBlend = 1f;
                voiceAudio.maxDistance = 25f;
                voiceAudio.rolloffMode = AudioRolloffMode.Linear;
                voiceAudio.playOnAwake = false;
            }

            if (mimicCollider == null) mimicCollider = GetComponent<CapsuleCollider>();

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        [Server]
        public void SetState(EMimicState newState)
        {
            if (currentState == newState) return;
            EMimicState oldState = currentState;
            currentState = newState;
            OnStateChanged(oldState, newState);
        }

        [Server]
        public void SetTarget(uint playerId)
        {
            targetPlayerId = playerId;
        }

        protected virtual void OnStateChanged(EMimicState oldState, EMimicState newState)
        {
            OnMimicStateChanged?.Invoke(oldState, newState);
        }

        private void OnStateChanged_Hook(EMimicState oldVal, EMimicState newVal)
        {
            OnMimicStateChanged?.Invoke(oldVal, newVal);
        }

        [Server]
        public virtual void TakeDamage(float amount)
        {
            if (health <= 0f) return;
            health -= amount;
            if (health <= 0f)
            {
                health = 0f;
                Die();
            }
        }

        [Server]
        public virtual void Die()
        {
            RpcOnDeath();
            NetworkServer.Destroy(gameObject);
        }

        [Server]
        public virtual void Contain()
        {
            var gameState = GameManager.Instance?.GameState;
            if (gameState != null)
                gameState.IncrementContained();

            RpcOnContained();
            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        private void RpcOnDeath() { }

        [ClientRpc]
        private void RpcOnContained() { }

        [Server]
        public void MarkIdentified()
        {
            isIdentified = true;
            RpcOnIdentified();
        }

        [ClientRpc]
        private void RpcOnIdentified()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r.material != null)
                    r.material.SetColor("_EmissionColor", Color.red * 0.5f);
            }
        }

        public PlayerCharacter GetNearestPlayer()
        {
            PlayerCharacter nearest = null;
            float minDist = DetectionRange;

            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                var state = player.GetComponent<MimicPlayerState>();
                if (state != null && !state.IsAlive) continue;

                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = player;
                }
            }

            return nearest;
        }

        public bool IsPlayerVisible(PlayerCharacter player)
        {
            if (player == null) return false;

            Vector3 direction = player.transform.position - transform.position;
            float distance = direction.magnitude;

            if (distance > DetectionRange) return false;

            if (Physics.Raycast(transform.position + Vector3.up, direction.normalized, out RaycastHit hit, distance))
            {
                return hit.collider.GetComponent<PlayerCharacter>() != null;
            }

            return false;
        }

        public PlayerCharacter GetTargetPlayer()
        {
            if (targetPlayerId == 0) return null;
            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                if (player.netId == targetPlayerId)
                    return player;
            }
            return null;
        }

        public float GetHealthPercent() => maxHealth > 0f ? health / maxHealth : 0f;

        public int GetNearbyPlayerCount(float radius)
        {
            int count = 0;
            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                var state = player.GetComponent<MimicPlayerState>();
                if (state != null && !state.IsAlive) continue;

                if (Vector3.Distance(transform.position, player.transform.position) <= radius)
                    count++;
            }
            return count;
        }

        private void OnTriggerEnter(Collider other)
        {
            var id = GetComponent<Mirror.NetworkIdentity>();
            if (id != null && !id.isServer) return;

            var player = other.GetComponent<PlayerCharacter>();
            if (player == null) return;

            var controller = GetComponent<MimicAIController>();
            if (controller != null)
                controller.OnPlayerDetected(player);
        }

        public void SetVoiceProfile(VoiceProfile profile)
        {
            voiceProfile = profile;
        }

        public VoiceProfile GetVoiceProfile() => voiceProfile;
    }
}
