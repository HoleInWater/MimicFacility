using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;
using MimicFacility.Lore;

namespace MimicFacility.Facility
{
    public class ResearchTerminal : NetworkBehaviour, IInteractable
    {
        [SerializeField] private string zoneTag;
        [SerializeField] private string terminalId;
        [SerializeField] private bool requiresKeycard = true;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private BoxCollider interactionZone;
        [SerializeField] private AudioSource audioSource;

        [Header("Audio")]
        [SerializeField] private AudioClip accessGrantedClip;
        [SerializeField] private AudioClip accessDeniedClip;
        [SerializeField] private AudioClip entryReadClip;

        [Header("Visuals")]
        [SerializeField] private Material staticScreenMaterial;
        [SerializeField] private Material activeScreenMaterial;
        [SerializeField] private MeshRenderer screenRenderer;

        [SyncVar(hook = nameof(OnUnlockedChanged))]
        private bool isUnlocked;

        public string ZoneTag => zoneTag;
        public string TerminalId => terminalId;
        public bool IsUnlocked => isUnlocked;

        public event Action<List<LoreEntry>> OnTerminalAccessed;
        public event Action<string> OnEntryRead;

        public void OnInteract(PlayerCharacter player)
        {
            if (!isServer) return;

            if (!isUnlocked && requiresKeycard)
            {
                if (!PlayerHasKeycard(player))
                {
                    RpcPlaySound(false);
                    return;
                }

                isUnlocked = true;
            }
            else if (!isUnlocked)
            {
                isUnlocked = true;
            }

            RpcPlaySound(true);

            int corruption = GetCorruptionLevel();
            var entries = GetAvailableEntries(corruption);
            RpcTerminalUnlocked(entries.Select(e => e.entryId).ToArray());
        }

        public List<LoreEntry> GetAvailableEntries(int corruptionLevel)
        {
            var loreDb = LoreDatabase.Instance;
            if (loreDb == null) return new List<LoreEntry>();
            return loreDb.GetEntriesForTerminal(terminalId, corruptionLevel);
        }

        [Server]
        public void ReadEntry(string entryId)
        {
            var loreDb = LoreDatabase.Instance;
            if (loreDb == null) return;

            loreDb.MarkEntryAsRead(entryId);
            RpcEntryRead(entryId);
        }

        [ClientRpc]
        private void RpcTerminalUnlocked(string[] entryIds)
        {
            var loreDb = LoreDatabase.Instance;
            if (loreDb == null) return;

            int corruption = GetCorruptionLevel();
            var allEntries = loreDb.GetEntriesForTerminal(terminalId, corruption);
            var entries = allEntries.Where(e => entryIds.Contains(e.entryId)).ToList();

            OnTerminalAccessed?.Invoke(entries);
        }

        [ClientRpc]
        private void RpcEntryRead(string entryId)
        {
            OnEntryRead?.Invoke(entryId);
        }

        [ClientRpc]
        private void RpcPlaySound(bool granted)
        {
            if (audioSource == null) return;
            audioSource.PlayOneShot(granted ? accessGrantedClip : accessDeniedClip);
        }

        private void OnUnlockedChanged(bool oldVal, bool newVal)
        {
            if (screenRenderer != null && newVal)
                screenRenderer.material = activeScreenMaterial;
        }

        private bool PlayerHasKeycard(PlayerCharacter player)
        {
            var state = player.PlayerState;
            if (state == null) return false;

            if (state.PrimaryGear != null)
            {
                var gear = state.PrimaryGear.GetComponent<Gear.GearItem>();
                if (gear != null && gear.GearName == "Keycard") return true;
            }

            if (state.SecondaryGear != null)
            {
                var gear = state.SecondaryGear.GetComponent<Gear.GearItem>();
                if (gear != null && gear.GearName == "Keycard") return true;
            }

            return false;
        }

        private int GetCorruptionLevel()
        {
            var tracker = FindObjectOfType<AI.Persistence.CorruptionTracker>();
            return tracker != null ? tracker.CorruptionIndex : 0;
        }

        public void ServerInteract(PlayerCharacter player)
        {
            OnInteract(player);
        }
    }
}
