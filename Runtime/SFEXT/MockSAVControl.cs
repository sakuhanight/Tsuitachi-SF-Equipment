using UdonSharp;
using UnityEngine;

namespace TSFE.SFEXT
{
    /// <summary>
    /// SFEXT_AdvancedEngine テスト用のモック SaccAirVehicle
    /// 実際の SaccAirVehicle なしでエンジンをテスト可能
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MockSAVControl : UdonSharpBehaviour
    {
        [Header("SaccAirVehicle Variables (Mock)")]
        public float ThrottleInput = 0f;
        public float ThrottleStrength = 0f;
        public float AirSpeed = 0f;
        public Animator VehicleAnimator;
        public Transform ControlsRoot;

        [Header("Debug")]
        public bool showDebug = true;

        void OnGUI()
        {
            if (!showDebug) return;

            float y = 360;
            float lineHeight = 20;
            float width = 350;
            float height = 80;

            GUI.Box(new Rect(10, y, width, height), "");
            y += 10;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "Mock SAVControl");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "ThrottleStrength: " + ThrottleStrength.ToString("F2") + " N");
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "AirSpeed: " + AirSpeed.ToString("F1") + " m/s");
        }
    }
}
