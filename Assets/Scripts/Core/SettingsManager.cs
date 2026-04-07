using System;
using System.IO;
using UnityEngine;

namespace MimicFacility.Core
{
    public enum EGraphicsQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    [Serializable]
    public class GameSettings
    {
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float voiceVolume = 1f;
        public float mouseSensitivity = 1f;
        public bool invertY;
        [Range(60, 120)] public int fieldOfView = 90;
        public bool enableDeviceHorror = true;
        public bool enableSubliminalFrames = true;
        public EGraphicsQuality graphicsQuality = EGraphicsQuality.High;
        public bool fullscreen = true;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public bool vsync = true;
    }

    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public event Action OnSettingsChanged;

        [SerializeField] private GameSettings currentSettings = new GameSettings();
        public GameSettings Settings => currentSettings;

        private string _settingsPath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _settingsPath = Path.Combine(Application.persistentDataPath, "settings.json");
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Save()
        {
            Validate();
            try
            {
                string json = JsonUtility.ToJson(currentSettings, true);
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to save settings: {e.Message}");
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    string json = File.ReadAllText(_settingsPath);
                    JsonUtility.FromJsonOverwrite(json, currentSettings);
                    Validate();
                    ApplySettings();
                }
                else
                {
                    ResetToDefaults();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load settings, using defaults: {e.Message}");
                ResetToDefaults();
            }
        }

        public void ApplySettings()
        {
            Validate();

            AudioListener.volume = currentSettings.masterVolume;

            Screen.SetResolution(
                currentSettings.resolutionWidth,
                currentSettings.resolutionHeight,
                currentSettings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed
            );

            QualitySettings.vSyncCount = currentSettings.vsync ? 1 : 0;

            if (!currentSettings.vsync)
                Application.targetFrameRate = 300;
            else
                Application.targetFrameRate = -1;

            int qualityIndex = currentSettings.graphicsQuality switch
            {
                EGraphicsQuality.Low => 0,
                EGraphicsQuality.Medium => 1,
                EGraphicsQuality.High => 2,
                EGraphicsQuality.Ultra => 3,
                _ => 2
            };

            if (qualityIndex < QualitySettings.names.Length)
                QualitySettings.SetQualityLevel(qualityIndex, true);

            if (InputManager.Instance != null)
            {
                InputManager.Instance.MouseSensitivity = currentSettings.mouseSensitivity;
                InputManager.Instance.InvertY = currentSettings.invertY;
            }

            OnSettingsChanged?.Invoke();
        }

        public void Validate()
        {
            currentSettings.masterVolume = Mathf.Clamp01(currentSettings.masterVolume);
            currentSettings.musicVolume = Mathf.Clamp01(currentSettings.musicVolume);
            currentSettings.sfxVolume = Mathf.Clamp01(currentSettings.sfxVolume);
            currentSettings.voiceVolume = Mathf.Clamp01(currentSettings.voiceVolume);
            currentSettings.mouseSensitivity = Mathf.Clamp(currentSettings.mouseSensitivity, 0.1f, 10f);
            currentSettings.fieldOfView = Mathf.Clamp(currentSettings.fieldOfView, 60, 120);
            currentSettings.resolutionWidth = Mathf.Max(640, currentSettings.resolutionWidth);
            currentSettings.resolutionHeight = Mathf.Max(480, currentSettings.resolutionHeight);
        }

        public void ResetToDefaults()
        {
            currentSettings = new GameSettings();
            ApplySettings();
            Save();
        }

        public void SetMasterVolume(float volume)
        {
            currentSettings.masterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = currentSettings.masterVolume;
            OnSettingsChanged?.Invoke();
        }

        public void SetGraphicsQuality(EGraphicsQuality quality)
        {
            currentSettings.graphicsQuality = quality;
            ApplySettings();
        }

        public void SetResolution(int width, int height, bool fullscreen)
        {
            currentSettings.resolutionWidth = Mathf.Max(640, width);
            currentSettings.resolutionHeight = Mathf.Max(480, height);
            currentSettings.fullscreen = fullscreen;
            ApplySettings();
        }

        public void SetFieldOfView(int fov)
        {
            currentSettings.fieldOfView = Mathf.Clamp(fov, 60, 120);
            OnSettingsChanged?.Invoke();
        }
    }
}
