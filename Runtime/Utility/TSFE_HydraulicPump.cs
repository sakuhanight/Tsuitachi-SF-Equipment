using UdonSharp;
using UnityEngine;

namespace TSFE.Utility
{
    /// <summary>
    /// 油圧ポンプコンポーネント
    /// エンジン駆動、電動、RAT（Ram Air Turbine）に対応
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TSFE_HydraulicPump : UdonSharpBehaviour
    {
        public enum PumpType
        {
            EngineDriven,   // エンジン駆動（N2回転数）
            Electric,       // 電動（電力バス必要）
            RAT             // Ram Air Turbine（速度＋展開状態）
        }

        [Header("ポンプ設定")]
        [Tooltip("ポンプ種類")]
        public PumpType pumpType = PumpType.EngineDriven;

        [Header("エンジン駆動設定（EngineDrivenの場合）")]
        [Tooltip("駆動元エンジン（SFEXT_AdvancedEngine）")]
        public UdonSharpBehaviour engineComponent;

        [Tooltip("エンジンN2パラメータ名")]
        public string engineN2ParameterName = "N2";

        [Tooltip("ポンプ動作開始N2閾値（%）")]
        public float minimumN2 = 50f;

        [Header("電動設定（Electricの場合）")]
        [Tooltip("電力バス")]
        public TSFE_PowerBus powerBus;

        [Tooltip("ポンプ電源スイッチGameObject（activeで有効）")]
        public GameObject pumpSwitch;

        [Header("RAT設定（RATの場合）")]
        [Tooltip("SaccAirVehicle制御コンポーネント")]
        public UdonSharpBehaviour SAVControl;

        [Tooltip("RAT展開状態GameObject（activeで展開）")]
        public GameObject ratDeployedObject;

        [Tooltip("ポンプ動作開始速度（m/s）")]
        public float minimumSpeed = 50f;

        [Header("ポンプ状態")]
        [Tooltip("更新間隔（秒）")]
        public float updateInterval = 0.1f;

        /// <summary>
        /// ポンプが動作しているか（読み取り専用）
        /// </summary>
        [System.NonSerialized] public bool Running = false;

        private float lastUpdateTime = 0f;

        void Update()
        {
            if (Time.time - lastUpdateTime < updateInterval)
                return;

            UpdatePumpState();
            lastUpdateTime = Time.time;
        }

        private void UpdatePumpState()
        {
            bool running = false;

            switch (pumpType)
            {
                case PumpType.EngineDriven:
                    running = CheckEngineDriven();
                    break;

                case PumpType.Electric:
                    running = CheckElectric();
                    break;

                case PumpType.RAT:
                    running = CheckRAT();
                    break;
            }

            Running = running;
        }

        private bool CheckEngineDriven()
        {
            if (engineComponent == null) return false;

            object n2Value = engineComponent.GetProgramVariable(engineN2ParameterName);
            if (n2Value == null) return false;

            float n2 = 0f;
            if (n2Value is float)
                n2 = (float)n2Value;
            else if (n2Value is int)
                n2 = (float)(int)n2Value;
            else
                return false;

            return n2 >= minimumN2;
        }

        private bool CheckElectric()
        {
            // 電力バスチェック
            if (powerBus == null || !powerBus.Powered) return false;

            // スイッチチェック（設定されていない場合は常時ON）
            if (pumpSwitch != null && !pumpSwitch.activeInHierarchy) return false;

            return true;
        }

        private bool CheckRAT()
        {
            // RAT展開チェック
            if (ratDeployedObject != null && !ratDeployedObject.activeInHierarchy) return false;

            // 速度チェック
            if (SAVControl == null) return false;

            object speedValue = SAVControl.GetProgramVariable("Speed");
            if (speedValue == null) return false;

            float speed = 0f;
            if (speedValue is float)
                speed = (float)speedValue;
            else if (speedValue is int)
                speed = (float)(int)speedValue;
            else
                return false;

            return speed >= minimumSpeed;
        }

        /// <summary>
        /// 手動でポンプ状態を取得
        /// </summary>
        public bool IsRunning()
        {
            UpdatePumpState();
            return Running;
        }
    }
}
