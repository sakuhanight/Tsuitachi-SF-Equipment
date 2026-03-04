using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TSFE.SFEXT
{
    /// <summary>
    /// エンジン起動停止切り替え
    /// - エンジンOFF時: AutoStarterを起動（APU自動起動 → 全エンジン起動 → APU停止）
    /// - エンジンON時: 全エンジンカット（燃料OFF）
    /// DFUNCでトグルスイッチとして使用可能
    ///
    /// INOP判定: fireHandlePulledがtrueのエンジンは使用不可として除外
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_EngineToggle : UdonSharpBehaviour
    {
        [Header("コンポーネント参照")]
        [Tooltip("自動始動シーケンスコンポーネント")]
        public SFEXT_AutoStarter autoStarter;

        [Tooltip("エンジンコンポーネント配列（状態判定とカット用）")]
        public SFEXT_AdvancedEngine[] engines;

        [Header("動作設定")]
        [Tooltip("エンジンカット時にスターターもOFFにする")]
        public bool turnOffStarterOnCut = true;

        [Header("表示設定")]
        [Tooltip("エンジンON時に有効化するGameObject")]
        public GameObject enginesOnIndicator;

        /// <summary>
        /// エンジンがINOP（使用不可）か判定
        /// fireHandlePulled（火災ハンドル引いた）= エンジン使用不可
        /// </summary>
        private bool IsEngineInop(SFEXT_AdvancedEngine engine)
        {
            if (engine == null) return true;
            return engine.fireHandlePulled;
        }

        /// <summary>
        /// 稼働可能なエンジンがすべてONか判定
        /// INOP（fireHandlePulled=true）のエンジンは除外
        /// </summary>
        public bool AllOperableEnginesRunning
        {
            get
            {
                if (engines == null || engines.Length == 0) return false;

                int runningCount = 0;
                int operableCount = 0;
                for (int i = 0; i < engines.Length; i++)
                {
                    var engine = engines[i];
                    if (engine == null) continue;

                    // INOP判定（火災ハンドル引いたエンジンは除外）
                    if (IsEngineInop(engine)) continue;

                    operableCount++;
                    if (engine.EngineOn) runningCount++;
                }

                return operableCount > 0 && runningCount >= operableCount;
            }
        }

        void Update()
        {
            UpdateIndicator();
        }

        /// <summary>
        /// トグル（公開メソッド）
        /// エンジンOFF時 → AutoStarter起動
        /// エンジンON時 → エンジンカット
        /// </summary>
        public void Toggle()
        {
            if (AllOperableEnginesRunning)
            {
                // エンジンON → カット
                CutEngines();
            }
            else
            {
                // エンジンOFF → AutoStarter起動
                StartEngines();
            }
        }

        /// <summary>
        /// エンジン起動（AutoStarter経由）
        /// </summary>
        public void StartEngines()
        {
            if (autoStarter == null)
            {
                Debug.LogWarning("[EngineToggle] AutoStarter is not set");
                return;
            }

            Debug.Log("[EngineToggle] Starting engines via AutoStarter");
            autoStarter.StartSequence();
        }

        /// <summary>
        /// エンジンカット（稼働可能なエンジンの燃料OFF）
        /// INOPエンジンは除外
        /// </summary>
        public void CutEngines()
        {
            if (engines == null || engines.Length == 0)
            {
                Debug.LogWarning("[EngineToggle] No engines configured");
                return;
            }

            Debug.Log("[EngineToggle] Cutting all operable engines");
            int cutCount = 0;
            for (int i = 0; i < engines.Length; i++)
            {
                var engine = engines[i];
                if (engine == null) continue;

                // INOP判定（火災ハンドル引いたエンジンはスキップ）
                if (IsEngineInop(engine))
                {
                    Debug.Log($"[EngineToggle] Engine {i} is INOP (fire handle pulled) - skipping");
                    continue;
                }

                engine.fuel = false;
                if (turnOffStarterOnCut)
                {
                    engine.starter = false;
                }
                engine.RequestSerialization();
                cutCount++;
            }

            Debug.Log($"[EngineToggle] Cut {cutCount} engine(s)");
        }

        private void UpdateIndicator()
        {
            if (enginesOnIndicator != null)
            {
                enginesOnIndicator.SetActive(AllOperableEnginesRunning);
            }
        }
    }
}
