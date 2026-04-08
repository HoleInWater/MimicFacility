using System;
using System.Collections;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;

namespace MimicFacility.Gear
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class GearItem : NetworkBehaviour, IInteractable
    {
        [Header("Gear Visuals")]
        [SerializeField] private MeshRenderer gearMesh;
        [SerializeField] private Collider pickupCollider;

        [SyncVar(hook = nameof(OnPickedUpChanged))]
        private bool isPickedUp;
        public bool IsPickedUp => isPickedUp;

        [SyncVar]
        private int usesRemaining = -1;
        protected int UsesRemaining { get => usesRemaining; set => usesRemaining = value; }

        [SyncVar]
        private uint ownerPlayerId;
        public uint OwnerPlayerId => ownerPlayerId;

        public abstract string GearName { get; }

        public abstract void OnUse(PlayerCharacter player);
        public abstract void OnStopUse(PlayerCharacter player);

        public virtual void OnPickedUp(PlayerCharacter player)
        {
            if (!isServer) return;

            isPickedUp = true;
            ownerPlayerId = player.netId;

            transform.SetParent(player.transform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            RpcPickedUp(player.netId);
        }

        public virtual void OnDropped(PlayerCharacter player)
        {
            if (!isServer) return;

            Vector3 dropPosition = player.transform.position + player.transform.forward * 1f;
            dropPosition.y = player.transform.position.y;

            transform.SetParent(null);
            transform.position = dropPosition;

            isPickedUp = false;
            ownerPlayerId = 0;

            RpcDropped(dropPosition);
        }

        protected bool ConsumeUse()
        {
            if (usesRemaining == -1) return true;
            if (usesRemaining <= 0) return false;

            usesRemaining--;
            return true;
        }

        public void OnInteract(PlayerCharacter player)
        {
            if (isPickedUp) return;
            player.EquipGear(GetComponent<Characters.GearBase>());
            OnPickedUp(player);
        }

        [ClientRpc]
        private void RpcPickedUp(uint ownerNetId)
        {
            SetVisuals(false);
        }

        [ClientRpc]
        private void RpcDropped(Vector3 position)
        {
            transform.position = position;
            SetVisuals(true);
        }

        private void SetVisuals(bool visible)
        {
            if (gearMesh != null) gearMesh.enabled = visible;
            if (pickupCollider != null) pickupCollider.enabled = visible;
        }

        private void OnPickedUpChanged(bool oldVal, bool newVal)
        {
            SetVisuals(!newVal);
        }

        protected PlayerCharacter GetOwnerPlayer()
        {
            if (ownerPlayerId == 0) return null;

            if (NetworkServer.spawned.TryGetValue(ownerPlayerId, out var identity))
                return identity.GetComponent<PlayerCharacter>();

            if (NetworkClient.spawned.TryGetValue(ownerPlayerId, out identity))
                return identity.GetComponent<PlayerCharacter>();

            return null;
        }
    }
}
