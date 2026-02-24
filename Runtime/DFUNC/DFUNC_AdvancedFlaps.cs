using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_AdvancedFlaps : UdonSharpBehaviour
    {
        [Header("Specs")]
        public float[] detents = { 0, 1, 2, 5, 10, 15, 25, 30, 40 };
        [Tooltip("KIAS")] public float[] speedLimits = { 340, 250, 250, 250, 210, 200, 190, 175, 162 };
        public float dragMultiplier = 1.4f;
        public float liftMultiplier = 1.35f;
        public float response = 1f;
        public GameObject powerSource;

        [Header("Inputs")]
        public float controllerSensitivity = 0.1f;
        public Vector3 vrInputAxis = Vector3.forward;
        public KeyCode desktopKey = KeyCode.F;
        public bool seamless = true;

        [Header("Animator")]
        public string boolParameterName = "flaps";
        public string angleParameterName = "flapsangle";
        public string targetAngleParameterName = "flapstarget";
        public string brokenParameterName = "flapsbroken";

        [Header("Sounds")]
        public AudioSource[] audioSources = { };
        public float soundResponse = 1;
        public AudioSource[] breakingSounds = { };

        [Header("Faults")]
        public float meanTimeBetweenActuatorBrokenOnOverspeed = 120.0f;
        public float meanTimeBetweenWingBrokenOnOverspeed = 240.0f;
        public float overspeedDamageMultiplier = 10.0f;
        public float brokenDragMultiplier = 2.9f;
        public float brokenLiftMultiplier = 0.3f;

        [Header("Haptics")]
        [Range(0, 1)] public float hapticDuration = 0.2f;
        [Range(0, 1)] public float hapticAmplitude = 0.5f;
        [Range(0, 1)] public float hapticFrequency = 0.1f;

        public UdonSharpBehaviour SAVControl;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;

        [System.NonSerialized] public bool LeftDial = false;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public SaccEntity EntityControl;

        [HideInInspector] public int targetDetentIndex, detentIndex;
        [HideInInspector] public float detentAngle, targetDetentAngle, speedLimit, targetSpeedLimit, angle, maxAngle;

        private Animator vehicleAnimator;
        [System.NonSerialized][UdonSynced(UdonSyncMode.Smooth)] public float targetAngle;
        [UdonSynced] private bool actuatorBroken;
        [UdonSynced][FieldChangeCallback(nameof(WingBroken))] private bool _wingBroken;
        private bool WingBroken
        {
            set
            {
                if (value == _wingBroken) return;
                _wingBroken = value;
                if (vehicleAnimator) vehicleAnimator.SetBool(brokenParameterName, value);
                if (value)
                {
                    foreach (var src in breakingSounds)
                    {
                        if (src) src.PlayScheduled(Random.value * 0.1f);
                    }
                }
            }
            get => _wingBroken;
        }

        private VRCPlayerApi.TrackingDataType trackingTarget;
        private bool hasPilot, isPilot, isOwner, selected;
        private Transform controlsRoot;
        private float[] audioVolumes, audioPitches;

        public void DFUNC_LeftDial()
        {
            trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        public void DFUNC_RightDial()
        {
            trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
        }

        public void SFEXT_L_EntityStart()
        {
            var entity = EntityControl;
            vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
            controlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
            if (!controlsRoot) controlsRoot = entity.transform;

            maxAngle = detents[detents.Length - 1];

            audioVolumes = new float[audioSources.Length];
            audioPitches = new float[audioSources.Length];
            for (var i = 0; i < audioSources.Length; i++)
            {
                var src = audioSources[i];
                if (!src) continue;
                audioVolumes[i] = src.volume;
                audioPitches[i] = src.pitch;
            }

            ResetStatus();
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            isOwner = true;
            selected = false;
        }
        public void SFEXT_O_PilotExit() { isPilot = false; }
        public void SFEXT_O_TakeOwnership() { isOwner = true; }
        public void SFEXT_O_LoseOwnership() { isOwner = false; }

        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit() { hasPilot = false; }
        public void SFEXT_G_Explode() { ResetStatus(); }
        public void SFEXT_G_RespawnButton() { ResetStatus(); }

        public void DFUNC_Selected() { selected = true; }
        public void DFUNC_Deselected() { selected = false; }

        private float prevAngle, prevTargetAngle;
        private void Update()
        {
            var dt = Time.deltaTime;

            UpdateDetents();

            if (isOwner) ApplyDamage(dt);

            var actuatorMoving = !actuatorBroken && (!powerSource || powerSource.activeInHierarchy);
            UpdateSounds(dt, actuatorMoving);

            if (actuatorMoving) angle = Mathf.MoveTowards(angle, targetAngle, response * dt);

            var flapsChanged = !Mathf.Approximately(angle, prevAngle);
            prevAngle = angle;

            var targetChanged = !Mathf.Approximately(targetAngle, prevTargetAngle);
            prevTargetAngle = targetAngle;

            if (flapsChanged)
            {
                if (vehicleAnimator)
                {
                    vehicleAnimator.SetFloat(angleParameterName, angle / maxAngle);
                    vehicleAnimator.SetBool(boolParameterName, !Mathf.Approximately(angle, 0));
                }
                ApplyParameters();
            }

            if (targetChanged)
            {
                if (vehicleAnimator) vehicleAnimator.SetFloat(targetAngleParameterName, targetAngle / maxAngle);
            }

            if (!hasPilot && !flapsChanged) gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (isPilot) HandleInput();
        }

        private void ResetStatus()
        {
            angle = targetAngle = 0;
            actuatorBroken = false;
            WingBroken = false;

            var sav = SAVControl;
            sav.SetProgramVariable("ExtraDrag", (float)sav.GetProgramVariable("ExtraDrag") - appliedExtraDrag);
            sav.SetProgramVariable("ExtraLift", (float)sav.GetProgramVariable("ExtraLift") - appliedExtraLift);
            appliedExtraDrag = 0;
            appliedExtraLift = 0;

            gameObject.SetActive(false);
        }

        private bool prevTrigger;
        private Vector3 trackingOrigin;
        private float targetAngleOrigin;
        private void HandleInput()
        {
            if (selected)
            {
                var trigger = TSFEUtil.IsTriggerPressed(LeftDial);
                var triggerChanged = prevTrigger != trigger;
                prevTrigger = trigger;

                if (trigger)
                {
                    var trackingPosition = controlsRoot.InverseTransformPoint(Networking.LocalPlayer.GetTrackingData(trackingTarget).position);
                    if (triggerChanged)
                    {
                        trackingOrigin = trackingPosition;
                        targetAngleOrigin = targetAngle;
                    }
                    else
                    {
                        targetAngle = Mathf.Clamp(targetAngleOrigin - Vector3.Dot(trackingPosition - trackingOrigin, vrInputAxis) * maxAngle / controllerSensitivity, 0, maxAngle);
                    }
                }

                if (triggerChanged && !trigger && !seamless)
                {
                    UpdateDetents();
                    targetAngle = targetDetentAngle;
                }
            }

            if (Input.GetKeyDown(desktopKey))
            {
                targetAngle = detents[(targetDetentIndex + 1) % detents.Length];
            }
        }

        private void UpdateDetents()
        {
            while (detentIndex > 0 && detents[detentIndex] > angle) detentIndex--;
            while (detentIndex < detents.Length - 1 && detents[detentIndex] < angle) detentIndex++;
            detentAngle = detents[detentIndex];

            var prev = targetDetentIndex;
            while (targetDetentIndex > 0 && detents[targetDetentIndex] > targetAngle) targetDetentIndex--;
            while (targetDetentIndex < detents.Length - 1 && detents[targetDetentIndex] < targetAngle) targetDetentIndex++;

            if (isPilot && targetDetentIndex != prev)
                TSFEUtil.PlayHaptics(LeftDial, hapticDuration, hapticAmplitude, hapticFrequency);

            targetDetentAngle = detents[targetDetentIndex];
            targetSpeedLimit = speedLimits[targetDetentIndex];
            speedLimit = speedLimits[detentIndex];
        }

        private void UpdateSounds(float dt, bool actuatorAvailable)
        {
            var moving = actuatorAvailable && !Mathf.Approximately(targetAngle, angle);
            for (var i = 0; i < audioSources.Length; i++)
            {
                var src = audioSources[i];
                if (!src) continue;
                var volume = Mathf.Lerp(src.volume, moving ? audioVolumes[i] : 0.0f, soundResponse * dt);
                if (Mathf.Approximately(volume, 0))
                {
                    if (src.isPlaying) { src.Stop(); src.volume = 0; src.pitch = 0.8f; }
                }
                else
                {
                    src.volume = volume;
                    src.pitch = Mathf.Lerp(src.pitch, (moving ? 1.0f : 0.8f) * audioPitches[i], soundResponse * dt);
                    if (!src.isPlaying) { src.loop = true; src.time = src.clip.length * (Random.value % 1.0f); src.Play(); }
                }
            }
        }

        private void ApplyDamage(float dt)
        {
            var airSpeed = TSFEUtil.ToKnots((float)SAVControl.GetProgramVariable("AirSpeed"));
            var damage = Mathf.Max(airSpeed - speedLimit, 0) / speedLimit * overspeedDamageMultiplier;
            if (damage > 0)
            {
                if (!actuatorBroken && TSFEUtil.CheckMTBFScaled(dt, meanTimeBetweenActuatorBrokenOnOverspeed, damage))
                {
                    actuatorBroken = true;
                }
                if (!WingBroken && TSFEUtil.CheckMTBFScaled(dt, meanTimeBetweenWingBrokenOnOverspeed, damage))
                {
                    WingBroken = true;
                    actuatorBroken = true;
                    ApplyParameters();
                }
            }
        }

        private float appliedExtraDrag, appliedExtraLift;
        private void ApplyParameters()
        {
            var normalizedPosition = angle / maxAngle;
            var extraDrag = WingBroken ? brokenDragMultiplier - 1 : (dragMultiplier - 1) * normalizedPosition;
            var extraLift = WingBroken ? brokenLiftMultiplier - 1 : (liftMultiplier - 1) * normalizedPosition;

            var sav = SAVControl;
            sav.SetProgramVariable("ExtraDrag", (float)sav.GetProgramVariable("ExtraDrag") + extraDrag - appliedExtraDrag);
            sav.SetProgramVariable("ExtraLift", (float)sav.GetProgramVariable("ExtraLift") + extraLift - appliedExtraLift);
            appliedExtraDrag = extraDrag;
            appliedExtraLift = extraLift;
        }

        public void NextDetent()
        {
            targetAngle = detents[Mathf.Clamp(targetDetentIndex + 1, 0, detents.Length - 1)];
            UpdateDetents();
        }

        public void PreviousDetent()
        {
            targetAngle = detents[Mathf.Clamp(targetDetentIndex - 1, 0, detents.Length - 1)];
            UpdateDetents();
        }
    }
}
