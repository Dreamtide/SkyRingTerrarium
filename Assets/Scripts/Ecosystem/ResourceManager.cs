using UnityEngine;
using System;
using System.Collections.Generic;

namespace SkyRingTerrarium.Ecosystem
{
    /// <summary>
    /// Manages resource nodes that spawn, grow, mature, deplete, and regenerate.
    /// Growth is affected by weather, day/night, and nearby creatures.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        #region Events
        public static event Action<ResourceNode> OnResourceSpawned;
        public static event Action<ResourceNode> OnResourceDepleted;
        public static event Action OnBloomStarted;
        public static event Action OnBloomEnded;
        #endregion

        #region Serialized Fields
        [Header("Resource Limits")]
        [SerializeField] private int maxResourceNodes = 50;
        [SerializeField] private float spawnCheckInterval = 15f;
        [SerializeField] private float minSpawnDistance = 3f;

        [Header("Spawn Settings")]
        [SerializeField] private List<ResourceSpawnData> resourceTypes;
        [SerializeField] private float baseSpawnChance = 0.3f;

        [Header("Bloom Events")]
        [SerializeField] private float bloomChance = 0.05f;
        [SerializeField] private float bloomDuration = 60f;
        [SerializeField] private float bloomGrowthMultiplier = 3f;
        [SerializeField] private int bloomSpawnCount = 10;

        [Header("Season Modifiers")]
        [SerializeField] private float springGrowthMod = 1.2f;
        [SerializeField] private float summerGrowthMod = 1.0f;
        [SerializeField] private float autumnGrowthMod = 0.8f;
        [SerializeField] private float winterGrowthMod = 0.4f;
        #endregion

        #region Public Properties
        public int ActiveResourceCount => allResources.Count;
        public bool IsBloomActive => isBloomActive;
        public float CurrentGrowthMultiplier => GetCurrentGrowthMultiplier();
        #endregion

        #region Private Fields
        private List<ResourceNode> allResources = new List<ResourceNode>();
        private float spawnTimer;
        private bool isBloomActive;
        private float bloomTimer;
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
            World.WorldTimeManager.OnSeasonChanged += OnSeasonChanged;
            World.WeatherSystem.OnWeatherChanged += OnWeatherChanged;
        }

        private void OnDestroy()
        {
            World.WorldTimeManager.OnSeasonChanged -= OnSeasonChanged;
            World.WeatherSystem.OnWeatherChanged -= OnWeatherChanged;
        }

        private void Update()
        {
            UpdateSpawning();
            UpdateBloom();
            CleanupDeadResources();
        }
        #endregion

        #region Registration
        public void RegisterResource(ResourceNode resource)
        {
            if (!allResources.Contains(resource))
            {
                allResources.Add(resource);
                OnResourceSpawned?.Invoke(resource);
            }
        }

        public void UnregisterResource(ResourceNode resource)
        {
            if (allResources.Remove(resource))
            {
                OnResourceDepleted?.Invoke(resource);
            }
        }
        #endregion

        #region Spawning
        private void UpdateSpawning()
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer < spawnCheckInterval) return;
            spawnTimer = 0f;

            if (allResources.Count >= maxResourceNodes) return;

            float spawnChance = baseSpawnChance * GetSeasonSpawnModifier();
            
            if (UnityEngine.Random.value < spawnChance)
            {
                SpawnRandomResource();
            }
        }

        private void SpawnRandomResource()
        {
            if (resourceTypes.Count == 0) return;

            float totalWeight = 0f;
            foreach (var type in resourceTypes)
            {
                totalWeight += type.spawnWeight * GetTypeSeasonModifier(type.resourceType);
            }

            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;

            foreach (var type in resourceTypes)
            {
                cumulative += type.spawnWeight * GetTypeSeasonModifier(type.resourceType);
                if (roll <= cumulative)
                {
                    SpawnResource(type);
                    break;
                }
            }
        }

        private void SpawnResource(ResourceSpawnData data)
        {
            if (data.prefab == null) return;

            Vector2 spawnPos = GetValidSpawnPosition();
            if (spawnPos == Vector2.zero) return;

            GameObject obj = Instantiate(data.prefab, spawnPos, Quaternion.identity, transform);
            var resource = obj.GetComponent<ResourceNode>();
            if (resource != null)
            {
                resource.Initialize(data.resourceType);
            }
        }

        private Vector2 GetValidSpawnPosition()
        {
            for (int attempts = 0; attempts < 10; attempts++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = 20f; // Ring radius
                Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                bool valid = true;
                foreach (var resource in allResources)
                {
                    if (resource != null && Vector2.Distance(pos, resource.transform.position) < minSpawnDistance)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid) return pos;
            }
            return Vector2.zero;
        }

        private float GetSeasonSpawnModifier()
        {
            if (World.WorldTimeManager.Instance == null) return 1f;

            return World.WorldTimeManager.Instance.CurrentSeason switch
            {
                World.WorldTimeManager.Season.Spring => 1.5f,
                World.WorldTimeManager.Season.Summer => 1.2f,
                World.WorldTimeManager.Season.Autumn => 0.8f,
                World.WorldTimeManager.Season.Winter => 0.3f,
                _ => 1f
            };
        }

        private float GetTypeSeasonModifier(ResourceType type)
        {
            if (World.WorldTimeManager.Instance == null) return 1f;

            var season = World.WorldTimeManager.Instance.CurrentSeason;
            
            return (type, season) switch
            {
                (ResourceType.Flower, World.WorldTimeManager.Season.Spring) => 2f,
                (ResourceType.Fruit, World.WorldTimeManager.Season.Summer) => 2f,
                (ResourceType.Mushroom, World.WorldTimeManager.Season.Autumn) => 2f,
                (ResourceType.Crystal, World.WorldTimeManager.Season.Winter) => 1.5f,
                _ => 1f
            };
        }
        #endregion

        #region Growth Multipliers
        public float GetCurrentGrowthMultiplier()
        {
            float multiplier = 1f;

            // Time of day
            if (World.WorldTimeManager.Instance != null)
            {
                multiplier *= World.WorldTimeManager.Instance.CurrentTimeOfDayPhase switch
                {
                    World.WorldTimeManager.TimeOfDay.Day => 1.2f,
                    World.WorldTimeManager.TimeOfDay.Dawn => 1.1f,
                    World.WorldTimeManager.TimeOfDay.Dusk => 0.9f,
                    World.WorldTimeManager.TimeOfDay.Night => 0.6f,
                    _ => 1f
                };

                // Season
                multiplier *= World.WorldTimeManager.Instance.CurrentSeason switch
                {
                    World.WorldTimeManager.Season.Spring => springGrowthMod,
                    World.WorldTimeManager.Season.Summer => summerGrowthMod,
                    World.WorldTimeManager.Season.Autumn => autumnGrowthMod,
                    World.WorldTimeManager.Season.Winter => winterGrowthMod,
                    _ => 1f
                };
            }

            // Weather
            if (World.WeatherSystem.Instance != null)
            {
                multiplier *= World.WeatherSystem.Instance.CurrentWeather switch
                {
                    World.WeatherSystem.WeatherState.Clear => 1f,
                    World.WeatherSystem.WeatherState.Stormy => 1.3f,
                    World.WeatherSystem.WeatherState.Misty => 1.1f,
                    World.WeatherSystem.WeatherState.Calm => 0.9f,
                    World.WeatherSystem.WeatherState.Windy => 0.8f,
                    _ => 1f
                };
            }

            // Bloom event
            if (isBloomActive)
            {
                multiplier *= bloomGrowthMultiplier;
            }

            return multiplier;
        }
        #endregion

        #region Bloom Events
        private void UpdateBloom()
        {
            if (isBloomActive)
            {
                bloomTimer -= Time.deltaTime;
                if (bloomTimer <= 0)
                {
                    EndBloom();
                }
            }
        }

        public void TriggerBloom()
        {
            if (isBloomActive) return;

            isBloomActive = true;
            bloomTimer = bloomDuration;
            
            // Spawn extra resources
            for (int i = 0; i < bloomSpawnCount && allResources.Count < maxResourceNodes; i++)
            {
                SpawnRandomResource();
            }

            OnBloomStarted?.Invoke();
        }

        private void EndBloom()
        {
            isBloomActive = false;
            OnBloomEnded?.Invoke();
        }

        private void OnSeasonChanged(World.WorldTimeManager.Season season)
        {
            // Spring blooms
            if (season == World.WorldTimeManager.Season.Spring && UnityEngine.Random.value < bloomChance * 2f)
            {
                TriggerBloom();
            }
        }

        private void OnWeatherChanged(World.WeatherSystem.WeatherState weather)
        {
            // Rain after calm can trigger bloom
            if (weather == World.WeatherSystem.WeatherState.Stormy && UnityEngine.Random.value < bloomChance)
            {
                TriggerBloom();
            }
        }
        #endregion

        #region Queries
        public ResourceNode GetNearestResource(Vector2 position, float maxDistance)
        {
            ResourceNode nearest = null;
            float nearestDist = maxDistance;

            foreach (var resource in allResources)
            {
                if (resource == null || resource.IsDepleted) continue;

                float dist = Vector2.Distance(position, resource.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = resource;
                }
            }

            return nearest;
        }

        public List<ResourceNode> GetResourcesInRange(Vector2 position, float range)
        {
            var result = new List<ResourceNode>();
            foreach (var resource in allResources)
            {
                if (resource != null && !resource.IsDepleted)
                {
                    if (Vector2.Distance(position, resource.transform.position) <= range)
                    {
                        result.Add(resource);
                    }
                }
            }
            return result;
        }
        #endregion

        #region Cleanup
        private void CleanupDeadResources()
        {
            allResources.RemoveAll(r => r == null);
        }
        #endregion
    }

    public enum ResourceType
    {
        Mote,       // Basic energy particles
        Flower,     // Spring-dominant
        Fruit,      // Summer-dominant  
        Mushroom,   // Autumn-dominant
        Crystal,    // Winter-dominant
        Nectar,     // Rare, high energy
        Spore       // Reproducing resource
    }

    [System.Serializable]
    public struct ResourceSpawnData
    {
        public string name;
        public ResourceType resourceType;
        public GameObject prefab;
        public float spawnWeight;
    }
}