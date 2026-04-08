using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;

namespace MimicFacility.Entities
{
    /// <summary>
    /// The Mimic Hive Mind — all mimics are one organism.
    /// When one sees you, they all know where you are.
    /// When one hears your voice, they all learn your phrases.
    /// When one dies, the others become more aggressive.
    /// They are not individuals. They are fingers on the same hand.
    /// </summary>
    public class MimicHiveMind : NetworkBehaviour
    {
        public static MimicHiveMind Instance { get; private set; }

        [Header("Hive State")]
        [SyncVar] private int totalMimicCount;
        [SyncVar] private int aliveMimicCount;
        [SyncVar] private int containedCount;
        [SyncVar] private float hiveAggression;
        [SyncVar] private float hiveAwareness;

        [Header("Shared Knowledge")]
        [SerializeField] private float knowledgeShareRange = 1000f;
        [SerializeField] private float aggressionPerDeath = 0.15f;
        [SerializeField] private float awarenessDecayRate = 0.02f;
        [SerializeField] private float coordinationRadius = 30f;

        // Shared voice data — when one mimic hears a phrase, all mimics get it
        private readonly Dictionary<string, List<string>> sharedVoiceData = new Dictionary<string, List<string>>();

        // Shared player positions — when one mimic spots a player, all know
        private readonly Dictionary<uint, PlayerSighting> sharedSightings = new Dictionary<uint, PlayerSighting>();

        // Active mimics in the hive
        private readonly List<MimicBase> hiveMimics = new List<MimicBase>();

        // Death memory — the hive remembers how each mimic died
        private readonly List<DeathRecord> deathRecords = new List<DeathRecord>();

        // Coordinated hunt targets — prevents all mimics chasing the same player
        private readonly Dictionary<uint, uint> huntAssignments = new Dictionary<uint, uint>();

        public int TotalCount => totalMimicCount;
        public int AliveCount => aliveMimicCount;
        public float Aggression => hiveAggression;
        public float Awareness => hiveAwareness;

        public struct PlayerSighting
        {
            public Vector3 lastPosition;
            public float timestamp;
            public uint spottedByMimicId;
            public bool isCurrentlyVisible;
        }

        public struct DeathRecord
        {
            public Vector3 deathPosition;
            public float timestamp;
            public uint killedByPlayerId;
            public string deathCause;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        [Server]
        void Update()
        {
            if (!isServer) return;

            // Awareness decays over time — mimics forget if they don't see players
            hiveAwareness = Mathf.Max(0f, hiveAwareness - awarenessDecayRate * Time.deltaTime);

            // Clean old sightings
            var staleKeys = new List<uint>();
            foreach (var kvp in sharedSightings)
            {
                if (Time.time - kvp.Value.timestamp > 30f)
                    staleKeys.Add(kvp.Key);
            }
            foreach (var key in staleKeys)
                sharedSightings.Remove(key);

            // Coordinate hunting — reassign if targets are stale
            if (Time.frameCount % 60 == 0)
                CoordinateHunting();
        }

        // ═══════════════════════════════════════════════════════════════
        // REGISTRATION — mimics join/leave the hive
        // ═══════════════════════════════════════════════════════════════

        [Server]
        public void RegisterMimic(MimicBase mimic)
        {
            if (!hiveMimics.Contains(mimic))
            {
                hiveMimics.Add(mimic);
                totalMimicCount = hiveMimics.Count;
                aliveMimicCount = hiveMimics.Count;
                Debug.Log($"[HiveMind] Mimic registered. Hive size: {aliveMimicCount}");
            }
        }

        [Server]
        public void UnregisterMimic(MimicBase mimic, string cause = "unknown", uint killedBy = 0)
        {
            if (hiveMimics.Remove(mimic))
            {
                aliveMimicCount = hiveMimics.Count;

                deathRecords.Add(new DeathRecord
                {
                    deathPosition = mimic.transform.position,
                    timestamp = Time.time,
                    killedByPlayerId = killedBy,
                    deathCause = cause
                });

                // Hive responds to death — aggression rises
                hiveAggression = Mathf.Min(1f, hiveAggression + aggressionPerDeath);

                // Alert nearby mimics to flee from the death location
                AlertNearby(mimic.transform.position, EMimicState.Fleeing, 15f);

                Debug.Log($"[HiveMind] Mimic lost. Remaining: {aliveMimicCount}. Aggression: {hiveAggression:F2}");

                RpcHiveMemberLost(mimic.transform.position);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // SHARED KNOWLEDGE — one sees, all know
        // ═══════════════════════════════════════════════════════════════

        [Server]
        public void SharePlayerSighting(uint mimicId, uint playerId, Vector3 position, bool currentlyVisible)
        {
            sharedSightings[playerId] = new PlayerSighting
            {
                lastPosition = position,
                timestamp = Time.time,
                spottedByMimicId = mimicId,
                isCurrentlyVisible = currentlyVisible
            };

            hiveAwareness = Mathf.Min(1f, hiveAwareness + 0.05f);
        }

        [Server]
        public void ShareVoiceData(string playerId, string phrase)
        {
            if (!sharedVoiceData.ContainsKey(playerId))
                sharedVoiceData[playerId] = new List<string>();

            if (!sharedVoiceData[playerId].Contains(phrase))
            {
                sharedVoiceData[playerId].Add(phrase);
                // Cap at 50 phrases per player
                if (sharedVoiceData[playerId].Count > 50)
                    sharedVoiceData[playerId].RemoveAt(0);
            }
        }

        public PlayerSighting? GetPlayerSighting(uint playerId)
        {
            if (sharedSightings.TryGetValue(playerId, out var sighting))
                return sighting;
            return null;
        }

        public List<string> GetSharedPhrases(string playerId)
        {
            if (sharedVoiceData.TryGetValue(playerId, out var phrases))
                return phrases;
            return new List<string>();
        }

        public Vector3? GetNearestKnownPlayerPosition(Vector3 fromPosition)
        {
            Vector3? nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var kvp in sharedSightings)
            {
                if (Time.time - kvp.Value.timestamp > 15f) continue;

                float dist = Vector3.Distance(fromPosition, kvp.Value.lastPosition);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = kvp.Value.lastPosition;
                }
            }

            return nearest;
        }

        // ═══════════════════════════════════════════════════════════════
        // COORDINATION — the hive hunts as one
        // ═══════════════════════════════════════════════════════════════

        [Server]
        private void CoordinateHunting()
        {
            huntAssignments.Clear();

            // Gather all known player positions
            var knownPlayers = new List<KeyValuePair<uint, Vector3>>();
            foreach (var kvp in sharedSightings)
            {
                if (Time.time - kvp.Value.timestamp < 20f)
                    knownPlayers.Add(new KeyValuePair<uint, Vector3>(kvp.Key, kvp.Value.lastPosition));
            }

            if (knownPlayers.Count == 0 || hiveMimics.Count == 0) return;

            // Distribute mimics across known players — no two mimics chase the same target
            // unless there are more mimics than players
            int mimicIdx = 0;
            foreach (var mimic in hiveMimics)
            {
                if (mimic == null) continue;
                var target = knownPlayers[mimicIdx % knownPlayers.Count];
                huntAssignments[mimic.netId] = target.Key;
                mimicIdx++;
            }
        }

        public uint? GetHuntTarget(uint mimicId)
        {
            if (huntAssignments.TryGetValue(mimicId, out uint targetId))
                return targetId;
            return null;
        }

        [Server]
        public void AlertNearby(Vector3 position, EMimicState state, float radius)
        {
            foreach (var mimic in hiveMimics)
            {
                if (mimic == null) continue;
                float dist = Vector3.Distance(mimic.transform.position, position);
                if (dist <= radius)
                    mimic.SetState(state);
            }
        }

        [Server]
        public void AlertAll(EMimicState state)
        {
            foreach (var mimic in hiveMimics)
            {
                if (mimic != null)
                    mimic.SetState(state);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // AGGRESSION MODIFIERS
        // ═══════════════════════════════════════════════════════════════

        public float GetSpeedMultiplier()
        {
            // Mimics get faster as the hive loses members
            return 1f + hiveAggression * 0.5f;
        }

        public float GetDetectionMultiplier()
        {
            // Mimics detect further when aggressive
            return 1f + hiveAggression * 0.3f;
        }

        public float GetDamageMultiplier()
        {
            // Mimics hit harder when desperate
            return 1f + hiveAggression * 0.4f;
        }

        public bool ShouldCoordinateAttack()
        {
            // At high aggression, mimics rush together instead of stalking
            return hiveAggression > 0.6f;
        }

        // ═══════════════════════════════════════════════════════════════
        // DEATH ANALYSIS — the hive learns from its losses
        // ═══════════════════════════════════════════════════════════════

        public Vector3? GetMostDangerousArea()
        {
            if (deathRecords.Count == 0) return null;

            // Find cluster of deaths — that area is dangerous
            Vector3 sum = Vector3.zero;
            int recentDeaths = 0;
            foreach (var record in deathRecords)
            {
                if (Time.time - record.timestamp < 120f)
                {
                    sum += record.deathPosition;
                    recentDeaths++;
                }
            }

            if (recentDeaths == 0) return null;
            return sum / recentDeaths;
        }

        public bool IsAreaDangerous(Vector3 position, float radius = 10f)
        {
            foreach (var record in deathRecords)
            {
                if (Time.time - record.timestamp < 60f &&
                    Vector3.Distance(position, record.deathPosition) < radius)
                    return true;
            }
            return false;
        }

        [ClientRpc]
        private void RpcHiveMemberLost(Vector3 position)
        {
            // Visual/audio feedback on all clients that the hive reacted
            Debug.Log($"[HiveMind] The hive shudders. A member was lost at {position}.");
        }

        public IReadOnlyList<MimicBase> AllMimics => hiveMimics;
    }
}
