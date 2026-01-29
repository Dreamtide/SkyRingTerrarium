# Phase 3: Living World Systems

## Overview

The Living World Systems make the Sky Ring Terrarium feel alive - the world simulates continuously whether the player watches or not. This creates an immersive ecosystem where time, weather, creatures, and resources interact dynamically.

## Architecture

```
âââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
â                    LivingWorldBootstrap                          â
â  (Initializes all systems in correct order)                      â
âââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
                              â
        âââââââââââââââââââââââ¼ââââââââââââââââââââââ
        â¼                     â¼                     â¼
âââââââââââââââââ   âââââââââââââââââââ   âââââââââââââââââââ
âWorldTimeManagerâ   â  WeatherSystem  â   â StarFieldSystem â
â - Day/Night   â   â - Weather statesâ   â - Night stars   â
â - Seasons     â   â - Wind          â   â - Aurora        â
â - Years       â   â - Transitions   â   â - Twinkle       â
âââââââââââââââââ   âââââââââââââââââââ   âââââââââââââââââââ
        â                     â
        ââââââââââââ¬âââââââââââ
                   â¼
         âââââââââââââââââââ
         âTerrariumSimulatorâ
         â - Temperature   â
         â - Humidity      â
         â - State Hub     â
         âââââââââââââââââââ
                   â
        ââââââââââââ¼âââââââââââ
        â¼          â¼          â¼
ââââââââââââââââ ââââââââââââââââ âââââââââââââââââââ
âEcosystemMgr  â âResourceMgr   â âWorldEventManagerâ
â - Creatures  â â - Nodes      â â - Meteor shower â
â - Population â â - Growth     â â - Aurora waves  â
â - Migration  â â - Blooms     â â - Migrations    â
ââââââââââââââââ ââââââââââââââââ âââââââââââââââââââ
        â                â
        â¼                â¼
ââââââââââââââââ ââââââââââââââââ
â CreatureAI   â â ResourceNode â
â - Behaviors  â â - Growth     â
â - Needs      â â - Stages     â
â - Movement   â â - Harvest    â
ââââââââââââââââ ââââââââââââââââ
                   â
                   â¼
         ââââââââââââââââââââââââ
         âOfflineProgressionMgr â
         â - Save/Load          â
         â - Time simulation    â
         â - Population sim     â
         ââââââââââââââââââââââââ
```

## Systems

### 1. WorldTimeManager

Manages time progression with configurable day length.

**Configuration:**
- `realMinutesPerGameDay`: Real minutes for one game day (default: 10)
- `daysPerSeason`: Game days per season (default: 7)

**Time of Day Phases:**
- **Dawn** (0.2-0.3): Golden hour, creatures wake
- **Day** (0.3-0.7): Full brightness, peak activity
- **Dusk** (0.7-0.85): Purple hues, creatures settle
- **Night** (0.85-0.2): Deep blue, nocturnal activity

**Seasons:**
- Spring, Summer, Autumn, Winter
- Each affects day length, colors, creature behavior

**Events:**
```csharp
WorldTimeManager.OnTimeOfDayChanged += (float normalized) => { };
WorldTimeManager.OnTimeOfDayPhaseChanged += (TimeOfDay phase) => { };
WorldTimeManager.OnSeasonChanged += (Season season) => { };
WorldTimeManager.OnDayChanged += (int day) => { };
```

### 2. WeatherSystem

Manages weather states with gradual transitions.

**Weather States:**
- **Clear**: Normal conditions, good visibility
- **Windy**: Strong wind, affects particles and thrust
- **Stormy**: Rain, lightning, reduced visibility
- **Calm**: No wind, peaceful
- **Misty**: Low visibility, humid

**Features:**
- Gradual transitions between states
- Wind direction and strength
- Lightning strikes during storms
- Season-weighted weather probabilities

**Events:**
```csharp
WeatherSystem.OnWeatherChanged += (WeatherState state) => { };
WeatherSystem.OnWeatherTransition += (from, to, progress) => { };
WeatherSystem.OnLightningStrike += () => { };
```

### 3. StarFieldSystem

Manages the night sky.

**Features:**
- Procedurally generated stars
- Smooth fade in/out at dusk/dawn
- Twinkling effect
- Aurora borealis during special events

### 4. EcosystemManager

Manages creature populations and dynamics.

**Population System:**
- Carrying capacity limits
- Birth/death rates
- Predator-prey dynamics
- Auto-spawning to maintain balance

**Food Chain:**
```
Producers (Plants/Motes) â Herbivores â Predators
```

**Migration:**
- Seasonal migrations
- Weather-triggered movement
- Configurable waypoints

### 5. CreatureAI

Individual creature behavior system.

**Behavior States:**
- **Idle**: Resting, minimal activity
- **Wandering**: Exploring the environment
- **SeekingFood**: Hunting for resources
- **Fleeing**: Escaping predators
- **Resting**: Recovering energy
- **Reproducing**: Finding mate, spawning offspring
- **Migrating**: Moving with the herd

**Activity Patterns:**
- **Diurnal**: Active during day
- **Nocturnal**: Active during night
- **Crepuscular**: Active at dawn/dusk
- **Cathemeral**: Active any time

**Needs System:**
- Health, Energy, Hunger
- Hunger increases over time
- Energy depletes with activity
- Reproduction requires energy threshold

### 6. ResourceManager

Manages resource spawning and growth.

**Resource Types:**
- Mote (basic energy)
- Flower (spring-dominant)
- Fruit (summer-dominant)
- Mushroom (autumn-dominant)
- Crystal (winter-dominant)
- Nectar (rare, high energy)
- Spore (reproducing)

**Growth Modifiers:**
- Time of day affects growth rate
- Seasons affect spawn rates
- Weather affects growth speed
- Bloom events multiply everything

### 7. ResourceNode

Individual resource with growth stages.

**Growth Stages:**
1. Seed â Sprout â Growing â Mature â Flowering â Depleted
2. Depleted resources regenerate over time

**Visual Feedback:**
- Scale changes with growth
- Color changes per stage
- Particle effects at maturity

### 8. WorldEventManager

Special world events system.

**Event Types:**
- **Meteor Shower**: Night event, drops special resources
- **Aurora Wave**: Visual spectacle, boosts creatures
- **Migration**: Mass creature movement
- **Bloom**: Resource explosion
- **Solar Flare**: Temperature spike
- **Cosmic Drift**: Rare visual event
- **Harmonic Resonance**: Ultra-rare phenomenon

**Triggers:**
- Random chance based on conditions
- Season-specific events
- Weather-triggered events
- Time-of-day requirements

### 9. OfflineProgressionManager

Simulates world when player is away.

**Features:**
- Saves world state on exit/pause
- Calculates elapsed real time
- Simulates time, weather, population
- Caps maximum offline time (24h default)

**Simulation:**
- Time progression
- Weather changes
- Population dynamics (births/deaths)
- Resource growth/depletion

## Integration

### Adding to Scene

1. Create empty GameObject "LivingWorld"
2. Add `LivingWorldBootstrap` component
3. Systems auto-create if prefabs not assigned

### Listening to World State

```csharp
void Start()
{
    TerrariumSimulator.OnStateChanged += OnWorldStateChanged;
}

void OnWorldStateChanged(TerrariumState state)
{
    Debug.Log($"Temp: {state.temperature}Â°C, Season: {state.season}");
    Debug.Log($"Population: {state.ecosystemStats.totalPopulation}");
}
```

### Creating Custom Creatures

1. Create prefab with `CreatureAI` component
2. Configure creature type, activity pattern
3. Add to `EcosystemManager.spawnableCreatures`

### Creating Custom Resources

1. Create prefab with `ResourceNode` component
2. Configure growth stages, sprites
3. Add to `ResourceManager.resourceTypes`

## Performance Considerations

- Creature updates use simple state machines
- Resources batch visual updates
- Offline simulation is capped at 1000 steps
- Population limits prevent runaway spawning

## Save Data

Stored in PlayerPrefs with key "SkyRingTerrarium_WorldState":
- Time state (day, season, time of day)
- Weather state
- Population counts
- Resource count

## Namespace Structure

```
SkyRingTerrarium.World
âââ WorldTimeManager
âââ WeatherSystem
âââ StarFieldSystem
âââ WorldEventManager
âââ Meteor
âââ OfflineProgressionManager
âââ LivingWorldBootstrap

SkyRingTerrarium.Ecosystem
âââ EcosystemManager
âââ CreatureAI
âââ ResourceManager
âââ ResourceNode

SkyRingTerrarium.Terrarium
âââ TerrariumSimulator
```

## Future Enhancements

- More creature species with unique behaviors
- Creature evolution over generations
- Player-influenced ecosystem changes
- Biome-specific resources
- Achievement system for rare events
