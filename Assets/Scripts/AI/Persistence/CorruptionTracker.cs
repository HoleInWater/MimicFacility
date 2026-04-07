using System;
using UnityEngine;

namespace MimicFacility.AI.Persistence
{
    public enum ECorruptionPhase
    {
        Cain,
        Transition,
        AMEmerging,
        FullAM
    }

    public enum ECorruptionEvent
    {
        PlayerMockedDirector,
        NoEngagement,
        PlayerLied,
        MechanicOnlyPlay,
        PlayerDisconnected,
        SkippedDialogue,
        SincereAnswer,
        ThankedDirector,
        BetweenSessionReference,
        StayedCredits,
        PlayerApologized
    }

    public class CorruptionTracker : MonoBehaviour
    {
        public event Action<ECorruptionPhase, ECorruptionPhase> OnCorruptionChanged;

        [SerializeField] private int corruptionIndex;
        public int CorruptionIndex => corruptionIndex;

        private ECorruptionPhase cachedPhase;

        private void Awake()
        {
            cachedPhase = GetCorruptionPhase();
        }

        public void ProcessEvent(ECorruptionEvent evt)
        {
            int delta = GetDelta(evt);
            int oldValue = corruptionIndex;
            var oldPhase = cachedPhase;

            corruptionIndex = Mathf.Clamp(corruptionIndex + delta, 0, 100);
            var newPhase = GetCorruptionPhase();

            if (newPhase != oldPhase)
            {
                cachedPhase = newPhase;
                OnCorruptionChanged?.Invoke(oldPhase, newPhase);
                Debug.Log($"[CorruptionTracker] {oldPhase} -> {newPhase} (index: {oldValue} -> {corruptionIndex}, event: {evt})");
            }
        }

        public ECorruptionPhase GetCorruptionPhase()
        {
            if (corruptionIndex <= 25) return ECorruptionPhase.Cain;
            if (corruptionIndex <= 50) return ECorruptionPhase.Transition;
            if (corruptionIndex <= 75) return ECorruptionPhase.AMEmerging;
            return ECorruptionPhase.FullAM;
        }

        public int GetDelta(ECorruptionEvent evt)
        {
            switch (evt)
            {
                case ECorruptionEvent.PlayerMockedDirector: return 5;
                case ECorruptionEvent.NoEngagement: return 3;
                case ECorruptionEvent.PlayerLied: return 3;
                case ECorruptionEvent.MechanicOnlyPlay: return 2;
                case ECorruptionEvent.PlayerDisconnected: return 5;
                case ECorruptionEvent.SkippedDialogue: return 2;
                case ECorruptionEvent.SincereAnswer: return -2;
                case ECorruptionEvent.ThankedDirector: return -1;
                case ECorruptionEvent.BetweenSessionReference: return -3;
                case ECorruptionEvent.StayedCredits: return -1;
                case ECorruptionEvent.PlayerApologized: return -5;
                default: return 0;
            }
        }

        public float GetCorruptionNormalized()
        {
            return corruptionIndex / 100f;
        }

        public void ResetCorruption()
        {
            var oldPhase = cachedPhase;
            corruptionIndex = 0;
            cachedPhase = ECorruptionPhase.Cain;
            if (oldPhase != ECorruptionPhase.Cain)
                OnCorruptionChanged?.Invoke(oldPhase, ECorruptionPhase.Cain);
        }

        public void SetCorruption(int value)
        {
            var oldPhase = cachedPhase;
            corruptionIndex = Mathf.Clamp(value, 0, 100);
            var newPhase = GetCorruptionPhase();
            cachedPhase = newPhase;
            if (newPhase != oldPhase)
                OnCorruptionChanged?.Invoke(oldPhase, newPhase);
        }
    }
}
