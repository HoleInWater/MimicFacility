using System.Collections;
using UnityEngine;
using Mirror;

namespace MimicFacility.Facility
{
    public class FacilityLight : NetworkBehaviour
    {
        [SerializeField] private string zoneTag;
        [SerializeField] private float defaultIntensity = 1.5f;
        [SerializeField] private Light lightComponent;
        [SerializeField] private MeshRenderer[] emissiveRenderers;

        [Header("Emissive")]
        [SerializeField] private Color emissiveOnColor = Color.white;
        [SerializeField] private Color emissiveOffColor = Color.black;

        [SyncVar(hook = nameof(OnIsOnChanged))]
        private bool isOn = true;

        public string ZoneTag => zoneTag;
        public bool IsOn => isOn;

        private Coroutine _flickerCoroutine;

        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            if (lightComponent == null)
                lightComponent = GetComponentInChildren<Light>();
        }

        private void Start()
        {
            ApplyLightState(isOn);
        }

        [Server]
        public void TurnOff()
        {
            isOn = false;
        }

        [Server]
        public void TurnOn()
        {
            isOn = true;
        }

        public void Flicker(float duration)
        {
            if (_flickerCoroutine != null)
                StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = StartCoroutine(FlickerCoroutine(duration));
        }

        private IEnumerator FlickerCoroutine(float duration)
        {
            bool previousState = isOn;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float intensity = Random.value > 0.5f ? defaultIntensity : 0f;
                if (lightComponent != null)
                    lightComponent.intensity = intensity;
                UpdateEmissiveMaterials(intensity > 0f);

                float wait = Random.Range(0.05f, 0.15f);
                elapsed += wait;
                yield return new WaitForSeconds(wait);
            }

            ApplyLightState(previousState);
            _flickerCoroutine = null;
        }

        public void FlickerOnce()
        {
            StartCoroutine(FlickerOnceCoroutine());
        }

        private IEnumerator FlickerOnceCoroutine()
        {
            if (lightComponent != null)
                lightComponent.intensity = 0f;
            UpdateEmissiveMaterials(false);

            yield return new WaitForSeconds(0.1f);

            ApplyLightState(isOn);
        }

        private void OnIsOnChanged(bool oldVal, bool newVal)
        {
            ApplyLightState(newVal);
        }

        private void ApplyLightState(bool on)
        {
            if (lightComponent != null)
            {
                lightComponent.enabled = on;
                lightComponent.intensity = on ? defaultIntensity : 0f;
            }

            UpdateEmissiveMaterials(on);
        }

        private void UpdateEmissiveMaterials(bool on)
        {
            if (emissiveRenderers == null) return;

            Color color = on ? emissiveOnColor : emissiveOffColor;
            foreach (var renderer in emissiveRenderers)
            {
                if (renderer == null) continue;
                foreach (var mat in renderer.materials)
                    mat.SetColor(EmissiveColorId, color);
            }
        }
    }
}
