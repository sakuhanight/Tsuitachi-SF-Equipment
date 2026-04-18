using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.DFUNC
{
    /// <summary>
    /// 汎用メソッド呼び出しDFUNC
    /// 任意のUdonSharpBehaviourコンポーネントの公開メソッドを呼び出す
    /// VRダイヤル操作とキーボード入力に対応
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_MethodCaller : UdonSharpBehaviour
    {
        [Header("対象コンポーネント")]
        [Tooltip("メソッドを呼び出すUdonSharpBehaviourコンポーネント")]
        public UdonSharpBehaviour targetComponent;

        [Header("メソッド設定")]
        [Tooltip("呼び出すメソッド名（引数なしのpublicメソッド）")]
        public string methodName;

        [Header("入力設定")]
        [Tooltip("デスクトップ用キーコード")]
        public KeyCode keyCode = KeyCode.G;

        [Header("表示設定")]
        [Tooltip("ダイヤル表示GameObject")]
        public GameObject Dial_Funcon;
        [Tooltip("ダイヤル表示GameObject配列")]
        public GameObject[] Dial_Funcon_Array;

        // SaccFlightAndVehicles自動注入フィールド
        [System.NonSerialized] public SaccEntity EntityControl;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public bool LeftDial = false;

        // 標準状態変数
        private bool isPilot, isOwner, selected, hasPilot;
        private VRCPlayerApi.TrackingDataType trackingTarget;

        // コンポーネント固有の状態
        private bool prevTriggerPressed = false;

        public void SFEXT_L_EntityStart()
        {
            isOwner = Networking.IsOwner(gameObject);
            TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, false);
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!isPilot && !selected) return;

            // VRトリガー入力
            if (selected)
            {
                bool triggerPressed = TSFEUtil.IsTriggerPressed(LeftDial);
                if (triggerPressed && !prevTriggerPressed)
                {
                    ExecuteMethod();
                }
                prevTriggerPressed = triggerPressed;
            }
            else
            {
                prevTriggerPressed = false;
            }

            // キー入力
            if (keyCode != KeyCode.None && Input.GetKeyDown(keyCode))
            {
                ExecuteMethod();
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

            // LeftDialに応じてtrackingTargetを設定（保険）
            trackingTarget = LeftDial
                ? VRCPlayerApi.TrackingDataType.LeftHand
                : VRCPlayerApi.TrackingDataType.RightHand;
        }

        public void DFUNC_Deselected()
        {
            selected = false;
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
            TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, false);
        }

        /// <summary>
        /// 公開メソッド：外部から直接呼び出し可能
        /// </summary>
        public void Execute()
        {
            ExecuteMethod();
        }

        private void ExecuteMethod()
        {
            if (!targetComponent || string.IsNullOrEmpty(methodName)) return;

            targetComponent.SendCustomEvent(methodName);

            // Funcon表示をトグル
            bool isActive = Dial_Funcon ? Dial_Funcon.activeSelf : (Dial_Funcon_Array != null && Dial_Funcon_Array.Length > 0 ? Dial_Funcon_Array[0].activeSelf : false);
            TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, !isActive);
        }
    }
}
