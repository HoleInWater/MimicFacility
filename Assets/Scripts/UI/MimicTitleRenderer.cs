using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MimicFacility.UI
{
    public class MimicTitleRenderer : MonoBehaviour
    {
        [Header("Style")]
        [SerializeField] private Color primaryColor = new Color(0.85f, 0.08f, 0.08f);
        [SerializeField] private Color glowColor = new Color(1f, 0.15f, 0.1f, 0.4f);
        [SerializeField] private Color glitchColor = new Color(0f, 0.9f, 0.9f, 0.6f);
        [SerializeField] private int textureWidth = 1024;
        [SerializeField] private int textureHeight = 256;

        [Header("Animation")]
        [SerializeField] private float drawDuration = 3f;
        [SerializeField] private float glitchIntensity = 0.3f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseAmount = 0.15f;
        [SerializeField] private float scanLineSpeed = 50f;

        private RawImage targetImage;
        private Texture2D titleTexture;
        private Color[] basePixels;
        private bool isDrawing;
        private float drawProgress;

        private void Awake()
        {
            targetImage = GetComponent<RawImage>();
            if (targetImage == null)
                targetImage = gameObject.AddComponent<RawImage>();

            titleTexture = GenerateTitleTexture();
            basePixels = titleTexture.GetPixels();
            targetImage.texture = titleTexture;
            targetImage.color = Color.white;
        }

        public void StartDrawing(float duration)
        {
            drawDuration = duration;
            StartCoroutine(DrawSequence());
        }

        private IEnumerator DrawSequence()
        {
            isDrawing = true;
            drawProgress = 0f;

            while (drawProgress < 1f)
            {
                drawProgress += Time.deltaTime / drawDuration;
                UpdateReveal(drawProgress);
                yield return null;
            }

            isDrawing = false;
        }

        private void Update()
        {
            if (!isDrawing && titleTexture != null)
            {
                ApplyLiveEffects();
            }
        }

        private void UpdateReveal(float progress)
        {
            int revealX = Mathf.RoundToInt(progress * textureWidth);
            var pixels = (Color[])basePixels.Clone();

            for (int y = 0; y < textureHeight; y++)
            for (int x = 0; x < textureWidth; x++)
            {
                int idx = y * textureWidth + x;
                if (x > revealX)
                {
                    pixels[idx] = Color.clear;
                }
                else if (x > revealX - 20)
                {
                    float edge = (float)(revealX - x) / 20f;
                    pixels[idx] *= edge;
                    // Glitch at the drawing edge
                    if (Random.value < glitchIntensity)
                    {
                        int offsetY = y + Random.Range(-3, 4);
                        if (offsetY >= 0 && offsetY < textureHeight)
                        {
                            int srcIdx = offsetY * textureWidth + x;
                            pixels[idx] = Color.Lerp(pixels[idx], glitchColor, 0.5f);
                        }
                    }
                }
            }

            titleTexture.SetPixels(pixels);
            titleTexture.Apply();
        }

        private void ApplyLiveEffects()
        {
            var pixels = (Color[])basePixels.Clone();
            float time = Time.time;

            // Pulse
            float pulse = 1f + Mathf.Sin(time * pulseSpeed) * pulseAmount;

            // Scan line
            int scanY = Mathf.RoundToInt((time * scanLineSpeed) % textureHeight);

            for (int y = 0; y < textureHeight; y++)
            for (int x = 0; x < textureWidth; x++)
            {
                int idx = y * textureWidth + x;
                if (pixels[idx].a < 0.01f) continue;

                pixels[idx] *= pulse;

                // Scan line
                if (Mathf.Abs(y - scanY) < 2)
                    pixels[idx] = Color.Lerp(pixels[idx], Color.white, 0.3f);

                // Random micro-glitch
                if (Random.value < 0.001f)
                    pixels[idx] = glitchColor;
            }

            titleTexture.SetPixels(pixels);
            titleTexture.Apply();
        }

        private Texture2D GenerateTitleTexture()
        {
            var tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            var pixels = new Color[textureWidth * textureHeight];

            // Clear
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // "MIMIC" — 5 letters, each gets a section
            float letterWidth = textureWidth / 5.5f;
            float startX = textureWidth * 0.05f;
            float topY = textureHeight * 0.85f;
            float botY = textureHeight * 0.15f;
            float letterH = topY - botY;
            float strokeW = letterWidth * 0.18f;

            DrawM(pixels, startX, botY, letterWidth, letterH, strokeW);
            DrawI(pixels, startX + letterWidth * 1.1f, botY, letterWidth * 0.4f, letterH, strokeW);
            DrawM(pixels, startX + letterWidth * 1.6f, botY, letterWidth, letterH, strokeW);
            DrawI(pixels, startX + letterWidth * 2.7f, botY, letterWidth * 0.4f, letterH, strokeW);
            DrawC(pixels, startX + letterWidth * 3.2f, botY, letterWidth, letterH, strokeW);

            // Glow pass — blur bright pixels outward
            var glowed = new Color[pixels.Length];
            System.Array.Copy(pixels, glowed, pixels.Length);

            for (int y = 2; y < textureHeight - 2; y++)
            for (int x = 2; x < textureWidth - 2; x++)
            {
                int idx = y * textureWidth + x;
                if (pixels[idx].a < 0.1f) continue;

                // Spread glow to neighbors
                for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= textureWidth || ny < 0 || ny >= textureHeight) continue;

                    int nIdx = ny * textureWidth + nx;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float falloff = 1f / (1f + dist * 2f);
                    Color glow = glowColor * falloff * pixels[idx].a;

                    glowed[nIdx] = new Color(
                        Mathf.Max(glowed[nIdx].r, glow.r),
                        Mathf.Max(glowed[nIdx].g, glow.g),
                        Mathf.Max(glowed[nIdx].b, glow.b),
                        Mathf.Max(glowed[nIdx].a, glow.a)
                    );
                }
            }

            // Distortion — slight vertical offset on some columns
            var distorted = new Color[glowed.Length];
            System.Array.Copy(glowed, distorted, glowed.Length);
            System.Random rng = new System.Random(77);
            for (int x = 0; x < textureWidth; x++)
            {
                int offset = rng.NextDouble() < 0.1 ? rng.Next(-2, 3) : 0;
                for (int y = 0; y < textureHeight; y++)
                {
                    int srcY = Mathf.Clamp(y + offset, 0, textureHeight - 1);
                    distorted[y * textureWidth + x] = glowed[srcY * textureWidth + x];
                }
            }

            // Horizontal scratch lines
            for (int i = 0; i < 8; i++)
            {
                int scratchY = rng.Next((int)botY, (int)topY);
                float alpha = (float)rng.NextDouble() * 0.15f;
                for (int x = 0; x < textureWidth; x++)
                {
                    int idx = scratchY * textureWidth + x;
                    distorted[idx] = Color.Lerp(distorted[idx], primaryColor * 0.5f, alpha);
                }
            }

            tex.SetPixels(distorted);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        // ═══════════════════════════════════════════════════════════════
        // LETTER DRAWING — thick strokes with rough edges
        // ═══════════════════════════════════════════════════════════════

        private void DrawM(Color[] pixels, float x, float y, float w, float h, float stroke)
        {
            // Left vertical
            FillRect(pixels, x, y, stroke, h);
            // Right vertical
            FillRect(pixels, x + w - stroke, y, stroke, h);
            // Left diagonal down to center
            DrawDiagonal(pixels, x + stroke, y + h, x + w / 2f, y + h * 0.5f, stroke * 0.8f);
            // Right diagonal up from center
            DrawDiagonal(pixels, x + w / 2f, y + h * 0.5f, x + w - stroke, y + h, stroke * 0.8f);
        }

        private void DrawI(Color[] pixels, float x, float y, float w, float h, float stroke)
        {
            // Top bar
            FillRect(pixels, x, y + h - stroke, w, stroke);
            // Bottom bar
            FillRect(pixels, x, y, w, stroke);
            // Center vertical
            FillRect(pixels, x + w / 2f - stroke / 2f, y, stroke, h);
        }

        private void DrawC(Color[] pixels, float x, float y, float w, float h, float stroke)
        {
            // Left vertical
            FillRect(pixels, x, y, stroke, h);
            // Top horizontal
            FillRect(pixels, x, y + h - stroke, w, stroke);
            // Bottom horizontal
            FillRect(pixels, x, y, w, stroke);
        }

        private void FillRect(Color[] pixels, float fx, float fy, float fw, float fh)
        {
            int x0 = Mathf.Max(0, Mathf.RoundToInt(fx));
            int y0 = Mathf.Max(0, Mathf.RoundToInt(fy));
            int x1 = Mathf.Min(textureWidth - 1, Mathf.RoundToInt(fx + fw));
            int y1 = Mathf.Min(textureHeight - 1, Mathf.RoundToInt(fy + fh));

            System.Random rng = new System.Random(x0 * 31 + y0 * 17);

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                // Rough edges — skip random pixels on borders
                bool isBorder = (x == x0 || x == x1 || y == y0 || y == y1);
                if (isBorder && rng.NextDouble() < 0.3) continue;

                float edgeDist = Mathf.Min(
                    Mathf.Min(x - x0, x1 - x),
                    Mathf.Min(y - y0, y1 - y)
                );
                float alpha = Mathf.Clamp01(edgeDist / 2f + 0.5f);

                int idx = y * textureWidth + x;
                Color c = primaryColor;
                c.a = alpha;

                // Slight color variation
                float noise = (float)rng.NextDouble() * 0.1f - 0.05f;
                c.r = Mathf.Clamp01(c.r + noise);
                c.g = Mathf.Clamp01(c.g + noise * 0.5f);

                pixels[idx] = Color.Lerp(pixels[idx], c, c.a);
            }
        }

        private void DrawDiagonal(Color[] pixels, float x0, float y0, float x1, float y1, float thickness)
        {
            int steps = Mathf.RoundToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)));
            if (steps == 0) return;

            int halfThick = Mathf.RoundToInt(thickness / 2f);

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                int cx = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int cy = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));

                for (int dy = -halfThick; dy <= halfThick; dy++)
                for (int dx = -halfThick; dx <= halfThick; dx++)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px < 0 || px >= textureWidth || py < 0 || py >= textureHeight) continue;

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > halfThick) continue;

                    float alpha = 1f - (dist / halfThick);
                    int idx = py * textureWidth + px;
                    Color c = primaryColor;
                    c.a = alpha;
                    pixels[idx] = Color.Lerp(pixels[idx], c, alpha);
                }
            }
        }
    }
}
