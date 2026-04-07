using System;
using System.Collections.Generic;
using UnityEngine;
using MimicFacility.AI.Persistence;

namespace MimicFacility.Horror
{
    public enum EDeviceTrick
    {
        NotificationSpoof,
        WindowFocus,
        SubliminalFrame,
        PauseMenuInject,
        LoadingScreen,
        Freeze
    }

    public class DeviceHorrorManager : MonoBehaviour
    {
        [SerializeField] private CorruptionTracker corruptionTracker;

        private NotificationSpoofSystem _notificationSystem;
        private WindowFocusObserver _windowFocusObserver;
        private SubliminalFrameSystem _subliminalSystem;
        private PauseMenuInjection _pauseMenuInjection;
        private LoadingScreenSystem _loadingScreenSystem;

        private readonly HashSet<EDeviceTrick> _usedTricks = new HashSet<EDeviceTrick>();
        private readonly Queue<ScheduledTrick> _scheduledTricks = new Queue<ScheduledTrick>();
        private float _lastTrickTime = -120f;
        private const float GlobalCooldown = 120f;

        private static readonly Dictionary<EDeviceTrick, int> MinCorruption = new Dictionary<EDeviceTrick, int>
        {
            { EDeviceTrick.LoadingScreen, 10 },
            { EDeviceTrick.SubliminalFrame, 15 },
            { EDeviceTrick.NotificationSpoof, 25 },
            { EDeviceTrick.PauseMenuInject, 40 },
            { EDeviceTrick.WindowFocus, 50 },
            { EDeviceTrick.Freeze, 60 }
        };

        private struct ScheduledTrick
        {
            public EDeviceTrick Trick;
            public float ExecuteTime;
        }

        private void Awake()
        {
            _notificationSystem = GetComponentInChildren<NotificationSpoofSystem>();
            _windowFocusObserver = GetComponentInChildren<WindowFocusObserver>();
            _subliminalSystem = GetComponentInChildren<SubliminalFrameSystem>();
            _pauseMenuInjection = GetComponentInChildren<PauseMenuInjection>();
            _loadingScreenSystem = GetComponentInChildren<LoadingScreenSystem>();

            if (_notificationSystem == null) _notificationSystem = CreateChild<NotificationSpoofSystem>();
            if (_windowFocusObserver == null) _windowFocusObserver = CreateChild<WindowFocusObserver>();
            if (_subliminalSystem == null) _subliminalSystem = CreateChild<SubliminalFrameSystem>();
            if (_pauseMenuInjection == null) _pauseMenuInjection = CreateChild<PauseMenuInjection>();
            if (_loadingScreenSystem == null) _loadingScreenSystem = CreateChild<LoadingScreenSystem>();
        }

        private T CreateChild<T>() where T : MonoBehaviour
        {
            var child = new GameObject(typeof(T).Name);
            child.transform.SetParent(transform);
            return child.AddComponent<T>();
        }

        private void Update()
        {
            ProcessScheduledTricks();
        }

        public void ScheduleTrick(EDeviceTrick trick, float delay)
        {
            int corruption = corruptionTracker != null ? corruptionTracker.CorruptionIndex : 0;
            if (!CanUseTrick(trick, corruption)) return;

            _scheduledTricks.Enqueue(new ScheduledTrick
            {
                Trick = trick,
                ExecuteTime = Time.time + delay
            });
        }

        public void ExecuteTrick(EDeviceTrick trick)
        {
            int corruption = corruptionTracker != null ? corruptionTracker.CorruptionIndex : 0;
            if (!CanUseTrick(trick, corruption)) return;

            MarkTrickUsed(trick);
            _lastTrickTime = Time.time;

            switch (trick)
            {
                case EDeviceTrick.NotificationSpoof:
                    if (_notificationSystem != null)
                        _notificationSystem.PlayNotificationSound();
                    break;
                case EDeviceTrick.WindowFocus:
                    break;
                case EDeviceTrick.SubliminalFrame:
                    if (_subliminalSystem != null)
                        _subliminalSystem.TriggerRandom();
                    break;
                case EDeviceTrick.PauseMenuInject:
                    if (_pauseMenuInjection != null)
                        _pauseMenuInjection.OnPauseMenuOpened();
                    break;
                case EDeviceTrick.LoadingScreen:
                    if (_loadingScreenSystem != null)
                        _loadingScreenSystem.BeginLoadingScreen("UNKNOWN");
                    break;
                case EDeviceTrick.Freeze:
                    break;
            }
        }

        public bool CanUseTrick(EDeviceTrick trick, int corruption)
        {
            if (_usedTricks.Contains(trick)) return false;
            if (Time.time - _lastTrickTime < GlobalCooldown) return false;
            if (!MinCorruption.TryGetValue(trick, out int minCorruption)) return false;
            return corruption >= minCorruption;
        }

        public bool HasTrickBeenUsed(EDeviceTrick trick)
        {
            return _usedTricks.Contains(trick);
        }

        public void MarkTrickUsed(EDeviceTrick trick)
        {
            _usedTricks.Add(trick);
        }

        public void EvaluateTrickOpportunities()
        {
            if (corruptionTracker == null) return;
            int corruption = corruptionTracker.CorruptionIndex;

            if (CanUseTrick(EDeviceTrick.SubliminalFrame, corruption))
                ScheduleTrick(EDeviceTrick.SubliminalFrame, UnityEngine.Random.Range(5f, 15f));

            if (CanUseTrick(EDeviceTrick.NotificationSpoof, corruption) && _notificationSystem != null && _notificationSystem.IsAvailable)
                ScheduleTrick(EDeviceTrick.NotificationSpoof, UnityEngine.Random.Range(10f, 30f));

            if (CanUseTrick(EDeviceTrick.LoadingScreen, corruption))
                ScheduleTrick(EDeviceTrick.LoadingScreen, UnityEngine.Random.Range(2f, 8f));
        }

        private void ProcessScheduledTricks()
        {
            if (_scheduledTricks.Count == 0) return;

            while (_scheduledTricks.Count > 0 && _scheduledTricks.Peek().ExecuteTime <= Time.time)
            {
                var scheduled = _scheduledTricks.Dequeue();
                ExecuteTrick(scheduled.Trick);
            }
        }
    }
}
