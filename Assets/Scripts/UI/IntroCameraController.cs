using UnityEngine;

namespace MimicFacility.UI
{
    public class IntroCameraController : MonoBehaviour
    {
        public enum Phase { FacilityExterior, Corridor, ControlRoom, TitleHold }

        [Header("Phase 1 -- Facility Exterior")]
        public Vector3 exteriorStart = new Vector3(0f, 3f, -20f);
        public Vector3 exteriorEnd = new Vector3(0f, 2.5f, -8f);
        public Vector3 exteriorLookAt = new Vector3(0f, 5f, 30f);
        public float exteriorPushDuration = 30f;

        [Header("Phase 3 -- Corridor (scene moves, camera stays)")]
        public Vector3 corridorCamPos = new Vector3(0f, 1.8f, 0f);
        public Vector3 corridorLookAt = new Vector3(0f, 1.6f, 30f);
        public float corridorScrollSpeed = 0.8f;
        public float corridorPushDuration = 26f;
        [Tooltip("The corridor scene root — moves backward to create infinite hallway effect")]
        public Transform corridorSceneRoot;

        [Header("Phase 4 -- AI Core Room")]
        public Vector3 controlRoomCenter = new Vector3(0f, 2f, 0f);
        public float orbitStartHeight = 4f;
        public float orbitEndHeight = 2.5f;
        public float orbitStartRadius = 5f;
        public float orbitEndRadius = 3f;
        public float orbitSpeed = 0.06f;
        public float orbitDuration = 20f;

        [Header("Phase 5 -- Title Hold")]
        public float titleZoomStart = 60f;
        public float titleZoomEnd = 40f;
        public float titleZoomDuration = 12f;

        [Header("Breathing Drift")]
        public float breathAmount = 0.02f;
        public float breathSpeed = 0.4f;
        public float breathVertical = 0.008f;

        [Header("Head Bob (Corridor)")]
        public float bobAmount = 0.03f;
        public float bobSpeed = 1.8f;

        [Header("Shake")]
        public float shakeDecay = 3f;

        [Header("Beat Pulse")]
        public float beatPulseAmount = 0.15f;
        public float beatPulseDecay = 4f;

        private Phase currentPhase = Phase.FacilityExterior;
        private float phaseTimer;
        private float orbitAngle;
        private float currentShake;
        private float currentBeatPulse;
        private Camera cam;
        private float baseFOV;

        void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = gameObject.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            baseFOV = cam.fieldOfView;

            transform.position = exteriorStart;
            transform.LookAt(exteriorLookAt);
        }

        public void SetPhase(Phase phase)
        {
            currentPhase = phase;
            phaseTimer = 0f;

            switch (phase)
            {
                case Phase.FacilityExterior:
                    transform.position = exteriorStart;
                    transform.LookAt(exteriorLookAt);
                    baseFOV = 65f;
                    break;
                case Phase.Corridor:
                    transform.position = corridorCamPos;
                    transform.LookAt(corridorLookAt);
                    baseFOV = 58f;
                    break;
                case Phase.ControlRoom:
                    orbitAngle = 0f;
                    baseFOV = 55f;
                    break;
                case Phase.TitleHold:
                    Vector3 dir = transform.position - controlRoomCenter;
                    orbitAngle = Mathf.Atan2(dir.z, dir.x);
                    baseFOV = titleZoomStart;
                    break;
            }

            if (cam != null) cam.fieldOfView = baseFOV;
        }

        public void TriggerShake(float amount)
        {
            currentShake = amount;
        }

        public void TriggerBeatPulse()
        {
            currentBeatPulse = beatPulseAmount;
        }

        void Update()
        {
            phaseTimer += Time.deltaTime;
            float t = Time.time;

            switch (currentPhase)
            {
                case Phase.FacilityExterior:
                    UpdateExterior(t);
                    break;
                case Phase.Corridor:
                    UpdateCorridor(t);
                    break;
                case Phase.ControlRoom:
                    UpdateControlRoom(t);
                    break;
                case Phase.TitleHold:
                    UpdateTitleHold(t);
                    break;
            }

            // Camera shake
            if (currentShake > 0.001f)
            {
                Vector3 shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(t * 25f, 0f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(0f, t * 25f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(t * 20f, t * 20f) - 0.5f)
                ) * currentShake * 0.05f;

                transform.position += shakeOffset;
                currentShake = Mathf.Lerp(currentShake, 0f, shakeDecay * Time.deltaTime);
            }

            // Beat pulse FOV
            if (currentBeatPulse > 0.001f)
            {
                if (cam != null) cam.fieldOfView += currentBeatPulse * 3f;
                currentBeatPulse = Mathf.Lerp(currentBeatPulse, 0f, beatPulseDecay * Time.deltaTime);
            }
        }

        // ── Phase 1: Slow push toward the facility ────────────────────
        void UpdateExterior(float t)
        {
            float progress = Mathf.Clamp01(phaseTimer / exteriorPushDuration);
            // Ease-in-out for cinematic feel: 3t^2 - 2t^3
            float eased = progress * progress * (3f - 2f * progress);

            Vector3 basePos = Vector3.Lerp(exteriorStart, exteriorEnd, eased);

            // Breathing drift
            float bx = Mathf.Sin(t * breathSpeed) * breathAmount;
            float by = Mathf.Sin(t * breathSpeed * 0.7f) * breathVertical;

            transform.position = basePos + new Vector3(bx, by, 0f);
            transform.LookAt(exteriorLookAt + new Vector3(bx * 0.5f, 0f, 0f));

            // Slow FOV creep — getting closer, more claustrophobic
            if (cam != null)
                cam.fieldOfView = Mathf.Lerp(65f, 58f, eased);
        }

        // ── Phase 3: Corridor scrolls backward — camera stays still ───
        // Creates an infinite hallway effect. The scene moves, not you.
        void UpdateCorridor(float t)
        {
            // Camera stays in place with subtle bob and breathing
            float bobX = Mathf.Sin(t * bobSpeed) * bobAmount;
            float bobY = Mathf.Abs(Mathf.Sin(t * bobSpeed * 2f)) * bobAmount * 0.6f;
            float bx = Mathf.Sin(t * breathSpeed * 1.2f) * breathAmount * 0.5f;

            transform.position = corridorCamPos + new Vector3(bobX + bx, bobY, 0f);

            float lookSway = Mathf.Sin(t * 0.3f) * 0.5f;
            transform.LookAt(corridorLookAt + new Vector3(lookSway, 0f, 0f));

            // Move the corridor scene backward — infinite hallway
            if (corridorSceneRoot != null)
            {
                corridorSceneRoot.position -= Vector3.forward * corridorScrollSpeed * Time.deltaTime;
            }

            // FOV tightens slowly
            float progress = Mathf.Clamp01(phaseTimer / corridorPushDuration);
            if (cam != null)
                cam.fieldOfView = Mathf.Lerp(58f, 50f, progress);
        }

        // ── Phase 4: Orbit the Director's control room ────────────────
        void UpdateControlRoom(float t)
        {
            float progress = Mathf.Clamp01(phaseTimer / orbitDuration);
            float eased = progress * progress * (3f - 2f * progress);

            orbitAngle += orbitSpeed * Time.deltaTime;

            float h = Mathf.Lerp(orbitStartHeight, orbitEndHeight, eased);
            float r = Mathf.Lerp(orbitStartRadius, orbitEndRadius, eased);

            float x = controlRoomCenter.x + Mathf.Cos(orbitAngle) * r;
            float z = controlRoomCenter.z + Mathf.Sin(orbitAngle) * r;

            // Breathing on the orbit
            float by = Mathf.Sin(t * breathSpeed) * breathVertical * 2f;

            transform.position = new Vector3(x, h + by, z);
            transform.LookAt(controlRoomCenter + Vector3.up * 1.5f);

            // FOV widens slightly to show the room
            if (cam != null)
                cam.fieldOfView = Mathf.Lerp(55f, 50f, eased);
        }

        // ── Phase 5: Title — keep orbiting, NO zoom, more dramatic ─────
        void UpdateTitleHold(float t)
        {
            // Keep orbiting at same speed — don't slow down or zoom
            orbitAngle += orbitSpeed * Time.deltaTime;

            float h = orbitEndHeight;
            float r = orbitEndRadius;

            float x = controlRoomCenter.x + Mathf.Cos(orbitAngle) * r;
            float z = controlRoomCenter.z + Mathf.Sin(orbitAngle) * r;

            // More dramatic breathing during title
            float by = Mathf.Sin(t * breathSpeed * 1.5f) * breathVertical * 4f;
            float bx = Mathf.Sin(t * breathSpeed * 0.8f) * breathAmount * 2f;

            transform.position = new Vector3(x + bx, h + by, z);
            transform.LookAt(controlRoomCenter + Vector3.up * 1.5f);

            // NO zoom — keep FOV constant
            if (cam != null)
                cam.fieldOfView = 50f;
        }
    }
}
