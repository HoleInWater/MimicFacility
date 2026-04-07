using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MimicFacility.Core;
using UnityEngine.Audio;

namespace MimicFacility.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider voiceVolumeSlider;
        [SerializeField] private AudioMixer audioMixer;

        [Header("Video")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Slider fieldOfViewSlider;
        [SerializeField] private TextMeshProUGUI fovValueText;

        [Header("Controls")]
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private Toggle invertYToggle;
        [SerializeField] private Button rebindInteractButton;
        [SerializeField] private Button rebindFlashlightButton;

        [Header("Horror")]
        [SerializeField] private Toggle enableDeviceHorrorToggle;
        [SerializeField] private Toggle enableSubliminalFramesToggle;
        [SerializeField] private TextMeshProUGUI horrorWarningText;

        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button revertButton;
        [SerializeField] private Button resetDefaultsButton;

        private Resolution[] availableResolutions;
        private List<string> resolutionOptions = new List<string>();

        private void OnEnable()
        {
            SetupListeners();
            PopulateResolutions();
            PopulateQualityLevels();
            LoadCurrentSettings();
        }

        private void SetupListeners()
        {
            masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
            musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
            sfxVolumeSlider?.onValueChanged.AddListener(OnSfxVolumeChanged);
            voiceVolumeSlider?.onValueChanged.AddListener(OnVoiceVolumeChanged);
            fieldOfViewSlider?.onValueChanged.AddListener(OnFovChanged);
            mouseSensitivitySlider?.onValueChanged.AddListener(OnSensitivityChanged);

            fullscreenToggle?.onValueChanged.AddListener(_ => { });
            vsyncToggle?.onValueChanged.AddListener(_ => { });
            invertYToggle?.onValueChanged.AddListener(_ => { });
            enableDeviceHorrorToggle?.onValueChanged.AddListener(_ => { });
            enableSubliminalFramesToggle?.onValueChanged.AddListener(_ => { });

            applyButton?.onClick.AddListener(OnApplyClicked);
            revertButton?.onClick.AddListener(OnRevertClicked);
            resetDefaultsButton?.onClick.AddListener(OnResetDefaultsClicked);
        }

        private void PopulateResolutions()
        {
            if (resolutionDropdown == null) return;

            var seen = new HashSet<string>();
            var filtered = new List<Resolution>();

            foreach (var res in Screen.resolutions.OrderByDescending(r => r.width).ThenByDescending(r => r.height))
            {
                string key = $"{res.width}x{res.height}";
                if (seen.Add(key))
                    filtered.Add(res);
            }

            availableResolutions = filtered.ToArray();
            resolutionOptions.Clear();
            resolutionDropdown.ClearOptions();

            foreach (var res in availableResolutions)
            {
                string label = $"{res.width}x{res.height} @ {Mathf.RoundToInt((float)res.refreshRateRatio.value)}Hz";
                resolutionOptions.Add(label);
            }

            resolutionDropdown.AddOptions(resolutionOptions);
        }

        private void PopulateQualityLevels()
        {
            if (qualityDropdown == null) return;

            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(QualitySettings.names.ToList());
        }

        public void LoadCurrentSettings()
        {
            var sm = SettingsManager.Instance;
            if (sm == null) return;

            var s = sm.Settings;

            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(s.masterVolume);
            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(s.musicVolume);
            if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(s.sfxVolume);
            if (voiceVolumeSlider != null) voiceVolumeSlider.SetValueWithoutNotify(s.voiceVolume);

            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(s.fullscreen);
            if (vsyncToggle != null) vsyncToggle.SetIsOnWithoutNotify(s.vsync);
            if (fieldOfViewSlider != null) fieldOfViewSlider.SetValueWithoutNotify(s.fieldOfView);
            if (fovValueText != null) fovValueText.text = s.fieldOfView.ToString();

            if (mouseSensitivitySlider != null) mouseSensitivitySlider.SetValueWithoutNotify(s.mouseSensitivity);
            if (invertYToggle != null) invertYToggle.SetIsOnWithoutNotify(s.invertY);

            if (enableDeviceHorrorToggle != null) enableDeviceHorrorToggle.SetIsOnWithoutNotify(s.enableDeviceHorror);
            if (enableSubliminalFramesToggle != null) enableSubliminalFramesToggle.SetIsOnWithoutNotify(s.enableSubliminalFrames);

            if (horrorWarningText != null)
                horrorWarningText.text = "These effects extend beyond the game window. They are designed to be unsettling.";

            if (qualityDropdown != null)
            {
                int qi = (int)s.graphicsQuality;
                if (qi < qualityDropdown.options.Count)
                    qualityDropdown.SetValueWithoutNotify(qi);
            }

            if (resolutionDropdown != null && availableResolutions != null)
            {
                for (int i = 0; i < availableResolutions.Length; i++)
                {
                    if (availableResolutions[i].width == s.resolutionWidth
                        && availableResolutions[i].height == s.resolutionHeight)
                    {
                        resolutionDropdown.SetValueWithoutNotify(i);
                        break;
                    }
                }
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (audioMixer != null)
                audioMixer.SetFloat("MasterVolume", LinearToDecibel(value));
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (audioMixer != null)
                audioMixer.SetFloat("MusicVolume", LinearToDecibel(value));
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (audioMixer != null)
                audioMixer.SetFloat("SFXVolume", LinearToDecibel(value));
        }

        private void OnVoiceVolumeChanged(float value)
        {
            if (audioMixer != null)
                audioMixer.SetFloat("VoiceVolume", LinearToDecibel(value));
        }

        private void OnFovChanged(float value)
        {
            if (fovValueText != null) fovValueText.text = Mathf.RoundToInt(value).ToString();
        }

        private void OnSensitivityChanged(float value) { }

        public void OnApplyClicked()
        {
            var sm = SettingsManager.Instance;
            if (sm == null) return;

            var s = sm.Settings;
            s.masterVolume = masterVolumeSlider != null ? masterVolumeSlider.value : s.masterVolume;
            s.musicVolume = musicVolumeSlider != null ? musicVolumeSlider.value : s.musicVolume;
            s.sfxVolume = sfxVolumeSlider != null ? sfxVolumeSlider.value : s.sfxVolume;
            s.voiceVolume = voiceVolumeSlider != null ? voiceVolumeSlider.value : s.voiceVolume;
            s.fullscreen = fullscreenToggle != null && fullscreenToggle.isOn;
            s.vsync = vsyncToggle != null && vsyncToggle.isOn;
            s.fieldOfView = fieldOfViewSlider != null ? Mathf.RoundToInt(fieldOfViewSlider.value) : s.fieldOfView;
            s.mouseSensitivity = mouseSensitivitySlider != null ? mouseSensitivitySlider.value : s.mouseSensitivity;
            s.invertY = invertYToggle != null && invertYToggle.isOn;
            s.enableDeviceHorror = enableDeviceHorrorToggle != null && enableDeviceHorrorToggle.isOn;
            s.enableSubliminalFrames = enableSubliminalFramesToggle != null && enableSubliminalFramesToggle.isOn;

            if (qualityDropdown != null)
                s.graphicsQuality = (EGraphicsQuality)qualityDropdown.value;

            if (resolutionDropdown != null && availableResolutions != null
                && resolutionDropdown.value < availableResolutions.Length)
            {
                var res = availableResolutions[resolutionDropdown.value];
                s.resolutionWidth = res.width;
                s.resolutionHeight = res.height;
            }

            sm.ApplySettings();
            sm.Save();
        }

        public void OnRevertClicked()
        {
            var sm = SettingsManager.Instance;
            if (sm == null) return;
            sm.Load();
            LoadCurrentSettings();
        }

        public void OnResetDefaultsClicked()
        {
            var sm = SettingsManager.Instance;
            if (sm == null) return;
            sm.ResetToDefaults();
            LoadCurrentSettings();
        }

        private static float LinearToDecibel(float linear)
        {
            return linear > 0.0001f ? 20f * Mathf.Log10(linear) : -80f;
        }
    }
}
