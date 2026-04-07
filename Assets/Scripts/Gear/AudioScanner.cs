using System;
using System.Collections;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;
using MimicFacility.Entities;
using MimicFacility.Gameplay;

namespace MimicFacility.Gear
{
    [Serializable]
    public struct ScanResult
    {
        public uint targetNetId;
        public bool isMimic;
        public float waveformIntegrity;
        public float timestamp;
    }

    public class AudioScanner : GearItem
    {
        public override string GearName => "Audio Scanner";

        public event Action<ScanResult> OnScanComplete;

        [Header("Scanner Settings")]
        [SerializeField] private float scanRange = 5f;
        [SerializeField] private float scanDuration = 2f;

        private bool isScanning;
        public bool IsScanning => isScanning;

        private void Awake()
        {
            UsesRemaining = 5;
        }

        public override void OnUse(PlayerCharacter player)
        {
            if (!isServer) return;
            if (isScanning) return;
            if (!ConsumeUse()) return;

            StartCoroutine(ScanCoroutine(player));
        }

        public override void OnStopUse(PlayerCharacter player) { }

        [Server]
        private IEnumerator ScanCoroutine(PlayerCharacter player)
        {
            isScanning = true;

            yield return new WaitForSeconds(scanDuration);

            var colliders = Physics.OverlapSphere(player.transform.position, scanRange);
            NetworkIdentity nearestTarget = null;
            float nearestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col.gameObject == player.gameObject) continue;

                var identity = col.GetComponent<NetworkIdentity>();
                if (identity == null) continue;

                bool isValidTarget = col.GetComponent<PlayerCharacter>() != null
                    || col.GetComponent<MimicBase>() != null;
                if (!isValidTarget) continue;

                float dist = Vector3.Distance(player.transform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestTarget = identity;
                }
            }

            isScanning = false;

            if (nearestTarget == null) yield break;

            bool isMimic = nearestTarget.GetComponent<MimicBase>() != null;
            float integrity = isMimic
                ? UnityEngine.Random.Range(0.6f, 0.85f)
                : UnityEngine.Random.Range(0.92f, 1.0f);

            var result = new ScanResult
            {
                targetNetId = nearestTarget.netId,
                isMimic = isMimic,
                waveformIntegrity = integrity,
                timestamp = Time.time
            };

            RpcScanComplete(result);
        }

        [ClientRpc]
        private void RpcScanComplete(ScanResult result)
        {
            OnScanComplete?.Invoke(result);
        }
    }
}
