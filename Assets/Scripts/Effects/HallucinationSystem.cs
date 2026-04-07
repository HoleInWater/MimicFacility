using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

namespace MimicFacility.Effects
{
    public enum EHallucinationType
    {
        AudioDistortion,
        VisualFlicker,
        ShadowMovement,
        FalsePlayerEcho,
        EnvironmentalShift
    }

    [Serializable]
    public class HallucinationEvent
    {
        public EHallucinationType Type;
        public float Intensity;
        public float Duration;
        public Vector3 SourceLocation;
        public float ElapsedTime;

        public bool IsExpired => ElapsedTime >= Duration;
    }

    public class HallucinationSystem : MonoBehaviour
    {
        [Header("Spore Settings")]
        [SerializeField] private float sporeExposure;
        [SerializeField] private float decayRate = 0.05f;
        [SerializeField] private float maxExposure = 1f;

        [Header("Audio")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string pitchParameter = "MasterPitch";

        [Header("Visual Effects")]
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private ParticleSystem shadowParticles;
        [SerializeField] private GameObject falsePlayerIndicatorPrefab;

        [Header("Thresholds")]
        [SerializeField] private float audioDistortionThreshold = 0.3f;
        [SerializeField] private float visualFlickerThreshold = 0.5f;
        [SerializeField] private float shadowMovementThreshold = 0.7f;
        [SerializeField] private float environmentalShiftThreshold = 0.9f;

        private readonly List<HallucinationEvent> _activeEvents = new List<HallucinationEvent>();
        private float _hallucinationCooldown;
        private GameObject _activeFalseIndicator;

        public float SporeExposure => sporeExposure;
        public IReadOnlyList<HallucinationEvent> ActiveEvents => _activeEvents;

        private void Update()
        {
            DecayExposure();
            UpdateActiveEvents();
            CheckThresholds();
            ApplyPostProcessEffects();
        }

        public void AddSporeExposure(float amount, float filterEfficiency = 0f)
        {
            float effective = amount * (1f - Mathf.Clamp01(filterEfficiency));
            sporeExposure = Mathf.Min(maxExposure, sporeExposure + effective);
        }

        public void TriggerHallucination(HallucinationEvent evt)
        {
            if (evt == null) return;
            _activeEvents.Add(evt);
            OnHallucinationTriggered(evt);
        }

        private void DecayExposure()
        {
            if (sporeExposure > 0f)
                sporeExposure = Mathf.Max(0f, sporeExposure - decayRate * Time.deltaTime);
        }

        private void UpdateActiveEvents()
        {
            for (int i = _activeEvents.Count - 1; i >= 0; i--)
            {
                _activeEvents[i].ElapsedTime += Time.deltaTime;
                ProcessEvent(_activeEvents[i]);
            }

            RemoveExpiredEvents();
        }

        private void CheckThresholds()
        {
            _hallucinationCooldown -= Time.deltaTime;
            if (_hallucinationCooldown > 0f) return;

            if (sporeExposure >= environmentalShiftThreshold)
            {
                TriggerHallucination(new HallucinationEvent
                {
                    Type = EHallucinationType.EnvironmentalShift,
                    Intensity = sporeExposure,
                    Duration = 3f,
                    SourceLocation = transform.position
                });
                _hallucinationCooldown = 8f;
            }
            else if (sporeExposure >= shadowMovementThreshold)
            {
                TriggerHallucination(new HallucinationEvent
                {
                    Type = EHallucinationType.ShadowMovement,
                    Intensity = sporeExposure,
                    Duration = 2f,
                    SourceLocation = transform.position + transform.right * 3f
                });
                TriggerHallucination(new HallucinationEvent
                {
                    Type = EHallucinationType.FalsePlayerEcho,
                    Intensity = sporeExposure,
                    Duration = 1.5f,
                    SourceLocation = transform.position + transform.forward * 5f
                });
                _hallucinationCooldown = 6f;
            }
            else if (sporeExposure >= visualFlickerThreshold)
            {
                TriggerHallucination(new HallucinationEvent
                {
                    Type = EHallucinationType.VisualFlicker,
                    Intensity = sporeExposure,
                    Duration = 1f
                });
                _hallucinationCooldown = 4f;
            }
            else if (sporeExposure >= audioDistortionThreshold)
            {
                TriggerHallucination(new HallucinationEvent
                {
                    Type = EHallucinationType.AudioDistortion,
                    Intensity = sporeExposure,
                    Duration = 2f
                });
                _hallucinationCooldown = 5f;
            }
        }

        private void ProcessEvent(HallucinationEvent evt)
        {
            float progress = Mathf.Clamp01(evt.ElapsedTime / evt.Duration);

            switch (evt.Type)
            {
                case EHallucinationType.AudioDistortion:
                    if (audioMixer != null)
                    {
                        float pitch = Mathf.Lerp(1f, 0.85f, evt.Intensity * (1f - progress));
                        audioMixer.SetFloat(pitchParameter, pitch);
                    }
                    break;

                case EHallucinationType.VisualFlicker:
                    if (postProcessVolume != null)
                    {
                        float pulse = Mathf.PingPong(evt.ElapsedTime * 8f, 1f);
                        postProcessVolume.weight = Mathf.Lerp(0f, evt.Intensity, pulse);
                    }
                    break;

                case EHallucinationType.ShadowMovement:
                    if (shadowParticles != null && !shadowParticles.isPlaying)
                        shadowParticles.Play();
                    break;

                case EHallucinationType.FalsePlayerEcho:
                    if (_activeFalseIndicator == null && falsePlayerIndicatorPrefab != null)
                        _activeFalseIndicator = Instantiate(falsePlayerIndicatorPrefab, evt.SourceLocation, Quaternion.identity);
                    break;

                case EHallucinationType.EnvironmentalShift:
                    break;
            }
        }

        private void OnHallucinationTriggered(HallucinationEvent evt)
        {
            if (evt.Type == EHallucinationType.ShadowMovement && shadowParticles != null)
            {
                shadowParticles.transform.position = evt.SourceLocation;
            }
        }

        public void ApplyPostProcessEffects()
        {
            if (postProcessVolume == null) return;

            bool hasVisualEvent = false;
            foreach (var evt in _activeEvents)
            {
                if (evt.Type == EHallucinationType.VisualFlicker)
                {
                    hasVisualEvent = true;
                    break;
                }
            }

            if (!hasVisualEvent)
                postProcessVolume.weight = Mathf.Lerp(postProcessVolume.weight, sporeExposure * 0.3f, Time.deltaTime * 2f);
        }

        public void RemoveExpiredEvents()
        {
            for (int i = _activeEvents.Count - 1; i >= 0; i--)
            {
                if (!_activeEvents[i].IsExpired) continue;

                var evt = _activeEvents[i];

                if (evt.Type == EHallucinationType.AudioDistortion && audioMixer != null)
                    audioMixer.SetFloat(pitchParameter, 1f);

                if (evt.Type == EHallucinationType.ShadowMovement && shadowParticles != null)
                    shadowParticles.Stop();

                if (evt.Type == EHallucinationType.FalsePlayerEcho && _activeFalseIndicator != null)
                {
                    Destroy(_activeFalseIndicator);
                    _activeFalseIndicator = null;
                }

                _activeEvents.RemoveAt(i);
            }
        }

        public void ClearAllHallucinations()
        {
            _activeEvents.Clear();
            sporeExposure = 0f;

            if (audioMixer != null)
                audioMixer.SetFloat(pitchParameter, 1f);

            if (shadowParticles != null)
                shadowParticles.Stop();

            if (_activeFalseIndicator != null)
            {
                Destroy(_activeFalseIndicator);
                _activeFalseIndicator = null;
            }
        }
    }
}
