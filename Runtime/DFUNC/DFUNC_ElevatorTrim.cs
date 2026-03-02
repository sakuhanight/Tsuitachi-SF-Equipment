using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_ElevatorTrim : UdonSharpBehaviour
    {
        public float controllerSensitivity = 0.5f;
        public KeyCode desktopUp = KeyCode.U;
        public KeyCode desktopDown = KeyCode.Y;
        public float desktopStep = 0.02f;
        public float trimStrengthMultiplier = 1;
        public float trimStrengthCurve = 1;
        public string animatorParameterName = "elevtrim";
        public Vector3 vrInputAxis = Vector3.forward;
        public float trimBias = 0;

        [Header("Haptics")]
        [Range(0, 1)] public float hapticDuration = 0.2f;
        [Range(0, 1)] public float hapticAmplitude = 0.5f;
        [Range(0, 1)] public float hapticFrequency = 0.1f;

        [Header("Debug")]
        public Transform debugControllerTransform;

        public UdonSharpBehaviour SAVControl;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;

        [System.NonSerialized] public bool LeftDial = false;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public SaccEntity EntityControl;

        [System.NonSerialized][UdonSynced] public float trim;

        private VRCPlayerApi.TrackingDataType trackingTarget;
        private Transform controlsRoot;
        private Rigidbody vehicleRigidbody;
        private Animator vehicleAnimator;
        private float trimStrength, rotMultiMaxSpeed;
        private bool hasPilot, isPilot, isOwner, isSelected, isDirty, triggered, prevTriggered;
        private Vector3 prevTrackingPosition;
        private float sliderInput, prevTrim;

        public void DFUNC_LeftDial()
        {
            trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        public void DFUNC_RightDial()
        {
            trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
        }
        public void DFUNC_Selected()
        {
            isSelected = true;
            prevTriggered = false;
        }
        public void DFUNC_Deselected() { isSelected = false; }

        public void SFEXT_L_EntityStart()
        {
            var entity = EntityControl;
            controlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
            if (!controlsRoot) controlsRoot = entity.transform;

            rotMultiMaxSpeed = (float)SAVControl.GetProgramVariable("RotMultiMaxSpeed");
            vehicleRigidbody = entity.GetComponent<Rigidbody>();
            vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");

            var pitchStrength = (float)SAVControl.GetProgramVariable("PitchStrength");
            trimStrength = pitchStrength * trimStrengthMultiplier;

            ResetStatus();
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            isOwner = true;
            isSelected = false;
            prevTriggered = false;
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

        private void ResetStatus()
        {
            prevTrim = trim = 0;
            if (vehicleAnimator) vehicleAnimator.SetFloat(animatorParameterName, 0.5f);
        }

        private void FixedUpdate()
        {
            if (!isOwner || !isPilot) return;

            var rotInputs = (Vector3)SAVControl.GetProgramVariable("RotationInputs");
            var trimComponent = -(Mathf.Sign(trim) * Mathf.Pow(Mathf.Abs(trim), trimStrengthCurve) + trimBias);
            rotInputs.x = Mathf.Clamp(rotInputs.x + trimComponent * trimStrengthMultiplier, -1, 1);
            SAVControl.SetProgramVariable("RotationInputs", rotInputs);
        }

        private void Update()
        {
            isDirty = false;

            if (isPilot)
            {
                trim = Mathf.Clamp(trim + sliderInput, -1, 1);
                if (!Mathf.Approximately(sliderInput, 0) && Time.frameCount % Mathf.Max(1, Mathf.FloorToInt(hapticDuration / Time.fixedDeltaTime)) == 0)
                {
                    TSFEUtil.PlayHaptics(LeftDial, hapticDuration, hapticAmplitude, hapticFrequency);
                }
            }

            var trimChanged = !Mathf.Approximately(trim, prevTrim);
            prevTrim = trim;
            if (trimChanged)
            {
                isDirty = true;
                if (vehicleAnimator) vehicleAnimator.SetFloat(animatorParameterName, TSFEUtil.Remap01(trim, -1, 1));
            }

            if (!hasPilot && !isDirty) gameObject.SetActive(false);
        }

        public override void PostLateUpdate()
        {
            if (!isPilot) return;

            prevTriggered = triggered;
            triggered = isSelected && TSFEUtil.IsTriggerPressed(LeftDial) || debugControllerTransform;

            if (triggered)
            {
                var pos = debugControllerTransform
                    ? controlsRoot.InverseTransformPoint(debugControllerTransform.position)
                    : controlsRoot.InverseTransformPoint(Networking.LocalPlayer.GetTrackingData(trackingTarget).position);

                if (prevTriggered)
                {
                    var delta = pos - prevTrackingPosition;
                    sliderInput = Vector3.Dot(delta, vrInputAxis) * controllerSensitivity;
                }
                else
                {
                    sliderInput = 0;
                }
                prevTrackingPosition = pos;
            }
            else
            {
                sliderInput = 0;
            }

            if (Input.GetKeyDown(desktopUp))
            {
                sliderInput = desktopStep;
            }
            if (Input.GetKeyDown(desktopDown))
            {
                sliderInput = -desktopStep;
            }
        }

        public void TrimUp() { trim += desktopStep; }
        public void TrimDown() { trim -= desktopStep; }
    }
}
