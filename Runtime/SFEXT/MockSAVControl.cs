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
        public bool showThrottleStrength = true;

        void OnGUI()
        {
            if (!showThrottleStrength) return;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 14;
            style.normal.textColor = Color.cyan;

            float y = 360;
            float lineHeight = 20;
            float width = 350;
            float height = 60;

            GUI.Box(new Rect(10, y, width, height), "", style);
            y += 10;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), "<b>Mock SAVControl</b>", style);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"ThrottleStrength: {ThrottleStrength:F2} N", style);
            y += lineHeight;

            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"AirSpeed: {AirSpeed:F1} m/s", style);
        }
    }
}
