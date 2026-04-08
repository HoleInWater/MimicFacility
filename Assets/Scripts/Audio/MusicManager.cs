using System;
using UnityEngine;

namespace MimicFacility.Audio
{
    public enum MusicTrack
    {
        None,
        Exploration,
        Tension,
        Chase,
        Danger,
        Victory,
        Defeat
    }

    [Serializable]
    public class MusicTrackEntry
    {
        public MusicTrack track;
        public AudioClip clip;
        [Range(0f, 1f)] public float baseVolume = 0.5f;
        public bool loop = true;
    }

    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Tracks")]
        [SerializeField] private MusicTrackEntry[] tracks = new MusicTrackEntry[]
        {
            new MusicTrackEntry { track = MusicTrack.Exploration, baseVolume = 0.3f },
            new MusicTrackEntry { track = MusicTrack.Tension, baseVolume = 0.45f },
            new MusicTrackEntry { track = MusicTrack.Chase, baseVolume = 0.6f },
            new MusicTrackEntry { track = MusicTrack.Danger, baseVolume = 0.55f },
            new MusicTrackEntry { track = MusicTrack.Victory, baseVolume = 0.5f, loop = false },
            new MusicTrackEntry { track = MusicTrack.Defeat, baseVolume = 0.5f, loop = false },
        };

        [Header("Crossfade")]
        [SerializeField] private float crossfadeDuration = 2f;

        [Header("Intensity")]
        [SerializeField] [Range(0f, 1f)] private float intensity = 0.5f;
        [SerializeField] private float intensitySmoothing = 2f;

        [Header("Stingers")]
        [SerializeField] private AudioClip stingerEntitySpotted;
        [SerializeField] private AudioClip stingerContainmentUsed;
        [SerializeField] private AudioClip stingerPlayerDeath;
        [SerializeField] private AudioClip stingerFalsePositive;
        [SerializeField] [Range(0f, 1f)] private float stingerVolume = 0.8f;

        [Header("Master")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _stingerSource;

        private MusicTrack _currentTrack = MusicTrack.None;
        private MusicTrack _targetTrack = MusicTrack.None;

        private bool _crossfading;
        private float _crossfadeTimer;
        private bool _sourceAIsActive = true;

        private float _smoothedIntensity;

        public MusicTrack CurrentTrack => _currentTrack;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sourceA = CreateMusicSource("MusicSourceA");
            _sourceB = CreateMusicSource("MusicSourceB");
            _stingerSource = CreateStingerSource();

            _smoothedIntensity = intensity;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private AudioSource CreateMusicSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.priority = 48;
            return source;
        }

        private AudioSource CreateStingerSource()
        {
            var go = new GameObject("StingerSource");
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.priority = 16;
            return source;
        }

        private void Update()
        {
            UpdateIntensity();
            UpdateCrossfade();
            UpdateActiveVolume();
        }

        private void UpdateIntensity()
        {
            _smoothedIntensity = Mathf.MoveTowards(
                _smoothedIntensity,
                intensity,
                intensitySmoothing * Time.deltaTime
            );
        }

        private void UpdateCrossfade()
        {
            if (!_crossfading)
                return;

            _crossfadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_crossfadeTimer / crossfadeDuration);

            var fadingOut = _sourceAIsActive ? _sourceB : _sourceA;
            var fadingIn = _sourceAIsActive ? _sourceA : _sourceB;

            var targetEntry = FindTrackEntry(_targetTrack);
            float targetBaseVolume = targetEntry != null ? targetEntry.baseVolume : 0.5f;

            fadingOut.volume = Mathf.Lerp(fadingOut.volume, 0f, t) * masterVolume;
            fadingIn.volume = Mathf.Lerp(0f, targetBaseVolume * _smoothedIntensity, t) * masterVolume;

            if (t >= 1f)
            {
                _crossfading = false;
                fadingOut.Stop();
                fadingOut.volume = 0f;
                _currentTrack = _targetTrack;
            }
        }

        private void UpdateActiveVolume()
        {
            if (_crossfading)
                return;

            var active = _sourceAIsActive ? _sourceA : _sourceB;
            var entry = FindTrackEntry(_currentTrack);
            if (entry == null || _currentTrack == MusicTrack.None)
                return;

            float target = entry.baseVolume * _smoothedIntensity * masterVolume;
            active.volume = Mathf.MoveTowards(active.volume, target, Time.deltaTime);
        }

        public void SetTrack(MusicTrack track)
        {
            if (track == _currentTrack && !_crossfading)
                return;

            if (track == _targetTrack && _crossfading)
                return;

            _targetTrack = track;

            if (track == MusicTrack.None)
            {
                FadeOutAll();
                return;
            }

            var entry = FindTrackEntry(track);
            if (entry == null)
            {
                Debug.LogWarning($"[MusicManager] No entry configured for track {track}");
                return;
            }

            _sourceAIsActive = !_sourceAIsActive;
            var incoming = _sourceAIsActive ? _sourceA : _sourceB;

            incoming.volume = 0f;

            if (entry.clip != null)
            {
                incoming.clip = entry.clip;
                incoming.loop = entry.loop;
                incoming.Play();
            }
            else
            {
                Debug.Log($"[MusicManager] Track {track} would play (no clip assigned, base volume {entry.baseVolume:F2})");
            }

            _crossfading = true;
            _crossfadeTimer = 0f;
        }

        public void PlayStinger(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.Log("[MusicManager] Stinger triggered (null clip)");
                return;
            }

            _stingerSource.PlayOneShot(clip, stingerVolume * masterVolume);
        }

        public void PlayStingerEntitySpotted()
        {
            if (stingerEntitySpotted != null)
                PlayStinger(stingerEntitySpotted);
            else
                Debug.Log("[MusicManager] Stinger: entity spotted (no clip assigned)");
        }

        public void PlayStingerContainmentUsed()
        {
            if (stingerContainmentUsed != null)
                PlayStinger(stingerContainmentUsed);
            else
                Debug.Log("[MusicManager] Stinger: containment used (no clip assigned)");
        }

        public void PlayStingerPlayerDeath()
        {
            if (stingerPlayerDeath != null)
                PlayStinger(stingerPlayerDeath);
            else
                Debug.Log("[MusicManager] Stinger: player death (no clip assigned)");
        }

        public void PlayStingerFalsePositive()
        {
            if (stingerFalsePositive != null)
                PlayStinger(stingerFalsePositive);
            else
                Debug.Log("[MusicManager] Stinger: false positive (no clip assigned)");
        }

        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp01(value);
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
        }

        public void SetCrossfadeDuration(float seconds)
        {
            crossfadeDuration = Mathf.Max(0.1f, seconds);
        }

        public void Stop()
        {
            SetTrack(MusicTrack.None);
        }

        private void FadeOutAll()
        {
            _crossfading = true;
            _crossfadeTimer = 0f;
            _targetTrack = MusicTrack.None;

            _sourceAIsActive = !_sourceAIsActive;
            var incoming = _sourceAIsActive ? _sourceA : _sourceB;
            incoming.volume = 0f;
            incoming.Stop();
        }

        private MusicTrackEntry FindTrackEntry(MusicTrack track)
        {
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].track == track)
                    return tracks[i];
            }
            return null;
        }
    }
}
