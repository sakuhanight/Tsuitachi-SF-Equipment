# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

**Tsuitachi-SF-Equipment (TSFE)** is a Unity package providing advanced equipment systems for SaccFlightAndVehicles 1.8 (SFV). It implements realistic aircraft systems including flaps, landing gear, engines, avionics, and cockpit instruments for VRChat worlds.

- **Package name**: `net.tsuitachi.sf-equipment`
- **Unity version**: 2022.3+
- **Dependencies**: VRChat Worlds SDK 3.7.0+, SaccFlightAndVehicles 1.8.0+, UdonSharp 1.x
- **Namespace**: `TSFE` (formerly `SFAdvEquipment` in design docs)
- **License**: MIT

## Project Structure

```
Runtime/
  ├── Accessories/     # Pickup objects (chocks, etc.)
  ├── Avionics/        # GPWS, aural warnings
  ├── DFUNC/           # Dial functions (VR/desktop controls)
  ├── SFEXT/           # SaccEntity extensions (engines, gear, APU, instruments)
  └── Utility/         # TSFEUtil - shared math/helper functions

Editor/
  └── (Editor scripts for custom inspectors)

docs/
  ├── detailed-design.md        # Complete technical specification
  ├── implementation-plan.md    # Migration roadmap
  └── investigation-report.md   # Original analysis
```

## Architecture

### Component Categories

1. **DFUNC** (Dial Functions): VR/desktop interactive controls
   - Derive from `UdonSharpBehaviour` directly (not from a base class)
   - Auto-injected fields: `EntityControl`, `LeftDial`, `DialPosition`
   - Must implement: `DFUNC_Selected()`, `DFUNC_Deselected()`, `DFUNC_LeftDial()`, `DFUNC_RightDial()`
   - Handle VR trigger input manually using `Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger")` or `TSFEUtil.IsTriggerPressed(LeftDial)`

2. **SFEXT** (SaccEntity Extensions): Systems attached to vehicles
   - Receive lifecycle events: `SFEXT_L_EntityStart`, `SFEXT_O_PilotEnter`, `SFEXT_G_Explode`, etc.
   - Access SaccAirVehicle via `SAVControl.GetProgramVariable("FieldName")`

3. **SFEXTP** (Passenger Extensions): Passenger-specific systems
   - Receive: `SFEXTP_O_UserEnter`, `SFEXTP_G_PilotEnter`, etc.
   - For features like seat adjusters and control transfers

### Key Patterns

**SaccAirVehicle Data Access:**
```csharp
// Reading
float airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed");
bool engineOn = (bool)SAVControl.GetProgramVariable("EngineOn");

// Writing (accumulative)
float currentDrag = (float)SAVControl.GetProgramVariable("ExtraDrag");
SAVControl.SetProgramVariable("ExtraDrag", currentDrag + deltaDrag);
```

**Common SAVControl Fields:**
- Physics: `ExtraDrag`, `ExtraLift`, `AirSpeed`, `AirVel`, `Atmosphere`, `VehicleRigidbody`
- Engine: `EngineOn`, `ThrottleStrength`, `EngineOutput`, `Fuel`, `FullFuel`
- State: `Taxiing`, `Floating`, `PitchDown`
- Animation: `VehicleAnimator`

**Dial Display Management:**
```csharp
TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, isSelected);
```

**VR Haptics:**
```csharp
TSFEUtil.PlayHaptics(LeftDial, duration, amplitude, frequency);
```

## Core Utilities (TSFEUtil)

Located in `Runtime/Utility/TSFEUtil.cs`. All methods are `static`:

**Unit Conversions:**
- `ToKnots(ms)` / `FromKnots(knots)` - m/s ↔ KIAS
- `ToFeet(meters)` / `FromFeet(feet)` - meters ↔ feet
- Constants: `MS_TO_KNOTS`, `KNOTS_TO_MS`, `METERS_TO_FEET`, `FPM_TO_MS`

**Math:**
- `Remap01(value, min, max)` - Linear remap to 0-1
- `ClampedRemap01(value, min, max)` - Clamped 0-1 remap
- `ClampedRemap(value, oldMin, oldMax, newMin, newMax)`
- `Lerp3(a, b, c, t, tMin, tMid, tMax)` - 3-point lerp
- `Lerp4(...)` - 4-point lerp

**Failure Modeling:**
- `CheckMTBF(deltaTime, mtbf)` - Probabilistic failure check
- `CheckMTBF(deltaTime, mtbf, damageMultiplier)` - With damage scaling

**DFUNC Helpers:**
- `GetTriggerInput(leftDial)` - VR trigger value
- `IsTriggerPressed(leftDial)` - Boolean trigger (>0.75)
- `SetDialFuncon(dialFuncon, dialFunconArray, active)` - Toggle dial displays

## Component Dependencies

**Phase 1 (Core, independent):**
- DFUNC_AdvancedFlaps
- DFUNC_ElevatorTrim
- DFUNC_AdvancedSpeedBrake
- SFEXT_AuxiliaryPowerUnit
- GPWS (works with SFV standard gear/flaps)

**Phase 2 (Engine & Gear systems):**
- SFEXT_AdvancedEngine ← DFUNC_AdvancedThrustReverser, SFEXT_EngineFanDriver, SFEXT_Warning
- SFEXT_AdvancedGear ← DFUNC_AdvancedParkingBrake
- DFUNC_AutoStarter → SFEXT_AuxiliaryPowerUnit, SFEXT_AdvancedEngine
- SFEXT_AdvancedPropellerThrust
- SFEXT_InstrumentsAnimationDriver

**Phase 3 (Avionics & Utilities):**
- AuralWarnings (optionally uses DFUNC_AdvancedFlaps)
- DFUNC_ThrustReverser (standard, non-AdvancedEngine version)
- DFUNC_SeatAdjuster, DFUNCP_IHaveControl
- SFEXT_OutsideOnly, SFEXT_PassengerOnly, SFEXT_SeatsOnly
- SFEXT_BoardingCollider

**Phase 4 (Specialized):**
- DFUNC_AdvancedWaterRudder
- SFEXT_WakeTurbulence, SFEXT_DihedralEffect
- PickupChock

## Key Systems

### DFUNC_AdvancedFlaps
Multi-detent flaps with speed limits, overspeed damage (actuator/wing breakage), MTBF failure modeling, haptic feedback. Exposes `targetAngle`, `speedLimit`, `detentIndex` for GPWS/AuralWarnings integration.

### SFEXT_AdvancedEngine
Turbofan simulation: dual-spool (N1/N2), EGT/ECT temperatures, startup sequence, thrust reverser, fire/meltdown modeling, player ingestion hazards, jet blast particles. 8 synced variables. Execution order: 1000.

### GPWS (Ground Proximity Warning System)
6-mode terrain/altitude warnings: sink rate, terrain pull-up, altitude loss after takeoff, gear/flaps too low. Radio altimeter raycast. Execution order: 1100.

### SFEXT_InstrumentsAnimationDriver
Drives 10 analog instruments via Animator: ADI (attitude), HI (heading), ASI (airspeed), altimeter, turn coordinator, slip indicator, VSI, compass, clock. Supports vacuum/electric/pitot power sources.

## Development Notes

**Sync Modes:**
- Continuous: DFUNC_AdvancedFlaps, DFUNC_ElevatorTrim, DFUNC_AdvancedSpeedBrake, SFEXT_AdvancedEngine, SFEXT_AdvancedGear, SFEXT_AdvancedPropellerThrust
- Manual: DFUNC_AdvancedParkingBrake, DFUNC_AdvancedWaterRudder, SFEXT_AuxiliaryPowerUnit, DFUNC_AutoStarter, DFUNC_ThrustReverser
- NoVariableSync/None: All avionics, utilities, visual-only systems

**Custom Execution Orders:**
- SFEXT_AdvancedEngine: 1000
- DFUNC_AutoStarter: 1000
- GPWS: 1100

**Testing Approach:**
Work in phases (1→2→3→4) to respect dependencies. Test each script individually before integration. Use detailed-design.md for algorithm details.

**Code Style:**
- Use `TSFEUtil` static methods instead of duplicating common logic
- Always support both `Dial_Funcon` (single GameObject) and `Dial_Funcon_Array` (GameObject[])
- Minimize `GetProgramVariable` calls in tight loops (cache values)
- Use `FieldChangeCallback` for synced variables that trigger side effects

## Migrating from EsnyaSFAddons

This package is a successor to EsnyaSFAddons, adapted for SFV 1.8:
- Removed `DFUNC_Base` inheritance → manual VR trigger handling
- Removed UdonToolkit attributes (`[SectionHeader]`, `[HideIf]`, etc.) → standard Unity `[Header]`, `[Tooltip]`
- Removed InariUdon/JetBrains.Annotations dependencies
- Changed SaccAirVehicle direct references → `UdonSharpBehaviour SAVControl` + `GetProgramVariable`
- Namespace: `EsnyaSFAddons` → `TSFE`

## Assembly Definitions

**TSFE.Runtime** (`Runtime/TSFE.Runtime.asmdef`):
- References: UdonSharp.Runtime, VRC.Udon, VRC.SDKBase, VRC.Udon.Serialization.OdinSerializer, SaccFlightAndVehicles.Runtime
- Root namespace: `TSFE`
- Auto-referenced: true

**TSFE.Editor** (`Editor/TSFE.Editor.asmdef`):
- References: TSFE.Runtime, UdonSharp.Editor, VRC SDKs
- Platform: Editor only
- Root namespace: `TSFE.Editor`

## Documentation

- `docs/detailed-design.md`: Complete technical specification (1638 lines) - read this for detailed algorithms, field definitions, and state machines
- `docs/implementation-plan.md`: Phase-by-phase migration plan
- `docs/investigation-report.md`: Original analysis of EsnyaSFAddons

When implementing or modifying components, always cross-reference with detailed-design.md for exact specifications.
