using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.Gear;

namespace MimicFacility.Audio
{
    public class VoiceChatManager : NetworkBehaviour
    {
        public event Action<byte[], int> OnVoiceReceived;

        [SerializeField] private int sampleRate = 16000;
        [SerializeField] private int micBufferLengthSec = 1;
        [SerializeField] private float voiceActivityThreshold = 0.01f;
        [SerializeField] private SpatialAudioProcessor spatialProcessor;

        private AudioClip micCapture;
        private string micDevice;
        private bool isCapturing;
        private int lastSamplePos;

        private readonly Dictionary<int, AudioSource> playerAudioSources = new Dictionary<int, AudioSource>();

        public bool IsCapturing => isCapturing;

        public void StartCapture()
        {
            if (isCapturing) return;
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("No microphone detected.");
                return;
            }

            micDevice = Microphone.devices[0];
            micCapture = Microphone.Start(micDevice, true, micBufferLengthSec, sampleRate);
            lastSamplePos = 0;
            isCapturing = true;
        }

        public void StopCapture()
        {
            if (!isCapturing) return;

            int currentPos = Microphone.GetPosition(micDevice);
            Microphone.End(micDevice);
            isCapturing = false;

            if (micCapture == null) return;

            int sampleCount = currentPos > lastSamplePos
                ? currentPos - lastSamplePos
                : (micCapture.samples - lastSamplePos) + currentPos;

            if (sampleCount <= 0) return;

            float[] samples = new float[sampleCount];
            micCapture.GetData(samples, lastSamplePos);

            if (!VoiceActivityDetection(samples)) return;
            if (IsInJammedZone()) return;

            byte[] compressed = CompressAudio(samples);
            int senderId = (int)netId;
            CmdSendVoiceData(compressed, senderId);
        }

        [Command]
        private void CmdSendVoiceData(byte[] compressedAudio, int senderId)
        {
            Vector3 sourcePosition = transform.position;
            RpcReceiveVoiceData(compressedAudio, senderId, sourcePosition);
        }

        [ClientRpc]
        private void RpcReceiveVoiceData(byte[] audio, int senderId, Vector3 sourcePosition)
        {
            if ((int)netId == senderId) return;

            OnVoiceReceived?.Invoke(audio, senderId);

            float[] samples = DecompressAudio(audio);
            PlayReceivedAudio(samples, sourcePosition, senderId);
        }

        public static byte[] CompressAudio(float[] samples)
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

        public static float[] DecompressAudio(byte[] data)
        {
            int sampleCount = data.Length / 2;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short pcm = (short)(data[i * 2] | (data[i * 2 + 1] << 8));
                samples[i] = pcm / 32767f;
            }
            return samples;
        }

        private void PlayReceivedAudio(float[] samples, Vector3 position, int senderId)
        {
            if (!playerAudioSources.TryGetValue(senderId, out AudioSource source) || source == null)
            {
                GameObject go = new GameObject($"VoiceSource_{senderId}");
                go.transform.SetParent(transform);
                source = go.AddComponent<AudioSource>();
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.maxDistance = 50f;
                source.dopplerLevel = 0f;
                playerAudioSources[senderId] = source;
            }

            source.transform.position = position;

            AudioClip clip = AudioClip.Create($"voice_{senderId}_{Time.frameCount}", samples.Length, 1, 16000, false);
            clip.SetData(samples, 0);

            if (spatialProcessor != null)
            {
                Transform listener = Camera.main != null ? Camera.main.transform : transform;
                var audioParams = spatialProcessor.ComputeParamsFromTransforms(listener, source.transform, ~0);
                var result = spatialProcessor.ProcessSpatialAudio(audioParams);
                spatialProcessor.ApplyToAudioSource(source, result);
            }

            source.clip = clip;
            source.Play();
        }

        public bool VoiceActivityDetection(float[] samples)
        {
            if (samples == null || samples.Length == 0) return false;

            float energy = 0f;
            for (int i = 0; i < samples.Length; i++)
                energy += samples[i] * samples[i];

            energy /= samples.Length;
            return energy > voiceActivityThreshold;
        }

        private bool IsInJammedZone()
        {
            var jammers = FindObjectsOfType<SignalJammer>();
            foreach (var jammer in jammers)
            {
                if (jammer.IsActive && jammer.IsInJamZone(transform.position))
                    return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (isCapturing)
            {
                Microphone.End(micDevice);
                isCapturing = false;
            }

            foreach (var kvp in playerAudioSources)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            playerAudioSources.Clear();
        }
    }
}
