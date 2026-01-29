using UnityEngine;
using System;
using System.Collections.Generic;

namespace SkyRingTerrarium.World
{
    /// <summary>
    /// Manages weather states with gradual transitions, affecting visuals and gameplay.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        #region Events
        public static event Action<WeatherState> OnWeatherChanged;
        public static event Action<WeatherState, WeatherState, float> OnWeatherTransition;
        public static event Action OnLightningStrike;
        #endregion

        #region Enums
        public enum WeatherState { Clear, Windy, Stormy, Calm, Misty }
        #endregion

        #region Serialized Fields
        [Header("Weather Configuration")]
        [SerializeField] private WeatherState initialWeather = WeatherState.Clear;
        [SerializeField] private float minWeatherDuration = 60f;
        [SerializeField] private float maxWeatherDuration = 300f;
        [SerializeField] private float transitionDuration = 30f;

        [Header("Wind Settings")]
        [SerializeField] private float clearWindStrength = 0.1f;
        [SerializeField] private float windyWindStrength = 0.8f;
        [SerializeField] private float stormyWindStrength = 1.5f;
        [SerializeField] private float calmWindStrength = 0f;
        [SerializeField] private float mistyWindStrength = 0.2f;

        [Header("Storm Effects")]
        [SerializeField] private float lightningChancePerSecond = 0.05f;
        [SerializeField] private float lightningFlashDuration = 0.15f;
        [SerializeField] private float lightningIntensity = 3f;

        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem mistParticles;
        [SerializeField] private ParticleSystem windParticles;

        [Header("Season Weather Weights")]
        [SerializeField] private WeatherWeights springWeights;
        [SerializeField] private WeatherWeights summerWeights;
        [SerializeField] private WeatherWeights autumnWeights;
        [SerializeField] private WeatherWeights winterWeights;
        #endregion

        #region Public Properties
        public WeatherState CurrentWeather => currentWeather;
        public WeatherState TargetWeather => targetWeather;
        public float TransitionProgress => transitionProgress;
        public bool IsTransitioning => isTransitioning;
        public float CurrentWindStrength => currentWindStrength;
        public Vector2 WindDirection => windDirection;
        public float Precipitation => currentPrecipitation;
        public float Visibility => currentVisibility;
        #endregion

        #region Private Fields
        private WeatherState currentWeather;
        private WeatherState targetWeather;
        private float transitionProgress;
        private bool isTransitioning;
        private float weatherTimer;
        private float currentWeatherDuration;
        private float currentWindStrength;
        private Vector2 windDirection;
        private float currentPrecipitation;
        private float currentVisibility;
        private float lightningTimer;
        private float flashTimer;
        private bool isFlashing;
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
            
            currentWeather = initialWeather;
            targetWeather = initialWeather;
            windDirection = UnityEngine.Random.insideUnitCircle.normalized;
            ApplyWeatherEffects(currentWeather, 1f);
            ScheduleNextWeatherChange();
        }

        private void Start()
        {
            WorldTimeManager.OnSeasonChanged += OnSeasonChanged;
        }

        private void OnDestroy()
        {
            WorldTimeManager.OnSeasonChanged -= OnSeasonChanged;
        }

        private void Update()
        {
            UpdateWeatherTimer();
            UpdateTransition();
            UpdateStormEffects();
            UpdateWindDirection();
        }
        #endregion

        #region Weather Updates
        private void UpdateWeatherTimer()
        {
            if (isTransitioning) return;

            weatherTimer += Time.deltaTime;
            if (weatherTimer >= currentWeatherDuration)
            {
                SelectNextWeather();
            }
        }

        private void UpdateTransition()
        {
            if (!isTransitioning) return;

            transitionProgress += Time.deltaTime / transitionDuration;
            
            if (transitionProgress >= 1f)
            {
                transitionProgress = 1f;
                isTransitioning = false;
                currentWeather = targetWeather;
                OnWeatherChanged?.Invoke(currentWeather);
                ScheduleNextWeatherChange();
            }

            ApplyWeatherEffects(currentWeather, 1f - transitionProgress);
            ApplyWeatherEffects(targetWeather, transitionProgress);
            OnWeatherTransition?.Invoke(currentWeather, targetWeather, transitionProgress);
        }

        private void UpdateStormEffects()
        {
            if (currentWeather != WeatherState.Stormy && targetWeather != WeatherState.Stormy)
            {
                isFlashing = false;
                return;
            }

            if (isFlashing)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0)
                {
                    isFlashing = false;
                }
            }
            else
            {
                lightningTimer += Time.deltaTime;
                if (UnityEngine.Random.value < lightningChancePerSecond * Time.deltaTime)
                {
                    TriggerLightning();
                }
            }
        }

        private void TriggerLightning()
        {
            isFlashing = true;
            flashTimer = lightningFlashDuration;
            OnLightningStrike?.Invoke();
        }

        private void UpdateWindDirection()
        {
            float rotationSpeed = currentWindStrength * 0.1f;
            float angle = Mathf.PerlinNoise(Time.time * 0.1f, 0) * 360f;
            windDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }
        #endregion

        #region Weather Selection
        private void SelectNextWeather()
        {
            WeatherWeights weights = GetCurrentSeasonWeights();
            targetWeather = SelectWeatherFromWeights(weights);
            
            if (targetWeather != currentWeather)
            {
                isTransitioning = true;
                transitionProgress = 0f;
            }
            else
            {
                ScheduleNextWeatherChange();
            }
        }

        private WeatherWeights GetCurrentSeasonWeights()
        {
            if (WorldTimeManager.Instance == null)
                return springWeights;

            return WorldTimeManager.Instance.CurrentSeason switch
            {
                WorldTimeManager.Season.Spring => springWeights,
                WorldTimeManager.Season.Summer => summerWeights,
                WorldTimeManager.Season.Autumn => autumnWeights,
                WorldTimeManager.Season.Winter => winterWeights,
                _ => springWeights
            };
        }

        private WeatherState SelectWeatherFromWeights(WeatherWeights weights)
        {
            float total = weights.clear + weights.windy + weights.stormy + weights.calm + weights.misty;
            float roll = UnityEngine.Random.value * total;

            if (roll < weights.clear) return WeatherState.Clear;
            roll -= weights.clear;
            if (roll < weights.windy) return WeatherState.Windy;
            roll -= weights.windy;
            if (roll < weights.stormy) return WeatherState.Stormy;
            roll -= weights.stormy;
            if (roll < weights.calm) return WeatherState.Calm;
            return WeatherState.Misty;
        }

        private void ScheduleNextWeatherChange()
        {
            weatherTimer = 0f;
            currentWeatherDuration = UnityEngine.Random.Range(minWeatherDuration, maxWeatherDuration);
        }

        private void OnSeasonChanged(WorldTimeManager.Season season)
        {
            if (UnityEngine.Random.value < 0.5f)
            {
                SelectNextWeather();
            }
        }
        #endregion

        #region Weather Effects
        private void ApplyWeatherEffects(WeatherState weather, float intensity)
        {
            float windTarget = weather switch
            {
                WeatherState.Clear => clearWindStrength,
                WeatherState.Windy => windyWindStrength,
                WeatherState.Stormy => stormyWindStrength,
                WeatherState.Calm => calmWindStrength,
                WeatherState.Misty => mistyWindStrength,
                _ => clearWindStrength
            };

            currentWindStrength = Mathf.Lerp(currentWindStrength, windTarget, intensity * Time.deltaTime * 2f);

            currentPrecipitation = weather switch
            {
                WeatherState.Stormy => intensity,
                WeatherState.Misty => intensity * 0.3f,
                _ => 0f
            };

            currentVisibility = weather switch
            {
                WeatherState.Clear => 1f,
                WeatherState.Windy => 0.9f,
                WeatherState.Stormy => 0.4f,
                WeatherState.Calm => 1f,
                WeatherState.Misty => 0.3f,
                _ => 1f
            };

            UpdateParticles(weather, intensity);
        }

        private void UpdateParticles(WeatherState weather, float intensity)
        {
            if (rainParticles != null)
            {
                var emission = rainParticles.emission;
                emission.rateOverTime = weather == WeatherState.Stormy ? 100f * intensity : 0f;
            }

            if (mistParticles != null)
            {
                var emission = mistParticles.emission;
                emission.rateOverTime = weather == WeatherState.Misty ? 50f * intensity : 0f;
            }

            if (windParticles != null)
            {
                var emission = windParticles.emission;
                emission.rateOverTime = weather == WeatherState.Windy ? 30f * intensity : 
                                        weather == WeatherState.Stormy ? 50f * intensity : 5f;
            }
        }
        #endregion

        #region Public Methods
        public void SetWeather(WeatherState weather, bool instant = false)
        {
            if (instant)
            {
                currentWeather = weather;
                targetWeather = weather;
                isTransitioning = false;
                ApplyWeatherEffects(weather, 1f);
                OnWeatherChanged?.Invoke(weather);
            }
            else
            {
                targetWeather = weather;
                isTransitioning = true;
                transitionProgress = 0f;
            }
        }

        public float GetWindInfluence(Vector2 position)
        {
            return currentWindStrength;
        }

        public Vector2 GetWindForce(Vector2 position)
        {
            return windDirection * currentWindStrength;
        }

        public bool IsLightningFlashing => isFlashing;
        public float LightningIntensity => isFlashing ? lightningIntensity : 0f;
        #endregion

        #region Save/Load
        public WeatherState LoadWeatherState(WeatherState savedWeather, float elapsedTime)
        {
            int weatherChanges = Mathf.FloorToInt(elapsedTime / ((minWeatherDuration + maxWeatherDuration) / 2f));
            
            currentWeather = savedWeather;
            for (int i = 0; i < weatherChanges; i++)
            {
                currentWeather = SelectWeatherFromWeights(GetCurrentSeasonWeights());
            }
            
            targetWeather = currentWeather;
            ApplyWeatherEffects(currentWeather, 1f);
            return currentWeather;
        }
        #endregion
    }

    [System.Serializable]
    public struct WeatherWeights
    {
        public float clear;
        public float windy;
        public float stormy;
        public float calm;
        public float misty;

        public WeatherWeights(float clear, float windy, float stormy, float calm, float misty)
        {
            this.clear = clear;
            this.windy = windy;
            this.stormy = stormy;
            this.calm = calm;
            this.misty = misty;
        }
    }
}