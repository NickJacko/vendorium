using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // Einstellungen-Panel: Grafik, Audio, Steuerung.
    // Alle Einstellungen werden in PlayerPrefs gespeichert und beim Start geladen.
    public class SettingsPanel : MonoBehaviour
    {
        // --- Grafik ---
        [Header("Grafik")]
        [SerializeField] private TMP_Dropdown qualityDropdown;     // Low/Medium/High/Ultra
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;

        // --- Audio ---
        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider ambientVolumeSlider;

        [SerializeField] private TextMeshProUGUI masterVolumeLabel;
        [SerializeField] private TextMeshProUGUI musicVolumeLabel;
        [SerializeField] private TextMeshProUGUI sfxVolumeLabel;
        [SerializeField] private TextMeshProUGUI ambientVolumeLabel;

        // --- Steuerung ---
        [Header("Steuerung")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TextMeshProUGUI sensitivityLabel;

        // --- Buttons ---
        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;

        private Resolution[] _resolutions;
        private const string PREF_QUALITY     = "settings_quality";
        private const string PREF_RESOLUTION  = "settings_resolution";
        private const string PREF_FULLSCREEN  = "settings_fullscreen";
        private const string PREF_VSYNC       = "settings_vsync";
        private const string PREF_SENSITIVITY = "settings_sensitivity";

        private void Awake()
        {
            applyButton?.onClick.AddListener(OnApply);
            closeButton?.onClick.AddListener(Close);
            resetButton?.onClick.AddListener(OnReset);

            masterVolumeSlider?.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetMasterVolume(v);
                if (masterVolumeLabel != null) masterVolumeLabel.text = $"{v * 100:F0}%";
            });
            musicVolumeSlider?.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetMusicVolume(v);
                if (musicVolumeLabel != null) musicVolumeLabel.text = $"{v * 100:F0}%";
            });
            sfxVolumeSlider?.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetSFXVolume(v);
                if (sfxVolumeLabel != null) sfxVolumeLabel.text = $"{v * 100:F0}%";
            });
            ambientVolumeSlider?.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetAmbientVolume(v);
                if (ambientVolumeLabel != null) ambientVolumeLabel.text = $"{v * 100:F0}%";
            });
            sensitivitySlider?.onValueChanged.AddListener(v =>
            {
                if (sensitivityLabel != null) sensitivityLabel.text = $"{v:F1}";
            });

            gameObject.SetActive(false);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            LoadCurrentSettings();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void LoadCurrentSettings()
        {
            // Auflösungen befüllen
            PopulateResolutions();

            // Grafik-Werte
            if (qualityDropdown != null)
                qualityDropdown.value = PlayerPrefs.GetInt(PREF_QUALITY, QualitySettings.GetQualityLevel());

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;

            if (vsyncToggle != null)
                vsyncToggle.isOn = PlayerPrefs.GetInt(PREF_VSYNC, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;

            // Audio-Werte von AudioManager holen
            if (AudioManager.Instance != null)
            {
                SetSlider(masterVolumeSlider,  masterVolumeLabel,  AudioManager.Instance.GetMasterVolume());
                SetSlider(musicVolumeSlider,   musicVolumeLabel,   AudioManager.Instance.GetMusicVolume());
                SetSlider(sfxVolumeSlider,     sfxVolumeLabel,     AudioManager.Instance.GetSFXVolume());
                SetSlider(ambientVolumeSlider, ambientVolumeLabel, AudioManager.Instance.GetAmbientVolume());
            }

            // Maus-Sensitivity
            float sens = PlayerPrefs.GetFloat(PREF_SENSITIVITY, 2f);
            SetSlider(sensitivitySlider, sensitivityLabel, sens, $"{sens:F1}");
        }

        private void OnApply()
        {
            // Grafik
            if (qualityDropdown != null)
            {
                QualitySettings.SetQualityLevel(qualityDropdown.value, applyExpensiveChanges: true);
                PlayerPrefs.SetInt(PREF_QUALITY, qualityDropdown.value);
            }

            if (resolutionDropdown != null && _resolutions != null &&
                resolutionDropdown.value < _resolutions.Length)
            {
                var res = _resolutions[resolutionDropdown.value];
                bool fs = fullscreenToggle != null && fullscreenToggle.isOn;
                Screen.SetResolution(res.width, res.height, fs);
                PlayerPrefs.SetInt(PREF_RESOLUTION, resolutionDropdown.value);
                PlayerPrefs.SetInt(PREF_FULLSCREEN, fs ? 1 : 0);
            }

            if (vsyncToggle != null)
            {
                QualitySettings.vSyncCount = vsyncToggle.isOn ? 1 : 0;
                PlayerPrefs.SetInt(PREF_VSYNC, vsyncToggle.isOn ? 1 : 0);
            }

            // Maus-Sensitivity speichern (PlayerController liest PlayerPrefs beim Start)
            if (sensitivitySlider != null)
                PlayerPrefs.SetFloat(PREF_SENSITIVITY, sensitivitySlider.value);

            PlayerPrefs.Save();
            Close();
        }

        private void OnReset()
        {
            if (masterVolumeSlider  != null) masterVolumeSlider.value  = 1f;
            if (musicVolumeSlider   != null) musicVolumeSlider.value   = 0.7f;
            if (sfxVolumeSlider     != null) sfxVolumeSlider.value     = 1f;
            if (ambientVolumeSlider != null) ambientVolumeSlider.value = 0.5f;
            if (sensitivitySlider   != null) sensitivitySlider.value   = 2f;
            if (qualityDropdown     != null) qualityDropdown.value     = 2; // High
            if (fullscreenToggle    != null) fullscreenToggle.isOn     = true;
            if (vsyncToggle         != null) vsyncToggle.isOn          = true;
        }

        private void PopulateResolutions()
        {
            if (resolutionDropdown == null) return;

            _resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            var options = new List<string>();
            int currentIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                options.Add($"{_resolutions[i].width} x {_resolutions[i].height}");
                if (_resolutions[i].width  == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height)
                    currentIndex = i;
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = PlayerPrefs.GetInt(PREF_RESOLUTION, currentIndex);
            resolutionDropdown.RefreshShownValue();
        }

        private void SetSlider(Slider slider, TextMeshProUGUI label, float value, string labelText = null)
        {
            if (slider != null) slider.value = value;
            if (label  != null) label.text   = labelText ?? $"{value * 100:F0}%";
        }
    }
}
