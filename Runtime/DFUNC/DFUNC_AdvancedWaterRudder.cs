using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using SFAdvEquipment.Utility;

namespace SFAdvEquipment.DFUNC
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

        private Animator vehicleAnimator;
        private Rigidbody vehicleRigidbody;
        private float rudderAngle;
        private Vector3 localForce;
        private float forceMultiplier;
        private bool selected;
        private bool prevTrigger;

        [UdonSynced][FieldChangeCallback(nameof(Extracted))] private bool _extracted;
        public bool Extracted
        {
            set
            {
                SFAEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, value);
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

            UpdateActive();
            SFEXT_G_Reappear();
        }

        public void SFEXT_O_PilotEnter()
        {
            selected = false;
            UpdateActive();
        }

        public void SFEXT_O_PilotExit()
        {
            selected = false;
            UpdateActive();
        }

        public void SFEXT_G_TakeOff() => UpdateActive();
        public void SFEXT_G_TouchDownWater() => UpdateActive();

        public void SFEXT_G_RespawnButton() => SFEXT_G_Reappear();
        public void SFEXT_G_Reappear()
        {
            Extracted = defaultExtracted;
            if (Networking.IsOwner(gameObject)) RequestSerialization();
        }

        public void DFUNC_Selected() { selected = true; prevTrigger = true; }
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
                var trigger = SFAEUtil.IsTriggerPressed(LeftDial);
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
            Extracted = true;
            RequestSerialization();
        }

        public void Retract()
        {
            Extracted = false;
            RequestSerialization();
        }

        public void Toggle()
        {
            Extracted = !Extracted;
            RequestSerialization();
        }
    }
}
