using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using TSFE.Utility;

namespace TSFE.SFEXT
{
    /// <summary>
    /// SFEXT_AuxiliaryPowerUnit のテスト用スクリプト
    /// 高度制限テスト機能付き
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SFEXT_AuxiliaryPowerUnitTest : UdonSharpBehaviour
    {
        [Header("テスト対象")]
        public SFEXT_AuxiliaryPowerUnit apu;

        [Header("UI表示 (任意)")]
        public Text debugText;

        [Header("キーバインド (Desktop)")]
        public KeyCode apuToggleKey = KeyCode.A;

        private bool isOwner;

        void Start()
        {
            isOwner = Networking.IsOwner(gameObject);
        }

        void Update()
        {
            if (!isOwner) return;
            if (apu == null) return;

            // キーボード入力
            if (Input.GetKeyDown(apuToggleKey))
            {
                apu.ToggleAPU();
                RequestSerialization();
            }

            // UI更新
            UpdateDebugUI();
        }

        private void UpdateDebugUI()
        {
            if (debugText == null || apu == null) return;

            string text = "SFEXT_AuxiliaryPowerUnit Test\n\n";
            text += "Controls:\n";
            text += apuToggleKey + ": Toggle APU\n\n";
            text += "APU State:\n";
            text += "State: " + apu.State.ToString() + "\n";
            text += "Running: " + (apu.State == APUState.Running ? "YES" : "NO") + "\n\n";

            // 高度制限情報
            if (apu.SAVControl != null)
            {
                var altitudeObj = apu.SAVControl.GetProgramVariable("Altitude");
                if (altitudeObj != null)
                {
                    float altitude = (float)altitudeObj;
                    float altFt = TSFEUtil.ToFeet(altitude);
                    float maxAltFt = TSFEUtil.ToFeet(apu.maxOperatingAltitude);
                    bool withinLimit = altitude <= apu.maxOperatingAltitude;

                    text += "Altitude Limit:\n";
                    text += "Current: " + altitude.ToString("F0") + " m (FL" + (altFt / 100).ToString("F0") + ")\n";
                    text += "Max: " + apu.maxOperatingAltitude.ToString("F0") + " m (FL" + (maxAltFt / 100).ToString("F0") + ")\n";
                    text += "Status: " + (withinLimit ? "OK" : "EXCEEDED") + "\n";
                }
            }

            debugText.text = text;
        }
    }
}
