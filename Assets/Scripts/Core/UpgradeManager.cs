using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Manages player upgrades and currency.
    /// MVP Upgrades: Mote density, Wind strength, Event frequency, Drifter speed, Drifter count, Float band stability
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("Currency")]
        [SerializeField] private int startingCurrency = 100;
        private int currency;

        // Events
        public event Action<int> OnCurrencyChanged;
        public event Action<UpgradeType, int> OnUpgradePurchased;

        // Upgrade definitions
        private Dictionary<UpgradeType, UpgradeDefinition> upgrades;
        private Dictionary<UpgradeType, int> upgradeLevels;

        public int Currency => currency;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeUpgrades();
        }

        private void Start()
        {
            currency = startingCurrency;
        }

        private void InitializeUpgrades()
        {
            upgrades = new Dictionary<UpgradeType, UpgradeDefinition>
            {
                {
                    UpgradeType.MoteDensity,
                    new UpgradeDefinition
                    {
                        Name = "Mote Density",
                        Description = "Increase the density of collectible motes",
                        MaxLevel = 10,
                        BaseCost = 50,
                        CostMultiplier = 1.5f,
                        BaseValue = 1f,
                        ValuePerLevel = 0.2f
                    }
                },
                {
                    UpgradeType.WindStrength,
                    new UpgradeDefinition
                    {
                        Name = "Wind Strength",
                        Description = "Increase the power of wind currents",
                        MaxLevel = 10,
                        BaseCost = 75,
                        CostMultiplier = 1.6f,
                        BaseValue = 1f,
                        ValuePerLevel = 0.15f
                    }
                },
                {
                    UpgradeType.EventFrequency,
                    new UpgradeDefinition
                    {
                        Name = "Event Frequency",
                        Description = "Increase how often world events occur",
                        MaxLevel = 8,
                        BaseCost = 100,
                        CostMultiplier = 1.8f,
                        BaseValue = 1f,
                        ValuePerLevel = 0.1f
                    }
                },
                {
                    UpgradeType.DrifterSpeed,
                    new UpgradeDefinition
                    {
                        Name = "Drifter Speed",
                        Description = "Increase the movement speed of drifter creatures",
                        MaxLevel = 10,
                        BaseCost = 60,
                        CostMultiplier = 1.5f,
                        BaseValue = 1f,
                        ValuePerLevel = 0.1f
                    }
                },
                {
                    UpgradeType.DrifterCount,
                    new UpgradeDefinition
                    {
                        Name = "Drifter Count",
                        Description = "Increase the maximum number of drifter creatures",
                        MaxLevel = 15,
                        BaseCost = 80,
                        CostMultiplier = 1.7f,
                        BaseValue = 5f,
                        ValuePerLevel = 2f
                    }
                },
                {
                    UpgradeType.FloatBandStability,
                    new UpgradeDefinition
                    {
                        Name = "Float Band Stability",
                        Description = "Reduce wobble and increase stability of float bands",
                        MaxLevel = 8,
                        BaseCost = 120,
                        CostMultiplier = 2f,
                        BaseValue = 1f,
                        ValuePerLevel = 0.15f
                    }
                }
            };

            upgradeLevels = new Dictionary<UpgradeType, int>();
            foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
            {
                upgradeLevels[type] = 0;
            }
        }

        #region Public API

        public bool CanAfford(int cost)
        {
            return currency >= cost;
        }

        public void AddCurrency(int amount)
        {
            currency += amount;
            OnCurrencyChanged?.Invoke(currency);
        }

        public bool SpendCurrency(int amount)
        {
            if (!CanAfford(amount)) return false;
            currency -= amount;
            OnCurrencyChanged?.Invoke(currency);
            return true;
        }

        public void SetCurrency(int amount)
        {
            currency = amount;
            OnCurrencyChanged?.Invoke(currency);
        }

        public int GetUpgradeLevel(UpgradeType type)
        {
            return upgradeLevels.TryGetValue(type, out int level) ? level : 0;
        }

        public int GetUpgradeCost(UpgradeType type)
        {
            if (!upgrades.TryGetValue(type, out UpgradeDefinition def)) return int.MaxValue;
            int level = GetUpgradeLevel(type);
            if (level >= def.MaxLevel) return int.MaxValue;
            return Mathf.RoundToInt(def.BaseCost * Mathf.Pow(def.CostMultiplier, level));
        }

        public float GetUpgradeValue(UpgradeType type)
        {
            if (!upgrades.TryGetValue(type, out UpgradeDefinition def)) return 1f;
            int level = GetUpgradeLevel(type);
            return def.BaseValue + (def.ValuePerLevel * level);
        }

        public int GetMaxLevel(UpgradeType type)
        {
            return upgrades.TryGetValue(type, out UpgradeDefinition def) ? def.MaxLevel : 0;
        }

        public bool CanUpgrade(UpgradeType type)
        {
            if (!upgrades.TryGetValue(type, out UpgradeDefinition def)) return false;
            int level = GetUpgradeLevel(type);
            if (level >= def.MaxLevel) return false;
            return CanAfford(GetUpgradeCost(type));
        }

        public bool TryPurchaseUpgrade(UpgradeType type)
        {
            if (!CanUpgrade(type)) return false;

            int cost = GetUpgradeCost(type);
            if (!SpendCurrency(cost)) return false;

            upgradeLevels[type]++;
            OnUpgradePurchased?.Invoke(type, upgradeLevels[type]);

            // Notify relevant systems
            ApplyUpgradeEffect(type);

            return true;
        }

        public UpgradeDefinition GetUpgradeDefinition(UpgradeType type)
        {
            return upgrades.TryGetValue(type, out UpgradeDefinition def) ? def : null;
        }

        public UpgradeLevelData[] GetAllUpgradeLevels()
        {
            List<UpgradeLevelData> data = new List<UpgradeLevelData>();
            foreach (var kvp in upgradeLevels)
            {
                data.Add(new UpgradeLevelData
                {
                    upgradeId = kvp.Key.ToString(),
                    level = kvp.Value
                });
            }
            return data.ToArray();
        }

        public void SetAllUpgradeLevels(UpgradeLevelData[] levels)
        {
            foreach (var data in levels)
            {
                if (Enum.TryParse(data.upgradeId, out UpgradeType type))
                {
                    upgradeLevels[type] = data.level;
                    ApplyUpgradeEffect(type);
                }
            }
        }

        #endregion

        #region Effects

        private void ApplyUpgradeEffect(UpgradeType type)
        {
            float value = GetUpgradeValue(type);

            switch (type)
            {
                case UpgradeType.MoteDensity:
                    // ResourceManager handles this
                    ResourceManager resourceMgr = FindFirstObjectByType<ResourceManager>();
                    resourceMgr?.SetMoteDensityMultiplier(value);
                    break;

                case UpgradeType.WindStrength:
                    // WeatherSystem handles this
                    WeatherSystem weather = FindFirstObjectByType<WeatherSystem>();
                    weather?.SetWindStrengthMultiplier(value);
                    break;

                case UpgradeType.EventFrequency:
                    // WorldEventManager handles this
                    WorldEventManager events = FindFirstObjectByType<WorldEventManager>();
                    events?.SetEventFrequencyMultiplier(value);
                    break;

                case UpgradeType.DrifterSpeed:
                    // EcosystemManager handles this
                    EcosystemManager ecosystem = FindFirstObjectByType<EcosystemManager>();
                    ecosystem?.SetDrifterSpeedMultiplier(value);
                    break;

                case UpgradeType.DrifterCount:
                    // EcosystemManager handles this
                    EcosystemManager ecosystemCount = FindFirstObjectByType<EcosystemManager>();
                    ecosystemCount?.SetMaxDrifterCount(Mathf.RoundToInt(value));
                    break;

                case UpgradeType.FloatBandStability:
                    // FloatBand handles this globally
                    FloatBand.SetGlobalStabilityMultiplier(value);
                    break;
            }
        }

        #endregion
    }

    public enum UpgradeType
    {
        MoteDensity,
        WindStrength,
        EventFrequency,
        DrifterSpeed,
        DrifterCount,
        FloatBandStability
    }

    [Serializable]
    public class UpgradeDefinition
    {
        public string Name;
        public string Description;
        public int MaxLevel;
        public int BaseCost;
        public float CostMultiplier;
        public float BaseValue;
        public float ValuePerLevel;
    }
}
