# Sky Ring Terrarium - Gameplay Mathematics Documentation

## Table of Contents
1. [Core Physics](#core-physics)
2. [Movement Calculations](#movement-calculations)
3. [Gravity System](#gravity-system)
4. [Thrust Mechanics](#thrust-mechanics)
5. [Economy Formulas](#economy-formulas)
6. [Balance Parameters](#balance-parameters)

---

## Core Physics

### Gravity
Unity's default gravity: **g = -9.81 m/s²**

Modified gravity formula:
```
effectiveGravity = baseGravity * gravityStrength * gravityDirection
```

Where:
- `baseGravity` = 9.81 m/s²
- `gravityStrength` = configurable multiplier (default: 1.0)
- `gravityDirection` = 1 (normal) or -1 (inverted)

### Terminal Velocity (if implemented)
```
terminalVelocity = sqrt((2 * mass * g) / (density * area * dragCoefficient))
```

For gameplay simplicity, we cap maximum fall speed at:
```
maxFallSpeed = 25 m/s (configurable)
```

---

## Movement Calculations

### Horizontal Movement
```
targetSpeed = inputX * (isRunning ? runSpeed : walkSpeed) * speedMultiplier
acceleration = (targetSpeed != 0) ? accelerationRate : decelerationRate
controlFactor = isGrounded ? 1.0 : airControlMultiplier

velocity.x += (targetSpeed - velocity.x) * acceleration * controlFactor * deltaTime
```

### Default Values
| Parameter | Value | Unit |
|-----------|-------|------|
| walkSpeed | 5.0 | m/s |
| runSpeed | 8.0 | m/s |
| accelerationRate | 50.0 | m/s² |
| decelerationRate | 40.0 | m/s² |
| airControlMultiplier | 0.6 | ratio |

### Speed Multiplier Range
```
minSpeedMultiplier = 0.25x
maxSpeedMultiplier = 3.0x
speedStep = 0.25x
```

---

## Gravity System

### Gravity Flip Timing
```
flipDuration = 0.3 seconds
flipCooldown = 1.0 second
flipAnimationCurve = EaseInOut(0, 0, 1, 1)
```

### Flip Rotation
```
targetRotation = gravityDirection == -1 ? 180° : 0°
interpolatedRotation = Quaternion.Slerp(startRot, endRot, flipCurve(t))
```

### Gravity Zones (Float Bands)
```
Zone Type       | Gravity Multiplier
----------------|-------------------
Normal          | 1.0
Float Band      | 0.0 (zero-g)
Low Gravity     | 0.3
High Gravity    | 2.0
```

---

## Thrust Mechanics

### Thrust Force Application
```
thrustVector = Vector2.up * thrustForce * gravityDirection * speedMultiplier
ApplyForce(thrustVector, ForceMode2D.Force)
```

### Fuel Consumption
```
fuelConsumed = fuelConsumptionRate * deltaTime
currentFuel = max(0, currentFuel - fuelConsumed)
```

### Fuel Recharge (while grounded)
```
fuelRecharged = fuelRechargeRate * deltaTime
currentFuel = min(maxFuel, currentFuel + fuelRecharged)
```

### Thrust Parameters
| Parameter | Value | Unit |
|-----------|-------|------|
| thrustForce | 15.0 | N |
| maxFuel | 100.0 | units |
| fuelConsumptionRate | 30.0 | units/s |
| fuelRechargeRate | 20.0 | units/s |
| maxThrustDuration | 2.0 | seconds |
| thrustCooldown | 0.5 | seconds |

### Time to Empty (full thrust)
```
timeToEmpty = maxFuel / fuelConsumptionRate
            = 100 / 30
            = 3.33 seconds
```

### Time to Recharge (grounded, from empty)
```
timeToRecharge = maxFuel / fuelRechargeRate
               = 100 / 20
               = 5.0 seconds
```

---

## Jump Mechanics

### Jump Force
```
jumpVelocity = jumpForce * gravityDirection * speedMultiplier
velocity.y = jumpVelocity
```

### Coyote Time
Allows jump within a grace period after leaving ground:
```
canJump = isGrounded || (timeSinceGrounded <= coyoteTime)
coyoteTime = 0.15 seconds
```

### Jump Buffering
Registers jump input before landing:
```
shouldJump = justPressedJump || (timeSinceJumpPressed <= jumpBufferTime)
jumpBufferTime = 0.1 seconds
```

### Jump Height Calculation
```
maxJumpHeight = (jumpForce²) / (2 * gravity)
              = (12²) / (2 * 9.81)
              = 144 / 19.62
              ≈ 7.34 meters
```

### Time to Apex
```
timeToApex = jumpForce / gravity
           = 12 / 9.81
           ≈ 1.22 seconds
```

---

## Economy Formulas

### Starting Currency
```
startingCurrency = 100 units
```

### Gravity Flip Cost
```
flipCost = 10 units per flip
```

### Airtime Earnings
```
airtimeEarnings = airtimeDuration * airtimeCurrencyRate
airtimeCurrencyRate = 1.0 units/second
```

### Combo System
```
comboBonus = flipComboBonus * (comboMultiplier ^ comboCount)
comboBonus = min(comboBonus, flipComboBonus * maxComboMultiplier)

flipComboBonus = 5 units
comboMultiplier = 1.1
maxComboMultiplier = 5.0
```

### Combo Examples
| Combo Count | Multiplier | Bonus |
|-------------|------------|-------|
| 1 | 1.1 | 5.5 |
| 2 | 1.21 | 6.05 |
| 3 | 1.33 | 6.65 |
| 5 | 1.61 | 8.05 |
| 10 | 2.59 | 12.95 |
| 17+ | 5.0 (cap) | 25.0 |

### Currency Pickup Values
| Size | Value |
|------|-------|
| Small | 5 units |
| Medium | 15 units |
| Large | 50 units |

---

## Balance Parameters Summary

### Movement Balance
```yaml
Ground Control:
  walk_speed: 5.0 m/s
  run_speed: 8.0 m/s
  acceleration: 50 m/s²
  deceleration: 40 m/s²

Air Control:
  air_multiplier: 0.6
  max_fall_speed: 25 m/s
```

### Jump/Thrust Balance
```yaml
Jump:
  force: 12 units
  max_height: ~7.3m
  time_to_apex: ~1.2s
  coyote_time: 0.15s
  buffer_time: 0.1s

Thrust:
  force: 15 N
  max_fuel: 100
  consumption: 30/s
  recharge: 20/s (grounded)
  max_duration: 2.0s
  cooldown: 0.5s
```

### Gravity Balance
```yaml
Flip:
  cooldown: 1.0s
  duration: 0.3s
  cost: 10 currency

Zones:
  normal: 1.0x
  float_band: 0.0x
  low_gravity: 0.3x
  high_gravity: 2.0x
```

### Economy Balance
```yaml
Starting: 100 units
Flip Cost: 10 units

Earnings:
  airtime_rate: 1.0/s
  combo_base: 5
  combo_mult: 1.1
  combo_cap: 5.0x

Pickups:
  small: 5
  medium: 15
  large: 50
```

---

## Frame-Rate Independence

All physics calculations use deltaTime for frame-rate independence:

```csharp
// Velocity-based movement
velocity += acceleration * Time.deltaTime;
position += velocity * Time.deltaTime;

// Force-based movement (handled by physics engine)
rigidbody.AddForce(force * Time.fixedDeltaTime, ForceMode2D.Force);
```

### Fixed vs Variable Timestep
- **Physics (FixedUpdate)**: 0.02s (50 FPS physics)
- **Input/Visuals (Update)**: Variable, frame-dependent

---

## Tuning Guidelines

### Making the game EASIER
- Increase coyoteTime (more forgiving jumps)
- Increase airControlMultiplier (better air steering)
- Reduce flipCooldown (more flips available)
- Reduce flipCost (cheaper flips)
- Increase fuelRechargeRate (faster recovery)

### Making the game HARDER
- Reduce coyoteTime
- Reduce airControlMultiplier
- Increase flipCooldown
- Increase flipCost
- Reduce fuelRechargeRate
- Reduce maxFuel

### Adjusting Game Speed
Use the speedMultiplier system:
- 0.25x = Very slow (learning/puzzle)
- 1.0x = Normal gameplay
- 2.0x = Fast/challenge mode
- 3.0x = Expert/speedrun mode

All movement, jump, and thrust values scale with speedMultiplier.
