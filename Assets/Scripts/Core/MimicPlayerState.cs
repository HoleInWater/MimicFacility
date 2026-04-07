using System;
using UnityEngine;
using Mirror;

namespace MimicFacility.Core
{
    public class MimicPlayerState : NetworkBehaviour
    {
        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;
        public event Action OnConverted;
        public event Action<float> OnSporeExposureChanged;

        [SyncVar(hook = nameof(OnSubjectNumberChanged))]
        private int subjectNumber;
        public int SubjectNumber => subjectNumber;

        [SyncVar(hook = nameof(OnDisplayNameChanged))]
        private string displayName;
        public string DisplayName => displayName;

        [SyncVar(hook = nameof(OnConvertedChanged))]
        private bool isConverted;
        public bool IsConverted => isConverted;

        [SyncVar(hook = nameof(OnAliveChanged))]
        private bool isAlive = true;
        public bool IsAlive => isAlive;

        [SyncVar]
        private string currentZone = "Unknown";
        public string CurrentZone => currentZone;

        [SyncVar]
        private NetworkIdentity primaryGear;
        public NetworkIdentity PrimaryGear => primaryGear;

        [SyncVar]
        private NetworkIdentity secondaryGear;
        public NetworkIdentity SecondaryGear => secondaryGear;

        [SyncVar(hook = nameof(OnHealthValueChanged))]
        private float health = 100f;
        public float Health => health;

        [SerializeField] private float maxHealth = 100f;
        public float MaxHealth => maxHealth;

        [SyncVar]
        private float sporeExposure;
        public float SporeExposure => sporeExposure;

        [SerializeField] private float sporeConversionThreshold = 100f;

        public override void OnStartServer()
        {
            health = maxHealth;
            isAlive = true;
            isConverted = false;
            sporeExposure = 0f;
        }

        [Server]
        public void Initialize(int subject, string name)
        {
            subjectNumber = subject;
            displayName = name;
        }

        [Server]
        public void SetZone(string zone)
        {
            currentZone = zone;
        }

        [Server]
        public void TakeDamage(float amount)
        {
            if (!isAlive) return;

            health = Mathf.Max(0f, health - amount);

            if (health <= 0f)
                Die();
        }

        [Server]
        public void Heal(float amount)
        {
            if (!isAlive) return;
            health = Mathf.Min(maxHealth, health + amount);
        }

        [Server]
        public void AddSporeExposure(float amount)
        {
            if (!isAlive || isConverted) return;

            sporeExposure = Mathf.Min(sporeConversionThreshold, sporeExposure + amount);
            RpcSporeExposureChanged(sporeExposure);

            if (sporeExposure >= sporeConversionThreshold)
                Convert();
        }

        [Server]
        public void ReduceSporeExposure(float amount)
        {
            sporeExposure = Mathf.Max(0f, sporeExposure - amount);
            RpcSporeExposureChanged(sporeExposure);
        }

        [Server]
        public void Die()
        {
            if (!isAlive) return;

            isAlive = false;
            health = 0f;

            var gameState = GameManager.Instance?.GameState;
            if (gameState != null && connectionToClient != null)
                gameState.SetPlayerAlive(connectionToClient.connectionId, false);

            var playerData = GetPlayerData();
            if (playerData != null)
                playerData.isAlive = false;

            RpcOnDeath();
        }

        [Server]
        public void Revive(float healthAmount)
        {
            if (isAlive || isConverted) return;

            isAlive = true;
            health = Mathf.Clamp(healthAmount, 1f, maxHealth);

            var gameState = GameManager.Instance?.GameState;
            if (gameState != null && connectionToClient != null)
                gameState.SetPlayerAlive(connectionToClient.connectionId, true);

            var playerData = GetPlayerData();
            if (playerData != null)
                playerData.isAlive = true;
        }

        [Server]
        public void Convert()
        {
            if (isConverted) return;

            isConverted = true;
            sporeExposure = sporeConversionThreshold;

            var playerData = GetPlayerData();
            if (playerData != null)
                playerData.isConverted = true;

            RpcOnConverted();
        }

        [Server]
        public void MarkConverted() => Convert();

        [Command]
        public void CmdRequestEquipGear(NetworkIdentity gearIdentity, bool primary)
        {
            if (gearIdentity == null) return;

            if (primary)
                primaryGear = gearIdentity;
            else
                secondaryGear = gearIdentity;
        }

        [Command]
        public void CmdRequestUseGear(bool primary)
        {
            NetworkIdentity gear = primary ? primaryGear : secondaryGear;
            if (gear == null) return;

            // Gear use is delegated to the gear component itself
            gear.gameObject.SendMessage("ServerUse", this, SendMessageOptions.DontRequireReceiver);
        }

        [Command]
        public void CmdRequestInteract(NetworkIdentity target)
        {
            if (target == null || !isAlive) return;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > 3f) return;

            target.gameObject.SendMessage("ServerInteract", this, SendMessageOptions.DontRequireReceiver);
        }

        private GameManager.PlayerData GetPlayerData()
        {
            if (connectionToClient == null || GameManager.Instance == null) return null;
            return GameManager.Instance.GetPlayer(connectionToClient.connectionId);
        }

        [ClientRpc]
        private void RpcOnDeath()
        {
            OnDeath?.Invoke();
        }

        [ClientRpc]
        private void RpcOnConverted()
        {
            OnConverted?.Invoke();
        }

        [ClientRpc]
        private void RpcSporeExposureChanged(float exposure)
        {
            OnSporeExposureChanged?.Invoke(exposure);
        }

        private void OnSubjectNumberChanged(int oldVal, int newVal) { }
        private void OnDisplayNameChanged(string oldVal, string newVal) { }

        private void OnConvertedChanged(bool oldVal, bool newVal)
        {
            if (newVal) OnConverted?.Invoke();
        }

        private void OnAliveChanged(bool oldVal, bool newVal)
        {
            if (!newVal) OnDeath?.Invoke();
        }

        private void OnHealthValueChanged(float oldVal, float newVal)
        {
            OnHealthChanged?.Invoke(newVal, maxHealth);
        }
    }
}
