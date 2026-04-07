using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace MimicFacility.Gear
{
    [Serializable]
    public class GearSpawnPoint
    {
        public Transform position;
        public GearItem prefab;
        public float respawnTime = 120f;
    }

    public class GearManager : NetworkBehaviour
    {
        public event Action<GearItem> OnGearPickedUp;
        public event Action<GearItem> OnGearDropped;

        [Header("Spawn Configuration")]
        [SerializeField] private List<GearSpawnPoint> spawnPoints = new List<GearSpawnPoint>();
        [SerializeField] private List<GearItem> gearPrefabs = new List<GearItem>();

        private readonly List<GearItem> spawnedGear = new List<GearItem>();
        private readonly Dictionary<GearItem, GearSpawnPoint> gearToSpawnPoint = new Dictionary<GearItem, GearSpawnPoint>();

        public override void OnStartServer()
        {
            SpawnInitialGear();
        }

        [Server]
        public void SpawnInitialGear()
        {
            foreach (var point in spawnPoints)
            {
                if (point.prefab == null || point.position == null) continue;
                SpawnGearAtPoint(point);
            }
        }

        [Server]
        private void SpawnGearAtPoint(GearSpawnPoint point)
        {
            var instance = Instantiate(point.prefab, point.position.position, point.position.rotation);
            NetworkServer.Spawn(instance.gameObject);

            spawnedGear.Add(instance);
            gearToSpawnPoint[instance] = point;
        }

        [Server]
        public void RegisterGearPickup(GearItem gear)
        {
            OnGearPickedUp?.Invoke(gear);

            if (gearToSpawnPoint.TryGetValue(gear, out var point))
            {
                if (point.respawnTime > 0f)
                    StartCoroutine(RespawnGearDelayed(point));
            }
        }

        [Server]
        public void RegisterGearDrop(GearItem gear)
        {
            OnGearDropped?.Invoke(gear);
        }

        [Server]
        private IEnumerator RespawnGearDelayed(GearSpawnPoint point)
        {
            yield return new WaitForSeconds(point.respawnTime);
            RespawnGear(point);
        }

        [Server]
        public void RespawnGear(GearSpawnPoint point)
        {
            if (point.prefab == null || point.position == null) return;
            SpawnGearAtPoint(point);
        }

        public List<GearItem> GetAvailableGear()
        {
            var available = new List<GearItem>();
            foreach (var gear in spawnedGear)
            {
                if (gear != null && !gear.IsPickedUp)
                    available.Add(gear);
            }
            return available;
        }

        private void OnDestroy()
        {
            spawnedGear.Clear();
            gearToSpawnPoint.Clear();
        }
    }
}
