using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SkyRingTerrarium.UI
{
    /// <summary>
    /// Pause menu with resume, settings, and quit options.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("Menu Panel")]
        [SerializeField] private GameObject menuPanel;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("References")]
        [SerializeField] private SettingsMenu settingsMenu;

        // Events
        public event Action OnPaused;
        public event Action OnResumed;

        private bool isPaused = false;
        public bool IsPaused => isPaused;

        private void Start()
        {
            // Setup button listeners
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);

            // Subscribe to input
            InputManager input = InputManager.Instance;
            if (input != null)
            {
                input.OnPausePressed += TogglePause;
            }

            // Start unpaused
            Resume();
        }

        private void Update()
        {
            // Fallback for pause input
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        #region Pause Control

        public void TogglePause()
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        public void Pause()
        {
            if (settingsMenu != null && settingsMenu.IsOpen)
                return; // Don't pause if settings is already open

            isPaused = true;
            Time.timeScale = 0f;

            if (menuPanel != null)
                menuPanel.SetActive(true);

            // Enable UI input only
            InputManager.Instance?.EnableUIOnly();

            OnPaused?.Invoke();
            AudioManager.Instance?.PlayUISound("pause");
        }

        public void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;

            if (menuPanel != null)
                menuPanel.SetActive(false);

            // Close settings if open
            if (settingsMenu != null && settingsMenu.IsOpen)
                settingsMenu.Close();

            // Re-enable player input
            InputManager.Instance?.EnablePlayerInput();

            OnResumed?.Invoke();
        }

        private void OpenSettings()
        {
            if (settingsMenu != null)
            {
                settingsMenu.Open();
            }
            AudioManager.Instance?.PlayUISound("button_click");
        }

        private void QuitGame()
        {
            // Save before quitting
            SaveSystem.Instance?.SaveGame();

            AudioManager.Instance?.PlayUISound("button_click");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        private void OnDestroy()
        {
            InputManager input = InputManager.Instance;
            if (input != null)
            {
                input.OnPausePressed -= TogglePause;
            }

            // Ensure time scale is reset
            Time.timeScale = 1f;
        }
    }
}
