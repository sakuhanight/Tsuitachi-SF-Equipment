using System;
using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_ThrustReverser : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public float ReversingThrottleMultiplier = -0.5f;
        public KeyCode KeyboardControl = KeyCode.R;
        [Tooltip("SAVControl.VehicleAnimator when null")] public Animator ThrustReverserAnimator;
        public string ParameterName = "reverse";
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;

        [NonSerialized] public float ReversingEngineOutput;
        [NonSerialized] public SaccEntity EntityControl;
        [NonSerialized] public bool LeftDial;
        [NonSerialized] public int DialPosition = -999;

        private float ThrottleStrength, ReversingThrottleStrength;
        private bool Selected, isPilot, lowFuel;

        [UdonSynced][FieldChangeCallback(nameof(Reversing))] private bool _reversing;
        public bool Reversing
        {
            private set
            {
                if (value == _reversing) return;
                _reversing = value;

                if (isPilot)
                {
                    SAVControl.SetProgramVariable("ThrottleStrength", value ? -ReversingThrottleStrength : ThrottleStrength);

                    var throttleOverridden = (int)SAVControl.GetProgramVariable("ThrottleOverridden_");
                    SAVControl.SetProgramVariable("ThrottleOverridden_", throttleOverridden + (value ? 1 : -1));
                    SAVControl.SetProgramVariable("ThrottleOverride", value ? 1.0f : 0.0f);
                }

                EntityControl.SendEventToExtensions(value ? "SFEXT_O_StartReversing" : "SFEXT_O_StopReversing");
                if (ThrustReverserAnimator) ThrustReverserAnimator.SetBool(ParameterName, value);
                TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, value);
            }
            get => _reversing;
        }

        public void DFUNC_LeftDial() { LeftDial = true; }
        public void DFUNC_RightDial() { LeftDial = false; }
        public void DFUNC_Selected() => Selected = true;
        public void DFUNC_Deselected() => Selected = false;

        public void SFEXT_L_EntityStart()
        {
            if (!ThrustReverserAnimator) ThrustReverserAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
            ThrottleStrength = (float)SAVControl.GetProgramVariable("ThrottleStrength");
            ReversingThrottleStrength = ThrottleStrength * ReversingThrottleMultiplier;
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            Reversing = false;
            ReversingEngineOutput = 0;
            RequestSerialization();
        }

        public void SFEXT_O_PilotExit()
        {
            isPilot = false;
            Reversing = false;
            RequestSerialization();
        }

        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
            ReversingEngineOutput = 0;
        }

        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
            ReversingEngineOutput = 0;
        }

        public void SFEXT_G_LowFuel() { lowFuel = true; }
        public void SFEXT_G_NotLowFuel() { lowFuel = false; }

        private float GetInput()
        {
            if (lowFuel) return 0.0f;
            if (Input.GetKey(KeyboardControl)) return 1.0f;
            if (!Selected) return 0.0f;
            return TSFEUtil.GetTriggerInput(LeftDial);
        }

        private void Update()
        {
            if (!isPilot) return;

            var engineOn = (bool)SAVControl.GetProgramVariable("EngineOn");
            var trigger = GetInput() > 0.75f && engineOn;
            if (trigger != Reversing)
            {
                Reversing = trigger;
                RequestSerialization();
            }
        }
    }
}
