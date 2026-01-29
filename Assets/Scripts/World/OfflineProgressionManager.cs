using UnityEngine;
using System;

namespace SkyRingTerrarium.World
{
    /// <summary>
    /// Manages offline progression - simulating world state forward when player returns.
    /// Calculates elapsed real time and applies simplified fast-forward simulation.
    /// </summary>
    public class OfflineProgressionManager : MonoBehaviour
    {
        public static OfflineProgressionManager Instance { get; private set; }

        #region Events
        public static event Action<float> OnOfflineProgressionStart;
        public static event Action<OfflineProgressionReport> OnOfflineProgressionComplete;
        #endregion

        #region Serialized Fields
        [Header("Offline Limits")]
        [SerializeField] private float maxOfflineHours = 24f;
        [SerializeField] private float simulationSpeedMultiplier = 100f;
        [SerializeField] private int maxSimulationSteps = 1000;

        [Header("Save Settings")]
        [SerializeField] private string saveKey = "SkyRingTerrarium_WorldState";
        [SerializeField] private bool autoSaveOnQuit = true;
        [SerializeField] private float autoSaveInterval = 60f;
        #endregion

        #region Private Fields
        private float autoSaveTimer;
        private WorldSaveState lastSaveState;
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
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadAndSimulate();
        }

        private void Update()
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                autoSaveTimer = 0f;
                SaveWorldState();
            }
        }

        private void OnApplicationQuit()
        {
            if (autoSaveOnQuit)
            {
                SaveWorldState();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveWorldState();
            }
            else
            {
                LoadAndSimulate();
            }
        }
        #endregion

        #region Save System
        public void SaveWorldState()
        {
            var state = new WorldSaveState
            {
                saveTimeTicks = DateTime.UtcNow.Ticks,
                
                // Time state
                timeOfDay = WorldTimeManager.Instance?.TimeOfDayNormalized ?? 0f,
                currentDay = WorldTimeManager.Instance?.CurrentDay ?? 0,
                currentYear = WorldTimeManager.Instance?.CurrentYear ?? 0,
                currentSeason = (int)(WorldTimeManager.Instance?.CurrentSeason ?? 0),
                
                // Weather state
                currentWeather = (int)(WeatherSystem.Instance?.CurrentWeather ?? 0),
                
                // Population counts (simplified)
                producerCount = Ecosystem.EcosystemManager.Instance?.GetPopulationByType(Ecosystem.CreatureAI.CreatureType.Producer) ?? 0,
                herbivoreCount = Ecosystem.EcosystemManager.Instance?.GetPopulationByType(Ecosystem.CreatureAI.CreatureType.Herbivore) ?? 0,
                predatorCount = Ecosystem.EcosystemManager.Instance?.GetPopulationByType(Ecosystem.CreatureAI.CreatureType.Predator) ?? 0,
                
                // Resource count
                resourceCount = Ecosystem.ResourceManager.Instance?.ActiveResourceCount ?? 0
            };

            string json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();
            
            lastSaveState = state;
        }

        public void LoadAndSimulate()
        {
            if (!PlayerPrefs.HasKey(saveKey))
            {
                return;
            }

            string json = PlayerPrefs.GetString(saveKey);
            var state = JsonUtility.FromJson<WorldSaveState>(json);

            long ticksElapsed = DateTime.UtcNow.Ticks - state.saveTimeTicks;
            float secondsElapsed = ticksElapsed / (float)TimeSpan.TicksPerSecond;
            
            float maxOfflineSeconds = maxOfflineHours * 3600f;
            secondsElapsed = Mathf.Min(secondsElapsed, maxOfflineSeconds);

            if (secondsElapsed < 1f)
            {
                ApplyState(state);
                return;
            }

            OnOfflineProgressionStart?.Invoke(secondsElapsed);
            
            var report = SimulateOfflineTime(state, secondsElapsed);
            
            OnOfflineProgressionComplete?.Invoke(report);
        }
        #endregion

        #region Simulation
        private OfflineProgressionReport SimulateOfflineTime(WorldSaveState startState, float elapsedSeconds)
        {
            var report = new OfflineProgressionReport
            {
                elapsedRealSeconds = elapsedSeconds,
                startState = startState
            };

            // Calculate simulation steps
            float simulatedGameSeconds = elapsedSeconds * simulationSpeedMultiplier;
            int steps = Mathf.Min(Mathf.CeilToInt(elapsedSeconds / 60f), maxSimulationSteps);
            float secondsPerStep = elapsedSeconds / steps;

            // Simulate time progression
            float timeProgression = elapsedSeconds / (WorldTimeManager.Instance != null ? 
                (PlayerPrefs.GetFloat("realMinutesPerGameDay", 10f) * 60f) : 600f);
            
            float newTimeOfDay = (startState.timeOfDay + timeProgression) % 1f;
            int daysProgressed = Mathf.FloorToInt(startState.timeOfDay + timeProgression);
            int newDay = startState.currentDay + daysProgressed;

            // Simulate weather changes
            int weatherChanges = Mathf.FloorToInt(elapsedSeconds / 180f); // Weather changes roughly every 3 minutes
            int newWeather = startState.currentWeather;
            for (int i = 0; i < weatherChanges; i++)
            {
                if (UnityEngine.Random.value < 0.3f)
                {
                    newWeather = UnityEngine.Random.Range(0, 5);
                }
            }

            // Simulate population dynamics (simplified)
            float birthRate = 0.001f;
            float deathRate = 0.0008f;
            float predationRate = 0.0005f;

            int producers = startState.producerCount;
            int herbivores = startState.herbivoreCount;
            int predators = startState.predatorCount;

            for (int step = 0; step < steps; step++)
            {
                // Producer growth
                int producerBirths = Mathf.RoundToInt(producers * birthRate * secondsPerStep);
                int producerDeaths = Mathf.RoundToInt(producers * deathRate * secondsPerStep);
                producers = Mathf.Max(1, producers + producerBirths - producerDeaths);

                // Herbivore dynamics
                int herbivoreBirths = producers > 0 ? Mathf.RoundToInt(herbivores * birthRate * secondsPerStep) : 0;
                int herbivoreDeaths = Mathf.RoundToInt(herbivores * deathRate * secondsPerStep);
                int herbivoresPredated = predators > 0 ? Mathf.RoundToInt(herbivores * predationRate * predators * secondsPerStep) : 0;
                herbivores = Mathf.Max(0, herbivores + herbivoreBirths - herbivoreDeaths - herbivoresPredated);

                // Predator dynamics
                int predatorBirths = herbivores > 2 ? Mathf.RoundToInt(predators * birthRate * 0.5f * secondsPerStep) : 0;
                int predatorDeaths = herbivores < 2 ? 
                    Mathf.RoundToInt(predators * deathRate * 2f * secondsPerStep) : 
                    Mathf.RoundToInt(predators * deathRate * secondsPerStep);
                predators = Mathf.Max(0, predators + predatorBirths - predatorDeaths);

                // Cap populations
                int maxPop = 100;
                producers = Mathf.Min(producers, Mathf.RoundToInt(maxPop * 0.5f));
                herbivores = Mathf.Min(herbivores, Mathf.RoundToInt(maxPop * 0.35f));
                predators = Mathf.Min(predators, Mathf.RoundToInt(maxPop * 0.15f));
            }

            // Resource simulation
            int resourceGrowth = Mathf.RoundToInt(elapsedSeconds / 30f);
            int resourceDepletion = herbivores * Mathf.RoundToInt(elapsedSeconds / 60f);
            int newResourceCount = Mathf.Clamp(startState.resourceCount + resourceGrowth - resourceDepletion, 5, 50);

            // Create final state
            var endState = new WorldSaveState
            {
                saveTimeTicks = DateTime.UtcNow.Ticks,
                timeOfDay = newTimeOfDay,
                currentDay = newDay,
                currentYear = startState.currentYear + (newDay / 28),
                currentSeason = (newDay / 7) % 4,
                currentWeather = newWeather,
                producerCount = producers,
                herbivoreCount = herbivores,
                predatorCount = predators,
                resourceCount = newResourceCount
            };

            report.endState = endState;
            report.daysSimulated = daysProgressed;
            report.populationChange = (producers + herbivores + predators) - 
                                     (startState.producerCount + startState.herbivoreCount + startState.predatorCount);
            report.weatherChanges = weatherChanges;

            ApplyState(endState);
            
            return report;
        }

        private void ApplyState(WorldSaveState state)
        {
            // Apply to WorldTimeManager
            WorldTimeManager.Instance?.SetTime(state.timeOfDay, state.currentDay, 
                (WorldTimeManager.Season)state.currentSeason);

            // Apply to WeatherSystem
            WeatherSystem.Instance?.SetWeather((WeatherSystem.WeatherState)state.currentWeather, true);

            // Note: Actual creature/resource spawning would need to happen through 
            // EcosystemManager and ResourceManager based on the counts
        }
        #endregion

        #region Debug
        public void ClearSaveData()
        {
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.Save();
        }

        public WorldSaveState GetLastSaveState()
        {
            return lastSaveState;
        }
        #endregion
    }

    [System.Serializable]
    public struct WorldSaveState
    {
        public long saveTimeTicks;
        
        // Time
        public float timeOfDay;
        public int currentDay;
        public int currentYear;
        public int currentSeason;
        
        // Weather
        public int currentWeather;
        
        // Population
        public int producerCount;
        public int herbivoreCount;
        public int predatorCount;
        
        // Resources
        public int resourceCount;
    }

    [System.Serializable]
    public struct OfflineProgressionReport
    {
        public float elapsedRealSeconds;
        public int daysSimulated;
        public int populationChange;
        public int weatherChanges;
        public WorldSaveState startState;
        public WorldSaveState endState;
    }
}