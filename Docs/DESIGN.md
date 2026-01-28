# Sky Ring Terrarium - Design Document

## Overview
Sky Ring Terrarium is a Unity-based simulation that combines orbital physics mechanics with ecosystem simulation. The project features a ring-shaped structure with unique gravity zones and a self-sustaining terrarium environment.

## Core Architecture

### 1. Physics Layer
- **GravitySystem**: Manages gravity direction and magnitude for all physics objects
- **FloatBand**: Zero-gravity zone detection and object behavior
- **OrbitalLoop**: Orbital trajectory calculation and maintenance

### 2. Simulation Layer
- **TerrariumSimulator**: Manages ecosystem variables (temperature, humidity, nutrients)
- **LifeCycleManager**: Handles growth, reproduction, and decay cycles

### 3. Unity Integration
- Custom physics using FixedUpdate for deterministic simulation
- ScriptableObjects for configuration
- Events system for decoupled communication

## Technical Specifications

### Gravity System
```
- Ring Radius: Configurable (default 100 units)
- Gravity Strength: 9.81 m/s² (configurable)
- Gravity Direction: Radially inward toward ring center
- Float Band Height: 10 units (configurable)
```

### Terrarium Parameters
```
- Temperature Range: -40°C to 60°C
- Humidity Range: 0% to 100%
- Update Frequency: 0.1s
- Day/Night Cycle: 24 minutes real-time
```

## Future Enhancements
- Procedural terrain generation
- Weather systems
- Creature AI behaviors
- Multiplayer support
