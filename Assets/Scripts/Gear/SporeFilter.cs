using System;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;

namespace MimicFacility.Gear
{
    public class SporeFilter : GearItem
    {
        public override string GearName => "Spore Filter";

        public event Action OnFilterDepleted;

        [Header("Filter Settings")]
        [SerializeField] private float filterEfficiency = 0.75f;
        [SerializeField] private float maxDuration = 120f;

        [SyncVar(hook = nameof(OnWornChanged))]
        private bool isWorn;
        public bool IsWorn => isWorn;

        private float remainingDuration;

        public float DurationNormalized => remainingDuration / maxDuration;

        private void Awake()
        {
            UsesRemaining = -1;
            remainingDuration = maxDuration;
        }

        private void Update()
        {
            if (!isServer || !isWorn) return;

            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0f)
            {
                remainingDuration = 0f;
                isWorn = false;
                OnFilterDepleted?.Invoke();
            }
        }

        public override void OnUse(PlayerCharacter player)
        {
            if (!isServer) return;
            if (!isWorn && remainingDuration <= 0f) return;

            isWorn = !isWorn;
        }

        public override void OnStopUse(PlayerCharacter player)
        {
            if (!isServer) return;
            if (isWorn)
                isWorn = false;
        }

        public float GetFilterEfficiency()
        {
            return isWorn ? filterEfficiency : 0f;
        }

        private void OnWornChanged(bool oldVal, bool newVal) { }
    }
}
