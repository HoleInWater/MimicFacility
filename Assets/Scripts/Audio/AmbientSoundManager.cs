using System;
using UnityEngine;
using MimicFacility.Core;
using MimicFacility.AI.Persistence;

namespace MimicFacility.Audio
{
    public enum AmbientLayer
    {
        BaseHum,
        VentilationRumble,
        HeartbeatPulse,
        StaticBuzz,
        SporeWhisper,
        DistantScreams
    }

    [Serializable]
    public class AmbientLayerConfig
    {
        public AmbientLayer layer;
        public AudioClip clip;
        [Range(0f, 1f)] public float maxVolume = 1f;
        public float fadeSpeed = 1f;

        [HideInInspector] public AudioSource source;
        [HideInInspector] public float targetVolume;
        [HideInInspector] public float currentVolume;
    }

    public class AmbientSoundManager : MonoBehaviour
    {
        public static AmbientSoundManager Instance { get; private set; }

        [Header("Ambient Layers")]
        [SerializeField] private AmbientLayerConfig[] layers = new AmbientLayerConfig[]
        {
            new AmbientLayerConfig { layer = AmbientLayer.BaseHum, maxVolume = 0.3f, fadeSpeed = 0.5f },
            new AmbientLayerConfig { layer = AmbientLayer.VentilationRumble, maxVolume = 0.4f, fadeSpeed = 0.8f },
            new AmbientLayerConfig { layer = AmbientLayer.HeartbeatPulse, maxVolume = 0.6f, fadeSpeed = 1.5f },
            new AmbientLayerConfig { layer = AmbientLayer.StaticBuzz, maxVolume = 0.35f, fadeSpeed = 1.2f },
            new AmbientLayerConfig { layer = AmbientLayer.SporeWhisper, maxVolume = 0.5f, fadeSpeed = 0.7f },
            new AmbientLayerConfig { layer = AmbientLayer.DistantScreams, maxVolume = 0.45f, fadeSpeed = 0.6f },
        };

        [Header("One-Shot Scares")]
        [SerializeField] private AudioClip[] metalClangClips;
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private AudioClip[] whisperClips;
        [SerializeField] private AudioClip[] doorSlamClips;
        [SerializeField] [Range(0f, 1f)] private float scareVolume = 0.7f;

        [Header("Scare Timing")]
        [SerializeField] private float scareIntervalMin = 30f;
        [SerializeField] private float scareIntervalMax = 90f;
        [SerializeField] [Range(0f, 1f)] private float corruptionScareThreshold = 0.2f;

        [Header("Entity Detection")]
        [SerializeField] private float entityNearbyRange = 15f;
        [SerializeField] private float directorSpeakerRange = 8f;

        [Header("Spore Thresholds")]
        [SerializeField] private float sporeWhisperThreshold = 30f;

        private AudioSource _scareSource;
        private float _scareTimer;
        private float _nextScareInterval;
        private string _currentZone = "";
        private MimicPlayerState _localPlayer;
        private CorruptionTracker _corruptionTracker;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeLayers();
            InitializeScareSource();
            RollNextScareInterval();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void InitializeLayers()
        {
            for (int i = 0; i < layers.Length; i++)
            {
                var config = layers[i];
                var source = gameObject.AddComponent<AudioSource>();
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0f;
                source.priority = 64;

                if (config.clip != null)
                {
                    source.clip = config.clip;
                    source.Play();
                }
                else
                {
                    Debug.Log($"[AmbientSoundManager] No clip assigned for layer {config.layer}, will log playback.");
                }

                config.source = source;
                config.currentVolume = 0f;
                config.targetVolume = 0f;
            }
        }

        private void InitializeScareSource()
        {
            _scareSource = gameObject.AddComponent<AudioSource>();
            _scareSource.loop = false;
            _scareSource.playOnAwake = false;
            _scareSource.spatialBlend = 0f;
            _scareSource.priority = 32;
        }

        private void Update()
        {
            CacheReferences();
            UpdateLayerTargets();
            UpdateLayers();
            UpdateScareTimer();
        }

        private void CacheReferences()
        {
            if (_localPlayer == null)
                _localPlayer = FindObjectOfType<MimicPlayerState>();

            if (_corruptionTracker == null)
                _corruptionTracker = FindObjectOfType<CorruptionTracker>();
        }

        private void UpdateLayerTargets()
        {
            float sporeExposure = _localPlayer != null ? _localPlayer.SporeExposure : 0f;
            float healthRatio = _localPlayer != null ? _localPlayer.Health / _localPlayer.MaxHealth : 1f;
            float corruption = _corruptionTracker != null ? _corruptionTracker.GetCorruptionNormalized() : 0f;
            bool entityNearby = IsEntityNearby();
            bool nearSpeaker = IsNearDirectorSpeaker();

            for (int i = 0; i < layers.Length; i++)
            {
                var config = layers[i];
                float target = 0f;

                switch (config.layer)
                {
                    case AmbientLayer.BaseHum:
                        target = config.maxVolume;
                        break;

                    case AmbientLayer.VentilationRumble:
                        target = IsInFacilityZone() ? config.maxVolume : 0f;
                        break;

                    case AmbientLayer.HeartbeatPulse:
                        if (entityNearby)
                            target = config.maxVolume;
                        else if (healthRatio < 0.3f)
                            target = config.maxVolume * (1f - healthRatio / 0.3f);
                        break;

                    case AmbientLayer.StaticBuzz:
                        target = nearSpeaker ? config.maxVolume : 0f;
                        break;

                    case AmbientLayer.SporeWhisper:
                        if (sporeExposure >= sporeWhisperThreshold)
                        {
                            float sporeRatio = Mathf.InverseLerp(sporeWhisperThreshold, 100f, sporeExposure);
                            target = config.maxVolume * sporeRatio;
                        }
                        break;

                    case AmbientLayer.DistantScreams:
                        if (corruption > 0.4f)
                        {
                            float screamsRatio = Mathf.InverseLerp(0.4f, 1f, corruption);
                            target = config.maxVolume * screamsRatio;
                        }
                        break;
                }

                config.targetVolume = target;
            }
        }

        private void UpdateLayers()
        {
            float dt = Time.deltaTime;

            for (int i = 0; i < layers.Length; i++)
            {
                var config = layers[i];
                if (Mathf.Approximately(config.currentVolume, config.targetVolume))
                    continue;

                config.currentVolume = Mathf.MoveTowards(
                    config.currentVolume,
                    config.targetVolume,
                    config.fadeSpeed * dt
                );

                if (config.source != null)
                {
                    config.source.volume = config.currentVolume;

                    if (config.clip == null && config.currentVolume > 0.01f && !config.source.isPlaying)
                        Debug.Log($"[AmbientSoundManager] Layer {config.layer} would play at volume {config.currentVolume:F2}");
                }
            }
        }

        private void UpdateScareTimer()
        {
            float corruption = _corruptionTracker != null ? _corruptionTracker.GetCorruptionNormalized() : 0f;
            if (corruption < corruptionScareThreshold)
                return;

            _scareTimer += Time.deltaTime;
            if (_scareTimer < _nextScareInterval)
                return;

            _scareTimer = 0f;
            float corruptionScale = Mathf.InverseLerp(corruptionScareThreshold, 1f, corruption);
            float adjustedMin = Mathf.Lerp(scareIntervalMin, scareIntervalMin * 0.5f, corruptionScale);
            float adjustedMax = Mathf.Lerp(scareIntervalMax, scareIntervalMax * 0.5f, corruptionScale);
            _nextScareInterval = UnityEngine.Random.Range(adjustedMin, adjustedMax);

            PlayRandomScare();
        }

        private void PlayRandomScare()
        {
            AudioClip[][] scarePools = { metalClangClips, footstepClips, whisperClips, doorSlamClips };
            string[] scareNames = { "metal clang", "footsteps", "whisper", "door slam" };

            int poolIndex = UnityEngine.Random.Range(0, scarePools.Length);
            AudioClip[] pool = scarePools[poolIndex];

            if (pool == null || pool.Length == 0)
            {
                Debug.Log($"[AmbientSoundManager] One-shot scare: {scareNames[poolIndex]} (no clip assigned)");
                return;
            }

            AudioClip clip = pool[UnityEngine.Random.Range(0, pool.Length)];
            if (clip == null)
            {
                Debug.Log($"[AmbientSoundManager] One-shot scare: {scareNames[poolIndex]} (null clip in pool)");
                return;
            }

            float volumeJitter = scareVolume * UnityEngine.Random.Range(0.8f, 1f);
            _scareSource.PlayOneShot(clip, volumeJitter);
        }

        public void SetZone(string zoneName)
        {
            if (_currentZone == zoneName)
                return;

            string previousZone = _currentZone;
            _currentZone = zoneName ?? "";
            Debug.Log($"[AmbientSoundManager] Zone changed: {previousZone} -> {_currentZone}");
        }

        public void SetLocalPlayer(MimicPlayerState player)
        {
            _localPlayer = player;
        }

        public AmbientLayerConfig GetLayerConfig(AmbientLayer layer)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].layer == layer)
                    return layers[i];
            }
            return null;
        }

        public void SetLayerTargetVolume(AmbientLayer layer, float volume)
        {
            var config = GetLayerConfig(layer);
            if (config != null)
                config.targetVolume = Mathf.Clamp01(volume);
        }

        private bool IsEntityNearby()
        {
            if (_localPlayer == null)
                return false;

            Vector3 playerPos = _localPlayer.transform.position;
            float rangeSqr = entityNearbyRange * entityNearbyRange;

            var entities = FindObjectsOfType<Entities.MimicBase>();
            for (int i = 0; i < entities.Length; i++)
            {
                float distSqr = (entities[i].transform.position - playerPos).sqrMagnitude;
                if (distSqr <= rangeSqr)
                    return true;
            }

            return false;
        }

        private bool IsNearDirectorSpeaker()
        {
            if (_localPlayer == null)
                return false;

            var speakers = GameObject.FindGameObjectsWithTag("DirectorSpeaker");
            if (speakers == null || speakers.Length == 0)
                return false;

            Vector3 playerPos = _localPlayer.transform.position;
            float rangeSqr = directorSpeakerRange * directorSpeakerRange;

            for (int i = 0; i < speakers.Length; i++)
            {
                float distSqr = (speakers[i].transform.position - playerPos).sqrMagnitude;
                if (distSqr <= rangeSqr)
                    return true;
            }

            return false;
        }

        private bool IsInFacilityZone()
        {
            if (string.IsNullOrEmpty(_currentZone))
                return true;

            return _currentZone != "Exterior" && _currentZone != "ExtractionZone";
        }

        private void RollNextScareInterval()
        {
            _nextScareInterval = UnityEngine.Random.Range(scareIntervalMin, scareIntervalMax);
        }
    }
}
