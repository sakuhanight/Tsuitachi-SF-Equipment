using UdonSharp;
using UnityEngine;

namespace TSFE.SFEXT
{
    /// <summary>
    /// SFEXT_AdvancedEngine テスト用のモック SaccAirVehicle
    /// 実際の SaccAirVehicle なしでエンジンをテスト可能
    ///
    /// Inspector でリアルタイム値を確認できます
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MockSAVControl : UdonSharpBehaviour
    {
        [Header("SaccAirVehicle Variables (Mock)")]
        [Tooltip("スロットル入力 (0-1)")]
        public float ThrottleInput = 0f;

        [Tooltip("現在の推力 (N)")]
        public float ThrottleStrength = 0f;

        [Tooltip("対気速度 (m/s)")]
        public float AirSpeed = 0f;

        public Animator VehicleAnimator;
        public Transform ControlsRoot;
    }
}
