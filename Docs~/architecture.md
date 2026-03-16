# Architecture

## Overview

TSFE follows a component-based architecture that integrates with SaccFlightAndVehicles (SFV) through extension points. The system is organized into four main categories:

1. **DFUNC** - Dial Functions (VR/desktop interactive controls)
2. **SFEXT** - SaccEntity Extensions (vehicle systems)
3. **Avionics** - Aviation electronics (GPWS, warnings)
4. **Utilities** - Shared helper systems

## Component Categories

### DFUNC (Dial Functions)

**Purpose**: Interactive controls for VR/desktop users attached to vehicle dials.

**Base Pattern**:
- Derive from `UdonSharpBehaviour` directly (no base class)
- Auto-injected fields by SFV:
  - `EntityControl` (SaccEntity)
  - `LeftDial` (bool)
  - `DialPosition` (int)
- Required methods:
  - `DFUNC_Selected()` - Called when dial is selected
  - `DFUNC_Deselected()` - Called when dial is deselected
  - `DFUNC_LeftDial()` - Left dial rotation
  - `DFUNC_RightDial()` - Right dial rotation

**VR Input Pattern**:
```csharp
// Manual trigger handling (no DFUNC_Base)
float trigger = LeftDial
    ? Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger")
    : Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");

bool pressed = trigger > 0.75f;
```

Or use TSFEUtil helper:
```csharp
bool pressed = TSFEUtil.IsTriggerPressed(LeftDial);
```

**Dial Display Pattern**:
```csharp
// Toggle dial display when state changes
TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, isActive);
```

**Implemented DFUNC Components**:
- `DFUNC_AdvancedFlaps` - Multi-detent flaps
- `DFUNC_AdvancedParkingBrake` - Parking brake
- `DFUNC_AdvancedSpeedBrake` - Speed brake
- `DFUNC_AdvancedThrustReverser` - Thrust reverser (for AdvancedEngine)
- `DFUNC_AdvancedWaterRudder` - Water rudder
- `DFUNC_ElevatorTrim` - Elevator trim
- `DFUNC_MethodCaller` - Generic method caller
- `DFUNC_ThrustReverser` - Standard thrust reverser

### SFEXT (SaccEntity Extensions)

**Purpose**: Vehicle systems attached to SaccEntity that receive lifecycle events.

**Lifecycle Events**:
- `SFEXT_L_EntityStart()` - Entity initialization
- `SFEXT_O_PilotEnter()` - Local pilot enters
- `SFEXT_O_PilotExit()` - Local pilot exits
- `SFEXT_P_PassengerEnter()` - Local passenger enters
- `SFEXT_P_PassengerExit()` - Local passenger exits
- `SFEXT_G_Explode()` - Vehicle exploded
- `SFEXT_G_RespawnButton()` - Respawn button pressed
- `SFEXT_O_TakeOwnership()` - Local player takes ownership
- `SFEXT_O_LoseOwnership()` - Local player loses ownership

**SaccAirVehicle Data Access Pattern**:
```csharp
// Reading
float airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed");
bool engineOn = (bool)SAVControl.GetProgramVariable("EngineOn");

// Writing (accumulative for physics)
float currentDrag = (float)SAVControl.GetProgramVariable("ExtraDrag");
SAVControl.SetProgramVariable("ExtraDrag", currentDrag + deltaDrag);
```

**Common SAVControl Fields**:
- **Physics**: `ExtraDrag`, `ExtraLift`, `AirSpeed`, `AirVel`, `Atmosphere`, `VehicleRigidbody`
- **Engine**: `EngineOn`, `ThrottleStrength`, `EngineOutput`, `Fuel`, `FullFuel`
- **State**: `Taxiing`, `Floating`, `PitchDown`
- **Animation**: `VehicleAnimator`

**Implemented SFEXT Components**:
- `SFEXT_AdvancedEngine` - Turbofan simulation
- `SFEXT_AdvancedGear` - Landing gear
- `SFEXT_AdvancedPropellerThrust` - Propeller thrust
- `SFEXT_AuxiliaryPowerUnit` - APU system
- `SFEXT_AutoStarter` - Auto engine startup
- `SFEXT_BoardingCollider` - Boarding area
- `SFEXT_DihedralEffect` - Dihedral effect
- `SFEXT_EngineFanDriver` - Fan rotation
- `SFEXT_EngineToggle` - Engine on/off toggle
- `SFEXT_InstrumentsAnimationDriver` - Instrument driver
- `SFEXT_OutsideOnly` - Outside-only objects
- `SFEXT_PassengerOnly` - Passenger-only objects
- `SFEXT_SeatsOnly` - Seats-only objects
- `SFEXT_WakeTurbulence` - Wake turbulence
- `SFEXT_Warning` - Generic warnings

### Avionics

**Purpose**: Aviation electronics systems (typically no sync, local-only).

**Pattern**:
- Usually `BehaviourSyncMode.None` (local-only)
- Access SFEXT components via public references
- Optional integration with DFUNC components

**Implemented Components**:
- `GPWS` - Ground Proximity Warning System
- `AuralWarnings` - Aural warning sounds

### Utilities

**Purpose**: Shared helper systems and math utilities.

#### TSFEUtil (Static Helper Class)

**Unit Conversions**:
```csharp
float knots = TSFEUtil.ToKnots(metersPerSecond);
float ms = TSFEUtil.FromKnots(knots);
float feet = TSFEUtil.ToFeet(meters);
float meters = TSFEUtil.FromFeet(feet);
```

**Math Helpers**:
```csharp
// Linear remap to 0-1
float normalized = TSFEUtil.Remap01(value, min, max);

// Clamped remap
float clamped = TSFEUtil.ClampedRemap01(value, min, max);

// 3-point lerp
float result = TSFEUtil.Lerp3(a, b, c, t, tMin, tMid, tMax);
```

**Failure Modeling**:
```csharp
// MTBF-based failure check
if (TSFEUtil.CheckMTBF(deltaTime, mtbfHours)) {
    // Component failed
}

// With damage multiplier
if (TSFEUtil.CheckMTBF(deltaTime, mtbfHours, damageMultiplier)) {
    // Accelerated failure
}
```

**DFUNC Helpers**:
```csharp
float trigger = TSFEUtil.GetTriggerInput(leftDial);
bool pressed = TSFEUtil.IsTriggerPressed(leftDial);
TSFEUtil.SetDialFuncon(dialFuncon, dialFunconArray, active);
```

#### System Buses

**TSFE_PowerBus** - Electrical power distribution:
- Battery, APU generator, engine generators
- Power priority system
- Voltage output management

**TSFE_BleedAirBus** - Bleed air distribution:
- APU bleed, engine bleed
- Pressure management

**TSFE_HydraulicBus** - Hydraulic system:
- Multiple hydraulic circuits
- Pressure management
- Pump integration via `TSFE_HydraulicPump`

#### Parameter Mapping

**TSFE_ParameterTransform** - Maps parameters to Transform properties (position, rotation, scale)

**TSFE_ParameterText** - Maps parameters to TextMeshPro text display

## Sync Modes

### Continuous Sync
Components with real-time state synchronization:
- `DFUNC_AdvancedFlaps`
- `DFUNC_ElevatorTrim`
- `DFUNC_AdvancedSpeedBrake`
- `SFEXT_AdvancedEngine`
- `SFEXT_AdvancedGear`
- `SFEXT_AdvancedPropellerThrust`

### Manual Sync
Components with event-based synchronization:
- `DFUNC_AdvancedParkingBrake`
- `DFUNC_AdvancedWaterRudder`
- `SFEXT_AuxiliaryPowerUnit`
- `SFEXT_AutoStarter`
- `DFUNC_ThrustReverser`

### No Sync
Local-only components:
- All Avionics (GPWS, AuralWarnings)
- All Utilities (visual/display only)
- SFEXT_Warning
- DFUNC_MethodCaller

## Execution Order

Custom execution orders for timing-critical components:
- `SFEXT_AdvancedEngine`: **1000** (must run before dependent systems)
- `SFEXT_AutoStarter`: **1000** (coordinates with engine)
- `GPWS`: **1100** (reads engine/flaps state after update)

## Component Dependencies

### Phase 1 (Core, Independent)
- `DFUNC_AdvancedFlaps`
- `DFUNC_ElevatorTrim`
- `DFUNC_AdvancedSpeedBrake`
- `SFEXT_AuxiliaryPowerUnit`
- `GPWS` (works with standard SFV gear/flaps)

### Phase 2 (Engine & Gear)
- `SFEXT_AdvancedEngine` ← `DFUNC_AdvancedThrustReverser`, `SFEXT_EngineFanDriver`, `SFEXT_Warning`
- `SFEXT_AdvancedGear` ← `DFUNC_AdvancedParkingBrake`
- `SFEXT_AutoStarter` → `SFEXT_AuxiliaryPowerUnit`, `SFEXT_AdvancedEngine`
- `SFEXT_EngineToggle` → `SFEXT_AutoStarter`
- `SFEXT_AdvancedPropellerThrust`
- `SFEXT_InstrumentsAnimationDriver`

### Phase 3 (Avionics & Utilities)
- `AuralWarnings` (optionally uses `DFUNC_AdvancedFlaps`)
- `DFUNC_ThrustReverser` (standard, non-AdvancedEngine)
- `DFUNC_MethodCaller`
- `SFEXT_OutsideOnly`, `SFEXT_PassengerOnly`, `SFEXT_SeatsOnly`
- `SFEXT_BoardingCollider`

### Phase 4 (Specialized)
- `DFUNC_AdvancedWaterRudder`
- `SFEXT_WakeTurbulence`
- `SFEXT_DihedralEffect`
- `PickupChock`

## Design Patterns

### State Management
Most components use explicit state enums:
```csharp
public enum EngineState { Off, Starting, Windmilling, Running }
[UdonSynced] public EngineState State;
```

### FieldChangeCallback Pattern
```csharp
[UdonSynced, FieldChangeCallback(nameof(Fuel))]
private bool _fuel;
public bool Fuel
{
    get => _fuel;
    set
    {
        _fuel = value;
        if (value) OnFuelEnabled();
        else OnFuelDisabled();
    }
}
```

### INOP (Inoperable) Pattern
Components support INOP state for realism:
```csharp
public bool IsInoperable; // Set by fire handle, maintenance, etc.

// Check before allowing operations
if (IsInoperable) return;
```

## Testing Framework

**TestScenario** - Defines automated test sequences
**TestScenarioRunner** - Executes test scenarios
**MockSAVControl** - Mock SaccAirVehicle for unit testing

## Migration from EsnyaSFAddons

Key architectural changes:
1. **No DFUNC_Base**: Manual VR trigger handling required
2. **No UdonToolkit**: Use standard Unity `[Header]`, `[Tooltip]`
3. **No InariUdon**: Removed dependency
4. **SAVControl pattern**: `UdonSharpBehaviour` reference + `GetProgramVariable()`
5. **Namespace**: `EsnyaSFAddons` → `TSFE`

## Assembly Definitions

**TSFE.Runtime** (`Runtime/TSFE.Runtime.asmdef`):
- References: UdonSharp.Runtime, VRC.Udon, VRC.SDKBase, VRC.Udon.Serialization.OdinSerializer, SaccFlightAndVehicles.Runtime
- Root namespace: `TSFE`
- Auto-referenced: true

**TSFE.Editor** (`Editor/TSFE.Editor.asmdef`):
- References: TSFE.Runtime, UdonSharp.Editor, VRC SDKs
- Platform: Editor only
- Root namespace: `TSFE.Editor`
