using UnityEngine;

namespace MimicFacility.UI
{
    public class IntroSearchlightSweep : MonoBehaviour
    {
        [SerializeField] private float sweepSpeed = 15f;
        [SerializeField] private float sweepAngle = 40f;
        [SerializeField] private float startAngle = 50f;

        private float baseY;

        void Start()
        {
            baseY = transform.eulerAngles.y;
        }

        void Update()
        {
            float sweep = Mathf.Sin(Time.time * sweepSpeed * Mathf.Deg2Rad) * sweepAngle;
            transform.rotation = Quaternion.Euler(startAngle, baseY + sweep, 0f);
        }
    }

    public class IntroWarningLightBlink : MonoBehaviour
    {
        [SerializeField] private float onDuration = 0.8f;
        [SerializeField] private float offDuration = 1.5f;
        [SerializeField] private float fadeSpeed = 4f;

        private Light lightComp;
        private float maxIntensity;
        private float timer;
        private bool isOn;

        void Start()
        {
            lightComp = GetComponent<Light>();
            if (lightComp != null)
                maxIntensity = lightComp.intensity;
            timer = onDuration;
            isOn = true;
        }

        void Update()
        {
            if (lightComp == null) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                isOn = !isOn;
                timer = isOn ? onDuration : offDuration;
            }

            float target = isOn ? maxIntensity : 0f;
            lightComp.intensity = Mathf.Lerp(lightComp.intensity, target, fadeSpeed * Time.deltaTime);
        }
    }

    public class IntroLightFlicker : MonoBehaviour
    {
        [SerializeField] private float minIntensity = 0.1f;
        [SerializeField] private float maxIntensity = 1f;
        [SerializeField] private float flickerSpeed = 8f;
        [SerializeField] private float smoothness = 5f;
        [SerializeField] private bool isBroken;

        private Light lightComp;
        private float targetIntensity;
        private float noiseOffset;

        void Start()
        {
            lightComp = GetComponent<Light>();
            noiseOffset = Random.Range(0f, 100f);
            if (lightComp != null && isBroken)
                maxIntensity = lightComp.intensity * 0.4f;
        }

        void Update()
        {
            if (lightComp == null) return;

            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + noiseOffset, noiseOffset);

            if (isBroken)
            {
                // Broken lights mostly off with occasional sparks
                bool spark = Random.value < 0.02f;
                targetIntensity = spark ? maxIntensity * 2f : noise * minIntensity;
            }
            else
            {
                // Working lights have subtle flicker
                targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
            }

            lightComp.intensity = Mathf.Lerp(lightComp.intensity, targetIntensity, smoothness * Time.deltaTime);
        }
    }

    public class IntroDoorCreak : MonoBehaviour
    {
        [SerializeField] private float creakAngle = 8f;
        [SerializeField] private float creakSpeed = 0.3f;
        [SerializeField] private float creakDelay = 5f;

        private Quaternion baseRotation;
        private float timer;
        private bool creaking;
        private float creakProgress;

        void Start()
        {
            baseRotation = transform.localRotation;
            timer = creakDelay + Random.Range(0f, 3f);
        }

        void Update()
        {
            timer -= Time.deltaTime;

            if (!creaking && timer <= 0f)
            {
                creaking = true;
                creakProgress = 0f;
            }

            if (creaking)
            {
                creakProgress += Time.deltaTime * creakSpeed;

                // Swing out, pause, swing back
                float angle;
                if (creakProgress < 1f)
                    angle = Mathf.Sin(creakProgress * Mathf.PI) * creakAngle;
                else
                {
                    angle = 0f;
                    creaking = false;
                    timer = creakDelay + Random.Range(0f, 5f);
                }

                transform.localRotation = baseRotation * Quaternion.Euler(0f, angle, 0f);
            }
        }
    }

    public class IntroWheelchairRock : MonoBehaviour
    {
        [SerializeField] private float rockAmount = 3f;
        [SerializeField] private float rockSpeed = 0.5f;

        private Vector3 basePos;
        private float offset;

        void Start()
        {
            basePos = transform.localPosition;
            offset = Random.Range(0f, 10f);
        }

        void Update()
        {
            float rock = Mathf.Sin((Time.time + offset) * rockSpeed) * rockAmount * 0.01f;
            transform.localPosition = basePos + new Vector3(rock, 0f, rock * 0.5f);
            transform.localRotation = Quaternion.Euler(0f, 0f, rock * 10f);
        }
    }

    public class IntroPaperFloat : MonoBehaviour
    {
        [SerializeField] private float floatHeight = 0.02f;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float driftAmount = 0.005f;

        private Vector3 basePos;
        private float offset;

        void Start()
        {
            basePos = transform.localPosition;
            offset = Random.Range(0f, 100f);
        }

        void Update()
        {
            float t = Time.time + offset;
            float y = Mathf.Sin(t * floatSpeed) * floatHeight;
            float x = Mathf.Sin(t * floatSpeed * 0.7f) * driftAmount;
            float z = Mathf.Cos(t * floatSpeed * 0.4f) * driftAmount;
            transform.localPosition = basePos + new Vector3(x, y, z);
            transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.2f) * 5f, 0f);
        }
    }

    public class IntroVentBreathe : MonoBehaviour
    {
        [SerializeField] private float breatheSpeed = 0.8f;
        [SerializeField] private float breatheAmount = 0.1f;
        [SerializeField] private float pushInterval = 8f;
        [SerializeField] private float pushDuration = 0.5f;

        private Vector3 baseScale;
        private float pushTimer;
        private float pushProgress = -1f;

        void Start()
        {
            baseScale = transform.localScale;
            pushTimer = pushInterval + Random.Range(0f, 3f);
        }

        void Update()
        {
            // Subtle breathing
            float breathe = 1f + Mathf.Sin(Time.time * breatheSpeed) * breatheAmount * 0.1f;
            transform.localScale = baseScale * breathe;

            // Occasional push — like something inside tried to get out
            pushTimer -= Time.deltaTime;
            if (pushTimer <= 0f && pushProgress < 0f)
            {
                pushProgress = 0f;
            }

            if (pushProgress >= 0f)
            {
                pushProgress += Time.deltaTime / pushDuration;
                float push = Mathf.Sin(pushProgress * Mathf.PI) * breatheAmount * 0.5f;
                transform.localPosition += Vector3.down * push * Time.deltaTime;

                if (pushProgress >= 1f)
                {
                    pushProgress = -1f;
                    pushTimer = pushInterval + Random.Range(0f, 5f);
                }
            }
        }
    }

    public class IntroScreenGlitch : MonoBehaviour
    {
        [SerializeField] private float glitchInterval = 3f;
        [SerializeField] private float glitchDuration = 0.15f;

        private Renderer rend;
        private Color baseEmission;
        private float timer;
        private float glitchTimer;
        private bool isGlitching;

        void Start()
        {
            rend = GetComponent<Renderer>();
            if (rend != null && rend.material.HasProperty("_EmissionColor"))
                baseEmission = rend.material.GetColor("_EmissionColor");
            timer = glitchInterval + Random.Range(0f, 2f);
        }

        void Update()
        {
            if (rend == null) return;

            timer -= Time.deltaTime;
            if (timer <= 0f && !isGlitching)
            {
                isGlitching = true;
                glitchTimer = glitchDuration;
            }

            if (isGlitching)
            {
                glitchTimer -= Time.deltaTime;
                // Random emission color flicker
                Color glitch = new Color(
                    Random.Range(0f, 0.3f),
                    Random.Range(0f, 0.3f),
                    Random.Range(0f, 0.1f)
                );
                if (rend.material.HasProperty("_EmissionColor"))
                    rend.material.SetColor("_EmissionColor", glitch);

                if (glitchTimer <= 0f)
                {
                    isGlitching = false;
                    timer = glitchInterval + Random.Range(0f, 4f);
                    if (rend.material.HasProperty("_EmissionColor"))
                        rend.material.SetColor("_EmissionColor", baseEmission);
                }
            }
            else
            {
                // Subtle pulse when not glitching
                float pulse = 1f + Mathf.Sin(Time.time * 2f) * 0.15f;
                if (rend.material.HasProperty("_EmissionColor"))
                    rend.material.SetColor("_EmissionColor", baseEmission * pulse);
            }
        }
    }
}
