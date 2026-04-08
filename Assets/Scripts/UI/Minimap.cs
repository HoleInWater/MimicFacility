using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;
using MimicFacility.Entities;

namespace MimicFacility.UI
{
    public class Minimap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Layout")]
        [SerializeField] private float mapSize = 200f;
        [SerializeField] private float margin = 16f;
        [SerializeField] private float borderThickness = 3f;

        [Header("Camera")]
        [SerializeField] private float defaultOrthoSize = 40f;
        [SerializeField] private float minOrthoSize = 10f;
        [SerializeField] private float maxOrthoSize = 120f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float cameraHeight = 100f;

        [Header("Dots")]
        [SerializeField] private float localPlayerDotSize = 8f;
        [SerializeField] private float otherPlayerDotSize = 6f;
        [SerializeField] private float entityDotSize = 6f;
        [SerializeField] private float verificationDotSize = 8f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseAmount = 0.2f;

        [Header("Fog of War")]
        [SerializeField] private int fogTextureSize = 256;
        [SerializeField] private float fogRevealRadius = 12f;
        [SerializeField] private float fogWorldExtent = 200f;

        [Header("Colors")]
        [SerializeField] private Color localPlayerColor = new Color(0.2f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color otherPlayerColor = new Color(0.3f, 0.5f, 0.9f, 1f);
        [SerializeField] private Color entityColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color verificationColor = new Color(0.95f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color borderColor = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        [SerializeField] private Color fogColor = new Color(0.02f, 0.02f, 0.04f, 0.92f);

        [Header("Behavior")]
        [SerializeField] private bool northUp;
        [SerializeField] private float referenceRefreshInterval = 0.5f;

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private RectTransform _rootRect;
        private GameObject _rootObject;

        private Camera _minimapCamera;
        private RenderTexture _renderTexture;
        private RawImage _mapImage;

        // Fog of war
        private Texture2D _fogTexture;
        private Color32[] _fogPixels;
        private bool _fogDirty;
        private RawImage _fogOverlay;

        // Dots
        private readonly List<Image> _dotPool = new List<Image>();
        private int _activeDotCount;

        // Zone tooltip
        private TextMeshProUGUI _zoneTooltipText;
        private CanvasGroup _zoneTooltipGroup;

        // Label
        private TextMeshProUGUI _titleText;

        // State
        private bool _isVisible = true;
        private bool _isHovering;
        private float _currentOrthoSize;
        private float _lastRefreshTime;

        // Cached references
        private PlayerCharacter _localPlayerCharacter;
        private MimicPlayerState _localPlayerState;
        private readonly HashSet<uint> _detectedEntityIds = new HashSet<uint>();

        private void Awake()
        {
            _currentOrthoSize = defaultOrthoSize;
            BuildCanvas();
            BuildMinimapRoot();
            BuildMinimapCamera();
            BuildMapDisplay();
            BuildFogOfWar();
            BuildBorder();
            BuildTitle();
            BuildZoneTooltip();
            PreallocateDots(24);
        }

        private void OnDestroy()
        {
            if (_minimapCamera != null)
                Destroy(_minimapCamera.gameObject);

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }

            if (_fogTexture != null)
                Destroy(_fogTexture);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
                ToggleVisibility();

            if (!_isVisible) return;

            RefreshReferences();
            UpdateCamera();
            UpdateFogOfWar();
            UpdateDots();
            HandleZoom();
            UpdateZoneTooltip();
        }

        #region Construction

        private void BuildCanvas()
        {
            var canvasObj = new GameObject("Minimap_Canvas");
            canvasObj.transform.SetParent(transform);

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 90;

            _scaler = canvasObj.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        private void BuildMinimapRoot()
        {
            _rootObject = CreateUIObject("MinimapRoot", _canvas.transform);
            _rootRect = _rootObject.GetComponent<RectTransform>();
            _rootRect.anchorMin = new Vector2(1f, 1f);
            _rootRect.anchorMax = new Vector2(1f, 1f);
            _rootRect.pivot = new Vector2(1f, 1f);
            _rootRect.anchoredPosition = new Vector2(-margin, -margin);
            _rootRect.sizeDelta = new Vector2(mapSize, mapSize);

            // Add EventTrigger support through the IPointer interfaces on this MonoBehaviour.
            // The root needs a graphic to receive pointer events, but the RawImage
            // on the map display handles that below. We add a transparent raycast target here
            // as a fallback.
            var bgImage = _rootObject.AddComponent<Image>();
            bgImage.color = Color.clear;
            bgImage.raycastTarget = true;
        }

        private void BuildMinimapCamera()
        {
            var cameraObj = new GameObject("MinimapCamera");
            cameraObj.transform.SetParent(transform);

            _minimapCamera = cameraObj.AddComponent<Camera>();
            _minimapCamera.orthographic = true;
            _minimapCamera.orthographicSize = _currentOrthoSize;
            _minimapCamera.nearClipPlane = 0.3f;
            _minimapCamera.farClipPlane = cameraHeight + 50f;
            _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            _minimapCamera.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
            _minimapCamera.cullingMask = ~0; // All layers; refine if needed per project layering
            _minimapCamera.depth = -10f;
            _minimapCamera.enabled = true;

            // Disable audio listener if one was auto-added
            var listener = cameraObj.GetComponent<AudioListener>();
            if (listener != null) Destroy(listener);

            _renderTexture = new RenderTexture((int)mapSize * 2, (int)mapSize * 2, 16, RenderTextureFormat.ARGB32);
            _renderTexture.antiAliasing = 2;
            _renderTexture.filterMode = FilterMode.Bilinear;
            _renderTexture.Create();

            _minimapCamera.targetTexture = _renderTexture;

            // Position above origin; will be moved to follow player in UpdateCamera
            cameraObj.transform.position = new Vector3(0f, cameraHeight, 0f);
            cameraObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void BuildMapDisplay()
        {
            var displayObj = CreateUIObject("MapDisplay", _rootObject.transform);
            var displayRect = displayObj.GetComponent<RectTransform>();
            StretchFull(displayRect);

            _mapImage = displayObj.AddComponent<RawImage>();
            _mapImage.texture = _renderTexture;
            _mapImage.raycastTarget = false;

            // Add mask so dots and fog are clipped to the map area
            _rootObject.AddComponent<RectMask2D>();
        }

        private void BuildFogOfWar()
        {
            _fogTexture = new Texture2D(fogTextureSize, fogTextureSize, TextureFormat.RGBA32, false);
            _fogTexture.filterMode = FilterMode.Bilinear;
            _fogTexture.wrapMode = TextureWrapMode.Clamp;

            _fogPixels = new Color32[fogTextureSize * fogTextureSize];
            byte fogR = (byte)(fogColor.r * 255);
            byte fogG = (byte)(fogColor.g * 255);
            byte fogB = (byte)(fogColor.b * 255);
            byte fogA = (byte)(fogColor.a * 255);

            var opaque = new Color32(fogR, fogG, fogB, fogA);
            for (int i = 0; i < _fogPixels.Length; i++)
                _fogPixels[i] = opaque;

            _fogTexture.SetPixels32(_fogPixels);
            _fogTexture.Apply();

            var fogObj = CreateUIObject("FogOverlay", _rootObject.transform);
            var fogRect = fogObj.GetComponent<RectTransform>();
            StretchFull(fogRect);

            _fogOverlay = fogObj.AddComponent<RawImage>();
            _fogOverlay.texture = _fogTexture;
            _fogOverlay.raycastTarget = false;
        }

        private void BuildBorder()
        {
            // Top border
            CreateBorderEdge("BorderTop", _rootObject.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, borderThickness));
            // Bottom border
            CreateBorderEdge("BorderBottom", _rootObject.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, borderThickness));
            // Left border
            CreateBorderEdge("BorderLeft", _rootObject.transform,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(borderThickness, 0f));
            // Right border
            CreateBorderEdge("BorderRight", _rootObject.transform,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(borderThickness, 0f));
        }

        private void CreateBorderEdge(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 sizeDelta)
        {
            var edgeObj = CreateUIObject(name, parent);
            var rect = edgeObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;

            var img = edgeObj.AddComponent<Image>();
            img.color = borderColor;
            img.raycastTarget = false;
        }

        private void BuildTitle()
        {
            var titleObj = CreateUIObject("MapTitle", _rootObject.transform);
            var rect = titleObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 2f);
            rect.sizeDelta = new Vector2(0f, 16f);

            // Background behind title
            var titleBg = titleObj.AddComponent<Image>();
            titleBg.color = borderColor;
            titleBg.raycastTarget = false;

            var labelObj = CreateUIObject("MapTitleLabel", titleObj.transform);
            var labelRect = labelObj.GetComponent<RectTransform>();
            StretchFull(labelRect);

            _titleText = labelObj.AddComponent<TextMeshProUGUI>();
            _titleText.text = "FACILITY MAP";
            _titleText.fontSize = 9f;
            _titleText.color = new Color(0.6f, 0.6f, 0.6f, 0.9f);
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.characterSpacing = 4f;
            _titleText.enableWordWrapping = false;
            _titleText.raycastTarget = false;
        }

        private void BuildZoneTooltip()
        {
            var tooltipObj = CreateUIObject("ZoneTooltip", _rootObject.transform);
            var rect = tooltipObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -4f);
            rect.sizeDelta = new Vector2(0f, 18f);

            var tooltipBg = tooltipObj.AddComponent<Image>();
            tooltipBg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
            tooltipBg.raycastTarget = false;

            var labelObj = CreateUIObject("ZoneTooltipLabel", tooltipObj.transform);
            var labelRect = labelObj.GetComponent<RectTransform>();
            StretchFull(labelRect);

            _zoneTooltipText = labelObj.AddComponent<TextMeshProUGUI>();
            _zoneTooltipText.text = "";
            _zoneTooltipText.fontSize = 10f;
            _zoneTooltipText.color = new Color(0.75f, 0.75f, 0.75f, 0.9f);
            _zoneTooltipText.alignment = TextAlignmentOptions.Center;
            _zoneTooltipText.enableWordWrapping = false;
            _zoneTooltipText.raycastTarget = false;

            _zoneTooltipGroup = tooltipObj.AddComponent<CanvasGroup>();
            _zoneTooltipGroup.alpha = 0f;
        }

        private void PreallocateDots(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var dot = CreateDot();
                dot.gameObject.SetActive(false);
                _dotPool.Add(dot);
            }
        }

        private Image CreateDot()
        {
            var dotObj = CreateUIObject("MapDot", _rootObject.transform);
            var rect = dotObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(localPlayerDotSize, localPlayerDotSize);

            var img = dotObj.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;

            return img;
        }

        #endregion

        #region Update Logic

        private void RefreshReferences()
        {
            if (Time.time - _lastRefreshTime < referenceRefreshInterval) return;
            _lastRefreshTime = Time.time;

            if (_localPlayerCharacter == null || _localPlayerState == null)
            {
                var localPlayer = NetworkClient.localPlayer;
                if (localPlayer != null)
                {
                    _localPlayerCharacter = localPlayer.GetComponent<PlayerCharacter>();
                    _localPlayerState = localPlayer.GetComponent<MimicPlayerState>();
                }
            }

            // Refresh detected entity set from any AudioScanner results nearby.
            // Entities flagged as identified by the scanner are tracked persistently.
            foreach (var mimic in FindObjectsOfType<MimicBase>())
            {
                if (mimic.IsIdentified)
                    _detectedEntityIds.Add(mimic.netId);
            }
        }

        private void UpdateCamera()
        {
            if (_minimapCamera == null) return;

            _minimapCamera.orthographicSize = Mathf.Lerp(
                _minimapCamera.orthographicSize,
                _currentOrthoSize,
                Time.deltaTime * 8f);

            Transform followTarget = _localPlayerCharacter != null
                ? _localPlayerCharacter.transform
                : null;

            if (followTarget != null)
            {
                Vector3 camPos = new Vector3(
                    followTarget.position.x,
                    cameraHeight,
                    followTarget.position.z);
                _minimapCamera.transform.position = camPos;

                if (!northUp)
                {
                    float playerYaw = followTarget.eulerAngles.y;
                    _minimapCamera.transform.rotation = Quaternion.Euler(90f, playerYaw, 0f);
                }
                else
                {
                    _minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }
        }

        private void UpdateFogOfWar()
        {
            if (_localPlayerCharacter == null) return;

            Vector3 playerPos = _localPlayerCharacter.transform.position;

            // Map world position to texture coordinates
            float normX = (playerPos.x + fogWorldExtent) / (fogWorldExtent * 2f);
            float normZ = (playerPos.z + fogWorldExtent) / (fogWorldExtent * 2f);

            int centerX = Mathf.Clamp(Mathf.RoundToInt(normX * fogTextureSize), 0, fogTextureSize - 1);
            int centerY = Mathf.Clamp(Mathf.RoundToInt(normZ * fogTextureSize), 0, fogTextureSize - 1);

            float texelWorldSize = (fogWorldExtent * 2f) / fogTextureSize;
            int radiusPixels = Mathf.CeilToInt(fogRevealRadius / texelWorldSize);

            bool changed = false;

            int xMin = Mathf.Max(0, centerX - radiusPixels);
            int xMax = Mathf.Min(fogTextureSize - 1, centerX + radiusPixels);
            int yMin = Mathf.Max(0, centerY - radiusPixels);
            int yMax = Mathf.Min(fogTextureSize - 1, centerY + radiusPixels);

            float radiusSq = radiusPixels * radiusPixels;

            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    float distSq = dx * dx + dy * dy;

                    if (distSq > radiusSq) continue;

                    int idx = y * fogTextureSize + x;
                    if (_fogPixels[idx].a == 0) continue;

                    // Soften the edge with a gradient
                    float distNorm = Mathf.Sqrt(distSq) / radiusPixels;
                    byte targetAlpha = (byte)(Mathf.Clamp01(distNorm - 0.7f) / 0.3f * fogColor.a * 255f);

                    if (_fogPixels[idx].a > targetAlpha)
                    {
                        _fogPixels[idx].a = targetAlpha;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                _fogTexture.SetPixels32(_fogPixels);
                _fogTexture.Apply();
            }
        }

        private void UpdateDots()
        {
            _activeDotCount = 0;

            if (_localPlayerCharacter == null) return;

            Vector3 cameraPos = _minimapCamera.transform.position;
            float orthoSize = _minimapCamera.orthographicSize;
            float aspect = _minimapCamera.aspect;
            Quaternion cameraRot = _minimapCamera.transform.rotation;

            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

            // Local player (green, pulsing)
            PlaceDot(
                _localPlayerCharacter.transform.position,
                localPlayerColor,
                localPlayerDotSize * pulse,
                cameraPos, orthoSize, aspect, cameraRot);

            // Other players (blue, pulsing)
            foreach (var player in FindObjectsOfType<PlayerCharacter>())
            {
                if (player == _localPlayerCharacter) continue;

                var state = player.GetComponent<MimicPlayerState>();
                if (state != null && !state.IsAlive) continue;

                PlaceDot(
                    player.transform.position,
                    otherPlayerColor,
                    otherPlayerDotSize * pulse,
                    cameraPos, orthoSize, aspect, cameraRot);
            }

            // Entities (red — only if scanner has detected them)
            foreach (var entity in FindObjectsOfType<MimicBase>())
            {
                if (!_detectedEntityIds.Contains(entity.netId)) continue;

                PlaceDot(
                    entity.transform.position,
                    entityColor,
                    entityDotSize,
                    cameraPos, orthoSize, aspect, cameraRot);
            }

            // Verification target (yellow)
            PlaceVerificationTarget(cameraPos, orthoSize, aspect, cameraRot, pulse);

            // Hide unused dots
            for (int i = _activeDotCount; i < _dotPool.Count; i++)
                _dotPool[i].gameObject.SetActive(false);
        }

        private void PlaceVerificationTarget(Vector3 cameraPos, float orthoSize, float aspect,
            Quaternion cameraRot, float pulse)
        {
            var verificationSystem = FindObjectOfType<Gameplay.VerificationSystem>();
            if (verificationSystem == null) return;

            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer == null) return;

            int localConnId = localPlayer.connectionToClient != null
                ? localPlayer.connectionToClient.connectionId
                : (int)localPlayer.netId;

            var assignment = verificationSystem.GetAssignment(localConnId);
            if (assignment == null) return;

            // Find the target player by name
            foreach (var ps in FindObjectsOfType<MimicPlayerState>())
            {
                if (ps.DisplayName == assignment.targetName)
                {
                    PlaceDot(
                        ps.transform.position,
                        verificationColor,
                        verificationDotSize * pulse,
                        cameraPos, orthoSize, aspect, cameraRot);
                    break;
                }
            }
        }

        private void PlaceDot(Vector3 worldPos, Color color, float size,
            Vector3 cameraPos, float orthoSize, float aspect, Quaternion cameraRot)
        {
            // Project world position into the minimap camera's view space
            Vector3 offset = worldPos - cameraPos;
            Vector3 localOffset = Quaternion.Inverse(cameraRot) * offset;

            // In the camera's local space after a 90-degree down look:
            // x maps to screen x, -z maps to screen y (because camera looks down -y in local space)
            float viewX = localOffset.x;
            float viewY = -localOffset.z;

            // Normalize to [-1, 1] range relative to ortho bounds
            float normX = viewX / (orthoSize * aspect);
            float normY = viewY / orthoSize;

            // Cull dots outside the visible area
            if (Mathf.Abs(normX) > 1f || Mathf.Abs(normY) > 1f) return;

            // Convert to pixel position within the map rect
            float pixelX = normX * (mapSize * 0.5f);
            float pixelY = normY * (mapSize * 0.5f);

            Image dot = GetDot();
            dot.gameObject.SetActive(true);
            dot.color = color;

            var rect = dot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(pixelX, pixelY);
        }

        private Image GetDot()
        {
            if (_activeDotCount < _dotPool.Count)
                return _dotPool[_activeDotCount++];

            var newDot = CreateDot();
            _dotPool.Add(newDot);
            _activeDotCount++;
            return newDot;
        }

        private void HandleZoom()
        {
            if (!_isHovering) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.001f) return;

            _currentOrthoSize -= scroll * zoomSpeed * _currentOrthoSize * 0.5f;
            _currentOrthoSize = Mathf.Clamp(_currentOrthoSize, minOrthoSize, maxOrthoSize);
        }

        private void UpdateZoneTooltip()
        {
            if (!_isHovering || _localPlayerCharacter == null)
            {
                if (_zoneTooltipGroup.alpha > 0f)
                    _zoneTooltipGroup.alpha = Mathf.MoveTowards(_zoneTooltipGroup.alpha, 0f, Time.deltaTime * 4f);
                return;
            }

            // Raycast from the minimap camera at the cursor position on the minimap
            // to find zone trigger volumes
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootRect, Input.mousePosition, null, out localPoint);

            // Convert local point to normalized viewport coords for the minimap camera
            float normX = (localPoint.x / mapSize) + 0.5f;
            float normY = (localPoint.y / mapSize) + 0.5f;

            if (normX < 0f || normX > 1f || normY < 0f || normY > 1f)
            {
                _zoneTooltipGroup.alpha = Mathf.MoveTowards(_zoneTooltipGroup.alpha, 0f, Time.deltaTime * 4f);
                return;
            }

            Ray ray = _minimapCamera.ViewportPointToRay(new Vector3(normX, normY, 0f));

            string zoneName = null;
            if (Physics.Raycast(ray, out RaycastHit hit, cameraHeight + 50f))
            {
                string objName = hit.collider.gameObject.name;
                if (objName.StartsWith("Room_") || objName.StartsWith("Zone"))
                    zoneName = FormatZoneName(objName);
            }

            // Also check trigger colliders beneath the ray
            if (zoneName == null)
            {
                var hits = Physics.RaycastAll(ray, cameraHeight + 50f);
                foreach (var h in hits)
                {
                    string objName = h.collider.gameObject.name;
                    if (objName.StartsWith("Room_") || objName.StartsWith("Zone"))
                    {
                        zoneName = FormatZoneName(objName);
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(zoneName))
            {
                _zoneTooltipText.text = zoneName;
                _zoneTooltipGroup.alpha = Mathf.MoveTowards(_zoneTooltipGroup.alpha, 1f, Time.deltaTime * 6f);
            }
            else
            {
                _zoneTooltipGroup.alpha = Mathf.MoveTowards(_zoneTooltipGroup.alpha, 0f, Time.deltaTime * 4f);
            }
        }

        #endregion

        #region Public API

        public void ToggleVisibility()
        {
            _isVisible = !_isVisible;
            _rootObject.SetActive(_isVisible);

            if (_minimapCamera != null)
                _minimapCamera.enabled = _isVisible;
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            _rootObject.SetActive(_isVisible);

            if (_minimapCamera != null)
                _minimapCamera.enabled = _isVisible;
        }

        public void SetNorthUp(bool enabled)
        {
            northUp = enabled;
        }

        public bool IsNorthUp => northUp;

        public void SetMapSize(float size)
        {
            mapSize = Mathf.Max(100f, size);
            if (_rootRect != null)
                _rootRect.sizeDelta = new Vector2(mapSize, mapSize);
        }

        public void RegisterDetectedEntity(uint netId)
        {
            _detectedEntityIds.Add(netId);
        }

        public void ClearDetectedEntities()
        {
            _detectedEntityIds.Clear();
        }

        public void ResetFogOfWar()
        {
            if (_fogPixels == null || _fogTexture == null) return;

            byte fogA = (byte)(fogColor.a * 255);
            byte fogR = (byte)(fogColor.r * 255);
            byte fogG = (byte)(fogColor.g * 255);
            byte fogB = (byte)(fogColor.b * 255);

            var opaque = new Color32(fogR, fogG, fogB, fogA);
            for (int i = 0; i < _fogPixels.Length; i++)
                _fogPixels[i] = opaque;

            _fogTexture.SetPixels32(_fogPixels);
            _fogTexture.Apply();
        }

        #endregion

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
        }

        #endregion

        #region Helpers

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static string FormatZoneName(string rawName)
        {
            // Convert "Room_Medical_Bay" or "Zone_Engineering" to readable form
            string cleaned = rawName;
            if (cleaned.StartsWith("Room_"))
                cleaned = cleaned.Substring(5);
            else if (cleaned.StartsWith("Zone_"))
                cleaned = cleaned.Substring(5);
            else if (cleaned.StartsWith("Zone"))
                cleaned = cleaned.Substring(4);

            cleaned = cleaned.Replace('_', ' ').Trim();

            if (string.IsNullOrEmpty(cleaned))
                return rawName;

            return cleaned;
        }

        #endregion
    }
}
