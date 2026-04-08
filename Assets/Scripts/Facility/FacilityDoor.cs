using System.Collections;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;

namespace MimicFacility.Facility
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(AudioSource))]
    public class FacilityDoor : NetworkBehaviour, IInteractable
    {
        [SerializeField] private string zoneTag;
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float openSpeed = 2f;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private BoxCollider blockingVolume;
        [SerializeField] private AudioSource audioSource;

        [Header("Audio")]
        [SerializeField] private AudioClip lockSound;
        [SerializeField] private AudioClip unlockSound;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip rattleSound;

        [Header("Materials")]
        [SerializeField] private Material lockedMaterial;
        [SerializeField] private Material unlockedMaterial;
        [SerializeField] private MeshRenderer indicatorRenderer;

        [SyncVar(hook = nameof(OnLockStateChanged))]
        private bool isLocked;

        [SyncVar(hook = nameof(OnOpenStateChanged))]
        private bool isOpen;

        public string ZoneTag => zoneTag;
        public bool IsLocked => isLocked;
        public bool IsOpen => isOpen;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private Coroutine _animCoroutine;

        private void Awake()
        {
            if (doorPivot != null)
            {
                _closedRotation = doorPivot.localRotation;
                _openRotation = _closedRotation * Quaternion.Euler(0f, openAngle, 0f);
            }
        }

        public void OnInteract(PlayerCharacter player)
        {
            if (isLocked)
            {
                PlaySound(rattleSound);
                return;
            }

            if (isServer)
                ToggleOpen();
        }

        [Server]
        public void Lock()
        {
            if (isLocked) return;
            isLocked = true;

            if (isOpen)
                SetOpen(false);

            RpcPlaySound(true);
        }

        [Server]
        public void Unlock()
        {
            if (!isLocked) return;
            isLocked = false;
            RpcPlaySound(false);
        }

        [Server]
        private void ToggleOpen()
        {
            SetOpen(!isOpen);
        }

        [Server]
        private void SetOpen(bool open)
        {
            isOpen = open;

            if (blockingVolume != null)
                blockingVolume.enabled = !open;
        }

        [ClientRpc]
        private void RpcPlaySound(bool locked)
        {
            PlaySound(locked ? lockSound : unlockSound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        private void OnLockStateChanged(bool oldVal, bool newVal)
        {
            if (indicatorRenderer != null)
                indicatorRenderer.material = newVal ? lockedMaterial : unlockedMaterial;
        }

        private void OnOpenStateChanged(bool oldVal, bool newVal)
        {
            PlaySound(newVal ? openSound : closeSound);

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateDoor(newVal));
        }

        private IEnumerator AnimateDoor(bool opening)
        {
            if (doorPivot == null) yield break;

            Quaternion start = doorPivot.localRotation;
            Quaternion target = opening ? _openRotation : _closedRotation;
            float elapsed = 0f;

            while (elapsed < openSpeed)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openSpeed);
                doorPivot.localRotation = Quaternion.Lerp(start, target, t);
                yield return null;
            }

            doorPivot.localRotation = target;
            _animCoroutine = null;
        }

        public void ServerInteract(PlayerCharacter player)
        {
            OnInteract(player);
        }
    }
}
