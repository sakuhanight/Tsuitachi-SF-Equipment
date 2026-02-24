using System;
using UdonSharp;
using UnityEngine;
using VRC.Udon;
using SaccFlightAndVehicles;
using SFAdvEquipment.Utility;

namespace SFAdvEquipment.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_InstrumentsAnimationDriver : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public Animator instrumentsAnimator;

        public float vacuumPowerResponse = 1.0f;
        public GameObject batteryBus;
        public float batteryVoltageResponse = 1.0f;
        public float magneticDeclination;
        public float smoothedVelocityResponse = 0.25f;

        [Header("ADI")]
        public bool hasADI = true;
        public bool adiElectric;
        public float maxPitch = 30;
        public string pitchFloatParameter = "pitch";
        public string rollFloatParameter = "roll";

        [Header("HI")]
        public bool hasHI = true;
        public bool hiElectric;
        public string headingFloatParameter = "heading";

        [Header("ASI")]
        public bool hasASI = true;
        public float maxAirspeed = 180.0f;
        public float asiResponse = 0.25f;
        public string airspeedFloatParameter = "airspeed";

        [Header("Altimeter")]
        public bool hasAltimeter = true;
        public float maxAltitude = 20000;
        public float altimeterResponse = 0.25f;
        public string altitudeFloatParameter = "altitude";

        [Header("TC")]
        public bool hasTC = true;
        public bool tcElectric = true;
        public float maxTurn = 360.0f / 60.0f * 2.0f;
        public float turnResponse = 1.0f;
        public string turnRateFloatParameter = "turnrate";

        [Header("SI")]
        public bool hasSI = true;
        public float maxSlip = 12.0f;
        public float slipResponse = 0.2f;
        public string slipAngleFloatParameter = "slipangle";

        [Header("VSI")]
        public bool hasVSI = true;
        public float maxVerticalSpeed = 2000;
        public float vsiResponse = 0.25f;
        public string verticalSpeedFloatParameter = "vs";

        [Header("Magnetic Compass")]
        public bool hasMagneticCompass = true;
        public float compassResponse = 0.5f;
        public string magneticCompassFloatParameter = "compass";

        [Header("Clock")]
        public bool hasClock = true;
        public bool localTime;
        public string clockTimeParameter = "clocktime";

        [System.NonSerialized] public SaccEntity EntityControl;

        private Rigidbody vehicleRigidbody;
        private bool vacuum;
        private bool initialized;
        private float vacuumPower;
        private float batteryVoltage;
        private Vector3 prevPosition;
        private float turnRate;
        private float slipAngle;
        private Vector3 position;
        private float deltaTime;
        private float roll;
        private float heading;
        private Vector3 velocity;
        private Vector3 acceleration;
        private Vector3 smoothedVelocity;
        private float prevRoll;
        private float prevHeading;
        private Vector3 prevVelocity;
        private Vector3 forward;
        private Vector3 up;
        private float compassHeading;

        private bool Battery => !batteryBus || batteryBus.activeInHierarchy;

        private bool _inVehicle;
        private bool InVehicle
        {
            get => _inVehicle;
            set
            {
                _inVehicle = value;
                if (value)
                {
                    vacuum = (bool)SAVControl.GetProgramVariable("EngineOn");
                    vacuumPower = vacuum ? 1.0f : 0.0f;
                    batteryVoltage = Battery ? 1.0f : 0.0f;
                    gameObject.SetActive(true);
                }
            }
        }

        public void SFEXT_L_EntityStart()
        {
            vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
            if (!instrumentsAnimator) instrumentsAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");

            var navaidDatabaseObj = GameObject.Find("NavaidDatabase");
            if (navaidDatabaseObj)
            {
                var udon = (UdonBehaviour)navaidDatabaseObj.GetComponent(typeof(UdonBehaviour));
                if (udon) magneticDeclination = (float)udon.GetProgramVariable("magneticDeclination");
            }

            vacuum = (bool)SAVControl.GetProgramVariable("EngineOn");
            initialized = true;
        }

        public void SFEXT_G_EngineOn() { vacuum = true; }
        public void SFEXT_G_EngineOff() { vacuum = false; }

        public void SFEXT_O_PilotEnter() { InVehicle = true; }
        public void SFEXT_O_PilotExit() { InVehicle = false; }
        public void SFEXT_P_PassengerEnter() { InVehicle = true; }
        public void SFEXT_P_PassengerExit() { InVehicle = false; }

        public override void PostLateUpdate()
        {
            if (!initialized) return;

            forward = transform.forward;
            up = transform.up;

            position = transform.position;
            deltaTime = Time.deltaTime;
            roll = Vector3.SignedAngle(up, Vector3.ProjectOnPlane(Vector3.up, forward).normalized, forward);
            heading = transform.eulerAngles.y;
            velocity = (position - prevPosition) / deltaTime;
            acceleration = (velocity - prevVelocity) / deltaTime;

            smoothedVelocity = Vector3.Lerp(smoothedVelocity, velocity, deltaTime * smoothedVelocityResponse);

            var batteryVoltageTarget = Battery ? 1.0f : 0.0f;
            var batteryVoltageUpdate = !Mathf.Approximately(batteryVoltageTarget, batteryVoltage);
            if (batteryVoltageUpdate) batteryVoltage = Mathf.MoveTowards(batteryVoltage, batteryVoltageTarget, deltaTime * batteryVoltageResponse);

            var vacuumPowerTarget = vacuum ? 1.0f : 0.0f;
            var vacuumPowerUpdate = !Mathf.Approximately(vacuumPower, vacuumPowerTarget);
            if (vacuumPowerUpdate) vacuumPower = Mathf.Lerp(vacuumPower, vacuumPowerTarget, deltaTime * vacuumPowerResponse);

            if (hasADI) ADI_Update(adiElectric ? batteryVoltage : vacuumPower);
            if (hasHI) HI_Update(hiElectric ? batteryVoltage : vacuumPower);
            if (hasASI) ASI_Update();
            if (hasAltimeter) Altimeter_Update();
            if (hasTC) TC_Update(tcElectric ? batteryVoltage : vacuumPower);
            if (hasSI) SI_Update();
            if (hasVSI) VSI_Update();
            if (hasMagneticCompass) MC_Update();
            if (hasClock) Clock_Update();

            prevPosition = position;
            prevRoll = roll;
            prevHeading = heading;
            prevVelocity = velocity;

            if (!(InVehicle || vacuumPowerUpdate || batteryVoltageUpdate))
            {
                gameObject.SetActive(false);
            }
        }

        private void ADI_Update(float power)
        {
            var pitch = Mathf.DeltaAngle(vehicleRigidbody.transform.localEulerAngles.x, 0);
            instrumentsAnimator.SetFloat(pitchFloatParameter, SFAEUtil.Remap01(pitch, -maxPitch, maxPitch) * power);
            instrumentsAnimator.SetFloat(rollFloatParameter, SFAEUtil.Remap01(Mathf.Lerp(30.0f, roll, power), -180.0f, 180.0f));
        }

        private void HI_Update(float power)
        {
            var magneticHeading = (heading + magneticDeclination + 360) % 360;
            instrumentsAnimator.SetFloat(headingFloatParameter, Mathf.Lerp(33.0f, magneticHeading, power) / 360.0f);
        }

        private void ASI_Update()
        {
            var windGustiness = (float)SAVControl.GetProgramVariable("WindGustiness");
            var windTurbulenceScale = (float)SAVControl.GetProgramVariable("WindTurbulanceScale");
            var wind = (Vector3)SAVControl.GetProgramVariable("Wind");
            var windGustStrength = (float)SAVControl.GetProgramVariable("WindGustStrength");
            var atmosphere = (float)SAVControl.GetProgramVariable("Atmosphere");

            var timeGustiness = Time.time * windGustiness;
            var gustx = timeGustiness + (position.x * windTurbulenceScale);
            var gustz = timeGustiness + (position.z * windTurbulenceScale);
            var finalWind = (wind + Vector3.Normalize(new Vector3(
                Mathf.PerlinNoise(gustx + 9000, gustz) - .5f,
                0,
                Mathf.PerlinNoise(gustx, gustz + 9999) - .5f)) * windGustStrength) * atmosphere;

            var airspeed = Mathf.Max(Vector3.Dot(smoothedVelocity - finalWind, forward), 0);
            instrumentsAnimator.SetFloat(airspeedFloatParameter, SFAEUtil.ToKnots(airspeed) / maxAirspeed);
        }

        private void Altimeter_Update()
        {
            var seaLevel = (float)SAVControl.GetProgramVariable("SeaLevel");
            var altitude = SFAEUtil.ToFeet(position.y - seaLevel);
            instrumentsAnimator.SetFloat(altitudeFloatParameter, Mathf.Clamp01(altitude / maxAltitude));
        }

        private void TC_Update(float power)
        {
            turnRate = Mathf.Lerp(turnRate, (Mathf.DeltaAngle(heading, prevHeading) + Mathf.DeltaAngle(roll, prevRoll) * 0.5f) / deltaTime, deltaTime * turnResponse);
            instrumentsAnimator.SetFloat(turnRateFloatParameter, SFAEUtil.Remap01(turnRate, -maxTurn, maxTurn) * power);
        }

        private void SI_Update()
        {
            slipAngle = Mathf.Lerp(slipAngle, Mathf.Clamp(Vector3.SignedAngle(-up, Vector3.ProjectOnPlane(Physics.gravity - acceleration, forward), forward), -maxSlip, maxSlip), deltaTime * slipResponse);
            instrumentsAnimator.SetFloat(slipAngleFloatParameter, SFAEUtil.Remap01(slipAngle, -maxSlip, maxSlip));
        }

        private void VSI_Update()
        {
            var verticalSpeed = SFAEUtil.ToFeet(smoothedVelocity.y) * 60;
            instrumentsAnimator.SetFloat(verticalSpeedFloatParameter, SFAEUtil.Remap01(verticalSpeed, -maxVerticalSpeed, maxVerticalSpeed));
        }

        private void MC_Update()
        {
            compassHeading = (Mathf.LerpAngle(compassHeading, heading + magneticDeclination, deltaTime * compassResponse) + 360.0f) % 360.0f;
            instrumentsAnimator.SetFloat(magneticCompassFloatParameter, compassHeading / 360.0f);
        }

        private const int SecondsOfDay = 60 * 60 * 24;
        private void Clock_Update()
        {
            var time = (float)Math.Floor((localTime ? DateTime.Now : DateTime.UtcNow).TimeOfDay.TotalSeconds) / SecondsOfDay;
            instrumentsAnimator.SetFloat(clockTimeParameter, time);
        }
    }
}
