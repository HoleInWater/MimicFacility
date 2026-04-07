using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MimicFacility.Facility
{
    public class EnvironmentManager : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField] private Material cleanMaterial;
        [SerializeField] private Material corruptedMaterial;
        [SerializeField] private MeshRenderer[] environmentRenderers;

        [Header("Lighting")]
        [SerializeField] private List<Light> ambientLights;
        [SerializeField] private Color normalAmbientColor = new Color(0.9f, 0.95f, 1f);
        [SerializeField] private Color corruptedAmbientColor = new Color(0.6f, 0.2f, 0.15f);

        [Header("Audio")]
        [SerializeField] private AudioSource ambientAudioSource;
        [SerializeField] private AudioClip normalAmbience;
        [SerializeField] private AudioClip corruptedAmbience;
        [SerializeField] private AudioSource secondaryAudioSource;

        [Header("Fog")]
        [SerializeField] private float baseFogDensity = 0.005f;
        [SerializeField] private float maxFogDensity = 0.08f;
        [SerializeField] private Color normalFogColor = Color.gray;
        [SerializeField] private Color corruptedFogColor = new Color(0.3f, 0.15f, 0.1f);

        [Header("Post Processing")]
        [SerializeField] private Volume postProcessVolume;

        [Header("Round Visuals")]
        [SerializeField] private ParticleSystem ambientSporeParticles;
        [SerializeField] private GameObject[] bloodDecals;
        [SerializeField] private AudioClip[] ambientCreaks;
        [SerializeField] private AudioSource creakSource;

        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private float _currentCorruptionNormalized;
        private int _currentRound;

        public void UpdateEnvironment(int corruptionIndex)
        {
            float t = Mathf.Clamp01(corruptionIndex / 100f);
            _currentCorruptionNormalized = t;

            UpdateMaterials(t);
            UpdateAmbientLighting(t);
            UpdateAmbientAudio(t);
            UpdateFog(t);
            UpdatePostProcess(t);

            CheckCorruptionMilestones(corruptionIndex);
        }

        private void UpdateMaterials(float t)
        {
            if (environmentRenderers == null || cleanMaterial == null || corruptedMaterial == null) return;

            foreach (var renderer in environmentRenderers)
            {
                if (renderer == null) continue;
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty(BlendId))
                        mat.SetFloat(BlendId, t);
                }
            }
        }

        private void UpdateAmbientLighting(float t)
        {
            Color ambient = Color.Lerp(normalAmbientColor, corruptedAmbientColor, t);
            RenderSettings.ambientLight = ambient;

            if (ambientLights == null) return;
            foreach (var light in ambientLights)
            {
                if (light != null)
                    light.color = ambient;
            }
        }

        private void UpdateAmbientAudio(float t)
        {
            if (ambientAudioSource == null) return;

            if (t < 0.1f)
            {
                if (ambientAudioSource.clip != normalAmbience)
                {
                    ambientAudioSource.clip = normalAmbience;
                    ambientAudioSource.Play();
                }
                ambientAudioSource.volume = 1f;
                if (secondaryAudioSource != null)
                    secondaryAudioSource.volume = 0f;
            }
            else
            {
                if (ambientAudioSource.clip != normalAmbience)
                {
                    ambientAudioSource.clip = normalAmbience;
                    ambientAudioSource.Play();
                }
                ambientAudioSource.volume = 1f - t;

                if (secondaryAudioSource != null)
                {
                    if (secondaryAudioSource.clip != corruptedAmbience)
                    {
                        secondaryAudioSource.clip = corruptedAmbience;
                        secondaryAudioSource.Play();
                    }
                    secondaryAudioSource.volume = t;
                }
            }
        }

        private void UpdateFog(float t)
        {
            RenderSettings.fog = true;
            RenderSettings.fogDensity = Mathf.Lerp(baseFogDensity, maxFogDensity, t);
            RenderSettings.fogColor = Color.Lerp(normalFogColor, corruptedFogColor, t);
        }

        private void UpdatePostProcess(float t)
        {
            if (postProcessVolume != null)
                postProcessVolume.weight = t;
        }

        public void ApplyRoundChanges(int round)
        {
            _currentRound = round;

            switch (round)
            {
                case 1:
                    if (ambientSporeParticles != null)
                        ambientSporeParticles.Stop();
                    SetBloodDecalsActive(false);
                    break;

                case 2:
                    var facilityControl = FindObjectOfType<FacilityControlSystem>();
                    if (facilityControl != null)
                    {
                        var lights = FindObjectsOfType<FacilityLight>();
                        foreach (var light in lights)
                        {
                            if (Random.value < 0.3f)
                                light.Flicker(Random.Range(2f, 5f));
                        }
                    }

                    if (ambientSporeParticles != null)
                    {
                        var emission = ambientSporeParticles.emission;
                        emission.rateOverTime = 5f;
                        ambientSporeParticles.Play();
                    }
                    break;

                case 3:
                    SetBloodDecalsActive(true);

                    if (ambientSporeParticles != null)
                    {
                        var emission = ambientSporeParticles.emission;
                        emission.rateOverTime = 30f;
                        ambientSporeParticles.Play();
                    }

                    if (creakSource != null && ambientCreaks != null && ambientCreaks.Length > 0)
                    {
                        InvokeRepeating(nameof(PlayRandomCreak), 2f, Random.Range(8f, 15f));
                    }
                    break;
            }
        }

        private void CheckCorruptionMilestones(int corruption)
        {
            if (corruption >= 25 && corruption < 26)
            {
                var lights = FindObjectsOfType<FacilityLight>();
                foreach (var light in lights)
                    light.FlickerOnce();
            }

            if (corruption >= 50 && corruption < 51)
            {
                if (ambientLights != null)
                {
                    foreach (var light in ambientLights)
                    {
                        if (light != null)
                            light.color = Color.red;
                    }
                }
            }

            if (corruption >= 75 && corruption < 76)
            {
                var doors = FindObjectsOfType<FacilityDoor>();
                foreach (var door in doors)
                {
                    if (Random.value < 0.2f)
                        door.Lock();
                }
            }
        }

        private void SetBloodDecalsActive(bool active)
        {
            if (bloodDecals == null) return;
            foreach (var decal in bloodDecals)
            {
                if (decal != null)
                    decal.SetActive(active);
            }
        }

        private void PlayRandomCreak()
        {
            if (creakSource == null || ambientCreaks == null || ambientCreaks.Length == 0) return;
            creakSource.PlayOneShot(ambientCreaks[Random.Range(0, ambientCreaks.Length)]);
        }
    }
}
