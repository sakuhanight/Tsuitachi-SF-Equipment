using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.DFUNC
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

        // 標準状態変数
        private bool isPilot, isOwner, selected, hasPilot;
        private VRCPlayerApi.TrackingDataType trackingTarget;

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

            // LeftDialに応じてtrackingTargetを設定（保険）
            trackingTarget = LeftDial
                ? VRCPlayerApi.TrackingDataType.LeftHand
                : VRCPlayerApi.TrackingDataType.RightHand;

            // エンジンのOwnershipを取得（エンジンが同期を管理）
            foreach (var engine in engines)
            {
                if (engine && !Networking.IsOwner(engine.gameObject))
                {
                    Networking.SetOwner(Networking.LocalPlayer, engine.gameObject);
                }
            }
        }
        public void DFUNC_Deselected() { selected = false; }

        public void SFEXT_L_EntityStart()
        {
            isOwner = Networking.IsOwner(gameObject);
            TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, false);
            gameObject.SetActive(false);
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            isOwner = true;
            selected = false;
        }

        public void SFEXT_O_PilotExit()
        {
            isPilot = false;
            selected = false;
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
            gameObject.SetActive(false);
        }

        public void SFEXT_G_Explode() { ResetStatus(); }
        public void SFEXT_G_RespawnButton() { ResetStatus(); }

        private void ResetStatus()
        {
            // リバーサーをオフに戻す
            foreach (var engine in engines)
            {
                if (engine) engine.SetProgramVariable("reversing", false);
            }
            TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, false);
        }

        private void Update()
        {
            // isPilot（左席Owner）または selected（ダイヤル選択中）なら入力処理
            if (!isPilot && !selected) return;

            var trigger = Input.GetKey(keyboardControl)
                || (selected && TSFEUtil.IsTriggerPressed(LeftDial));

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

            TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, trigger);
        }
    }
}
