using System;
using UnityEngine;

namespace MimicFacility.Core
{
    public class FallbackInputManager : MonoBehaviour
    {
        public static FallbackInputManager Instance { get; private set; }

        public event Action<Vector2> OnMoveInput;
        public event Action<Vector2> OnLookInput;
        public event Action OnInteractPressed;
        public event Action OnUseGearPressed;
        public event Action OnToggleFlashlightPressed;
        public event Action OnPushToTalkStart;
        public event Action OnPushToTalkEnd;
        public event Action OnPausePressed;

        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool invertY;

        private bool pushToTalkHeld;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
                OnMoveInput?.Invoke(new Vector2(h, v));

            float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertY ? -1f : 1f);
            if (Mathf.Abs(mx) > 0.001f || Mathf.Abs(my) > 0.001f)
                OnLookInput?.Invoke(new Vector2(mx, my));

            if (Input.GetKeyDown(KeyCode.E))
                OnInteractPressed?.Invoke();

            if (Input.GetMouseButtonDown(0))
                OnUseGearPressed?.Invoke();

            if (Input.GetKeyDown(KeyCode.F))
                OnToggleFlashlightPressed?.Invoke();

            if (Input.GetKeyDown(KeyCode.V) && !pushToTalkHeld)
            {
                pushToTalkHeld = true;
                OnPushToTalkStart?.Invoke();
            }
            if (Input.GetKeyUp(KeyCode.V) && pushToTalkHeld)
            {
                pushToTalkHeld = false;
                OnPushToTalkEnd?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                OnPausePressed?.Invoke();
        }

        public void SetMouseSensitivity(float value)
        {
            mouseSensitivity = Mathf.Clamp(value, 0.1f, 10f);
        }

        public void SetInvertY(bool value)
        {
            invertY = value;
        }
    }
}
