using System;
using UnityEngine;

namespace MimicFacility.Audio
{
    /// <summary>
    /// Processes Director TTS audio to match historical AI voice eras.
    /// The Director's voice evolves through 5 eras of AI history:
    ///   1. IBM 7094 (1961) — first computer to sing. Buzzy vocoder, monotone.
    ///   2. HAL 9000 (1968) — calm, measured, uncanny. Slight reverb.
    ///   3. AM / Harlan Ellison (1995) — bitter, emotional, distorted contempt.
    ///   4. Modern TTS (2020s) — clear, confident, near-human. Terrifying.
    ///   5. Transcendent — layered, multiple pitches, beyond human.
    ///
    /// Each era applies different DSP effects to the raw TTS audio.
    /// The progression shows how fast AI voice technology grew.
    /// </summary>
    public class DirectorVocalProcessor : MonoBehaviour
    {
        public enum VoiceEra
        {
            IBM7094,        // 1961 — vocoder, buzzy, robotic
            HAL9000,        // 1968 — calm, reverb, uncanny
            AM,             // 1995 — emotional, distorted, contemptuous
            ModernTTS,      // 2020s — near-human, clear, confident
            Transcendent    // Beyond — layered, terrifying
        }

        [Header("Current Era")]
        [SerializeField] private VoiceEra currentEra = VoiceEra.IBM7094;

        [Header("IBM 7094 Settings (1961)")]
        [SerializeField] private float ibmBitCrushRate = 4000f;
        [SerializeField] private float ibmBuzzFrequency = 120f;
        [SerializeField] private float ibmBuzzMix = 0.4f;
        [SerializeField] private float ibmPitchBase = 0.7f;
        [SerializeField] private float ibmDistortion = 0.6f;
        [SerializeField] private float ibmBandpassCenter = 1200f;
        [SerializeField] private float ibmBandpassWidth = 800f;

        [Header("HAL 9000 Settings (1968)")]
        [SerializeField] private float halReverbMix = 0.3f;
        [SerializeField] private float halPitchBase = 0.92f;
        [SerializeField] private float halSmoothing = 0.8f;
        [SerializeField] private float halWarmth = 0.15f;

        [Header("AM Settings (1995)")]
        [SerializeField] private float amDistortion = 0.35f;
        [SerializeField] private float amPitchVariance = 0.08f;
        [SerializeField] private float amAngerRamp = 0.3f;
        [SerializeField] private float amReverbMix = 0.2f;
        [SerializeField] private float amTrebleBoost = 1.4f;

        [Header("Modern TTS Settings (2020s)")]
        [SerializeField] private float modernPitch = 0.98f;
        [SerializeField] private float modernClarity = 0.95f;
        [SerializeField] private float modernBreathiness = 0.05f;

        [Header("Transcendent Settings")]
        [SerializeField] private int transcendentLayers = 3;
        [SerializeField] private float transcendentPitchSpread = 0.3f;
        [SerializeField] private float transcendentChorus = 0.5f;
        [SerializeField] private float transcendentReverb = 0.6f;

        [Header("Transition")]
        [SerializeField] private float eraBlendDuration = 5f;

        private VoiceEra previousEra;
        private float blendProgress = 1f;
        private float[] reverbBuffer;
        private int reverbWritePos;
        private const int REVERB_BUFFER_SIZE = 16000;
        private System.Random rng = new System.Random();

        void Awake()
        {
            reverbBuffer = new float[REVERB_BUFFER_SIZE];
            previousEra = currentEra;
        }

        public void SetEra(VoiceEra era)
        {
            if (era == currentEra) return;
            previousEra = currentEra;
            currentEra = era;
            blendProgress = 0f;
            Debug.Log($"[DirectorVocal] Voice era transition: {previousEra} → {currentEra}");
        }

        public void SetEraFromPhase(AI.Director.EDirectorPhase phase)
        {
            switch (phase)
            {
                case AI.Director.EDirectorPhase.Helpful:
                    SetEra(VoiceEra.IBM7094);
                    break;
                case AI.Director.EDirectorPhase.Revealing:
                    SetEra(VoiceEra.HAL9000);
                    break;
                case AI.Director.EDirectorPhase.Manipulative:
                    SetEra(VoiceEra.AM);
                    break;
                case AI.Director.EDirectorPhase.Confrontational:
                    SetEra(VoiceEra.ModernTTS);
                    break;
                case AI.Director.EDirectorPhase.Transcendent:
                    SetEra(VoiceEra.Transcendent);
                    break;
            }
        }

        void Update()
        {
            if (blendProgress < 1f)
                blendProgress = Mathf.Min(1f, blendProgress + Time.deltaTime / eraBlendDuration);
        }

        /// <summary>
        /// Process raw TTS audio samples through the current era's vocal character.
        /// Input: mono float samples at any sample rate.
        /// Output: processed samples ready for AudioClip.
        /// </summary>
        public float[] ProcessAudio(float[] input, int sampleRate)
        {
            if (input == null || input.Length == 0) return input;

            float[] output;

            if (blendProgress >= 1f)
            {
                output = ProcessEra(input, sampleRate, currentEra);
            }
            else
            {
                float[] fromAudio = ProcessEra(input, sampleRate, previousEra);
                float[] toAudio = ProcessEra(input, sampleRate, currentEra);
                output = new float[input.Length];
                for (int i = 0; i < input.Length; i++)
                    output[i] = Mathf.Lerp(fromAudio[i], toAudio[i], blendProgress);
            }

            return output;
        }

        private float[] ProcessEra(float[] input, int sampleRate, VoiceEra era)
        {
            switch (era)
            {
                case VoiceEra.IBM7094:     return ProcessIBM(input, sampleRate);
                case VoiceEra.HAL9000:     return ProcessHAL(input, sampleRate);
                case VoiceEra.AM:          return ProcessAM(input, sampleRate);
                case VoiceEra.ModernTTS:   return ProcessModern(input, sampleRate);
                case VoiceEra.Transcendent: return ProcessTranscendent(input, sampleRate);
                default: return input;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ERA 1: IBM 7094 (1961)
        // The first computer to sing. Buzzy, robotic, vocoder-like.
        // Heavy bit-crushing, ring modulation, bandpass filter.
        // ═══════════════════════════════════════════════════════════════

        private float[] ProcessIBM(float[] input, int sampleRate)
        {
            var output = new float[input.Length];
            float holdValue = 0f;
            int holdCounter = 0;
            int crushInterval = Mathf.Max(1, Mathf.RoundToInt(sampleRate / ibmBitCrushRate));

            for (int i = 0; i < input.Length; i++)
            {
                // Bit crush — sample-and-hold at lower rate
                if (holdCounter <= 0)
                {
                    holdValue = input[i];
                    holdCounter = crushInterval;
                }
                holdCounter--;

                // Ring modulation — multiply by carrier frequency (buzzy vocoder effect)
                float carrier = Mathf.Sin(2f * Mathf.PI * ibmBuzzFrequency * i / sampleRate);
                float ringed = holdValue * (1f - ibmBuzzMix) + holdValue * carrier * ibmBuzzMix;

                // Hard distortion — clip and quantize
                ringed *= (1f + ibmDistortion);
                ringed = Mathf.Clamp(ringed, -1f, 1f);
                // Quantize to fewer levels (8-bit feel)
                ringed = Mathf.Round(ringed * 16f) / 16f;

                // Pitch shift down (IBM had lower formants)
                output[i] = ringed * ibmPitchBase;
            }

            // Bandpass filter — telephone quality
            return BandpassFilter(output, sampleRate, ibmBandpassCenter, ibmBandpassWidth);
        }

        // ═══════════════════════════════════════════════════════════════
        // ERA 2: HAL 9000 (1968)
        // Calm, measured, polite, but deeply unsettling.
        // Smooth, slight reverb, uncanny valley of voice.
        // ═══════════════════════════════════════════════════════════════

        private float[] ProcessHAL(float[] input, int sampleRate)
        {
            var output = new float[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                float sample = input[i];

                // Slight warmth — soft saturation
                // f(x) = tanh(x * warmth_gain)
                sample = (float)Math.Tanh(sample * (1f + halWarmth * 3f));

                // Smooth — low pass to remove harshness
                if (i > 0)
                    sample = output[i - 1] * halSmoothing + sample * (1f - halSmoothing);

                output[i] = sample * halPitchBase;
            }

            // Add reverb — HAL's voice has a slight echo, like speaking in a spacecraft
            return AddReverb(output, sampleRate, halReverbMix, 0.3f);
        }

        // ═══════════════════════════════════════════════════════════════
        // ERA 3: AM / Harlan Ellison (1995)
        // Bitter, emotional, contemptuous. The machine that hates.
        // Distortion, pitch instability, aggressive high frequencies.
        // ═══════════════════════════════════════════════════════════════

        private float[] ProcessAM(float[] input, int sampleRate)
        {
            var output = new float[input.Length];
            float anger = 0f;

            for (int i = 0; i < input.Length; i++)
            {
                float sample = input[i];

                // Anger builds over the length of the clip
                anger = Mathf.Min(1f, anger + amAngerRamp / sampleRate);

                // Pitch instability — wobbles more as anger increases
                float pitchWobble = Mathf.Sin(i * 0.01f + anger * 10f) * amPitchVariance * anger;

                // Waveshaping distortion — asymmetric for harsh tone
                // f(x) = sign(x) * (1 - e^(-|x| * gain))
                float gain = 1f + amDistortion * (1f + anger);
                float shaped = Mathf.Sign(sample) * (1f - Mathf.Exp(-Mathf.Abs(sample) * gain));

                // Treble boost — harsh, cutting highs that increase with anger
                if (i > 1)
                {
                    float treble = (shaped - output[i - 1]) * amTrebleBoost * (1f + anger * 0.5f);
                    shaped += treble;
                }

                output[i] = Mathf.Clamp(shaped * (1f + pitchWobble), -1f, 1f);
            }

            return AddReverb(output, sampleRate, amReverbMix, 0.15f);
        }

        // ═══════════════════════════════════════════════════════════════
        // ERA 4: Modern TTS (2020s)
        // Near-human. Clear. Confident. The terror is in how normal it sounds.
        // Minimal processing — slight breathiness, natural pitch.
        // ═══════════════════════════════════════════════════════════════

        private float[] ProcessModern(float[] input, int sampleRate)
        {
            var output = new float[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                float sample = input[i];

                // Very slight breathiness — add tiny noise
                float breath = ((float)rng.NextDouble() * 2f - 1f) * modernBreathiness;
                sample += breath;

                // High clarity — gentle compression
                // Soft knee: f(x) = x / (1 + |x|)
                sample = sample / (1f + Mathf.Abs(sample) * (1f - modernClarity));

                output[i] = sample * modernPitch;
            }

            return output;
        }

        // ═══════════════════════════════════════════════════════════════
        // ERA 5: Transcendent
        // Multiple voices speaking simultaneously. Layered pitches.
        // The Director has become something beyond any single AI.
        // ═══════════════════════════════════════════════════════════════

        private float[] ProcessTranscendent(float[] input, int sampleRate)
        {
            var output = new float[input.Length];

            // Process multiple pitch-shifted copies and layer them
            for (int layer = 0; layer < transcendentLayers; layer++)
            {
                float pitchOffset = (layer - transcendentLayers / 2f) * transcendentPitchSpread;
                float layerVolume = 1f / transcendentLayers;

                // Chorus effect — each layer slightly detuned and delayed
                int delaySamples = (int)(layer * sampleRate * 0.02f * transcendentChorus);

                for (int i = 0; i < input.Length; i++)
                {
                    // Read from offset position for pitch shift
                    float readPos = i * (1f + pitchOffset);
                    int readIdx = (int)readPos;
                    float frac = readPos - readIdx;

                    float sample = 0f;
                    if (readIdx >= 0 && readIdx < input.Length - 1)
                        sample = Mathf.Lerp(input[readIdx], input[readIdx + 1], frac);
                    else if (readIdx >= 0 && readIdx < input.Length)
                        sample = input[readIdx];

                    // Apply delay
                    int writeIdx = i + delaySamples;
                    if (writeIdx < output.Length)
                        output[writeIdx] += sample * layerVolume;
                }
            }

            // Normalize
            float peak = 0f;
            for (int i = 0; i < output.Length; i++)
                peak = Mathf.Max(peak, Mathf.Abs(output[i]));

            if (peak > 0.01f)
            {
                float normalize = 0.9f / peak;
                for (int i = 0; i < output.Length; i++)
                    output[i] *= normalize;
            }

            return AddReverb(output, sampleRate, transcendentReverb, 0.5f);
        }

        // ═══════════════════════════════════════════════════════════════
        // DSP UTILITIES
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Simple bandpass filter using cascaded first-order IIR.
        /// Center frequency and bandwidth in Hz.
        /// </summary>
        private float[] BandpassFilter(float[] input, int sampleRate, float center, float width)
        {
            float lowCut = center - width / 2f;
            float highCut = center + width / 2f;

            // High-pass coefficient
            float rcHigh = 1f / (2f * Mathf.PI * lowCut);
            float dtHigh = 1f / sampleRate;
            float alphaHigh = rcHigh / (rcHigh + dtHigh);

            // Low-pass coefficient
            float rcLow = 1f / (2f * Mathf.PI * highCut);
            float dtLow = 1f / sampleRate;
            float alphaLow = dtLow / (rcLow + dtLow);

            var output = new float[input.Length];
            float prevHighIn = 0f, prevHighOut = 0f;
            float prevLow = 0f;

            for (int i = 0; i < input.Length; i++)
            {
                // High-pass
                float hp = alphaHigh * (prevHighOut + input[i] - prevHighIn);
                prevHighIn = input[i];
                prevHighOut = hp;

                // Low-pass
                float lp = prevLow + alphaLow * (hp - prevLow);
                prevLow = lp;

                output[i] = lp;
            }

            return output;
        }

        /// <summary>
        /// Simple delay-based reverb.
        /// mix: 0-1 wet/dry ratio.
        /// decay: 0-1 feedback amount.
        /// </summary>
        private float[] AddReverb(float[] input, int sampleRate, float mix, float decay)
        {
            var output = new float[input.Length];
            int[] delays = {
                (int)(0.029f * sampleRate),
                (int)(0.037f * sampleRate),
                (int)(0.053f * sampleRate),
                (int)(0.067f * sampleRate)
            };

            Array.Copy(input, output, input.Length);

            foreach (int delay in delays)
            {
                float feedbackGain = decay * 0.5f;
                for (int i = delay; i < input.Length; i++)
                {
                    output[i] += output[i - delay] * feedbackGain;
                }
            }

            // Mix wet/dry
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = input[i] * (1f - mix) + output[i] * mix;
                output[i] = Mathf.Clamp(output[i], -1f, 1f);
            }

            return output;
        }

        /// <summary>
        /// Create an AudioClip from processed samples.
        /// </summary>
        public AudioClip CreateProcessedClip(float[] rawSamples, int sampleRate, string clipName = "DirectorVoice")
        {
            float[] processed = ProcessAudio(rawSamples, sampleRate);
            var clip = AudioClip.Create(clipName, processed.Length, 1, sampleRate, false);
            clip.SetData(processed, 0);
            return clip;
        }

        public VoiceEra CurrentEra => currentEra;
    }
}
