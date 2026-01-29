using UnityEngine;
using System;

namespace SkyRingTerrarium.Ecosystem
{
    /// <summary>
    /// Individual resource node with growth stages, harvesting, and regeneration.
    /// </summary>
    public class ResourceNode : MonoBehaviour
    {
        #region Events
        public event Action<ResourceNode> OnHarvested;
        public event Action<ResourceNode> OnDepleted;
        public event Action<ResourceNode, GrowthStage> OnGrowthStageChanged;
        #endregion

        #region Enums
        public enum GrowthStage { Seed, Sprout, Growing, Mature, Flowering, Depleted }
        #endregion

        #region Serialized Fields
        [Header("Resource Identity")]
        [SerializeField] private ResourceType resourceType = ResourceType.Mote;
        [SerializeField] private string resourceName = "Resource";

        [Header("Growth Settings")]
        [SerializeField] private float baseGrowthRate = 0.1f;
        [SerializeField] private float seedDuration = 5f;
        [SerializeField] private float sproutDuration = 10f;
        [SerializeField] private float growingDuration = 20f;
        [SerializeField] private float matureDuration = 60f;
        [SerializeField] private float floweringDuration = 30f;

        [Header("Resource Values")]
        [SerializeField] private float maxResourceAmount = 100f;
        [SerializeField] private float harvestAmount = 20f;
        [SerializeField] private float regenerationRate = 5f;

        [Header("Visual Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] growthStageSprites;
        [SerializeField] private Color[] growthStageColors;
        [SerializeField] private float maxScale = 1f;
        [SerializeField] private float minScale = 0.2f;

        [Header("Particle Effects")]
        [SerializeField] private ParticleSystem growthParticles;
        [SerializeField] private ParticleSystem harvestParticles;
        [SerializeField] private ParticleSystem matureGlowParticles;
        #endregion

        #region Public Properties
        public ResourceType Type => resourceType;
        public string Name => resourceName;
        public GrowthStage CurrentStage => currentStage;
        public float CurrentAmount => currentAmount;
        public float GrowthProgress => growthProgress;
        public bool IsDepleted => currentStage == GrowthStage.Depleted;
        public bool IsMature => currentStage == GrowthStage.Mature || currentStage == GrowthStage.Flowering;
        #endregion

        #region Private Fields
        private GrowthStage currentStage;
        private float currentAmount;
        private float growthProgress;
        private float stageTimer;
        private float currentStageDuration;
        private bool isRegistered;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (!isRegistered)
            {
                Initialize(resourceType);
            }
        }

        private void Update()
        {
            UpdateGrowth();
            UpdateRegeneration();
            UpdateVisuals();
        }

        private void OnDestroy()
        {
            ResourceManager.Instance?.UnregisterResource(this);
        }
        #endregion

        #region Initialization
        public void Initialize(ResourceType type)
        {
            resourceType = type;
            currentStage = GrowthStage.Seed;
            currentAmount = 0f;
            growthProgress = 0f;
            stageTimer = 0f;
            SetStageDuration();
            
            ResourceManager.Instance?.RegisterResource(this);
            isRegistered = true;
            
            UpdateVisuals();
        }

        private void SetStageDuration()
        {
            currentStageDuration = currentStage switch
            {
                GrowthStage.Seed => seedDuration,
                GrowthStage.Sprout => sproutDuration,
                GrowthStage.Growing => growingDuration,
                GrowthStage.Mature => matureDuration,
                GrowthStage.Flowering => floweringDuration,
                _ => float.MaxValue
            };
        }
        #endregion

        #region Growth
        private void UpdateGrowth()
        {
            if (currentStage == GrowthStage.Depleted) return;

            float growthMultiplier = ResourceManager.Instance?.CurrentGrowthMultiplier ?? 1f;
            float effectiveGrowthRate = baseGrowthRate * growthMultiplier;

            stageTimer += Time.deltaTime * effectiveGrowthRate;
            growthProgress = Mathf.Clamp01(stageTimer / currentStageDuration);

            if (stageTimer >= currentStageDuration)
            {
                AdvanceStage();
            }

            // Accumulate resources during mature/flowering stages
            if (currentStage == GrowthStage.Mature || currentStage == GrowthStage.Flowering)
            {
                if (currentAmount < maxResourceAmount)
                {
                    currentAmount += regenerationRate * growthMultiplier * Time.deltaTime;
                    currentAmount = Mathf.Min(currentAmount, maxResourceAmount);
                }
            }
        }

        private void AdvanceStage()
        {
            GrowthStage previousStage = currentStage;
            
            currentStage = currentStage switch
            {
                GrowthStage.Seed => GrowthStage.Sprout,
                GrowthStage.Sprout => GrowthStage.Growing,
                GrowthStage.Growing => GrowthStage.Mature,
                GrowthStage.Mature => GrowthStage.Flowering,
                GrowthStage.Flowering => GrowthStage.Mature, // Cycles between mature and flowering
                _ => GrowthStage.Depleted
            };

            stageTimer = 0f;
            SetStageDuration();

            if (currentStage == GrowthStage.Mature && previousStage == GrowthStage.Growing)
            {
                currentAmount = maxResourceAmount * 0.5f;
                if (growthParticles != null)
                {
                    growthParticles.Play();
                }
            }

            OnGrowthStageChanged?.Invoke(this, currentStage);
            UpdateVisuals();
        }
        #endregion

        #region Regeneration
        private void UpdateRegeneration()
        {
            if (currentStage != GrowthStage.Depleted) return;
            if (ResourceManager.Instance == null) return;

            float growthMultiplier = ResourceManager.Instance.CurrentGrowthMultiplier;
            stageTimer += Time.deltaTime * growthMultiplier;

            if (stageTimer >= seedDuration * 2f)
            {
                // Regenerate from depleted
                currentStage = GrowthStage.Seed;
                stageTimer = 0f;
                SetStageDuration();
                UpdateVisuals();
            }
        }
        #endregion

        #region Harvesting
        public bool CanHarvest()
        {
            return IsMature && currentAmount >= harvestAmount;
        }

        public bool Harvest(float amount)
        {
            if (!CanHarvest()) return false;

            float harvested = Mathf.Min(amount, currentAmount);
            currentAmount -= harvested;

            if (harvestParticles != null)
            {
                harvestParticles.Play();
            }

            OnHarvested?.Invoke(this);

            if (currentAmount <= 0)
            {
                Deplete();
            }

            UpdateVisuals();
            return true;
        }

        public float HarvestAll()
        {
            if (!IsMature) return 0f;

            float harvested = currentAmount;
            currentAmount = 0f;
            
            if (harvestParticles != null)
            {
                harvestParticles.Play();
            }

            OnHarvested?.Invoke(this);
            Deplete();

            return harvested;
        }

        private void Deplete()
        {
            currentStage = GrowthStage.Depleted;
            stageTimer = 0f;
            
            if (matureGlowParticles != null)
            {
                matureGlowParticles.Stop();
            }

            OnDepleted?.Invoke(this);
            UpdateVisuals();
        }
        #endregion

        #region Visuals
        private void UpdateVisuals()
        {
            UpdateSprite();
            UpdateScale();
            UpdateColor();
            UpdateParticles();
        }

        private void UpdateSprite()
        {
            if (spriteRenderer == null || growthStageSprites == null) return;
            
            int index = (int)currentStage;
            if (index < growthStageSprites.Length && growthStageSprites[index] != null)
            {
                spriteRenderer.sprite = growthStageSprites[index];
            }
        }

        private void UpdateScale()
        {
            float targetScale = currentStage switch
            {
                GrowthStage.Seed => minScale,
                GrowthStage.Sprout => Mathf.Lerp(minScale, maxScale * 0.4f, growthProgress),
                GrowthStage.Growing => Mathf.Lerp(maxScale * 0.4f, maxScale * 0.8f, growthProgress),
                GrowthStage.Mature => maxScale,
                GrowthStage.Flowering => maxScale * 1.1f,
                GrowthStage.Depleted => maxScale * 0.5f,
                _ => maxScale
            };

            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.deltaTime * 2f);
        }

        private void UpdateColor()
        {
            if (spriteRenderer == null || growthStageColors == null) return;

            int index = (int)currentStage;
            if (index < growthStageColors.Length)
            {
                Color targetColor = growthStageColors[index];
                
                if (currentStage == GrowthStage.Depleted)
                {
                    targetColor.a = 0.5f;
                }
                
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * 2f);
            }
        }

        private void UpdateParticles()
        {
            if (matureGlowParticles != null)
            {
                if ((currentStage == GrowthStage.Mature || currentStage == GrowthStage.Flowering) && !matureGlowParticles.isPlaying)
                {
                    matureGlowParticles.Play();
                }
                else if (currentStage != GrowthStage.Mature && currentStage != GrowthStage.Flowering && matureGlowParticles.isPlaying)
                {
                    matureGlowParticles.Stop();
                }
            }
        }
        #endregion

        #region Environment Interaction
        public void ApplyEnvironmentEffect(float effect)
        {
            // Positive effect speeds growth, negative slows it
            baseGrowthRate = Mathf.Max(0.01f, baseGrowthRate + effect);
        }

        public float GetNutrientValue()
        {
            return resourceType switch
            {
                ResourceType.Mote => 10f,
                ResourceType.Flower => 20f,
                ResourceType.Fruit => 30f,
                ResourceType.Mushroom => 25f,
                ResourceType.Crystal => 40f,
                ResourceType.Nectar => 50f,
                ResourceType.Spore => 15f,
                _ => 10f
            };
        }
        #endregion
    }
}