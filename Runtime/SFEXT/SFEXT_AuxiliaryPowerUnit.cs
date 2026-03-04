using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TSFE.SFEXT
{
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

        [Header("状態インジケータ")]
        [Tooltip("APU起動中に有効化するGameObject（PowerBus/BleedAirBusからの参照用）")]
        public GameObject apuStartedIndicator;

        [NonSerialized] public bool started, terminated;
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
        private float stateChangedTime;
        private void Update()
        {
            if (!initialized) return;

            float dt = Time.deltaTime;

            if (run != prevRun)
            {
                Debug.Log($"[APU] State changed: run={run}, prevRun={prevRun}");
                prevRun = run;
                stateChangedTime = Time.time;
                if (run) OnStart();
                else OnShutdown();
            }

            var stateTime = Time.time - stateChangedTime;
            if (run)
            {
                if (!started)
                {
                    OnStarting(stateTime, dt);
                }
                else
                {
                    Update_Started(dt);
                }
            }
            else
            {
                OnShuttingDown(stateTime, dt);
            }

            UpdateSound();
            UpdateApuStartedIndicator();
        }

        public void StartAPU()
        {
            if (!run) ToggleAPU();
        }
        public void StopAPU()
        {
            if (run) ToggleAPU();
        }
        public void ToggleAPU()
        {
            if (!isOwner)
            {
                Debug.LogWarning("[APU] Not owner, cannot toggle APU");
                return;
            }

            // 始動しようとしている場合は電源チェック
            if (!run && !CheckPowerAvailable())
            {
                Debug.LogWarning("[APU] Cannot start: No power available");
                return;
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

        private void ResetStatus()
        {
            run = false;
            started = false;
            terminated = true;
            N = 0f;

            // インジケータも初期化
            if (apuStartedIndicator != null)
            {
                apuStartedIndicator.SetActive(false);
            }
        }

        private void OnStart()
        {
            Debug.Log("[APU] OnStart called");
            terminated = false;
            started = false;

            SetParticleEmission(exhaustEffect, true);
        }

        private void OnStarting(float stateTime, float dt)
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

            // N更新: 始動目標回転数まで上昇
            float targetN = ratedN * starterTargetN;
            N = Mathf.MoveTowards(N, targetN, nStartupResponse * Mathf.Abs(targetN - N) * dt);

            // 目標回転数到達で started に遷移
            if (isOwner && N >= targetN * 0.99f)
            {
                Debug.Log($"[APU] Starting complete, N={N:F0} RPM");
                OnStarted();
            }
        }

        private void OnStarted()
        {
            Debug.Log("[APU] OnStarted called - APU is now running");
            started = true;
            terminated = false;
        }

        private void Update_Started(float dt)
        {
            // N更新: 定格回転数まで上昇
            N = Mathf.MoveTowards(N, ratedN, nResponse * Mathf.Abs(ratedN - N) * dt);
        }

        private void OnShutdown()
        {
            terminated = false;
            started = false;
        }

        private void OnShuttingDown(float stateTime, float dt)
        {
            // N更新: 0まで減少
            N = Mathf.MoveTowards(N, 0f, nDecreaseResponse * N * dt);

            if (N <= 0.01f)
            {
                OnTerminated();
            }
        }

        private void OnTerminated()
        {
            started = false;
            terminated = true;
            N = 0f;

            SetParticleEmission(exhaustEffect, false);
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
                if (run && N > 0.01f && N < starterTargetRPM)
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
                bool shouldPlay = (run && N >= startCrossFadeN) || (!run && N > 0.01f && N >= stopCrossFadeN);

                if (shouldPlay)
                {
                    // 有効化（Volume0で）
                    if (!apuLoopSound.gameObject.activeInHierarchy)
                    {
                        apuLoopSound.volume = 0f;
                        apuLoopSound.gameObject.SetActive(true);
                        apuLoopSound.Play();
                    }

                    if (run && N >= startCrossFadeN)
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
                    else if (!run && N > 0.01f && N >= stopCrossFadeN)
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
                if (!run && N > 0.01f)
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
            if (apuStartedIndicator != null)
            {
                apuStartedIndicator.SetActive(started);
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
