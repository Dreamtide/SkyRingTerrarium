using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SkyRingTerrarium.UI
{
    /// <summary>
    /// Main game HUD displaying currency, day/season/time, weather icon, and player stats.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI currencyText;
        [SerializeField] private Image currencyIcon;

        [Header("Time Display")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI seasonText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Image dayNightIcon;

        [Header("Weather Display")]
        [SerializeField] private Image weatherIcon;
        [SerializeField] private TextMeshProUGUI weatherText;

        [Header("Player Stats")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Weather Icons")]
        [SerializeField] private Sprite clearWeatherIcon;
        [SerializeField] private Sprite windyWeatherIcon;
        [SerializeField] private Sprite stormyWeatherIcon;
        [SerializeField] private Sprite moteShowerIcon;
        [SerializeField] private Sprite auroraWindIcon;

        [Header("Day/Night Icons")]
        [SerializeField] private Sprite dayIcon;
        [SerializeField] private Sprite nightIcon;

        // References
        private UpgradeManager upgradeManager;
        private WorldTimeManager timeManager;
        private WeatherSystem weatherSystem;
        private PlayerController player;

        private void Start()
        {
            // Find managers
            upgradeManager = UpgradeManager.Instance;
            timeManager = FindFirstObjectByType<WorldTimeManager>();
            weatherSystem = FindFirstObjectByType<WeatherSystem>();
            player = FindFirstObjectByType<PlayerController>();

            // Subscribe to events
            if (upgradeManager != null)
            {
                upgradeManager.OnCurrencyChanged += UpdateCurrencyDisplay;
                UpdateCurrencyDisplay(upgradeManager.Currency);
            }

            if (timeManager != null)
            {
                timeManager.OnTimeChanged += UpdateTimeDisplay;
                timeManager.OnDayChanged += UpdateDayDisplay;
                timeManager.OnSeasonChanged += UpdateSeasonDisplay;
                timeManager.OnDayNightChanged += UpdateDayNightIcon;
                
                // Initial update
                UpdateTimeDisplay(timeManager.CurrentWorldTime);
                UpdateDayDisplay(timeManager.CurrentDay);
                UpdateSeasonDisplay(timeManager.CurrentSeason);
                UpdateDayNightIcon(timeManager.IsDay);
            }

            if (weatherSystem != null)
            {
                weatherSystem.OnWeatherChanged += UpdateWeatherDisplay;
                UpdateWeatherDisplay(weatherSystem.CurrentWeather);
            }
        }

        private void Update()
        {
            UpdatePlayerStats();
        }

        #region Display Updates

        private void UpdateCurrencyDisplay(int amount)
        {
            if (currencyText != null)
            {
                currencyText.text = FormatCurrency(amount);
            }
        }

        private void UpdateTimeDisplay(float worldTime)
        {
            if (timeText != null)
            {
                int hours = Mathf.FloorToInt(worldTime * 24f) % 24;
                int minutes = Mathf.FloorToInt((worldTime * 24f * 60f) % 60f);
                timeText.text = $"{hours:D2}:{minutes:D2}";
            }
        }

        private void UpdateDayDisplay(int day)
        {
            if (dayText != null)
            {
                dayText.text = $"Day {day}";
            }
        }

        private void UpdateSeasonDisplay(Season season)
        {
            if (seasonText != null)
            {
                seasonText.text = GetSeasonName(season);
            }
        }

        private void UpdateDayNightIcon(bool isDay)
        {
            if (dayNightIcon != null)
            {
                dayNightIcon.sprite = isDay ? dayIcon : nightIcon;
            }
        }

        private void UpdateWeatherDisplay(WeatherType weather)
        {
            if (weatherIcon != null)
            {
                weatherIcon.sprite = GetWeatherIcon(weather);
            }

            if (weatherText != null)
            {
                weatherText.text = GetWeatherName(weather);
            }
        }

        private void UpdatePlayerStats()
        {
            if (player == null) return;

            // Health display would come from player component
            // This is a placeholder for the health system
            if (healthBar != null)
            {
                // healthBar.value = player.HealthPercent;
            }

            if (healthText != null)
            {
                // healthText.text = $"{player.CurrentHealth}/{player.MaxHealth}";
            }
        }

        #endregion

        #region Helpers

        private string FormatCurrency(int amount)
        {
            if (amount >= 1000000)
                return $"{amount / 1000000f:F1}M";
            if (amount >= 1000)
                return $"{amount / 1000f:F1}K";
            return amount.ToString();
        }

        private string GetSeasonName(Season season)
        {
            return season switch
            {
                Season.Spring => "Spring",
                Season.Summer => "Summer",
                Season.Autumn => "Autumn",
                Season.Winter => "Winter",
                _ => "Unknown"
            };
        }

        private string GetWeatherName(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => "Clear",
                WeatherType.Windy => "Windy",
                WeatherType.Stormy => "Storm",
                WeatherType.MoteShower => "Mote Shower",
                WeatherType.AuroraWind => "Aurora",
                _ => "Unknown"
            };
        }

        private Sprite GetWeatherIcon(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => clearWeatherIcon,
                WeatherType.Windy => windyWeatherIcon,
                WeatherType.Stormy => stormyWeatherIcon,
                WeatherType.MoteShower => moteShowerIcon,
                WeatherType.AuroraWind => auroraWindIcon,
                _ => clearWeatherIcon
            };
        }

        #endregion

        private void OnDestroy()
        {
            if (upgradeManager != null)
            {
                upgradeManager.OnCurrencyChanged -= UpdateCurrencyDisplay;
            }

            if (timeManager != null)
            {
                timeManager.OnTimeChanged -= UpdateTimeDisplay;
                timeManager.OnDayChanged -= UpdateDayDisplay;
                timeManager.OnSeasonChanged -= UpdateSeasonDisplay;
                timeManager.OnDayNightChanged -= UpdateDayNightIcon;
            }

            if (weatherSystem != null)
            {
                weatherSystem.OnWeatherChanged -= UpdateWeatherDisplay;
            }
        }
    }
}
