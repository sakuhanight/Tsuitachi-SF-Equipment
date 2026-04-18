using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_AdvancedWaterRudder : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;
        public bool defaultExtracted = false;
        public AnimationCurve liftCoefficientCurve = AnimationCurve.Linear(0, 0, 30, 0.1f);
        public AnimationCurve dragCoefficientCurve = AnimationCurve.Linear(0, 0, 30, 0.01f);
        public float referenceArea = 1.0f;
        public float waterDensity = 999.1026f;
        public float maxRudderAngle = 30.0f;
        public float response = 0.5f;

        [System.NonSerialized] public SaccEntity EntityControl;
        [System.NonSerialized] public bool LeftDial;
        [System.NonSerialized] public int DialPosition = -999;

        // 標準状態変数
        private bool isPilot, isOwner, selected, hasPilot;
        private VRCPlayerApi.TrackingDataType trackingTarget;
        private Transform controlsRoot;

        // コンポーネント固有の状態
        private Animator vehicleAnimator;
        private Rigidbody vehicleRigidbody;
        private float rudderAngle;
        private Vector3 localForce;
        private float forceMultiplier;
        private bool prevTrigger;

        [UdonSynced][FieldChangeCallback(nameof(Extracted))] private bool _extracted;
        public bool Extracted
        {
            set
            {
                TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, value);
                if (vehicleAnimator) vehicleAnimator.SetBool("waterrudder", value);
                _extracted = value;
            }
            get => _extracted;
        }

        private void Start()
        {
            gameObject.SetActive(false);
        }

        public void SFEXT_L_EntityStart()
        {
            vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
            vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
            controlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
            if (!controlsRoot) controlsRoot = EntityControl.transform;

            isOwner = Networking.IsOwner(gameObject);
            UpdateActive();
            ResetStatus();
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            isOwner = true;
            selected = false;
            UpdateActive();
        }

        public void SFEXT_O_PilotExit()
        {
            isPilot = false;
            selected = false;
            UpdateActive();
        }

        public void SFEXT_O_TakeOwnership() { isOwner = true; }
        public void SFEXT_O_LoseOwnership() { isOwner = false; }

        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            gameObject.SetActive(true);
        }

        public void SFEXT_G_PilotExit()
        {
            hasPilot = false;
        }

        public void SFEXT_G_TakeOff() => UpdateActive();
        public void SFEXT_G_TouchDownWater() => UpdateActive();

        public void SFEXT_G_Explode() { ResetStatus(); }
        public void SFEXT_G_RespawnButton() { ResetStatus(); }

        private void ResetStatus()
        {
            Extracted = defaultExtracted;
            if (isOwner)
            {
                RequestSerialization();
            }
        }

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
            selected = true;
            prevTrigger = true;

            // LeftDialに応じてtrackingTargetを設定（保険）
            trackingTarget = LeftDial
                ? VRCPlayerApi.TrackingDataType.LeftHand
                : VRCPlayerApi.TrackingDataType.RightHand;

            // 非Ownerが選択した場合、Ownershipを取得
            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }

        public void DFUNC_Deselected() { selected = false; }

        public void KeyboardInput() => Toggle();

        private void FixedUpdate()
        {
            if (!(Extracted && vehicleRigidbody)) return;
            vehicleRigidbody.AddForceAtPosition(transform.TransformVector(localForce), transform.position);
        }

        private void Update()
        {
            if (selected)
            {
                var trigger = TSFEUtil.IsTriggerPressed(LeftDial);
                if (trigger && !prevTrigger) Toggle();
                prevTrigger = trigger;
            }

            if (!(Extracted && vehicleRigidbody)) return;

            var velocity = vehicleRigidbody.velocity;
            var speed = velocity.magnitude;

            var rotationInputs = (Vector3)SAVControl.GetProgramVariable("RotationInputs");
            var rudderTargetAngle = rotationInputs.z * maxRudderAngle;
            rudderAngle = Mathf.Lerp(rudderAngle, rudderTargetAngle, Time.deltaTime * response);

            var rudderAoA = GetRudderAoA(rudderAngle, velocity);
            localForce = (Vector3.right * liftCoefficientCurve.Evaluate(rudderAoA) - Vector3.back * dragCoefficientCurve.Evaluate(rudderAoA)) * Mathf.Pow(speed, 2) * forceMultiplier;
        }

        private void UpdateActive()
        {
            var piloting = EntityControl && EntityControl.Piloting;
            var floating = SAVControl && (bool)SAVControl.GetProgramVariable("Floating");
            var isActive = piloting && floating;

            if (isActive)
            {
                forceMultiplier = 0.5f * waterDensity * referenceArea;
            }
            else
            {
                rudderAngle = 0.0f;
                localForce = Vector3.zero;
            }

            gameObject.SetActive(isActive);
        }

        private float GetRudderAoA(float angle, Vector3 velocity)
        {
            var rotatedVelocity = Quaternion.AngleAxis(angle, transform.up) * velocity;
            return Mathf.Approximately(rotatedVelocity.sqrMagnitude, 0.0f)
                ? 0.0f
                : -Mathf.Atan(Vector3.Dot(rotatedVelocity, transform.right) / Vector3.Dot(rotatedVelocity, transform.forward)) * Mathf.Rad2Deg;
        }

        public void Extract()
        {
            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            Extracted = true;
            RequestSerialization();
        }

        public void Retract()
        {
            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            Extracted = false;
            RequestSerialization();
        }

        public void Toggle()
        {
            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            Extracted = !Extracted;
            RequestSerialization();
        }
    }
}
