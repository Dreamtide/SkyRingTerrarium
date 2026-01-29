using UnityEngine;
using System;
using System.Collections.Generic;

namespace SkyRingTerrarium.World
{
    /// <summary>
    /// Manages special world events: meteor showers, aurora waves, migrations, blooms, and rare phenomena.
    /// </summary>
    public class WorldEventManager : MonoBehaviour
    {
        public static WorldEventManager Instance { get; private set; }

        #region Events
        public static event Action<WorldEvent> OnEventStarted;
        public static event Action<WorldEvent> OnEventEnded;
        public static event Action<Vector2> OnMeteorImpact;
        public static event Action OnAuroraWave;
        #endregion

        #region Enums
        public enum WorldEventType
        {
            MeteorShower,
            AuroraWave,
            Migration,
            Bloom,
            SolarFlare,
            CosmicDrift,
            HarmonicResonance
        }
        #endregion

        #region Serialized Fields
        [Header("Event Timing")]
        [SerializeField] private float eventCheckInterval = 120f;
        [SerializeField] private float minTimeBetweenEvents = 180f;
        [SerializeField] private int maxConcurrentEvents = 2;

        [Header("Meteor Shower")]
        [SerializeField] private float meteorShowerChance = 0.05f;
        [SerializeField] private float meteorShowerDuration = 60f;
        [SerializeField] private float meteorSpawnInterval = 2f;
        [SerializeField] private GameObject meteorPrefab;
        [SerializeField] private GameObject specialResourcePrefab;

        [Header("Aurora Wave")]
        [SerializeField] private float auroraWaveChance = 0.08f;
        [SerializeField] private float auroraWaveDuration = 90f;
        [SerializeField] private float auroraCreatureBoost = 1.5f;

        [Header("Bloom Event")]
        [SerializeField] private float bloomChance = 0.1f;
        [SerializeField] private float bloomDuration = 120f;

        [Header("Solar Flare")]
        [SerializeField] private float solarFlareChance = 0.03f;
        [SerializeField] private float solarFlareDuration = 30f;
        [SerializeField] private float solarFlareIntensityBoost = 2f;

        [Header("Cosmic Drift")]
        [SerializeField] private float cosmicDriftChance = 0.02f;
        [SerializeField] private float cosmicDriftDuration = 45f;

        [Header("Harmonic Resonance")]
        [SerializeField] private float harmonicResonanceChance = 0.01f;
        [SerializeField] private float harmonicResonanceDuration = 60f;

        [Header("Condition Requirements")]
        [SerializeField] private bool meteorRequiresNight = true;
        [SerializeField] private bool auroraRequiresClear = true;
        #endregion

        #region Public Properties
        public List<WorldEvent> ActiveEvents => activeEvents;
        public bool HasActiveEvent => activeEvents.Count > 0;
        public WorldEvent CurrentMainEvent => activeEvents.Count > 0 ? activeEvents[0] : null;
        #endregion

        #region Private Fields
        private List<WorldEvent> activeEvents = new List<WorldEvent>();
        private float eventCheckTimer;
        private float lastEventTime;
        private float meteorSpawnTimer;
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
        }

        private void Start()
        {
            WorldTimeManager.OnTimeOfDayPhaseChanged += OnTimePhaseChanged;
            WeatherSystem.OnWeatherChanged += OnWeatherChanged;
            WorldTimeManager.OnSeasonChanged += OnSeasonChanged;
        }

        private void OnDestroy()
        {
            WorldTimeManager.OnTimeOfDayPhaseChanged -= OnTimePhaseChanged;
            WeatherSystem.OnWeatherChanged -= OnWeatherChanged;
            WorldTimeManager.OnSeasonChanged -= OnSeasonChanged;
        }

        private void Update()
        {
            UpdateEventCheck();
            UpdateActiveEvents();
        }
        #endregion

        #region Event Checking
        private void UpdateEventCheck()
        {
            eventCheckTimer += Time.deltaTime;
            if (eventCheckTimer < eventCheckInterval) return;
            eventCheckTimer = 0f;

            if (Time.time - lastEventTime < minTimeBetweenEvents) return;
            if (activeEvents.Count >= maxConcurrentEvents) return;

            TryTriggerRandomEvent();
        }

        private void TryTriggerRandomEvent()
        {
            // Check each event type
            if (CanTriggerMeteorShower() && UnityEngine.Random.value < meteorShowerChance)
            {
                TriggerEvent(WorldEventType.MeteorShower);
                return;
            }

            if (CanTriggerAurora() && UnityEngine.Random.value < auroraWaveChance)
            {
                TriggerEvent(WorldEventType.AuroraWave);
                return;
            }

            if (UnityEngine.Random.value < bloomChance)
            {
                TriggerEvent(WorldEventType.Bloom);
                return;
            }

            if (UnityEngine.Random.value < solarFlareChance)
            {
                TriggerEvent(WorldEventType.SolarFlare);
                return;
            }

            if (UnityEngine.Random.value < cosmicDriftChance)
            {
                TriggerEvent(WorldEventType.CosmicDrift);
                return;
            }

            if (UnityEngine.Random.value < harmonicResonanceChance)
            {
                TriggerEvent(WorldEventType.HarmonicResonance);
                return;
            }
        }

        private bool CanTriggerMeteorShower()
        {
            if (!meteorRequiresNight) return true;
            if (WorldTimeManager.Instance == null) return true;
            return WorldTimeManager.Instance.IsNight;
        }

        private bool CanTriggerAurora()
        {
            if (!auroraRequiresClear) return true;
            if (WeatherSystem.Instance == null) return true;
            return WeatherSystem.Instance.CurrentWeather == WeatherSystem.WeatherState.Clear ||
                   WeatherSystem.Instance.CurrentWeather == WeatherSystem.WeatherState.Calm;
        }
        #endregion

        #region Event Triggering
        public void TriggerEvent(WorldEventType eventType)
        {
            float duration = GetEventDuration(eventType);
            
            var worldEvent = new WorldEvent
            {
                type = eventType,
                startTime = Time.time,
                duration = duration,
                remainingTime = duration
            };

            activeEvents.Add(worldEvent);
            lastEventTime = Time.time;

            OnEventStart(worldEvent);
            OnEventStarted?.Invoke(worldEvent);
        }

        private float GetEventDuration(WorldEventType type)
        {
            return type switch
            {
                WorldEventType.MeteorShower => meteorShowerDuration,
                WorldEventType.AuroraWave => auroraWaveDuration,
                WorldEventType.Bloom => bloomDuration,
                WorldEventType.SolarFlare => solarFlareDuration,
                WorldEventType.CosmicDrift => cosmicDriftDuration,
                WorldEventType.HarmonicResonance => harmonicResonanceDuration,
                _ => 60f
            };
        }

        private void OnEventStart(WorldEvent worldEvent)
        {
            switch (worldEvent.type)
            {
                case WorldEventType.MeteorShower:
                    meteorSpawnTimer = 0f;
                    break;
                    
                case WorldEventType.AuroraWave:
                    StarFieldSystem.Instance?.TriggerAurora();
                    OnAuroraWave?.Invoke();
                    break;
                    
                case WorldEventType.Bloom:
                    Ecosystem.ResourceManager.Instance?.TriggerBloom();
                    break;
                    
                case WorldEventType.Migration:
                    Ecosystem.EcosystemManager.Instance?.StartMigration();
                    break;
            }
        }
        #endregion

        #region Event Updates
        private void UpdateActiveEvents()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                var worldEvent = activeEvents[i];
                worldEvent.remainingTime -= Time.deltaTime;

                UpdateEvent(worldEvent);

                if (worldEvent.remainingTime <= 0)
                {
                    EndEvent(worldEvent);
                    activeEvents.RemoveAt(i);
                }
                else
                {
                    activeEvents[i] = worldEvent;
                }
            }
        }

        private void UpdateEvent(WorldEvent worldEvent)
        {
            switch (worldEvent.type)
            {
                case WorldEventType.MeteorShower:
                    UpdateMeteorShower();
                    break;
            }
        }

        private void UpdateMeteorShower()
        {
            meteorSpawnTimer += Time.deltaTime;
            if (meteorSpawnTimer >= meteorSpawnInterval)
            {
                meteorSpawnTimer = 0f;
                SpawnMeteor();
            }
        }

        private void SpawnMeteor()
        {
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float spawnRadius = 30f;
            Vector2 spawnPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;

            if (meteorPrefab != null)
            {
                var meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
                var meteorScript = meteor.GetComponent<Meteor>();
                if (meteorScript != null)
                {
                    meteorScript.Initialize(-spawnPos.normalized, OnMeteorLanded);
                }
            }
        }

        private void OnMeteorLanded(Vector2 impactPosition)
        {
            OnMeteorImpact?.Invoke(impactPosition);
            
            // Chance to spawn special resource
            if (specialResourcePrefab != null && UnityEngine.Random.value < 0.3f)
            {
                Instantiate(specialResourcePrefab, impactPosition, Quaternion.identity);
            }
        }

        private void EndEvent(WorldEvent worldEvent)
        {
            switch (worldEvent.type)
            {
                case WorldEventType.AuroraWave:
                    StarFieldSystem.Instance?.StopAurora();
                    break;
            }

            OnEventEnded?.Invoke(worldEvent);
        }
        #endregion

        #region Condition Responses
        private void OnTimePhaseChanged(WorldTimeManager.TimeOfDay phase)
        {
            // Night-specific event chances
            if (phase == WorldTimeManager.TimeOfDay.Night)
            {
                if (UnityEngine.Random.value < meteorShowerChance * 0.5f)
                {
                    TriggerEvent(WorldEventType.MeteorShower);
                }
            }
        }

        private void OnWeatherChanged(WeatherSystem.WeatherState weather)
        {
            // Clear weather after storm can trigger aurora
            if (weather == WeatherSystem.WeatherState.Clear && WorldTimeManager.Instance?.IsNight == true)
            {
                if (UnityEngine.Random.value < auroraWaveChance)
                {
                    TriggerEvent(WorldEventType.AuroraWave);
                }
            }
        }

        private void OnSeasonChanged(WorldTimeManager.Season season)
        {
            // Seasonal event triggers
            if (season == WorldTimeManager.Season.Spring)
            {
                if (UnityEngine.Random.value < bloomChance * 2f)
                {
                    TriggerEvent(WorldEventType.Bloom);
                }
            }
        }
        #endregion

        #region Public Query Methods
        public bool IsEventActive(WorldEventType type)
        {
            return activeEvents.Exists(e => e.type == type);
        }

        public float GetEventProgress(WorldEventType type)
        {
            var worldEvent = activeEvents.Find(e => e.type == type);
            if (worldEvent.duration > 0)
            {
                return 1f - (worldEvent.remainingTime / worldEvent.duration);
            }
            return 0f;
        }

        public float GetAuroraBoostMultiplier()
        {
            return IsEventActive(WorldEventType.AuroraWave) ? auroraCreatureBoost : 1f;
        }

        public float GetSolarFlareIntensity()
        {
            return IsEventActive(WorldEventType.SolarFlare) ? solarFlareIntensityBoost : 1f;
        }
        #endregion
    }

    [System.Serializable]
    public struct WorldEvent
    {
        public WorldEventManager.WorldEventType type;
        public float startTime;
        public float duration;
        public float remainingTime;
    }
}