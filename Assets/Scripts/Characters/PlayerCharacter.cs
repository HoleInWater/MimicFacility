using System;
using System.Collections;
using UnityEngine;
using Mirror;
using MimicFacility.Core;
using MimicFacility.AI.Voice;

namespace MimicFacility.Characters
{
    public interface IInteractable
    {
        void OnInteract(PlayerCharacter player);
    }

    public abstract class GearBase : NetworkBehaviour
    {
        public abstract string GearName { get; }
        public abstract void Use(PlayerCharacter wielder);
        public abstract void OnEquipped(PlayerCharacter wielder);
        public abstract void OnUnequipped(PlayerCharacter wielder);
    }

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class PlayerCharacter : NetworkBehaviour
    {
        [Header("Components")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private Light flashlight;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float gravity = -15f;

        [Header("Interaction")]
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private LayerMask interactableLayer;

        [Header("Audio")]
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private AudioSource footstepSource;
        [SerializeField] private float walkStepInterval = 0.5f;
        [SerializeField] private float sprintStepInterval = 0.33f;

        private MimicPlayerState _playerState;
        private Vector3 _velocity;
        private float _cameraPitch;
        private float _footstepTimer;
        private bool _flashlightOn;
        private string _currentZone = "Unknown";
        private bool _isPushToTalking;
        private AudioClip _micClip;
        private string _micDevice;

        [SyncVar] private bool syncFlashlight;

        private GearBase _primaryGear;
        private GearBase _secondaryGear;
        private bool _primarySelected = true;

        public MimicPlayerState PlayerState => _playerState;

        public override void OnStartLocalPlayer()
        {
            playerCamera.enabled = true;
            audioListener.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _playerState = GetComponent<MimicPlayerState>();

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnInteractPressed += HandleInteract;
                InputManager.Instance.OnUseGearPressed += HandleUseGear;
                InputManager.Instance.OnToggleFlashlightPressed += HandleToggleFlashlight;
                InputManager.Instance.OnPushToTalkStart += HandlePushToTalkStart;
                InputManager.Instance.OnPushToTalkEnd += HandlePushToTalkEnd;
            }
        }

        private void Start()
        {
            // Fallback for non-networked testing (no Mirror host)
            var id = GetComponent<Mirror.NetworkIdentity>();
            if (id == null)
            {
                if (playerCamera != null) playerCamera.enabled = true;
                if (audioListener != null) audioListener.enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                _playerState = GetComponent<MimicPlayerState>();
                if (characterController == null) characterController = GetComponent<CharacterController>();
                if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
                if (audioListener == null) audioListener = GetComponentInChildren<AudioListener>();
                if (flashlight == null) flashlight = GetComponentInChildren<Light>();
                if (voiceSource == null) voiceSource = GetComponent<AudioSource>();
                if (footstepSource == null) footstepSource = GetComponent<AudioSource>();
            }
        }

        private void OnDisable()
        {
            if (!IsLocal() || InputManager.Instance == null) return;
            InputManager.Instance.OnInteractPressed -= HandleInteract;
            InputManager.Instance.OnUseGearPressed -= HandleUseGear;
            InputManager.Instance.OnToggleFlashlightPressed -= HandleToggleFlashlight;
            InputManager.Instance.OnPushToTalkStart -= HandlePushToTalkStart;
            InputManager.Instance.OnPushToTalkEnd -= HandlePushToTalkEnd;
        }

        public override void OnStartClient()
        {
            if (!IsLocal())
            {
                if (playerCamera != null) playerCamera.enabled = false;
                if (audioListener != null) audioListener.enabled = false;
            }
        }

        private bool IsLocal()
        {
            var id = GetComponent<Mirror.NetworkIdentity>();
            return id == null || id.isLocalPlayer;
        }

        private void Update()
        {
            if (!IsLocal()) return;

            HandleMovement();
            HandleMouseLook();
            UpdateFootsteps();
            UpdateFlashlight();
        }

        private void HandleMovement()
        {
            if (characterController == null) return;

            bool isGrounded = characterController.isGrounded;
            if (isGrounded && _velocity.y < 0f)
                _velocity.y = -2f;

            Vector2 moveInput = Vector2.zero;
            if (InputManager.Instance != null)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                moveInput = new Vector2(h, v);
            }

            bool sprinting = Input.GetKey(KeyCode.LeftShift);
            float speed = sprinting ? walkSpeed * sprintMultiplier : walkSpeed;

            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            move = Vector3.ClampMagnitude(move, 1f) * speed;
            characterController.Move(move * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
                _velocity.y = jumpForce;

            _velocity.y += gravity * Time.deltaTime;
            characterController.Move(_velocity * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            float sensitivity = 1f;
            if (SettingsManager.Instance != null)
                sensitivity = SettingsManager.Instance.Settings.mouseSensitivity;

            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            bool invertY = SettingsManager.Instance != null && SettingsManager.Instance.Settings.invertY;
            _cameraPitch += invertY ? mouseY : -mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -90f, 90f);

            if (playerCamera == null) return;
            playerCamera.transform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleInteract()
        {
            if (playerCamera == null) return;
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
            {
                var interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.OnInteract(this);
                    var netId = hit.collider.GetComponent<NetworkIdentity>();
                    if (netId != null)
                        CmdInteract(netId);
                }
            }
        }

        private void HandleUseGear()
        {
            GearBase current = _primarySelected ? _primaryGear : _secondaryGear;
            if (current == null) return;

            current.Use(this);
            CmdUseGear(_primarySelected);
        }

        private void HandleToggleFlashlight()
        {
            _flashlightOn = !_flashlightOn;
            if (flashlight != null)
                flashlight.enabled = _flashlightOn;
            CmdToggleFlashlight(_flashlightOn);
        }

        private void HandlePushToTalkStart()
        {
            if (Microphone.devices.Length == 0) return;
            _isPushToTalking = true;
            _micDevice = Microphone.devices[0];
            _micClip = Microphone.Start(_micDevice, false, 30, 16000);
        }

        private void HandlePushToTalkEnd()
        {
            if (!_isPushToTalking) return;
            _isPushToTalking = false;

            int position = Microphone.GetPosition(_micDevice);
            Microphone.End(_micDevice);

            if (_micClip == null || position <= 0) return;

            float[] samples = new float[position];
            _micClip.GetData(samples, 0);

            byte[] audioBytes = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short val = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
                audioBytes[i * 2] = (byte)(val & 0xFF);
                audioBytes[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
            }

            CmdSendVoiceData(audioBytes);
        }

        public void EquipGear(GearBase gear, bool primary = true)
        {
            if (primary)
            {
                _primaryGear?.OnUnequipped(this);
                _primaryGear = gear;
                gear?.OnEquipped(this);
            }
            else
            {
                _secondaryGear?.OnUnequipped(this);
                _secondaryGear = gear;
                gear?.OnEquipped(this);
            }

            if (gear != null)
                CmdEquipGear(gear.netIdentity, primary);
        }

        public void UseCurrentGear()
        {
            HandleUseGear();
        }

        public void DropGear(bool primary = true)
        {
            GearBase gear = primary ? _primaryGear : _secondaryGear;
            if (gear == null) return;

            gear.OnUnequipped(this);
            gear.transform.SetParent(null);
            gear.transform.position = transform.position + transform.forward * 1f;

            if (primary) _primaryGear = null;
            else _secondaryGear = null;
        }

        public string GetCurrentZone() => _currentZone;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsLocal()) return;
            if (other.CompareTag("Zone"))
            {
                _currentZone = other.gameObject.name;
                if (_playerState != null && isServer)
                    _playerState.SetZone(_currentZone);
            }
        }

        private void UpdateFootsteps()
        {
            if (characterController == null || !characterController.isGrounded) return;

            float speed = characterController.velocity.magnitude;
            if (speed < 0.1f) return;

            bool sprinting = Input.GetKey(KeyCode.LeftShift);
            float interval = sprinting ? sprintStepInterval : walkStepInterval;
            _footstepTimer += Time.deltaTime;

            if (_footstepTimer >= interval)
            {
                _footstepTimer = 0f;
                OnFootstep();
            }
        }

        private void OnFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0 || footstepSource == null) return;
            footstepSource.PlayOneShot(footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)]);
        }

        private void UpdateFlashlight()
        {
            if (flashlight != null)
                flashlight.enabled = syncFlashlight;
        }

        [Command]
        private void CmdInteract(NetworkIdentity target)
        {
            if (target == null) return;
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist > interactRange) return;
            target.gameObject.SendMessage("ServerInteract", this, SendMessageOptions.DontRequireReceiver);
        }

        [Command]
        private void CmdUseGear(bool primary)
        {
            if (_playerState != null)
                _playerState.CmdRequestUseGear(primary);
        }

        [Command]
        private void CmdEquipGear(NetworkIdentity gearIdentity, bool primary)
        {
            if (_playerState != null)
                _playerState.CmdRequestEquipGear(gearIdentity, primary);
        }

        [Command]
        private void CmdToggleFlashlight(bool on)
        {
            syncFlashlight = on;
        }

        [Command]
        private void CmdSendVoiceData(byte[] audioData)
        {
            var voiceSystem = FindObjectOfType<VoiceLearningSystem>();
            if (voiceSystem == null) return;

            string playerId = connectionToClient != null
                ? connectionToClient.connectionId.ToString()
                : netId.ToString();

            voiceSystem.RecordPhrase(playerId, Convert.ToBase64String(audioData));
        }
    }
}
