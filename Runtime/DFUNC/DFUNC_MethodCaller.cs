using UdonSharp;
using UnityEngine;

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

        [Header("実行タイミング")]
        [Tooltip("ダイヤル選択時に実行（DFUNC_Selected）")]
        public bool executeOnSelected = false;

        [Tooltip("ダイヤル選択解除時に実行（DFUNC_Deselected）")]
        public bool executeOnDeselected = false;

        [Tooltip("左ダイヤル回転時に実行（DFUNC_LeftDial）")]
        public bool executeOnLeftDial = false;

        [Tooltip("右ダイヤル回転時に実行（DFUNC_RightDial）")]
        public bool executeOnRightDial = false;

        [Tooltip("VRトリガー押下時に実行（入力値 > 0.75）")]
        public bool executeOnTriggerPress = false;

        [Header("キー入力設定")]
        [Tooltip("キーボード入力で実行")]
        public bool executeOnKeyDown = false;

        [Tooltip("実行するKeyCode")]
        public KeyCode keyCode = KeyCode.None;

        [Header("表示設定")]
        [Tooltip("選択中に有効化するGameObject（単一）")]
        public GameObject Dial_Funcon;

        [Tooltip("選択中に有効化するGameObject（配列）")]
        public GameObject[] Dial_Funcon_Array;

        // SaccFlightAndVehicles自動注入フィールド
        [System.NonSerialized] public UdonSharpBehaviour EntityControl;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public bool LeftDial = false;

        private bool isSelected = false;
        private bool isPilot = false;
        private bool prevTriggerPressed = false;

        void Start()
        {
            Debug.Log($"[DFUNC_MethodCaller] Start called on {gameObject.name}, isPilot={isPilot}");
            // Funconの初期状態を非表示にする
            TSFE.Utility.TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, false);

            // パイロットがいない場合のみGameObjectを無効化
            // （SFEXT_O_PilotEnterでisPilotがtrueになった後は無効化しない）
            if (!isPilot)
            {
                Debug.Log($"[DFUNC_MethodCaller] Deactivating GameObject (no pilot)");
                gameObject.SetActive(false);
            }
        }

        // ========================================
        // SaccFlightAndVehicles イベント
        // ========================================

        public void SFEXT_G_PilotEnter()
        {
            Debug.Log($"[DFUNC_MethodCaller] SFEXT_G_PilotEnter called on {gameObject.name}");
            gameObject.SetActive(true);
        }

        public void SFEXT_G_PilotExit()
        {
            Debug.Log($"[DFUNC_MethodCaller] SFEXT_G_PilotExit called on {gameObject.name}");
            gameObject.SetActive(false);
        }

        public void SFEXT_O_PilotEnter()
        {
            Debug.Log($"[DFUNC_MethodCaller] SFEXT_O_PilotEnter called, isPilot = true");
            isPilot = true;
        }

        public void SFEXT_O_PilotExit()
        {
            Debug.Log($"[DFUNC_MethodCaller] SFEXT_O_PilotExit called, isPilot = false");
            isPilot = false;
        }

        // ========================================
        // DFUNC イベント
        // ========================================

        public void DFUNC_Selected()
        {
            isSelected = true;
            TSFE.Utility.TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, true);

            if (executeOnSelected)
            {
                ExecuteMethod();
            }
        }

        public void DFUNC_Deselected()
        {
            isSelected = false;
            TSFE.Utility.TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, false);

            if (executeOnDeselected)
            {
                ExecuteMethod();
            }
        }

        public void DFUNC_LeftDial()
        {
            if (executeOnLeftDial)
            {
                ExecuteMethod();
            }
        }

        public void DFUNC_RightDial()
        {
            if (executeOnRightDial)
            {
                ExecuteMethod();
            }
        }

        void Update()
        {
            // VRトリガー入力チェック（選択中のみ）
            if (isSelected && executeOnTriggerPress)
            {
                bool triggerPressed = TSFE.Utility.TSFEUtil.IsTriggerPressed(LeftDial);
                if (triggerPressed && !prevTriggerPressed)
                {
                    Debug.Log("[DFUNC_MethodCaller] VR Trigger pressed");
                    ExecuteMethod();
                }
                prevTriggerPressed = triggerPressed;
            }

            // キー入力チェック（パイロット時のみ）
            if (isPilot && executeOnKeyDown && keyCode != KeyCode.None)
            {
                if (Input.GetKeyDown(keyCode))
                {
                    Debug.Log($"[DFUNC_MethodCaller] Key pressed: {keyCode}");
                    ExecuteMethod();
                }
            }
        }

        /// <summary>
        /// 公開メソッド：外部から直接呼び出し可能
        /// </summary>
        public void Execute()
        {
            ExecuteMethod();
        }

        /// <summary>
        /// 実際のメソッド実行
        /// </summary>
        private void ExecuteMethod()
        {
            if (targetComponent == null)
            {
                Debug.LogWarning("[DFUNC_MethodCaller] Target component is null");
                return;
            }

            if (string.IsNullOrEmpty(methodName))
            {
                Debug.LogWarning("[DFUNC_MethodCaller] Method name is empty");
                return;
            }

            Debug.Log($"[DFUNC_MethodCaller] Calling {targetComponent.GetType().Name}.{methodName}()");
            targetComponent.SendCustomEvent(methodName);
        }
    }
}
