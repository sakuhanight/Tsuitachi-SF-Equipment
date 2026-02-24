using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_AdvancedSpeedBrake : UdonSharpBehaviour
    {
        public float liftMultiplier = 0.6f;
        public float dragMultiplier = 1.4f;
        public float response = 1.0f;

        [Header("Inputs")]
        public float vrInputDistance = 0.1f;
        public float incrementStep = 0.5f;
        public KeyCode desktopKey = KeyCode.B;

        [Header("Animation")]
        public string floatParameterName = "speedbrake";
        public string floatInputParameterName = "speedbrakeinput";

        public UdonSharpBehaviour SAVControl;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;

        [System.NonSerialized] public bool LeftDial = false;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public SaccEntity EntityControl;

        private Animator vehicleAnimator;
        private Transform controlsRoot;
        private VRCPlayerApi.TrackingDataType trackingTarget;

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(TargetAngle))] private float _targetAngle;
        public float TargetAngle
        {
            private set
            {
                var clamped = Mathf.Clamp01(value);
                if (vehicleAnimator) vehicleAnimator.SetFloat(floatInputParameterName, clamped);
                TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, clamped > 0);
                _targetAngle = clamped;
            }
            get => _targetAngle;
        }

        private float _angle;
        private float Angle
        {
            set
            {
                var clamped = Mathf.Clamp01(value);
                var diff = clamped - _angle;

                var sav = SAVControl;
                sav.SetProgramVariable("ExtraLift", (float)sav.GetProgramVariable("ExtraLift") + diff * liftMultiplier);
                sav.SetProgramVariable("ExtraDrag", (float)sav.GetProgramVariable("ExtraDrag") + diff * dragMultiplier);

                if (vehicleAnimator) vehicleAnimator.SetFloat(floatParameterName, clamped);
                _angle = clamped;
            }
            get => _angle;
        }

        private bool isPilot, isSelected;
        private Vector3 prevHandPosition;
        private bool _triggerState;
        private bool TriggerState
        {
            set
            {
                if (value && !_triggerState) OnTriggerDown();
                _triggerState = value;
            }
            get => _triggerState;
        }

        public void DFUNC_LeftDial()
        {
            trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        public void DFUNC_RightDial()
        {
            trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
        }
        public void DFUNC_Selected() { isSelected = true; }
        public void DFUNC_Deselected() { isSelected = false; }

        public void SFEXT_L_EntityStart()
        {
            vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
            controlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
            if (!controlsRoot) controlsRoot = EntityControl.transform;
            SFEXT_G_ReAppear();
        }

        public void SFEXT_G_ReAppear()
        {
            TargetAngle = 0;
            Angle = 0;
        }
        public void SFEXT_G_PilotEnter() { gameObject.SetActive(true); }
        public void SFEXT_G_PilotExit() { gameObject.SetActive(false); }
        public void SFEXT_O_PilotEnter() { isPilot = true; }
        public void SFEXT_O_PilotExit() { isPilot = false; isSelected = false; }

        private void Update()
        {
            if (isPilot)
            {
                TriggerState = isSelected && TSFEUtil.IsTriggerPressed(LeftDial);
                if (Input.GetKeyDown(desktopKey)) TargetAngle = 1.0f;
                else if (Input.GetKeyUp(desktopKey)) TargetAngle = 0.0f;
            }

            if (!Mathf.Approximately(TargetAngle, Angle))
            {
                Angle = Mathf.MoveTowards(Angle, TargetAngle, response * Time.deltaTime);
            }
        }

        private Vector3 GetLocalHandPosition()
        {
            return controlsRoot.InverseTransformPoint(Networking.LocalPlayer.GetTrackingData(trackingTarget).position);
        }

        private void OnTriggerDown()
        {
            prevHandPosition = GetLocalHandPosition();
        }

        public override void PostLateUpdate()
        {
            if (isPilot && TriggerState)
            {
                var handPos = GetLocalHandPosition();
                TargetAngle -= Vector3.Dot(handPos - prevHandPosition, Vector3.forward) / vrInputDistance;
                prevHandPosition = handPos;
            }
        }

        public void IncreaseAngle() { TargetAngle += incrementStep; }
        public void DecreaseAngle() { TargetAngle -= incrementStep; }
    }
}
