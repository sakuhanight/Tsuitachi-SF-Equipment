using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TSFE.Utility
{
    /// <summary>
    /// テストシナリオランナー
    /// 複数のテストシナリオを自動実行し、結果を検証
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TestScenarioRunner : UdonSharpBehaviour
    {
        [Header("テスト対象")]
        [Tooltip("MockSAVControl参照")]
        public TSFE.SFEXT.MockSAVControl mockSAV;

        [Tooltip("AutoStarter参照")]
        public TSFE.SFEXT.SFEXT_AutoStarter autoStarter;

        [Tooltip("APU参照")]
        public TSFE.SFEXT.SFEXT_AuxiliaryPowerUnit apu;

        [Tooltip("エンジン配列")]
        public TSFE.SFEXT.SFEXT_AdvancedEngine[] engines;

        [Header("テストシナリオ")]
        [Tooltip("実行するシナリオ配列")]
        public TestScenario[] scenarios;

        [Header("実行制御")]
        [Tooltip("自動実行（Start時）")]
        public bool autoRun = false;

        [Tooltip("シナリオ間の間隔 (秒)")]
        public float scenarioInterval = 2f;

        // 実行状態
        private int currentScenarioIndex = -1;
        private bool isRunning = false;
        private float scenarioStartTime = 0f;
        private string lastResult = "";

        void Start()
        {
            if (autoRun)
            {
                SendCustomEventDelayedSeconds(nameof(RunAllScenarios), 1f);
            }
        }

        /// <summary>
        /// 全シナリオを順次実行
        /// ContextMenuから手動実行可能
        /// </summary>
        public void RunAllScenarios()
        {
            if (isRunning)
            {
                Debug.LogWarning("[TestScenarioRunner] Already running");
                return;
            }

            if (scenarios == null || scenarios.Length == 0)
            {
                Debug.LogError("[TestScenarioRunner] No scenarios defined");
                return;
            }

            Debug.Log($"[TestScenarioRunner] Starting {scenarios.Length} scenario(s)");
            currentScenarioIndex = 0;
            isRunning = true;
            RunCurrentScenario();
        }

        /// <summary>
        /// 現在のシナリオを実行
        /// </summary>
        private void RunCurrentScenario()
        {
            if (currentScenarioIndex < 0 || currentScenarioIndex >= scenarios.Length)
            {
                Debug.Log("[TestScenarioRunner] All scenarios completed");
                isRunning = false;
                return;
            }

            var scenario = scenarios[currentScenarioIndex];
            Debug.Log($"[TestScenarioRunner] === Scenario {currentScenarioIndex + 1}/{scenarios.Length}: {scenario.scenarioName} ===");
            Debug.Log($"[TestScenarioRunner] Description: {scenario.description}");

            // シナリオ適用
            ApplyScenario(scenario);

            // 待機時間後にシナリオを開始
            if (scenario.preWaitTime > 0f)
            {
                SendCustomEventDelayedSeconds(nameof(StartScenarioAction), scenario.preWaitTime);
            }
            else
            {
                StartScenarioAction();
            }
        }

        /// <summary>
        /// シナリオの飛行条件を適用
        /// </summary>
        private void ApplyScenario(TestScenario scenario)
        {
            if (mockSAV == null)
            {
                Debug.LogWarning("[TestScenarioRunner] MockSAV is null");
                return;
            }

            mockSAV.Altitude = scenario.altitude;
            mockSAV.AirSpeed = scenario.airSpeed;
            mockSAV.AirVel = mockSAV.transform.forward * scenario.airSpeed;
            mockSAV.Atmosphere = scenario.atmosphere;
            mockSAV.Taxiing = scenario.taxiing;

            if (mockSAV.FullFuel > 0f)
            {
                mockSAV.Fuel = mockSAV.FullFuel * (scenario.fuelPercent / 100f);
            }

            Debug.Log($"[TestScenarioRunner] Applied: Alt={scenario.altitude:F0}m, Speed={scenario.airSpeed:F0}m/s, Atm={scenario.atmosphere:F2}, Fuel={scenario.fuelPercent:F0}%");
        }

        /// <summary>
        /// シナリオアクションを開始（AutoStarter起動）
        /// </summary>
        public void StartScenarioAction()
        {
            if (autoStarter == null)
            {
                Debug.LogWarning("[TestScenarioRunner] AutoStarter is null - skipping action");
                ScheduleVerification();
                return;
            }

            Debug.Log("[TestScenarioRunner] Starting AutoStarter sequence");
            scenarioStartTime = Time.time;
            autoStarter.StartSequence();

            // 検証をスケジュール
            ScheduleVerification();
        }

        /// <summary>
        /// 結果検証をスケジュール
        /// </summary>
        private void ScheduleVerification()
        {
            var scenario = scenarios[currentScenarioIndex];
            SendCustomEventDelayedSeconds(nameof(VerifyResult), scenario.verificationTimeout);
        }

        /// <summary>
        /// 結果を検証
        /// </summary>
        public void VerifyResult()
        {
            var scenario = scenarios[currentScenarioIndex];
            float elapsedTime = Time.time - scenarioStartTime;

            bool success = true;
            string resultMessage = "";

            // APU状態検証
            if (apu != null && scenario.expectAPURunning)
            {
                bool apuRunning = (apu.State == TSFE.SFEXT.APUState.Running);
                if (!apuRunning)
                {
                    success = false;
                    resultMessage += $"APU not running (State={apu.State}); ";
                }
            }

            // エンジン状態検証
            if (engines != null && scenario.expectAllEnginesRunning)
            {
                int runningCount = 0;
                for (int i = 0; i < engines.Length; i++)
                {
                    if (engines[i] != null && engines[i].State == TSFE.SFEXT.EngineState.Running)
                    {
                        runningCount++;
                    }
                }

                if (runningCount < engines.Length)
                {
                    success = false;
                    resultMessage += $"Only {runningCount}/{engines.Length} engines running; ";
                }
            }

            // AutoStarter状態検証
            if (autoStarter != null)
            {
                var state = autoStarter.state;
                if (state == TSFE.SFEXT.AutoStarterSequenceState.Failed)
                {
                    success = false;
                    resultMessage += $"AutoStarter failed; ";
                }
            }

            // 結果ログ
            if (success)
            {
                Debug.Log($"[TestScenarioRunner] ✓ PASSED: {scenario.scenarioName} ({elapsedTime:F1}s)");
                lastResult = "PASSED";
            }
            else
            {
                Debug.LogError($"[TestScenarioRunner] ✗ FAILED: {scenario.scenarioName} - {resultMessage}({elapsedTime:F1}s)");
                lastResult = $"FAILED: {resultMessage}";
            }

            // 次のシナリオへ
            SendCustomEventDelayedSeconds(nameof(NextScenario), scenario.postWaitTime);
        }

        /// <summary>
        /// 次のシナリオへ進む
        /// </summary>
        public void NextScenario()
        {
            currentScenarioIndex++;

            if (currentScenarioIndex >= scenarios.Length)
            {
                Debug.Log("[TestScenarioRunner] === All scenarios completed ===");
                isRunning = false;
                return;
            }

            // 間隔を置いて次のシナリオを実行
            SendCustomEventDelayedSeconds(nameof(RunCurrentScenario), scenarioInterval);
        }

        /// <summary>
        /// 実行を中断
        /// </summary>
        public void StopAllScenarios()
        {
            Debug.Log("[TestScenarioRunner] Stopping scenarios");
            isRunning = false;
            currentScenarioIndex = -1;
        }
    }
}
