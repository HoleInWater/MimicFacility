using UnityEngine;
using System.Collections;
using MimicFacility.Core;

namespace MimicFacility.Characters
{
    [PlayerComponent("Physics", order: 40, optional: true)]
    public class PlayerRagdoll : MonoBehaviour
    {
        [Header("Settings")]
        public float minImpactForce = 15f;
        public float ragdollDuration = 2f;
        public float recoverDuration = 0.5f;

        [Header("References")]
        public Animator animator;

        private Rigidbody[] ragdollBodies;
        private Collider[] ragdollColliders;
        private bool isRagdollActive;
        private bool isRecovering;
        private Rigidbody mainRb;

        void Start()
        {
            if (animator == null) animator = GetComponent<Animator>();
            mainRb = GetComponent<Rigidbody>();

            ragdollBodies = GetComponentsInChildren<Rigidbody>();
            ragdollColliders = GetComponentsInChildren<Collider>();

            DisableRagdoll();
        }

        void DisableRagdoll()
        {
            foreach (var rb in ragdollBodies)
            {
                if (rb == mainRb) continue;
                rb.isKinematic = true;
            }
            foreach (var col in ragdollColliders)
            {
                if (col.gameObject == gameObject) continue;
                col.enabled = false;
            }

            if (animator != null) animator.enabled = true;
            isRagdollActive = false;
        }

        void EnableRagdoll()
        {
            if (animator != null) animator.enabled = false;

            foreach (var rb in ragdollBodies)
            {
                if (rb == mainRb) continue;
                rb.isKinematic = false;
            }
            foreach (var col in ragdollColliders)
            {
                if (col.gameObject == gameObject) continue;
                col.enabled = true;
            }

            isRagdollActive = true;
        }

        public void OnImpact(Vector3 direction, float force)
        {
            if (isRagdollActive || isRecovering) return;
            if (force < minImpactForce) return;

            StartCoroutine(RagdollSequence(direction, force));
        }

        IEnumerator RagdollSequence(Vector3 direction, float force)
        {
            EnableRagdoll();

            foreach (var rb in ragdollBodies)
            {
                if (rb == mainRb) continue;
                rb.AddForce(direction * force * 0.3f, ForceMode.Impulse);
            }

            yield return new WaitForSecondsRealtime(ragdollDuration);

            isRecovering = true;
            DisableRagdoll();

            yield return new WaitForSecondsRealtime(recoverDuration);
            isRecovering = false;
        }

        public void OnDeath()
        {
            StopAllCoroutines();
            EnableRagdoll();
        }

        public bool IsRagdollActive() => isRagdollActive;
    }
}
