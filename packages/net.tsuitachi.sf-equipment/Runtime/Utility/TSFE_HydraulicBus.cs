using UdonSharp;
using UnityEngine;

namespace TSFE.Utility
{
    /// <summary>
    /// 油圧バスシステム
    /// 複数のHydraulicPumpから油圧供給を統合管理
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TSFE_HydraulicBus : UdonSharpBehaviour
    {
        [Header("油圧ポンプ")]
        [Tooltip("油圧ポンプ配列（TSFE_HydraulicPump）")]
        public TSFE_HydraulicPump[] hydraulicPumps;

        [Header("油圧状態")]
        [Tooltip("更新間隔（秒）")]
        public float updateInterval = 0.1f;

        [Tooltip("必要な最小動作ポンプ数（冗長性）")]
        public int minimumRunningPumps = 1;

        /// <summary>
        /// 油圧が供給されているか（読み取り専用）
        /// </summary>
        [System.NonSerialized] public bool Pressurized = false;

        /// <summary>
        /// 動作中のポンプ数（読み取り専用）
        /// </summary>
        [System.NonSerialized] public int RunningPumpCount = 0;

        private float lastUpdateTime = 0f;

        void Update()
        {
            if (Time.time - lastUpdateTime < updateInterval)
                return;

            UpdateHydraulicState();
            lastUpdateTime = Time.time;
        }

        private void UpdateHydraulicState()
        {
            int runningCount = 0;

            if (hydraulicPumps != null)
            {
                foreach (var pump in hydraulicPumps)
                {
                    if (pump == null) continue;
                    if (pump.Running) runningCount++;
                }
            }

            RunningPumpCount = runningCount;
            Pressurized = runningCount >= minimumRunningPumps;
        }

        /// <summary>
        /// 手動で油圧状態を取得
        /// </summary>
        public bool IsPressurized()
        {
            UpdateHydraulicState();
            return Pressurized;
        }

        /// <summary>
        /// 動作中のポンプ数を取得
        /// </summary>
        public int GetRunningPumpCount()
        {
            UpdateHydraulicState();
            return RunningPumpCount;
        }
    }
}
