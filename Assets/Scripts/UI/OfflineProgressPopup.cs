using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SkyRingTerrarium.UI
{
    /// <summary>
    /// Popup that displays offline progress report when returning to the game.
    /// </summary>
    public class OfflineProgressPopup : MonoBehaviour
    {
        [Header("Popup Panel")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI timeAwayText;
        [SerializeField] private TextMeshProUGUI currencyEarnedText;
        [SerializeField] private TextMeshProUGUI resourcesGatheredText;
        [SerializeField] private TextMeshProUGUI creaturesActivityText;
        [SerializeField] private TextMeshProUGUI eventsOccurredText;

        [Header("Button")]
        [SerializeField] private Button collectButton;
        [SerializeField] private TextMeshProUGUI collectButtonText;

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;

        // Events
        public event Action OnProgressCollected;
        public event Action OnPopupClosed;

        private OfflineProgressReport currentReport;

        private void Start()
        {
            if (collectButton != null)
            {
                collectButton.onClick.AddListener(CollectAndClose);
            }

            // Subscribe to offline progress manager
            OfflineProgressionManager offlineMgr = FindFirstObjectByType<OfflineProgressionManager>();
            if (offlineMgr != null)
            {
                offlineMgr.OnOfflineProgressCalculated += ShowReport;
            }

            // Start hidden
            Hide();
        }

        #region Public API

        public void ShowReport(OfflineProgressReport report)
        {
            if (report == null || report.TimeAwaySeconds < 60f)
            {
                // Don't show for very short absences
                return;
            }

            currentReport = report;
            PopulateUI(report);
            Show();

            AudioManager.Instance?.PlayUISound("popup_open");
        }

        public void Show()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
            }

            // Fade in
            if (canvasGroup != null)
            {
                StartCoroutine(FadeIn());
            }

            // Pause game while showing
            Time.timeScale = 0f;
        }

        public void Hide()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }

            Time.timeScale = 1f;
        }

        #endregion

        #region UI Population

        private void PopulateUI(OfflineProgressReport report)
        {
            if (titleText != null)
            {
                titleText.text = "Welcome Back!";
            }

            if (timeAwayText != null)
            {
                timeAwayText.text = FormatTimeAway(report.TimeAwaySeconds);
            }

            if (currencyEarnedText != null)
            {
                currencyEarnedText.text = $"+{report.CurrencyEarned:N0} Currency";
            }

            if (resourcesGatheredText != null)
            {
                resourcesGatheredText.text = $"{report.ResourcesGathered:N0} Resources Gathered";
            }

            if (creaturesActivityText != null)
            {
                creaturesActivityText.text = GetCreatureActivityText(report);
            }

            if (eventsOccurredText != null)
            {
                eventsOccurredText.text = $"{report.EventsOccurred} Events Occurred";
            }

            if (collectButtonText != null)
            {
                collectButtonText.text = "Collect";
            }
        }

        private string FormatTimeAway(float seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);

            if (time.TotalDays >= 1)
            {
                return $"You were away for {time.Days} day{(time.Days > 1 ? "s" : "")} and {time.Hours} hour{(time.Hours != 1 ? "s" : "")}";
            }
            else if (time.TotalHours >= 1)
            {
                return $"You were away for {time.Hours} hour{(time.Hours != 1 ? "s" : "")} and {time.Minutes} minute{(time.Minutes != 1 ? "s" : "")}";
            }
            else
            {
                return $"You were away for {time.Minutes} minute{(time.Minutes != 1 ? "s" : "")}";
            }
        }

        private string GetCreatureActivityText(OfflineProgressReport report)
        {
            if (report.CreaturesBorn > 0 && report.CreaturesDied > 0)
            {
                return $"{report.CreaturesBorn} creatures born, {report.CreaturesDied} passed away";
            }
            else if (report.CreaturesBorn > 0)
            {
                return $"{report.CreaturesBorn} new creatures born!";
            }
            else if (report.CreaturesDied > 0)
            {
                return $"{report.CreaturesDied} creatures passed away";
            }
            return "Creatures thrived in your absence";
        }

        #endregion

        #region Actions

        private void CollectAndClose()
        {
            // Apply the offline progress
            if (currentReport != null)
            {
                ApplyProgress(currentReport);
            }

            OnProgressCollected?.Invoke();
            AudioManager.Instance?.PlayUISound("collect");

            // Fade out and close
            StartCoroutine(FadeOutAndClose());
        }

        private void ApplyProgress(OfflineProgressReport report)
        {
            // Add currency
            UpgradeManager.Instance?.AddCurrency(report.CurrencyEarned);

            // Other progress is typically already simulated by OfflineProgressionManager
            // This just confirms collection
        }

        #endregion

        #region Animation

        private System.Collections.IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = elapsed / fadeInDuration;
                }
                yield return null;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        private System.Collections.IEnumerator FadeOutAndClose()
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f - (elapsed / fadeOutDuration);
                }
                yield return null;
            }

            Hide();
            OnPopupClosed?.Invoke();
        }

        #endregion

        private void OnDestroy()
        {
            OfflineProgressionManager offlineMgr = FindFirstObjectByType<OfflineProgressionManager>();
            if (offlineMgr != null)
            {
                offlineMgr.OnOfflineProgressCalculated -= ShowReport;
            }
        }
    }
}
