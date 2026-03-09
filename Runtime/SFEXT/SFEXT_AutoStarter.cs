using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TSFE.SFEXT
{
    /// <summary>
    /// AutoStarterのシーケンス状態（UdonSharpはネストされた型をサポートしないため外に出す）
    /// </summary>
    public enum AutoStarterSequenceState
    {
        Idle,                   // 待機中
        StartingBattery,        // バッテリー起動中
        StartingAPU,            // APU起動中
        WaitingAPU,             // APU起動完了待ち
        StartingEngines,        // エンジン起動中
        WaitingEngines,         // エンジン起動完了待ち
        StoppingAPU,            // APU停止中
        Completed,              // 完了
        Failed                  // 失敗
    }

    /// <summary>
    /// 自動エンジン始動シーケンス
    /// 1. バッテリーON
    /// 2. APU起動 → 起動完了待ち
    /// 3. 全エンジン始動 (順次または同時) → 全エンジン起動完了待ち
    /// 4. APU停止
    /// 5. 完了
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SFEXT_AutoStarter : UdonSharpBehaviour
    {
        [Header("電源システム")]
        [Tooltip("PowerBusコンポーネント（バッテリー制御用）")]
        public TSFE.Utility.TSFE_PowerBus powerBus;

        [Header("APUシステム")]
        [Tooltip("APUコンポーネント")]
        public SFEXT_AuxiliaryPowerUnit apu;

        [Header("エンジンシステム")]
        [Tooltip("エンジンコンポーネント配列（始動順序）")]
        public SFEXT_AdvancedEngine[] engines;

        [Tooltip("エンジンを順次起動（false=同時起動）")]
        public bool sequentialEngineStart = true;

        [Tooltip("次のエンジン起動までの待機時間（秒、順次起動時のみ）")]
        public float engineStartDelay = 5f;

        [Header("タイミング設定")]
        [Tooltip("APU起動完了チェック間隔（秒）")]
        public float apuCheckInterval = 0.5f;

        [Tooltip("エンジン起動完了チェック間隔（秒）")]
        public float engineCheckInterval = 0.5f;

        [Tooltip("APU停止前の待機時間（秒、全エンジン起動完了後）")]
        public float apuShutdownDelay = 3f;

        [Header("状態表示")]
        [Tooltip("状態表示用GameObject（シーケンス実行中に有効化）")]
        public GameObject runningIndicator;

        [System.NonSerialized] public AutoStarterSequenceState state = AutoStarterSequenceState.Idle;
        [System.NonSerialized] public string statusMessage = "";

        private bool isOwner;
        private float stateStartTime;
        private int currentEngineIndex = 0;

        void Start()
        {
            isOwner = Networking.IsOwner(gameObject);
            UpdateIndicator();
        }

        /// <summary>
        /// エンジンがINOP（使用不可）か判定
        /// </summary>
        private bool IsEngineInop(SFEXT_AdvancedEngine engine)
        {
            if (engine == null) return true;
            return engine.IsInoperable;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            isOwner = Networking.IsOwner(gameObject);
        }

        /// <summary>
        /// 自動始動シーケンス開始（公開メソッド）
        /// </summary>
        public void StartSequence()
        {
            if (state != AutoStarterSequenceState.Idle && state != AutoStarterSequenceState.Completed && state != AutoStarterSequenceState.Failed)
            {
                Debug.LogWarning("[AutoStarter] Sequence already running");
                return;
            }

            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            Debug.Log("[AutoStarter] Starting auto-start sequence");
            state = AutoStarterSequenceState.StartingBattery;
            stateStartTime = Time.time;
            currentEngineIndex = 0;
            UpdateIndicator();
        }

        /// <summary>
        /// シーケンス中断（公開メソッド）
        /// APU停止、全エンジンカット、スターターOFF
        /// </summary>
        public void AbortSequence()
        {
            if (state == AutoStarterSequenceState.Idle)
            {
                Debug.LogWarning("[AutoStarter] No sequence running");
                return;
            }

            Debug.Log("[AutoStarter] Aborting sequence - stopping APU and cutting all engines");

            // APU停止（APUが存在する場合、StopAPU()が自動判定）
            if (apu != null)
            {
                Debug.Log("[AutoStarter] Stopping APU on abort");
                apu.StopAPU();
            }

            // 全エンジンカット（稼働可能なエンジンのみ）
            if (engines != null)
            {
                for (int i = 0; i < engines.Length; i++)
                {
                    var engine = engines[i];
                    if (engine == null) continue;

                    // INOP判定（火災ハンドル引いたエンジンはスキップ）
                    if (IsEngineInop(engine)) continue;

                    engine.fuel = false;
                    engine.starter = false;
                    engine.RequestSerialization();
                }
            }

            state = AutoStarterSequenceState.Failed;
            statusMessage = "Aborted by user";
            UpdateIndicator();
        }

        /// <summary>
        /// シーケンスリセット（公開メソッド）
        /// </summary>
        public void ResetSequence()
        {
            Debug.Log("[AutoStarter] Resetting sequence");
            state = AutoStarterSequenceState.Idle;
            statusMessage = "";
            currentEngineIndex = 0;
            UpdateIndicator();
        }

        void Update()
        {
            if (!isOwner) return;
            if (state == AutoStarterSequenceState.Idle || state == AutoStarterSequenceState.Completed || state == AutoStarterSequenceState.Failed) return;

            if (state == AutoStarterSequenceState.StartingBattery)
            {
                UpdateStartingBattery();
            }
            else if (state == AutoStarterSequenceState.StartingAPU)
            {
                UpdateStartingAPU();
            }
            else if (state == AutoStarterSequenceState.WaitingAPU)
            {
                UpdateWaitingAPU();
            }
            else if (state == AutoStarterSequenceState.StartingEngines)
            {
                UpdateStartingEngines();
            }
            else if (state == AutoStarterSequenceState.WaitingEngines)
            {
                UpdateWaitingEngines();
            }
            else if (state == AutoStarterSequenceState.StoppingAPU)
            {
                UpdateStoppingAPU();
            }
        }

        private void UpdateStartingBattery()
        {
            if (powerBus == null)
            {
                Debug.Log("[AutoStarter] No PowerBus - skipping battery");
                statusMessage = "Battery: Skipped (No PowerBus)";
                TransitionToStartingAPU();
                return;
            }

            if (!powerBus.BatteryOn)
            {
                Debug.Log("[AutoStarter] Turning on battery");
                powerBus.SetBatteryOn();
                statusMessage = "Battery: ON";
            }

            TransitionToStartingAPU();
        }

        private void TransitionToStartingAPU()
        {
            state = AutoStarterSequenceState.StartingAPU;
            stateStartTime = Time.time;
            UpdateIndicator();
        }

        private void UpdateStartingAPU()
        {
            // 全エンジンがWindmill始動可能かチェック
            bool allEnginesCanWindmillStart = true;
            int validEngineCount = 0;

            if (engines != null)
            {
                for (int i = 0; i < engines.Length; i++)
                {
                    var engine = engines[i];
                    if (engine == null) continue;
                    if (IsEngineInop(engine)) continue;
                    if (engine.State == TSFE.SFEXT.EngineState.Running) continue;

                    validEngineCount++;

                    bool canWindmillStart = (engine.State == TSFE.SFEXT.EngineState.Windmilling)
                                           && (engine.N2 >= engine.takeOffN2 * engine.minN2ForIgnition);

                    if (!canWindmillStart)
                    {
                        allEnginesCanWindmillStart = false;
                        break;
                    }
                }
            }

            // 全エンジンがWindmill始動可能ならAPUをスキップ
            if (validEngineCount > 0 && allEnginesCanWindmillStart)
            {
                Debug.Log("[AutoStarter] All engines can windmill start - skipping APU");
                TransitionToStartingEngines();
                return;
            }

            if (apu == null)
            {
                Debug.LogWarning("[AutoStarter] No APU configured - cannot start engines without power");
                state = AutoStarterSequenceState.Failed;
                statusMessage = "Failed: No APU";
                UpdateIndicator();
                return;
            }

            if (apu.State == TSFE.SFEXT.APUState.Starting)
            {
                Debug.Log("[AutoStarter] APU already starting - waiting");
                state = AutoStarterSequenceState.WaitingAPU;
                stateStartTime = Time.time;
                statusMessage = "APU: Starting...";
                UpdateIndicator();
                return;
            }

            if (apu.State == TSFE.SFEXT.APUState.Running)
            {
                Debug.Log("[AutoStarter] APU already started - proceeding to engines");
                TransitionToStartingEngines();
                return;
            }

            // Off状態またはStopping状態から始動
            Debug.Log($"[AutoStarter] Starting APU from {apu.State} state");
            apu.StartAPU();
            state = AutoStarterSequenceState.WaitingAPU;
            stateStartTime = Time.time;
            statusMessage = "APU: Starting...";
            UpdateIndicator();
        }

        private void UpdateWaitingAPU()
        {
            if (Time.time - stateStartTime < apuCheckInterval) return;

            if (apu == null)
            {
                state = AutoStarterSequenceState.Failed;
                statusMessage = "Failed: APU component missing";
                UpdateIndicator();
                return;
            }

            if (apu.State == TSFE.SFEXT.APUState.Running)
            {
                Debug.Log("[AutoStarter] APU started successfully");
                statusMessage = "APU: Started";
                TransitionToStartingEngines();
                return;
            }

            // 継続待機
            stateStartTime = Time.time;
        }

        private void TransitionToStartingEngines()
        {
            state = AutoStarterSequenceState.StartingEngines;
            stateStartTime = Time.time;
            currentEngineIndex = 0;
            UpdateIndicator();
        }

        private void UpdateStartingEngines()
        {
            if (engines == null || engines.Length == 0)
            {
                Debug.LogWarning("[AutoStarter] No engines configured");
                state = AutoStarterSequenceState.Failed;
                statusMessage = "Failed: No engines";
                UpdateIndicator();
                return;
            }

            if (sequentialEngineStart)
            {
                // 順次起動モード
                if (currentEngineIndex >= engines.Length)
                {
                    // 全エンジン起動コマンド完了
                    Debug.Log("[AutoStarter] All engines start commanded - waiting for completion");
                    state = AutoStarterSequenceState.WaitingEngines;
                    stateStartTime = Time.time;
                    statusMessage = "Engines: Waiting for all engines to start...";
                    UpdateIndicator();
                    return;
                }

                // 待機時間チェック（最初のエンジンは即座に起動）
                if (currentEngineIndex > 0 && Time.time - stateStartTime < engineStartDelay)
                {
                    return;
                }

                var engine = engines[currentEngineIndex];
                if (engine == null)
                {
                    Debug.LogWarning($"[AutoStarter] Engine {currentEngineIndex} is null - skipping");
                    currentEngineIndex++;
                    stateStartTime = Time.time;
                    return;
                }

                // INOP判定（火災ハンドル引いたエンジンはスキップ）
                if (IsEngineInop(engine))
                {
                    Debug.Log($"[AutoStarter] Engine {currentEngineIndex} is INOP (fire handle pulled) - skipping");
                    currentEngineIndex++;
                    stateStartTime = Time.time;
                    return;
                }

                if (engine.State == TSFE.SFEXT.EngineState.Running)
                {
                    Debug.Log($"[AutoStarter] Engine {currentEngineIndex} already running - skipping");
                    currentEngineIndex++;
                    stateStartTime = Time.time;
                    return;
                }

                // Windmill始動判定：Windmilling状態かつN2が十分高い場合は燃料のみで再始動
                bool isWindmillStart = (engine.State == TSFE.SFEXT.EngineState.Windmilling)
                                       && (engine.N2 >= engine.takeOffN2 * engine.minN2ForIgnition);

                if (isWindmillStart)
                {
                    Debug.Log($"[AutoStarter] Engine {currentEngineIndex}: Windmill start (N2={engine.N2:F0} RPM >= {engine.takeOffN2 * engine.minN2ForIgnition:F0} RPM) - fuel only");
                    engine.fuel = true;
                    engine.RequestSerialization();
                    statusMessage = $"Engine {currentEngineIndex + 1}/{engines.Length}: Windmill Start...";
                }
                else
                {
                    Debug.Log($"[AutoStarter] Engine {currentEngineIndex}: Normal start (starter + fuel)");
                    engine.starter = true;
                    engine.fuel = true;
                    engine.RequestSerialization();
                    statusMessage = $"Engine {currentEngineIndex + 1}/{engines.Length}: Starting...";
                }

                currentEngineIndex++;
                stateStartTime = Time.time;
                UpdateIndicator();
            }
            else
            {
                // 同時起動モード
                Debug.Log("[AutoStarter] Starting all engines simultaneously");
                for (int i = 0; i < engines.Length; i++)
                {
                    var engine = engines[i];
                    if (engine == null)
                    {
                        Debug.LogWarning($"[AutoStarter] Engine {i} is null - skipping");
                        continue;
                    }

                    // INOP判定（火災ハンドル引いたエンジンはスキップ）
                    if (IsEngineInop(engine))
                    {
                        Debug.Log($"[AutoStarter] Engine {i} is INOP (fire handle pulled) - skipping");
                        continue;
                    }

                    if (engine.State == TSFE.SFEXT.EngineState.Running)
                    {
                        Debug.Log($"[AutoStarter] Engine {i} already running - skipping");
                        continue;
                    }

                    // Windmill始動判定：Windmilling状態かつN2が十分高い場合は燃料のみで再始動
                    bool isWindmillStart = (engine.State == TSFE.SFEXT.EngineState.Windmilling)
                                           && (engine.N2 >= engine.takeOffN2 * engine.minN2ForIgnition);

                    if (isWindmillStart)
                    {
                        Debug.Log($"[AutoStarter] Engine {i}: Windmill start (N2={engine.N2:F0} RPM >= {engine.takeOffN2 * engine.minN2ForIgnition:F0} RPM) - fuel only");
                        engine.fuel = true;
                    }
                    else
                    {
                        Debug.Log($"[AutoStarter] Engine {i}: Normal start (starter + fuel)");
                        engine.starter = true;
                        engine.fuel = true;
                    }
                    engine.RequestSerialization();
                }

                state = AutoStarterSequenceState.WaitingEngines;
                stateStartTime = Time.time;
                statusMessage = "Engines: Starting all...";
                UpdateIndicator();
            }
        }

        private void UpdateWaitingEngines()
        {
            if (Time.time - stateStartTime < engineCheckInterval) return;

            if (engines == null || engines.Length == 0)
            {
                state = AutoStarterSequenceState.Failed;
                statusMessage = "Failed: No engines";
                UpdateIndicator();
                return;
            }

            // 稼働可能なエンジンの起動状態チェック（INOPエンジンは除外）
            int startedCount = 0;
            int operableCount = 0;
            for (int i = 0; i < engines.Length; i++)
            {
                var engine = engines[i];
                if (engine == null) continue;

                // INOP判定（火災ハンドル引いたエンジンは除外）
                if (IsEngineInop(engine)) continue;

                operableCount++;
                if (engine.State == TSFE.SFEXT.EngineState.Running)
                {
                    startedCount++;

                    // スターターOFF（エンジンが起動したら）
                    if (engine.starter)
                    {
                        engine.starter = false;
                        engine.RequestSerialization();
                        Debug.Log($"[AutoStarter] Engine {i} started - turning off starter");
                    }
                }
            }

            statusMessage = $"Engines: {startedCount}/{operableCount} operable running";
            UpdateIndicator();

            if (startedCount >= operableCount)
            {
                Debug.Log("[AutoStarter] All engines started successfully");
                state = AutoStarterSequenceState.StoppingAPU;
                stateStartTime = Time.time;
                statusMessage = "Engines: All started, stopping APU...";
                UpdateIndicator();
                return;
            }

            // 継続待機
            stateStartTime = Time.time;
        }

        private void UpdateStoppingAPU()
        {
            if (Time.time - stateStartTime < apuShutdownDelay) return;

            if (apu != null && apu.State == TSFE.SFEXT.APUState.Running)
            {
                Debug.Log("[AutoStarter] Stopping APU");
                apu.StopAPU();
                statusMessage = "APU: Stopping...";
                UpdateIndicator();
            }

            state = AutoStarterSequenceState.Completed;
            statusMessage = "Sequence completed successfully";
            Debug.Log("[AutoStarter] Sequence completed");
            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            if (runningIndicator != null)
            {
                bool running = (state != AutoStarterSequenceState.Idle && state != AutoStarterSequenceState.Completed && state != AutoStarterSequenceState.Failed);
                runningIndicator.SetActive(running);
            }
        }
    }
}
