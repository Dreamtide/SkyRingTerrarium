using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SkyRingTerrarium.UI
{
    /// <summary>
    /// Settings menu for quality, audio, and control options.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        [Header("Menu Panel")]
        [SerializeField] private GameObject menuPanel;

        [Header("Quality Settings")]
        [SerializeField] private TMP_Dropdown qualityDropdown;

        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeText;
        [SerializeField] private TextMeshProUGUI musicVolumeText;
        [SerializeField] private TextMeshProUGUI sfxVolumeText;

        [Header("Visual Settings")]
        [SerializeField] private Toggle screenShakeToggle;
        [SerializeField] private Toggle particlesToggle;

        [Header("Control Settings")]
        [SerializeField] private Button rebindMoveButton;
        [SerializeField] private Button rebindJumpButton;
        [SerializeField] private Button rebindInteractButton;
        [SerializeField] private Button resetBindingsButton;
        [SerializeField] private TextMeshProUGUI moveBindingText;
        [SerializeField] private TextMeshProUGUI jumpBindingText;
        [SerializeField] private TextMeshProUGUI interactBindingText;

        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetDefaultsButton;

        private bool isOpen = false;
        public bool IsOpen => isOpen;

        // Events
        public event Action OnSettingsOpened;
        public event Action OnSettingsClosed;

        private void Start()
        {
            SetupUI();
            LoadCurrentSettings();
            Close();
        }

        private void SetupUI()
        {
            // Quality dropdown
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "Low", "Medium", "High" });
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }

            // Volume sliders
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.minValue = 0f;
                masterVolumeSlider.maxValue = 1f;
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            // Toggles
            if (screenShakeToggle != null)
                screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
            if (particlesToggle != null)
                particlesToggle.onValueChanged.AddListener(OnParticlesChanged);

            // Rebind buttons
            if (rebindMoveButton != null)
                rebindMoveButton.onClick.AddListener(() => StartRebind("Move"));
            if (rebindJumpButton != null)
                rebindJumpButton.onClick.AddListener(() => StartRebind("Jump"));
            if (rebindInteractButton != null)
                rebindInteractButton.onClick.AddListener(() => StartRebind("Interact"));
            if (resetBindingsButton != null)
                resetBindingsButton.onClick.AddListener(ResetAllBindings);

            // Action buttons
            if (backButton != null)
                backButton.onClick.AddListener(Close);
            if (resetDefaultsButton != null)
                resetDefaultsButton.onClick.AddListener(ResetToDefaults);

            // Subscribe to input manager events
            InputManager input = InputManager.Instance;
            if (input != null)
            {
                input.OnBindingChanged += OnBindingChanged;
                input.OnRebindStarted += OnRebindStarted;
                input.OnRebindComplete += OnRebindComplete;
            }
        }

        private void LoadCurrentSettings()
        {
            GameSettingsManager settings = GameSettingsManager.Instance;
            if (settings == null) return;

            GameSettings current = settings.CurrentSettings;

            // Quality
            if (qualityDropdown != null)
                qualityDropdown.value = (int)current.qualityLevel;

            // Audio
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = current.masterVolume;
                UpdateVolumeText(masterVolumeText, current.masterVolume);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = current.musicVolume;
                UpdateVolumeText(musicVolumeText, current.musicVolume);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = current.sfxVolume;
                UpdateVolumeText(sfxVolumeText, current.sfxVolume);
            }

            // Toggles
            if (screenShakeToggle != null)
                screenShakeToggle.isOn = current.screenShakeEnabled;
            if (particlesToggle != null)
                particlesToggle.isOn = current.particlesEnabled;

            // Load binding display strings
            UpdateBindingDisplays();
        }

        #region Menu Control

        public void Open()
        {
            if (menuPanel != null)
                menuPanel.SetActive(true);
            isOpen = true;
            LoadCurrentSettings();
            OnSettingsOpened?.Invoke();
        }

        public void Close()
        {
            if (menuPanel != null)
                menuPanel.SetActive(false);
            isOpen = false;
            OnSettingsClosed?.Invoke();
        }

        #endregion

        #region Settings Callbacks

        private void OnQualityChanged(int index)
        {
            GameSettingsManager.Instance?.SetQualityLevel((QualityLevel)index);
            AudioManager.Instance?.PlayUISound("slider_change");
        }

        private void OnMasterVolumeChanged(float value)
        {
            GameSettingsManager.Instance?.SetMasterVolume(value);
            UpdateVolumeText(masterVolumeText, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            GameSettingsManager.Instance?.SetMusicVolume(value);
            UpdateVolumeText(musicVolumeText, value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            GameSettingsManager.Instance?.SetSFXVolume(value);
            UpdateVolumeText(sfxVolumeText, value);
            AudioManager.Instance?.PlayUISound("slider_change"); // Preview sound
        }

        private void OnScreenShakeChanged(bool enabled)
        {
            GameSettingsManager.Instance?.SetScreenShake(enabled);
            AudioManager.Instance?.PlayUISound("toggle");
        }

        private void OnParticlesChanged(bool enabled)
        {
            GameSettingsManager.Instance?.SetParticles(enabled);
            AudioManager.Instance?.PlayUISound("toggle");
        }

        private void UpdateVolumeText(TextMeshProUGUI text, float value)
        {
            if (text != null)
            {
                text.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        #endregion

        #region Input Rebinding

        private void StartRebind(string actionName)
        {
            InputManager.Instance?.StartRebind(actionName);
            AudioManager.Instance?.PlayUISound("button_click");
        }

        private void OnRebindStarted(string actionName)
        {
            // Update UI to show "Press key..." prompt
            TextMeshProUGUI targetText = actionName switch
            {
                "Move" => moveBindingText,
                "Jump" => jumpBindingText,
                "Interact" => interactBindingText,
                _ => null
            };

            if (targetText != null)
            {
                targetText.text = "Press key...";
                targetText.color = Color.yellow;
            }
        }

        private void OnRebindComplete(string actionName)
        {
            UpdateBindingDisplays();
            AudioManager.Instance?.PlayUISound("rebind_complete");
        }

        private void OnBindingChanged(string actionName, string newBinding)
        {
            UpdateBindingDisplays();
        }

        private void UpdateBindingDisplays()
        {
            InputManager input = InputManager.Instance;
            if (input == null) return;

            if (moveBindingText != null)
            {
                moveBindingText.text = input.GetBindingDisplayString("Move");
                moveBindingText.color = Color.white;
            }
            if (jumpBindingText != null)
            {
                jumpBindingText.text = input.GetBindingDisplayString("Jump");
                jumpBindingText.color = Color.white;
            }
            if (interactBindingText != null)
            {
                interactBindingText.text = input.GetBindingDisplayString("Interact");
                interactBindingText.color = Color.white;
            }
        }

        private void ResetAllBindings()
        {
            InputManager.Instance?.ResetAllBindings();
            UpdateBindingDisplays();
            AudioManager.Instance?.PlayUISound("reset");
        }

        #endregion

        private void ResetToDefaults()
        {
            GameSettingsManager.Instance?.ResetToDefaults();
            InputManager.Instance?.ResetAllBindings();
            LoadCurrentSettings();
            AudioManager.Instance?.PlayUISound("reset");
        }

        private void OnDestroy()
        {
            InputManager input = InputManager.Instance;
            if (input != null)
            {
                input.OnBindingChanged -= OnBindingChanged;
                input.OnRebindStarted -= OnRebindStarted;
                input.OnRebindComplete -= OnRebindComplete;
            }
        }
    }
}
