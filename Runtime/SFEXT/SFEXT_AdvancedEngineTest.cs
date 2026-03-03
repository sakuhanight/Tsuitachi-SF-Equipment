using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TSFE.SFEXT
{
    /// <summary>
    /// SFEXT_AdvancedEngine のテスト用スクリプト
    /// デスクトップ/VRで手動操作してエンジン動作を確認
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SFEXT_AdvancedEngineTest : UdonSharpBehaviour
    {
        [Header("テスト対象")]
        public SFEXT_AdvancedEngine engine;

        [Header("デバッグ表示")]
        public bool showDebugInfo = true;

        [Header("キーバインド (Desktop)")]
        public KeyCode starterKey = KeyCode.I;
        public KeyCode fuelKey = KeyCode.F;
        public KeyCode reverserKey = KeyCode.R;
        public KeyCode throttleUpKey = KeyCode.RightShift;
        public KeyCode throttleDownKey = KeyCode.RightControl;

        private float throttleInput = 0f;
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
                RequestSerialization();
            }

            if (Input.GetKeyDown(fuelKey))
            {
                engine.fuel = !engine.fuel;
                RequestSerialization();
            }

            if (Input.GetKeyDown(reverserKey))
            {
                engine.reversing = !engine.reversing;
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
        }

        void OnGUI()
        {
            if (!showDebugInfo || engine == null) return;

            float y = 10;
            float lineHeight = 20;
            float width = 350;
            float height = 340;

            GUI.Box(new Rect(10, y, width, height), "");

            y += 10;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), "SFEXT_AdvancedEngine Test");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "Starter: " + engine.starter + " [" + starterKey + "]");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "Fuel: " + engine.fuel + " [" + fuelKey + "]");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "Throttle: " + throttleInput.ToString("F2") + " [" + throttleUpKey + "/" + throttleDownKey + "]");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "Reversing: " + engine.reversing + " [" + reverserKey + "]");
            y += lineHeight;

            y += lineHeight / 2;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), "Engine State:");
            y += lineHeight;

            float n1Pct = engine.N1 / engine.takeOffN1 * 100f;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), "N1: " + engine.N1.ToString("F1") + " RPM (" + n1Pct.ToString("F1") + "%)");
            y += lineHeight;

            float n2Pct = engine.N2 / engine.takeOffN2 * 100f;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), "N2: " + engine.N2.ToString("F1") + " RPM (" + n2Pct.ToString("F1") + "%)");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "EGT: " + engine.EGT.ToString("F0") + " C");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "ECT: " + engine.ECT.ToString("F0") + " C");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "Fire: " + engine.fire);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "Engine On: " + engine.EngineOn);
            y += lineHeight;

            y += lineHeight / 2;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), "Controls:");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), starterKey + ": Toggle Starter");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), fuelKey + ": Toggle Fuel");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), throttleUpKey + ": Increase Throttle");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), throttleDownKey + ": Decrease Throttle");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), reverserKey + ": Toggle Reverser");
        }
    }
}
