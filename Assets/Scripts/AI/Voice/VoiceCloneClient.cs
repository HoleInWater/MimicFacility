using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace MimicFacility.AI.Voice
{
    [Serializable]
    public class VoiceCloneRequest
    {
        public string text;
        public string speakerReferenceId;
        public float temperature = 0.7f;
        public float exaggerationFactor = 1.0f;
    }

    [Serializable]
    public class VoiceCloneResponse
    {
        public byte[] audioData;
        public int sampleRate = 24000;
        public float durationSeconds;
        public bool success;
        public string errorMessage;
    }

    public class VoiceCloneClient : MonoBehaviour
    {
        [SerializeField] private string endpoint = "http://localhost:8100";
        [SerializeField] private float requestTimeout = 15f;

        public void SetEndpoint(string url)
        {
            endpoint = url.TrimEnd('/');
        }

        public void SendCloneRequest(VoiceCloneRequest req, Action<VoiceCloneResponse> callback)
        {
            StartCoroutine(CloneRequestCoroutine(req, callback));
        }

        public void CheckServerHealth(Action<bool> callback)
        {
            StartCoroutine(HealthCheckCoroutine(callback));
        }

        private IEnumerator CloneRequestCoroutine(VoiceCloneRequest req, Action<VoiceCloneResponse> callback)
        {
            var response = new VoiceCloneResponse();
            string url = endpoint + "/api/synthesize";

            var body = new VoiceCloneRequestBody
            {
                text = req.text,
                speaker_reference_id = req.speakerReferenceId,
                temperature = req.temperature,
                exaggeration_factor = req.exaggerationFactor
            };
            string json = JsonUtility.ToJson(body);

            using (var webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.timeout = (int)requestTimeout;

                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    response.success = false;
                    response.errorMessage = $"Voice clone request failed: {webRequest.error}";
                    callback?.Invoke(response);
                    yield break;
                }

                ParseAudioResponse(webRequest, response);
                response.success = true;
            }

            callback?.Invoke(response);
        }

        private IEnumerator HealthCheckCoroutine(Action<bool> callback)
        {
            string url = endpoint + "/api/health";

            using (var webRequest = UnityWebRequest.Get(url))
            {
                webRequest.timeout = 3;
                yield return webRequest.SendWebRequest();
                callback?.Invoke(webRequest.result == UnityWebRequest.Result.Success);
            }
        }

        private void ParseAudioResponse(UnityWebRequest webRequest, VoiceCloneResponse response)
        {
            response.audioData = webRequest.downloadHandler.data;

            string sampleRateHeader = webRequest.GetResponseHeader("X-Sample-Rate");
            if (!string.IsNullOrEmpty(sampleRateHeader) && int.TryParse(sampleRateHeader, out int parsedRate))
                response.sampleRate = parsedRate;

            if (response.audioData != null && response.sampleRate > 0)
            {
                int sampleCount = response.audioData.Length / 2;
                response.durationSeconds = sampleCount / (float)response.sampleRate;
            }
        }

        public AudioClip CreateAudioClip(VoiceCloneResponse response)
        {
            if (response.audioData == null || response.audioData.Length == 0)
                return null;

            int sampleCount = response.audioData.Length / 2;
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = (short)(response.audioData[i * 2] | (response.audioData[i * 2 + 1] << 8));
                samples[i] = sample / 32768f;
            }

            var clip = AudioClip.Create("VoiceClone", sampleCount, 1, response.sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        [Serializable]
        private class VoiceCloneRequestBody
        {
            public string text;
            public string speaker_reference_id;
            public float temperature;
            public float exaggeration_factor;
        }
    }
}
