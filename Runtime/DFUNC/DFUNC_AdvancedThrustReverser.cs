using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;
using SFAdvEquipment.Utility;

namespace SFAdvEquipment.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_AdvancedThrustReverser : UdonSharpBehaviour
    {
        public UdonSharpBehaviour[] engines;
        public KeyCode keyboardControl = KeyCode.R;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;

        [System.NonSerialized] public SaccEntity EntityControl;
        [System.NonSerialized] public bool LeftDial;
        [System.NonSerialized] public int DialPosition = -999;

        private bool selected, isPilot;

        public void DFUNC_LeftDial() { }
        public void DFUNC_RightDial() { }
        public void DFUNC_Selected() { selected = true; }
        public void DFUNC_Deselected() { selected = false; }

        public void SFEXT_L_EntityStart()
        {
            SFAEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, false);
            gameObject.SetActive(false);
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            gameObject.SetActive(true);
        }

        public void SFEXT_O_PilotExit()
        {
            isPilot = false;
            selected = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isPilot) return;

            var trigger = Input.GetKey(keyboardControl)
                || (selected && SFAEUtil.IsTriggerPressed(LeftDial));

            foreach (var engine in engines)
            {
                if (!engine) continue;
                var reversing = (bool)engine.GetProgramVariable("reversing");
                var throttleInput = (float)engine.GetProgramVariable("throttleInput");
                if (trigger && !reversing && Mathf.Approximately(throttleInput, 0))
                    engine.SetProgramVariable("reversing", true);
                else if (!trigger && reversing)
                    engine.SetProgramVariable("reversing", false);
            }

            SFAEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, trigger);
        }
    }
}
