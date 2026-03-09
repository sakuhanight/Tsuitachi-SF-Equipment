using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TSFE.SFEXT
{
    /// <summary>
    /// APU状態
    ///
    /// 状態遷移:
    /// - Off → Starting: run=true に変更（StartAPU()またはToggleAPU()）
    /// - Starting → Running: N が starterTargetN * ratedN に到達
    /// - Running → Stopping: run=false に変更（StopAPU()またはToggleAPU()）
    /// - Stopping → Off: N が 0 に到達
    /// - Stopping → Starting: run=true に変更（再始動）
    ///
    /// runフラグとの関係:
    /// - run=true: Off/Stopping → Starting → Running
    /// - run=false: Starting/Running → Stopping → Off
    /// </summary>
    public enum APUState
    {
        Off = 0,        // 完全停止（N=0、run=false）
        Starting = 1,   // 始動中（スターター稼働、run=true、N上昇中）
        Running = 2,    // 正常運転中（run=true、N=ratedN）
        Stopping = 3    // 停止中（スプールダウン、run=false、N減少中）
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SFEXT_AuxiliaryPowerUnit : UdonSharpBehaviour
    {
        [Header("RPM設定")]
        [Tooltip("アイドル回転数 (RPM)")]
        public float idleN = 8000f;
        [Tooltip("定格回転数 (RPM)")]
        public float ratedN = 10000f;
        [Tooltip("始動目標回転数 (% of ratedN)")]
        [Range(0.3f, 0.7f)]
        public float starterTargetN = 0.5f;
        [Tooltip("始動時の回転数応答速度")]
        public float nStartupResponse = 0.3f;
        [Tooltip("運転時の回転数応答速度")]
        public float nResponse = 0.5f;
        [Tooltip("停止時の回転数応答速度")]
        public float nDecreaseResponse = 0.2f;

        [Header("サウンド")]
        [Tooltip("APU始動音用AudioSource")]
        public AudioSource apuStartSound;
        [Tooltip("APU運転音用AudioSource")]
        public AudioSource apuLoopSound;
        [Tooltip("APU停止音用AudioSource")]
        public AudioSource apuStopSound;

        [Header("サウンド設定")]
        [Tooltip("始動音→運転音クロスフェード開始タイミング（% of 始動目標RPM）")]
        [Range(0.3f, 0.9f)]
        public float startCrossFadeStart = 0.5f;
        [Tooltip("運転音→停止音クロスフェード開始タイミング（% of 定格RPM）")]
        [Range(0.7f, 1.0f)]
        public float stopCrossFadeStart = 0.9f;

        [Header("サウンド音量調整 (1.0 = 通常)")]
        [Tooltip("apuStartSound音量倍率")]
        [Range(0f, 2f)]
        public float startVolumeMultiplier = 1f;
        [Tooltip("apuLoopSound音量倍率")]
        [Range(0f, 2f)]
        public float loopVolumeMultiplier = 1f;
        [Tooltip("apuStopSound音量倍率")]
        [Range(0f, 2f)]
        public float stopVolumeMultiplier = 1f;

        [Header("エフェクト")]
        public ParticleSystem exhaustEffect;

        [Header("電源システム")]
        [Tooltip("APU始動に必要な電源GameObject（バッテリーまたはGPU、null=電源不要）")]
        public GameObject powerSource;

        [Header("高度制限")]
        [Tooltip("APU最大作動高度 (メートル) - 現実: FL200 = 6096m")]
        public float maxOperatingAltitude = 6096f;
        [Tooltip("SaccAirVehicle参照（高度情報取得用）")]
        public UdonSharpBehaviour SAVControl;

        [Header("状態インジケータ")]
        [Tooltip("APU起動中に有効化するGameObject（PowerBus/BleedAirBusからの参照用）")]
        public GameObject apuRunningIndicator;

        // 状態管理
        [UdonSynced] private int _apuStateInt = 0;

        /// <summary>
        /// APU状態（外部スクリプトから読み取り可能）
        /// </summary>
        public APUState State
        {
            get => (APUState)_apuStateInt;
            private set { _apuStateInt = (int)value; }
        }

        // State判定ヘルパープロパティ
        public bool IsOff => State == APUState.Off;
        public bool IsStarting => State == APUState.Starting;
        public bool IsRunning => State == APUState.Running;
        public bool IsStopping => State == APUState.Stopping;
        public bool CanStart => State == APUState.Off || State == APUState.Stopping;

        [UdonSynced] private bool run;

        // 内部状態
        private bool initialized;
        private float apuStartVol, apuStartPit;
        private float apuLoopVol, apuLoopPit;
        private float apuStopVol, apuStopPit;
        private float N; // 現在の回転数

        void Start()
        {
            // テスト環境用: SFEXT_L_EntityStartが呼ばれない場合の初期化
            if (!initialized)
            {
                InitializeAPU();
            }
        }

        public void SFEXT_L_EntityStart()
        {
            InitializeAPU();
        }

        private void InitializeAPU()
        {
            // AudioSource初期化（すべてループ、初期無効）
            if (apuStartSound) { apuStartVol = apuStartSound.volume; apuStartPit = apuStartSound.pitch; apuStartSound.loop = true; apuStartSound.volume = 0f; apuStartSound.gameObject.SetActive(false); }
            if (apuLoopSound) { apuLoopVol = apuLoopSound.volume; apuLoopPit = apuLoopSound.pitch; apuLoopSound.loop = true; apuLoopSound.volume = 0f; apuLoopSound.gameObject.SetActive(false); }
            if (apuStopSound) { apuStopVol = apuStopSound.volume; apuStopPit = apuStopSound.pitch; apuStopSound.loop = true; apuStopSound.volume = 0f; apuStopSound.gameObject.SetActive(false); }

            ResetStatus();

            // テスト環境用の初期化
            isOwner = Networking.IsOwner(gameObject);

            Debug.Log($"[APU] Initialized: isOwner={isOwner}");
            initialized = true;
        }

        private bool isOwner;
        public void SFEXT_O_PilotEnter() { isOwner = true; }
        public void SFEXT_O_TakeOwnership() { isOwner = true; }
        public void SFEXT_O_LoseOwnership() { isOwner = false; }
        public void SFEXT_G_RespawnButton() { ResetStatus(); }
        public void SFEXT_G_Explode() { ResetStatus(); }

        private bool prevRun;
        private APUState prevState;
        private float stateChangedTime;

        private void Update()
        {
            if (!initialized) return;

            float dt = Time.deltaTime;

            // ========================================================================
            // 状態遷移ロジック: runフラグに基づいて状態の整合性を毎フレーム強制
            // ========================================================================
            //
            // run=true の場合:
            //   - Off/Stopping → Starting に遷移（再始動可能）
            //   - Starting → Running は Update_Starting() 内で自動遷移（N到達時）
            //
            // run=false の場合:
            //   - Starting/Running → Stopping に遷移
            //   - Stopping → Off は Update_Stopping() 内で自動遷移（N=0時）
            //
            if (run)
            {
                // run=true なら、Off/Stoppingから始動する
                if (State == APUState.Off || State == APUState.Stopping)
                {
                    if (State != prevState || !prevRun)
                    {
                        Debug.Log($"[APU] Starting requested (run=true, state={State})");
                    }
                    State = APUState.Starting;
                }
            }
            else
            {
                // run=false なら、Starting/Runningから停止する
                if (State == APUState.Starting || State == APUState.Running)
                {
                    if (State != prevState || prevRun)
                    {
                        Debug.Log($"[APU] Stopping requested (run=false, state={State})");
                    }
                    State = APUState.Stopping;
                }
            }

            prevRun = run;

            // 状態変更検知
            if (State != prevState)
            {
                Debug.Log($"[APU] State changed: {prevState} -> {State}");
                prevState = State;
                stateChangedTime = Time.time;
            }

            var stateTime = Time.time - stateChangedTime;

            // 状態別処理
            switch (State)
            {
                case APUState.Off:
                    Update_Off(dt);
                    break;
                case APUState.Starting:
                    Update_Starting(stateTime, dt);
                    break;
                case APUState.Running:
                    Update_Running(dt);
                    break;
                case APUState.Stopping:
                    Update_Stopping(dt);
                    break;
            }

            UpdateSound();
            UpdateApuStartedIndicator();
        }

        /// <summary>
        /// APU始動を要求（AutoStarter等から呼ばれる）
        ///
        /// 動作:
        /// - run=false → ToggleAPU()で run=true に変更（通常の始動）
        /// - run=true かつ Stopping/Off状態 → 強制的にStartingに遷移（再始動高速化）
        /// </summary>
        public void StartAPU()
        {
            if (!isOwner) return;

            // run=falseの場合、Toggleでtrueにする
            if (!run)
            {
                ToggleAPU();
            }
            // run=trueだがStopping/Off状態の場合、強制的にStartingに遷移
            else if (State == APUState.Off || State == APUState.Stopping)
            {
                Debug.Log($"[APU] StartAPU() forcing transition from {State} to Starting (run already true)");
                State = APUState.Starting;
                RequestSerialization();
            }
        }

        /// <summary>
        /// APU停止を要求（AutoStarter等から呼ばれる）
        ///
        /// 動作:
        /// - run=true → ToggleAPU()で run=false に変更（通常の停止）
        /// - run=false かつ Starting/Running状態 → 強制的にStoppingに遷移（停止高速化）
        /// </summary>
        public void StopAPU()
        {
            if (!isOwner) return;

            // run=trueの場合、Toggleでfalseにする
            if (run)
            {
                ToggleAPU();
            }
            // run=falseだがStarting/Running状態の場合、強制的にStoppingに遷移
            else if (State == APUState.Starting || State == APUState.Running)
            {
                Debug.Log($"[APU] StopAPU() forcing transition from {State} to Stopping (run already false)");
                State = APUState.Stopping;
                RequestSerialization();
            }
        }
        public void ToggleAPU()
        {
            if (!isOwner)
            {
                Debug.LogWarning("[APU] Not owner, cannot toggle APU");
                return;
            }

            // 始動しようとしている場合は電源と高度をチェック
            if (!run)
            {
                if (!CheckPowerAvailable())
                {
                    Debug.LogWarning("[APU] Cannot start: No power available");
                    return;
                }

                if (!CheckAltitudeWithinLimit())
                {
                    float currentAltitude = GetCurrentAltitude();
                    Debug.LogWarning($"[APU] Cannot start: Altitude {currentAltitude:F0}m exceeds limit {maxOperatingAltitude:F0}m");
                    return;
                }
            }

            Debug.Log($"[APU] ToggleAPU called: run {run} -> {!run}");
            run = !run;
            RequestSerialization();
        }

        /// <summary>
        /// APU始動用の電源が利用可能かチェック
        /// </summary>
        private bool CheckPowerAvailable()
        {
            // powerSourceがnull → 電源不要（テスト用、または独立電源APU）
            if (powerSource == null) return true;

            // powerSourceが有効 → 電源供給あり
            return powerSource.activeInHierarchy;
        }

        /// <summary>
        /// 電源が利用可能か（外部からの参照用）
        /// </summary>
        public bool PowerAvailable => CheckPowerAvailable();

        /// <summary>
        /// 現在の高度を取得（メートル）
        /// </summary>
        private float GetCurrentAltitude()
        {
            if (SAVControl == null) return 0f;
            var altitudeObj = SAVControl.GetProgramVariable("Altitude");
            if (altitudeObj == null) return 0f;
            return (float)altitudeObj;
        }

        /// <summary>
        /// 現在の高度がAPU作動限界内かチェック
        /// </summary>
        private bool CheckAltitudeWithinLimit()
        {
            float altitude = GetCurrentAltitude();
            return altitude <= maxOperatingAltitude;
        }

        private void ResetStatus()
        {
            run = false;
            State = APUState.Off;
            N = 0f;

            // インジケータも初期化
            if (apuRunningIndicator != null)
            {
                apuRunningIndicator.SetActive(false);
            }
        }

        // ===== 状態別Update =====

        private void Update_Off(float dt)
        {
            // RPMを0に保持
            N = 0f;
            SetParticleEmission(exhaustEffect, false);
        }

        private void Update_Starting(float stateTime, float dt)
        {
            // 始動中に電源が切れたら停止
            if (!CheckPowerAvailable())
            {
                Debug.LogWarning("[APU] Power lost during startup - shutting down");
                if (isOwner)
                {
                    run = false;
                    RequestSerialization();
                }
                return;
            }

            // エフェクト開始
            SetParticleEmission(exhaustEffect, true);

            // N更新: 始動目標回転数まで上昇
            float targetN = ratedN * starterTargetN;

            // Stopping状態から再始動した場合、すでにtargetN以上のRPMがある可能性がある
            if (N >= targetN * 0.99f)
            {
                // すでに目標RPMに達している → 即座にRunningに遷移
                if (isOwner)
                {
                    Debug.Log($"[APU] Already at target RPM {N:F0} -> Running");
                    State = APUState.Running;
                }
            }
            else
            {
                // 目標RPMまで上昇
                N = Mathf.MoveTowards(N, targetN, nStartupResponse * Mathf.Abs(targetN - N) * dt);

                // 目標回転数到達でRunningに遷移
                if (isOwner && N >= targetN * 0.99f)
                {
                    Debug.Log($"[APU] Starting complete, N={N:F0} RPM -> Running");
                    State = APUState.Running;
                }
            }
        }

        private void Update_Running(float dt)
        {
            // 高度超過チェック（稼働中に高度制限を超えたら自動停止）
            if (isOwner && !CheckAltitudeWithinLimit())
            {
                float currentAltitude = GetCurrentAltitude();
                Debug.LogWarning($"[APU] Altitude {currentAltitude:F0}m exceeded limit {maxOperatingAltitude:F0}m - auto shutdown");
                run = false;
                RequestSerialization();
                return;
            }

            // N更新: 定格回転数まで上昇
            N = Mathf.MoveTowards(N, ratedN, nResponse * Mathf.Abs(ratedN - N) * dt);
        }

        private void Update_Stopping(float dt)
        {
            // N更新: 0まで減少
            N = Mathf.MoveTowards(N, 0f, nDecreaseResponse * N * dt);

            // RPMが0に達したらOffに遷移
            if (N <= 0.01f)
            {
                Debug.Log("[APU] Stopping complete -> Off");
                State = APUState.Off;
                N = 0f;
                SetParticleEmission(exhaustEffect, false);
            }
        }

        private void UpdateSound()
        {
            float nNorm = N / ratedN;

            // クロスフェード境界値
            float starterTargetRPM = ratedN * starterTargetN;
            float startCrossFadeN = starterTargetRPM * startCrossFadeStart;
            float stopCrossFadeN = ratedN * stopCrossFadeStart;

            // apuStartSound: 始動音（0 ～ starterTargetRPM、startCrossFadeNからフェードアウト開始）
            if (apuStartSound)
            {
                if (State == APUState.Starting && N > 0.01f && N < starterTargetRPM)
                {
                    // 有効化（Volume0で）
                    if (!apuStartSound.gameObject.activeInHierarchy)
                    {
                        apuStartSound.volume = 0f;
                        apuStartSound.gameObject.SetActive(true);
                        apuStartSound.Play();
                    }

                    // ピッチ: 0 ～ starterTargetRPM で 0.5 → 1.0 (RPMに比例)
                    float pitchProgress = N / starterTargetRPM;
                    apuStartSound.pitch = apuStartPit * (0.5f + pitchProgress * 0.5f);

                    if (N < startCrossFadeN)
                    {
                        // クロスフェード前: 0 ～ startCrossFadeN で音量増加
                        float startProgress = N / startCrossFadeN;
                        apuStartSound.volume = apuStartVol * startProgress * startVolumeMultiplier;
                    }
                    else
                    {
                        // クロスフェード中: startCrossFadeN ～ starterTargetRPM でフェードアウト
                        float fadeOut = 1.0f - (N - startCrossFadeN) / (starterTargetRPM - startCrossFadeN);
                        apuStartSound.volume = apuStartVol * fadeOut * startVolumeMultiplier;
                    }
                }
                else
                {
                    // 無効化
                    if (apuStartSound.gameObject.activeInHierarchy)
                    {
                        apuStartSound.volume = 0f;
                        apuStartSound.Stop();
                        apuStartSound.gameObject.SetActive(false);
                    }
                }
            }

            // apuLoopSound: 運転音（startCrossFadeN以降、停止時はstopCrossFadeN以上）
            if (apuLoopSound)
            {
                bool isRunningPhase = (State == APUState.Starting || State == APUState.Running);
                bool isStoppingPhase = (State == APUState.Stopping);
                bool shouldPlay = (isRunningPhase && N >= startCrossFadeN) || (isStoppingPhase && N > 0.01f && N >= stopCrossFadeN);

                if (shouldPlay)
                {
                    // 有効化（Volume0で）
                    if (!apuLoopSound.gameObject.activeInHierarchy)
                    {
                        apuLoopSound.volume = 0f;
                        apuLoopSound.gameObject.SetActive(true);
                        apuLoopSound.Play();
                    }

                    if (isRunningPhase && N >= startCrossFadeN)
                    {
                        // 始動・運転フェーズ
                        // ピッチ: startCrossFadeN(2500) ～ ratedN(10000) で 0.6 → 1.0 (RPMに比例)
                        float pitchProgress = (N - startCrossFadeN) / (ratedN - startCrossFadeN);
                        apuLoopSound.pitch = apuLoopPit * (0.6f + pitchProgress * 0.4f);

                        if (N < starterTargetRPM)
                        {
                            // クロスフェード中: startCrossFadeN ～ starterTargetRPM でフェードイン
                            float fadeIn = (N - startCrossFadeN) / (starterTargetRPM - startCrossFadeN);
                            apuLoopSound.volume = apuLoopVol * fadeIn * loopVolumeMultiplier;
                        }
                        else
                        {
                            // 通常運転: starterTargetRPM以降はフルボリューム
                            apuLoopSound.volume = apuLoopVol * loopVolumeMultiplier;
                        }
                    }
                    else if (isStoppingPhase && N > 0.01f && N >= stopCrossFadeN)
                    {
                        // 停止時クロスフェード: stopCrossFadeN ～ ratedN でフェードアウト
                        float fadeOut = (N - stopCrossFadeN) / (ratedN - stopCrossFadeN);
                        apuLoopSound.volume = apuLoopVol * fadeOut * loopVolumeMultiplier;

                        // ピッチはRPMに比例
                        float pitchProgress = (N - startCrossFadeN) / (ratedN - startCrossFadeN);
                        apuLoopSound.pitch = apuLoopPit * (0.6f + pitchProgress * 0.4f);
                    }
                }
                else
                {
                    // 無効化
                    if (apuLoopSound.gameObject.activeInHierarchy)
                    {
                        apuLoopSound.volume = 0f;
                        apuLoopSound.Stop();
                        apuLoopSound.gameObject.SetActive(false);
                    }
                }
            }

            // apuStopSound: 停止音（停止シーケンス中、N > 0 から）
            if (apuStopSound)
            {
                if (State == APUState.Stopping && N > 0.01f)
                {
                    // 有効化（Volume0で）
                    if (!apuStopSound.gameObject.activeInHierarchy)
                    {
                        apuStopSound.volume = 0f;
                        apuStopSound.gameObject.SetActive(true);
                        apuStopSound.Play();
                    }

                    // ピッチ: ratedN ～ 0 で 1.0 → 0.5 (RPMに比例)
                    apuStopSound.pitch = apuStopPit * (0.5f + nNorm * 0.5f);

                    if (N >= stopCrossFadeN)
                    {
                        // クロスフェード中（N >= stopCrossFadeN）: フェードイン
                        float fadeIn = 1.0f - (N - stopCrossFadeN) / (ratedN - stopCrossFadeN);
                        apuStopSound.volume = apuStopVol * fadeIn * stopVolumeMultiplier;
                    }
                    else
                    {
                        // クロスフェード完了後（N < stopCrossFadeN）: 通常の減衰
                        apuStopSound.volume = apuStopVol * nNorm * stopVolumeMultiplier;
                    }
                }
                else
                {
                    // 無効化
                    if (apuStopSound.gameObject.activeInHierarchy)
                    {
                        apuStopSound.volume = 0f;
                        apuStopSound.Stop();
                        apuStopSound.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void UpdateApuStartedIndicator()
        {
            if (apuRunningIndicator != null)
            {
                apuRunningIndicator.SetActive(State == APUState.Running);
            }
        }

        private void SetParticleEmission(ParticleSystem system, bool value)
        {
            if (!system) return;
            var emission = system.emission;
            if (emission.enabled != value) emission.enabled = value;
        }
    }
}
