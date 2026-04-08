using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Entities;
using MimicFacility.Characters;

namespace MimicFacility.Gameplay
{
    public enum EEntityType
    {
        Mimic,
        Stalker,
        Fraud,
        Phantom,
        Parasite,
        Skinwalker,
        Warden,
        Singer
    }

    [Serializable]
    public class EntitySpawnEntry
    {
        public EEntityType entityType;
        public int count;
    }

    [Serializable]
    public class RoundSpawnConfig
    {
        public int round;
        public List<EntitySpawnEntry> entries = new List<EntitySpawnEntry>();
    }

    public class EntitySpawnManager : NetworkBehaviour
    {
        [Header("Entity Prefabs")]
        [SerializeField] private List<GameObject> mimicPrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> stalkerPrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> fraudPrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> phantomPrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> parasitePrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> skinwalkerPrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> wardenPrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> singerPrefabs = new List<GameObject>();

        [Header("Spawn Settings")]
        [SerializeField] private float minPlayerDistance = 15f;
        [SerializeField] private float maxSpawnRadius = 80f;
        [SerializeField] private float navMeshSampleRadius = 10f;
        [SerializeField] private int maxSpawnAttempts = 30;
        [SerializeField] private float lineOfSightDotThreshold = 0.5f;

        [Header("Spawn Table")]
        [SerializeField] private List<RoundSpawnConfig> spawnTable = new List<RoundSpawnConfig>();

        private readonly Dictionary<EEntityType, List<GameObject>> activeEntities = new Dictionary<EEntityType, List<GameObject>>();
        private bool singerSpawned;

        public event Action<EEntityType, GameObject> OnEntitySpawned;
        public event Action<EEntityType> OnEntityDied;

        void Awake()
        {
            foreach (EEntityType type in Enum.GetValues(typeof(EEntityType)))
                activeEntities[type] = new List<GameObject>();

            if (spawnTable.Count == 0)
                BuildDefaultSpawnTable();
        }

        private void BuildDefaultSpawnTable()
        {
            // Round 1: voice recording phase, no entities
            spawnTable.Add(new RoundSpawnConfig
            {
                round = 1,
                entries = new List<EntitySpawnEntry>()
            });

            // Round 2: first contact
            spawnTable.Add(new RoundSpawnConfig
            {
                round = 2,
                entries = new List<EntitySpawnEntry>
                {
                    new EntitySpawnEntry { entityType = EEntityType.Mimic, count = 2 },
                    new EntitySpawnEntry { entityType = EEntityType.Stalker, count = 1 }
                }
            });

            // Round 3: escalation — Singer appears
            spawnTable.Add(new RoundSpawnConfig
            {
                round = 3,
                entries = new List<EntitySpawnEntry>
                {
                    new EntitySpawnEntry { entityType = EEntityType.Mimic, count = 3 },
                    new EntitySpawnEntry { entityType = EEntityType.Stalker, count = 1 },
                    new EntitySpawnEntry { entityType = EEntityType.Fraud, count = 1 },
                    new EntitySpawnEntry { entityType = EEntityType.Phantom, count = 1 },
                    new EntitySpawnEntry { entityType = EEntityType.Singer, count = 1 }
                }
            });
        }

        /// <summary>
        /// Generates a spawn config for rounds beyond the explicit table.
        /// Each round past 3 adds more dangerous types.
        /// </summary>
        private RoundSpawnConfig GenerateEscalationConfig(int round)
        {
            var config = new RoundSpawnConfig { round = round };
            int escalation = round - 3;

            config.entries.Add(new EntitySpawnEntry
            {
                entityType = EEntityType.Mimic,
                count = 3 + escalation
            });
            config.entries.Add(new EntitySpawnEntry
            {
                entityType = EEntityType.Stalker,
                count = 1 + Mathf.FloorToInt(escalation / 2f)
            });
            config.entries.Add(new EntitySpawnEntry
            {
                entityType = EEntityType.Fraud,
                count = 1 + Mathf.FloorToInt(escalation / 2f)
            });
            config.entries.Add(new EntitySpawnEntry
            {
                entityType = EEntityType.Phantom,
                count = 1 + Mathf.FloorToInt(escalation / 3f)
            });

            if (round >= 4)
            {
                config.entries.Add(new EntitySpawnEntry
                {
                    entityType = EEntityType.Parasite,
                    count = Mathf.FloorToInt(escalation / 2f) + 1
                });
            }

            if (round >= 5)
            {
                config.entries.Add(new EntitySpawnEntry
                {
                    entityType = EEntityType.Skinwalker,
                    count = 1
                });
            }

            if (round >= 6)
            {
                config.entries.Add(new EntitySpawnEntry
                {
                    entityType = EEntityType.Warden,
                    count = 1
                });
            }

            return config;
        }

        // ═══════════════════════════════════════════════════════════════
        // SPAWNING
        // ═══════════════════════════════════════════════════════════════

        [Server]
        public void SpawnForRound(int round)
        {
            RoundSpawnConfig config = null;

            foreach (var entry in spawnTable)
            {
                if (entry.round == round)
                {
                    config = entry;
                    break;
                }
            }

            // Beyond explicit table — generate escalation
            if (config == null)
                config = GenerateEscalationConfig(round);

            foreach (var entry in config.entries)
            {
                // Singer: only one ever exists
                if (entry.entityType == EEntityType.Singer)
                {
                    if (singerSpawned) continue;
                    SpawnEntity(EEntityType.Singer);
                    singerSpawned = true;
                    continue;
                }

                for (int i = 0; i < entry.count; i++)
                    SpawnEntity(entry.entityType);
            }

            Debug.Log($"[EntitySpawnManager] Spawned entities for round {round}. " +
                      $"Total alive: {GetTotalEntityCount()}");
        }

        [Server]
        private void SpawnEntity(EEntityType type)
        {
            GameObject prefab = SelectPrefab(type);
            if (prefab == null)
            {
                Debug.LogWarning($"[EntitySpawnManager] No prefab available for {type}");
                return;
            }

            Vector3 spawnPos = FindValidSpawnPosition();
            Quaternion spawnRot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            GameObject entity = Instantiate(prefab, spawnPos, spawnRot);
            NetworkServer.Spawn(entity);

            activeEntities[type].Add(entity);

            // Register mimics with the hive mind
            if (type == EEntityType.Mimic)
            {
                var mimicBase = entity.GetComponent<MimicBase>();
                if (mimicBase != null && MimicHiveMind.Instance != null)
                    MimicHiveMind.Instance.RegisterMimic(mimicBase);
            }

            // Track entity death
            var networkIdentity = entity.GetComponent<NetworkIdentity>();
            if (networkIdentity != null)
            {
                var tracker = entity.AddComponent<EntityDeathTracker>();
                tracker.Initialize(type, this);
            }

            OnEntitySpawned?.Invoke(type, entity);
        }

        private GameObject SelectPrefab(EEntityType type)
        {
            List<GameObject> pool = GetPrefabPool(type);
            if (pool == null || pool.Count == 0) return null;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        private List<GameObject> GetPrefabPool(EEntityType type)
        {
            switch (type)
            {
                case EEntityType.Mimic:      return mimicPrefabs;
                case EEntityType.Stalker:    return stalkerPrefabs;
                case EEntityType.Fraud:      return fraudPrefabs;
                case EEntityType.Phantom:    return phantomPrefabs;
                case EEntityType.Parasite:   return parasitePrefabs;
                case EEntityType.Skinwalker: return skinwalkerPrefabs;
                case EEntityType.Warden:     return wardenPrefabs;
                case EEntityType.Singer:     return singerPrefabs;
                default:                     return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // SPAWN POSITION — away from players, valid NavMesh, no LOS
        // ═══════════════════════════════════════════════════════════════

        private Vector3 FindValidSpawnPosition()
        {
            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * maxSpawnRadius;
                randomOffset.y = 0f;
                Vector3 candidate = Vector3.zero + randomOffset;

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                    continue;

                Vector3 position = hit.position;

                if (!IsFarEnoughFromPlayers(position))
                    continue;

                if (IsInPlayerLineOfSight(position))
                    continue;

                return position;
            }

            // Fallback: just find any valid NavMesh point far from origin
            Vector3 fallback = UnityEngine.Random.insideUnitSphere * maxSpawnRadius;
            fallback.y = 0f;
            if (NavMesh.SamplePosition(fallback, out NavMeshHit fallbackHit, navMeshSampleRadius * 2f, NavMesh.AllAreas))
                return fallbackHit.position;

            Debug.LogWarning("[EntitySpawnManager] Failed to find valid spawn position, using origin.");
            return Vector3.zero;
        }

        private bool IsFarEnoughFromPlayers(Vector3 position)
        {
            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                var state = player.GetComponent<MimicPlayerState>();
                if (state != null && !state.IsAlive) continue;

                if (Vector3.Distance(position, player.transform.position) < minPlayerDistance)
                    return false;
            }
            return true;
        }

        private bool IsInPlayerLineOfSight(Vector3 position)
        {
            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                var state = player.GetComponent<MimicPlayerState>();
                if (state != null && !state.IsAlive) continue;

                Camera cam = player.GetComponentInChildren<Camera>();
                if (cam == null) continue;

                Vector3 toEntity = (position - cam.transform.position).normalized;
                float dot = Vector3.Dot(cam.transform.forward, toEntity);

                if (dot > lineOfSightDotThreshold)
                {
                    // Within view cone — check if occluded
                    float distance = Vector3.Distance(cam.transform.position, position);
                    if (!Physics.Raycast(cam.transform.position, toEntity, distance))
                        return true;
                }
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════
        // DESPAWN
        // ═══════════════════════════════════════════════════════════════

        [Server]
        public void DespawnAll()
        {
            foreach (var kvp in activeEntities)
            {
                foreach (var entity in kvp.Value)
                {
                    if (entity != null)
                        NetworkServer.Destroy(entity);
                }
                kvp.Value.Clear();
            }

            Debug.Log("[EntitySpawnManager] All entities despawned.");
        }

        // ═══════════════════════════════════════════════════════════════
        // TRACKING
        // ═══════════════════════════════════════════════════════════════

        public int GetEntityCount(EEntityType type)
        {
            if (!activeEntities.TryGetValue(type, out var list))
                return 0;

            list.RemoveAll(e => e == null);
            return list.Count;
        }

        public int GetTotalEntityCount()
        {
            int total = 0;
            foreach (var kvp in activeEntities)
            {
                kvp.Value.RemoveAll(e => e == null);
                total += kvp.Value.Count;
            }
            return total;
        }

        [Server]
        internal void NotifyEntityDied(EEntityType type, GameObject entity)
        {
            if (activeEntities.TryGetValue(type, out var list))
                list.Remove(entity);

            OnEntityDied?.Invoke(type);

            Debug.Log($"[EntitySpawnManager] {type} died. Remaining: {GetEntityCount(type)} of type, {GetTotalEntityCount()} total.");

            // If Singer dies, allow another to spawn in future rounds
            if (type == EEntityType.Singer)
                singerSpawned = false;
        }
    }

    /// <summary>
    /// Attached to spawned entities to detect destruction and report back
    /// to the EntitySpawnManager.
    /// </summary>
    public class EntityDeathTracker : MonoBehaviour
    {
        private EEntityType entityType;
        private EntitySpawnManager spawnManager;

        public void Initialize(EEntityType type, EntitySpawnManager manager)
        {
            entityType = type;
            spawnManager = manager;
        }

        void OnDestroy()
        {
            if (spawnManager != null)
                spawnManager.NotifyEntityDied(entityType, gameObject);
        }
    }
}
