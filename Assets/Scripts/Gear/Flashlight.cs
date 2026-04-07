using System;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;

namespace MimicFacility.Gear
{
    public class Flashlight : GearItem
    {
        public override string GearName => "Flashlight";

        [Header("Flashlight Settings")]
        [SerializeField] private Light spotlight;

        [SyncVar(hook = nameof(OnRepIsOn))]
        private bool isOn;
        public bool IsOn => isOn;

        [SerializeField] private float maxBattery = 120f;
        private float currentBattery;

        public float BatteryNormalized => currentBattery / maxBattery;

        public event Action OnBatteryDepleted;

        private void Awake()
        {
            UsesRemaining = -1;
            currentBattery = maxBattery;

            if (spotlight != null)
            {
                spotlight.type = LightType.Spot;
                spotlight.intensity = 3f;
                spotlight.range = 20f;
                spotlight.spotAngle = 35f;
                spotlight.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isServer || !isOn) return;

            currentBattery -= Time.deltaTime;
            if (currentBattery <= 0f)
            {
                currentBattery = 0f;
                isOn = false;
                OnBatteryDepleted?.Invoke();
            }
        }

        public override void OnUse(PlayerCharacter player)
        {
            if (!isServer) return;
            if (!isOn && currentBattery <= 0f) return;

            isOn = !isOn;
        }

        public override void OnStopUse(PlayerCharacter player) { }

        public void RechargeBattery(float seconds)
        {
            currentBattery = Mathf.Min(maxBattery, currentBattery + seconds);
        }

        private void OnRepIsOn(bool oldVal, bool newVal)
        {
            if (spotlight != null)
                spotlight.gameObject.SetActive(newVal);
        }
    }
}
