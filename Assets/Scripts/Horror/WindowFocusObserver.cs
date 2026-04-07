using System;
using System.Collections;
using UnityEngine;

namespace MimicFacility.Horror
{
    public class WindowFocusObserver : MonoBehaviour
    {
        [SerializeField] private AudioClip[] whisperClips;
        [SerializeField] private float minimumBackgroundTime = 3f;
        [SerializeField] private float whisperDelay = 0.5f;
        [SerializeField] private float whisperVolume = 0.7f;

        private float _backgroundStartTime;
        private bool _isInBackground;
        private AudioSource _audioSource;

        public event Action OnWhisperPlayed;

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f;
            _audioSource.playOnAwake = false;
            _audioSource.volume = whisperVolume;
        }

        private void OnEnable()
        {
            Application.focusChanged += HandleFocusChanged;
        }

        private void OnDisable()
        {
            Application.focusChanged -= HandleFocusChanged;
        }

        private void HandleFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
            {
                _backgroundStartTime = Time.realtimeSinceStartup;
                _isInBackground = true;
                return;
            }

            if (_isInBackground)
            {
                _isInBackground = false;
                float elapsed = Time.realtimeSinceStartup - _backgroundStartTime;

                if (elapsed >= minimumBackgroundTime)
                    StartCoroutine(PlayWhisperDelayed());
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            HandleFocusChanged(hasFocus);
        }

        private IEnumerator PlayWhisperDelayed()
        {
            yield return new WaitForSecondsRealtime(whisperDelay);
            PlayWhisper();
        }

        public void PlayWhisper()
        {
            if (whisperClips == null || whisperClips.Length == 0) return;

            AudioClip clip = whisperClips[UnityEngine.Random.Range(0, whisperClips.Length)];
            if (clip == null) return;

            _audioSource.clip = clip;
            _audioSource.Play();

            OnWhisperPlayed?.Invoke();
        }

        public void SetWhisperClips(AudioClip[] clips)
        {
            whisperClips = clips;
        }

        public bool HasWhisperClips()
        {
            return whisperClips != null && whisperClips.Length > 0;
        }

        public float GetBackgroundDuration()
        {
            if (!_isInBackground) return 0f;
            return Time.realtimeSinceStartup - _backgroundStartTime;
        }
    }
}
