using UnityEngine;
using MimicFacility.Core;

namespace MimicFacility.Characters
{
    [PlayerComponent("Movement", order: 10)]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 3.5f;
        public float sprintSpeed = 6f;
        public float rotationSpeed = 10f;
        public float mouseSensitivity = 200f;

        [Header("Jumping & Gravity")]
        public float jumpVelocity = 6f;
        public float fallMultiplier = 3f;
        public float lowJumpMultiplier = 2f;
        public float jumpBufferTime = 0.2f;
        public LayerMask groundLayer = ~0;

        [Header("Stamina")]
        public float sprintDrainRate = 6f;
        public float jumpStaminaCost = 15f;

        [Header("Camera")]
        public Camera playerCamera;
        public float verticalClamp = 85f;

        [Header("Smoothness")]
        public float acceleration = 50f;

        [Header("External Modifiers")]
        public float externalSpeedMultiplier = 1f;

        private Rigidbody rb;
        private bool isGrounded;
        private float jumpBufferCounter;
        private bool jumpRequested;
        private float xRotation;
        private float yRotation;
        private Vector3 moveDirection;
        private float currentActiveSpeed;
        private PlayerStamina staminaSystem;
        private SixthSense sixthSense;
        private bool spaceHeld;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.isKinematic = false;
            rb.sleepThreshold = 0f;

            if (GetComponent<PlayerAutoSetup>() == null)
            {
                rb.linearDamping = 0.5f;
                rb.angularDamping = 5f;
            }
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            staminaSystem = GetComponent<PlayerStamina>();
            sixthSense = GetComponent<SixthSense>();

            if (SettingsManager.Instance != null)
                mouseSensitivity = SettingsManager.Instance.Settings.mouseSensitivity * 200f;
        }

        void Update()
        {
            isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.2f, groundLayer);

            spaceHeld = Input.GetKey(KeyCode.Space);

            if (Input.GetKeyDown(KeyCode.Space))
                jumpBufferCounter = jumpBufferTime;
            else
                jumpBufferCounter -= Time.deltaTime;

            if (jumpBufferCounter > 0f && isGrounded)
            {
                if (staminaSystem == null || staminaSystem.currentStamina >= jumpStaminaCost)
                {
                    jumpRequested = true;
                    jumpBufferCounter = 0f;

                    if (staminaSystem != null)
                        staminaSystem.UseStamina(jumpStaminaCost);
                }
            }

            HandleCamera();
        }

        void FixedUpdate()
        {
            Vector3 horizontalVelocity = moveDirection * currentActiveSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);

            HandleMovement();
            HandleJump();
            HandleGravity();
        }

        void HandleMovement()
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            float speed = moveSpeed;
            bool isMoving = Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f;
            bool isTryingToSprint = Input.GetKey(KeyCode.LeftShift) && isMoving;
            bool hasStamina = staminaSystem == null || staminaSystem.currentStamina > 1f;
            bool sixthSenseAllows = sixthSense == null || sixthSense.CanSprint;

            if (isTryingToSprint && hasStamina && sixthSenseAllows)
            {
                speed = sprintSpeed;
                if (staminaSystem != null)
                    staminaSystem.DrainStamina(sprintDrainRate);
            }

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            moveDirection = (forward * z + right * x).normalized;

            bool staminaDepleted = staminaSystem != null && staminaSystem.currentStamina <= 0f;
            currentActiveSpeed = staminaDepleted ? 0f : speed * externalSpeedMultiplier;

            if (staminaSystem != null && staminaSystem.IsExhausted)
                currentActiveSpeed *= staminaSystem.ExhaustionPenalty;
        }

        void HandleJump()
        {
            if (!jumpRequested) return;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);
            jumpRequested = false;
        }

        void HandleGravity()
        {
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
            else if (rb.linearVelocity.y > 0 && !spaceHeld)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
            }
        }

        void HandleCamera()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

            if (playerCamera != null)
                playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        public bool IsGrounded() => isGrounded;
        public bool IsSprinting() => Input.GetKey(KeyCode.LeftShift) && isGrounded && moveDirection.magnitude > 0.1f;
        public float GetCurrentSpeed() => rb != null ? rb.linearVelocity.magnitude : 0f;
        public Vector3 GetVelocity() => rb != null ? rb.linearVelocity : Vector3.zero;
        public Vector3 GetInputDirection() => moveDirection;
    }
}
