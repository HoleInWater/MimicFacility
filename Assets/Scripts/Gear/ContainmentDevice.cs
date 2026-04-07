using System.Collections;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;
using MimicFacility.Gameplay;

namespace MimicFacility.Gear
{
    public class ContainmentDevice : GearItem
    {
        public override string GearName => "Containment Device";

        [Header("Containment Settings")]
        [SerializeField] private float captureRange = 3f;
        [SerializeField] private float stunDuration = 10f;

        private void Awake()
        {
            UsesRemaining = 1;
        }

        public override void OnUse(PlayerCharacter player)
        {
            if (!isServer) return;
            if (!ConsumeUse()) return;

            var colliders = Physics.OverlapSphere(player.transform.position, captureRange);
            Transform nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col.gameObject == player.gameObject) continue;

                bool isTarget = col.GetComponent<MimicBase>() != null
                    || col.GetComponent<PlayerCharacter>() != null;
                if (!isTarget) continue;

                float dist = Vector3.Distance(player.transform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = col.transform;
                }
            }

            if (nearest == null)
            {
                RpcContainmentActivated(false);
                return;
            }

            var mimic = nearest.GetComponent<MimicBase>();
            if (mimic != null)
            {
                mimic.Contain();
                NetworkServer.Destroy(mimic.gameObject);
                RpcContainmentActivated(true);
                return;
            }

            var targetPlayer = nearest.GetComponent<PlayerCharacter>();
            if (targetPlayer != null)
            {
                StartCoroutine(StunPlayer(targetPlayer));
                RpcContainmentActivated(false);
            }
        }

        public override void OnStopUse(PlayerCharacter player) { }

        [Server]
        private IEnumerator StunPlayer(PlayerCharacter target)
        {
            target.enabled = false;
            yield return new WaitForSeconds(stunDuration);
            if (target != null)
                target.enabled = true;
        }

        [ClientRpc]
        private void RpcContainmentActivated(bool hitMimic) { }
    }
}
