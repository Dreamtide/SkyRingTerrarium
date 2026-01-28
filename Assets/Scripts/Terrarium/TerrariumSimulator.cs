using UnityEngine;
using System;

namespace SkyRingTerrarium.Terrarium
{
    /// <summary>
    /// Manages the terrarium ecosystem simulation including temperature,
    /// humidity, nutrients, and day/night cycles.
    /// </summary>
    public class TerrariumSimulator : MonoBehaviour
    {
        public static TerrariumSimulator Instance { get; private set; }

        public static event Action<float> OnTemperatureChanged;
        public static event Action<float> OnHumidityChanged;
        public static event Action<bool> OnDayNightCycleChanged;

        [Header("Environment Bounds")]
        [SerializeField] private float minTemperature = -40f;
        [SerializeField] private float maxTemperature = 60f;
        [SerializeField] private float minHumidity = 0f;
        [SerializeField] private float maxHumidity = 100f;

        [Header("Initial Conditions")]
        [SerializeField] private float initialTemperature = 22f;
        [SerializeField] private float initialHumidity = 65f;
        [SerializeField] private float initialNutrientLevel = 100f;

        [Header("Day/Night Cycle")]
        [SerializeField] private float dayLengthMinutes = 12f;
        [SerializeField] private float nightLengthMinutes = 12f;
        [SerializeField] private float dayTemperatureBonus = 10f;
        [SerializeField] private float nightTemperaturePenalty = 8f;

        [Header("Simulation Settings")]
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private float temperatureChangeRate = 0.5f;
        [SerializeField] private float humidityChangeRate = 0.3f;
        [SerializeField] private float nutrientDepletionRate = 0.01f;

        // Current state
        private float currentTemperature;
        private float currentHumidity;
        private float currentNutrients;
        private float dayNightTimer;
        private bool isDaytime = true;
        private float timeSinceLastUpdate;

        // Properties
        public float Temperature => currentTemperature;
        public float Humidity => currentHumidity;
        public float Nutrients => currentNutrients;
        public bool IsDaytime => isDaytime;
        public float DayProgress => isDaytime 
            ? dayNightTimer / (dayLengthMinutes * 60f) 
            : dayNightTimer / (nightLengthMinutes * 60f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeEnvironment();
        }

        private void InitializeEnvironment()
        {
            currentTemperature = initialTemperature;
            currentHumidity = initialHumidity;
            currentNutrients = initialNutrientLevel;
            dayNightTimer = 0f;
            isDaytime = true;
        }

        private void Update()
        {
            timeSinceLastUpdate += Time.deltaTime;

            if (timeSinceLastUpdate >= updateInterval)
            {
                UpdateSimulation(timeSinceLastUpdate);
                timeSinceLastUpdate = 0f;
            }
        }

        private void UpdateSimulation(float deltaTime)
        {
            UpdateDayNightCycle(deltaTime);
            UpdateTemperature(deltaTime);
            UpdateHumidity(deltaTime);
            UpdateNutrients(deltaTime);
        }

        private void UpdateDayNightCycle(float deltaTime)
        {
            dayNightTimer += deltaTime;

            float cycleLength = isDaytime 
                ? dayLengthMinutes * 60f 
                : nightLengthMinutes * 60f;

            if (dayNightTimer >= cycleLength)
            {
                dayNightTimer = 0f;
                isDaytime = !isDaytime;
                OnDayNightCycleChanged?.Invoke(isDaytime);
            }
        }

        private void UpdateTemperature(float deltaTime)
        {
            float targetTemp = CalculateTargetTemperature();
            float previousTemp = currentTemperature;

            currentTemperature = Mathf.MoveTowards(
                currentTemperature,
                targetTemp,
                temperatureChangeRate * deltaTime
            );

            currentTemperature = Mathf.Clamp(currentTemperature, minTemperature, maxTemperature);

            if (Mathf.Abs(currentTemperature - previousTemp) > 0.01f)
            {
                OnTemperatureChanged?.Invoke(currentTemperature);
            }
        }

        private float CalculateTargetTemperature()
        {
            float baseTemp = initialTemperature;
            float cycleProgress = DayProgress;

            if (isDaytime)
            {
                float dayModifier = Mathf.Sin(cycleProgress * Mathf.PI) * dayTemperatureBonus;
                return baseTemp + dayModifier;
            }
            else
            {
                float nightModifier = -Mathf.Sin(cycleProgress * Mathf.PI) * nightTemperaturePenalty;
                return baseTemp + nightModifier;
            }
        }

        private void UpdateHumidity(float deltaTime)
        {
            float previousHumidity = currentHumidity;

            // Temperature affects humidity (warmer = lower humidity through evaporation)
            float tempFactor = (currentTemperature - initialTemperature) / 20f;
            float humidityChange = -tempFactor * humidityChangeRate * deltaTime;

            // Night increases humidity (condensation)
            if (!isDaytime)
            {
                humidityChange += humidityChangeRate * 0.5f * deltaTime;
            }

            currentHumidity += humidityChange;
            currentHumidity = Mathf.Clamp(currentHumidity, minHumidity, maxHumidity);

            if (Mathf.Abs(currentHumidity - previousHumidity) > 0.01f)
            {
                OnHumidityChanged?.Invoke(currentHumidity);
            }
        }

        private void UpdateNutrients(float deltaTime)
        {
            currentNutrients -= nutrientDepletionRate * deltaTime;
            currentNutrients = Mathf.Max(0f, currentNutrients);
        }

        // Public API for external systems
        public void AddNutrients(float amount)
        {
            currentNutrients = Mathf.Min(100f, currentNutrients + amount);
        }

        public void AddWater(float amount)
        {
            currentHumidity = Mathf.Clamp(currentHumidity + amount, minHumidity, maxHumidity);
            OnHumidityChanged?.Invoke(currentHumidity);
        }

        public void SetTemperatureModifier(float modifier)
        {
            float newTemp = currentTemperature + modifier;
            currentTemperature = Mathf.Clamp(newTemp, minTemperature, maxTemperature);
            OnTemperatureChanged?.Invoke(currentTemperature);
        }

        public EnvironmentSnapshot GetSnapshot()
        {
            return new EnvironmentSnapshot
            {
                Temperature = currentTemperature,
                Humidity = currentHumidity,
                Nutrients = currentNutrients,
                IsDaytime = isDaytime,
                DayProgress = DayProgress
            };
        }

        public void LoadSnapshot(EnvironmentSnapshot snapshot)
        {
            currentTemperature = snapshot.Temperature;
            currentHumidity = snapshot.Humidity;
            currentNutrients = snapshot.Nutrients;
            isDaytime = snapshot.IsDaytime;
        }
    }

    [Serializable]
    public struct EnvironmentSnapshot
    {
        public float Temperature;
        public float Humidity;
        public float Nutrients;
        public bool IsDaytime;
        public float DayProgress;
    }
}
