using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MimicFacility.Gameplay;

namespace MimicFacility.UI
{
    public enum NotificationType
    {
        EntitySpotted,
        PlayerDeath,
        MimicContained,
        Miscontainment,
        TaskComplete,
        DirectorMessage,
        SystemAlert,
        RoundChange
    }

    public class NotificationFeed : MonoBehaviour
    {
        private const int MaxVisible = 5;
        private const float HoldDuration = 4f;
        private const float FadeDuration = 0.5f;
        private const float SlotHeight = 32f;
        private const float FeedWidth = 420f;
        private const float PaddingLeft = 16f;
        private const float PaddingBottom = 16f;
        private const float ScrollSpeed = 200f;

        private static NotificationFeed _instance;

        [SerializeField] private AudioClip importantPing;
        [SerializeField] private AudioClip standardPing;

        private Canvas _canvas;
        private RectTransform _feedRoot;
        private AudioSource _audioSource;
        private readonly List<NotificationSlot> _activeSlots = new List<NotificationSlot>();
        private readonly Queue<NotificationSlot> _pool = new Queue<NotificationSlot>();

        private static readonly Dictionary<NotificationType, Color> TypeColors = new Dictionary<NotificationType, Color>
        {
            { NotificationType.EntitySpotted,   new Color(1.0f, 0.55f, 0.0f, 1f) },
            { NotificationType.PlayerDeath,     new Color(0.9f, 0.1f, 0.1f, 1f) },
            { NotificationType.MimicContained,  new Color(0.2f, 0.85f, 0.2f, 1f) },
            { NotificationType.Miscontainment,  new Color(0.85f, 0.2f, 0.3f, 1f) },
            { NotificationType.TaskComplete,    new Color(0.4f, 0.75f, 1.0f, 1f) },
            { NotificationType.DirectorMessage, new Color(0.7f, 0.5f, 0.9f, 1f) },
            { NotificationType.SystemAlert,     new Color(1.0f, 0.9f, 0.2f, 1f) },
            { NotificationType.RoundChange,     new Color(0.95f, 0.95f, 0.95f, 1f) }
        };

        private static readonly HashSet<NotificationType> ImportantTypes = new HashSet<NotificationType>
        {
            NotificationType.PlayerDeath,
            NotificationType.MimicContained,
            NotificationType.Miscontainment,
            NotificationType.SystemAlert,
            NotificationType.RoundChange
        };

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            BuildUI();
            SubscribeToEventBus();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                UnsubscribeFromEventBus();
                _instance = null;
            }
        }

        public static void AddNotification(string text, NotificationType type)
        {
            if (_instance == null) return;
            _instance.PushNotification(text, type);
        }

        private void BuildUI()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 90;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            var rootGo = new GameObject("FeedRoot");
            rootGo.transform.SetParent(_canvas.transform, false);
            _feedRoot = rootGo.AddComponent<RectTransform>();
            _feedRoot.anchorMin = new Vector2(0f, 0f);
            _feedRoot.anchorMax = new Vector2(0f, 0f);
            _feedRoot.pivot = new Vector2(0f, 0f);
            _feedRoot.anchoredPosition = new Vector2(PaddingLeft, PaddingBottom);
            _feedRoot.sizeDelta = new Vector2(FeedWidth, SlotHeight * MaxVisible);

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = 0.5f;
        }

        private void PushNotification(string text, NotificationType type)
        {
            while (_activeSlots.Count >= MaxVisible)
            {
                var oldest = _activeSlots[0];
                RetireSlot(oldest);
            }

            string timestamp = DateTime.Now.ToString("HH:mm");
            string formatted = $"[{timestamp}] {text}";
            Color color = TypeColors.ContainsKey(type) ? TypeColors[type] : Color.white;

            NotificationSlot slot = AcquireSlot();
            slot.Init(formatted, color, _feedRoot);
            _activeSlots.Add(slot);

            LayoutSlots();
            slot.Coroutine = StartCoroutine(SlotLifecycle(slot));
            PlayPing(type);
        }

        private void LayoutSlots()
        {
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                float targetY = i * SlotHeight;
                _activeSlots[i].SetTargetY(targetY);
            }
        }

        private IEnumerator SlotLifecycle(NotificationSlot slot)
        {
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / FadeDuration);
                slot.SetAlpha(alpha);
                yield return null;
            }
            slot.SetAlpha(1f);

            yield return new WaitForSecondsRealtime(HoldDuration);

            elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / FadeDuration);
                slot.SetAlpha(alpha);
                yield return null;
            }
            slot.SetAlpha(0f);

            RetireSlot(slot);
        }

        private void Update()
        {
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                _activeSlots[i].AnimateTowardTarget(ScrollSpeed * Time.unscaledDeltaTime);
            }
        }

        private void RetireSlot(NotificationSlot slot)
        {
            if (slot.Coroutine != null)
                StopCoroutine(slot.Coroutine);

            _activeSlots.Remove(slot);
            slot.Hide();
            _pool.Enqueue(slot);
            LayoutSlots();
        }

        private NotificationSlot AcquireSlot()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();
            return new NotificationSlot();
        }

        private void PlayPing(NotificationType type)
        {
            if (_audioSource == null) return;

            AudioClip clip = ImportantTypes.Contains(type) ? importantPing : standardPing;
            if (clip != null)
                _audioSource.PlayOneShot(clip);
        }

        private void SubscribeToEventBus()
        {
            EventBus.OnEntitySpotted += HandleEntitySpotted;
            EventBus.OnPlayerDeath += HandlePlayerDeath;
            EventBus.OnPlayerConverted += HandlePlayerConverted;
            EventBus.OnMimicContained += HandleMimicContained;
            EventBus.OnMiscontainment += HandleMiscontainment;
            EventBus.OnTaskCompleted += HandleTaskCompleted;
            EventBus.OnRoundStarted += HandleRoundStarted;
            EventBus.OnRoundEnded += HandleRoundEnded;
            EventBus.OnDirectorSpoke += HandleDirectorSpoke;
            EventBus.OnExtractionAvailable += HandleExtractionAvailable;
            EventBus.OnGameOver += HandleGameOver;
            EventBus.OnGameWin += HandleGameWin;
        }

        private void UnsubscribeFromEventBus()
        {
            EventBus.OnEntitySpotted -= HandleEntitySpotted;
            EventBus.OnPlayerDeath -= HandlePlayerDeath;
            EventBus.OnPlayerConverted -= HandlePlayerConverted;
            EventBus.OnMimicContained -= HandleMimicContained;
            EventBus.OnMiscontainment -= HandleMiscontainment;
            EventBus.OnTaskCompleted -= HandleTaskCompleted;
            EventBus.OnRoundStarted -= HandleRoundStarted;
            EventBus.OnRoundEnded -= HandleRoundEnded;
            EventBus.OnDirectorSpoke -= HandleDirectorSpoke;
            EventBus.OnExtractionAvailable -= HandleExtractionAvailable;
            EventBus.OnGameOver -= HandleGameOver;
            EventBus.OnGameWin -= HandleGameWin;
        }

        private void HandleEntitySpotted(string entityType, string location)
        {
            AddNotification($"Entity spotted near {location}", NotificationType.EntitySpotted);
        }

        private void HandlePlayerDeath(string playerName)
        {
            AddNotification($"{playerName} has been killed", NotificationType.PlayerDeath);
        }

        private void HandlePlayerConverted(string playerName)
        {
            AddNotification($"Signal lost: {playerName}", NotificationType.SystemAlert);
        }

        private void HandleMimicContained(string playerName, string mimicType)
        {
            AddNotification($"{playerName} contained a {mimicType}", NotificationType.MimicContained);
        }

        private void HandleMiscontainment(string playerName)
        {
            AddNotification($"MISCONTAINMENT by {playerName}", NotificationType.Miscontainment);
        }

        private void HandleTaskCompleted(string taskName, string playerName)
        {
            AddNotification($"{playerName} completed {taskName}", NotificationType.TaskComplete);
        }

        private void HandleRoundStarted(int roundNumber)
        {
            AddNotification($"Round {roundNumber} started", NotificationType.RoundChange);
        }

        private void HandleRoundEnded(int roundNumber)
        {
            AddNotification($"Round {roundNumber} ended", NotificationType.RoundChange);
        }

        private void HandleDirectorSpoke(string message)
        {
            AddNotification(message, NotificationType.DirectorMessage);
        }

        private void HandleExtractionAvailable()
        {
            AddNotification("EXTRACTION IS NOW AVAILABLE", NotificationType.SystemAlert);
        }

        private void HandleGameOver(string reason)
        {
            AddNotification($"FACILITY LOST: {reason}", NotificationType.SystemAlert);
        }

        private void HandleGameWin()
        {
            AddNotification("EXTRACTION SUCCESSFUL", NotificationType.SystemAlert);
        }

        private class NotificationSlot
        {
            private GameObject _root;
            private TextMeshProUGUI _text;
            private CanvasGroup _group;
            private RectTransform _rect;
            private float _targetY;

            public Coroutine Coroutine { get; set; }

            public void Init(string message, Color color, RectTransform parent)
            {
                if (_root == null)
                    CreateGameObject();

                _root.transform.SetParent(parent, false);
                _root.SetActive(true);

                _text.text = message;
                _text.color = color;
                _group.alpha = 0f;

                _rect.anchoredPosition = new Vector2(0f, 0f);
                _targetY = 0f;
            }

            public void SetAlpha(float alpha)
            {
                if (_group != null)
                    _group.alpha = alpha;
            }

            public void SetTargetY(float y)
            {
                _targetY = y;
            }

            public void AnimateTowardTarget(float maxDelta)
            {
                if (_rect == null) return;
                var pos = _rect.anchoredPosition;
                pos.y = Mathf.MoveTowards(pos.y, _targetY, maxDelta);
                _rect.anchoredPosition = pos;
            }

            public void Hide()
            {
                if (_root != null)
                    _root.SetActive(false);
            }

            private void CreateGameObject()
            {
                _root = new GameObject("Notification");
                _rect = _root.AddComponent<RectTransform>();
                _rect.anchorMin = new Vector2(0f, 0f);
                _rect.anchorMax = new Vector2(0f, 0f);
                _rect.pivot = new Vector2(0f, 0f);
                _rect.sizeDelta = new Vector2(FeedWidth, SlotHeight);

                _group = _root.AddComponent<CanvasGroup>();
                _group.alpha = 0f;
                _group.blocksRaycasts = false;
                _group.interactable = false;

                var textGo = new GameObject("Text");
                textGo.transform.SetParent(_root.transform, false);

                var textRect = textGo.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8f, 0f);
                textRect.offsetMax = new Vector2(-8f, 0f);

                _text = textGo.AddComponent<TextMeshProUGUI>();
                _text.fontSize = 14f;
                _text.fontStyle = FontStyles.Normal;
                _text.alignment = TextAlignmentOptions.MidlineLeft;
                _text.enableWordWrapping = false;
                _text.overflowMode = TextOverflowModes.Ellipsis;
                _text.raycastTarget = false;
            }
        }
    }
}
