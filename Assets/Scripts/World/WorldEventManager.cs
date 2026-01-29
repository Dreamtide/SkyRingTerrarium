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
        [SerializeField] private float bloomDuration = 180f;
        [SerializeField] private float solarFlareDuration = 30f;
        [SerializeField] private float cosmicDriftDuration = 240f;
        [SerializeField] private float harmonicResonanceDuration = 90f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        #endregion

        private List<WorldEvent> activeEvents = new List<WorldEvent>();
        private float lastEventCheckTime;
        private float lastEventEndTime;

        public IReadOnlyList<WorldEvent> ActiveEvents => activeEvents;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            UpdateActiveEvents();
            CheckForNewEvents();
        }

        private void UpdateActiveEvents()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                WorldEvent evt = activeEvents[i];
                evt.ElapsedTime += Time.deltaTime;
                activeEvents[i] = evt;

                if (evt.ElapsedTime >= evt.Duration)
                {
                    EndEvent(i);
                }
                else
                {
                    UpdateEventEffects(evt);
                }
            }
        }

        private void CheckForNewEvents()
        {
            if (Time.time - lastEventCheckTime < eventCheckInterval) return;
            if (Time.time - lastEventEndTime < minTimeBetweenEvents) return;
            if (activeEvents.Count >= maxConcurrentEvents) return;

            lastEventCheckTime = Time.time;
            TryStartRandomEvent();
        }

        private void TryStartRandomEvent()
        {
            float roll = UnityEngine.Random.value;
            float cumulative = 0f;

            var eventChances = new (WorldEventType type, float chance)[]
            {
                (WorldEventType.MeteorShower, meteorShowerChance),
                (WorldEventType.AuroraWave, auroraWaveChance),
                (WorldEventType.Migration, migrationChance),
                (WorldEventType.Bloom, bloomChance),
                (WorldEventType.SolarFlare, solarFlareChance),
                (WorldEventType.CosmicDrift, cosmicDriftChance),
                (WorldEventType.HarmonicResonance, harmonicResonanceChance)
            };

            foreach (var (type, chance) in eventChances)
            {
                cumulative += chance;
                if (roll < cumulative && !IsEventTypeActive(type))
                {
                    StartEvent(type);
                    break;
                }
            }
        }

        private bool IsEventTypeActive(WorldEventType type)
        {
            foreach (var evt in activeEvents)
            {
                if (evt.Type == type) return true;
            }
            return false;
        }

        public void StartEvent(WorldEventType type)
        {
            float duration = GetEventDuration(type);
            WorldEvent newEvent = new WorldEvent
            {
                Type = type,
                Duration = duration,
                ElapsedTime = 0f,
                Intensity = 1f,
                Position = GetEventPosition(type)
            };

            activeEvents.Add(newEvent);
            OnEventStarted?.Invoke(newEvent);

            if (debugMode)
            {
                Debug.Log($"[WorldEvent] Started: {type} (Duration: {duration:F1}s)");
            }

            InitializeEventEffects(newEvent);
        }

        private void EndEvent(int index)
        {
            if (index < 0 || index >= activeEvents.Count) return;

            WorldEvent evt = activeEvents[index];
            activeEvents.RemoveAt(index);
            lastEventEndTime = Time.time;

            OnEventEnded?.Invoke(evt);

            if (debugMode)
            {
                Debug.Log($"[WorldEvent] Ended: {evt.Type}");
            }

            CleanupEventEffects(evt);
        }

        private float GetEventDuration(WorldEventType type)
        {
            return type switch
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

        private Vector3 GetEventPosition(WorldEventType type)
        {
            // Events can occur at random positions around the ring
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = 100f; // Assume standard ring radius
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private void InitializeEventEffects(WorldEvent evt)
        {
            switch (evt.Type)
            {
                case WorldEventType.MeteorShower:
                    // Start spawning meteors
                    break;
                case WorldEventType.AuroraWave:
                    OnAuroraWave?.Invoke();
                    break;
                case WorldEventType.Migration:
                    // Trigger creature migration
                    break;
                case WorldEventType.Bloom:
                    // Start flora bloom effects
                    break;
                case WorldEventType.SolarFlare:
                    // Increase lighting intensity
                    break;
                case WorldEventType.CosmicDrift:
                    // Modify gravity slightly
                    break;
                case WorldEventType.HarmonicResonance:
                    // Create harmonic effects
                    break;
            }
        }

        private void UpdateEventEffects(WorldEvent evt)
        {
            float progress = evt.ElapsedTime / evt.Duration;
            
            // Intensity ramps up then down
            float intensity = progress < 0.2f ? progress / 0.2f :
                             progress > 0.8f ? (1f - progress) / 0.2f : 1f;

            if (evt.Type == WorldEventType.MeteorShower && UnityEngine.Random.value < 0.02f * intensity)
            {
                Vector2 impactPos = new Vector2(
                    evt.Position.x + UnityEngine.Random.Range(-50f, 50f),
                    evt.Position.z + UnityEngine.Random.Range(-50f, 50f)
                );
                OnMeteorImpact?.Invoke(impactPos);
            }
        }

        private void CleanupEventEffects(WorldEvent evt)
        {
            // Clean up any event-specific effects
        }

        // Public API
        public void ForceStartEvent(WorldEventType type)
        {
            if (!IsEventTypeActive(type))
            {
                StartEvent(type);
            }
        }

        public void ForceEndAllEvents()
        {
            while (activeEvents.Count > 0)
            {
                EndEvent(0);
            }
        }

        public WorldEvent? GetActiveEvent(WorldEventType type)
        {
            foreach (var evt in activeEvents)
            {
                if (evt.Type == type) return evt;
            }
            return null;
        }
    }

    /// <summary>
    /// Represents an active world event
    /// </summary>
    public struct WorldEvent
    {
        public WorldEventManager.WorldEventType Type;
        public float Duration;
        public float ElapsedTime;
        public float Intensity;
        public Vector3 Position;

        public float Progress => Duration > 0 ? ElapsedTime / Duration : 0f;
        public float RemainingTime => Mathf.Max(0f, Duration - ElapsedTime);
    }
}