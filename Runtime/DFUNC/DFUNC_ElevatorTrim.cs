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
        [Header("Inputs")]
        [Tooltip("VRでトリガー+前後操作時の移動検出閾値（メートル）")]
        public float vrInputThreshold = 0.05f;
        public Vector3 vrInputAxis = Vector3.forward;
        public KeyCode desktopUp = KeyCode.U;
        public KeyCode desktopDown = KeyCode.Y;
        public float trimStep = 0.02f;

        [Header("Trim Settings")]
        public float trimStrengthMultiplier = 1;
        public float trimStrengthCurve = 1;
        public float trimBias = 0;

        [Header("Animator")]
        public string animatorParameterName = "elevtrim";

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

        [System.NonSerialized][UdonSynced] public float trim;

        private VRCPlayerApi.TrackingDataType trackingTarget;
        private Transform controlsRoot;
        private Animator vehicleAnimator;
        private bool hasPilot, isPilot, isOwner, isSelected, isDirty, prevTrigger;
        private Vector3 trackingOrigin;
        private float prevTrim;

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
            prevTrigger = false;
            // 非Ownerが選択した場合、Ownershipを取得
            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }
        public void DFUNC_Deselected() { isSelected = false; }

        public void SFEXT_L_EntityStart()
        {
            var entity = EntityControl;
            controlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
            if (!controlsRoot) controlsRoot = entity.transform;

            vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");

            ResetStatus();
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            isOwner = true;
            isSelected = false;
            prevTrigger = false;
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
            var trimChanged = !Mathf.Approximately(trim, prevTrim);
            prevTrim = trim;
            if (trimChanged)
            {
                isDirty = true;
                if (vehicleAnimator) vehicleAnimator.SetFloat(animatorParameterName, TSFEUtil.Remap01(trim, -1, 1));
            }

            if (!hasPilot && !isDirty) gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            // isPilot（左席Owner）または isSelected（ダイヤル選択中）なら入力処理
            if (!isPilot && !isSelected) return;

            if (isSelected)
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
                    }
                    else
                    {
                        var delta = Vector3.Dot(trackingPosition - trackingOrigin, vrInputAxis);
                        if (delta > vrInputThreshold)
                        {
                            TrimDown();
                            trackingOrigin = trackingPosition;
                        }
                        else if (delta < -vrInputThreshold)
                        {
                            TrimUp();
                            trackingOrigin = trackingPosition;
                        }
                    }
                }
            }

            if (Input.GetKeyDown(desktopUp))
            {
                TrimUp();
            }
            if (Input.GetKeyDown(desktopDown))
            {
                TrimDown();
            }
        }

        public void TrimUp()
        {
            trim = Mathf.Clamp(trim + trimStep, -1, 1);
            if (isPilot) TSFEUtil.PlayHaptics(LeftDial, hapticDuration, hapticAmplitude, hapticFrequency);
        }

        public void TrimDown()
        {
            trim = Mathf.Clamp(trim - trimStep, -1, 1);
            if (isPilot) TSFEUtil.PlayHaptics(LeftDial, hapticDuration, hapticAmplitude, hapticFrequency);
        }
    }
}
