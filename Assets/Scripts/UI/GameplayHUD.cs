using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Gear;
using MimicFacility.Gameplay;
using MimicFacility.Effects;
using MimicFacility.Characters;

namespace MimicFacility.UI
{
    public class GameplayHUD : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float directorMessageFadeDuration = 2f;
        [SerializeField] private float damageFlashDuration = 0.4f;
        [SerializeField] private float lowHealthPulseSpeed = 3f;
        [SerializeField] private float crosshairPulseSpeed = 4f;
        [SerializeField] private float crosshairPulseAmount = 0.15f;
        [SerializeField] private float proximityNearRange = 8f;

        private Canvas _canvas;
        private CanvasScaler _scaler;

        // Status bars
        private Image _healthBarFill;
        private Image _healthBarBg;
        private Image _sporeBarFill;
        private Image _sporeBarBg;
        private Image _staminaBarFill;
        private Image _staminaBarBg;

        // Gear display
        private TextMeshProUGUI _gearNameText;
        private TextMeshProUGUI _gearUsesText;

        // Crosshair
        private Image _crosshairDot;
        private Image _crosshairRing;
        private bool _isAimingAtInteractable;

        // Interaction prompt
        private TextMeshProUGUI _interactionPromptText;
        private CanvasGroup _interactionPromptGroup;

        // Director messages
        private TextMeshProUGUI _directorMessageText;
        private CanvasGroup _directorMessageGroup;
        private readonly Queue<(string message, float duration)> _messageQueue = new();
        private Coroutine _directorMessageCoroutine;
        private bool _isShowingDirectorMessage;

        // Top-left info
        private TextMeshProUGUI _roundText;
        private TextMeshProUGUI _entityCountText;
        private TextMeshProUGUI _containedCountText;

        // Verification
        private TextMeshProUGUI _verificationTargetText;
        private Image _proximityIndicator;
        private string _verificationTargetName;

        // Compass
        private TextMeshProUGUI _compassText;

        // Push-to-talk
        private TextMeshProUGUI _pttText;
        private CanvasGroup _pttGroup;

        // Damage flash
        private Image _damageVignette;
        private float _damageFlashTimer;

        // Low health warning
        private Image _lowHealthVignette;

        // Spore hallucination overlay
        private Image _sporeOverlay;

        // Cached references
        private Camera _mainCamera;
        private MimicPlayerState _localPlayerState;
        private PlayerCharacter _localPlayerCharacter;
        private NetworkedGameState _gameState;
        private VerificationSystem _verificationSystem;
        private HallucinationSystem _hallucinationSystem;
        private float _lastRefreshTime;

        private static readonly Color BarBgColor = new(0.12f, 0.12f, 0.12f, 0.7f);
        private static readonly Color HealthColor = new(0.8f, 0.15f, 0.1f, 1f);
        private static readonly Color SporeColor = new(0.2f, 0.75f, 0.15f, 1f);
        private static readonly Color StaminaColor = new(0.9f, 0.8f, 0.1f, 1f);
        private static readonly Color TextColor = new(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color DimTextColor = new(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color ProximityNearColor = new(0.2f, 0.9f, 0.2f, 0.8f);
        private static readonly Color ProximityFarColor = new(0.5f, 0.5f, 0.5f, 0.4f);

        private void Awake()
        {
            BuildCanvas();
            BuildStatusBars();
            BuildGearDisplay();
            BuildCrosshair();
            BuildInteractionPrompt();
            BuildDirectorMessageArea();
            BuildTopLeftInfo();
            BuildVerificationDisplay();
            BuildCompass();
            BuildPushToTalkIndicator();
            BuildDamageFlash();
            BuildLowHealthWarning();
            BuildSporeOverlay();
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnPushToTalkStart += OnPTTStart;
                InputManager.Instance.OnPushToTalkEnd += OnPTTEnd;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnPushToTalkStart -= OnPTTStart;
                InputManager.Instance.OnPushToTalkEnd -= OnPTTEnd;
            }
        }

        private void Update()
        {
            RefreshReferences();
            UpdateStatusBars();
            UpdateGearDisplay();
            UpdateCrosshair();
            UpdateTopLeftInfo();
            UpdateVerificationDisplay();
            UpdateCompass();
            UpdateDamageFlash();
            UpdateLowHealthWarning();
            UpdateSporeOverlay();
        }

        #region Construction

        private void BuildCanvas()
        {
            var canvasObj = new GameObject("GameplayHUD_Canvas");
            canvasObj.transform.SetParent(transform);

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            _scaler = canvasObj.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        private void BuildStatusBars()
        {
            float barWidth = 220f;
            float barHeight = 16f;
            float spacing = 6f;
            float leftMargin = 24f;
            float bottomMargin = 24f;

            (_healthBarBg, _healthBarFill) = CreateBar(
                "HealthBar", HealthColor,
                new Vector2(leftMargin, bottomMargin + (barHeight + spacing) * 2),
                new Vector2(barWidth, barHeight));

            (_sporeBarBg, _sporeBarFill) = CreateBar(
                "SporeBar", SporeColor,
                new Vector2(leftMargin, bottomMargin + (barHeight + spacing)),
                new Vector2(barWidth, barHeight));

            (_staminaBarBg, _staminaBarFill) = CreateBar(
                "StaminaBar", StaminaColor,
                new Vector2(leftMargin, bottomMargin),
                new Vector2(barWidth, barHeight));

            AddBarLabel(_healthBarBg.transform, "HP");
            AddBarLabel(_sporeBarBg.transform, "SPORE");
            AddBarLabel(_staminaBarBg.transform, "STAM");
        }

        private (Image bg, Image fill) CreateBar(string name, Color fillColor, Vector2 position, Vector2 size)
        {
            var bgObj = CreateUIObject(name + "_Bg", _canvas.transform);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.zero;
            bgRect.pivot = new Vector2(0f, 0f);
            bgRect.anchoredPosition = position;
            bgRect.sizeDelta = size;

            var bg = bgObj.AddComponent<Image>();
            bg.color = BarBgColor;

            var fillObj = CreateUIObject(name + "_Fill", bgObj.transform);
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);

            var fill = fillObj.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            return (bg, fill);
        }

        private void AddBarLabel(Transform parent, string label)
        {
            var labelObj = CreateUIObject("Label", parent);
            var rect = labelObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(4f, 0f);
            rect.offsetMax = Vector2.zero;

            var text = labelObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 9f;
            text.color = new Color(1f, 1f, 1f, 0.6f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontStyle = FontStyles.Bold;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
        }

        private void BuildGearDisplay()
        {
            float rightMargin = 24f;
            float bottomMargin = 36f;

            var gearNameObj = CreateUIObject("GearName", _canvas.transform);
            var nameRect = gearNameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(1f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(1f, 0f);
            nameRect.anchoredPosition = new Vector2(-rightMargin, bottomMargin);
            nameRect.sizeDelta = new Vector2(200f, 24f);

            _gearNameText = gearNameObj.AddComponent<TextMeshProUGUI>();
            _gearNameText.fontSize = 16f;
            _gearNameText.color = TextColor;
            _gearNameText.alignment = TextAlignmentOptions.BottomRight;
            _gearNameText.fontStyle = FontStyles.Bold;
            _gearNameText.enableWordWrapping = false;
            _gearNameText.raycastTarget = false;

            var gearUsesObj = CreateUIObject("GearUses", _canvas.transform);
            var usesRect = gearUsesObj.GetComponent<RectTransform>();
            usesRect.anchorMin = new Vector2(1f, 0f);
            usesRect.anchorMax = new Vector2(1f, 0f);
            usesRect.pivot = new Vector2(1f, 0f);
            usesRect.anchoredPosition = new Vector2(-rightMargin, bottomMargin - 20f);
            usesRect.sizeDelta = new Vector2(200f, 18f);

            _gearUsesText = gearUsesObj.AddComponent<TextMeshProUGUI>();
            _gearUsesText.fontSize = 12f;
            _gearUsesText.color = DimTextColor;
            _gearUsesText.alignment = TextAlignmentOptions.BottomRight;
            _gearUsesText.enableWordWrapping = false;
            _gearUsesText.raycastTarget = false;
        }

        private void BuildCrosshair()
        {
            var dotObj = CreateUIObject("CrosshairDot", _canvas.transform);
            var dotRect = dotObj.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(4f, 4f);
            dotRect.anchoredPosition = Vector2.zero;

            _crosshairDot = dotObj.AddComponent<Image>();
            _crosshairDot.color = new Color(1f, 1f, 1f, 0.7f);
            _crosshairDot.raycastTarget = false;

            var ringObj = CreateUIObject("CrosshairRing", _canvas.transform);
            var ringRect = ringObj.GetComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(20f, 20f);
            ringRect.anchoredPosition = Vector2.zero;

            _crosshairRing = ringObj.AddComponent<Image>();
            _crosshairRing.color = new Color(1f, 1f, 1f, 0.35f);
            _crosshairRing.raycastTarget = false;

            // Make the ring hollow by using a filled circle sprite approach:
            // We create a slightly smaller child mask to punch out the center.
            var innerMask = CreateUIObject("CrosshairRingInner", ringObj.transform);
            var innerRect = innerMask.GetComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.5f, 0.5f);
            innerRect.anchorMax = new Vector2(0.5f, 0.5f);
            innerRect.pivot = new Vector2(0.5f, 0.5f);
            innerRect.sizeDelta = new Vector2(16f, 16f);
            innerRect.anchoredPosition = Vector2.zero;

            // Since we can't do ring sprites without assets, use outline approach instead.
            // Remove the inner mask and use Outline component on the ring.
            Destroy(innerMask);
            _crosshairRing.color = new Color(1f, 1f, 1f, 0f);
            var outline = ringObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.35f);
            outline.effectDistance = new Vector2(1f, 1f);
            // Use a thin white image with outline for a ring effect
            _crosshairRing.color = new Color(1f, 1f, 1f, 0.12f);
        }

        private void BuildInteractionPrompt()
        {
            var promptObj = CreateUIObject("InteractionPrompt", _canvas.transform);
            var rect = promptObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -30f);
            rect.sizeDelta = new Vector2(300f, 30f);

            _interactionPromptText = promptObj.AddComponent<TextMeshProUGUI>();
            _interactionPromptText.fontSize = 14f;
            _interactionPromptText.color = TextColor;
            _interactionPromptText.alignment = TextAlignmentOptions.Center;
            _interactionPromptText.enableWordWrapping = false;
            _interactionPromptText.raycastTarget = false;

            _interactionPromptGroup = promptObj.AddComponent<CanvasGroup>();
            _interactionPromptGroup.alpha = 0f;
        }

        private void BuildDirectorMessageArea()
        {
            var msgObj = CreateUIObject("DirectorMessage", _canvas.transform);
            var rect = msgObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -60f);
            rect.sizeDelta = new Vector2(600f, 60f);

            _directorMessageText = msgObj.AddComponent<TextMeshProUGUI>();
            _directorMessageText.fontSize = 16f;
            _directorMessageText.color = new Color(0.9f, 0.85f, 0.7f, 1f);
            _directorMessageText.alignment = TextAlignmentOptions.Center;
            _directorMessageText.fontStyle = FontStyles.Italic;
            _directorMessageText.enableWordWrapping = true;
            _directorMessageText.raycastTarget = false;

            _directorMessageGroup = msgObj.AddComponent<CanvasGroup>();
            _directorMessageGroup.alpha = 0f;
        }

        private void BuildTopLeftInfo()
        {
            float topMargin = 24f;
            float leftMargin = 24f;
            float lineHeight = 22f;

            _roundText = CreateTextElement("RoundText",
                new Vector2(leftMargin, -topMargin),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                18f, FontStyles.Bold);

            _entityCountText = CreateTextElement("EntityCount",
                new Vector2(leftMargin, -(topMargin + lineHeight)),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                14f, FontStyles.Normal);

            _containedCountText = CreateTextElement("ContainedCount",
                new Vector2(leftMargin, -(topMargin + lineHeight * 2)),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                14f, FontStyles.Normal);
        }

        private void BuildVerificationDisplay()
        {
            float topMargin = 24f;
            float rightMargin = 24f;

            var targetObj = CreateUIObject("VerificationTarget", _canvas.transform);
            var rect = targetObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-rightMargin, -topMargin);
            rect.sizeDelta = new Vector2(280f, 24f);

            _verificationTargetText = targetObj.AddComponent<TextMeshProUGUI>();
            _verificationTargetText.fontSize = 14f;
            _verificationTargetText.color = TextColor;
            _verificationTargetText.alignment = TextAlignmentOptions.TopRight;
            _verificationTargetText.fontStyle = FontStyles.Bold;
            _verificationTargetText.enableWordWrapping = false;
            _verificationTargetText.raycastTarget = false;
            _verificationTargetText.text = "";

            var indicatorObj = CreateUIObject("ProximityIndicator", _canvas.transform);
            var indRect = indicatorObj.GetComponent<RectTransform>();
            indRect.anchorMin = new Vector2(1f, 1f);
            indRect.anchorMax = new Vector2(1f, 1f);
            indRect.pivot = new Vector2(1f, 1f);
            indRect.anchoredPosition = new Vector2(-rightMargin, -(topMargin + 28f));
            indRect.sizeDelta = new Vector2(10f, 10f);

            _proximityIndicator = indicatorObj.AddComponent<Image>();
            _proximityIndicator.color = ProximityFarColor;
            _proximityIndicator.raycastTarget = false;
        }

        private void BuildCompass()
        {
            var compassObj = CreateUIObject("Compass", _canvas.transform);
            var rect = compassObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -12f);
            rect.sizeDelta = new Vector2(400f, 24f);

            _compassText = compassObj.AddComponent<TextMeshProUGUI>();
            _compassText.fontSize = 12f;
            _compassText.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            _compassText.alignment = TextAlignmentOptions.Center;
            _compassText.enableWordWrapping = false;
            _compassText.raycastTarget = false;
            _compassText.characterSpacing = 8f;
        }

        private void BuildPushToTalkIndicator()
        {
            var pttObj = CreateUIObject("PTTIndicator", _canvas.transform);
            var rect = pttObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(200f, 24f);

            _pttText = pttObj.AddComponent<TextMeshProUGUI>();
            _pttText.text = "TRANSMITTING";
            _pttText.fontSize = 14f;
            _pttText.color = new Color(1f, 0.3f, 0.3f, 1f);
            _pttText.alignment = TextAlignmentOptions.Center;
            _pttText.fontStyle = FontStyles.Bold;
            _pttText.enableWordWrapping = false;
            _pttText.raycastTarget = false;

            _pttGroup = pttObj.AddComponent<CanvasGroup>();
            _pttGroup.alpha = 0f;
        }

        private void BuildDamageFlash()
        {
            var flashObj = CreateUIObject("DamageVignette", _canvas.transform);
            var rect = flashObj.GetComponent<RectTransform>();
            StretchFull(rect);

            _damageVignette = flashObj.AddComponent<Image>();
            _damageVignette.color = new Color(0.6f, 0f, 0f, 0f);
            _damageVignette.raycastTarget = false;
        }

        private void BuildLowHealthWarning()
        {
            var warnObj = CreateUIObject("LowHealthVignette", _canvas.transform);
            var rect = warnObj.GetComponent<RectTransform>();
            StretchFull(rect);

            _lowHealthVignette = warnObj.AddComponent<Image>();
            _lowHealthVignette.color = new Color(0.5f, 0f, 0f, 0f);
            _lowHealthVignette.raycastTarget = false;
        }

        private void BuildSporeOverlay()
        {
            var sporeObj = CreateUIObject("SporeOverlay", _canvas.transform);
            var rect = sporeObj.GetComponent<RectTransform>();
            StretchFull(rect);

            _sporeOverlay = sporeObj.AddComponent<Image>();
            _sporeOverlay.color = new Color(0.1f, 0.4f, 0.05f, 0f);
            _sporeOverlay.raycastTarget = false;
        }

        #endregion

        #region Update Logic

        private void RefreshReferences()
        {
            if (Time.time - _lastRefreshTime < 1f) return;
            _lastRefreshTime = Time.time;

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_gameState == null)
                _gameState = GameManager.Instance != null ? GameManager.Instance.GameState : null;

            if (_verificationSystem == null)
                _verificationSystem = FindObjectOfType<VerificationSystem>();

            if (_hallucinationSystem == null)
                _hallucinationSystem = FindObjectOfType<HallucinationSystem>();

            if (_localPlayerState == null || _localPlayerCharacter == null)
            {
                var localPlayer = NetworkClient.localPlayer;
                if (localPlayer != null)
                {
                    _localPlayerState = localPlayer.GetComponent<MimicPlayerState>();
                    _localPlayerCharacter = localPlayer.GetComponent<PlayerCharacter>();
                }
            }
        }

        private void UpdateStatusBars()
        {
            if (_localPlayerState != null)
            {
                float healthNorm = _localPlayerState.MaxHealth > 0f
                    ? _localPlayerState.Health / _localPlayerState.MaxHealth
                    : 0f;
                _healthBarFill.fillAmount = Mathf.Clamp01(healthNorm);

                float sporeNorm = _localPlayerState.SporeExposure / 100f;
                _sporeBarFill.fillAmount = Mathf.Clamp01(sporeNorm);
            }

            // Stamina is not yet networked on MimicPlayerState; show full until wired.
            _staminaBarFill.fillAmount = 1f;
        }

        private void UpdateGearDisplay()
        {
            if (_localPlayerCharacter == null)
            {
                _gearNameText.text = "";
                _gearUsesText.text = "";
                return;
            }

            // Read gear from player state sync vars
            GearItem activeGear = null;
            if (_localPlayerState != null && _localPlayerState.PrimaryGear != null)
                activeGear = _localPlayerState.PrimaryGear.GetComponent<GearItem>();

            if (activeGear != null)
            {
                _gearNameText.text = activeGear.GearName;
                // UsesRemaining is protected; read the SyncVar via reflection-free approach.
                // GearItem exposes IsPickedUp but not uses publicly. Show gear name only
                // until a public accessor is added. We display what we can.
                _gearUsesText.text = "";
            }
            else
            {
                _gearNameText.text = "NO GEAR";
                _gearNameText.color = DimTextColor;
                _gearUsesText.text = "";
            }
        }

        private void UpdateCrosshair()
        {
            if (_isAimingAtInteractable)
            {
                float pulse = 1f + Mathf.Sin(Time.time * crosshairPulseSpeed) * crosshairPulseAmount;
                _crosshairRing.transform.localScale = Vector3.one * pulse;
                _crosshairDot.transform.localScale = Vector3.one * pulse;
                _crosshairRing.color = new Color(1f, 1f, 1f, 0.5f);
            }
            else
            {
                _crosshairRing.transform.localScale = Vector3.one;
                _crosshairDot.transform.localScale = Vector3.one;
                _crosshairRing.color = new Color(1f, 1f, 1f, 0.12f);
            }
        }

        private void UpdateTopLeftInfo()
        {
            if (_gameState != null)
            {
                _roundText.text = $"ROUND {_gameState.CurrentRound}";
                _entityCountText.text = $"Entities: {_gameState.ActiveMimicCount}";
                _containedCountText.text = $"Contained: {_gameState.ContainedMimicCount}";
            }
            else
            {
                _roundText.text = "ROUND --";
                _entityCountText.text = "Entities: --";
                _containedCountText.text = "Contained: --";
            }
        }

        private void UpdateVerificationDisplay()
        {
            if (!string.IsNullOrEmpty(_verificationTargetName))
            {
                _verificationTargetText.text = $"WATCHING: {_verificationTargetName}";
                _verificationTargetText.gameObject.SetActive(true);
                _proximityIndicator.gameObject.SetActive(true);

                UpdateProximityIndicator();
            }
            else
            {
                _verificationTargetText.gameObject.SetActive(false);
                _proximityIndicator.gameObject.SetActive(false);
            }
        }

        private void UpdateProximityIndicator()
        {
            if (_verificationSystem == null || _localPlayerState == null) return;

            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer == null) return;

            int localConnId = localPlayer.connectionToClient != null
                ? localPlayer.connectionToClient.connectionId
                : -1;

            // On client side, use netId as fallback identifier
            if (localConnId < 0)
                localConnId = (int)localPlayer.netId;

            var assignment = _verificationSystem.GetAssignment(localConnId);
            if (assignment == null)
            {
                _proximityIndicator.color = ProximityFarColor;
                return;
            }

            // Find target transform
            Transform targetTransform = null;
            foreach (var ps in FindObjectsOfType<MimicPlayerState>())
            {
                if (ps.DisplayName == _verificationTargetName)
                {
                    targetTransform = ps.transform;
                    break;
                }
            }

            if (targetTransform != null && localPlayer.transform != null)
            {
                float distance = Vector3.Distance(localPlayer.transform.position, targetTransform.position);
                _proximityIndicator.color = distance <= proximityNearRange
                    ? ProximityNearColor
                    : ProximityFarColor;
            }
            else
            {
                _proximityIndicator.color = ProximityFarColor;
            }
        }

        private void UpdateCompass()
        {
            if (_mainCamera == null) return;

            float yaw = _mainCamera.transform.eulerAngles.y;
            _compassText.text = BuildCompassString(yaw);
        }

        private static readonly string[] CompassLabels = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        private static readonly float[] CompassBearings = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

        private static string BuildCompassString(float yaw)
        {
            // Normalize to 0-360
            yaw = ((yaw % 360f) + 360f) % 360f;

            const float windowHalf = 90f;
            var parts = new List<string>(5);

            for (int i = 0; i < CompassLabels.Length; i++)
            {
                float delta = Mathf.DeltaAngle(yaw, CompassBearings[i]);
                if (Mathf.Abs(delta) <= windowHalf)
                    parts.Add(CompassLabels[i]);
            }

            // Sort by proximity to center heading
            parts.Sort((a, b) =>
            {
                float dA = Mathf.Abs(Mathf.DeltaAngle(yaw, GetBearing(a)));
                float dB = Mathf.Abs(Mathf.DeltaAngle(yaw, GetBearing(b)));
                return dA.CompareTo(dB);
            });

            return string.Join("  -  ", parts);
        }

        private static float GetBearing(string dir)
        {
            return dir switch
            {
                "N" => 0f,
                "NE" => 45f,
                "E" => 90f,
                "SE" => 135f,
                "S" => 180f,
                "SW" => 225f,
                "W" => 270f,
                "NW" => 315f,
                _ => 0f
            };
        }

        private void UpdateDamageFlash()
        {
            if (_damageFlashTimer > 0f)
            {
                _damageFlashTimer -= Time.deltaTime;
                float alpha = Mathf.Clamp01(_damageFlashTimer / damageFlashDuration) * 0.4f;
                _damageVignette.color = new Color(0.6f, 0f, 0f, alpha);
            }
            else
            {
                _damageVignette.color = new Color(0.6f, 0f, 0f, 0f);
            }
        }

        private void UpdateLowHealthWarning()
        {
            if (_localPlayerState == null || _localPlayerState.MaxHealth <= 0f)
            {
                _lowHealthVignette.color = new Color(0.5f, 0f, 0f, 0f);
                return;
            }

            float healthRatio = _localPlayerState.Health / _localPlayerState.MaxHealth;

            if (healthRatio < 0.25f && healthRatio > 0f)
            {
                float pulse = (Mathf.Sin(Time.time * lowHealthPulseSpeed) + 1f) * 0.5f;
                float intensity = Mathf.Lerp(0.05f, 0.2f, 1f - (healthRatio / 0.25f));
                _lowHealthVignette.color = new Color(0.5f, 0f, 0f, pulse * intensity);
            }
            else
            {
                _lowHealthVignette.color = new Color(0.5f, 0f, 0f, 0f);
            }
        }

        private void UpdateSporeOverlay()
        {
            float exposure = 0f;

            if (_hallucinationSystem != null)
            {
                exposure = _hallucinationSystem.SporeExposure;
            }
            else if (_localPlayerState != null)
            {
                exposure = _localPlayerState.SporeExposure / 100f;
            }

            float alpha = Mathf.Clamp01(exposure) * 0.25f;
            _sporeOverlay.color = new Color(0.1f, 0.4f, 0.05f, alpha);
        }

        #endregion

        #region Public API

        public void ShowInteractionPrompt(string action)
        {
            if (_interactionPromptGroup == null) return;
            _interactionPromptText.text = $"Press E to {action}";
            _interactionPromptGroup.alpha = 1f;
            _isAimingAtInteractable = true;
        }

        public void HideInteractionPrompt()
        {
            if (_interactionPromptGroup == null) return;
            _interactionPromptGroup.alpha = 0f;
            _isAimingAtInteractable = false;
        }

        public void ShowDirectorMessage(string message, float duration = 8f)
        {
            if (_isShowingDirectorMessage)
            {
                _messageQueue.Enqueue((message, duration));
                return;
            }

            if (_directorMessageCoroutine != null)
                StopCoroutine(_directorMessageCoroutine);

            _directorMessageCoroutine = StartCoroutine(DirectorMessageCoroutine(message, duration));
        }

        public void TriggerDamageFlash()
        {
            _damageFlashTimer = damageFlashDuration;
        }

        public void SetVerificationTarget(string targetName)
        {
            _verificationTargetName = targetName;
        }

        public void ClearVerificationTarget()
        {
            _verificationTargetName = null;
        }

        public void UpdateHealth(float normalized)
        {
            if (_healthBarFill != null)
                _healthBarFill.fillAmount = Mathf.Clamp01(normalized);
        }

        public void UpdateSporeExposure(float normalized)
        {
            if (_sporeBarFill != null)
                _sporeBarFill.fillAmount = Mathf.Clamp01(normalized);
        }

        public void UpdateStamina(float normalized)
        {
            if (_staminaBarFill != null)
                _staminaBarFill.fillAmount = Mathf.Clamp01(normalized);
        }

        public void UpdateGameState(NetworkedGameState state)
        {
            if (state == null) return;

            _roundText.text = $"ROUND {state.CurrentRound}";
            _entityCountText.text = $"Entities: {state.ActiveMimicCount}";
            _containedCountText.text = $"Contained: {state.ContainedMimicCount}";
        }

        public void UpdateGearInfo(string gearName, int usesRemaining)
        {
            _gearNameText.text = gearName ?? "NO GEAR";
            _gearNameText.color = gearName != null ? TextColor : DimTextColor;

            if (usesRemaining < 0)
                _gearUsesText.text = "";
            else
                _gearUsesText.text = $"Uses: {usesRemaining}";
        }

        #endregion

        #region Coroutines

        private IEnumerator DirectorMessageCoroutine(string message, float duration)
        {
            _isShowingDirectorMessage = true;
            _directorMessageText.text = message;
            _directorMessageGroup.alpha = 0f;

            // Fade in
            float fadeIn = Mathf.Min(0.5f, duration * 0.1f);
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                _directorMessageGroup.alpha = Mathf.Clamp01(elapsed / fadeIn);
                yield return null;
            }
            _directorMessageGroup.alpha = 1f;

            // Hold
            float holdTime = duration - fadeIn - directorMessageFadeDuration;
            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);

            // Fade out
            elapsed = 0f;
            while (elapsed < directorMessageFadeDuration)
            {
                elapsed += Time.deltaTime;
                _directorMessageGroup.alpha = 1f - Mathf.Clamp01(elapsed / directorMessageFadeDuration);
                yield return null;
            }
            _directorMessageGroup.alpha = 0f;
            _isShowingDirectorMessage = false;

            if (_messageQueue.Count > 0)
            {
                var next = _messageQueue.Dequeue();
                _directorMessageCoroutine = StartCoroutine(DirectorMessageCoroutine(next.message, next.duration));
            }
        }

        #endregion

        #region PTT Callbacks

        private void OnPTTStart()
        {
            if (_pttGroup != null)
                _pttGroup.alpha = 1f;
        }

        private void OnPTTEnd()
        {
            if (_pttGroup != null)
                _pttGroup.alpha = 0f;
        }

        #endregion

        #region Helpers

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private TextMeshProUGUI CreateTextElement(
            string name, Vector2 position,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            float fontSize, FontStyles style)
        {
            var obj = CreateUIObject(name, _canvas.transform);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300f, 24f);

            var text = obj.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontStyle = style;
            text.enableWordWrapping = false;
            text.raycastTarget = false;

            return text;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
