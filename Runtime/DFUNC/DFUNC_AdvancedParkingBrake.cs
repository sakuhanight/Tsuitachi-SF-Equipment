using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using SFAdvEquipment.Utility;

namespace SFAdvEquipment.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_AdvancedParkingBrake : UdonSharpBehaviour
    {
        public KeyCode desktopControl = KeyCode.N;
        public string parameterName = "parkingbrake";

        public UdonSharpBehaviour SAVControl;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;

        [System.NonSerialized] public bool LeftDial = false;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public SaccEntity EntityControl;

        [System.NonSerialized][UdonSynced][FieldChangeCallback(nameof(State))] private bool _state = false;
        public bool State
        {
            private set
            {
                _state = value;
                if (!initialized) return;
                if (vehicleAnimator) vehicleAnimator.SetBool(parameterName, value);
                SFAEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, value);
                if (gears != null)
                {
                    foreach (var gear in gears) gear.SetProgramVariable("parkingBrake", value);
                }
            }
            get => _state;
        }

        private Animator vehicleAnimator;
        private UdonSharpBehaviour[] gears;
        private bool initialized, isPilot, selected;
        private bool _triggerLastFrame;

        public void DFUNC_LeftDial() { }
        public void DFUNC_RightDial() { }
        public void DFUNC_Selected() { selected = true; }
        public void DFUNC_Deselected() { selected = false; }

        public void SFEXT_L_EntityStart()
        {
            vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");

            var entity = EntityControl;
            var extBehaviours = entity.gameObject.GetComponentsInChildren<UdonSharpBehaviour>(true);
            var gearList = new UdonSharpBehaviour[extBehaviours.Length];
            int count = 0;
            foreach (var ext in extBehaviours)
            {
                if (ext.GetType().Name == "SFEXT_AdvancedGear")
                {
                    gearList[count++] = ext;
                }
            }
            gears = new UdonSharpBehaviour[count];
            System.Array.Copy(gearList, gears, count);

            gameObject.SetActive(false);
            initialized = true;
            State = false;
        }

        private void Toggle()
        {
            State = !State;
            RequestSerialization();
        }

        public void KeyboardInput() { Toggle(); }

        private void Update()
        {
            if (!isPilot) return;

            if (Input.GetKeyDown(desktopControl)) Toggle();

            if (selected && Networking.LocalPlayer.IsUserInVR())
            {
                var trigger = SFAEUtil.IsTriggerPressed(LeftDial);
                if (trigger && !_triggerLastFrame) Toggle();
                _triggerLastFrame = trigger;
            }
        }

        public void SFEXT_O_PilotEnter()
        {
            selected = false;
            isPilot = true;
        }
        public void SFEXT_O_PilotExit()
        {
            selected = false;
            isPilot = false;
        }
        public void SFEXT_G_PilotEnter() { gameObject.SetActive(true); }
        public void SFEXT_G_PilotExit() { gameObject.SetActive(false); }
        public void SFEXT_G_Explode() { ResetStatus(); }
        public void SFEXT_G_RespawnButton() { ResetStatus(); }

        private void ResetStatus()
        {
            State = false;
            RequestSerialization();
        }

        public void Set() { State = true; RequestSerialization(); }
        public void Release() { State = false; RequestSerialization(); }
    }
}
