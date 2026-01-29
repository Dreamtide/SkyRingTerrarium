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

        [Header("Event Probabilities")]
        [SerializeField] private float meteorShowerChance = 0.1f;
        [SerializeField] private float auroraWaveChance = 0.15f;
        [SerializeField] private float migrationChance = 0.08f;
        [SerializeField] private float bloomChance = 0.12f;
        [SerializeField] private float solarFlareChance = 0.05f;
        [SerializeField] private float cosmicDriftChance = 0.03f;
        [SerializeField] private float harmonicResonanceChance = 0.02f;

        [Header("Event Durations")]
        [SerializeField] private float meteorShowerDuration = 60f;
        [SerializeField] private float auroraWaveDuration = 120f;
        [SerializeField] private float migrationDuration = 300f;
        [SerializeField] private float bloomDuration = 240f;
        [SerializeField] private float solarFlareDuration = 30f;
        [SerializeField] private float cosmicDriftDuration = 180f;
        [SerializeField] private float harmonicResonanceDuration = 90f;
        #endregion

        #region Private Fields
        private List<WorldEvent> activeEvents = new List<WorldEvent>();
        private float nextEventCheckTime;
        private float lastEventEndTime;
        private System.Random eventRandom;
        #endregion

        #region Public Properties
        public IReadOnlyList<WorldEvent> ActiveEvents => activeEvents;
        public int ActiveEventCount => activeEvents.Count;
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            eventRandom = new System.Random();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            nextEventCheckTime = Time.time + eventCheckInterval;
        }

        private void Update()
        {
            UpdateActiveEvents();
            
            if (Time.time >= nextEventCheckTime)
            {
                CheckForNewEvents();
                nextEventCheckTime = Time.time + eventCheckInterval;
            }
        }

        private void UpdateActiveEvents()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                var worldEvent = activeEvents[i];
                worldEvent.Update(Time.deltaTime);
                
                if (worldEvent.IsExpired)
                {
                    EndEvent(worldEvent);
                    activeEvents.RemoveAt(i);
                }
            }
        }

        private void CheckForNewEvents()
        {
            if (activeEvents.Count >= maxConcurrentEvents) return;
            if (Time.time - lastEventEndTime < minTimeBetweenEvents) return;

            float roll = (float)eventRandom.NextDouble();
            float cumulativeChance = 0f;

            WorldEventType[] eventTypes = (WorldEventType[])Enum.GetValues(typeof(WorldEventType));
            float[] chances = { meteorShowerChance, auroraWaveChance, migrationChance, 
                               bloomChance, solarFlareChance, cosmicDriftChance, harmonicResonanceChance };

            for (int i = 0; i < eventTypes.Length && i < chances.Length; i++)
            {
                cumulativeChance += chances[i];
                if (roll <= cumulativeChance && !IsEventActive(eventTypes[i]))
                {
                    StartEvent(eventTypes[i]);
                    break;
                }
            }
        }

        /// <summary>
        /// Checks if an event of the specified type is currently active.
        /// </summary>
        public bool IsEventActive(WorldEventType eventType)
        {
            for (int i = 0; i < activeEvents.Count; i++)
            {
                if (activeEvents[i].Type == eventType)
                {
                    return true;
                }
            }
            return false;
        }

        public void StartEvent(WorldEventType eventType)
        {
            if (IsEventActive(eventType)) return;

            float duration = GetEventDuration(eventType);
            var worldEvent = new WorldEvent(eventType, duration);
            
            activeEvents.Add(worldEvent);
            OnEventStarted?.Invoke(worldEvent);
            
            Debug.Log($"[WorldEvent] {eventType} started (duration: {duration}s)");
            
            if (eventType == WorldEventType.AuroraWave)
            {
                OnAuroraWave?.Invoke();
            }
        }

        private void EndEvent(WorldEvent worldEvent)
        {
            OnEventEnded?.Invoke(worldEvent);
            lastEventEndTime = Time.time;
            Debug.Log($"[WorldEvent] {worldEvent.Type} ended");
        }

        private float GetEventDuration(WorldEventType eventType)
        {
            return eventType switch
            {
                WorldEventType.MeteorShower => meteorShowerDuration,
                WorldEventType.AuroraWave => auroraWaveDuration,
                WorldEventType.Migration => migrationDuration,
                WorldEventType.Bloom => bloomDuration,
                WorldEventType.SolarFlare => solarFlareDuration,
                WorldEventType.CosmicDrift => cosmicDriftDuration,
                WorldEventType.HarmonicResonance => harmonicResonanceDuration,
                _ => 60f
            };
        }

        public void TriggerMeteorImpact(Vector2 position)
        {
            OnMeteorImpact?.Invoke(position);
        }

        public void ForceStartEvent(WorldEventType eventType)
        {
            if (!IsEventActive(eventType))
            {
                StartEvent(eventType);
            }
        }

        public void ForceEndAllEvents()
        {
            foreach (var worldEvent in activeEvents)
            {
                OnEventEnded?.Invoke(worldEvent);
            }
            activeEvents.Clear();
            lastEventEndTime = Time.time;
        }

        public WorldEvent GetActiveEvent(WorldEventType eventType)
        {
            for (int i = 0; i < activeEvents.Count; i++)
            {
                if (activeEvents[i].Type == eventType)
                {
                    return activeEvents[i];
                }
            }
            return null;
        }
    }

    [Serializable]
    public class WorldEvent
    {
        public WorldEventManager.WorldEventType Type { get; private set; }
        public float Duration { get; private set; }
        public float ElapsedTime { get; private set; }
        public float Intensity { get; private set; }
        public bool IsExpired => ElapsedTime >= Duration;
        public float Progress => Mathf.Clamp01(ElapsedTime / Duration);

        public WorldEvent(WorldEventManager.WorldEventType type, float duration)
        {
            Type = type;
            Duration = duration;
            ElapsedTime = 0f;
            Intensity = 1f;
        }

        public void Update(float deltaTime)
        {
            ElapsedTime += deltaTime;
            
            float fadeInTime = Duration * 0.1f;
            float fadeOutTime = Duration * 0.2f;
            
            if (ElapsedTime < fadeInTime)
            {
                Intensity = ElapsedTime / fadeInTime;
            }
            else if (ElapsedTime > Duration - fadeOutTime)
            {
                Intensity = (Duration - ElapsedTime) / fadeOutTime;
            }
            else
            {
                Intensity = 1f;
            }
            
            Intensity = Mathf.Clamp01(Intensity);
        }
    }
}
