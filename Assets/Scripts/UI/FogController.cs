using UnityEngine;

namespace MimicFacility.UI
{
    /// <summary>
    /// Runtime fog controller — adjust fog from the Inspector or via script.
    /// Attach to any GameObject in the scene.
    /// </summary>
    [ExecuteInEditMode]
    public class FogController : MonoBehaviour
    {
        [Header("Fog Toggle")]
        public bool enableFog = true;

        [Header("Fog Type")]
        public FogMode fogMode = FogMode.Linear;

        [Header("Linear Fog")]
        [Range(0f, 100f)] public float fogStart = 25f;
        [Range(10f, 500f)] public float fogEnd = 200f;

        [Header("Exponential Fog")]
        [Range(0.001f, 0.2f)] public float fogDensity = 0.03f;

        [Header("Fog Color")]
        public Color fogColor = new Color(0.01f, 0.01f, 0.015f);

        [Header("Ambient Light")]
        public Color ambientColor = new Color(0.06f, 0.06f, 0.08f);

        [Header("Live Update")]
        public bool updateEveryFrame = true;

        void Start()
        {
            ApplyFog();
        }

        void Update()
        {
            if (updateEveryFrame)
                ApplyFog();
        }

        public void ApplyFog()
        {
            RenderSettings.fog = enableFog;
            RenderSettings.fogMode = fogMode;

            if (fogMode == FogMode.Linear)
            {
                RenderSettings.fogStartDistance = fogStart;
                RenderSettings.fogEndDistance = fogEnd;
            }
            else
            {
                RenderSettings.fogDensity = fogDensity;
            }

            RenderSettings.fogColor = fogColor;
            RenderSettings.ambientLight = ambientColor;
        }

        public void SetLinear(float start, float end)
        {
            fogMode = FogMode.Linear;
            fogStart = start;
            fogEnd = end;
            ApplyFog();
        }

        public void SetExponential(float density)
        {
            fogMode = FogMode.ExponentialSquared;
            fogDensity = density;
            ApplyFog();
        }
    }
}
