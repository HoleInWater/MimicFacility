using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MimicFacility.Effects
{
    public class PostProcessController : MonoBehaviour
    {
        [SerializeField] private Volume volume;
        [SerializeField] private VolumeProfile normalProfile;
        [SerializeField] private VolumeProfile horrorProfile;

        [Header("Damage Flash")]
        [SerializeField] private float damageFlashDuration = 0.3f;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0f, 0f, 0.4f);

        [Header("Freeze")]
        [SerializeField] private float freezeDesaturation = 0.8f;
        [SerializeField] private float freezeZoomAmount = 1.05f;

        [Header("Director Presence")]
        [SerializeField] private Color directorPresenceColor = new Color(0.1f, 0.0f, 0.15f, 0.2f);

        private Camera _camera;
        private float _defaultFov;
        private float _targetWeight;
        private float _currentWeight;
        private float _weightLerpSpeed = 2f;
        private bool _scanlineActive;
        private Coroutine _damageFlashCoroutine;
        private Coroutine _freezeCoroutine;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null)
                _defaultFov = _camera.fieldOfView;

            if (volume == null)
                volume = GetComponentInChildren<Volume>();
        }

        private void Update()
        {
            if (volume == null) return;

            _currentWeight = Mathf.Lerp(_currentWeight, _targetWeight, Time.deltaTime * _weightLerpSpeed);
            volume.weight = _currentWeight;
        }

        public void SporeExposureEffect(float exposure)
        {
            float clamped = Mathf.Clamp01(exposure);
            _targetWeight = clamped;

            if (volume != null && horrorProfile != null && normalProfile != null)
                volume.profile = clamped > 0.5f ? horrorProfile : normalProfile;
        }

        public void DamageFlash()
        {
            if (_damageFlashCoroutine != null)
                StopCoroutine(_damageFlashCoroutine);
            _damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
        }

        private IEnumerator DamageFlashCoroutine()
        {
            float originalWeight = _targetWeight;
            _targetWeight = 1f;
            _weightLerpSpeed = 20f;

            yield return new WaitForSeconds(damageFlashDuration * 0.3f);

            _targetWeight = originalWeight;
            _weightLerpSpeed = 4f;

            yield return new WaitForSeconds(damageFlashDuration * 0.7f);

            _weightLerpSpeed = 2f;
            _damageFlashCoroutine = null;
        }

        public void FreezeEffect(float duration)
        {
            if (_freezeCoroutine != null)
                StopCoroutine(_freezeCoroutine);
            _freezeCoroutine = StartCoroutine(FreezeCoroutine(duration));
        }

        private IEnumerator FreezeCoroutine(float duration)
        {
            float originalFov = _camera != null ? _camera.fieldOfView : _defaultFov;
            float targetFov = originalFov / freezeZoomAmount;
            float originalWeight = _targetWeight;

            _targetWeight = freezeDesaturation;
            _weightLerpSpeed = 10f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (_camera != null)
                {
                    float halfT = t < 0.5f ? t * 2f : (1f - t) * 2f;
                    _camera.fieldOfView = Mathf.Lerp(originalFov, targetFov, halfT);
                }

                yield return null;
            }

            if (_camera != null)
                _camera.fieldOfView = originalFov;

            _targetWeight = originalWeight;
            _weightLerpSpeed = 2f;
            _freezeCoroutine = null;
        }

        public void ScanlineEffect(bool enable)
        {
            _scanlineActive = enable;
        }

        public bool IsScanlineActive()
        {
            return _scanlineActive;
        }

        public void DirectorPresenceEffect(float intensity)
        {
            float clamped = Mathf.Clamp01(intensity);

            if (volume != null)
            {
                float baseWeight = _targetWeight;
                float presenceContribution = clamped * 0.3f;
                _targetWeight = Mathf.Min(1f, baseWeight + presenceContribution);
            }
        }

        public void LerpProfileWeight(float target, float speed)
        {
            _targetWeight = Mathf.Clamp01(target);
            _weightLerpSpeed = Mathf.Max(0.1f, speed);
        }

        public void ResetToNormal()
        {
            _targetWeight = 0f;
            _weightLerpSpeed = 2f;
            _scanlineActive = false;

            if (volume != null)
            {
                volume.weight = 0f;
                if (normalProfile != null)
                    volume.profile = normalProfile;
            }

            if (_camera != null)
                _camera.fieldOfView = _defaultFov;

            if (_damageFlashCoroutine != null)
            {
                StopCoroutine(_damageFlashCoroutine);
                _damageFlashCoroutine = null;
            }

            if (_freezeCoroutine != null)
            {
                StopCoroutine(_freezeCoroutine);
                _freezeCoroutine = null;
            }
        }
    }
}
