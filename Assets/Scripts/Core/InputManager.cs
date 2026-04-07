using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MimicFacility.Core
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public event Action<Vector2> OnMoveInput;
        public event Action<Vector2> OnLookInput;
        public event Action OnInteractPressed;
        public event Action OnUseGearPressed;
        public event Action OnToggleFlashlightPressed;
        public event Action OnPushToTalkStart;
        public event Action OnPushToTalkEnd;
        public event Action OnTrustChallengePressed;
        public event Action OnPausePressed;
        public event Action OnScoreboardPressed;
        public event Action OnScoreboardReleased;

        [SerializeField] private InputActionAsset inputActions;

        [Header("Sensitivity")]
        [SerializeField] private float mouseSensitivity = 1f;
        [SerializeField] private bool invertY;
        [SerializeField] private bool toggleCrouch;

        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set => mouseSensitivity = Mathf.Clamp(value, 0.1f, 10f);
        }

        public bool InvertY
        {
            get => invertY;
            set => invertY = value;
        }

        public bool ToggleCrouch
        {
            get => toggleCrouch;
            set => toggleCrouch = value;
        }

        private InputActionMap _gameplayMap;
        private InputActionMap _uiMap;
        private InputActionMap _spectatorMap;

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _interactAction;
        private InputAction _useGearAction;
        private InputAction _toggleFlashlightAction;
        private InputAction _pushToTalkAction;
        private InputAction _trustChallengeAction;
        private InputAction _pauseAction;
        private InputAction _scoreboardAction;

        private InputActionRebindingExtensions.RebindingOperation _activeRebind;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (inputActions != null)
                SetupActions();

            LoadBindings();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                UnsubscribeActions();
                _activeRebind?.Dispose();
                Instance = null;
            }
        }

        private void SetupActions()
        {
            _gameplayMap = inputActions.FindActionMap("Gameplay");
            _uiMap = inputActions.FindActionMap("UI");
            _spectatorMap = inputActions.FindActionMap("Spectator");

            if (_gameplayMap == null) return;

            _moveAction = _gameplayMap.FindAction("Move");
            _lookAction = _gameplayMap.FindAction("Look");
            _interactAction = _gameplayMap.FindAction("Interact");
            _useGearAction = _gameplayMap.FindAction("UseGear");
            _toggleFlashlightAction = _gameplayMap.FindAction("ToggleFlashlight");
            _pushToTalkAction = _gameplayMap.FindAction("PushToTalk");
            _trustChallengeAction = _gameplayMap.FindAction("TrustChallenge");
            _pauseAction = _gameplayMap.FindAction("Pause");
            _scoreboardAction = _gameplayMap.FindAction("Scoreboard");

            SubscribeActions();
        }

        private void SubscribeActions()
        {
            if (_interactAction != null)
                _interactAction.performed += HandleInteract;
            if (_useGearAction != null)
                _useGearAction.performed += HandleUseGear;
            if (_toggleFlashlightAction != null)
                _toggleFlashlightAction.performed += HandleToggleFlashlight;
            if (_trustChallengeAction != null)
                _trustChallengeAction.performed += HandleTrustChallenge;
            if (_pauseAction != null)
                _pauseAction.performed += HandlePause;

            if (_pushToTalkAction != null)
            {
                _pushToTalkAction.started += HandlePushToTalkStart;
                _pushToTalkAction.canceled += HandlePushToTalkEnd;
            }

            if (_scoreboardAction != null)
            {
                _scoreboardAction.started += HandleScoreboardPressed;
                _scoreboardAction.canceled += HandleScoreboardReleased;
            }
        }

        private void UnsubscribeActions()
        {
            if (_interactAction != null)
                _interactAction.performed -= HandleInteract;
            if (_useGearAction != null)
                _useGearAction.performed -= HandleUseGear;
            if (_toggleFlashlightAction != null)
                _toggleFlashlightAction.performed -= HandleToggleFlashlight;
            if (_trustChallengeAction != null)
                _trustChallengeAction.performed -= HandleTrustChallenge;
            if (_pauseAction != null)
                _pauseAction.performed -= HandlePause;

            if (_pushToTalkAction != null)
            {
                _pushToTalkAction.started -= HandlePushToTalkStart;
                _pushToTalkAction.canceled -= HandlePushToTalkEnd;
            }

            if (_scoreboardAction != null)
            {
                _scoreboardAction.started -= HandleScoreboardPressed;
                _scoreboardAction.canceled -= HandleScoreboardReleased;
            }
        }

        private void Update()
        {
            if (_moveAction != null)
                OnMoveInput?.Invoke(_moveAction.ReadValue<Vector2>());

            if (_lookAction != null)
            {
                Vector2 look = _lookAction.ReadValue<Vector2>() * mouseSensitivity;
                if (invertY)
                    look.y = -look.y;
                OnLookInput?.Invoke(look);
            }
        }

        private void HandleInteract(InputAction.CallbackContext ctx) => OnInteractPressed?.Invoke();
        private void HandleUseGear(InputAction.CallbackContext ctx) => OnUseGearPressed?.Invoke();
        private void HandleToggleFlashlight(InputAction.CallbackContext ctx) => OnToggleFlashlightPressed?.Invoke();
        private void HandleTrustChallenge(InputAction.CallbackContext ctx) => OnTrustChallengePressed?.Invoke();
        private void HandlePause(InputAction.CallbackContext ctx) => OnPausePressed?.Invoke();
        private void HandlePushToTalkStart(InputAction.CallbackContext ctx) => OnPushToTalkStart?.Invoke();
        private void HandlePushToTalkEnd(InputAction.CallbackContext ctx) => OnPushToTalkEnd?.Invoke();
        private void HandleScoreboardPressed(InputAction.CallbackContext ctx) => OnScoreboardPressed?.Invoke();
        private void HandleScoreboardReleased(InputAction.CallbackContext ctx) => OnScoreboardReleased?.Invoke();

        public void EnableGameplayInput()
        {
            _uiMap?.Disable();
            _spectatorMap?.Disable();
            _gameplayMap?.Enable();
        }

        public void EnableUIInput()
        {
            _gameplayMap?.Disable();
            _spectatorMap?.Disable();
            _uiMap?.Enable();
        }

        public void EnableSpectatorInput()
        {
            _gameplayMap?.Disable();
            _uiMap?.Disable();
            _spectatorMap?.Enable();
        }

        public void DisableAllInput()
        {
            _gameplayMap?.Disable();
            _uiMap?.Disable();
            _spectatorMap?.Disable();
        }

        public void StartRebind(string actionName, Action onComplete = null)
        {
            if (inputActions == null) return;

            var action = inputActions.FindAction(actionName);
            if (action == null) return;

            action.Disable();
            _activeRebind?.Dispose();

            _activeRebind = action.PerformInteractiveRebinding()
                .WithControlsExcluding("Mouse")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation =>
                {
                    operation.Dispose();
                    _activeRebind = null;
                    action.Enable();
                    SaveBindings();
                    onComplete?.Invoke();
                })
                .OnCancel(operation =>
                {
                    operation.Dispose();
                    _activeRebind = null;
                    action.Enable();
                })
                .Start();
        }

        public void CancelRebind()
        {
            _activeRebind?.Cancel();
        }

        public void SaveBindings()
        {
            if (inputActions == null) return;
            string overrides = inputActions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString("InputBindings", overrides);
            PlayerPrefs.Save();
        }

        public void LoadBindings()
        {
            if (inputActions == null) return;
            if (PlayerPrefs.HasKey("InputBindings"))
            {
                string overrides = PlayerPrefs.GetString("InputBindings");
                inputActions.LoadBindingOverridesFromJson(overrides);
            }
        }

        public void ResetBindings()
        {
            if (inputActions == null) return;
            inputActions.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey("InputBindings");
            PlayerPrefs.Save();
        }
    }
}
