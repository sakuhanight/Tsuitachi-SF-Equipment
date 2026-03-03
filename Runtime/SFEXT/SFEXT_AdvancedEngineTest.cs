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
        public float debugUIScale = 1.0f;

        [Header("キーバインド (Desktop)")]
        public KeyCode starterKey = KeyCode.I;
        public KeyCode fuelKey = KeyCode.F;
        public KeyCode reverserKey = KeyCode.R;
        public KeyCode throttleUpKey = KeyCode.RightShift;
        public KeyCode throttleDownKey = KeyCode.RightControl;

        private float throttleInput = 0f;
        private bool isOwner;
        private GUIStyle debugStyle;

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

            if (debugStyle == null)
            {
                debugStyle = new GUIStyle(GUI.skin.box);
                debugStyle.alignment = TextAnchor.UpperLeft;
                debugStyle.fontSize = Mathf.RoundToInt(14 * debugUIScale);
                debugStyle.normal.textColor = Color.white;
            }

            float y = 10;
            float lineHeight = 20 * debugUIScale;
            float width = 350 * debugUIScale;
            float height = 340 * debugUIScale;

            GUI.Box(new Rect(10, y, width, height), "", debugStyle);

            y += 10;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"<b>SFEXT_AdvancedEngine Test</b>", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"Starter: {engine.starter} [{starterKey}]", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"Fuel: {engine.fuel} [{fuelKey}]", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"Throttle: {throttleInput:F2} [{throttleUpKey}/{throttleDownKey}]", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"Reversing: {engine.reversing} [{reverserKey}]", debugStyle);
            y += lineHeight;

            y += lineHeight / 2;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"<b>Engine State:</b>", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"N1: {engine.N1:F1} RPM ({engine.N1 / engine.takeOffN1 * 100:F1}%)", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"N2: {engine.N2:F1} RPM ({engine.N2 / engine.takeOffN2 * 100:F1}%)", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"EGT: {engine.EGT:F0} °C", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"ECT: {engine.ECT:F0} °C", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"Fire: {engine.fire}", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"Engine On: {engine.EngineOn}", debugStyle);
            y += lineHeight;

            y += lineHeight / 2;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"<b>Controls:</b>", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"{starterKey}: Toggle Starter", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"{fuelKey}: Toggle Fuel", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"{throttleUpKey}: Increase Throttle", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"{throttleDownKey}: Decrease Throttle", debugStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"{reverserKey}: Toggle Reverser", debugStyle);
        }
    }
}
