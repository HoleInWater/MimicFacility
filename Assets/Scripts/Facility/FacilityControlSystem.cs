using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MimicFacility.Facility
{
    public enum EFacilityAction
    {
        LockDoor,
        UnlockDoor,
        KillLights,
        RestoreLights,
        ActivateSporeVent,
        DeactivateSporeVent,
        LockdownZone,
        RestoreZone
    }

    [Serializable]
    public class FacilityCommand
    {
        public EFacilityAction Action;
        public string TargetZoneTag;
        public float Duration;
        public float Delay;

        public FacilityCommand(EFacilityAction action, string zoneTag, float duration = 0f, float delay = 0f)
        {
            Action = action;
            TargetZoneTag = zoneTag;
            Duration = duration;
            Delay = delay;
        }
    }

    public class FacilityControlSystem : MonoBehaviour
    {
        private readonly List<FacilityDoor> _doors = new List<FacilityDoor>();
        private readonly List<FacilityLight> _lights = new List<FacilityLight>();
        private readonly List<SporeVent> _vents = new List<SporeVent>();
        private readonly Queue<FacilityCommand> _commandQueue = new Queue<FacilityCommand>();
        private bool _processingQueue;

        private void Start()
        {
            _doors.AddRange(FindObjectsOfType<FacilityDoor>());
            _lights.AddRange(FindObjectsOfType<FacilityLight>());
            _vents.AddRange(FindObjectsOfType<SporeVent>());
        }

        public void ExecuteCommand(FacilityCommand cmd)
        {
            if (cmd.Delay > 0f)
            {
                StartCoroutine(DelayedExecute(cmd));
                return;
            }

            DispatchCommand(cmd);

            if (cmd.Duration > 0f)
                StartCoroutine(RevertAfterDuration(cmd));
        }

        private IEnumerator DelayedExecute(FacilityCommand cmd)
        {
            yield return new WaitForSeconds(cmd.Delay);
            DispatchCommand(cmd);

            if (cmd.Duration > 0f)
                yield return StartCoroutine(RevertAfterDuration(cmd));
        }

        private void DispatchCommand(FacilityCommand cmd)
        {
            switch (cmd.Action)
            {
                case EFacilityAction.LockDoor:
                    foreach (var door in GetControllablesInZone<FacilityDoor>(cmd.TargetZoneTag))
                        door.Lock();
                    break;
                case EFacilityAction.UnlockDoor:
                    foreach (var door in GetControllablesInZone<FacilityDoor>(cmd.TargetZoneTag))
                        door.Unlock();
                    break;
                case EFacilityAction.KillLights:
                    foreach (var light in GetControllablesInZone<FacilityLight>(cmd.TargetZoneTag))
                        light.TurnOff();
                    break;
                case EFacilityAction.RestoreLights:
                    foreach (var light in GetControllablesInZone<FacilityLight>(cmd.TargetZoneTag))
                        light.TurnOn();
                    break;
                case EFacilityAction.ActivateSporeVent:
                    foreach (var vent in GetControllablesInZone<SporeVent>(cmd.TargetZoneTag))
                        vent.Activate();
                    break;
                case EFacilityAction.DeactivateSporeVent:
                    foreach (var vent in GetControllablesInZone<SporeVent>(cmd.TargetZoneTag))
                        vent.Deactivate();
                    break;
                case EFacilityAction.LockdownZone:
                    LockdownZone(cmd.TargetZoneTag);
                    break;
                case EFacilityAction.RestoreZone:
                    RestoreZone(cmd.TargetZoneTag);
                    break;
            }
        }

        private IEnumerator RevertAfterDuration(FacilityCommand cmd)
        {
            yield return new WaitForSeconds(cmd.Duration);

            EFacilityAction revert = cmd.Action switch
            {
                EFacilityAction.LockDoor => EFacilityAction.UnlockDoor,
                EFacilityAction.KillLights => EFacilityAction.RestoreLights,
                EFacilityAction.ActivateSporeVent => EFacilityAction.DeactivateSporeVent,
                EFacilityAction.LockdownZone => EFacilityAction.RestoreZone,
                _ => cmd.Action
            };

            DispatchCommand(new FacilityCommand(revert, cmd.TargetZoneTag));
        }

        public void LockdownZone(string zoneTag)
        {
            foreach (var door in GetControllablesInZone<FacilityDoor>(zoneTag))
                door.Lock();
            foreach (var light in GetControllablesInZone<FacilityLight>(zoneTag))
                light.TurnOff();
            foreach (var vent in GetControllablesInZone<SporeVent>(zoneTag))
                vent.Activate();
        }

        public void RestoreZone(string zoneTag)
        {
            foreach (var door in GetControllablesInZone<FacilityDoor>(zoneTag))
                door.Unlock();
            foreach (var light in GetControllablesInZone<FacilityLight>(zoneTag))
                light.TurnOn();
            foreach (var vent in GetControllablesInZone<SporeVent>(zoneTag))
                vent.Deactivate();
        }

        public List<T> GetControllablesInZone<T>(string zoneTag) where T : Component
        {
            if (typeof(T) == typeof(FacilityDoor))
                return _doors.Where(d => d.ZoneTag == zoneTag).Cast<T>().ToList();
            if (typeof(T) == typeof(FacilityLight))
                return _lights.Where(l => l.ZoneTag == zoneTag).Cast<T>().ToList();
            if (typeof(T) == typeof(SporeVent))
                return _vents.Where(v => v.ZoneTag == zoneTag).Cast<T>().ToList();
            return new List<T>();
        }

        public void IsolatePlayers(Transform player1, Transform player2)
        {
            Vector3 midpoint = (player1.position + player2.position) * 0.5f;
            var sorted = _doors.OrderBy(d => Vector3.Distance(d.transform.position, midpoint)).ToList();
            int count = Mathf.Min(sorted.Count, 4);
            for (int i = 0; i < count; i++)
                sorted[i].Lock();
        }

        public void EmotionalManipulation(float panicFrequency, string playerZone)
        {
            if (panicFrequency > 0.8f)
            {
                foreach (var vent in GetControllablesInZone<SporeVent>(playerZone))
                    vent.Activate();
            }

            if (panicFrequency > 0.6f)
            {
                foreach (var light in GetControllablesInZone<FacilityLight>(playerZone))
                    light.Flicker(3f);
            }
        }

        public void QueueCommand(FacilityCommand cmd)
        {
            _commandQueue.Enqueue(cmd);
            if (!_processingQueue)
                StartCoroutine(ProcessCommandQueue());
        }

        private IEnumerator ProcessCommandQueue()
        {
            _processingQueue = true;
            while (_commandQueue.Count > 0)
            {
                var cmd = _commandQueue.Dequeue();
                ExecuteCommand(cmd);
                yield return null;
            }
            _processingQueue = false;
        }
    }
}
