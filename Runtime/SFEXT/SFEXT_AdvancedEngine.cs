using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.SFEXT
{
    public enum EngineState
    {
        Off = 0,         // 完全停止（地上、N1/N2 = 0）
        Windmilling = 1, // 風車回転（飛行中、意図的停止後）
        Starting = 2,    // 始動中（スターター稼働）
        Running = 3,     // 正常運転中
        Seized = 4       // 破損・固着（MTBF/バードストライク等）
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(1000)]
    public class SFEXT_AdvancedEngine : UdonSharpBehaviour
    {
        [Header("動力系")]
        [Tooltip("最大推力 (N) - この値を機体質量で割った値がSaccに適用されます")]
        public float maxThrust = 130408.51f;
        public float thrustCurve = 2.0f;
        [Tooltip("アイドル時の推力 (% of maxThrust) - 通常7-10%")]
        [Range(0.05f, 0.15f)]
        public float idleThrustRatio = 0.075f;

        [Header("N1 (低圧) RPM")]
        public float idleN1 = 879.6f;
        public float referenceN1 = 4397f;
        public float takeOffN1 = 4586f;
        [Tooltip("N1上昇応答速度 (Idle→TakeOff: 約5秒)")]
        public float n1Response = 0.6f;
        [Tooltip("N1減少応答速度 (TakeOff→Idle: 約8秒)")]
        public float n1DecreaseResponse = 0.375f;
        [Tooltip("N1始動時応答速度 (未使用)")]
        public float n1StartupResponse = 0.01f;

        [Header("N2 (高圧) RPM")]
        public float idleN2 = 12100f;
        public float referenceN2 = 17167f;
        public float takeOffN2 = 20171f;
        [Tooltip("N2応答速度 (Fuel ON→Idle: 約20秒)")]
        public float n2Response = 0.15f;
        [Tooltip("N2減少応答速度")]
        public float n2DecreaseResponse = 0.12f;
        [Tooltip("N2始動応答速度 (Starter→目標: 約10秒)")]
        public float n2StartupResponse = 0.3f;

        [Header("温度 (°C)")]
        [Tooltip("外気温度 (ISA標準: 地上15°C、熱帯35°C、高高度-54°C)")]
        public float ambientTemp = 15f;
        public float idleEGT = 725f;
        public float continuousEGT = 1013f;
        public float takeOffEGT = 1038f;
        public float fireEGT = 1812f;
        public float idleECT = 196f;
        public float continuousECT = 274f;
        public float overheatECT = 343f;
        public float fireECT = 850f;
        public float egtResponse = 0.02f;
        public float ectResponse = 0.1f;
        public float ectOverheatResponse = 0.001f;

        [Header("逆噴射")]
        public float reverserRatio = 0.5f;
        public float reverserExtractResponse = 0.5f;
        public float reverserRetractResponse = 0.5f;

        [Header("アフターバーナー")]
        [Tooltip("アフターバーナー有効化")]
        public bool hasAfterburner = false;
        [Tooltip("アフターバーナー点火スロットル閾値（0-1）")]
        [Range(0.7f, 1.0f)]
        public float afterburnerThreshold = 0.95f;
        [Tooltip("アフターバーナー時の推力倍率")]
        [Range(1.0f, 2.0f)]
        public float afterburnerThrustMultiplier = 1.5f;
        [Tooltip("アフターバーナー点火/消火の応答速度")]
        public float afterburnerResponse = 2f;

        [Header("故障 MTBF (秒)")]
        [Tooltip("故障システムを有効化 - false で火災・メルトダウン・連続運転制限を完全に無効化")]
        public bool enableFailureSystem = true;
        public float mtbFireAtContinuous = 2592000f;
        public float mtbFireAtOverheat = 90f;
        public float mtbMeltdownOnFire = 90f;

        [Header("高出力連続運転制限")]
        [Tooltip("高出力連続運転制限を有効化")]
        public bool enableContinuousPowerLimit = true;
        [Tooltip("制限対象の推力比率 (0.9 = 90%以上)")]
        [Range(0.7f, 1.0f)]
        public float continuousPowerThreshold = 0.9f;
        [Tooltip("連続運転許容時間 (秒) - この時間を超えると故障判定開始")]
        public float continuousPowerTimeLimit = 120f;
        [Tooltip("制限時間超過後のMTBF (秒) - 短いほど故障しやすい")]
        public float mtbFireAtContinuousPower = 60f;

        [Header("始動システム")]
        [Tooltip("スターター電源/ブリード空気源（PowerBus/poweredIndicator または BleedAirBus/bleedAirIndicator、null=単独始動可能）")]
        public GameObject starterPowerSource;

        [Tooltip("N1軸スターター方式（T-4等）- false=N2軸スターター（CFM56, 737等の一般的な方式）")]
        public bool n1Start = false;

        [Header("N2軸スターター設定（n1Start=false時）")]
        [Tooltip("スターター目標N2 (% of takeOffN2) - CFM56: 25%, FJ44: 20%")]
        [Range(0.15f, 0.35f)]
        public float starterTargetN2Ratio = 0.25f;
        [Tooltip("燃料投入可能最低N2 (% of takeOffN2) - 現実: 15% (燃料ポンプ圧力確保に必要)")]
        [Range(0.15f, 0.35f)]
        public float minN2ForIgnition = 0.15f;

        [Header("N1軸スターター設定（n1Start=true時）")]
        [Tooltip("スターター目標N1 (% of takeOffN1) - T-4: 15%程度")]
        [Range(0.1f, 0.3f)]
        public float starterTargetN1Ratio = 0.15f;
        [Tooltip("自動燃料投入N1閾値 (% of takeOffN1) - T-4: 10%")]
        [Range(0.05f, 0.15f)]
        public float autoIgnitionN1Threshold = 0.10f;
        [Tooltip("始動時のN2/N1比（機械結合、燃焼前）")]
        [Range(0.8f, 1.5f)]
        public float startingN2toN1Ratio = 1.2f;
        [Tooltip("自動燃料投入を有効化（n1Start=true時のみ有効）")]
        public bool autoFuelInjection = true;

        [Header("共通始動設定")]
        [Tooltip("自動スターターカットを有効化 - アイドル回転到達時に自動的にスターターを切る")]
        public bool autoStarterCutoff = true;
        [Tooltip("自動スターターカット閾値 - n1Start ? (N1/idleN1) : (N2/idleN2)")]
        [Range(0.9f, 1.0f)]
        public float starterCutoffThreshold = 0.95f;

        [Header("質量設定")]
        [Tooltip("推力計算用の質量を手動設定（true）か、Rigidbody.massから自動取得（false）するか")]
        public bool useManualMass = false;
        [Tooltip("エンジン推力計算用の質量 (kg) - 現実的な機体重量。物理質量（Rigidbody.mass）はWheelCollider対策で別途軽量化可能")]
        public float engineCalculationMass = 19000f;

        [Header("推力適用設定")]
        [Tooltip("動的ThrottleStrength調整を有効化 - エンジン推力計算に基づいてThrottleStrengthを更新")]
        public bool enableDynamicThrust = true;
        [Tooltip("非対称推力システムを有効化 - エンジン位置に基づいてトルク(ヨーイング)を生成")]
        public bool enableAsymmetricThrust = false;
        [Tooltip("エンジン位置 (ローカル座標) - 機体重心からの相対位置、nullの場合はこのGameObjectの位置を使用")]
        public Transform enginePosition;

        [Header("Windmilling設定")]
        [Tooltip("Windmilling時のN1比率（基準速度時） - 290 KIAS で15% N2達成に調整")]
        [Range(0.05f, 0.35f)]
        public float windmillingN1Ratio = 0.27f;
        [Tooltip("Windmilling時のN2/N1比（機械損失考慮） - N2の方が高速回転")]
        [Range(1.5f, 4.0f)]
        public float windmillingN2toN1Ratio = 2.7f;
        [Tooltip("Windmilling最大N1制限 (% of takeOffN1) - 高速時のN1上限")]
        [Range(0.15f, 0.50f)]
        public float windmillingMaxN1Ratio = 0.35f;
        [Tooltip("Windmilling基準速度 (KIAS) - 現実の15% N2到達速度（Windmill始動可能速度）")]
        public float windmillingReferenceSpeed = 290f;
        [Tooltip("Windmilling維持最低速度 (KIAS) - これ以下でOff状態に遷移")]
        public float minimumWindmillingSpeed = 150f;

        [Header("Windmilling抗力")]
        [Tooltip("Windmilling抗力係数")]
        [Range(0.05f, 0.5f)]
        public float windmillingDragCoefficient = 0.15f;
        [Tooltip("抗力計算基準速度 (KIAS)")]
        public float windmillingDragReferenceSpeed = 290f;

        [Header("燃料消費")]
        [Tooltip("燃料消費システムを有効化 - エンジン稼働中にSAVControlのFuelを消費")]
        public bool enableFuelConsumption = true;
        [Tooltip("アイドル時の燃料消費率 (kg/s) - CFM56-7B: 約0.15 kg/s")]
        public float idleFuelFlow = 0.15f;
        [Tooltip("最大出力時の燃料消費率 (kg/s) - CFM56-7B: 約1.1 kg/s")]
        public float maxFuelFlow = 1.1f;
        [Tooltip("アフターバーナー時の燃料消費倍率 - 通常の2-3倍")]
        [Range(1.0f, 4.0f)]
        public float afterburnerFuelMultiplier = 2.5f;

        [Header("コンポーネント")]
        public UdonSharpBehaviour SAVControl;
        public Animator vehicleAnimator;
        public string n1ParameterName = "n1";
        public string n2ParameterName = "n2";
        public string reverserParameterName = "reverser";
        public string fireParameterName = "fire";
        public string afterburnerParameterName = "afterburner";

        [Header("サウンド")]
        public AudioSource idleSound;
        public AudioSource insideSound;
        public AudioSource thrustSound;
        public AudioSource takeoffSound;
        [Tooltip("逆噴射音（リバーサー展開中のみ）")]
        public AudioSource reverserSound;
        [Tooltip("アフターバーナー音（AB点火中のみ）")]
        public AudioSource afterburnerSound;

        [Header("サウンド音量調整 (1.0 = 通常)")]
        [Tooltip("idleSound音量倍率")]
        [Range(0f, 2f)]
        public float idleVolumeMultiplier = 1f;
        [Tooltip("insideSound音量倍率")]
        [Range(0f, 2f)]
        public float insideVolumeMultiplier = 1f;
        [Tooltip("thrustSound音量倍率")]
        [Range(0f, 2f)]
        public float thrustVolumeMultiplier = 1f;
        [Tooltip("takeoffSound音量倍率")]
        [Range(0f, 2f)]
        public float takeoffVolumeMultiplier = 1f;
        [Tooltip("reverserSound音量倍率")]
        [Range(0f, 2f)]
        public float reverserVolumeMultiplier = 1f;
        [Tooltip("afterburnerSound音量倍率")]
        [Range(0f, 2f)]
        public float afterburnerVolumeMultiplier = 1f;

        [Header("火災サウンド")]
        [Tooltip("エンジン火災の燃焼音（3D空間音）")]
        public AudioSource fireBurnSound;
        [Tooltip("火災警報音（2D UI音、コックピット内）")]
        public AudioSource fireAlarmSound;

        [Header("ドップラー制御")]
        [Tooltip("ドップラーコライダーGameObject - この範囲内ではSpatialBlend=0（2D音）、範囲外では1（3D音）")]
        public GameObject dopplerCollider;
        [Tooltip("範囲外（地上から聞く）でのSpatialBlend")]
        [Range(0f, 1f)]
        public float spatialBlendOutside = 1.0f;
        [Tooltip("範囲内（コックピット内）でのSpatialBlend")]
        [Range(0f, 1f)]
        public float spatialBlendInside = 0.0f;

        [Header("消火システム")]
        [Tooltip("消火剤投入時の火災消火成功率 (0-1)")]
        [Range(0f, 1f)]
        public float extinguishSuccessRate = 0.85f;
        [Tooltip("ファイヤーハンドル引いた状態で燃料カット")]
        public bool fireHandleCutsFuel = true;
        [Tooltip("ファイヤーハンドル引いた状態で油圧カット（将来の拡張用）")]
        public bool fireHandleCutsHydraulics = false;

        [Header("エフェクト")]
        public ParticleSystem jetBlastParticle;
        public Transform intakeTransform;
        public Transform exhaustTransform;
        public float intakeHazardRadius = 2f;
        public float exhaustHazardRadius = 3f;
        public float exhaustHazardDistance = 10f;

        [Header("状態インジケータ")]
        [Tooltip("エンジン起動中に有効化するGameObject（PowerBus/BleedAirBusからの参照用）")]
        public GameObject engineOnIndicator;

        [System.NonSerialized] public SaccEntity EntityControl;

        [UdonSynced(UdonSyncMode.None)] public bool reversing = false;
        [UdonSynced(UdonSyncMode.None)] public bool starter = false;
        [UdonSynced(UdonSyncMode.None)] public bool fuel = false;
        [UdonSynced(UdonSyncMode.Smooth)] public float N1 = 0f;
        [UdonSynced(UdonSyncMode.Smooth)] public float N2 = 0f;
        [UdonSynced(UdonSyncMode.Smooth)] public float EGT = 0f;
        [UdonSynced(UdonSyncMode.Smooth)] public float ECT = 0f;
        [UdonSynced(UdonSyncMode.None)] public bool fire = false;
        [UdonSynced(UdonSyncMode.None)] public bool fireHandlePulled = false;
        [UdonSynced(UdonSyncMode.None)] public bool fireAlarmMuted = false;

        [UdonSynced] private int _StateInt = 0;

        /// <summary>
        /// エンジン状態（外部スクリプトから読み取り可能）
        /// </summary>
        public EngineState State
        {
            get => (EngineState)_StateInt;
            private set { _StateInt = (int)value; }
        }

        private bool isOwner;
        private float throttleInput, reverserPosition, appliedThrust, appliedWindmillingDrag;
        private float idleVol, insideVol, thrustVol, takeoffVol, reverserVol, afterburnerVol;
        private float idlePit, insidePit, thrustPit, takeoffPit, reverserPit, afterburnerPit;
        private ParticleSystem.MainModule jetBlastMain;
        private float jetBlastInitialSpeed;
        private float continuousPowerTime; // 高出力連続運転時間
        private float afterburnerLevel; // アフターバーナーレベル (0-1)
        private float vehicleMass; // 機体質量 (kg)
        private bool localPlayerInDopplerZone; // ローカルプレイヤーがドップラーコライダー内にいるか

        void Start()
        {
            // テスト環境用: SFEXT_L_EntityStartが呼ばれない場合の初期化
            if (EntityControl == null)
            {
                SFEXT_L_EntityStart();
            }
        }

        public void SFEXT_L_EntityStart()
        {
            isOwner = EntityControl != null ? EntityControl.IsOwner : true;

            // 推力計算用の質量を決定
            if (useManualMass && engineCalculationMass > 0f)
            {
                vehicleMass = engineCalculationMass;
            }
            else if (SAVControl != null)
            {
                var rigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
                if (rigidbody != null)
                {
                    vehicleMass = rigidbody.mass;
                }
                else
                {
                    vehicleMass = engineCalculationMass;
                }
            }
            else
            {
                vehicleMass = engineCalculationMass;
            }


            // AudioSource初期化（すべてループ、初期無効）
            if (idleSound) { idleVol = idleSound.volume; idlePit = idleSound.pitch; idleSound.loop = true; idleSound.volume = 0f; idleSound.gameObject.SetActive(false); }
            if (insideSound) { insideVol = insideSound.volume; insidePit = insideSound.pitch; insideSound.loop = true; insideSound.volume = 0f; insideSound.gameObject.SetActive(false); }
            if (thrustSound) { thrustVol = thrustSound.volume; thrustPit = thrustSound.pitch; thrustSound.loop = true; thrustSound.volume = 0f; thrustSound.gameObject.SetActive(false); }
            if (takeoffSound) { takeoffVol = takeoffSound.volume; takeoffPit = takeoffSound.pitch; takeoffSound.loop = true; takeoffSound.volume = 0f; takeoffSound.gameObject.SetActive(false); }
            if (reverserSound) { reverserVol = reverserSound.volume; reverserPit = reverserSound.pitch; reverserSound.loop = true; reverserSound.volume = 0f; reverserSound.gameObject.SetActive(false); }
            if (afterburnerSound) { afterburnerVol = afterburnerSound.volume; afterburnerPit = afterburnerSound.pitch; afterburnerSound.loop = true; afterburnerSound.volume = 0f; afterburnerSound.gameObject.SetActive(false); }

            // 火災音初期化（初期無効）
            if (fireBurnSound) { fireBurnSound.loop = true; fireBurnSound.volume = 0f; fireBurnSound.gameObject.SetActive(false); }
            if (fireAlarmSound) { fireAlarmSound.loop = true; fireAlarmSound.volume = 0f; fireAlarmSound.gameObject.SetActive(false); }

            // ドップラーコライダー初期化（初期状態: 範囲外）
            localPlayerInDopplerZone = false;
            SetSpatialBlend(spatialBlendOutside);

            if (jetBlastParticle)
            {
                jetBlastMain = jetBlastParticle.main;
                jetBlastInitialSpeed = jetBlastMain.startSpeed.constant;
            }

            ResetEngine();
        }

        public void SFEXT_O_TakeOwnership()
        {
            isOwner = true;
        }

        public void SFEXT_O_LoseOwnership()
        {
            isOwner = false;
        }

        public void SFEXT_G_Explode()
        {
            ResetEngine();
        }

        public void SFEXT_G_RespawnButton()
        {
            ResetEngine();
        }

        public void SFEXT_G_ReSupply()
        {
            ResetEngine();
        }

        /// <summary>
        /// ファイヤーハンドルを引く/戻す
        /// </summary>
        public void ToggleFireHandle()
        {
            if (!isOwner) return;
            fireHandlePulled = !fireHandlePulled;
            RequestSerialization();
        }

        /// <summary>
        /// 消火剤投入（1回のみ使用可能、確率的に消火）
        /// </summary>
        public void DischargeExtinguisher()
        {
            if (!isOwner) return;
            if (!fire) return;

            // 確率的に消火
            float roll = UnityEngine.Random.value;
            if (roll < extinguishSuccessRate)
            {
                fire = false;
                RequestSerialization();
            }
        }

        /// <summary>
        /// 火災警報音のみミュート/アンミュート
        /// </summary>
        public void ToggleFireAlarmMute()
        {
            if (!isOwner) return;
            fireAlarmMuted = !fireAlarmMuted;
            RequestSerialization();
        }

        private void ResetEngine()
        {
            // Synced変数のリセット
            N1 = N2 = 0f;
            EGT = ECT = ambientTemp; // 大気温度で初期化
            reversing = starter = fuel = fire = false;
            fireHandlePulled = fireAlarmMuted = false;

            // エンジン状態リセット
            State = EngineState.Off;

            // ローカル変数のリセット
            reverserPosition = 0f;
            continuousPowerTime = 0f;
            afterburnerLevel = 0f;

            // ThrottleStrengthから適用済み推力を除去
            if (SAVControl != null && appliedThrust != 0f && vehicleMass > 0f)
            {
                float appliedAcceleration = appliedThrust / vehicleMass;
                float t = (float)SAVControl.GetProgramVariable("ThrottleStrength");
                SAVControl.SetProgramVariable("ThrottleStrength", t - appliedAcceleration);
                appliedThrust = 0f;
            }

            // Windmilling抗力を解除
            if (SAVControl != null && appliedWindmillingDrag != 0f)
            {
                float currentDrag = (float)SAVControl.GetProgramVariable("ExtraDrag");
                SAVControl.SetProgramVariable("ExtraDrag", currentDrag - appliedWindmillingDrag);
                appliedWindmillingDrag = 0f;
            }

            // Udon同期変数の変更を同期
            if (isOwner)
            {
                RequestSerialization();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (isOwner)
            {
                if (SAVControl != null)
                {
                    throttleInput = (float)SAVControl.GetProgramVariable("ThrottleInput");
                }
                UpdateEngineState(dt);
                UpdateEngine(dt);
                UpdateDamage(dt);
                UpdateWindmillingDrag(dt);
            }

            UpdateAnimation();
            UpdateSound();
            UpdateEffects();
            UpdatePlayerHazards();
            UpdateEngineOnIndicator();
        }

        private void UpdateEngine(float dt)
        {
            // N1/N2更新（状態別メソッドに委譲）
            UpdateN1N2(dt);

            // 温度更新
            // EGT: 燃焼時のみ上昇
            float targetEGT = State == EngineState.Running
                ? TSFEUtil.ClampedRemap(N1, idleN1, takeOffN1, idleEGT, takeOffEGT)
                : (fire ? fireEGT : ambientTemp);
            EGT = Mathf.Lerp(EGT, targetEGT, egtResponse * dt);

            // ECT: N2回転による圧縮熱・摩擦熱で上昇
            float targetECT = ambientTemp;
            if (fire)
            {
                targetECT = fireECT;
            }
            else if (N2 > 0f && State != EngineState.Seized)
            {
                // N2回転中はECT上昇 (Starter時: 約50°C、アイドル以上: idleECT～)
                float n2Norm = Mathf.Clamp01(N2 / idleN2);
                if (State == EngineState.Running)
                {
                    // 燃焼中: N1に基づく温度
                    targetECT = TSFEUtil.ClampedRemap(N1, idleN1, referenceN1, idleECT, continuousECT);
                }
                else
                {
                    // Starter時: 圧縮熱のみ (ambient + 35°C程度)
                    targetECT = ambientTemp + 35f * n2Norm;
                }
            }
            float ectResp = ECT > targetECT ? ectOverheatResponse : ectResponse;
            ECT = Mathf.Lerp(ECT, targetECT, ectResp * dt);

            // 逆噴射更新
            float revTarget = reversing ? 1f : 0f;
            float revResp = reversing ? reverserExtractResponse : reverserRetractResponse;
            reverserPosition = Mathf.MoveTowards(reverserPosition, revTarget, revResp * dt);

            // 推力計算
            // N1が0～idleN1: 0～idleThrustRatio、idleN1～takeOffN1: idleThrustRatio～100%
            float n1Norm = Mathf.Clamp01(N1 / takeOffN1);
            float thrustRatio = 0f;

            // エンジン停止中は推力0（安全チェック）
            if (State != EngineState.Running || N1 < 0.01f)
            {
                thrustRatio = 0f;
            }
            else if (N1 < idleN1)
            {
                // 0～idleN1の範囲: 線形に0～idleThrustRatioまで上昇
                thrustRatio = idleThrustRatio * Mathf.Clamp01(N1 / idleN1);
            }
            else
            {
                // idleN1～takeOffN1の範囲: idleThrustRatio～100%まで曲線的に上昇
                float t = Mathf.Clamp01((N1 - idleN1) / (takeOffN1 - idleN1));
                thrustRatio = Mathf.Lerp(idleThrustRatio, 1f, Mathf.Pow(t, thrustCurve));
            }
            float thrust = maxThrust * thrustRatio;
            float baseThrust = thrust; // アフターバーナー適用前の推力を保存

            // アフターバーナー適用
            if (hasAfterburner && afterburnerLevel > 0.01f)
            {
                float abMultiplier = Mathf.Lerp(1f, afterburnerThrustMultiplier, afterburnerLevel);
                thrust *= abMultiplier;

            }

            if (reverserPosition > 0f) thrust *= -(reverserRatio * reverserPosition);

            // デバッグログ（推力計算）
            if (Time.frameCount % 60 == 0) // 1秒ごと
            {
                Debug.Log($"[Engine Thrust] State={State}, N1={N1:F0}RPM, thrustRatio={thrustRatio:F3}, thrust={thrust:F0}N, appliedThrust={appliedThrust:F0}N");
            }

            // 推力適用
            if (SAVControl != null && vehicleMass > 0f)
            {
                // SaccAirVehicleの_EngineOn変数を制御
                bool savEngineOn = (bool)SAVControl.GetProgramVariable("_EngineOn");
                bool shouldBeOn = (State == EngineState.Running);

                if (shouldBeOn && !savEngineOn)
                {
                    SAVControl.SetProgramVariable("_EngineOn", true);
                }
                else if (!shouldBeOn && savEngineOn)
                {
                    SAVControl.SetProgramVariable("_EngineOn", false);
                }

                // Running状態の時のみ推力を適用
                if (State == EngineState.Running)
                {
                    // 動的ThrottleStrength調整
                    if (enableDynamicThrust)
                    {
                        // 現在のThrottleStrengthを取得
                        float currentThrottleStrength = (float)SAVControl.GetProgramVariable("ThrottleStrength");

                        // このエンジンの推力加速度を計算 (N → m/s²)
                        float thrustAcceleration = thrust / vehicleMass;

                        // 前回適用した推力との差分を計算
                        float appliedAcceleration = appliedThrust / vehicleMass;
                        float deltaAcceleration = thrustAcceleration - appliedAcceleration;

                        // ThrottleStrengthに差分を加算（複数エンジンの場合、各エンジンが自分の貢献分を加算）
                        float newThrottleStrength = currentThrottleStrength + deltaAcceleration;
                        SAVControl.SetProgramVariable("ThrottleStrength", newThrottleStrength);
                    }

                    // 非対称推力システム (トルク生成)
                    if (enableAsymmetricThrust)
                    {
                        // VehicleRigidbodyを取得
                        object rbObj = SAVControl.GetProgramVariable("VehicleRigidbody");
                        if (rbObj != null)
                        {
                            Rigidbody vehicleRb = (Rigidbody)rbObj;

                            // エンジン位置を取得 (nullの場合はこのGameObjectの位置)
                            Transform engPos = enginePosition != null ? enginePosition : transform;

                            // ワールド座標でのエンジン位置から機体重心までのベクトル
                            Vector3 rVector = engPos.position - vehicleRb.worldCenterOfMass;

                            // 推力方向 (エンジンのローカルZ軸 = 前方)
                            Vector3 thrustForce = engPos.forward * thrust;

                            // トルク計算: τ = r × F
                            // エンジンが左右に配置されている場合、片側停止で機体がヨーイングする
                            Vector3 torque = Vector3.Cross(rVector, thrustForce);

                            // Rigidbodyに直接トルクを適用 (ForceMode.Force = 連続的な力)
                            vehicleRb.AddTorque(torque, ForceMode.Force);
                        }
                    }

                    appliedThrust = thrust;
                }
                else
                {
                    // Running以外の状態では推力をゼロに保つ
                    appliedThrust = 0f;
                }
            }

            // 燃料消費
            if (enableFuelConsumption && SAVControl != null && State == EngineState.Running)
            {
                // 推力比率に基づいて燃料消費率を計算
                float fuelFlow = Mathf.Lerp(idleFuelFlow, maxFuelFlow, thrustRatio);

                // アフターバーナー使用時は燃料消費が増加
                if (hasAfterburner && afterburnerLevel > 0.01f)
                {
                    fuelFlow *= Mathf.Lerp(1f, afterburnerFuelMultiplier, afterburnerLevel);
                }

                // 燃料を消費
                float currentFuel = (float)SAVControl.GetProgramVariable("Fuel");
                float newFuel = Mathf.Max(0f, currentFuel - fuelFlow * dt);
                SAVControl.SetProgramVariable("Fuel", newFuel);

                // 燃料切れの場合、自動的にエンジン停止
                if (newFuel <= 0f && fuel)
                {
                    fuel = false;
                    RequestSerialization();
                    Debug.Log("[AdvancedEngine] Fuel exhausted - Engine shutting down");
                }
            }
        }

        private void UpdateDamage(float dt)
        {
            if (State == EngineState.Seized) return;

            // 高出力連続運転制限チェック
            if (enableContinuousPowerLimit && State == EngineState.Running)
            {
                // 現在の推力比率を計算
                float thrustRatio = 0f;
                if (N1 >= idleN1)
                {
                    float t = (N1 - idleN1) / (takeOffN1 - idleN1);
                    thrustRatio = Mathf.Lerp(idleThrustRatio, 1f, Mathf.Pow(t, thrustCurve));
                }

                // 制限閾値以上の出力か
                if (thrustRatio >= continuousPowerThreshold)
                {
                    continuousPowerTime += dt;

                    // 許容時間を超えた場合、故障判定開始
                    if (continuousPowerTime > continuousPowerTimeLimit)
                    {
                        float excessTime = continuousPowerTime - continuousPowerTimeLimit;
                        // 超過時間に応じてダメージ倍率を計算（1分超過で2倍、2分超過で3倍...）
                        float damageMultiplier = 1f + excessTime / 60f;

                        if (enableFailureSystem && TSFEUtil.CheckMTBFScaled(dt, mtbFireAtContinuousPower, damageMultiplier))
                        {
                            fire = true;
                            OnFireStart();
                        }
                    }
                }
                else
                {
                    // 出力が閾値未満に下がったらタイマーリセット
                    continuousPowerTime = 0f;
                }
            }

            // Afterburner logic
            if (hasAfterburner && State == EngineState.Running && !reversing)
            {
                float targetAB = (throttleInput > afterburnerThreshold) ? 1f : 0f;
                afterburnerLevel = Mathf.MoveTowards(afterburnerLevel, targetAB, afterburnerResponse * Mathf.Abs(targetAB - afterburnerLevel) * dt);
            }
            else
            {
                afterburnerLevel = Mathf.MoveTowards(afterburnerLevel, 0f, afterburnerResponse * afterburnerLevel * dt);
            }

            if (enableFailureSystem && !fire)
            {
                if (ECT >= overheatECT)
                {
                    float damage = (ECT - overheatECT) / (fireECT - overheatECT);
                    if (TSFEUtil.CheckMTBFScaled(dt, mtbFireAtOverheat, damage))
                    {
                        fire = true;
                        OnFireStart();
                    }
                }
                else if (ECT >= continuousECT)
                {
                    if (TSFEUtil.CheckMTBF(dt, mtbFireAtContinuous))
                    {
                        fire = true;
                        OnFireStart();
                    }
                }
            }

            if (enableFailureSystem && fire && TSFEUtil.CheckMTBF(dt, mtbMeltdownOnFire))
            {
                State = EngineState.Seized;
                fuel = starter = false;
            }
        }

        private void UpdateAnimation()
        {
            if (vehicleAnimator)
            {
                vehicleAnimator.SetFloat(n1ParameterName, N1 / takeOffN1);
                vehicleAnimator.SetFloat(n2ParameterName, N2 / takeOffN2);
                vehicleAnimator.SetFloat(reverserParameterName, reverserPosition);
                vehicleAnimator.SetBool(fireParameterName, fire);
                if (hasAfterburner)
                {
                    vehicleAnimator.SetFloat(afterburnerParameterName, afterburnerLevel);
                }
            }
        }

        private void UpdateSound()
        {
            float n1Norm = N1 / takeOffN1;
            float n2Norm = N2 / takeOffN2;

            // idleSound: N2が動いている間のみ再生
            if (idleSound)
            {
                bool shouldPlay = n2Norm > 0.01f; // N2が1%以上

                if (shouldPlay)
                {
                    // 有効化（Volume0で）
                    if (!idleSound.gameObject.activeInHierarchy)
                    {
                        idleSound.volume = 0f;
                        idleSound.gameObject.SetActive(true);
                        idleSound.Play();
                    }

                    idleSound.volume = idleVol * Mathf.Clamp01(n2Norm * 2f) * idleVolumeMultiplier;
                    idleSound.pitch = idlePit * (0.5f + n2Norm * 0.5f);
                }
                else
                {
                    // 無効化
                    if (idleSound.gameObject.activeInHierarchy)
                    {
                        idleSound.volume = 0f;
                        idleSound.Stop();
                        idleSound.gameObject.SetActive(false);
                    }
                }
            }

            // insideSound: N2が動いている間のみ再生
            if (insideSound)
            {
                bool shouldPlay = n2Norm > 0.01f;

                if (shouldPlay)
                {
                    // 有効化（Volume0で）
                    if (!insideSound.gameObject.activeInHierarchy)
                    {
                        insideSound.volume = 0f;
                        insideSound.gameObject.SetActive(true);
                        insideSound.Play();
                    }

                    insideSound.volume = insideVol * n2Norm * insideVolumeMultiplier;
                    insideSound.pitch = insidePit * (0.8f + n2Norm * 0.2f);
                }
                else
                {
                    // 無効化
                    if (insideSound.gameObject.activeInHierarchy)
                    {
                        insideSound.volume = 0f;
                        insideSound.Stop();
                        insideSound.gameObject.SetActive(false);
                    }
                }
            }

            // thrustSound: N1が動いている間のみ再生
            if (thrustSound)
            {
                bool shouldPlay = n1Norm > 0.01f; // N1が1%以上

                if (shouldPlay)
                {
                    // 有効化（Volume0で）
                    if (!thrustSound.gameObject.activeInHierarchy)
                    {
                        thrustSound.volume = 0f;
                        thrustSound.gameObject.SetActive(true);
                        thrustSound.Play();
                    }

                    thrustSound.volume = thrustVol * n1Norm * thrustVolumeMultiplier;
                    thrustSound.pitch = thrustPit * (0.7f + n1Norm * 0.3f);
                }
                else
                {
                    // 無効化
                    if (thrustSound.gameObject.activeInHierarchy)
                    {
                        thrustSound.volume = 0f;
                        thrustSound.Stop();
                        thrustSound.gameObject.SetActive(false);
                    }
                }
            }

            // takeoffSound: N1が80%以上でのみ再生
            if (takeoffSound)
            {
                bool shouldPlay = n1Norm > 0.8f; // N1が80%以上

                if (shouldPlay)
                {
                    // 有効化（Volume0で）
                    if (!takeoffSound.gameObject.activeInHierarchy)
                    {
                        takeoffSound.volume = 0f;
                        takeoffSound.gameObject.SetActive(true);
                        takeoffSound.Play();
                    }

                    // 音量: 80%以上で0→1に上昇
                    takeoffSound.volume = takeoffVol * Mathf.Max(0f, (n1Norm - 0.8f) * 5f) * takeoffVolumeMultiplier;
                    // ピッチ: 80%～100%で0.8→1.1に変化（N1に比例）
                    takeoffSound.pitch = takeoffPit * (0.8f + n1Norm * 0.3f);
                }
                else
                {
                    // 無効化
                    if (takeoffSound.gameObject.activeInHierarchy)
                    {
                        takeoffSound.volume = 0f;
                        takeoffSound.Stop();
                        takeoffSound.gameObject.SetActive(false);
                    }
                }
            }

            // 火災音の制御
            if (fireBurnSound)
            {
                if (fire)
                {
                    // 有効化
                    if (!fireBurnSound.gameObject.activeInHierarchy)
                    {
                        fireBurnSound.volume = 1f; // 火災音は常にフルボリューム
                        fireBurnSound.gameObject.SetActive(true);
                        fireBurnSound.Play();
                    }
                }
                else
                {
                    // 無効化
                    if (fireBurnSound.gameObject.activeInHierarchy)
                    {
                        fireBurnSound.Stop();
                        fireBurnSound.gameObject.SetActive(false);
                    }
                }
            }

            if (fireAlarmSound)
            {
                // 火災警報音: 火災発生中かつミュートされていない場合のみ再生
                if (fire && !fireAlarmMuted)
                {
                    // 有効化
                    if (!fireAlarmSound.gameObject.activeInHierarchy)
                    {
                        fireAlarmSound.volume = 1f; // 警報音は常にフルボリューム
                        fireAlarmSound.gameObject.SetActive(true);
                        fireAlarmSound.Play();
                    }
                }
                else
                {
                    // 無効化
                    if (fireAlarmSound.gameObject.activeInHierarchy)
                    {
                        fireAlarmSound.Stop();
                        fireAlarmSound.gameObject.SetActive(false);
                    }
                }
            }

            // reverserSound: リバーサー展開中（reverserPosition > 0.1）かつN1が動いている時のみ再生
            if (reverserSound)
            {
                // リバーサー展開度 (0-1) とN1の両方を考慮
                bool shouldPlay = reverserPosition > 0.1f && n1Norm > 0.1f;

                if (shouldPlay)
                {
                    // 有効化（Volume0で）
                    if (!reverserSound.gameObject.activeInHierarchy)
                    {
                        reverserSound.volume = 0f;
                        reverserSound.gameObject.SetActive(true);
                        reverserSound.Play();
                    }

                    // 音量: reverserPositionとN1の最大値を使用（より大きい音）
                    float volFactor = Mathf.Max(reverserPosition, n1Norm);
                    reverserSound.volume = reverserVol * volFactor * reverserVolumeMultiplier;
                    // ピッチ: N1に比例（0.7～1.0）
                    reverserSound.pitch = reverserPit * (0.7f + n1Norm * 0.3f);
                }
                else
                {
                    // 無効化
                    if (reverserSound.gameObject.activeInHierarchy)
                    {
                        reverserSound.volume = 0f;
                        reverserSound.Stop();
                        reverserSound.gameObject.SetActive(false);
                    }
                }
            }

            // afterburnerSound: アフターバーナー作動時
            if (afterburnerSound)
            {
                bool shouldPlay = hasAfterburner && afterburnerLevel > 0.01f;

                if (shouldPlay)
                {
                    // 有効化（Volume0で）
                    if (!afterburnerSound.gameObject.activeInHierarchy)
                    {
                        afterburnerSound.volume = 0f;
                        afterburnerSound.gameObject.SetActive(true);
                        afterburnerSound.Play();
                    }

                    // 音量: afterburnerLevelに比例
                    afterburnerSound.volume = afterburnerVol * afterburnerLevel * afterburnerVolumeMultiplier;
                    // ピッチ: afterburnerLevelに比例（0.9～1.1）
                    afterburnerSound.pitch = afterburnerPit * (0.9f + afterburnerLevel * 0.2f);
                }
                else
                {
                    // 無効化
                    if (afterburnerSound.gameObject.activeInHierarchy)
                    {
                        afterburnerSound.volume = 0f;
                        afterburnerSound.Stop();
                        afterburnerSound.gameObject.SetActive(false);
                    }
                }
            }

            // ドップラーコライダーチェック
            UpdateDopplerZone();
        }

        private void OnFireStart()
        {
            // 火災発生時の処理（音は UpdateSound() で自動的に再生される）
        }

        private void UpdateEffects()
        {
            if (jetBlastParticle)
            {
                jetBlastMain.startSpeed = jetBlastInitialSpeed * (N1 / takeOffN1);
            }
        }

        private void UpdatePlayerHazards()
        {
            if (State != EngineState.Running) return;

            VRCPlayerApi player = Networking.LocalPlayer;
            if (player == null) return;

            Vector3 playerPos = player.GetPosition();
            float n1Norm = N1 / takeOffN1;

            // 吸気ハザード
            if (intakeTransform != null)
            {
                Vector3 toPlayer = playerPos - intakeTransform.position;
                if (toPlayer.magnitude < intakeHazardRadius)
                {
                    Vector3 force = -toPlayer.normalized * n1Norm * 5f;
                    player.SetVelocity(player.GetVelocity() + force * Time.deltaTime);
                }
            }

            // 排気ハザード
            if (exhaustTransform != null)
            {
                Vector3 toPlayer = playerPos - exhaustTransform.position;
                float fwd = Vector3.Dot(toPlayer, exhaustTransform.forward);
                if (fwd > 0f && fwd < exhaustHazardDistance)
                {
                    float lat = Vector3.Cross(toPlayer, exhaustTransform.forward).magnitude;
                    if (lat < exhaustHazardRadius)
                    {
                        Vector3 force = exhaustTransform.forward * n1Norm * 20f;
                        player.SetVelocity(player.GetVelocity() + force * Time.deltaTime);
                    }
                }
            }
        }

        private bool CheckStarterPowerAvailable()
        {
            // starterPowerSourceがnull → 単独始動可能（テスト用、または自力始動エンジン）
            if (starterPowerSource == null) return true;

            // starterPowerSourceが有効 → 電源/ブリード供給あり
            return starterPowerSource.activeInHierarchy;
        }

        private void UpdateEngineOnIndicator()
        {
            if (engineOnIndicator != null)
            {
                engineOnIndicator.SetActive(State == EngineState.Running);
            }
        }

        /// <summary>
        /// ドップラーコライダー範囲チェック（Update内で呼ばれる）
        /// </summary>
        private void UpdateDopplerZone()
        {
            if (dopplerCollider == null)
            {
                Debug.Log("[AdvancedEngine] dopplerCollider is null");
                return;
            }

            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) return;

            // プレイヤー位置を取得
            Vector3 playerPos = localPlayer.GetPosition();

            // コライダーのBoundsを取得
            Collider col = dopplerCollider.GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning("[AdvancedEngine] dopplerCollider has no Collider component");
                return;
            }

            // プレイヤーがコライダー内にいるかチェック
            bool isInside = col.bounds.Contains(playerPos);

            // 状態が変化した場合のみSpatialBlendを更新
            if (isInside != localPlayerInDopplerZone)
            {
                localPlayerInDopplerZone = isInside;
                float blend = isInside ? spatialBlendInside : spatialBlendOutside;
                Debug.Log($"[AdvancedEngine] Player zone changed: isInside={isInside}, spatialBlend={blend}");
                SetSpatialBlend(blend);
            }
        }

        /// <summary>
        /// 全エンジンサウンドのSpatialBlendを設定
        /// </summary>
        private void SetSpatialBlend(float blend)
        {
            Debug.Log($"[AdvancedEngine] SetSpatialBlend: {blend}");
            if (idleSound != null)
            {
                idleSound.spatialBlend = blend;
                Debug.Log($"[AdvancedEngine] idleSound.spatialBlend = {idleSound.spatialBlend}");
            }
            if (insideSound != null) insideSound.spatialBlend = blend;
            if (thrustSound != null) thrustSound.spatialBlend = blend;
            if (takeoffSound != null) takeoffSound.spatialBlend = blend;
            if (reverserSound != null) reverserSound.spatialBlend = blend;
            if (afterburnerSound != null) afterburnerSound.spatialBlend = blend;
            if (fireBurnSound != null) fireBurnSound.spatialBlend = blend;
        }

        // ========================================================================
        // N1/N2更新ロジック（状態別）
        // ========================================================================

        private void UpdateN1N2(float dt)
        {
            switch (State)
            {
                case EngineState.Off:
                    // 慣性減衰（地上停止時のスピンダウン）
                    UpdateN1Decay(dt);
                    UpdateN2Decay(dt);
                    break;

                case EngineState.Starting:
                    if (n1Start)
                    {
                        UpdateN1FromStarter(dt);
                        UpdateN2FromN1Starting(dt);
                    }
                    else
                    {
                        UpdateN2FromStarter(dt);
                        UpdateN1FromN2Starting(dt);
                    }
                    break;

                case EngineState.Windmilling:
                    UpdateN1FromAirSpeed(dt);
                    UpdateN2FromN1Windmill(dt);
                    break;

                case EngineState.Running:
                    UpdateN2FromThrottle(dt);
                    UpdateN1FromN2(dt);
                    break;

                case EngineState.Seized:
                    UpdateN1Decay(dt);
                    UpdateN2Decay(dt);
                    break;
            }
        }

        // ===== N1軸スターター用（T-4等） =====
        private void UpdateN1FromStarter(float dt)
        {
            float targetN1 = takeOffN1 * starterTargetN1Ratio;
            N1 = Mathf.MoveTowards(N1, targetN1, n1StartupResponse * Mathf.Abs(N1 - targetN1) * dt);
        }

        private void UpdateN2FromN1Starting(float dt)
        {
            float targetN2 = N1 * startingN2toN1Ratio;
            N2 = Mathf.MoveTowards(N2, targetN2, n2StartupResponse * Mathf.Abs(N2 - targetN2) * dt);
        }

        // ===== N2軸スターター用（CFM56, 737等） =====
        private void UpdateN2FromStarter(float dt)
        {
            bool effectiveFuel = fuel && (!fireHandlePulled || !fireHandleCutsFuel);
            float target = effectiveFuel ? idleN2 : takeOffN2 * starterTargetN2Ratio;
            float resp = effectiveFuel ? n2Response : n2StartupResponse;
            N2 = Mathf.MoveTowards(N2, target, resp * Mathf.Abs(target - N2) * dt);
        }

        private void UpdateN1FromN2Starting(float dt)
        {
            // N2に応じてN1が機械的に追従
            float targetN1 = TSFEUtil.ClampedRemap(N2, 0f, idleN2, 0f, idleN1);
            N1 = Mathf.MoveTowards(N1, targetN1, n1StartupResponse * Mathf.Abs(N1 - targetN1) * dt);
        }

        // ===== Windmilling用 =====
        // NOTE: Windmilling RPMは速度に依存するため、高速飛行時はアイドルRPMより高くなることがあります。
        // これは物理的に正しい挙動です（例: 300kt飛行中のWindmilling N1 > 地上アイドル N1）
        private void UpdateN1FromAirSpeed(float dt)
        {
            if (SAVControl == null) return;

            float airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed");
            float atmosphere = (float)SAVControl.GetProgramVariable("Atmosphere");

            // Windmillingは動圧（IAS）に依存するため、TASをIASに変換
            // IAS = TAS * sqrt(atmosphere)
            float ias = airSpeed * Mathf.Sqrt(atmosphere);

            // KIASをm/sに変換
            float windmillingRefSpeed_ms = TSFEUtil.FromKnots(windmillingReferenceSpeed);

            float targetN1 = takeOffN1 * (ias / windmillingRefSpeed_ms)
                             * windmillingN1Ratio;
            float targetN1Unclamped = targetN1;
            targetN1 = Mathf.Clamp(targetN1, 0f, takeOffN1 * windmillingMaxN1Ratio);

            // デバッグログ（一時的）
            if (Time.frameCount % 60 == 0) // 1秒ごと
            {
                float targetN2 = targetN1 * windmillingN2toN1Ratio;
                float n2Pct = targetN2 / takeOffN2 * 100f;
                Debug.Log($"[Windmilling] TAS={airSpeed:F1}m/s, Atm={atmosphere:F2}, IAS={ias:F1}m/s ({TSFEUtil.ToKnots(ias):F0}KIAS), targetN1={targetN1Unclamped:F0}RPM→{targetN1:F0}RPM (idle={idleN1:F0}), targetN2={targetN2:F0}RPM ({n2Pct:F1}%)");
            }

            N1 = Mathf.MoveTowards(N1, targetN1, n1DecreaseResponse * Mathf.Abs(N1 - targetN1) * dt);
        }

        private void UpdateN2FromN1Windmill(float dt)
        {
            float targetN2 = N1 * windmillingN2toN1Ratio;
            N2 = Mathf.MoveTowards(N2, targetN2, n2DecreaseResponse * Mathf.Abs(N2 - targetN2) * dt);
        }

        // ===== Running用 =====
        private void UpdateN2FromThrottle(float dt)
        {
            float n2FromThrottle = Mathf.Lerp(idleN2, takeOffN2, throttleInput);
            float n2FromN1 = TSFEUtil.ClampedRemap(N1, idleN1, takeOffN1, idleN2, takeOffN2);
            float target = Mathf.Max(n2FromThrottle, n2FromN1);
            N2 = Mathf.MoveTowards(N2, target, n2Response * Mathf.Abs(target - N2) * dt);
        }

        private void UpdateN1FromN2(float dt)
        {
            float target = Mathf.Lerp(idleN1, takeOffN1, throttleInput);
            float n2Min = idleN2 * 0.99f;
            target = Mathf.Min(target, TSFEUtil.ClampedRemap(N2, n2Min, takeOffN2, idleN1, takeOffN1));
            float resp = target > N1 ? n1Response : n1DecreaseResponse;

            // デバッグログ（一時的）
            if (Time.frameCount % 60 == 0) // 1秒ごと
            {
                Debug.Log($"[Running] throttleInput={throttleInput:F2}, N1={N1:F0}RPM (target={target:F0}, idle={idleN1:F0}), N2={N2:F0}RPM");
            }

            N1 = Mathf.MoveTowards(N1, target, resp * Mathf.Abs(target - N1) * dt);
        }

        // ===== 慣性減衰用（Off/Seized状態） =====
        private void UpdateN1Decay(float dt)
        {
            N1 = Mathf.MoveTowards(N1, 0f, n1DecreaseResponse * N1 * 10f * dt);
        }

        private void UpdateN2Decay(float dt)
        {
            N2 = Mathf.MoveTowards(N2, 0f, n2DecreaseResponse * N2 * 10f * dt);
        }

        // ========================================================================
        // 状態遷移ロジック
        // ========================================================================

        private void UpdateEngineState(float dt)
        {
            EngineState prevState = State;
            bool starterPowerAvailable = CheckStarterPowerAvailable();
            float airSpeed = SAVControl != null ? (float)SAVControl.GetProgramVariable("AirSpeed") : 0f;
            float atmosphere = SAVControl != null ? (float)SAVControl.GetProgramVariable("Atmosphere") : 1f;
            bool effectiveFuel = fuel && (!fireHandlePulled || !fireHandleCutsFuel);

            // TASをIASに変換（Windmilling判定用）
            float ias = airSpeed * Mathf.Sqrt(atmosphere);
            float minWindmillSpeed_ms = TSFEUtil.FromKnots(minimumWindmillingSpeed);

            switch (State)
            {
                case EngineState.Off:
                    if (starter && starterPowerAvailable)
                        State = EngineState.Starting;
                    break;

                case EngineState.Starting:
                    // 自動燃料投入（N1軸スターターのみ）
                    if (n1Start && autoFuelInjection && !fuel && N1 >= takeOffN1 * autoIgnitionN1Threshold)
                    {
                        fuel = true;
                        RequestSerialization();
                    }

                    // Running状態への遷移判定
                    if (effectiveFuel && N2 >= takeOffN2 * minN2ForIgnition)
                    {
                        State = EngineState.Running;
                    }
                    break;

                case EngineState.Running:
                    // Seized状態への遷移はUpdateDamage()で直接設定されるため、ここでは燃料切れのみチェック
                    if (!effectiveFuel)
                    {
                        State = ias > minWindmillSpeed_ms
                            ? EngineState.Windmilling
                            : EngineState.Off;
                    }
                    break;

                case EngineState.Windmilling:
                    if (ias < minWindmillSpeed_ms && N1 < takeOffN1 * 0.01f)
                    {
                        State = EngineState.Off;
                    }
                    // Windmill始動：燃料投入のみで再始動（速度とN2が十分なら）
                    else if (effectiveFuel
                             && N2 >= takeOffN2 * minN2ForIgnition
                             && ias >= minWindmillSpeed_ms)
                    {
                        State = EngineState.Running;
                    }
                    // スターター使用による再始動（N2または速度が不足している場合）
                    else if (starter && starterPowerAvailable)
                    {
                        State = EngineState.Starting;
                    }
                    break;

                case EngineState.Seized:
                    // 固定状態（Respawn/Explodeまで）
                    break;
            }

            // 状態変更時の処理
            if (prevState != State && isOwner)
            {
                // Running以外に遷移した時、適用済み推力を除去
                if (prevState == EngineState.Running && State != EngineState.Running)
                {
                    if (SAVControl != null && appliedThrust != 0f && vehicleMass > 0f)
                    {
                        float appliedAcceleration = appliedThrust / vehicleMass;
                        float currentThrottle = (float)SAVControl.GetProgramVariable("ThrottleStrength");
                        SAVControl.SetProgramVariable("ThrottleStrength", currentThrottle - appliedAcceleration);
                        appliedThrust = 0f;
                    }
                }

                RequestSerialization();
            }

            // 自動スターターカット
            if (autoStarterCutoff && starter && State == EngineState.Starting)
            {
                bool shouldCutoff = n1Start
                    ? N1 >= idleN1 * starterCutoffThreshold
                    : N2 >= idleN2 * starterCutoffThreshold;

                if (shouldCutoff)
                {
                    starter = false;
                    RequestSerialization();
                }
            }

        }

        // ========================================================================
        // Windmilling抗力計算
        // ========================================================================

        private void UpdateWindmillingDrag(float dt)
        {
            if (SAVControl == null) return;

            if (State == EngineState.Windmilling && N1 > takeOffN1 * 0.01f)
            {
                float airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed");
                float atmosphere = (float)SAVControl.GetProgramVariable("Atmosphere");

                // TASをIASに変換
                float ias = airSpeed * Mathf.Sqrt(atmosphere);

                // KIASをm/sに変換
                float windmillingDragRefSpeed_ms = TSFEUtil.FromKnots(windmillingDragReferenceSpeed);

                // 速度²に比例する抗力（IASベース）
                float speedRatio = ias / windmillingDragRefSpeed_ms;
                float drag = windmillingDragCoefficient * (speedRatio * speedRatio);

                // ExtraDragに累積適用
                float currentDrag = (float)SAVControl.GetProgramVariable("ExtraDrag");
                SAVControl.SetProgramVariable("ExtraDrag",
                    currentDrag + drag - appliedWindmillingDrag);
                appliedWindmillingDrag = drag;
            }
            else if (appliedWindmillingDrag != 0f)
            {
                // 抗力解除
                float currentDrag = (float)SAVControl.GetProgramVariable("ExtraDrag");
                SAVControl.SetProgramVariable("ExtraDrag", currentDrag - appliedWindmillingDrag);
                appliedWindmillingDrag = 0f;
            }
        }

        public bool StarterPowerAvailable => CheckStarterPowerAvailable();
    }
}
