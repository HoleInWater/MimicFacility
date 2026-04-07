using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.Gear;

namespace MimicFacility.Audio
{
    [Serializable]
    public class EchoParams
    {
        public float gain;
        public int delaySamples;
        public float distance;
    }

    public class ProximityVoiceChat : NetworkBehaviour
    {
        public event Action<int, float[]> OnVoiceReceivedRaw;

        [Header("Proximity")]
        [SerializeField] private float maxVoiceRange = 30f;
        [SerializeField] private float fullVolumeRange = 5f;
        [SerializeField] private float behindWallAttenuation = 0.3f;

        [Header("Microphone")]
        [SerializeField] private int sampleRate = 16000;
        [SerializeField] private int captureBufferSeconds = 1;
        [SerializeField] private float voiceActivityThreshold = 0.01f;
        [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;

        [Header("Echo Simulation")]
        [SerializeField] private float ambientTemperatureCelsius = 20f;
        [SerializeField] private float minEchoDistance = 17.15f;
        [SerializeField] private float maxEchoGain = 0.5f;
        [SerializeField] private float echoDecayPerBounce = 0.6f;
        [SerializeField] private int maxEchoBounces = 3;
        [SerializeField] private LayerMask echoReflectionMask = ~0;

        [Header("Room Detection")]
        [SerializeField] private float roomProbeDistance = 50f;
        [SerializeField] private int roomProbeDirections = 12;

        [Header("References")]
        [SerializeField] private SpatialAudioProcessor spatialProcessor;

        private AudioClip micCapture;
        private string micDevice;
        private bool isCapturing;
        private int lastSamplePos;

        private readonly Dictionary<int, AudioSource> playerSources = new Dictionary<int, AudioSource>();
        private readonly Dictionary<int, float[]> echoBuffers = new Dictionary<int, float[]>();

        private float speedOfSound;
        private float[] roomDistances;

        public float SpeedOfSound => speedOfSound;
        public bool IsCapturing => isCapturing;

        private void Awake()
        {
            speedOfSound = 331.3f + 0.606f * ambientTemperatureCelsius;
            roomDistances = new float[roomProbeDirections];
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            if (Input.GetKeyDown(pushToTalkKey))
                StartCapture();

            if (Input.GetKeyUp(pushToTalkKey))
                StopCaptureAndSend();

            if (isCapturing)
                StreamCapture();
        }

        public void SetTemperature(float celsius)
        {
            ambientTemperatureCelsius = celsius;
            speedOfSound = 331.3f + 0.606f * celsius;
        }

        public void StartCapture()
        {
            if (isCapturing) return;
            if (Microphone.devices.Length == 0) return;

            micDevice = Microphone.devices[0];
            micCapture = Microphone.Start(micDevice, true, captureBufferSeconds, sampleRate);
            lastSamplePos = 0;
            isCapturing = true;
        }

        public void StopCaptureAndSend()
        {
            if (!isCapturing) return;

            int currentPos = Microphone.GetPosition(micDevice);
            Microphone.End(micDevice);
            isCapturing = false;

            if (micCapture == null) return;

            float[] samples = ExtractSamples(currentPos);
            if (samples == null || samples.Length == 0) return;
            if (!DetectVoiceActivity(samples)) return;
            if (IsInJammedZone()) return;

            byte[] compressed = CompressTo16BitPCM(samples);
            CmdBroadcastVoice(compressed, transform.position);
        }

        private void StreamCapture()
        {
            if (micCapture == null) return;

            int currentPos = Microphone.GetPosition(micDevice);
            if (currentPos == lastSamplePos) return;

            int sampleCount = currentPos > lastSamplePos
                ? currentPos - lastSamplePos
                : (micCapture.samples - lastSamplePos) + currentPos;

            if (sampleCount < sampleRate / 10) return;

            float[] samples = new float[sampleCount];
            micCapture.GetData(samples, lastSamplePos);
            lastSamplePos = currentPos;

            if (!DetectVoiceActivity(samples)) return;
            if (IsInJammedZone()) return;

            byte[] compressed = CompressTo16BitPCM(samples);
            CmdBroadcastVoice(compressed, transform.position);
        }

        [Command]
        private void CmdBroadcastVoice(byte[] compressedAudio, Vector3 sourcePosition)
        {
            int senderId = connectionToClient.connectionId;
            RpcReceiveVoice(compressedAudio, senderId, sourcePosition);
        }

        [ClientRpc]
        private void RpcReceiveVoice(byte[] audio, int senderId, Vector3 sourcePosition)
        {
            if (isLocalPlayer && connectionToClient != null &&
                connectionToClient.connectionId == senderId)
                return;

            Transform listener = Camera.main != null ? Camera.main.transform : transform;
            float distance = Vector3.Distance(listener.position, sourcePosition);

            if (distance > maxVoiceRange) return;

            float[] samples = DecompressFrom16BitPCM(audio);

            float proximityVolume = CalculateProximityVolume(distance);
            float occlusionFactor = CalculateOcclusion(listener.position, sourcePosition);

            for (int i = 0; i < samples.Length; i++)
                samples[i] *= proximityVolume * occlusionFactor;

            ProbeRoomGeometry(sourcePosition);
            EchoParams[] echoes = CalculateEchoes(sourcePosition, listener.position);
            float[] withEcho = ApplyEchoes(samples, echoes);

            OnVoiceReceivedRaw?.Invoke(senderId, withEcho);
            PlaySpatialVoice(withEcho, sourcePosition, senderId);
        }

        private float CalculateProximityVolume(float distance)
        {
            if (distance <= fullVolumeRange) return 1f;
            if (distance >= maxVoiceRange) return 0f;

            float t = (distance - fullVolumeRange) / (maxVoiceRange - fullVolumeRange);
            return 1f - t * t;
        }

        private float CalculateOcclusion(Vector3 listener, Vector3 source)
        {
            Vector3 dir = source - listener;
            float dist = dir.magnitude;

            if (Physics.Raycast(listener, dir.normalized, dist, echoReflectionMask))
                return behindWallAttenuation;

            return 1f;
        }

        private void ProbeRoomGeometry(Vector3 position)
        {
            float angleStep = 360f / roomProbeDirections;

            for (int i = 0; i < roomProbeDirections; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                if (Physics.Raycast(position, dir, out RaycastHit hit, roomProbeDistance, echoReflectionMask))
                    roomDistances[i] = hit.distance;
                else
                    roomDistances[i] = roomProbeDistance;
            }
        }

        // d = v * t / 2
        // Therefore: t = 2 * d / v
        // In samples: D = t * sampleRate = (2 * d / v) * sampleRate
        private EchoParams[] CalculateEchoes(Vector3 source, Vector3 listener)
        {
            var echoes = new List<EchoParams>();
            float v = speedOfSound;

            for (int bounce = 0; bounce < maxEchoBounces; bounce++)
            {
                for (int i = 0; i < roomProbeDirections; i++)
                {
                    float wallDistance = roomDistances[i];

                    // d = v * t / 2, so total path = 2 * wallDistance for first bounce
                    // For nth bounce, path = 2 * wallDistance * (bounce + 1)
                    float totalPath = 2f * wallDistance * (bounce + 1);
                    float echoDistance = totalPath / 2f;

                    if (echoDistance < minEchoDistance) continue;

                    // t = 2 * d / v (round-trip time)
                    float roundTripTime = totalPath / v;
                    int delaySamples = Mathf.RoundToInt(roundTripTime * sampleRate);

                    // Gain decays with each bounce and with distance
                    // a = maxEchoGain * (echoDecayPerBounce ^ (bounce + 1)) * (1 / (1 + k*d^2))
                    float distanceFactor = 1f / (1f + 0.01f * echoDistance * echoDistance);
                    float gain = maxEchoGain
                        * Mathf.Pow(echoDecayPerBounce, bounce + 1)
                        * distanceFactor;

                    if (gain < 0.01f) continue;

                    echoes.Add(new EchoParams
                    {
                        gain = gain,
                        delaySamples = delaySamples,
                        distance = echoDistance
                    });
                }
            }

            return echoes.ToArray();
        }

        // Digital echo: y[n] = x[n] + Σ(a_i * x[n - D_i])
        private float[] ApplyEchoes(float[] input, EchoParams[] echoes)
        {
            if (echoes == null || echoes.Length == 0) return input;

            int maxDelay = 0;
            foreach (var echo in echoes)
            {
                if (echo.delaySamples > maxDelay)
                    maxDelay = echo.delaySamples;
            }

            int outputLength = input.Length + maxDelay;
            float[] output = new float[outputLength];

            // Copy original signal: y[n] = x[n]
            Array.Copy(input, output, input.Length);

            // Add each echo: y[n] += a * x[n - D]
            foreach (var echo in echoes)
            {
                int D = echo.delaySamples;
                float a = echo.gain;

                for (int n = 0; n < input.Length; n++)
                {
                    int echoIndex = n + D;
                    if (echoIndex < outputLength)
                        output[echoIndex] += a * input[n];
                }
            }

            // Clamp to prevent clipping
            for (int i = 0; i < outputLength; i++)
                output[i] = Mathf.Clamp(output[i], -1f, 1f);

            return output;
        }

        private void PlaySpatialVoice(float[] samples, Vector3 position, int senderId)
        {
            if (!playerSources.TryGetValue(senderId, out AudioSource source) || source == null)
            {
                var go = new GameObject($"ProxVoice_{senderId}");
                go.transform.SetParent(transform);
                source = go.AddComponent<AudioSource>();
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.maxDistance = maxVoiceRange;
                source.dopplerLevel = 0f;
                playerSources[senderId] = source;
            }

            source.transform.position = position;

            var clip = AudioClip.Create($"proxvoice_{senderId}_{Time.frameCount}",
                samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);

            if (spatialProcessor != null)
            {
                Transform listener = Camera.main != null ? Camera.main.transform : transform;
                var audioParams = spatialProcessor.ComputeParamsFromTransforms(listener, source.transform, echoReflectionMask);
                var result = spatialProcessor.ProcessSpatialAudio(audioParams);
                spatialProcessor.ApplyToAudioSource(source, result);
            }

            source.clip = clip;
            source.Play();
        }

        // Utility: compute echo distance from round-trip time
        // d = v * t / 2
        public float EchoDistanceFromTime(float roundTripSeconds)
        {
            return speedOfSound * roundTripSeconds / 2f;
        }

        // Utility: compute round-trip time from distance
        // t = 2 * d / v
        public float EchoTimeFromDistance(float distance)
        {
            return 2f * distance / speedOfSound;
        }

        // Utility: minimum distance for perceptible echo (0.1s round-trip)
        // d_min = v * 0.1 / 2 = v / 20
        public float MinimumEchoDistance()
        {
            return speedOfSound / 20f;
        }

        private float[] ExtractSamples(int currentPos)
        {
            int sampleCount = currentPos > lastSamplePos
                ? currentPos - lastSamplePos
                : (micCapture.samples - lastSamplePos) + currentPos;

            if (sampleCount <= 0) return null;

            float[] samples = new float[sampleCount];
            micCapture.GetData(samples, lastSamplePos);
            return samples;
        }

        private bool DetectVoiceActivity(float[] samples)
        {
            float energy = 0f;
            for (int i = 0; i < samples.Length; i++)
                energy += samples[i] * samples[i];
            energy /= samples.Length;
            return energy > voiceActivityThreshold;
        }

        public static byte[] CompressTo16BitPCM(float[] samples)
        {
            byte[] data = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short pcm = (short)Mathf.Clamp(samples[i] * 32767f, short.MinValue, short.MaxValue);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }
            return data;
        }

        public static float[] DecompressFrom16BitPCM(byte[] data)
        {
            int count = data.Length / 2;
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                short pcm = (short)(data[i * 2] | (data[i * 2 + 1] << 8));
                samples[i] = pcm / 32767f;
            }
            return samples;
        }

        private bool IsInJammedZone()
        {
            foreach (var jammer in FindObjectsOfType<SignalJammer>())
            {
                if (jammer.IsActive && jammer.IsInJamZone(transform.position))
                    return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (isCapturing && !string.IsNullOrEmpty(micDevice))
                Microphone.End(micDevice);

            foreach (var kvp in playerSources)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            playerSources.Clear();
        }
    }
}
