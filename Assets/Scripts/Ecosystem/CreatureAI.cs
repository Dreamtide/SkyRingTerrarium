using UnityEngine;
using System;
using System.Collections.Generic;

namespace SkyRingTerrarium.Ecosystem
{
    /// <summary>
    /// Base creature AI with behaviors: Wander, Seek food, Flee, Rest, Reproduce.
    /// Creatures respond to time of day, weather, and ecosystem state.
    /// </summary>
    public class CreatureAI : MonoBehaviour
    {
        #region Enums
        public enum CreatureType { Producer, Herbivore, Predator }
        public enum BehaviorState { Idle, Wandering, SeekingFood, Fleeing, Resting, Reproducing, Migrating }
        public enum ActivityPattern { Diurnal, Nocturnal, Crepuscular, Cathemeral }
        #endregion

        #region Events
        public static event Action<CreatureAI> OnCreatureBorn;
        public static event Action<CreatureAI> OnCreatureDied;
        public static event Action<CreatureAI, BehaviorState> OnBehaviorChanged;
        #endregion

        #region Serialized Fields
        [Header("Creature Identity")]
        [SerializeField] private string speciesName = "Unknown";
        [SerializeField] private CreatureType creatureType = CreatureType.Herbivore;
        [SerializeField] private ActivityPattern activityPattern = ActivityPattern.Diurnal;

        [Header("Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float lifespan = 300f;

        [Header("Behavior Thresholds")]
        [SerializeField] private float hungerThreshold = 70f;
        [SerializeField] private float restThreshold = 20f;
        [SerializeField] private float reproduceThreshold = 80f;
        [SerializeField] private float fleeDistance = 5f;
        [SerializeField] private float detectionRange = 8f;

        [Header("Rates")]
        [SerializeField] private float hungerRate = 2f;
        [SerializeField] private float energyDrainRate = 1f;
        [SerializeField] private float restRecoveryRate = 5f;
        [SerializeField] private float foodEnergyGain = 30f;

        [Header("Reproduction")]
        [SerializeField] private float reproductionCooldown = 60f;
        [SerializeField] private float reproductionEnergyRequired = 60f;
        [SerializeField] private GameObject offspringPrefab;

        [Header("Migration")]
        [SerializeField] private bool canMigrate = true;
        [SerializeField] private float migrationSpeedMultiplier = 1.5f;

        [Header("Visuals")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private SpriteRenderer spriteRenderer;
        #endregion

        #region Public Properties
        public string SpeciesName => speciesName;
        public CreatureType Type => creatureType;
        public BehaviorState CurrentBehavior => currentBehavior;
        public float Health => currentHealth;
        public float Energy => currentEnergy;
        public float Hunger => currentHunger;
        public float Age => currentAge;
        public bool IsAlive => isAlive;
        public bool IsActive => isActiveTime;
        #endregion

        #region Private Fields
        private BehaviorState currentBehavior;
        private float currentHealth;
        private float currentEnergy;
        private float currentHunger;
        private float currentAge;
        private bool isAlive = true;
        private bool isActiveTime;
        private float reproductionTimer;
        private Vector2 targetPosition;
        private Transform currentTarget;
        private float stateTimer;
        private float wanderTimer;
        private Rigidbody2D rb;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            currentHealth = maxHealth;
            currentEnergy = maxEnergy;
            currentHunger = 0f;
            currentAge = 0f;
            reproductionTimer = reproductionCooldown;
        }

        private void Start()
        {
            World.WorldTimeManager.OnTimeOfDayPhaseChanged += OnTimePhaseChanged;
            World.WeatherSystem.OnWeatherChanged += OnWeatherChanged;
            EcosystemManager.Instance?.RegisterCreature(this);
            OnCreatureBorn?.Invoke(this);
            UpdateActivityState();
        }

        private void OnDestroy()
        {
            World.WorldTimeManager.OnTimeOfDayPhaseChanged -= OnTimePhaseChanged;
            World.WeatherSystem.OnWeatherChanged -= OnWeatherChanged;
            EcosystemManager.Instance?.UnregisterCreature(this);
        }

        private void Update()
        {
            if (!isAlive) return;

            UpdateAge();
            UpdateNeeds();
            UpdateBehavior();
            UpdateMovement();
            UpdateVisuals();
        }
        #endregion

        #region Need Updates
        private void UpdateAge()
        {
            currentAge += Time.deltaTime;
            if (currentAge >= lifespan)
            {
                Die("old age");
            }
        }

        private void UpdateNeeds()
        {
            float activityMultiplier = isActiveTime ? 1f : 0.3f;
            float weatherMultiplier = GetWeatherMultiplier();

            currentHunger += hungerRate * activityMultiplier * weatherMultiplier * Time.deltaTime;
            currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);

            if (currentBehavior != BehaviorState.Resting)
            {
                currentEnergy -= energyDrainRate * activityMultiplier * Time.deltaTime;
            }
            else
            {
                currentEnergy += restRecoveryRate * Time.deltaTime;
            }
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

            if (currentHunger >= maxHunger)
            {
                currentHealth -= Time.deltaTime * 5f;
            }

            if (currentHealth <= 0)
            {
                Die("starvation");
            }

            reproductionTimer -= Time.deltaTime;
        }

        private float GetWeatherMultiplier()
        {
            if (World.WeatherSystem.Instance == null) return 1f;
            
            return World.WeatherSystem.Instance.CurrentWeather switch
            {
                World.WeatherSystem.WeatherState.Stormy => 1.5f,
                World.WeatherSystem.WeatherState.Calm => 0.7f,
                World.WeatherSystem.WeatherState.Misty => 0.9f,
                _ => 1f
            };
        }
        #endregion

        #region Behavior State Machine
        private void UpdateBehavior()
        {
            if (!isActiveTime && currentBehavior != BehaviorState.Resting && currentBehavior != BehaviorState.Fleeing)
            {
                SetBehavior(BehaviorState.Resting);
                return;
            }

            Transform predator = FindNearbyPredator();
            if (predator != null && creatureType != CreatureType.Predator)
            {
                currentTarget = predator;
                SetBehavior(BehaviorState.Fleeing);
                return;
            }

            if (currentBehavior == BehaviorState.Fleeing && predator == null)
            {
                SetBehavior(BehaviorState.Idle);
            }

            if (currentEnergy < restThreshold && currentBehavior != BehaviorState.Fleeing)
            {
                SetBehavior(BehaviorState.Resting);
                return;
            }

            if (currentHunger > hungerThreshold)
            {
                Transform food = FindFood();
                if (food != null)
                {
                    currentTarget = food;
                    SetBehavior(BehaviorState.SeekingFood);
                    return;
                }
            }

            if (CanReproduce())
            {
                Transform mate = FindMate();
                if (mate != null)
                {
                    currentTarget = mate;
                    SetBehavior(BehaviorState.Reproducing);
                    return;
                }
            }

            if (currentBehavior == BehaviorState.Idle || currentBehavior == BehaviorState.Resting)
            {
                if (currentEnergy > restThreshold * 2f)
                {
                    SetBehavior(BehaviorState.Wandering);
                }
            }
        }

        private void SetBehavior(BehaviorState newBehavior)
        {
            if (newBehavior == currentBehavior) return;
            
            currentBehavior = newBehavior;
            stateTimer = 0f;
            OnBehaviorChanged?.Invoke(this, newBehavior);
        }
        #endregion

        #region Movement
        private void UpdateMovement()
        {
            if (rb == null) return;

            Vector2 direction = Vector2.zero;
            float speed = moveSpeed;

            switch (currentBehavior)
            {
                case BehaviorState.Wandering:
                    direction = GetWanderDirection();
                    break;
                    
                case BehaviorState.SeekingFood:
                case BehaviorState.Reproducing:
                    if (currentTarget != null)
                    {
                        direction = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
                        CheckTargetReached();
                    }
                    else
                    {
                        SetBehavior(BehaviorState.Wandering);
                    }
                    break;
                    
                case BehaviorState.Fleeing:
                    if (currentTarget != null)
                    {
                        direction = ((Vector2)transform.position - (Vector2)currentTarget.position).normalized;
                        speed *= 1.5f;
                    }
                    break;
                    
                case BehaviorState.Migrating:
                    direction = GetMigrationDirection();
                    speed *= migrationSpeedMultiplier;
                    break;
                    
                case BehaviorState.Resting:
                case BehaviorState.Idle:
                    direction = Vector2.zero;
                    break;
            }

            // Apply wind influence
            if (World.WeatherSystem.Instance != null)
            {
                direction += World.WeatherSystem.Instance.GetWindForce(transform.position) * 0.1f;
            }

            rb.velocity = direction * speed;
        }

        private Vector2 GetWanderDirection()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                targetPosition = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * UnityEngine.Random.Range(3f, 8f);
                wanderTimer = UnityEngine.Random.Range(2f, 5f);
            }
            
            Vector2 toTarget = targetPosition - (Vector2)transform.position;
            return toTarget.magnitude > 0.5f ? toTarget.normalized : Vector2.zero;
        }

        private Vector2 GetMigrationDirection()
        {
            if (EcosystemManager.Instance != null)
            {
                return EcosystemManager.Instance.GetMigrationTarget(this) - (Vector2)transform.position;
            }
            return GetWanderDirection();
        }

        private void CheckTargetReached()
        {
            if (currentTarget == null) return;
            
            float distance = Vector2.Distance(transform.position, currentTarget.position);
            
            if (distance < 1f)
            {
                if (currentBehavior == BehaviorState.SeekingFood)
                {
                    ConsumeFood();
                }
                else if (currentBehavior == BehaviorState.Reproducing)
                {
                    TryReproduce();
                }
            }
        }
        #endregion

        #region Detection
        private Transform FindNearbyPredator()
        {
            if (creatureType == CreatureType.Predator) return null;

            var creatures = EcosystemManager.Instance?.GetCreaturesInRange(transform.position, fleeDistance);
            if (creatures == null) return null;

            foreach (var creature in creatures)
            {
                if (creature != this && creature.creatureType == CreatureType.Predator && creature.IsAlive)
                {
                    return creature.transform;
                }
            }
            return null;
        }

        private Transform FindFood()
        {
            if (creatureType == CreatureType.Producer) return null;

            if (creatureType == CreatureType.Herbivore)
            {
                var resource = ResourceManager.Instance?.GetNearestResource(transform.position, detectionRange);
                return resource?.transform;
            }
            else if (creatureType == CreatureType.Predator)
            {
                var creatures = EcosystemManager.Instance?.GetCreaturesInRange(transform.position, detectionRange);
                if (creatures == null) return null;

                foreach (var creature in creatures)
                {
                    if (creature != this && creature.creatureType == CreatureType.Herbivore && creature.IsAlive)
                    {
                        return creature.transform;
                    }
                }
            }
            return null;
        }

        private Transform FindMate()
        {
            var creatures = EcosystemManager.Instance?.GetCreaturesInRange(transform.position, detectionRange);
            if (creatures == null) return null;

            foreach (var creature in creatures)
            {
                if (creature != this && creature.speciesName == speciesName && creature.CanReproduce())
                {
                    return creature.transform;
                }
            }
            return null;
        }
        #endregion

        #region Actions
        private void ConsumeFood()
        {
            if (creatureType == CreatureType.Herbivore)
            {
                var resource = currentTarget?.GetComponent<ResourceNode>();
                if (resource != null && resource.Harvest(10f))
                {
                    currentHunger = Mathf.Max(0, currentHunger - 30f);
                    currentEnergy = Mathf.Min(maxEnergy, currentEnergy + foodEnergyGain);
                }
            }
            else if (creatureType == CreatureType.Predator)
            {
                var prey = currentTarget?.GetComponent<CreatureAI>();
                if (prey != null && prey.IsAlive)
                {
                    prey.TakeDamage(prey.currentHealth);
                    currentHunger = 0f;
                    currentEnergy = maxEnergy;
                }
            }
            
            currentTarget = null;
            SetBehavior(BehaviorState.Wandering);
        }

        public bool CanReproduce()
        {
            return reproductionTimer <= 0 && 
                   currentEnergy >= reproductionEnergyRequired && 
                   currentHunger < hungerThreshold &&
                   isActiveTime;
        }

        private void TryReproduce()
        {
            if (!CanReproduce() || offspringPrefab == null) return;

            var mate = currentTarget?.GetComponent<CreatureAI>();
            if (mate != null && mate.CanReproduce())
            {
                currentEnergy -= reproductionEnergyRequired * 0.5f;
                mate.currentEnergy -= reproductionEnergyRequired * 0.5f;
                
                reproductionTimer = reproductionCooldown;
                mate.reproductionTimer = reproductionCooldown;

                Vector2 spawnPos = (transform.position + mate.transform.position) / 2f;
                spawnPos += UnityEngine.Random.insideUnitCircle * 0.5f;
                
                Instantiate(offspringPrefab, spawnPos, Quaternion.identity);
            }
            
            currentTarget = null;
            SetBehavior(BehaviorState.Wandering);
        }

        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die("predation");
            }
        }

        private void Die(string cause)
        {
            if (!isAlive) return;
            
            isAlive = false;
            OnCreatureDied?.Invoke(this);
            
            if (rb != null) rb.velocity = Vector2.zero;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
            
            Destroy(gameObject, 2f);
        }
        #endregion

        #region Time & Weather Response
        private void OnTimePhaseChanged(World.WorldTimeManager.TimeOfDay phase)
        {
            UpdateActivityState();
        }

        private void UpdateActivityState()
        {
            if (World.WorldTimeManager.Instance == null)
            {
                isActiveTime = true;
                return;
            }

            var phase = World.WorldTimeManager.Instance.CurrentTimeOfDayPhase;
            
            isActiveTime = activityPattern switch
            {
                ActivityPattern.Diurnal => phase == World.WorldTimeManager.TimeOfDay.Day || 
                                          phase == World.WorldTimeManager.TimeOfDay.Dawn,
                ActivityPattern.Nocturnal => phase == World.WorldTimeManager.TimeOfDay.Night || 
                                            phase == World.WorldTimeManager.TimeOfDay.Dusk,
                ActivityPattern.Crepuscular => phase == World.WorldTimeManager.TimeOfDay.Dawn || 
                                              phase == World.WorldTimeManager.TimeOfDay.Dusk,
                ActivityPattern.Cathemeral => true,
                _ => true
            };
        }

        private void OnWeatherChanged(World.WeatherSystem.WeatherState weather)
        {
            if (weather == World.WeatherSystem.WeatherState.Stormy && canMigrate)
            {
                if (UnityEngine.Random.value < 0.3f)
                {
                    SetBehavior(BehaviorState.Migrating);
                }
            }
        }
        #endregion

        #region Visuals
        private void UpdateVisuals()
        {
            if (trailRenderer != null)
            {
                trailRenderer.emitting = currentBehavior != BehaviorState.Resting && 
                                         currentBehavior != BehaviorState.Idle &&
                                         rb != null && rb.velocity.magnitude > 0.1f;
            }

            if (spriteRenderer != null)
            {
                float energyRatio = currentEnergy / maxEnergy;
                float alpha = Mathf.Lerp(0.5f, 1f, energyRatio);
                Color c = spriteRenderer.color;
                c.a = isActiveTime ? alpha : alpha * 0.6f;
                spriteRenderer.color = c;
            }
        }
        #endregion

        #region Migration Support
        public void TriggerMigration(Vector2 target)
        {
            if (!canMigrate) return;
            targetPosition = target;
            SetBehavior(BehaviorState.Migrating);
        }
        #endregion
    }
}