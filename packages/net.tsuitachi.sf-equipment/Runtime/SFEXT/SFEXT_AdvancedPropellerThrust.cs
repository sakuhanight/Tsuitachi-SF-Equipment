using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SFEXT_AdvancedPropellerThrust : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public UdonSharpBehaviour toggleEngine;

        [Header("Specs")]
        [Tooltip("hp")] public float power = 160.0f;
        [Tooltip("m")] public float diameter = 1.9304f;
        [Tooltip("rpm")] public AnimationCurve maxRPMCurve = AnimationCurve.Linear(0.0f, 2700.0f, 20000.0f, 2500.0f);
        [Tooltip("rpm")] public float minRPM = 600;
        [Tooltip("Throttle vs RPM")] public AnimationCurve throttleCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
        public float halfPowerAltitude = 21000.0f;
        [Tooltip("Best mixture control vs altitude")] public AnimationCurve bestMixtureControlCurve = AnimationCurve.Linear(0.0f, 0.8f, 20000.0f, 0.1f);
        public float mixtureErrorCoefficient = 0.3f;
        public float rpmResponse = 1.0f;

        [Header("Startup")]
        public float mixtureCutOffDelay = 1.0f;
        public GameObject batteryBus;

        [Header("Animation")]
        public string rpmFloatParameter = "rpm";
        public float animationMaxRPM = 3500;
        public string oilTempFloatParameter = "oiltemp";
        public float oilTempResponse = 0.1f;

        [Header("Failure")]
        public bool engineStall = true;
        [Tooltip("G")] public float minimumNegativeLoadFactor = -1.72f;
        public float mtbEngineStallNegativeLoad = 10.0f;
        public float mtbEngineStallOverNegativeLoad = 1.0f;

        [Header("Environment")]
        public float airDensity = 1.2249f;

        [Header("Hazard")]
        public bool hazardEnabled = true;
        public float minHazardRange = 1.5f;
        public float maxHazardRange = 3.0f;
        public float hazardKillDelay = 1.0f;
        public Vector3 killedPlayerPosition = new Vector3(0.0f, -10000.0f, 0.0f);
        public AudioSource strikedSound;

        [NonSerialized] public float mixture = 1.0f;
        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(RPM))] private float _rpm;
        public float RPM
        {
            private set
            {
                _rpm = value;
                if (animator) animator.SetFloat(rpmFloatParameter, value / animationMaxRPM);
            }
            get => _rpm;
        }

        [System.NonSerialized] public SaccEntity EntityControl;

        private Rigidbody vehicleRigidbody;
        private Transform vehicleTransform;
        private Animator animator;
        private Vector3 prevVelocity;
        private bool isOwner, engineOn;
        private float seaLevelThrust;
        private float mixtureCutOffTimer;
        private float slip, seaLevelThrustScale, smoothedTargetRPM;
        private float thrust;
        private float oilTemp;
        private bool broken;

        private void UpdatePropeller(float smoothedTarget, float v)
        {
            RPM = smoothedTarget * (1 - 0.1f * slip);
            slip = 1 - 31.5f * v / Mathf.Max(RPM, minRPM);
            seaLevelThrust = 1 / 120.0f * slip * Mathf.Pow(RPM, 2) * seaLevelThrustScale;
        }

        public void SFEXT_L_EntityStart()
        {
            vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
            vehicleTransform = vehicleRigidbody.transform;
            animator = vehicleRigidbody.GetComponent<Animator>();

            SAVControl.SetProgramVariable("ThrottleStrength", 0f);

            var maxRPM = maxRPMCurve.Evaluate(0.0f);
            seaLevelThrustScale = 1.0f;
            RPM = maxRPM;
            for (var i = 0; i < 10; i++) UpdatePropeller(maxRPM, 0);
            var t0 = seaLevelThrust;
            var ts = Mathf.Pow(2.0f * airDensity * Mathf.PI * Mathf.Pow(diameter / 2.0f, 2.0f) * Mathf.Pow(power * 735.499f, 2.0f), 1.0f / 3.0f);
            seaLevelThrustScale = ts / t0;

            SFEXT_G_Reappear();

            isOwner = Networking.IsOwner(EntityControl.gameObject);
        }

        public void SFEXT_O_TakeOwnership() { isOwner = true; }
        public void SFEXT_O_LoseOwnership() { isOwner = false; }

        public void SFEXT_G_EngineStartup()
        {
            if (batteryBus && toggleEngine && !batteryBus.activeInHierarchy && Networking.IsOwner(EntityControl.gameObject))
            {
                toggleEngine.SendCustomNetworkEvent(NetworkEventTarget.All, "EngineStartupCancel");
            }
            else
            {
                SFEXT_G_EngineOn();
            }
        }

        public void SFEXT_G_EngineOn()
        {
            engineOn = true;
            gameObject.SetActive(true);
        }

        public void SFEXT_G_EngineOff()
        {
            engineOn = false;
            mixtureCutOffTimer = 0.0f;
        }

        public void SFEXT_G_Reappear()
        {
            engineOn = false;
            broken = false;
            seaLevelThrust = 0;
            smoothedTargetRPM = 0;
            slip = 0;
            RPM = 0;
            oilTemp = 0.0f;
            gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (isOwner && thrust > 0) vehicleRigidbody.AddForceAtPosition(transform.forward * thrust, transform.position);
        }

        private void Update()
        {
            if (isOwner) OwnerUpdate();

            if (!engineOn && Mathf.Approximately(RPM, 0))
            {
                gameObject.SetActive(false);
                return;
            }

            if (!string.IsNullOrEmpty(oilTempFloatParameter) && animator)
            {
                var throttleInput = (float)SAVControl.GetProgramVariable("ThrottleInput");
                var oilTempTarget = engineOn ? Mathf.Lerp(0.2f, 1.0f, throttleInput) : 0.0f;
                if (!Mathf.Approximately(oilTemp, oilTempTarget))
                {
                    oilTemp = Mathf.MoveTowards(oilTemp, oilTempTarget, Time.deltaTime * oilTempResponse);
                    animator.SetFloat(oilTempFloatParameter, oilTemp);
                }
            }
        }

        private void OwnerUpdate()
        {
            if (Mathf.Approximately(mixture, 0))
            {
                if (mixtureCutOffTimer > mixtureCutOffDelay)
                {
                    mixtureCutOffTimer = 0;
                    EngineOff();
                    return;
                }
                mixtureCutOffTimer += Time.deltaTime * UnityEngine.Random.Range(0.9f, 1.1f);
            }

            var deltaTime = Time.deltaTime;
            var seaLevel = (float)SAVControl.GetProgramVariable("SeaLevel");
            var altitude = TSFEUtil.ToFeet(transform.position.y - seaLevel);
            var throttleInput = (float)SAVControl.GetProgramVariable("ThrottleInput");

            var bestMixtureControl = bestMixtureControlCurve.Evaluate(altitude);
            var mixtureError = Mathf.Abs(mixture - bestMixtureControl);

            var maxRPM = maxRPMCurve.Evaluate(altitude);

            var targetRPM = (engineOn && !broken)
                ? Mathf.Lerp(minRPM, maxRPM, throttleCurve.Evaluate(throttleInput)) / (1.0f + mixtureError * mixtureErrorCoefficient)
                : 0;
            smoothedTargetRPM = Mathf.Lerp(smoothedTargetRPM, targetRPM, deltaTime * rpmResponse);

            var airVel = (Vector3)SAVControl.GetProgramVariable("AirVel");
            UpdatePropeller(smoothedTargetRPM, Vector3.Dot(airVel, transform.forward));

            thrust = seaLevelThrust * Mathf.SmoothStep(1.0f, 0.0f, altitude / (halfPowerAltitude * 2.0f));

            var engineOutput = Mathf.Clamp01(RPM / maxRPM);
            SAVControl.SetProgramVariable("EngineOutput", engineOutput);

            if (Mathf.Approximately(engineOutput, 0.0f) && UnityEngine.Random.value < (1.0f - engineOutput)) EngineOff();

            if (engineStall)
            {
                var velocity = vehicleRigidbody.velocity;
                var acceleration = (velocity - prevVelocity) / deltaTime;
                prevVelocity = velocity;

                var gravity = Physics.gravity;
                var loadFactor = Vector3.Dot(acceleration - gravity, vehicleTransform.up) / gravity.magnitude;
                if (
                    loadFactor < minimumNegativeLoadFactor && UnityEngine.Random.value < Mathf.Abs((loadFactor - minimumNegativeLoadFactor) * deltaTime / mtbEngineStallOverNegativeLoad)
                    || loadFactor < 0 && UnityEngine.Random.value < Mathf.Clamp01(-loadFactor) * deltaTime / mtbEngineStallNegativeLoad
                )
                {
                    EngineOff();
                }
            }
        }

        private void EngineOff()
        {
            if (toggleEngine) toggleEngine.SendCustomEvent("ToggleEngine");
            else SAVControl.SendCustomEvent("SetEngineOff");
        }

        private void PostLateUpdate()
        {
            var localPlayer = Networking.LocalPlayer;

            if (!Utilities.IsValid(localPlayer) || !hazardEnabled || !EntityControl || EntityControl.InVehicle || Mathf.Approximately(RPM, 0)) return;

            var playerPosition = localPlayer.GetPosition();
            var relative = transform.InverseTransformPoint(playerPosition);
            var distance = relative.magnitude;

            if (distance > maxHazardRange) return;

            var normalizedRpm = RPM / maxRPMCurve.Evaluate(0.0f);
            var hazardRange = Mathf.Lerp(minHazardRange, maxHazardRange, normalizedRpm);

            if (distance > hazardRange) return;

            var forceScale = 1 - distance / hazardRange;

            if (relative.z >= 0)
            {
                PlayStrikeSound();
                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PlayerStrike));
                SendCustomEventDelayedSeconds(nameof(_KillPlayer), hazardKillDelay);
                AddPlayerForce(localPlayer, forceScale * thrust * (transform.position - playerPosition).normalized);
            }
            else
            {
                AddPlayerForce(localPlayer, -forceScale * thrust * transform.forward);
            }
        }

        public void PlayerStrike()
        {
            if (!broken)
            {
                broken = true;
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayStrikeSound));
            }
        }

        public void PlayStrikeSound()
        {
            if (strikedSound && !strikedSound.isPlaying) strikedSound.Play();
        }

        private void AddPlayerForce(VRCPlayerApi player, Vector3 force)
        {
            player.SetVelocity(player.GetVelocity() + (force + (player.IsPlayerGrounded() ? Vector3.up * 0.5f : Vector3.zero)) * Time.deltaTime);
        }

        public void _KillPlayer()
        {
            Networking.LocalPlayer.TeleportTo(killedPlayerPosition, Quaternion.identity);
        }
    }
}
