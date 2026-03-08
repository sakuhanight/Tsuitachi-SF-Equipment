using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;

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
        [Tooltip("トグル時に有効/無効を切り替えるGameObject配列")]
        public GameObject[] Dial_Funcon;

        // SaccFlightAndVehicles自動注入フィールド
        [System.NonSerialized] public SaccEntity EntityControl;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public bool LeftDial = false;

        private bool isSelected = false;
        private bool isPilot = false;
        private bool prevTriggerPressed = false;

        public void SFEXT_L_EntityStart()
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer != null && !EntityControl.IsOwner)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
            }

            Debug.Log($"[DFUNC_MethodCaller] SFEXT_L_EntityStart: IsOwner={EntityControl.IsOwner}, active={gameObject.activeInHierarchy}");
            // Funconの初期状態を非表示にする
            if (Dial_Funcon != null)
            {
                for (int i = 0; i < Dial_Funcon.Length; i++)
                {
                    if (Dial_Funcon[i] != null)
                    {
                        Dial_Funcon[i].SetActive(false);
                    }
                }
            }
        }

        void Start()
        {
            Debug.Log($"[DFUNC_MethodCaller] Start: isPilot={isPilot}, EntityControl={(EntityControl != null ? "OK" : "NULL")}");
        }

        void Update()
        {
            // 常時デバッグ（60フレームごと）
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[DFUNC_MethodCaller] Update: isPilot={isPilot}, isSelected={isSelected}, LeftDial={LeftDial}");
            }

            if (isPilot)
            {
                // VRトリガー入力チェック（選択中のみ、該当する側のトリガーのみ）
                if (isSelected)
                {
                    float trigger;
                    if (LeftDial)
                    {
                        trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                    }
                    else
                    {
                        trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                    }

                    // デバッグ: トリガー値表示
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[DFUNC_MethodCaller] Trigger value: {trigger:F3}");
                    }

                    // トリガー押下判定（0.75以上で押下）
                    bool triggerPressed = trigger > 0.75f;
                    if (triggerPressed && !prevTriggerPressed)
                    {
                        Debug.Log($"[DFUNC_MethodCaller] Trigger pressed! Executing method.");
                        ExecuteMethod();
                    }
                    prevTriggerPressed = triggerPressed;
                }
                else
                {
                    prevTriggerPressed = false;
                }

                // キー入力チェック
                if (keyCode != KeyCode.None && Input.GetKeyDown(keyCode))
                {
                    Debug.Log($"[DFUNC_MethodCaller] Key pressed: {keyCode}");
                    ExecuteMethod();
                }
            }
        }

        // ========================================
        // SaccFlightAndVehicles イベント
        // ========================================

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            Debug.Log($"[DFUNC_MethodCaller] SFEXT_O_PilotEnter: isPilot={isPilot}");
        }

        public void SFEXT_O_PilotExit()
        {
            isPilot = false;
            Debug.Log($"[DFUNC_MethodCaller] SFEXT_O_PilotExit: isPilot={isPilot}");
        }

        public void SFEXT_O_TakeOwnership()
        {
            gameObject.SetActive(true);
            Debug.Log($"[DFUNC_MethodCaller] SFEXT_O_TakeOwnership: active={gameObject.activeInHierarchy}");
        }

        public void SFEXT_O_LoseOwnership()
        {
            gameObject.SetActive(false);
            Debug.Log($"[DFUNC_MethodCaller] SFEXT_O_LoseOwnership: active={gameObject.activeInHierarchy}");
        }

        // ========================================
        // DFUNC イベント
        // ========================================

        public void DFUNC_Selected()
        {
            Debug.Log($"[DFUNC_MethodCaller] DFUNC_Selected called");
            isSelected = true;
        }

        public void DFUNC_Deselected()
        {
            Debug.Log($"[DFUNC_MethodCaller] DFUNC_Deselected called");
            isSelected = false;
        }

        public void DFUNC_LeftDial()
        {
            // 標準DFUNCでは使用しない（Updateでトリガー監視）
        }

        public void DFUNC_RightDial()
        {
            // 標準DFUNCでは使用しない（Updateでトリガー監視）
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

            // メソッド実行後、FUNCONの状態を切り替え
            ToggleFunconDisplay();
        }

        /// <summary>
        /// Funcon表示を切り替え（トグル）
        /// </summary>
        private void ToggleFunconDisplay()
        {
            if (Dial_Funcon != null)
            {
                for (int i = 0; i < Dial_Funcon.Length; i++)
                {
                    if (Dial_Funcon[i] != null)
                    {
                        Dial_Funcon[i].SetActive(!Dial_Funcon[i].activeSelf);
                    }
                }
            }
        }
    }
}
