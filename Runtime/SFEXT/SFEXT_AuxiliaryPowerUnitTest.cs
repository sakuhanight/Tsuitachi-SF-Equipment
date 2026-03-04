using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace TSFE.SFEXT
{
    /// <summary>
    /// SFEXT_AuxiliaryPowerUnit のテスト用スクリプト
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
            text += "Started: " + (apu.started ? "YES" : "NO") + "\n";
            text += "Terminated: " + (apu.terminated ? "YES" : "NO");

            debugText.text = text;
        }
    }
}
