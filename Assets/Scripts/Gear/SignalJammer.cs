using System;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;

namespace MimicFacility.Gear
{
    public class SignalJammer : GearItem
    {
        public override string GearName => "Signal Jammer";

        public event Action OnBatteryDepleted;

        [Header("Jammer Settings")]
        [SerializeField] private float jamRadius = 8f;
        [SerializeField] private float maxBattery = 60f;

        [SyncVar(hook = nameof(OnActiveChanged))]
        private bool isActive;
        public bool IsActive => isActive;

        private SphereCollider jamZone;
        private float currentBattery;

        private void Awake()
        {
            UsesRemaining = -1;
            currentBattery = maxBattery;

            jamZone = gameObject.AddComponent<SphereCollider>();
            jamZone.isTrigger = true;
            jamZone.radius = jamRadius;
            jamZone.enabled = false;
        }

        private void Update()
        {
            if (!isServer || !isActive) return;

            currentBattery -= Time.deltaTime;
            if (currentBattery <= 0f)
            {
                currentBattery = 0f;
                isActive = false;
                OnBatteryDepleted?.Invoke();
            }
        }

        public override void OnUse(PlayerCharacter player)
        {
            if (!isServer) return;
            if (!isActive && currentBattery <= 0f) return;

            isActive = !isActive;
        }

        public override void OnStopUse(PlayerCharacter player)
        {
            if (!isServer) return;
            if (isActive)
                isActive = false;
        }

        public bool IsInJamZone(Vector3 position)
        {
            if (!isActive) return false;
            return Vector3.Distance(transform.position, position) <= jamRadius;
        }

        public float BatteryNormalized => currentBattery / maxBattery;

        private void OnActiveChanged(bool oldVal, bool newVal)
        {
            if (jamZone != null)
                jamZone.enabled = newVal;
        }
    }
}
