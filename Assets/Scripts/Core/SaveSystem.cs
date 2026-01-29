using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Unified save system for all world state.
    /// Handles serialization/deserialization of player, world, creatures, and resources.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [Header("Auto-Save Settings")]
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
        [SerializeField] private bool autoSaveEnabled = true;

        [Header("Save File")]
        [SerializeField] private string saveFileName = "terrarium_save.json";

        // Events
        public event Action OnSaveStarted;
        public event Action OnSaveCompleted;
        public event Action<SaveData> OnLoadCompleted;
        public event Action<string> OnSaveError;

        private float lastAutoSaveTime;
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

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
            lastAutoSaveTime = Time.time;
        }

        private void Update()
        {
            if (autoSaveEnabled && Time.time - lastAutoSaveTime >= autoSaveInterval)
            {
                AutoSave();
                lastAutoSaveTime = Time.time;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGame();
            }
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        #region Public API

        public void SaveGame()
        {
            try
            {
                OnSaveStarted?.Invoke();

                SaveData saveData = GatherSaveData();
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(SaveFilePath, json);

                Debug.Log($"[SaveSystem] Game saved to {SaveFilePath}");
                OnSaveCompleted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
                OnSaveError?.Invoke(e.Message);
            }
        }

        public bool LoadGame()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.Log("[SaveSystem] No save file found, starting fresh.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                if (saveData == null)
                {
                    Debug.LogWarning("[SaveSystem] Save data is null, starting fresh.");
                    return false;
                }

                ApplySaveData(saveData);
                OnLoadCompleted?.Invoke(saveData);

                Debug.Log("[SaveSystem] Game loaded successfully.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
                OnSaveError?.Invoke(e.Message);
                return false;
            }
        }

        public bool HasSaveFile()
        {
            return File.Exists(SaveFilePath);
        }

        public void DeleteSaveFile()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveSystem] Save file deleted.");
            }
        }

        public DateTime GetLastSaveTime()
        {
            if (File.Exists(SaveFilePath))
            {
                return File.GetLastWriteTime(SaveFilePath);
            }
            return DateTime.MinValue;
        }

        #endregion

        #region Data Gathering

        private SaveData GatherSaveData()
        {
            SaveData data = new SaveData();

            // Meta
            data.saveVersion = 1;
            data.saveTime = DateTime.UtcNow.ToString("o");

            // Player
            GatherPlayerData(data);

            // World State
            GatherWorldTimeData(data);
            GatherWeatherData(data);

            // Upgrades
            GatherUpgradeData(data);

            // Creatures
            GatherCreatureData(data);

            // Resources
            GatherResourceData(data);

            // Events
            GatherEventData(data);

            return data;
        }

        private void GatherPlayerData(SaveData data)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                data.playerPosition = player.transform.position;
                data.playerVelocity = player.GetComponent<Rigidbody2D>()?.linearVelocity ?? Vector2.zero;
            }

            // Currency from upgrade manager
            UpgradeManager upgrades = UpgradeManager.Instance;
            if (upgrades != null)
            {
                data.currency = upgrades.Currency;
            }
        }

        private void GatherWorldTimeData(SaveData data)
        {
            WorldTimeManager timeManager = FindFirstObjectByType<WorldTimeManager>();
            if (timeManager != null)
            {
                data.worldTime = timeManager.CurrentWorldTime;
                data.currentDay = timeManager.CurrentDay;
                data.currentSeason = (int)timeManager.CurrentSeason;
            }
        }

        private void GatherWeatherData(SaveData data)
        {
            WeatherSystem weather = FindFirstObjectByType<WeatherSystem>();
            if (weather != null)
            {
                data.currentWeather = (int)weather.CurrentWeather;
                data.weatherDuration = weather.RemainingWeatherDuration;
            }
        }

        private void GatherUpgradeData(SaveData data)
        {
            UpgradeManager upgrades = UpgradeManager.Instance;
            if (upgrades != null)
            {
                data.upgradeLevels = upgrades.GetAllUpgradeLevels();
            }
        }

        private void GatherCreatureData(SaveData data)
        {
            EcosystemManager ecosystem = FindFirstObjectByType<EcosystemManager>();
            if (ecosystem != null)
            {
                data.creatures = ecosystem.GetCreatureSaveData();
            }
        }

        private void GatherResourceData(SaveData data)
        {
            ResourceManager resources = FindFirstObjectByType<ResourceManager>();
            if (resources != null)
            {
                data.resources = resources.GetResourceSaveData();
            }
        }

        private void GatherEventData(SaveData data)
        {
            WorldEventManager events = FindFirstObjectByType<WorldEventManager>();
            if (events != null)
            {
                data.eventHistory = events.GetEventHistorySaveData();
                data.eventCooldowns = events.GetEventCooldownsSaveData();
            }
        }

        #endregion

        #region Data Application

        private void ApplySaveData(SaveData data)
        {
            // Player
            ApplyPlayerData(data);

            // World Time
            ApplyWorldTimeData(data);

            // Weather
            ApplyWeatherData(data);

            // Upgrades
            ApplyUpgradeData(data);

            // Creatures
            ApplyCreatureData(data);

            // Resources
            ApplyResourceData(data);

            // Events
            ApplyEventData(data);
        }

        private void ApplyPlayerData(SaveData data)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.transform.position = data.playerPosition;
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = data.playerVelocity;
                }
            }

            UpgradeManager upgrades = UpgradeManager.Instance;
            if (upgrades != null)
            {
                upgrades.SetCurrency(data.currency);
            }
        }

        private void ApplyWorldTimeData(SaveData data)
        {
            WorldTimeManager timeManager = FindFirstObjectByType<WorldTimeManager>();
            if (timeManager != null)
            {
                timeManager.SetWorldTime(data.worldTime, data.currentDay, (Season)data.currentSeason);
            }
        }

        private void ApplyWeatherData(SaveData data)
        {
            WeatherSystem weather = FindFirstObjectByType<WeatherSystem>();
            if (weather != null)
            {
                weather.SetWeather((WeatherType)data.currentWeather, data.weatherDuration);
            }
        }

        private void ApplyUpgradeData(SaveData data)
        {
            UpgradeManager upgrades = UpgradeManager.Instance;
            if (upgrades != null && data.upgradeLevels != null)
            {
                upgrades.SetAllUpgradeLevels(data.upgradeLevels);
            }
        }

        private void ApplyCreatureData(SaveData data)
        {
            EcosystemManager ecosystem = FindFirstObjectByType<EcosystemManager>();
            if (ecosystem != null && data.creatures != null)
            {
                ecosystem.LoadCreatureSaveData(data.creatures);
            }
        }

        private void ApplyResourceData(SaveData data)
        {
            ResourceManager resources = FindFirstObjectByType<ResourceManager>();
            if (resources != null && data.resources != null)
            {
                resources.LoadResourceSaveData(data.resources);
            }
        }

        private void ApplyEventData(SaveData data)
        {
            WorldEventManager events = FindFirstObjectByType<WorldEventManager>();
            if (events != null)
            {
                if (data.eventHistory != null)
                    events.LoadEventHistorySaveData(data.eventHistory);
                if (data.eventCooldowns != null)
                    events.LoadEventCooldownsSaveData(data.eventCooldowns);
            }
        }

        #endregion

        private void AutoSave()
        {
            Debug.Log("[SaveSystem] Auto-saving...");
            SaveGame();
        }
    }

    #region Save Data Structures

    [Serializable]
    public class SaveData
    {
        // Meta
        public int saveVersion;
        public string saveTime;

        // Player
        public Vector3 playerPosition;
        public Vector2 playerVelocity;
        public int currency;

        // World Time
        public float worldTime;
        public int currentDay;
        public int currentSeason;

        // Weather
        public int currentWeather;
        public float weatherDuration;

        // Upgrades
        public UpgradeLevelData[] upgradeLevels;

        // Creatures
        public CreatureSaveData[] creatures;

        // Resources
        public ResourceSaveData[] resources;

        // Events
        public EventHistoryData[] eventHistory;
        public EventCooldownData[] eventCooldowns;
    }

    [Serializable]
    public class UpgradeLevelData
    {
        public string upgradeId;
        public int level;
    }

    [Serializable]
    public class CreatureSaveData
    {
        public string creatureType;
        public Vector3 position;
        public float health;
        public int state;
        public string uniqueId;
    }

    [Serializable]
    public class ResourceSaveData
    {
        public string resourceType;
        public Vector3 position;
        public float currentAmount;
        public float respawnTimer;
        public string uniqueId;
    }

    [Serializable]
    public class EventHistoryData
    {
        public string eventType;
        public string timestamp;
        public float worldTimeOccurred;
    }

    [Serializable]
    public class EventCooldownData
    {
        public string eventType;
        public float remainingCooldown;
    }

    #endregion
}
