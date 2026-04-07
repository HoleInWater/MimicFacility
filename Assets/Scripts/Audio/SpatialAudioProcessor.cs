using System;
using UnityEngine;

namespace MimicFacility.Audio
{
    [Serializable]
    public class SpatialAudioParams
    {
        public float distance;
        public float falloffCoefficient = 1f;
        public float occlusionDistance;
        public float occlusionAlpha = 0.5f;
        public float theta;
        public float phi;
        public float listenerVelocity;
        public float sourceVelocity;
        public float[] reflectionDistances = Array.Empty<float>();
    }

    [Serializable]
    public class SpatialAudioResult
    {
        public float leftVolume;
        public float rightVolume;
        public float interauralTimeDelay;
        public float dopplerFactor;
        public float totalAttenuation;
        public float[] reflectionLevels = Array.Empty<float>();
        public float[] reflectionDelays = Array.Empty<float>();
    }

    public class SpatialAudioProcessor : MonoBehaviour
    {
        public const float SPEED_OF_SOUND = 343f;
        public const float HEAD_RADIUS = 0.0875f;
        public const float DEFAULT_FALLOFF = 1f;
        public const float DEFAULT_OCCLUSION_ALPHA = 0.5f;

        [SerializeField] private float reflectionDecay = 0.6f;
        [SerializeField] private LayerMask occlusionMask = ~0;
        [SerializeField] private float maxReflectionDistance = 50f;

        private static readonly Vector3[] ReflectionDirections =
        {
            Vector3.right, Vector3.left,
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back
        };

        public SpatialAudioResult ProcessSpatialAudio(SpatialAudioParams p)
        {
            var result = new SpatialAudioResult();

            float d = Mathf.Max(p.distance, 0.001f);
            float k = p.falloffCoefficient;

            float distanceAtten = 1f / (1f + k * d * d);
            float occlusionAtten = Mathf.Exp(-p.occlusionAlpha * p.occlusionDistance);
            float baseAtten = distanceAtten * occlusionAtten;

            float thetaRad = p.theta * Mathf.Deg2Rad;
            float itd = HEAD_RADIUS * Mathf.Sin(thetaRad) / SPEED_OF_SOUND;
            result.interauralTimeDelay = itd;

            float cPlusVr = SPEED_OF_SOUND + p.listenerVelocity;
            float cPlusVs = SPEED_OF_SOUND + p.sourceVelocity;
            float doppler = Mathf.Clamp(cPlusVr / Mathf.Max(cPlusVs, 0.01f), 0.5f, 2f);
            result.dopplerFactor = doppler;

            float phiRad = p.phi * Mathf.Deg2Rad;
            float leftHRTF = ComputeHRTF(thetaRad, phiRad, isLeft: true);
            float rightHRTF = ComputeHRTF(thetaRad, phiRad, isLeft: false);

            float directLeft = baseAtten * doppler * leftHRTF;
            float directRight = baseAtten * doppler * rightHRTF;

            int refCount = p.reflectionDistances != null ? p.reflectionDistances.Length : 0;
            result.reflectionLevels = new float[refCount];
            result.reflectionDelays = new float[refCount];

            float reflectionSumLeft = 0f;
            float reflectionSumRight = 0f;

            for (int i = 0; i < refCount; i++)
            {
                float rd = Mathf.Max(p.reflectionDistances[i], 0.001f);
                float refDelay = rd / SPEED_OF_SOUND;
                float refLevel = (1f / (1f + k * rd * rd)) * reflectionDecay;

                result.reflectionDelays[i] = refDelay;
                result.reflectionLevels[i] = refLevel;

                reflectionSumLeft += refLevel * 0.7f;
                reflectionSumRight += refLevel * 0.7f;
            }

            result.leftVolume = Mathf.Clamp01(directLeft + reflectionSumLeft);
            result.rightVolume = Mathf.Clamp01(directRight + reflectionSumRight);
            result.totalAttenuation = baseAtten * doppler;

            return result;
        }

        public SpatialAudioParams ComputeParamsFromTransforms(Transform listener, Transform source, LayerMask mask)
        {
            var p = new SpatialAudioParams();
            Vector3 listenerPos = listener.position;
            Vector3 sourcePos = source.position;
            Vector3 toSource = sourcePos - listenerPos;
            float dist = toSource.magnitude;
            p.distance = dist;
            p.falloffCoefficient = DEFAULT_FALLOFF;
            p.occlusionAlpha = DEFAULT_OCCLUSION_ALPHA;

            Vector3 flatDir = new Vector3(toSource.x, 0f, toSource.z);
            Vector3 flatFwd = new Vector3(listener.forward.x, 0f, listener.forward.z);
            p.theta = Vector3.SignedAngle(flatFwd, flatDir, Vector3.up);

            if (dist > 0.001f)
            {
                float vertAngle = Mathf.Asin(Mathf.Clamp(toSource.y / dist, -1f, 1f)) * Mathf.Rad2Deg;
                p.phi = vertAngle;
            }

            p.occlusionDistance = ComputeOcclusionDistance(listenerPos, sourcePos, mask);

            var rbListener = listener.GetComponent<Rigidbody>();
            var rbSource = source.GetComponent<Rigidbody>();
            if (rbListener != null && rbSource != null && dist > 0.001f)
            {
                Vector3 dir = toSource.normalized;
                p.listenerVelocity = Vector3.Dot(rbListener.linearVelocity, dir);
                p.sourceVelocity = Vector3.Dot(rbSource.linearVelocity, dir);
            }

            p.reflectionDistances = ComputeReflectionDistances(sourcePos, mask);

            return p;
        }

        private float ComputeOcclusionDistance(Vector3 from, Vector3 to, LayerMask mask)
        {
            Vector3 dir = to - from;
            float maxDist = dir.magnitude;
            if (maxDist < 0.01f) return 0f;

            RaycastHit[] hits = Physics.RaycastAll(from, dir.normalized, maxDist, mask);
            if (hits == null || hits.Length == 0) return 0f;

            float totalThickness = 0f;
            foreach (var hit in hits)
            {
                Collider col = hit.collider;
                if (col == null) continue;

                float thickness = EstimateColliderThickness(col, dir.normalized, hit.point);
                totalThickness += thickness;
            }
            return totalThickness;
        }

        private float EstimateColliderThickness(Collider col, Vector3 direction, Vector3 entryPoint)
        {
            Vector3 exitCheck = entryPoint + direction * 10f;
            Ray reverseRay = new Ray(exitCheck, -direction);

            if (col.Raycast(reverseRay, out RaycastHit exitHit, 20f))
            {
                float thickness = Vector3.Distance(entryPoint, exitHit.point);
                return Mathf.Max(thickness, 0.1f);
            }

            return 0.3f;
        }

        private float[] ComputeReflectionDistances(Vector3 sourcePos, LayerMask mask)
        {
            float[] distances = new float[ReflectionDirections.Length];
            for (int i = 0; i < ReflectionDirections.Length; i++)
            {
                if (Physics.Raycast(sourcePos, ReflectionDirections[i], out RaycastHit hit,
                    maxReflectionDistance, mask))
                {
                    distances[i] = hit.distance;
                }
                else
                {
                    distances[i] = maxReflectionDistance;
                }
            }
            return distances;
        }

        public void ApplyToAudioSource(AudioSource source, SpatialAudioResult result)
        {
            if (source == null) return;

            float avgVolume = (result.leftVolume + result.rightVolume) * 0.5f;
            source.volume = Mathf.Clamp01(avgVolume);

            float pan = 0f;
            float totalVol = result.leftVolume + result.rightVolume;
            if (totalVol > 0.001f)
                pan = (result.rightVolume - result.leftVolume) / totalVol;

            source.panStereo = Mathf.Clamp(pan, -1f, 1f);
            source.pitch = Mathf.Clamp(result.dopplerFactor, 0.5f, 2f);
        }

        private float ComputeHRTF(float thetaRad, float phiRad, bool isLeft)
        {
            float angle = isLeft ? (Mathf.PI * 0.5f + thetaRad) : (Mathf.PI * 0.5f - thetaRad);
            float horizontal = (1f + Mathf.Cos(angle)) * 0.5f;
            float elevation = 1f - 0.1f * Mathf.Abs(Mathf.Sin(phiRad));
            return Mathf.Clamp01(horizontal * elevation);
        }
    }
}
