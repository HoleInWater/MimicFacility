using UnityEngine;
using UnityEngine.AI;
using MimicFacility.Core;

namespace MimicFacility.Characters
{
    [PlayerComponent("Movement", order: 15)]
    public class SixthSense : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float senseRadius = 25f;
        [SerializeField] private float checkInterval = 0.5f;
        [SerializeField] private LayerMask entityLayer = ~0;

        [Header("Sprint Gate")]
        [SerializeField] private float sprintUnlockDuration = 5f;
        [SerializeField] private float adrenalineFadeRate = 1f;

        [Header("Feedback")]
        [SerializeField] private float heartbeatBaseRate = 0.8f;
        [SerializeField] private float heartbeatChaseRate = 0.3f;

        public bool IsBeingChased { get; private set; }
        public bool CanSprint => adrenalineLevel > 0f;
        public float AdrenalineLevel => adrenalineLevel;
        public float ThreatDistance { get; private set; }

        private float adrenalineLevel;
        private float checkTimer;
        private PlayerMovement movement;

        void Start()
        {
            movement = GetComponent<PlayerMovement>();
        }

        void Update()
        {
            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0f)
            {
                checkTimer = checkInterval;
                DetectThreats();
            }

            if (IsBeingChased)
            {
                adrenalineLevel = Mathf.Min(1f, adrenalineLevel + Time.deltaTime / sprintUnlockDuration * 2f);
            }
            else
            {
                adrenalineLevel = Mathf.Max(0f, adrenalineLevel - adrenalineFadeRate * Time.deltaTime);
            }

            if (movement != null)
            {
                movement.externalSpeedMultiplier = CanSprint ? 1f : 0.6f;
            }
        }

        private void DetectThreats()
        {
            IsBeingChased = false;
            ThreatDistance = float.MaxValue;

            // Check Stalkers
            foreach (var stalker in FindObjectsOfType<MimicFacility.Entities.Stalker>())
            {
                if (stalker.IsFrozen) continue;
                float dist = Vector3.Distance(transform.position, stalker.transform.position);
                if (dist < senseRadius)
                {
                    NavMeshAgent agent = stalker.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.hasPath && !agent.isStopped)
                    {
                        IsBeingChased = true;
                        if (dist < ThreatDistance) ThreatDistance = dist;
                    }
                }
            }

            // Check Frauds in pursuit
            foreach (var fraud in FindObjectsOfType<MimicFacility.Entities.Fraud>())
            {
                if (fraud.CurrentState != MimicFacility.Entities.EFraudState.Pursuing) continue;
                float dist = Vector3.Distance(transform.position, fraud.transform.position);
                if (dist < senseRadius)
                {
                    IsBeingChased = true;
                    if (dist < ThreatDistance) ThreatDistance = dist;
                }
            }

            // Check any MimicBase in attack state
            foreach (var mimic in FindObjectsOfType<MimicFacility.Entities.MimicBase>())
            {
                float dist = Vector3.Distance(transform.position, mimic.transform.position);
                if (dist < senseRadius)
                {
                    NavMeshAgent agent = mimic.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.hasPath && !agent.isStopped)
                    {
                        Vector3 agentDir = (agent.destination - mimic.transform.position).normalized;
                        Vector3 toPlayer = (transform.position - mimic.transform.position).normalized;
                        if (Vector3.Dot(agentDir, toPlayer) > 0.7f)
                        {
                            IsBeingChased = true;
                            if (dist < ThreatDistance) ThreatDistance = dist;
                        }
                    }
                }
            }
        }

        public float GetHeartbeatRate()
        {
            if (!IsBeingChased) return heartbeatBaseRate;
            float urgency = 1f - Mathf.Clamp01(ThreatDistance / senseRadius);
            return Mathf.Lerp(heartbeatBaseRate, heartbeatChaseRate, urgency);
        }

        public float GetNormalizedThreat()
        {
            if (!IsBeingChased) return 0f;
            return 1f - Mathf.Clamp01(ThreatDistance / senseRadius);
        }
    }
}
