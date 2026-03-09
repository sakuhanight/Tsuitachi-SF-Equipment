using UdonSharp;
using UnityEngine;

namespace TSFE.Utility
{
    /// <summary>
    /// テストシナリオ定義
    /// MockSAVControlとエンジン/APUシステムのテスト用
    /// </summary>
    [System.Serializable]
    public class TestScenario
    {
        [Header("シナリオ設定")]
        [Tooltip("シナリオ名")]
        public string scenarioName = "Test Scenario";

        [Tooltip("説明")]
        [TextArea(2, 4)]
        public string description = "";

        [Header("飛行条件")]
        [Tooltip("高度 (メートル)")]
        public float altitude = 0f;

        [Tooltip("対気速度 (m/s)")]
        public float airSpeed = 0f;

        [Tooltip("大気密度 (0-1)")]
        [Range(0f, 1f)]
        public float atmosphere = 1f;

        [Tooltip("地上走行中")]
        public bool taxiing = true;

        [Header("燃料")]
        [Tooltip("燃料量 (%)")]
        [Range(0f, 100f)]
        public float fuelPercent = 100f;

        [Header("実行パラメータ")]
        [Tooltip("シナリオ実行前の待機時間 (秒)")]
        public float preWaitTime = 0f;

        [Tooltip("シナリオ実行後の待機時間 (秒)")]
        public float postWaitTime = 5f;

        [Tooltip("期待される結果の検証タイムアウト (秒)")]
        public float verificationTimeout = 60f;

        [Header("期待される結果")]
        [Tooltip("APUが起動すべきか")]
        public bool expectAPURunning = false;

        [Tooltip("全エンジンが起動すべきか")]
        public bool expectAllEnginesRunning = false;
    }
}
