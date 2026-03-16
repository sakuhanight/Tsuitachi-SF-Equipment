# Tsuitachi-SF-Equipment Documentation

## Overview

**Tsuitachi-SF-Equipment (TSFE)** is a Unity package providing advanced equipment systems for SaccFlightAndVehicles 1.8 (SFV). It implements realistic aircraft systems including flaps, landing gear, engines, avionics, and cockpit instruments for VRChat worlds.

- **Package name**: `net.tsuitachi.sf-equipment`
- **Unity version**: 2022.3+
- **Dependencies**: VRChat Worlds SDK 3.7.0+, SaccFlightAndVehicles 1.8.0+, UdonSharp 1.x
- **Namespace**: `TSFE`
- **License**: MIT

## Features

### Flight Control Systems
- **DFUNC_AdvancedFlaps** - Multi-detent flaps with speed limits, overspeed damage, MTBF failure modeling
- **DFUNC_ElevatorTrim** - Elevator trim with load factor limiter
- **DFUNC_AdvancedSpeedBrake** - Speed brake with deployment restrictions
- **DFUNC_AdvancedParkingBrake** - Parking brake system
- **DFUNC_AdvancedWaterRudder** - Water rudder for seaplanes

### Propulsion Systems
- **SFEXT_AdvancedEngine** - Turbofan simulation with dual-spool (N1/N2), EGT/ECT temperatures, startup sequence, thrust reverser
- **SFEXT_AdvancedPropellerThrust** - Propeller thrust modeling
- **DFUNC_AdvancedThrustReverser** - Thrust reverser for AdvancedEngine
- **DFUNC_ThrustReverser** - Standard thrust reverser

### Landing Gear
- **SFEXT_AdvancedGear** - Advanced landing gear with damage modeling

### Auxiliary Systems
- **SFEXT_AuxiliaryPowerUnit** - APU with startup/shutdown sequences
- **SFEXT_AutoStarter** - Automated engine startup sequence (Battery → APU → Engines → APU shutdown)
- **SFEXT_EngineToggle** - Engine on/off toggle using AutoStarter

### Avionics
- **GPWS** - Ground Proximity Warning System (6-mode terrain/altitude warnings)
- **AuralWarnings** - Aural warning system with configurable sounds
- **SFEXT_InstrumentsAnimationDriver** - Drives 10 analog instruments (ADI, HI, ASI, altimeter, etc.)

### Visual Effects
- **SFEXT_EngineFanDriver** - Engine fan rotation animation
- **SFEXT_WakeTurbulence** - Wake turbulence generation
- **SFEXT_DihedralEffect** - Dihedral effect simulation

### Utility Systems
- **TSFE_PowerBus** - Electrical power distribution (Battery, APU, Generators)
- **TSFE_BleedAirBus** - Bleed air distribution
- **TSFE_HydraulicBus** - Hydraulic system
- **TSFE_HydraulicPump** - Hydraulic pump
- **TSFE_ParameterTransform** - Transform parameter mapping
- **TSFE_ParameterText** - Text parameter display
- **DFUNC_MethodCaller** - Generic method caller for DFUNC integration

### Utilities
- **SFEXT_BoardingCollider** - Boarding area collider
- **SFEXT_OutsideOnly** - Outside-only objects
- **SFEXT_PassengerOnly** - Passenger-only objects
- **SFEXT_SeatsOnly** - Seats-only objects
- **SFEXT_Warning** - Generic warning system
- **PickupChock** - Wheel chock pickup object

## Documentation Structure

- **[README.md](README.md)** - This file
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture and design patterns
- **[API_REFERENCE.md](API_REFERENCE.md)** - Complete API reference for all components
- **[SETUP_GUIDE.md](SETUP_GUIDE.md)** - Setup and configuration guide
- **[COMPONENTS/](COMPONENTS/)** - Detailed component documentation
  - [DFUNC.md](COMPONENTS/DFUNC.md) - Dial function components
  - [SFEXT.md](COMPONENTS/SFEXT.md) - SaccEntity extensions
  - [Avionics.md](COMPONENTS/Avionics.md) - Avionics systems
  - [Utilities.md](COMPONENTS/Utilities.md) - Utility components

## Quick Start

1. Import SaccFlightAndVehicles 1.8.0+ and VRChat SDK
2. Import Tsuitachi-SF-Equipment package
3. Add desired SFEXT/DFUNC components to your SaccEntity
4. Configure component parameters in Unity Inspector
5. See [SETUP_GUIDE.md](SETUP_GUIDE.md) for detailed setup instructions

## Migration from EsnyaSFAddons

This package is a successor to EsnyaSFAddons, adapted for SFV 1.8:
- Removed `DFUNC_Base` inheritance → manual VR trigger handling
- Removed UdonToolkit attributes → standard Unity attributes
- Changed SaccAirVehicle direct references → `UdonSharpBehaviour SAVControl` + `GetProgramVariable`
- Namespace: `EsnyaSFAddons` → `TSFE`

## Support

For issues and feature requests, please use the GitHub issue tracker.

## License

MIT License - see LICENSE file for details
