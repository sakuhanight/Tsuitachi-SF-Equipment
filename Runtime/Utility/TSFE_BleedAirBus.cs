using UdonSharp;
using UnityEngine;

namespace TSFE.Utility
{
    /// <summary>
    /// ブリード空気バスシステム
    /// APU、エンジン、ASU（地上空調車）からのブリード空気供給を統合管理
    /// エンジンスターター（空気タービン式）に使用
    /// GameObject参照による高速判定
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TSFE_BleedAirBus : UdonSharpBehaviour
    {
        [Header("ブリード空気源状態GameObject")]
        [Tooltip("APU起動状態GameObject（APUが起動中に有効化されているGameObject）")]
        public GameObject apuRunningIndicator;

        [Tooltip("エンジン起動状態GameObject配列（エンジンが起動中に有効化されているGameObject）")]
        public GameObject[] engineRunningIndicators;

        [Tooltip("ASU（地上空調車）GameObject（接続中に有効化）")]
        public GameObject asuObject;

        [Header("ブリード空気状態")]
        [Tooltip("更新間隔（秒）")]
        public float updateInterval = 0.1f;

        [Tooltip("ブリード空気供給中に有効化するGameObject（任意）")]
        public GameObject bleedAirIndicator;

        /// <summary>
        /// ブリード空気が供給されているか（読み取り専用）
        /// </summary>
        [System.NonSerialized] public bool BleedAirAvailable = false;

        private float lastUpdateTime = 0f;

        void Update()
        {
            if (Time.time - lastUpdateTime < updateInterval)
                return;

            UpdateBleedAirState();
            lastUpdateTime = Time.time;
        }

        private void UpdateBleedAirState()
        {
            bool available = false;

            // APUチェック
            if (apuRunningIndicator != null && apuRunningIndicator.activeInHierarchy)
            {
                available = true;
                BleedAirAvailable = available;
                if (bleedAirIndicator != null) bleedAirIndicator.SetActive(true);
                return;
            }

            // エンジンチェック（1つでも動作していればOK、クロスブリード）
            if (engineRunningIndicators != null)
            {
                foreach (var engineIndicator in engineRunningIndicators)
                {
                    if (engineIndicator == null) continue;
                    if (engineIndicator.activeInHierarchy)
                    {
                        available = true;
                        BleedAirAvailable = available;
                        if (bleedAirIndicator != null) bleedAirIndicator.SetActive(true);
                        return;
                    }
                }
            }

            // ASUチェック
            if (asuObject != null && asuObject.activeInHierarchy)
            {
                available = true;
            }

            BleedAirAvailable = available;

            // インジケータGameObject更新
            if (bleedAirIndicator != null)
            {
                bleedAirIndicator.SetActive(available);
            }
        }

        /// <summary>
        /// 手動でブリード空気状態を取得
        /// </summary>
        public bool IsBleedAirAvailable()
        {
            UpdateBleedAirState();
            return BleedAirAvailable;
        }
    }
}
