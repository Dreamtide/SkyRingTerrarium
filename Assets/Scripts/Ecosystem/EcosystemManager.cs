using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyRingTerrarium.Ecosystem
{
    /// <summary>
    /// Manages the overall ecosystem: population dynamics, carrying capacity,
    /// migrations, and inter-species interactions.
    /// </summary>
    public class EcosystemManager : MonoBehaviour
    {
        public static EcosystemManager Instance { get; private set; }

        #region Events
        public static event Action<string, int> OnPopulationChanged;
        public static event Action OnMigrationStarted;
        public static event Action OnMigrationEnded;
        public static event Action<string> OnSpeciesExtinct;
        #endregion

        #region Serialized Fields
        [Header("Population Limits")]
        [SerializeField] private int globalCarryingCapacity = 100;
        [SerializeField] private int minPopulationForSpecies = 2;
        
        [Header("Population Balance")]
        [SerializeField] private float producerRatio = 0.5f;
        [SerializeField] private float herbivoreRatio = 0.35f;
        [SerializeField] private float predatorRatio = 0.15f;

        [Header("Auto-Spawning")]
        [SerializeField] private bool enableAutoSpawn = true;
        [SerializeField] private float spawnCheckInterval = 10f;
        [SerializeField] private List<CreatureSpawnData> spawnableCreatures;

        [Header("Migration")]
        [SerializeField] private float migrationCheckInterval = 60f;
        [SerializeField] private float migrationChance = 0.2f;
        [SerializeField] private float migrationDuration = 30f;
        [SerializeField] private List<Vector2> migrationWaypoints;

        [Header("Offline Simulation")]
        [SerializeField] private float offlineSimulationStep = 60f;
        #endregion

        #region Public Properties
        public int TotalPopulation => allCreatures.Count;
        public bool IsMigrationActive => isMigrationActive;
        public Dictionary<string, int> PopulationBySpecies => GetPopulationBySpecies();
        #endregion

        #region Private Fields
        private List<CreatureAI> allCreatures = new List<CreatureAI>();
        private Dictionary<string, List<CreatureAI>> creaturesBySpecies = new Dictionary<string, List<CreatureAI>>();
        private float spawnTimer;
        private float migrationTimer;
        private bool isMigrationActive;
        private Vector2 currentMigrationTarget;
        private float migrationActiveTimer;
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
            CreatureAI.OnCreatureBorn += OnCreatureBorn;
            CreatureAI.OnCreatureDied += OnCreatureDied;
            World.WorldTimeManager.OnSeasonChanged += OnSeasonChanged;
            World.WeatherSystem.OnWeatherChanged += OnWeatherChanged;
        }

        private void OnDestroy()
        {
            CreatureAI.OnCreatureBorn -= OnCreatureBorn;
            CreatureAI.OnCreatureDied -= OnCreatureDied;
            World.WorldTimeManager.OnSeasonChanged -= OnSeasonChanged;
            World.WeatherSystem.OnWeatherChanged -= OnWeatherChanged;
        }

        private void Update()
        {
            UpdateSpawning();
            UpdateMigration();
        }
        #endregion

        #region Registration
        public void RegisterCreature(CreatureAI creature)
        {
            if (!allCreatures.Contains(creature))
            {
                allCreatures.Add(creature);
                
                if (!creaturesBySpecies.ContainsKey(creature.SpeciesName))
                {
                    creaturesBySpecies[creature.SpeciesName] = new List<CreatureAI>();
                }
                creaturesBySpecies[creature.SpeciesName].Add(creature);
                
                OnPopulationChanged?.Invoke(creature.SpeciesName, creaturesBySpecies[creature.SpeciesName].Count);
            }
        }

        public void UnregisterCreature(CreatureAI creature)
        {
            allCreatures.Remove(creature);
            
            if (creaturesBySpecies.ContainsKey(creature.SpeciesName))
            {
                creaturesBySpecies[creature.SpeciesName].Remove(creature);
                int count = creaturesBySpecies[creature.SpeciesName].Count;
                OnPopulationChanged?.Invoke(creature.SpeciesName, count);
                
                if (count == 0)
                {
                    OnSpeciesExtinct?.Invoke(creature.SpeciesName);
                }
            }
        }

        private void OnCreatureBorn(CreatureAI creature)
        {
            // Already handled via RegisterCreature
        }

        private void OnCreatureDied(CreatureAI creature)
        {
            // Already handled via UnregisterCreature
        }
        #endregion

        #region Queries
        public List<CreatureAI> GetCreaturesInRange(Vector2 position, float range)
        {
            return allCreatures.Where(c => 
                c != null && 
                c.IsAlive && 
                Vector2.Distance(position, c.transform.position) <= range
            ).ToList();
        }

        public List<CreatureAI> GetCreaturesByType(CreatureAI.CreatureType type)
        {
            return allCreatures.Where(c => c != null && c.Type == type && c.IsAlive).ToList();
        }

        public List<CreatureAI> GetCreaturesBySpecies(string species)
        {
            if (creaturesBySpecies.TryGetValue(species, out var list))
            {
                return list.Where(c => c != null && c.IsAlive).ToList();
            }
            return new List<CreatureAI>();
        }

        public int GetPopulation(string species)
        {
            if (creaturesBySpecies.TryGetValue(species, out var list))
            {
                return list.Count(c => c != null && c.IsAlive);
            }
            return 0;
        }

        public int GetPopulationByType(CreatureAI.CreatureType type)
        {
            return allCreatures.Count(c => c != null && c.Type == type && c.IsAlive);
        }

        private Dictionary<string, int> GetPopulationBySpecies()
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in creaturesBySpecies)
            {
                result[kvp.Key] = kvp.Value.Count(c => c != null && c.IsAlive);
            }
            return result;
        }
        #endregion

        #region Auto-Spawning
        private void UpdateSpawning()
        {
            if (!enableAutoSpawn) return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer < spawnCheckInterval) return;
            spawnTimer = 0f;

            if (TotalPopulation >= globalCarryingCapacity) return;

            BalancePopulation();
        }

        private void BalancePopulation()
        {
            int producerCount = GetPopulationByType(CreatureAI.CreatureType.Producer);
            int herbivoreCount = GetPopulationByType(CreatureAI.CreatureType.Herbivore);
            int predatorCount = GetPopulationByType(CreatureAI.CreatureType.Predator);
            
            int targetProducers = Mathf.RoundToInt(globalCarryingCapacity * producerRatio);
            int targetHerbivores = Mathf.RoundToInt(globalCarryingCapacity * herbivoreRatio);
            int targetPredators = Mathf.RoundToInt(globalCarryingCapacity * predatorRatio);

            if (producerCount < targetProducers * 0.5f)
            {
                SpawnCreatureOfType(CreatureAI.CreatureType.Producer);
            }
            else if (herbivoreCount < targetHerbivores * 0.5f)
            {
                SpawnCreatureOfType(CreatureAI.CreatureType.Herbivore);
            }
            else if (predatorCount < targetPredators * 0.5f && herbivoreCount > minPopulationForSpecies)
            {
                SpawnCreatureOfType(CreatureAI.CreatureType.Predator);
            }
        }

        private void SpawnCreatureOfType(CreatureAI.CreatureType type)
        {
            var validSpawns = spawnableCreatures.Where(s => s.creatureType == type).ToList();
            if (validSpawns.Count == 0) return;

            var spawnData = validSpawns[UnityEngine.Random.Range(0, validSpawns.Count)];
            if (spawnData.prefab == null) return;

            Vector2 spawnPos = GetRandomSpawnPosition();
            Instantiate(spawnData.prefab, spawnPos, Quaternion.identity);
        }

        private Vector2 GetRandomSpawnPosition()
        {
            // Spawn on the ring surface
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = 20f; // Should match ring radius from RingWorldGenerator
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        #endregion

        #region Migration
        private void UpdateMigration()
        {
            migrationTimer += Time.deltaTime;

            if (isMigrationActive)
            {
                migrationActiveTimer -= Time.deltaTime;
                if (migrationActiveTimer <= 0)
                {
                    EndMigration();
                }
            }
            else if (migrationTimer >= migrationCheckInterval)
            {
                migrationTimer = 0f;
                TryStartMigration();
            }
        }

        private void TryStartMigration()
        {
            if (UnityEngine.Random.value > migrationChance) return;
            if (migrationWaypoints.Count == 0) return;

            StartMigration();
        }

        public void StartMigration()
        {
            isMigrationActive = true;
            migrationActiveTimer = migrationDuration;
            currentMigrationTarget = migrationWaypoints[UnityEngine.Random.Range(0, migrationWaypoints.Count)];
            
            // Trigger migration for herbivores
            var herbivores = GetCreaturesByType(CreatureAI.CreatureType.Herbivore);
            foreach (var creature in herbivores)
            {
                if (UnityEngine.Random.value < 0.6f)
                {
                    creature.TriggerMigration(currentMigrationTarget);
                }
            }

            OnMigrationStarted?.Invoke();
        }

        private void EndMigration()
        {
            isMigrationActive = false;
            OnMigrationEnded?.Invoke();
        }

        public Vector2 GetMigrationTarget(CreatureAI creature)
        {
            return currentMigrationTarget;
        }

        private void OnSeasonChanged(World.WorldTimeManager.Season season)
        {
            // Seasonal migrations
            if (season == World.WorldTimeManager.Season.Autumn || season == World.WorldTimeManager.Season.Spring)
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    StartMigration();
                }
            }
        }

        private void OnWeatherChanged(World.WeatherSystem.WeatherState weather)
        {
            // Weather-triggered migrations
            if (weather == World.WeatherSystem.WeatherState.Stormy && !isMigrationActive)
            {
                if (UnityEngine.Random.value < 0.3f)
                {
                    StartMigration();
                }
            }
        }
        #endregion

        #region Statistics
        public EcosystemStats GetStats()
        {
            return new EcosystemStats
            {
                totalPopulation = TotalPopulation,
                producerCount = GetPopulationByType(CreatureAI.CreatureType.Producer),
                herbivoreCount = GetPopulationByType(CreatureAI.CreatureType.Herbivore),
                predatorCount = GetPopulationByType(CreatureAI.CreatureType.Predator),
                speciesCount = creaturesBySpecies.Count,
                carryingCapacity = globalCarryingCapacity,
                isMigrating = isMigrationActive
            };
        }
        #endregion

        #region Offline Simulation
        public void SimulateOfflineTime(float elapsedSeconds)
        {
            int steps = Mathf.FloorToInt(elapsedSeconds / offlineSimulationStep);
            steps = Mathf.Min(steps, 1000); // Cap to prevent long calculations

            for (int i = 0; i < steps; i++)
            {
                SimulateStep();
            }
        }

        private void SimulateStep()
        {
            // Simplified population simulation
            int producers = GetPopulationByType(CreatureAI.CreatureType.Producer);
            int herbivores = GetPopulationByType(CreatureAI.CreatureType.Herbivore);
            int predators = GetPopulationByType(CreatureAI.CreatureType.Predator);

            // Natural deaths (simplified)
            float deathRate = 0.02f;
            int producerDeaths = Mathf.RoundToInt(producers * deathRate);
            int herbivoreDeaths = Mathf.RoundToInt(herbivores * deathRate);
            int predatorDeaths = Mathf.RoundToInt(predators * deathRate);

            // Predation
            if (predators > 0 && herbivores > 0)
            {
                int predationDeaths = Mathf.Min(predators, herbivores / 3);
                herbivoreDeaths += predationDeaths;
            }

            // Births (if below capacity)
            float birthRate = 0.03f;
            int maxBirths = globalCarryingCapacity - TotalPopulation;
            
            int producerBirths = Mathf.Min(Mathf.RoundToInt(producers * birthRate), maxBirths);
            int herbivoreBirths = Mathf.Min(Mathf.RoundToInt(herbivores * birthRate), maxBirths - producerBirths);
            int predatorBirths = Mathf.Min(Mathf.RoundToInt(predators * birthRate * 0.5f), maxBirths - producerBirths - herbivoreBirths);

            // Apply changes by spawning/destroying (simplified - would need prefab refs)
            // In actual implementation, this would modify stored population counts for offline state
        }
        #endregion
    }

    [System.Serializable]
    public struct CreatureSpawnData
    {
        public string speciesName;
        public GameObject prefab;
        public CreatureAI.CreatureType creatureType;
        public float spawnWeight;
    }

    [System.Serializable]
    public struct EcosystemStats
    {
        public int totalPopulation;
        public int producerCount;
        public int herbivoreCount;
        public int predatorCount;
        public int speciesCount;
        public int carryingCapacity;
        public bool isMigrating;
    }
}