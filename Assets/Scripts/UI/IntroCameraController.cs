using UnityEngine;

namespace MimicFacility.UI
{
    public class IntroCameraController : MonoBehaviour
    {
        public enum Phase { FacilityExterior, Corridor, ControlRoom, TitleHold }

        [Header("Phase 1 -- Facility Exterior (static shot)")]
        public Vector3 exteriorPosition = new Vector3(0f, 3f, -15f);
        public Vector3 exteriorLookAt = new Vector3(0f, 4f, 30f);

        [Header("Phase 3 -- Corridor (static shot)")]
        public Vector3 corridorPosition = new Vector3(0f, 1.8f, -3f);
        public Vector3 corridorLookAt = new Vector3(0f, 1.6f, 20f);

        [Header("Phase 4 -- Control Room (slow orbit)")]
        public Vector3 controlRoomCenter = new Vector3(0f, 0f, 0f);
        public float orbitHeight = 8f;
        public float orbitRadius = 6f;
        public float orbitSpeed = 0.04f;

        [Header("Phase 5 -- Title Hold")]
        public float titleZoomStart = 55f;
        public float titleZoomEnd = 42f;
        public float titleZoomDuration = 8f;

        [Header("Subtle Drift (all phases)")]
        public float driftAmount = 0.005f;
        public float driftSpeed = 0.12f;

        [Header("Shake (horror)")]
        public float shakeAmount = 0f;
        public float shakeDecay = 2f;

        private Phase currentPhase = Phase.FacilityExterior;
        private float phaseTimer;
        private float orbitAngle;
        private float currentShake;
        private Camera cam;

        void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = gameObject.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            transform.position = exteriorPosition;
            transform.LookAt(exteriorLookAt);
        }

        public void SetPhase(Phase phase)
        {
            currentPhase = phase;
            phaseTimer = 0f;

            switch (phase)
            {
                case Phase.FacilityExterior:
                    transform.position = exteriorPosition;
                    transform.LookAt(exteriorLookAt);
                    break;
                case Phase.Corridor:
                    transform.position = corridorPosition;
                    transform.LookAt(corridorLookAt);
                    break;
                case Phase.ControlRoom:
                    orbitAngle = 0f;
                    UpdateOrbitPosition();
                    break;
                case Phase.TitleHold:
                    Vector3 dir = transform.position - controlRoomCenter;
                    orbitAngle = Mathf.Atan2(dir.z, dir.x);
                    break;
            }
        }

        public void TriggerShake(float amount)
        {
            currentShake = amount;
        }

        void Update()
        {
            phaseTimer += Time.deltaTime;

            switch (currentPhase)
            {
                case Phase.FacilityExterior:
                    ApplyDrift(exteriorPosition, exteriorLookAt);
                    break;
                case Phase.Corridor:
                    ApplyDrift(corridorPosition, corridorLookAt);
                    break;
                case Phase.ControlRoom:
                    orbitAngle += orbitSpeed * Time.deltaTime;
                    UpdateOrbitPosition();
                    break;
                case Phase.TitleHold:
                    orbitAngle += orbitSpeed * 0.5f * Time.deltaTime;
                    UpdateOrbitPosition();
                    if (cam != null)
                    {
                        float t = Mathf.Clamp01(phaseTimer / titleZoomDuration);
                        cam.fieldOfView = Mathf.Lerp(titleZoomStart, titleZoomEnd, t);
                    }
                    break;
            }

            if (currentShake > 0.001f)
            {
                transform.position += Random.insideUnitSphere * currentShake * 0.01f;
                currentShake = Mathf.Lerp(currentShake, 0f, shakeDecay * Time.deltaTime);
            }
        }

        void UpdateOrbitPosition()
        {
            float descentT = Mathf.Clamp01(phaseTimer / 20f);
            float h = Mathf.Lerp(orbitHeight, orbitHeight * 0.8f, descentT);
            float r = Mathf.Lerp(orbitRadius, orbitRadius * 0.85f, descentT);

            float x = controlRoomCenter.x + Mathf.Cos(orbitAngle) * r;
            float z = controlRoomCenter.z + Mathf.Sin(orbitAngle) * r;
            transform.position = new Vector3(x, h, z);
            transform.LookAt(controlRoomCenter + Vector3.up * 2f);
        }

        void ApplyDrift(Vector3 basePos, Vector3 lookAt)
        {
            float t = Time.time;
            float dx = Mathf.Sin(t * driftSpeed) * driftAmount;
            float dy = Mathf.Sin(t * driftSpeed * 0.7f) * driftAmount * 0.5f;
            transform.position = basePos + new Vector3(dx, dy, 0f);
            transform.LookAt(lookAt);
        }
    }
}
