using System;
using UnityEngine;

namespace MimicFacility.Horror
{
    [Serializable]
    public class LoadingScreenData
    {
        public string PlayerName;
        public string HexCode;
        public string ClassificationText;
        public string DisplayText;
        public bool IsActive;
        public float Progress;
    }

    public class LoadingScreenSystem : MonoBehaviour
    {
        [SerializeField] private float minDisplayTime = 3f;
        [SerializeField] private float maxDisplayTime = 5f;

        private static readonly string[] ClassificationTexts =
        {
            "PENDING",
            "ACTIVE",
            "FLAGGED",
            "REVIEWING"
        };

        private LoadingScreenData _currentData;
        private float _targetDuration;
        private float _elapsedTime;
        private bool _isActive;
        private int _classificationIndex;
        private float _classificationRotateTimer;

        public event Action OnLoadingComplete;

        private void Update()
        {
            if (!_isActive || _currentData == null) return;

            _elapsedTime += Time.unscaledDeltaTime;
            _currentData.Progress = Mathf.Clamp01(_elapsedTime / _targetDuration);

            _classificationRotateTimer += Time.unscaledDeltaTime;
            if (_classificationRotateTimer >= 1f)
            {
                _classificationRotateTimer = 0f;
                _classificationIndex = (_classificationIndex + 1) % ClassificationTexts.Length;
                _currentData.ClassificationText = ClassificationTexts[_classificationIndex];
                UpdateDisplayText();
            }

            if (_elapsedTime >= _targetDuration)
                EndLoadingScreen();
        }

        public void BeginLoadingScreen(string playerName)
        {
            string resolvedName = string.IsNullOrEmpty(playerName) || playerName == "UNKNOWN"
                ? ResolveSteamName()
                : playerName;

            string hexCode = UnityEngine.Random.Range(0, 0xFFFFFF + 1).ToString("X8");
            _classificationIndex = 0;

            _currentData = new LoadingScreenData
            {
                PlayerName = resolvedName,
                HexCode = hexCode,
                ClassificationText = ClassificationTexts[0],
                IsActive = true,
                Progress = 0f
            };

            UpdateDisplayText();

            _targetDuration = UnityEngine.Random.Range(minDisplayTime, maxDisplayTime);
            _elapsedTime = 0f;
            _classificationRotateTimer = 0f;
            _isActive = true;
        }

        public void EndLoadingScreen()
        {
            _isActive = false;

            if (_currentData != null)
            {
                _currentData.IsActive = false;
                _currentData.Progress = 1f;
            }

            OnLoadingComplete?.Invoke();
        }

        public LoadingScreenData ConsumeLoadingData()
        {
            return _currentData;
        }

        public string ResolveSteamName()
        {
            try
            {
                string userName = System.Environment.UserName;
                if (!string.IsNullOrEmpty(userName))
                    return userName.ToUpperInvariant();
            }
            catch
            {
                // Fallback silently
            }

            return "SUBJECT";
        }

        private void UpdateDisplayText()
        {
            if (_currentData == null) return;
            _currentData.DisplayText = $"SUBJECT {_currentData.PlayerName} \u2014 ENTRY {_currentData.HexCode} \u2014 CLASSIFICATION: {_currentData.ClassificationText}";
        }

        public bool IsActive()
        {
            return _isActive;
        }

        public float GetProgress()
        {
            return _currentData?.Progress ?? 0f;
        }
    }
}
