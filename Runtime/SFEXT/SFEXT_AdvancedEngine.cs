using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using SaccFlightAndVehicles;
using TSFE.DFUNC;
using TSFE.Utility;

namespace TSFE.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(1000)]
    public class SFEXT_AdvancedEngine : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public UdonSharpBehaviour soundController;
        public UdonSharpBehaviour brake;
        public DFUNC_AdvancedParkingBrake parkingBrake;
        public SFEXT_AuxiliaryPowerUnit apu;

        [Header("Misc")]
        public float externalTemperature = 15.0f;
        public float randomRange = 0.2f;
        public float wheelWakeUpTorque = 1.0e-36f;

        [System.NonSerialized] public SaccEntity EntityControl;

        #region SFEXT Core
        private bool initialized, isOwner, isPilot, hasPilot, isPassenger;

        public void SFEXT_L_EntityStart()
        {
            Power_Start();
            Sound_Start();
            Fault_Start();
            Effect_Start();
            JetBlast_Start();

            gameObject.SetActive(false);
            initialized = true;
        }

        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit() { hasPilot = false; }
        public void SFEXT_O_PilotEnter() { isOwner = isPilot = true; }
        public void SFEXT_O_PilotExit() { isPilot = false; }
        public void SFEXT_P_PassengerEnter() { isPassenger = true; }
        public void SFEXT_P_PassengerExit() { isPassenger = false; }
        public void SFEXT_O_TakeOwnership() { isOwner = true; }
        public void SFEXT_O_LoseOwnership() { isOwner = false; }
        public void SFEXT_G_Explode() { ResetStatus(); }
        public void SFEXT_G_RespawnButton() { ResetStatus(); }
        public void SFEXT_L_BoardingEnter() { onBoarding = true; }
        public void SFEXT_L_BoardingExit() { onBoarding = false; }

        public void SFEXT_G_TouchDownWater()
        {
            if (isOwner)
            {
                stall = true;
                broken = true;
            }
        }

        private WheelCollider[] wheels;
        private void OnEnable()
        {
            if (wheels == null) wheels = GetComponentInParent<Rigidbody>().GetComponentsInChildren<WheelCollider>(true);
            foreach (var wheel in wheels) wheel.motorTorque += wheelWakeUpTorque;
        }

        private void OnDisable()
        {
            if (wheels != null)
                foreach (var wheel in wheels) wheel.motorTorque -= wheelWakeUpTorque;
        }

        private void FixedUpdate()
        {
            if (!initialized) return;
            if (isOwner) Power_OwnerFixedUpdate();
        }

        private void Update()
        {
            if (!initialized) return;

            var deltaTime = Time.deltaTime;

            if (isOwner)
            {
                Power_OwnerUpdate(deltaTime);
                Fault_OwnerUpdate(deltaTime);
            }

            Power_Update(deltaTime);
            Fault_Update();
            Sound_Update(deltaTime);
            Effect_Update();
            JetBlast_Update();

            var stopped = Mathf.Approximately(n2 + n1, 0) && Mathf.Approximately(egt, externalTemperature);
            if (!hasPilot && stopped)
            {
                if (isOwner) SAVControl.SetProgramVariable("EngineOutput", 0f);
                gameObject.SetActive(false);
            }
        }

        public override void PostLateUpdate()
        {
            PlayerStrike_Update();
        }

        private void ResetStatus()
        {
            Power_Reset();
            Fault_Reset();
        }

        public void _InstantStart()
        {
            starter = false;
            fuel = true;
            n2 = idleN2;
            n1 = idleN1;
        }
        #endregion

        #region Power
        [Header("Power")]
        [Tooltip("[N]")] public float maxThrust = 130408.51f;
        public float thrustCurve = 2.0f;

        [Header("N1")]
        [Tooltip("[rpm]")] public float idleN1 = 879.6f;
        [Tooltip("[rpm]")] public float referenceN1 = 4397;
        [Tooltip("[rpm]")] public float continuousN1 = 4397;
        [Tooltip("[rpm]")] public float takeOffN1 = 4586;
        public float n1Response = 0.1f;
        public float n1DecreaseResponse = 0.08f;
        public float n1StartupResponse = 0.01f;

        [Header("N2")]
        [Tooltip("[rpm]")] public float minN2 = 3433.4f;
        [Tooltip("[rpm]")] public float idleN2 = 8583.5f;
        [Tooltip("[rpm]")] public float referenceN2 = 17167;
        [Tooltip("[rpm]")] public float continuousN2 = 17167;
        [Tooltip("[rpm]")] public float takeOffN2 = 20171;
        public float n2Response = 0.05f;
        public float n2DecreaseResponse = 0.04f;
        public float n2StartupResponse = 0.005f;

        [Header("EGT")]
        [Tooltip("[C]")] public float idleEGT = 725;
        [Tooltip("[C]")] public float continuousEGT = 1013;
        [Tooltip("[C]")] public float takeOffEGT = 1038;
        [Tooltip("[C]")] public float fireEGT = 1812;
        public float egtResponse = 0.02f;

        [Header("ECT")]
        [Tooltip("[C]")] public float idleECT = 196;
        [Tooltip("[C]")] public float continuousECT = 274;
        [Tooltip("[C]")] public float overheatECT = 343;
        [Tooltip("[C]")] public float fireECT = 850;
        public float ectResponse = 0.1f;
        public float ectOverheatResponse = 0.001f;

        [Header("Oil")]
        [Tooltip("[C]")] public float idleOilTemperature = 31;
        [Tooltip("[C]")] public float maxOilTemperature = 140;
        [Tooltip("[C]")] public float takeOffOilTemperature = 155;
        [Tooltip("[hPa]")] public float idleOilPressure = 1200;
        [Tooltip("[hPa]")] public float maxOilPressure = 2000;

        [Header("Starter")]
        public bool autoRelease = true;
        public bool autoFuel;

        [Header("Reverser")]
        public float reverserRatio = 0.5f;
        public float reverserExtractResponse = 0.5f;
        public float reverserRetractResponse = 0.5f;

        [NonSerialized][UdonSynced] public bool reversing, starter, fuel;
        [NonSerialized][UdonSynced] public float n1, n2, egt, ect;
        [NonSerialized] public float throttleInput, normalizedThrust, oilTemperature, oilPressure;
        [NonSerialized] public float reverserPosition;

        private Rigidbody vehicleRigidbody;
        private Animator vehicleAnimator;
        private string gripAxis;

        public void EngageStarter() { starter = true; }
        public void DisengageStarter() { starter = false; }
        public void ToggleStarter() { starter = !starter; }
        public void FuelOn() { fuel = true; }
        public void FuelCutoff() { fuel = false; }
        public void ToggleFuel() { fuel = !fuel; }

        private void Power_Start()
        {
            SAVControl.SetProgramVariable("ThrottleStrength", 0f);
            SAVControl.SetProgramVariable("AccelerationResponse", 0f);

            var switchHands = (bool)SAVControl.GetProgramVariable("SwitchHandsJoyThrottle");
            gripAxis = switchHands ? "Oculus_CrossPlatform_SecondaryHandTrigger" : "Oculus_CrossPlatform_PrimaryHandTrigger";

            vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
            vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");

            Power_Reset();
        }

        private void Power_Reset()
        {
            starter = false;
            fuel = false;
            reversing = false;
            n1 = 0;
            n2 = 0;
            egt = externalTemperature;
            ect = externalTemperature;
            oilTemperature = externalTemperature;
            oilPressure = 1100;

            if (vehicleAnimator)
            {
                vehicleAnimator.SetBool("reverse", false);
                vehicleAnimator.SetFloat("reverser", 0);
            }
        }

        private void Power_OwnerFixedUpdate()
        {
            var thrust = normalizedThrust * maxThrust * Mathf.Lerp(1, -reverserRatio, reverserPosition * 2.0f - 1.0f);
            vehicleRigidbody.AddForceAtPosition(transform.forward * thrust, transform.position, ForceMode.Force);
        }

        private void Power_OwnerUpdate(float deltaTime)
        {
            var reverserInterlocked = reversing && reverserPosition < 0.5f;
            if (reverserInterlocked) SAVControl.SetProgramVariable("ThrottleInput", 0f);

            var throttleOverridden = (int)SAVControl.GetProgramVariable("ThrottleOverridden");
            var throttleOverride = (float)SAVControl.GetProgramVariable("ThrottleOverride");
            var savThrottleInput = (float)SAVControl.GetProgramVariable("ThrottleInput");

            throttleInput = reverserInterlocked ? 0.0f
                : (throttleOverridden > 0 && Input.GetAxis(gripAxis) < 0.75f ? throttleOverride : savThrottleInput);

            var isStarterAvailable = starter && (apu == null || apu.started);
            var isN2Running = fuel && n2 >= minN2 && !stall;
            var savFuel = (float)SAVControl.GetProgramVariable("Fuel");
            var savLowFuel = (float)SAVControl.GetProgramVariable("LowFuel");
            var targetN2 = (isStarterAvailable || isN2Running)
                ? Mathf.Lerp(fuel ? idleN2 : minN2 * 1.1f, takeOffN2, throttleInput) * TSFEUtil.ClampedRemap01(savFuel, 0, savLowFuel)
                : 0.0f;
            n2 = TwoWayMoveTowards(n2, targetN2, deltaTime * continuousN2 * Randomize(), isN2Running ? n2Response : n2StartupResponse, n2DecreaseResponse);

            var targetN1 = TSFEUtil.Lerp3(0, idleN1, takeOffN1, n2, 0, idleN2, takeOffN2);
            n1 = Mathf.MoveTowards(n1, targetN1, deltaTime * n1Response * continuousN1 * Randomize());

            normalizedThrust = Mathf.Clamp01(Mathf.Pow(n1 / takeOffN1, thrustCurve));

            var egtTarget = fire
                ? fireEGT
                : TSFEUtil.Lerp4(externalTemperature, fuel ? idleEGT : externalTemperature, continuousEGT, takeOffEGT, n2, 0, idleN2, continuousN2, takeOffN2);
            egt = Mathf.Lerp(egt, egtTarget, deltaTime * egtResponse * Randomize());

            var ectTarget = TSFEUtil.Lerp4(externalTemperature, idleECT, continuousECT, egt, egt, externalTemperature, idleEGT, continuousEGT, takeOffEGT);
            ect = Mathf.Lerp(ect, ectTarget, deltaTime * (egt <= continuousEGT || fire ? ectResponse : ectOverheatResponse) * Randomize());

            SAVControl.SetProgramVariable("EngineOutput", normalizedThrust);

            if (starter && autoFuel && n2 >= minN2 && !fuel) fuel = true;
            if (starter && autoRelease && fuel && n2 >= minN2 * 1.1f) starter = false;
        }

        private void Power_Update(float deltaTime)
        {
            reverserPosition = TwoWayMoveTowards(reverserPosition, reversing ? 1 : 0, deltaTime, reverserExtractResponse, reverserRetractResponse);
            if (vehicleAnimator)
            {
                vehicleAnimator.SetBool("reverse", reversing);
                vehicleAnimator.SetFloat("reverser", reverserPosition);
            }

            oilTemperature = TSFEUtil.Lerp4(externalTemperature, idleOilTemperature, maxOilTemperature, takeOffOilTemperature, ect, externalTemperature, idleECT, continuousECT, Mathf.Max(egt, continuousEGT));
            oilPressure = TSFEUtil.Lerp3(1013.25f, idleOilPressure, maxOilPressure, n2, 0, idleN2, takeOffN2);
        }
        #endregion

        #region Sound
        [Header("Sounds")]
        public AudioSource idleSound;
        public AudioSource insideSound;
        public AudioSource thrustSound;
        public AudioSource takeOffSound;
        public float soundResponse = 1.0f;

        private float idleVolume, insideVolume, thrustVolume, takeOffVolume;

        private void Sound_Start()
        {
            if (soundController)
            {
                var planeIdle = (AudioSource[])soundController.GetProgramVariable("PlaneIdle");
                var thrust = (AudioSource[])soundController.GetProgramVariable("Thrust");
                var planeInside = (AudioSource)soundController.GetProgramVariable("PlaneInside");
                MuteAudioSources(planeIdle);
                MuteAudioSources(thrust);
                MuteAudioSource(planeInside);
            }

            if (InitializeAudioSource(idleSound)) idleVolume = idleSound.volume;
            if (InitializeAudioSource(insideSound)) insideVolume = insideSound.volume;
            if (InitializeAudioSource(takeOffSound)) takeOffVolume = takeOffSound.volume;
            if (InitializeAudioSource(thrustSound)) thrustVolume = thrustSound.volume;
        }

        private void Sound_Update(float deltaTime)
        {
            var allDoorsClosed = soundController ? (bool)soundController.GetProgramVariable("AllDoorsClosed") : false;
            var isInside = (isPilot || isPassenger) && allDoorsClosed;
            var doppler = isInside ? 1.0f : Mathf.Min(soundController ? (float)soundController.GetProgramVariable("Doppler") : 1.0f, 2.25f);
            var silent = soundController ? (bool)soundController.GetProgramVariable("silent") : false;

            var n2ToIdle = TSFEUtil.Remap01(n2, 0, idleN2);
            var n1ToIdle = TSFEUtil.Remap01(n1, 0, idleN1);
            SetAudioVolumeAndPitch(idleSound, isInside ? 0.0f : n2ToIdle * idleVolume, TSFEUtil.Lerp3(0.0f, 1.0f, 2.7f, n2, 0.0f, idleN2, continuousN2) * doppler, soundResponse * deltaTime);
            SetAudioVolumeAndPitch(insideSound, isInside ? n1ToIdle * insideVolume : 0, TSFEUtil.Lerp3(0.0f, 0.8f, 1.2f, n1, 0, idleN1, takeOffN1), soundResponse * deltaTime);
            SetAudioVolumeAndPitch(thrustSound, n1ToIdle * thrustVolume * TSFEUtil.ClampedRemap01(n1, idleN1, takeOffN1) * (isInside ? 0.09f : 1.0f) * (silent ? 0.0f : 1.0f) * doppler, 1, soundResponse * deltaTime);
            SetAudioVolumeAndPitch(takeOffSound, n1ToIdle * takeOffVolume * TSFEUtil.ClampedRemap01(n1, continuousN1, takeOffN1) * (isInside ? 0.09f : 1.0f) * (silent ? 0.0f : 1.0f) * doppler, 1, soundResponse * deltaTime);
        }
        #endregion

        #region Effect
        [Header("Effects")]
        public ParticleSystem fireEffect;
        public ParticleSystem thrustEffect;
        private float fireStartSpeed, thrustStartSpeed;

        private void Effect_Start()
        {
            if (fireEffect) fireStartSpeed = fireEffect.main.startSpeedMultiplier;
            if (thrustEffect) thrustStartSpeed = thrustEffect.main.startSpeedMultiplier;
        }

        private void Effect_Update()
        {
            SetParticleEmission(fireEffect, fire, fireStartSpeed * Mathf.Max(n2 / takeOffN2, 0.1f));
            SetParticleEmission(thrustEffect, !fire && egt - externalTemperature > 15.0f, thrustStartSpeed * Mathf.Max(n1 / takeOffN1, 0.1f));
        }
        #endregion

        #region Fault
        [Header("Fault")]
        public float mtbFireAtContinuous = 30 * 24 * 60 * 60;
        public float mtbFireAtOverheat = 90;
        public float mtbFireAtFire = 10;
        public float mtbMeltdownOnFire = 90;

        [NonSerialized][UdonSynced] public bool fire;
        [NonSerialized] public bool overheat, stall, broken, dished;

        public void Dish() { dished = true; }

        private void Fault_Start() { Fault_Reset(); }

        private void Fault_Reset()
        {
            fire = false;
            stall = false;
            dished = false;
            broken = false;
        }

        private void Fault_OwnerUpdate(float deltaTime)
        {
            if (!fire && !dished && UnityEngine.Random.value < deltaTime / TSFEUtil.Lerp3(mtbFireAtContinuous, mtbFireAtOverheat, mtbFireAtFire, ect, continuousECT, overheatECT, fireECT))
            {
                fire = true;
            }

            if (fire && dished) fire = false;

            if (ect > fireECT && !stall && UnityEngine.Random.value < deltaTime / mtbMeltdownOnFire)
            {
                broken = true;
            }

            if (broken) stall = true;
        }

        private void Fault_Update()
        {
            overheat = ect > overheatECT;
        }
        #endregion

        #region Player Strike
        [Header("Player Strike")]
        public bool playerStrike = true;
        public float inletOffset = 2.0f;
        public float inletAreaIdleRange = 3.1f;
        public float inletAreaTakeOffRange = 4.2f;
        public float inletAreaAngle = 40.0f;
        public float exhaustAreaIdleRange = 60.0f;
        public float exhaustAreaTakeOffRange = 100.0f;
        public float exhaustAreaExtent = 8.0f;
        public float exhaustAreaAngle = 30.0f;
        public float idlePlayerAcceleration = 100;
        public float takeOffPlayerAcceleration = 1000;
        public float strikeDistance = 3.0f;
        public AudioSource strikeSoundSource;
        public AudioClip strikeSound;
        private bool onBoarding;

        private void PlayerStrike_Update()
        {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer) || !playerStrike || isPilot || isPassenger || onBoarding || Mathf.Approximately(n1, 0)) return;

            var playerPosition = localPlayer.GetPosition();
            var exhaustPlayerPosition = transform.InverseTransformPoint(playerPosition);
            var inletPlayerPosition = exhaustPlayerPosition - Vector3.forward * inletOffset;
            var inletDistance = inletPlayerPosition.magnitude;
            var inletDirection = inletPlayerPosition / inletDistance;

            if (inletDirection.z > 0 && inletDistance < TSFEUtil.Lerp3(0, inletAreaIdleRange, inletAreaTakeOffRange, n1, 0, idleN1, takeOffN1) && Mathf.Abs(Vector3.SignedAngle(Vector3.forward, inletDirection, Vector3.up)) < inletAreaAngle)
            {
                if (inletDistance < strikeDistance)
                {
                    PlayStrikeSound();
                    SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PlayerStrike));
                }
                AddPlayerForce(localPlayer, -transform.TransformDirection(inletDirection) * Mathf.Lerp(idlePlayerAcceleration, takeOffPlayerAcceleration, normalizedThrust));
            }
            else
            {
                var exhaustDistance = exhaustPlayerPosition.magnitude;
                var exhaustDirection = exhaustPlayerPosition / exhaustDistance;

                if (exhaustDirection.z < 0 && Mathf.Abs(exhaustPlayerPosition.x) < exhaustAreaExtent && exhaustDistance < TSFEUtil.Lerp3(0, exhaustAreaIdleRange, exhaustAreaTakeOffRange, n1, 0, idleN1, takeOffN1) && Mathf.Abs(Vector3.SignedAngle(Vector3.back, exhaustDirection, Vector3.up)) < exhaustAreaAngle)
                {
                    AddPlayerForce(localPlayer, -transform.forward * Mathf.Lerp(idlePlayerAcceleration, takeOffPlayerAcceleration, normalizedThrust));
                }

                if (exhaustDirection.z > 0 && reverserPosition > 0.5f && Mathf.Abs(exhaustPlayerPosition.x) < exhaustAreaExtent && exhaustDistance < TSFEUtil.Lerp3(0, exhaustAreaIdleRange, exhaustAreaTakeOffRange, n1, 0, idleN1, takeOffN1) && Mathf.Abs(Vector3.SignedAngle(Vector3.forward, exhaustDirection, Vector3.up)) < exhaustAreaAngle)
                {
                    AddPlayerForce(localPlayer, transform.forward * Mathf.Lerp(idlePlayerAcceleration, takeOffPlayerAcceleration, normalizedThrust));
                }
            }
        }

        public void PlayerStrike()
        {
            if (!broken)
            {
                broken = true;
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayStrikeSound));
            }
            fire = true;
        }

        public void PlayStrikeSound()
        {
            if (strikeSoundSource && strikeSound) strikeSoundSource.PlayOneShot(strikeSound);
        }

        private void AddPlayerForce(VRCPlayerApi player, Vector3 force)
        {
            player.SetVelocity(player.GetVelocity() + (force + (player.IsPlayerGrounded() ? Vector3.up * 0.5f : Vector3.zero)) * Time.deltaTime);
        }
        #endregion

        #region Jet Blast
        [Header("Jet Blast")]
        public ParticleSystem blastParticle;
        public ParticleSystem reverserBlastParticle;
        public float blastIdleSpeed = 10;
        public float blastTakeOffSpeed = 1000;

        private void JetBlast_Start()
        {
            SetParticleEmission(blastParticle, false, 0);
            SetParticleEmission(reverserBlastParticle, false, 0);
        }

        private void JetBlast_Update()
        {
            SetParticleEmission(blastParticle, !Mathf.Approximately(n1, 0), TSFEUtil.Lerp3(0, blastIdleSpeed, blastTakeOffSpeed, n1, 0, idleN1, takeOffN2));
            var reverserIntensity = Mathf.Clamp01(reverserPosition * 2 - 1);
            SetParticleEmission(reverserBlastParticle, !Mathf.Approximately(n1 * reverserIntensity, 0), TSFEUtil.Lerp3(0, blastIdleSpeed, blastTakeOffSpeed, n1, 0, idleN1, takeOffN2) * reverserIntensity * reverserRatio);
        }
        #endregion

        #region Utilities
        private void SetParticleEmission(ParticleSystem system, bool emit, float speed)
        {
            if (!system) return;
            if (emit)
            {
                var main = system.main;
                main.startSpeedMultiplier = speed;
            }
            var emission = system.emission;
            if (emission.enabled != emit) emission.enabled = emit;
        }

        private bool InitializeAudioSource(AudioSource audioSource)
        {
            if (!audioSource) return false;
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            return true;
        }

        private void SetAudioVolumeAndPitch(AudioSource audioSource, float volume, float pitch, float response)
        {
            if (!audioSource) return;

            var stop = Mathf.Approximately(volume, 0.0f) || Mathf.Approximately(pitch, 0.0f);

            if (!stop)
            {
                audioSource.volume = Mathf.Lerp(audioSource.volume, volume, response);
                audioSource.pitch = Mathf.Lerp(audioSource.pitch, pitch, response);
            }

            if (audioSource.isPlaying == stop)
            {
                if (stop)
                {
                    audioSource.Stop();
                }
                else
                {
                    audioSource.time = audioSource.clip.length * (UnityEngine.Random.value % 1.0f);
                    audioSource.volume = volume;
                    audioSource.pitch = pitch;
                    audioSource.Play();
                }
            }
        }

        private void MuteAudioSources(AudioSource[] audioSources)
        {
            if (audioSources == null) return;
            foreach (var audioSource in audioSources) MuteAudioSource(audioSource);
        }

        private void MuteAudioSource(AudioSource audioSource)
        {
            if (!audioSource) return;
            audioSource.mute = true;
            audioSource.playOnAwake = false;
            audioSource.priority = 255;
            audioSource.Stop();
        }

        private float Randomize()
        {
            return 1 + (UnityEngine.Random.value - 0.5f) * randomRange;
        }

        private float TwoWayMoveTowards(float a, float b, float maxDelta, float ascMultiplier, float dscMultiplier)
        {
            return Mathf.MoveTowards(a, b, maxDelta * (a < b ? ascMultiplier : dscMultiplier));
        }
        #endregion
    }
}
