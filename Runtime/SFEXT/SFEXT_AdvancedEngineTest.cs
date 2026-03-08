using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace TSFE.SFEXT
{
    /// <summary>
    /// SFEXT_AdvancedEngine のテスト用スクリプト（複数エンジン対応）
    /// デスクトップ/VRでスロットル操作してエンジン動作を確認
    ///
    /// ・スロットル/リバーサーコントロール: このスクリプトで全エンジン一括制御
    /// ・スターター/燃料: 各エンジンのInspectorで個別操作
    /// ・状態表示: このスクリプトでサマリー表示、各エンジンのInspectorで詳細確認
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SFEXT_AdvancedEngineTest : UdonSharpBehaviour
    {
        [Header("テスト対象エンジン (複数可)")]
        [Tooltip("スロットルを適用する全てのエンジン")]
        public SFEXT_AdvancedEngine[] engines;

        [Header("UI表示 (任意)")]
        public Text debugText;

        [Header("キーバインド (Desktop)")]
        public KeyCode throttleUpKey = KeyCode.RightShift;
        public KeyCode throttleDownKey = KeyCode.RightControl;
        public KeyCode reverserKey = KeyCode.R;

        [Header("現在の状態 (読み取り専用)")]
        public float throttleInput = 0f;
        public bool reversing = false;

        private bool isOwner;
        private UdonSharpBehaviour savControl;

        void Start()
        {
            isOwner = Networking.IsOwner(gameObject);

            // 最初のエンジンからSAVControlを取得
            if (engines != null && engines.Length > 0 && engines[0] != null)
            {
                savControl = engines[0].SAVControl;
            }
        }

        void Update()
        {
            if (!isOwner) return;

            // SAVControlから現在のスロットル値を読み取り
            if (savControl != null)
            {
                throttleInput = (float)savControl.GetProgramVariable("ThrottleInput");
            }

            // キーボード入力でスロットル変更
            if (Input.GetKey(throttleUpKey))
            {
                throttleInput = Mathf.Clamp01(throttleInput + Time.deltaTime * 0.5f);
                if (savControl != null)
                {
                    savControl.SetProgramVariable("ThrottleInput", throttleInput);
                }
            }

            if (Input.GetKey(throttleDownKey))
            {
                throttleInput = Mathf.Clamp01(throttleInput - Time.deltaTime * 0.5f);
                if (savControl != null)
                {
                    savControl.SetProgramVariable("ThrottleInput", throttleInput);
                }
            }

            // リバーサー操作（全エンジン一括）
            if (Input.GetKeyDown(reverserKey))
            {
                reversing = !reversing;
                if (engines != null)
                {
                    foreach (var engine in engines)
                    {
                        if (engine != null)
                        {
                            engine.reversing = reversing;
                            engine.RequestSerialization();
                        }
                    }
                }
                RequestSerialization();
            }

            // UI更新
            UpdateDebugUI();
        }

        private void UpdateDebugUI()
        {
            if (debugText == null || engines == null || engines.Length == 0) return;

            string text = "SFEXT_AdvancedEngine Test (Multiple Engines)\n\n";
            text += "Throttle Control:\n";
            text += throttleUpKey + "/" + throttleDownKey + ": Throttle [" + throttleInput.ToString("F2") + "]\n";
            text += reverserKey + ": Reverser [" + (reversing ? "ON" : "OFF") + "]\n\n";

            text += "Individual engine controls (Starter/Fuel):\n";
            text += "Use each engine's Inspector to control individually\n\n";

            // 各エンジンの状態を表示
            for (int i = 0; i < engines.Length; i++)
            {
                var engine = engines[i];
                if (engine == null) continue;

                float n1Pct = engine.N1 / engine.takeOffN1 * 100f;
                float n2Pct = engine.N2 / engine.takeOffN2 * 100f;

                // 推力計算（エンジン本体と同じロジック）
                float thrustRatio = 0f;
                if (engine.N1 >= engine.idleN1)
                {
                    float t = (engine.N1 - engine.idleN1) / (engine.takeOffN1 - engine.idleN1);
                    thrustRatio = Mathf.Lerp(engine.idleThrustRatio, 1f, Mathf.Pow(t, engine.thrustCurve));
                }
                float thrust = engine.maxThrust * thrustRatio;
                if (engine.reversing) thrust *= -engine.reverserRatio;

                // EngineStateの文字列変換
                string stateStr = engine.State.ToString();

                text += "--- Engine " + (i + 1) + " [" + stateStr + "] ---\n";
                text += "N1: " + engine.N1.ToString("F1") + " RPM (" + n1Pct.ToString("F1") + "%)\n";
                text += "N2: " + engine.N2.ToString("F1") + " RPM (" + n2Pct.ToString("F1") + "%)\n";
                text += "EGT: " + engine.EGT.ToString("F0") + " C | ECT: " + engine.ECT.ToString("F0") + " C\n";
                text += "Thrust: " + thrust.ToString("F1") + " N\n";
                text += "Starter: " + (engine.starter ? "ON" : "OFF") + " | Fuel: " + (engine.fuel ? "ON" : "OFF") + "\n";
                text += "Fire: " + (engine.fire ? "YES" : "NO") + " | Seized: " + (engine.State == SFEXT.EngineState.Seized ? "YES" : "NO") + "\n";
                if (engine.fireHandlePulled) text += "FIRE HANDLE: PULLED\n";
                text += "\n";
            }

            debugText.text = text;
        }
    }
}
