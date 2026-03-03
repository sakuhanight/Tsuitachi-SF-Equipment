using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace TSFE.SFEXT
{
    /// <summary>
    /// SFEXT_AdvancedEngine のテスト用スクリプト
    /// デスクトップ/VRで手動操作してエンジン動作を確認
    ///
    /// Inspector でエンジンの状態をリアルタイム確認できます
    /// または debugText (UI Text) を設定して画面表示も可能
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SFEXT_AdvancedEngineTest : UdonSharpBehaviour
    {
        [Header("テスト対象")]
        public SFEXT_AdvancedEngine engine;

        [Header("UI表示 (任意)")]
        public Text debugText;

        [Header("キーバインド (Desktop)")]
        public KeyCode starterKey = KeyCode.I;
        public KeyCode fuelKey = KeyCode.F;
        public KeyCode reverserKey = KeyCode.R;
        public KeyCode throttleUpKey = KeyCode.RightShift;
        public KeyCode throttleDownKey = KeyCode.RightControl;

        [Header("現在の状態 (読み取り専用)")]
        public float throttleInput = 0f;
        public bool starter = false;
        public bool fuel = false;
        public bool reversing = false;

        private bool isOwner;

        void Start()
        {
            isOwner = Networking.IsOwner(gameObject);
        }

        void Update()
        {
            if (!isOwner) return;
            if (engine == null) return;

            // キーボード入力
            if (Input.GetKeyDown(starterKey))
            {
                engine.starter = !engine.starter;
                starter = engine.starter;
                RequestSerialization();
            }

            if (Input.GetKeyDown(fuelKey))
            {
                engine.fuel = !engine.fuel;
                fuel = engine.fuel;
                RequestSerialization();
            }

            if (Input.GetKeyDown(reverserKey))
            {
                engine.reversing = !engine.reversing;
                reversing = engine.reversing;
                RequestSerialization();
            }

            if (Input.GetKey(throttleUpKey))
            {
                throttleInput = Mathf.Clamp01(throttleInput + Time.deltaTime * 0.5f);
            }

            if (Input.GetKey(throttleDownKey))
            {
                throttleInput = Mathf.Clamp01(throttleInput - Time.deltaTime * 0.5f);
            }

            // モックSAVControlにスロットル反映
            if (engine.SAVControl != null)
            {
                engine.SAVControl.SetProgramVariable("ThrottleInput", throttleInput);
            }

            // UI更新
            UpdateDebugUI();
        }

        private void UpdateDebugUI()
        {
            if (debugText == null || engine == null) return;

            float n1Pct = engine.N1 / engine.takeOffN1 * 100f;
            float n2Pct = engine.N2 / engine.takeOffN2 * 100f;

            string text = "SFEXT_AdvancedEngine Test\n\n";
            text += "Controls:\n";
            text += starterKey + ": Starter [" + (engine.starter ? "ON" : "OFF") + "]\n";
            text += fuelKey + ": Fuel [" + (engine.fuel ? "ON" : "OFF") + "]\n";
            text += throttleUpKey + "/" + throttleDownKey + ": Throttle [" + throttleInput.ToString("F2") + "]\n";
            text += reverserKey + ": Reverser [" + (engine.reversing ? "ON" : "OFF") + "]\n\n";
            text += "Engine State:\n";
            text += "N1: " + engine.N1.ToString("F1") + " RPM (" + n1Pct.ToString("F1") + "%)\n";
            text += "N2: " + engine.N2.ToString("F1") + " RPM (" + n2Pct.ToString("F1") + "%)\n";
            text += "EGT: " + engine.EGT.ToString("F0") + " C\n";
            text += "ECT: " + engine.ECT.ToString("F0") + " C\n";
            text += "Fire: " + (engine.fire ? "YES" : "NO") + "\n";
            text += "Engine On: " + (engine.EngineOn ? "YES" : "NO");

            debugText.text = text;
        }
    }
}
