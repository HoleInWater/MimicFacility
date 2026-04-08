using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace MimicFacility.Audio
{
    /// <summary>
    /// Client for Piper TTS with the HAL 9000 voice model.
    /// Piper runs locally as an HTTP server (default port 5000).
    /// The HAL model is from: https://huggingface.co/campwill/HAL-9000-Piper-TTS
    ///
    /// Setup:
    ///   1. Install Piper: pip install piper-tts
    ///   2. Download HAL model from HuggingFace
    ///   3. Run: piper --model hal-9000.onnx --http-server --port 5000
    ///   4. This client calls GET http://localhost:5000?text=...
    ///   5. Response is raw 22050Hz mono int16 PCM audio
    ///
    /// The Director's voice pipeline:
    ///   LLM generates text → PiperTTSClient synthesizes HAL voice
    ///   → DirectorVocalProcessor applies era-specific DSP
    ///   → AudioSource plays the result
    /// </summary>
    public class PiperTTSClient : MonoBehaviour
    {
        public static PiperTTSClient Instance { get; private set; }

        [Header("Server")]
        [SerializeField] private string serverUrl = "http://localhost:5000";
        [SerializeField] private float requestTimeout = 15f;

        [Header("Audio")]
        [SerializeField] private int sampleRate = 22050;
        [SerializeField] private float defaultVolume = 0.8f;

        [Header("Processing")]
        [SerializeField] private bool applyVocalProcessor = true;

        private DirectorVocalProcessor vocalProcessor;
        private bool isGenerating;

        public bool IsGenerating => isGenerating;
        public bool IsAvailable { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            vocalProcessor = FindObjectOfType<DirectorVocalProcessor>();
            StartCoroutine(CheckServerHealth());
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>
        /// Synthesize text to speech using the HAL 9000 voice model.
        /// Returns an AudioClip via callback.
        /// </summary>
        public void Speak(string text, Action<AudioClip> onComplete, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                onError?.Invoke("Empty text");
                return;
            }

            StartCoroutine(SynthesizeCoroutine(text, onComplete, onError));
        }

        /// <summary>
        /// Synthesize and immediately play on the given AudioSource.
        /// </summary>
        public void SpeakTo(string text, AudioSource source, Action onComplete = null)
        {
            Speak(text, clip =>
            {
                if (clip != null && source != null)
                {
                    source.clip = clip;
                    source.volume = defaultVolume;
                    source.Play();
                    onComplete?.Invoke();
                }
            }, error => Debug.LogWarning($"[PiperTTS] {error}"));
        }

        private IEnumerator SynthesizeCoroutine(string text, Action<AudioClip> onComplete, Action<string> onError)
        {
            isGenerating = true;

            string encodedText = UnityWebRequest.EscapeURL(text);
            string url = $"{serverUrl}?text={encodedText}";

            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)requestTimeout;
                request.SetRequestHeader("Accept", "audio/wav");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    isGenerating = false;
                    string error = $"Piper TTS request failed: {request.error}";
                    Debug.LogWarning($"[PiperTTS] {error}");
                    onError?.Invoke(error);
                    yield break;
                }

                byte[] audioBytes = request.downloadHandler.data;
                if (audioBytes == null || audioBytes.Length < 44)
                {
                    isGenerating = false;
                    onError?.Invoke("Empty or invalid audio response");
                    yield break;
                }

                AudioClip clip = CreateClipFromPCM(audioBytes, text);

                if (clip != null && applyVocalProcessor && vocalProcessor != null)
                {
                    clip = ApplyVocalProcessing(clip);
                }

                isGenerating = false;
                onComplete?.Invoke(clip);
            }
        }

        /// <summary>
        /// Convert raw PCM int16 bytes (possibly with WAV header) to AudioClip.
        /// Piper outputs 22050Hz mono int16.
        /// </summary>
        private AudioClip CreateClipFromPCM(byte[] data, string clipName)
        {
            int headerOffset = 0;

            // Check for WAV header (RIFF)
            if (data.Length > 44 && data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F')
            {
                headerOffset = 44;

                // Read sample rate from WAV header (bytes 24-27)
                int wavSampleRate = BitConverter.ToInt32(data, 24);
                if (wavSampleRate > 0)
                    sampleRate = wavSampleRate;
            }

            int pcmLength = data.Length - headerOffset;
            int sampleCount = pcmLength / 2; // int16 = 2 bytes per sample

            if (sampleCount <= 0) return null;

            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                int byteIndex = headerOffset + i * 2;
                if (byteIndex + 1 >= data.Length) break;

                short pcm = (short)(data[byteIndex] | (data[byteIndex + 1] << 8));
                samples[i] = pcm / 32767f;
            }

            var clip = AudioClip.Create(
                $"HAL_{clipName.GetHashCode():X8}",
                sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Run the audio through DirectorVocalProcessor for era-specific effects.
        /// </summary>
        private AudioClip ApplyVocalProcessing(AudioClip inputClip)
        {
            if (vocalProcessor == null) return inputClip;

            float[] samples = new float[inputClip.samples];
            inputClip.GetData(samples, 0);

            float[] processed = vocalProcessor.ProcessAudio(samples, inputClip.frequency);

            var outputClip = AudioClip.Create(
                inputClip.name + "_processed",
                processed.Length, 1, inputClip.frequency, false);
            outputClip.SetData(processed, 0);
            return outputClip;
        }

        /// <summary>
        /// Check if Piper server is running.
        /// </summary>
        public IEnumerator CheckServerHealth()
        {
            using (var request = UnityWebRequest.Get(serverUrl))
            {
                request.timeout = 3;
                yield return request.SendWebRequest();
                IsAvailable = request.result == UnityWebRequest.Result.Success;

                if (IsAvailable)
                    Debug.Log("[PiperTTS] HAL 9000 voice server is online.");
                else
                    Debug.LogWarning("[PiperTTS] Server not available. Director will use fallback text.");
            }
        }

        public void SetEndpoint(string url) { serverUrl = url; }
    }
}
