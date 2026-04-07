using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;

namespace MimicFacility.Gameplay
{
    [Serializable]
    public class VerificationAssignment
    {
        public int watcherConnectionId;
        public int targetConnectionId;
        public string watcherName;
        public string targetName;
        public float assignedTime;
        public bool targetReplaced;
        public float replacedTime;
        public bool watcherDetectedReplacement;
        public float detectionDeadline;
    }

    public enum EVerificationStatus
    {
        Active,
        TargetSafe,
        TargetReplaced,
        DetectionSuccess,
        DetectionFailed
    }

    public class VerificationSystem : NetworkBehaviour
    {
        public event Action<string> OnAssignmentReceived;
        public event Action<int, EVerificationStatus> OnStatusChanged;
        public event Action<int> OnDetectionFailed;

        [SerializeField] private float detectionWindow = 90f;
        [SerializeField] private float proximityBonusRange = 8f;
        [SerializeField] private float proximityCheckInterval = 2f;
        [SerializeField] private float scannerPenaltyMultiplier = 0.7f;

        private readonly Dictionary<int, VerificationAssignment> assignments = new Dictionary<int, VerificationAssignment>();
        private readonly Dictionary<int, EVerificationStatus> statuses = new Dictionary<int, EVerificationStatus>();
        private readonly Dictionary<int, float> proximityTime = new Dictionary<int, float>();
        private readonly HashSet<int> penalizedPlayers = new HashSet<int>();

        public float ScannerPenaltyMultiplier => scannerPenaltyMultiplier;

        public override void OnStartServer()
        {
            StartCoroutine(ProximityTrackingLoop());
        }

        [Server]
        public void AssignTargets(List<int> playerConnectionIds)
        {
            assignments.Clear();
            statuses.Clear();
            proximityTime.Clear();
            penalizedPlayers.Clear();

            var shuffled = new List<int>(playerConnectionIds);
            ShuffleList(shuffled);

            for (int i = 0; i < shuffled.Count; i++)
            {
                int watcherId = shuffled[i];
                int targetId = shuffled[(i + 1) % shuffled.Count];

                string watcherName = GetPlayerName(watcherId);
                string targetName = GetPlayerName(targetId);

                var assignment = new VerificationAssignment
                {
                    watcherConnectionId = watcherId,
                    targetConnectionId = targetId,
                    watcherName = watcherName,
                    targetName = targetName,
                    assignedTime = Time.time,
                    targetReplaced = false,
                    watcherDetectedReplacement = false,
                    detectionDeadline = -1f
                };

                assignments[watcherId] = assignment;
                statuses[watcherId] = EVerificationStatus.Active;
                proximityTime[watcherId] = 0f;

                RpcNotifyAssignment(watcherId, targetName);
            }
        }

        [ClientRpc]
        private void RpcNotifyAssignment(int watcherConnectionId, string targetName)
        {
            if (!IsLocalWatcher(watcherConnectionId)) return;

            OnAssignmentReceived?.Invoke(targetName);
        }

        [Server]
        public void OnTargetReplaced(int replacedPlayerId)
        {
            foreach (var kvp in assignments)
            {
                if (kvp.Value.targetConnectionId == replacedPlayerId)
                {
                    kvp.Value.targetReplaced = true;
                    kvp.Value.replacedTime = Time.time;
                    kvp.Value.detectionDeadline = Time.time + detectionWindow;
                    statuses[kvp.Key] = EVerificationStatus.TargetReplaced;

                    StartCoroutine(DetectionTimer(kvp.Key, detectionWindow));
                    break;
                }
            }
        }

        [Server]
        public void OnPlayerContainedMimic(int containingPlayerId, int mimicTargetId)
        {
            if (!assignments.ContainsKey(containingPlayerId)) return;

            var assignment = assignments[containingPlayerId];
            if (assignment.targetConnectionId != mimicTargetId) return;
            if (!assignment.targetReplaced) return;

            assignment.watcherDetectedReplacement = true;
            statuses[containingPlayerId] = EVerificationStatus.DetectionSuccess;
            OnStatusChanged?.Invoke(containingPlayerId, EVerificationStatus.DetectionSuccess);

            RpcNotifyDetectionSuccess(containingPlayerId);
        }

        private IEnumerator DetectionTimer(int watcherConnectionId, float window)
        {
            yield return new WaitForSeconds(window);

            if (!assignments.ContainsKey(watcherConnectionId)) yield break;

            var assignment = assignments[watcherConnectionId];
            if (assignment.watcherDetectedReplacement) yield break;
            if (!assignment.targetReplaced) yield break;

            statuses[watcherConnectionId] = EVerificationStatus.DetectionFailed;
            penalizedPlayers.Add(watcherConnectionId);
            OnDetectionFailed?.Invoke(watcherConnectionId);
            OnStatusChanged?.Invoke(watcherConnectionId, EVerificationStatus.DetectionFailed);

            RpcNotifyDetectionFailed(watcherConnectionId);
        }

        [ClientRpc]
        private void RpcNotifyDetectionSuccess(int watcherConnectionId)
        {
            if (!IsLocalWatcher(watcherConnectionId)) return;

            OnStatusChanged?.Invoke(watcherConnectionId, EVerificationStatus.DetectionSuccess);
        }

        [ClientRpc]
        private void RpcNotifyDetectionFailed(int watcherConnectionId)
        {
            if (!IsLocalWatcher(watcherConnectionId)) return;

            OnStatusChanged?.Invoke(watcherConnectionId, EVerificationStatus.DetectionFailed);
        }

        private IEnumerator ProximityTrackingLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(proximityCheckInterval);

                foreach (var kvp in assignments)
                {
                    int watcherId = kvp.Key;
                    int targetId = kvp.Value.targetConnectionId;

                    var watcher = FindPlayerTransform(watcherId);
                    var target = FindPlayerTransform(targetId);

                    if (watcher == null || target == null) continue;

                    float distance = Vector3.Distance(watcher.position, target.position);
                    if (distance <= proximityBonusRange)
                    {
                        if (!proximityTime.ContainsKey(watcherId))
                            proximityTime[watcherId] = 0f;
                        proximityTime[watcherId] += proximityCheckInterval;
                    }
                }
            }
        }

        public bool IsPlayerPenalized(int connectionId)
        {
            return penalizedPlayers.Contains(connectionId);
        }

        public float GetScannerAccuracy(int connectionId)
        {
            if (penalizedPlayers.Contains(connectionId))
                return scannerPenaltyMultiplier;
            return 1f;
        }

        public bool IsWatchingTarget(int watcherId, int potentialTargetId)
        {
            if (!assignments.ContainsKey(watcherId)) return false;
            return assignments[watcherId].targetConnectionId == potentialTargetId;
        }

        public float GetProximityTime(int watcherId)
        {
            return proximityTime.ContainsKey(watcherId) ? proximityTime[watcherId] : 0f;
        }

        public VerificationAssignment GetAssignment(int watcherConnectionId)
        {
            return assignments.ContainsKey(watcherConnectionId) ? assignments[watcherConnectionId] : null;
        }

        public string GetAssignmentGraphForDirector()
        {
            var lines = new List<string>();
            foreach (var kvp in assignments)
            {
                var a = kvp.Value;
                string status = statuses.ContainsKey(kvp.Key)
                    ? statuses[kvp.Key].ToString()
                    : "Unknown";
                float proxTime = GetProximityTime(kvp.Key);
                lines.Add($"{a.watcherName} is watching {a.targetName} (status: {status}, proximity: {proxTime:F0}s)");
            }
            return string.Join("\n", lines);
        }

        private Transform FindPlayerTransform(int connectionId)
        {
            foreach (var identity in NetworkServer.spawned.Values)
            {
                if (identity.connectionToClient != null &&
                    identity.connectionToClient.connectionId == connectionId)
                {
                    return identity.transform;
                }
            }
            return null;
        }

        private string GetPlayerName(int connectionId)
        {
            foreach (var ps in FindObjectsOfType<MimicPlayerState>())
            {
                if (ps.connectionToClient != null &&
                    ps.connectionToClient.connectionId == connectionId)
                    return ps.DisplayName;
            }
            return $"Subject-{connectionId}";
        }

        private bool IsLocalWatcher(int watcherConnectionId)
        {
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer == null) return false;
            foreach (var ps in FindObjectsOfType<MimicPlayerState>())
            {
                if (ps.netId == localPlayer.netId &&
                    ps.connectionToClient != null &&
                    ps.connectionToClient.connectionId == watcherConnectionId)
                    return true;
            }
            return false;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
