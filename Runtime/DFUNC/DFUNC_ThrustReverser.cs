using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_ThrustReverser : UdonSharpBehaviour
    {
        [Tooltip("SAV Controller（SaccAirVehicle）")]
        public UdonSharpBehaviour SAVControl;

        [Tooltip("リバーサー制御用キーボードキー")]
        public KeyCode keyboardControl = KeyCode.R;

        [Tooltip("ダイヤル表示GameObject")]
        public GameObject Dial_Funcon;

        [Tooltip("ダイヤル表示GameObject配列")]
        public GameObject[] Dial_Funcon_Array;

        [System.NonSerialized] public SaccEntity EntityControl;
        [System.NonSerialized] public bool LeftDial;
        [System.NonSerialized] public int DialPosition = -999;

        private bool selected, isPilot;
        private bool triggerLastFrame;

        public void DFUNC_LeftDial() { }
        public void DFUNC_RightDial() { }

        public void DFUNC_Selected()
        {
            selected = true;
            triggerLastFrame = false;
        }

        public void DFUNC_Deselected()
        {
            selected = false;
        }

        public void SFEXT_L_EntityStart()
        {
            if (!SAVControl)
            {
                SAVControl = EntityControl.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            }
            TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, false);
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

        public void KeyboardInput()
        {
            ToggleReverser();
        }

        private void Update()
        {
            // isPilot（左席Owner）または selected（ダイヤル選択中）なら入力処理
            if (!isPilot && !selected) return;

            bool trigger = Input.GetKey(keyboardControl)
                || (selected && TSFEUtil.IsTriggerPressed(LeftDial));

            // トグル動作（押した瞬間のみ）
            if (trigger && !triggerLastFrame)
            {
                ToggleReverser();
            }
            triggerLastFrame = trigger;

            // ダイヤル表示更新
            bool isReversing = IsReverserActive();
            TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, isReversing);
        }

        private void ToggleReverser()
        {
            if (!SAVControl) return;

            int currentInvertThrust = (int)SAVControl.GetProgramVariable("InvertThrust");
            bool isReversing = currentInvertThrust > 0;

            if (isReversing)
            {
                // リバーサー無効化（カウンター-1）
                SAVControl.SetProgramVariable("InvertThrust", currentInvertThrust - 1);
            }
            else
            {
                // リバーサー有効化（カウンター+1）
                SAVControl.SetProgramVariable("InvertThrust", currentInvertThrust + 1);
            }
        }

        private bool IsReverserActive()
        {
            if (!SAVControl) return false;
            int currentInvertThrust = (int)SAVControl.GetProgramVariable("InvertThrust");
            return currentInvertThrust > 0;
        }
    }
}
