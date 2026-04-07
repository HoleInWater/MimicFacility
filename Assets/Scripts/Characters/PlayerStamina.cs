using UnityEngine;
using MimicFacility.Core;

namespace MimicFacility.Characters
{
    [PlayerComponent("Movement", order: 100)]
    public class PlayerStamina : MonoBehaviour
    {
        [Header("Stamina Pool")]
        public float maxStamina = 100f;
        public float currentStamina;

        [Header("Aerobic / Anaerobic Model")]
        [Range(0.2f, 0.8f)]
        public float lactateThreshold = 0.50f;
        [Range(10f, 80f)]
        public float maxOxygenDebt = 40f;
        [Range(0.5f, 20f)]
        public float debtAccumulationRate = 5f;
        [Range(0.1f, 15f)]
        public float debtRecoveryRate = 0.5f;

        [Header("Recovery Rates")]
        [Range(0.5f, 20f)]
        public float aerobicRegenRate = 3f;
        [Range(0.1f, 5f)]
        public float anaerobicRegenRate = 0.5f;
        [Range(0.5f, 4f)]
        public float regenDelay = 2f;

        [Header("Exhaustion")]
        [Range(1f, 15f)]
        public float exhaustionDuration = 8f;
        [Range(0.1f, 1f)]
        public float exhaustionSpeedFactor = 0.4f;

        public bool IsExhausted { get; private set; }
        public bool IsInAnaerobicZone => currentStamina < maxStamina * lactateThreshold;
        public float OxygenDebt { get; private set; }

        public float ExhaustionPenalty => IsExhausted
            ? Mathf.Lerp(exhaustionSpeedFactor, 1f, 1f - (exhaustionTimer / exhaustionDuration))
            : 1f;

        private float regenTimer;
        private float exhaustionTimer;

        void Start()
        {
            currentStamina = maxStamina;
        }

        void Update()
        {
            TickExhaustion();
            TickRecovery();
        }

        private void TickExhaustion()
        {
            if (!IsExhausted) return;
            exhaustionTimer -= Time.deltaTime;
            if (exhaustionTimer <= 0f)
            {
                IsExhausted = false;
                exhaustionTimer = 0f;
            }
        }

        private void TickRecovery()
        {
            if (OxygenDebt > 0f && regenTimer <= 0f)
            {
                float debtDecayScale = IsExhausted ? 0.2f : 1f;
                OxygenDebt = Mathf.Max(0f, OxygenDebt - debtRecoveryRate * debtDecayScale * Time.deltaTime);
            }

            if (IsExhausted || regenTimer > 0f)
            {
                regenTimer = Mathf.Max(0f, regenTimer - Time.deltaTime);
                return;
            }

            if (currentStamina >= maxStamina) return;

            float depletion = 1f - (currentStamina / maxStamina);
            float regenScale = Mathf.Lerp(0.4f, 1f, depletion);
            float rate = OxygenDebt > 1f ? anaerobicRegenRate : aerobicRegenRate;

            currentStamina = Mathf.Clamp(currentStamina + rate * regenScale * Time.deltaTime, 0f, maxStamina);
        }

        public void DrainStamina(float amountPerSecond)
        {
            if (IsExhausted) return;

            currentStamina -= amountPerSecond * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            regenTimer = regenDelay;

            if (currentStamina < maxStamina * lactateThreshold)
            {
                float depthRatio = 1f - (currentStamina / (maxStamina * lactateThreshold));
                OxygenDebt = Mathf.Min(maxOxygenDebt,
                    OxygenDebt + debtAccumulationRate * depthRatio * Time.deltaTime);
            }

            if (currentStamina <= 0f) TriggerExhaustion();
        }

        public void UseStamina(float amount)
        {
            if (IsExhausted) return;

            currentStamina -= amount;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            regenTimer = regenDelay;

            if (currentStamina < maxStamina * lactateThreshold)
                OxygenDebt = Mathf.Min(maxOxygenDebt, OxygenDebt + amount * 0.4f);

            if (currentStamina <= 0f) TriggerExhaustion();
        }

        public float GetCurrentStamina() => currentStamina;
        public float GetNormalizedStamina() => currentStamina / maxStamina;

        private void TriggerExhaustion()
        {
            if (IsExhausted) return;
            IsExhausted = true;
            exhaustionTimer = exhaustionDuration;
            currentStamina = 0f;
            OxygenDebt = maxOxygenDebt;
            regenTimer = exhaustionDuration + regenDelay;
        }
    }
}
