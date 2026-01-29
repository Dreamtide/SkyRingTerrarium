using UnityEngine;
using System;
using SkyRingTerrarium.World;
using SkyRingTerrarium.Ecosystem;

namespace SkyRingTerrarium.Terrarium
{
    /// <summary>
    /// Enhanced Terrarium Simulator that integrates all living world systems.
    /// Acts as the central hub connecting environment, ecosystem, and time systems.
    /// </summary>
    public class TerrariumSimulator : MonoBehaviour
    {
        public static TerrariumSimulator Instance { get; private set; }

        #region Events
        public static event Action<float> OnTemperatureChanged;
        public static event Action<float> OnHumidityChanged;
        public static event Action<TerrariumState> OnStateChanged;
        #endregion

        #region Serialized Fields
        [Header("Environment Bounds")]
        [SerializeField] private float minTemperature = -40f;
        [SerializeField] private float maxTemperature = 60f;
        [SerializeField] private float minHumidity = 0f;
        [SerializeField] private float maxHumidity = 100f;

        [Header("Base Conditions")]
        [SerializeField] private float baseTemperature = 22f;
        [SerializeField] private float baseHumidity = 65f;

        [Header("Time Effects")]
        [SerializeField] private float dayTemperatureBonus = 15f;
        [SerializeField] private float nightTemperaturePenalty = 10f;

        [Header("Season Effects")]
        [SerializeField] private float springTempMod = 0f;
        [SerializeField] private float summerTempMod = 10f;
        [SerializeField] private float autumnTempMod = -5f;
        [SerializeField] private float winterTempMod = -20f;
        [SerializeField] private float springHumidityMod = 10f;
        [SerializeField] private float summerHumidityMod = -10f;
        [SerializeField] private float autumnHumidityMod = 5f;
        [SerializeField] private float winterHumidityMod = -5f;

        [Header("Weather Effects")]
        [SerializeField] private float stormyTempMod = -5f;
        [SerializeField] private float stormyHumidityMod = 30f;
        [SerializeField] private float windyTempMod = -3f;
        [SerializeField] private float mistyHumidityMod = 40f;

        [Header("Simulation")]
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private float smoothingSpeed = 2f;
        #endregion

        #region Public Properties
        public float Temperature => currentTemperature;
        public float Humidity => currentHumidity;
        public float NormalizedTemperature => (currentTemperature - minTemperature) / (maxTemperature - minTemperature);
        public float NormalizedHumidity => currentHumidity / maxHumidity;
        public TerrariumState CurrentState => currentState;
        #endregion

        #region Private Fields
        private float currentTemperature;
        private float currentHumidity;
        private float targetTemperature;
        private float targetHumidity;
        private float updateTimer;
        private TerrariumState currentState;
        private TerrariumState previousState;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            currentTemperature = baseTemperature;
            currentHumidity = baseHumidity;
            targetTemperature = baseTemperature;
            targetHumidity = baseHumidity;
        }

        private void Start()
        {
            SubscribeToEvents();
            UpdateTargetConditions();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateTargetConditions();
            }

            SmoothConditions();
            UpdateState();
        }
        #endregion

        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            WorldTimeManager.OnTimeOfDayPhaseChanged += OnTimePhaseChanged;
            WorldTimeManager.OnSeasonChanged += OnSeasonChanged;
            WeatherSystem.OnWeatherChanged += OnWeatherChanged;
            WorldEventManager.OnEventStarted += OnWorldEventStarted;
            WorldEventManager.OnEventEnded += OnWorldEventEnded;
        }

        private void UnsubscribeFromEvents()
        {
            WorldTimeManager.OnTimeOfDayPhaseChanged -= OnTimePhaseChanged;
            WorldTimeManager.OnSeasonChanged -= OnSeasonChanged;
            WeatherSystem.OnWeatherChanged -= OnWeatherChanged;
            WorldEventManager.OnEventStarted -= OnWorldEventStarted;
            WorldEventManager.OnEventEnded -= OnWorldEventEnded;
        }
        #endregion

        #region Condition Updates
        private void UpdateTargetConditions()
        {
            targetTemperature = baseTemperature;
            targetHumidity = baseHumidity;

            // Apply time of day effects
            ApplyTimeEffects();
            
            // Apply season effects
            ApplySeasonEffects();
            
            // Apply weather effects
            ApplyWeatherEffects();
            
            // Apply world event effects
            ApplyEventEffects();

            // Clamp to bounds
            targetTemperature = Mathf.Clamp(targetTemperature, minTemperature, maxTemperature);
            targetHumidity = Mathf.Clamp(targetHumidity, minHumidity, maxHumidity);
        }

        private void ApplyTimeEffects()
        {
            if (WorldTimeManager.Instance == null) return;

            var phase = WorldTimeManager.Instance.CurrentTimeOfDayPhase;
            targetTemperature += phase switch
            {
                WorldTimeManager.TimeOfDay.Day => dayTemperatureBonus,
                WorldTimeManager.TimeOfDay.Dawn => dayTemperatureBonus * 0.3f,
                WorldTimeManager.TimeOfDay.Dusk => -nightTemperaturePenalty * 0.3f,
                WorldTimeManager.TimeOfDay.Night => -nightTemperaturePenalty,
                _ => 0f
            };
        }

        private void ApplySeasonEffects()
        {
            if (WorldTimeManager.Instance == null) return;

            var season = WorldTimeManager.Instance.CurrentSeason;
            
            targetTemperature += season switch
            {
                WorldTimeManager.Season.Spring => springTempMod,
                WorldTimeManager.Season.Summer => summerTempMod,
                WorldTimeManager.Season.Autumn => autumnTempMod,
                WorldTimeManager.Season.Winter => winterTempMod,
                _ => 0f
            };

            targetHumidity += season switch
            {
                WorldTimeManager.Season.Spring => springHumidityMod,
                WorldTimeManager.Season.Summer => summerHumidityMod,
                WorldTimeManager.Season.Autumn => autumnHumidityMod,
                WorldTimeManager.Season.Winter => winterHumidityMod,
                _ => 0f
            };
        }

        private void ApplyWeatherEffects()
        {
            if (WeatherSystem.Instance == null) return;

            var weather = WeatherSystem.Instance.CurrentWeather;
            
            switch (weather)
            {
                case WeatherSystem.WeatherState.Stormy:
                    targetTemperature += stormyTempMod;
                    targetHumidity += stormyHumidityMod;
                    break;
                case WeatherSystem.WeatherState.Windy:
                    targetTemperature += windyTempMod;
                    break;
                case WeatherSystem.WeatherState.Misty:
                    targetHumidity += mistyHumidityMod;
                    break;
            }
        }

        private void ApplyEventEffects()
        {
            if (WorldEventManager.Instance == null) return;

            if (WorldEventManager.Instance.IsEventActive(WorldEventManager.WorldEventType.SolarFlare))
            {
                targetTemperature += 10f;
            }

            if (WorldEventManager.Instance.IsEventActive(WorldEventManager.WorldEventType.AuroraWave))
            {
                targetTemperature -= 5f;
            }
        }

        private void SmoothConditions()
        {
            float prevTemp = currentTemperature;
            float prevHumidity = currentHumidity;

            currentTemperature = Mathf.Lerp(currentTemperature, targetTemperature, smoothingSpeed * Time.deltaTime);
            currentHumidity = Mathf.Lerp(currentHumidity, targetHumidity, smoothingSpeed * Time.deltaTime);

            if (Mathf.Abs(currentTemperature - prevTemp) > 0.01f)
            {
                OnTemperatureChanged?.Invoke(currentTemperature);
            }

            if (Mathf.Abs(currentHumidity - prevHumidity) > 0.01f)
            {
                OnHumidityChanged?.Invoke(currentHumidity);
            }
        }
        #endregion

        #region State Management
        private void UpdateState()
        {
            currentState = new TerrariumState
            {
                temperature = currentTemperature,
                humidity = currentHumidity,
                timeOfDay = WorldTimeManager.Instance?.TimeOfDayNormalized ?? 0f,
                timePhase = WorldTimeManager.Instance?.CurrentTimeOfDayPhase ?? WorldTimeManager.TimeOfDay.Day,
                season = WorldTimeManager.Instance?.CurrentSeason ?? WorldTimeManager.Season.Spring,
                weather = WeatherSystem.Instance?.CurrentWeather ?? WeatherSystem.WeatherState.Clear,
                isDay = WorldTimeManager.Instance?.IsDay ?? true,
                windStrength = WeatherSystem.Instance?.CurrentWindStrength ?? 0f,
                visibility = WeatherSystem.Instance?.Visibility ?? 1f,
                ecosystemStats = EcosystemManager.Instance?.GetStats() ?? default,
                resourceCount = ResourceManager.Instance?.ActiveResourceCount ?? 0,
                growthMultiplier = ResourceManager.Instance?.CurrentGrowthMultiplier ?? 1f
            };

            if (!currentState.Equals(previousState))
            {
                OnStateChanged?.Invoke(currentState);
                previousState = currentState;
            }
        }
        #endregion

        #region Event Handlers
        private void OnTimePhaseChanged(WorldTimeManager.TimeOfDay phase)
        {
            UpdateTargetConditions();
        }

        private void OnSeasonChanged(WorldTimeManager.Season season)
        {
            UpdateTargetConditions();
        }

        private void OnWeatherChanged(WeatherSystem.WeatherState weather)
        {
            UpdateTargetConditions();
        }

        private void OnWorldEventStarted(WorldEvent worldEvent)
        {
            UpdateTargetConditions();
        }

        private void OnWorldEventEnded(WorldEvent worldEvent)
        {
            UpdateTargetConditions();
        }
        #endregion

        #region Public Methods
        public float GetGrowthMultiplier()
        {
            float multiplier = 1f;
            
            // Temperature sweet spot (15-30Â°C)
            if (currentTemperature >= 15f && currentTemperature <= 30f)
            {
                multiplier *= 1.2f;
            }
            else if (currentTemperature < 5f || currentTemperature > 40f)
            {
                multiplier *= 0.5f;
            }

            // Humidity effect
            if (currentHumidity >= 40f && currentHumidity <= 80f)
            {
                multiplier *= 1.1f;
            }

            return multiplier;
        }

        public bool IsHarshConditions()
        {
            return currentTemperature < 0f || currentTemperature > 45f || 
                   currentHumidity < 10f || currentHumidity > 95f;
        }

        public TerrariumState GetCurrentState()
        {
            return currentState;
        }
        #endregion
    }

    [System.Serializable]
    public struct TerrariumState
    {
        public float temperature;
        public float humidity;
        public float timeOfDay;
        public WorldTimeManager.TimeOfDay timePhase;
        public WorldTimeManager.Season season;
        public WeatherSystem.WeatherState weather;
        public bool isDay;
        public float windStrength;
        public float visibility;
        public EcosystemStats ecosystemStats;
        public int resourceCount;
        public float growthMultiplier;
    }
}