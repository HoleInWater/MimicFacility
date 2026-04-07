using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;
using MimicFacility.Facility;

namespace MimicFacility.Entities
{
    public class Warden : NetworkBehaviour
    {
        [Header("Zone Control")]
        [SerializeField] private string controlledZoneTag;
        [SerializeField] private float actionInterval = 8f;
        [SerializeField] private float lockDuration = 15f;
        [SerializeField] private float lightsOffDuration = 10f;
        [SerializeField] private float ventActiveDuration = 8f;

        [Header("Awareness")]
        [SerializeField] private float detectionRange = 20f;
        [SerializeField] private float aggressionRampRate = 0.1f;
        [SerializeField] private float maxAggression = 1f;

        [Header("Audio")]
        [SerializeField] private AudioSource wardenAudio;
        [SerializeField] private AudioClip ambientHum;
        [SerializeField] private AudioClip lockdownSound;
        [SerializeField] private AudioClip ventSound;

        [SyncVar] private float aggression;
        [SyncVar] private bool isInLockdown;

        private List<FacilityDoor> zoneDoors = new List<FacilityDoor>();
        private List<FacilityLight> zoneLights = new List<FacilityLight>();
        private List<SporeVent> zoneVents = new List<SporeVent>();
        private float nextActionTime;
        private int playersInZone;

        public override void OnStartServer()
        {
            foreach (var door in FindObjectsOfType<FacilityDoor>())
            {
                if (door.ZoneTag == controlledZoneTag)
                    zoneDoors.Add(door);
            }
            foreach (var light in FindObjectsOfType<FacilityLight>())
            {
                if (light.ZoneTag == controlledZoneTag)
                    zoneLights.Add(light);
            }
            foreach (var vent in FindObjectsOfType<SporeVent>())
            {
                if (vent.ZoneTag == controlledZoneTag)
                    zoneVents.Add(vent);
            }

            nextActionTime = Time.time + actionInterval;

            if (wardenAudio != null && ambientHum != null)
            {
                wardenAudio.clip = ambientHum;
                wardenAudio.loop = true;
                wardenAudio.Play();
            }
        }

        [Server]
        private void Update()
        {
            if (!isServer) return;

            CountPlayersInZone();
            UpdateAggression();

            if (Time.time >= nextActionTime && playersInZone > 0)
            {
                ChooseAction();
                float adjustedInterval = actionInterval * (1f - aggression * 0.5f);
                nextActionTime = Time.time + Mathf.Max(3f, adjustedInterval);
            }
        }

        [Server]
        private void CountPlayersInZone()
        {
            playersInZone = 0;
            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist <= detectionRange)
                    playersInZone++;
            }
        }

        [Server]
        private void UpdateAggression()
        {
            if (playersInZone > 0)
            {
                aggression = Mathf.Min(maxAggression, aggression + aggressionRampRate * Time.deltaTime);
            }
            else
            {
                aggression = Mathf.Max(0f, aggression - aggressionRampRate * 0.5f * Time.deltaTime);
            }
        }

        [Server]
        private void ChooseAction()
        {
            float roll = Random.value;

            if (roll < 0.3f && !isInLockdown)
            {
                StartCoroutine(LockdownSequence());
            }
            else if (roll < 0.55f)
            {
                StartCoroutine(LightsOutSequence());
            }
            else if (roll < 0.75f)
            {
                StartCoroutine(VentBurstSequence());
            }
            else if (roll < 0.9f)
            {
                FlickerLights();
            }
            else
            {
                if (playersInZone >= 2)
                    IsolatePlayers();
            }
        }

        [Server]
        private IEnumerator LockdownSequence()
        {
            isInLockdown = true;
            RpcPlaySound(0);

            foreach (var door in zoneDoors)
                door.Lock();

            yield return new WaitForSeconds(lockDuration * (1f + aggression));

            foreach (var door in zoneDoors)
                door.Unlock();

            isInLockdown = false;
        }

        [Server]
        private IEnumerator LightsOutSequence()
        {
            foreach (var light in zoneLights)
                light.TurnOff();

            yield return new WaitForSeconds(lightsOffDuration);

            foreach (var light in zoneLights)
                light.TurnOn();
        }

        [Server]
        private IEnumerator VentBurstSequence()
        {
            RpcPlaySound(1);

            foreach (var vent in zoneVents)
                vent.Activate();

            yield return new WaitForSeconds(ventActiveDuration);

            foreach (var vent in zoneVents)
                vent.Deactivate();
        }

        [Server]
        private void FlickerLights()
        {
            float flickerDuration = 2f + aggression * 3f;
            foreach (var light in zoneLights)
                light.Flicker(flickerDuration);
        }

        [Server]
        private void IsolatePlayers()
        {
            var playersInRange = new List<PlayerCharacter>();
            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist <= detectionRange)
                    playersInRange.Add(player);
            }

            if (playersInRange.Count < 2) return;

            FacilityDoor bestDoor = null;
            float bestScore = float.MaxValue;

            Vector3 midpoint = (playersInRange[0].transform.position + playersInRange[1].transform.position) * 0.5f;

            foreach (var door in zoneDoors)
            {
                float distToMid = Vector3.Distance(door.transform.position, midpoint);
                if (distToMid < bestScore)
                {
                    bestScore = distToMid;
                    bestDoor = door;
                }
            }

            if (bestDoor != null)
            {
                bestDoor.Lock();
                RpcPlaySound(0);
            }
        }

        [ClientRpc]
        private void RpcPlaySound(int soundType)
        {
            if (wardenAudio == null) return;

            AudioClip clip = soundType switch
            {
                0 => lockdownSound,
                1 => ventSound,
                _ => null
            };

            if (clip != null)
                wardenAudio.PlayOneShot(clip);
        }

        public float Aggression => aggression;
        public bool IsInLockdown => isInLockdown;
        public string ControlledZone => controlledZoneTag;
    }
}
