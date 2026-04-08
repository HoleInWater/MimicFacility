using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MimicFacility.UI
{
    public class ScaryScreenFlash : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float minInterval = 45f;
        [SerializeField] private float maxInterval = 120f;
        [SerializeField] private float flashDuration = 0.15f;
        [SerializeField] private float holdDuration = 0.8f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        [Header("References")]
        [SerializeField] private Canvas scaryCanvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private RawImage monsterImage;

        [Header("Audio")]
        [SerializeField] private AudioSource scareAudio;
        [SerializeField] private AudioClip[] scareClips;

        [Header("Settings")]
        [SerializeField] private bool enableRandomScares = true;
        [SerializeField] private int minCorruptionToTrigger = 20;

        private bool isShowing;
        private float nextScareTime;

        private void Start()
        {
            if (scaryCanvas == null)
                CreateCanvas();

            Hide();
            ScheduleNext();
        }

        private void Update()
        {
            if (!enableRandomScares || isShowing) return;

            if (Time.time >= nextScareTime)
            {
                var corruption = FindObjectOfType<MimicFacility.AI.Persistence.CorruptionTracker>();
                if (corruption != null && corruption.CorruptionIndex < minCorruptionToTrigger)
                {
                    ScheduleNext();
                    return;
                }

                StartCoroutine(ShowScare());
            }
        }

        public void TriggerScare()
        {
            if (!isShowing)
                StartCoroutine(ShowScare());
        }

        private IEnumerator ShowScare()
        {
            isShowing = true;

            if (scareAudio != null && scareClips != null && scareClips.Length > 0)
            {
                var clip = scareClips[UnityEngine.Random.Range(0, scareClips.Length)];
                scareAudio.PlayOneShot(clip, 0.9f);
            }

            // Flash in
            scaryCanvas.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = elapsed / flashDuration;
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSecondsRealtime(holdDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - (elapsed / fadeOutDuration);
                yield return null;
            }

            Hide();
            isShowing = false;
            ScheduleNext();
        }

        private void Hide()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (scaryCanvas != null) scaryCanvas.gameObject.SetActive(false);
        }

        private void ScheduleNext()
        {
            nextScareTime = Time.time + UnityEngine.Random.Range(minInterval, maxInterval);
        }

        private void CreateCanvas()
        {
            var obj = new GameObject("ScaryScreenCanvas");
            obj.transform.SetParent(transform);

            scaryCanvas = obj.AddComponent<Canvas>();
            scaryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaryCanvas.sortingOrder = 999;

            obj.AddComponent<CanvasScaler>();
            canvasGroup = obj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Black background
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(obj.transform, false);
            backgroundImage = bgObj.AddComponent<Image>();
            backgroundImage.color = Color.black;
            var bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Monster face — procedural since we have no art assets
            var monsterObj = new GameObject("Monster");
            monsterObj.transform.SetParent(obj.transform, false);
            var monsterRT = monsterObj.AddComponent<RectTransform>();
            monsterRT.anchorMin = new Vector2(0.2f, 0.15f);
            monsterRT.anchorMax = new Vector2(0.8f, 0.85f);
            monsterRT.sizeDelta = Vector2.zero;

            monsterImage = monsterObj.AddComponent<RawImage>();
            monsterImage.texture = GenerateMonsterTexture(256, 256);
            monsterImage.color = Color.white;

            // Scare audio source
            if (scareAudio == null)
            {
                scareAudio = obj.AddComponent<AudioSource>();
                scareAudio.spatialBlend = 0f;
                scareAudio.playOnAwake = false;
            }
        }

        private Texture2D GenerateMonsterTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            // Start all black
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.black;

            float cx = width * 0.5f;
            float cy = height * 0.5f;

            // Skull-like face shape — pale oval
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = (x - cx) / (width * 0.3f);
                float ny = (y - cy) / (height * 0.4f);
                float d = nx * nx + ny * ny;

                if (d < 1f)
                {
                    float fade = 1f - d;
                    float val = fade * 0.15f;
                    pixels[y * width + x] = new Color(val, val * 0.95f, val * 0.9f);
                }
            }

            // Left eye — dark hollow
            DrawEyeSocket(pixels, width, height, cx - width * 0.12f, cy + height * 0.08f, width * 0.08f, height * 0.06f);
            // Right eye — dark hollow
            DrawEyeSocket(pixels, width, height, cx + width * 0.12f, cy + height * 0.08f, width * 0.08f, height * 0.06f);

            // Eye glints — tiny bright dots
            DrawGlint(pixels, width, height, cx - width * 0.12f, cy + height * 0.08f, 2);
            DrawGlint(pixels, width, height, cx + width * 0.12f, cy + height * 0.08f, 2);

            // Mouth — wide dark gash
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float mx = (x - cx) / (width * 0.2f);
                float my = (y - (cy - height * 0.15f)) / (height * 0.03f);
                float mouthD = mx * mx + my * my;

                // Jagged edge using sin
                float jagged = Mathf.Sin(mx * 12f) * 0.3f;
                if (mouthD + jagged < 1f)
                {
                    pixels[y * width + x] = new Color(0.02f, 0f, 0f);
                }
            }

            // Noise/distortion — scattered bright pixels for creepy static effect
            System.Random rng = new System.Random(42);
            for (int i = 0; i < 800; i++)
            {
                int px = rng.Next(0, width);
                int py = rng.Next(0, height);
                float brightness = (float)rng.NextDouble() * 0.08f;
                int idx = py * width + px;
                pixels[idx] = new Color(
                    pixels[idx].r + brightness,
                    pixels[idx].g + brightness * 0.8f,
                    pixels[idx].b + brightness * 0.7f
                );
            }

            // Vertical scan lines
            for (int x = 0; x < width; x += 3)
            for (int y = 0; y < height; y++)
            {
                int idx = y * width + x;
                pixels[idx] *= 0.85f;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        private void DrawEyeSocket(Color[] pixels, int w, int h, float ecx, float ecy, float ew, float eh)
        {
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float ex = (x - ecx) / ew;
                float ey = (y - ecy) / eh;
                float d = ex * ex + ey * ey;
                if (d < 1f)
                {
                    float depth = (1f - d) * 0.8f;
                    int idx = y * w + x;
                    pixels[idx] = new Color(
                        pixels[idx].r * (1f - depth),
                        pixels[idx].g * (1f - depth),
                        pixels[idx].b * (1f - depth)
                    );
                }
            }
        }

        private void DrawGlint(Color[] pixels, int w, int h, float gx, float gy, int radius)
        {
            int ix = Mathf.RoundToInt(gx);
            int iy = Mathf.RoundToInt(gy);
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                int px = ix + dx;
                int py = iy + dy;
                if (px < 0 || px >= w || py < 0 || py >= h) continue;
                if (dx * dx + dy * dy > radius * radius) continue;
                pixels[py * w + px] = new Color(0.9f, 0.85f, 0.8f);
            }
        }
    }
}
