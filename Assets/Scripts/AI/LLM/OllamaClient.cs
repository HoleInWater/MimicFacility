using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace MimicFacility.AI.LLM
{
    [Serializable]
    public class LLMRequest
    {
        public string model = "phi3";
        public string systemPrompt;
        public string userPrompt;
        public float temperature = 0.7f;
        public int maxTokens = 256;
        public bool stream = false;
    }

    [Serializable]
    public class LLMResponse
    {
        public string text;
        public long generationTimeMs;
        public int tokenCount;
        public bool success;
        public string errorMessage;
    }

    public class OllamaClient : MonoBehaviour
    {
        [SerializeField] private string endpoint = "http://localhost:11434";
        [SerializeField] private float generateTimeout = 10f;
        [SerializeField] private float healthTimeout = 3f;

        public void SetEndpoint(string url)
        {
            endpoint = url.TrimEnd('/');
        }

        public void SendRequest(LLMRequest request, Action<LLMResponse> callback)
        {
            StartCoroutine(SendRequestCoroutine(request, callback));
        }

        public void CheckServerHealth(Action<bool> callback)
        {
            StartCoroutine(HealthCheckCoroutine(callback));
        }

        private IEnumerator SendRequestCoroutine(LLMRequest request, Action<LLMResponse> callback)
        {
            var response = new LLMResponse();
            long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string json = SerializeRequest(request);
            string url = endpoint + "/api/generate";

            using (var webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.timeout = (int)generateTimeout;

                yield return webRequest.SendWebRequest();

                long elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startTime;
                response.generationTimeMs = elapsed;

                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    response.success = false;
                    response.errorMessage = $"Connection refused: {webRequest.error}";
                    callback?.Invoke(response);
                    yield break;
                }

                if (webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    response.success = false;
                    response.errorMessage = $"Server error ({webRequest.responseCode}): {webRequest.error}";
                    callback?.Invoke(response);
                    yield break;
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    response.success = false;
                    response.errorMessage = $"Request failed: {webRequest.error}";
                    callback?.Invoke(response);
                    yield break;
                }

                string responseBody = webRequest.downloadHandler.text;
                if (!DeserializeResponse(responseBody, response))
                {
                    response.success = false;
                    response.errorMessage = $"Malformed response: could not parse JSON";
                    callback?.Invoke(response);
                    yield break;
                }

                response.success = true;
            }

            callback?.Invoke(response);
        }

        private IEnumerator HealthCheckCoroutine(Action<bool> callback)
        {
            string url = endpoint + "/api/tags";

            using (var webRequest = UnityWebRequest.Get(url))
            {
                webRequest.timeout = (int)healthTimeout;
                yield return webRequest.SendWebRequest();
                callback?.Invoke(webRequest.result == UnityWebRequest.Result.Success);
            }
        }

        private string SerializeRequest(LLMRequest request)
        {
            var wrapper = new OllamaRequestBody
            {
                model = request.model,
                prompt = request.userPrompt,
                system = request.systemPrompt,
                stream = request.stream,
                options = new OllamaOptions
                {
                    temperature = request.temperature,
                    num_predict = request.maxTokens
                }
            };
            return JsonUtility.ToJson(wrapper);
        }

        private bool DeserializeResponse(string json, LLMResponse target)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<OllamaResponseBody>(json);
                if (wrapper == null) return false;

                target.text = wrapper.response;
                target.tokenCount = wrapper.eval_count;
                return !string.IsNullOrEmpty(target.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OllamaClient] JSON parse error: {e.Message}");
                return false;
            }
        }

        [Serializable]
        private class OllamaRequestBody
        {
            public string model;
            public string prompt;
            public string system;
            public bool stream;
            public OllamaOptions options;
        }

        [Serializable]
        private class OllamaOptions
        {
            public float temperature;
            public int num_predict;
        }

        [Serializable]
        private class OllamaResponseBody
        {
            public string response;
            public int eval_count;
            public long total_duration;
        }
    }
}
