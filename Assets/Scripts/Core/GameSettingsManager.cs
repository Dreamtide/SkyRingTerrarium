using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Manages game settings including quality presets, audio, and controls.
    /// </summary>
    public class GameSettingsManager : MonoBehaviour
    {
        public static GameSettingsManager Instance { get; private set; }

        [Header("Quality Presets")]
        [SerializeField] private QualityPreset lowPreset;
        [SerializeField] private QualityPreset mediumPreset;
        [SerializeField] private QualityPreset highPreset;

        // Events
        public event Action<QualityLevel> OnQualityChanged;
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;

        // Current Settings
        private GameSettings currentSettings;

        public GameSettings CurrentSettings => currentSettings;
        public QualityLevel CurrentQuality => currentSettings.qualityLevel;

        private const string SETTINGS_KEY = "game_settings";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePresets();
            LoadSettings();
        }

        private void InitializePresets()
        {
            // Low Quality - for older devices
            lowPreset = new QualityPreset
            {
                particleMultiplier = 0.3f,
                creatureMultiplier = 0.5f,
                postProcessingEnabled = false,
                bloomEnabled = false,
                ambientOcclusionEnabled = false,
                shadowQuality = 0,
                textureQuality = 2, // Quarter resolution
                targetFrameRate = 30
            };

            // Medium Quality - balanced
            mediumPreset = new QualityPreset
            {
                particleMultiplier = 0.7f,
                creatureMultiplier = 0.8f,
                postProcessingEnabled = true,
                bloomEnabled = false,
                ambientOcclusionEnabled = false,
                shadowQuality = 1,
                textureQuality = 1, // Half resolution
                targetFrameRate = 60
            };

            // High Quality - full visuals
            highPreset = new QualityPreset
            {
                particleMultiplier = 1f,
                creatureMultiplier = 1f,
                postProcessingEnabled = true,
                bloomEnabled = true,
                ambientOcclusionEnabled = true,
                shadowQuality = 2,
                textureQuality = 0, // Full resolution
                targetFrameRate = 60
            };
        }

        #region Public API

        public void SetQualityLevel(QualityLevel level)
        {
            currentSettings.qualityLevel = level;
            ApplyQualitySettings();
            SaveSettings();
            OnQualityChanged?.Invoke(level);
        }

        public void SetMasterVolume(float volume)
        {
            currentSettings.masterVolume = Mathf.Clamp01(volume);
            AudioManager.Instance?.SetMasterVolume(currentSettings.masterVolume);
            SaveSettings();
            OnMasterVolumeChanged?.Invoke(currentSettings.masterVolume);
        }

        public void SetMusicVolume(float volume)
        {
            currentSettings.musicVolume = Mathf.Clamp01(volume);
            AudioManager.Instance?.SetMusicVolume(currentSettings.musicVolume);
            SaveSettings();
            OnMusicVolumeChanged?.Invoke(currentSettings.musicVolume);
        }

        public void SetSFXVolume(float volume)
        {
            currentSettings.sfxVolume = Mathf.Clamp01(volume);
            AudioManager.Instance?.SetSFXVolume(currentSettings.sfxVolume);
            SaveSettings();
            OnSFXVolumeChanged?.Invoke(currentSettings.sfxVolume);
        }

        public void SetScreenShake(bool enabled)
        {
            currentSettings.screenShakeEnabled = enabled;
            SaveSettings();
        }

        public void SetParticles(bool enabled)
        {
            currentSettings.particlesEnabled = enabled;
            SaveSettings();
        }

        public QualityPreset GetCurrentPreset()
        {
            return currentSettings.qualityLevel switch
            {
                QualityLevel.Low => lowPreset,
                QualityLevel.Medium => mediumPreset,
                QualityLevel.High => highPreset,
                _ => mediumPreset
            };
        }

        public float GetParticleMultiplier()
        {
            if (!currentSettings.particlesEnabled) return 0f;
            return GetCurrentPreset().particleMultiplier;
        }

        public float GetCreatureMultiplier()
        {
            return GetCurrentPreset().creatureMultiplier;
        }

        public bool IsPostProcessingEnabled()
        {
            return GetCurrentPreset().postProcessingEnabled;
        }

        public bool IsBloomEnabled()
        {
            return GetCurrentPreset().bloomEnabled;
        }

        public bool IsAmbientOcclusionEnabled()
        {
            return GetCurrentPreset().ambientOcclusionEnabled;
        }

        public void ResetToDefaults()
        {
            currentSettings = GetDefaultSettings();
            ApplyQualitySettings();
            SaveSettings();
        }

        #endregion

        #region Settings Persistence

        private void LoadSettings()
        {
            string json = PlayerPrefs.GetString(SETTINGS_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                currentSettings = GetDefaultSettings();
            }
            else
            {
                try
                {
                    currentSettings = JsonUtility.FromJson<GameSettings>(json);
                }
                catch
                {
                    currentSettings = GetDefaultSettings();
                }
            }

            ApplyQualitySettings();
        }

        private void SaveSettings()
        {
            string json = JsonUtility.ToJson(currentSettings);
            PlayerPrefs.SetString(SETTINGS_KEY, json);
            PlayerPrefs.Save();
        }

        private GameSettings GetDefaultSettings()
        {
            return new GameSettings
            {
                qualityLevel = QualityLevel.Medium,
                masterVolume = 1f,
                musicVolume = 0.7f,
                sfxVolume = 1f,
                screenShakeEnabled = true,
                particlesEnabled = true
            };
        }

        #endregion

        #region Apply Settings

        private void ApplyQualitySettings()
        {
            QualityPreset preset = GetCurrentPreset();

            // Unity Quality Settings
            QualitySettings.SetQualityLevel((int)currentSettings.qualityLevel, true);

            // Texture Quality
            QualitySettings.globalTextureMipmapLimit = preset.textureQuality;

            // Target Frame Rate
            Application.targetFrameRate = preset.targetFrameRate;

            // Notify systems that need to adjust
            EcosystemManager ecosystem = FindFirstObjectByType<EcosystemManager>();
            ecosystem?.SetCreatureMultiplier(preset.creatureMultiplier);

            // Apply audio settings
            AudioManager audio = AudioManager.Instance;
            if (audio != null)
            {
                audio.SetMasterVolume(currentSettings.masterVolume);
                audio.SetMusicVolume(currentSettings.musicVolume);
                audio.SetSFXVolume(currentSettings.sfxVolume);
            }

            Debug.Log($"[GameSettings] Applied quality level: {currentSettings.qualityLevel}");
        }

        #endregion
    }

    public enum QualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    [Serializable]
    public class QualityPreset
    {
        public float particleMultiplier;
        public float creatureMultiplier;
        public bool postProcessingEnabled;
        public bool bloomEnabled;
        public bool ambientOcclusionEnabled;
        public int shadowQuality;
        public int textureQuality;
        public int targetFrameRate;
    }

    [Serializable]
    public class GameSettings
    {
        public QualityLevel qualityLevel;
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        public bool screenShakeEnabled;
        public bool particlesEnabled;
    }
}
