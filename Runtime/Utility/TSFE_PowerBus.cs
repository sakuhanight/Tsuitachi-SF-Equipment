using UdonSharp;
using UnityEngine;

namespace TSFE.Utility
{
    /// <summary>
    /// 電力バスシステム
    /// APU、エンジン、GPU（地上電源）からの電力供給を統合管理
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TSFE_PowerBus : UdonSharpBehaviour
    {
        [Header("電源コンポーネント")]
        [Tooltip("APUコンポーネント（SFEXT_AuxiliaryPowerUnit）")]
        public UdonSharpBehaviour apuComponent;

        [Tooltip("APUの状態パラメータ名")]
        public string apuParameterName = "Running";

        [Tooltip("エンジンコンポーネント配列（SFEXT_AdvancedEngine）")]
        public UdonSharpBehaviour[] engineComponents;

        [Tooltip("エンジンの状態パラメータ名")]
        public string engineParameterName = "EngineOn";

        [Tooltip("GPU（地上電源）GameObject")]
        public GameObject gpuObject;

        [Header("電力状態")]
        [Tooltip("更新間隔（秒）")]
        public float updateInterval = 0.1f;

        /// <summary>
        /// 電力が供給されているか（読み取り専用）
        /// </summary>
        [System.NonSerialized] public bool Powered = false;

        private float lastUpdateTime = 0f;

        void Update()
        {
            if (Time.time - lastUpdateTime < updateInterval)
                return;

            UpdatePowerState();
            lastUpdateTime = Time.time;
        }

        private void UpdatePowerState()
        {
            bool powered = false;

            // APUチェック
            if (apuComponent != null)
            {
                object apuState = apuComponent.GetProgramVariable(apuParameterName);
                if (apuState != null && apuState is bool && (bool)apuState)
                {
                    powered = true;
                }
            }

            // エンジンチェック（1つでも動作していればOK）
            if (!powered && engineComponents != null)
            {
                foreach (var engine in engineComponents)
                {
                    if (engine == null) continue;
                    object engineState = engine.GetProgramVariable(engineParameterName);
                    if (engineState != null && engineState is bool && (bool)engineState)
                    {
                        powered = true;
                        break;
                    }
                }
            }

            // GPUチェック
            if (!powered && gpuObject != null)
            {
                powered = gpuObject.activeInHierarchy;
            }

            Powered = powered;
        }

        /// <summary>
        /// 手動で電力状態を取得
        /// </summary>
        public bool IsPowered()
        {
            UpdatePowerState();
            return Powered;
        }
    }
}
