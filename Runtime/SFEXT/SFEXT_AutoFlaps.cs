using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;
using TSFE.DFUNC;

namespace TSFE.SFEXT
{
    /// <summary>
    /// オートフラップモード
    /// </summary>
    public enum AutoFlapMode
    {
        Civilian = 0,  // 民間機モード（速度ベース）
        Military = 1,  // 戦闘機モード（AoA/G/Mach対応）
        IDLC = 2       // 統合デジタル飛行制御（F-35風）
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_AutoFlaps : UdonSharpBehaviour
    {
        [Header("References")]
        [Tooltip("制御対象のAdvancedFlaps")]
        public DFUNC_AdvancedFlaps advancedFlaps;
        [Tooltip("SaccAirVehicle参照")]
        public UdonSharpBehaviour SAVControl;

        [Header("Debug")]
        [Tooltip("デバッグログを有効化")]
        public bool enableDebugLog = false;

        [Header("Initial State")]
        [Tooltip("起動時にオートフラップを有効化")]
        public bool autoEnabledOnStart = false;

        [Header("Mode")]
        [Tooltip("0=Civilian, 1=Military, 2=IDLC")]
        public int mode = 0;

        [Header("Schedule (parallel arrays - must be same length)")]
        [Tooltip("目標フラップ角度 deg")]
        public float[] scheduleFlapAngle = new float[0];
        [Tooltip("最大速度 KIAS（-1: 無視）")]
        public float[] scheduleSpeedMax = new float[0];
        [Tooltip("最小AoA deg（-1: 無視）")]
        public float[] scheduleAoaMin = new float[0];
        [Tooltip("最小Gロード（-1: 無視）")]
        public float[] scheduleGMin = new float[0];
        [Tooltip("最大Mach（-1: 無視）")]
        public float[] scheduleMachMax = new float[0];
        [Tooltip("優先度（高い方が勝つ）")]
        public float[] schedulePriority = new float[0];

        [Header("IDLC")]
        [Tooltip("アプローチ基準フラップ角（deg）")]
        public float idlcBaseAngle = 15f;
        [Tooltip("スティックピッチ入力あたりのフラップ変化（deg）")]
        public float idlcPitchGain = 2.0f;
        [Tooltip("スロットル変化率あたりのフラップ変化（deg）")]
        public float idlcThrottleGain = 1.0f;
        [Tooltip("フラップ角下限（deg）")]
        public float idlcAngleMin = 0f;
        [Tooltip("フラップ角上限（deg）")]
        public float idlcAngleMax = 30f;
        [Tooltip("この速度以上はCivilianにフォールバック（KIAS）")]
        public float idlcFallbackIAS = 250f;

        [Header("Hysteresis")]
        [Tooltip("展開方向：speedMaxにこの値を加えて評価")]
        public float extendHysteresisKnots = 5f;
        [Tooltip("過速度保護：VFEのこの値手前から収納")]
        public float retractMarginKnots = 3f;

        [Header("Inhibit")]
        [Tooltip("脚収納中は展開禁止")]
        public bool inhibitOnGearUp = false;
        [Tooltip("脚収納中の最大許容フラップ角度 deg")]
        public float inhibitMaxAngle = 0f;

        [Header("Timing")]
        [Tooltip("デテント変更の最短間隔（秒）")]
        public float changeDebounceTime = 0.3f;

        [System.NonSerialized] public SaccEntity EntityControl;

        // 内部状態
        private bool _autoActive = false;
        private float _commandedAngle = 0f;
        private float _lastChangeTime = 0f;
        private float _prevThrottle = 0f;

        public void SFEXT_L_EntityStart()
        {
            // 初期化
            _autoActive = autoEnabledOnStart;
            _commandedAngle = 0f;
            _lastChangeTime = -changeDebounceTime;
            _prevThrottle = GetCurrentThrottle();

            if (enableDebugLog)
            {
                Debug.Log($"[AutoFlaps] SFEXT_L_EntityStart - Initialized. advancedFlaps={(advancedFlaps != null ? "OK" : "NULL")}, SAVControl={(SAVControl != null ? "OK" : "NULL")}, AutoEnabled={_autoActive}");
            }
        }

        private void Update()
        {
            if (!_autoActive)
            {
                if (enableDebugLog && Time.frameCount % 300 == 0)
                {
                    Debug.Log("[AutoFlaps] Update - AutoFlap is NOT active");
                }
                return;
            }

            if (advancedFlaps == null || SAVControl == null)
            {
                if (enableDebugLog)
                {
                    Debug.LogError($"[AutoFlaps] Update - Missing references! advancedFlaps={(advancedFlaps != null ? "OK" : "NULL")}, SAVControl={(SAVControl != null ? "OK" : "NULL")}");
                }
                return;
            }

            // 飛行状態取得
            float ias = GetIASKnots();
            float aoa = GetAoADegrees();
            float g = GetGLoad();
            float mach = GetMach();

            if (enableDebugLog && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[AutoFlaps] Flight State - IAS:{ias:F1}kt, AoA:{aoa:F2}°, G:{g:F2}, Mach:{mach:F2}");
            }

            // 目標角度計算
            float targetAngle = ComputeTargetAngle(ias, aoa, g, mach);

            if (enableDebugLog && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[AutoFlaps] Computed Target - Angle:{targetAngle:F1}°, Mode:{mode}");
            }

            // 保護機能適用
            float targetBeforeProtection = targetAngle;
            targetAngle = ApplyProtections(targetAngle, ias);
            targetAngle = ApplyInhibits(targetAngle);

            if (enableDebugLog && !Mathf.Approximately(targetBeforeProtection, targetAngle))
            {
                Debug.Log($"[AutoFlaps] Protection Applied - Before:{targetBeforeProtection:F1}°, After:{targetAngle:F1}°");
            }

            // 範囲チェック（最小・最大デテント角度でクランプ）
            if (advancedFlaps.detents.Length > 0)
            {
                float minAngle = advancedFlaps.detents[0];
                float maxAngle = advancedFlaps.detents[advancedFlaps.detents.Length - 1];
                targetAngle = Mathf.Clamp(targetAngle, Mathf.Min(minAngle, maxAngle), Mathf.Max(minAngle, maxAngle));
            }

            // 角度変更（変化が0.5°以上の場合のみ）
            if (Mathf.Abs(targetAngle - _commandedAngle) > 0.5f && CanChange())
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[AutoFlaps] Angle Change - From:{_commandedAngle:F1}° To:{targetAngle:F1}°");
                }

                advancedFlaps.targetAngle = targetAngle;
                _commandedAngle = targetAngle;
                _lastChangeTime = Time.time;
            }
        }

        /// <summary>
        /// オートフラップのオン/オフ
        /// </summary>
        public void SetAutoFlap(bool enabled)
        {
            _autoActive = enabled;
            if (enableDebugLog)
            {
                Debug.Log($"[AutoFlaps] SetAutoFlap - AutoFlap is now {(enabled ? "ENABLED" : "DISABLED")}");
            }
        }

        /// <summary>
        /// オートフラップをトグル
        /// </summary>
        public void ToggleAutoFlap()
        {
            SetAutoFlap(!_autoActive);
            if (enableDebugLog)
            {
                Debug.Log($"[AutoFlaps] ToggleAutoFlap - Toggled to {(_autoActive ? "ENABLED" : "DISABLED")}");
            }
        }

        private float ComputeTargetAngle(float ias, float aoa, float g, float mach)
        {
            if (mode == 0)
            {
                return ComputeCivilian(ias);
            }
            else if (mode == 1)
            {
                return ComputeMilitary(ias, aoa, g, mach);
            }
            else if (mode == 2)
            {
                return ComputeIDLC();
            }
            return 0f;
        }

        /// <summary>
        /// Civilianモード（速度ベース）
        /// </summary>
        private float ComputeCivilian(float ias)
        {
            float result = 0f;
            int length = Mathf.Min(scheduleFlapAngle.Length, scheduleSpeedMax.Length);

            if (enableDebugLog && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[AutoFlaps] ComputeCivilian - IAS:{ias:F1}kt, ScheduleLength:{length}");
            }

            for (int i = 0; i < length; i++)
            {
                if (scheduleSpeedMax[i] < 0) continue;
                if (ias <= scheduleSpeedMax[i] + extendHysteresisKnots)
                {
                    result = Mathf.Max(result, scheduleFlapAngle[i]);
                    if (enableDebugLog && Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[AutoFlaps] ComputeCivilian - Entry[{i}]: SpeedMax:{scheduleSpeedMax[i]:F1}, FlapAngle:{scheduleFlapAngle[i]:F1}°, Result:{result:F1}°");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Militaryモード（AoA/G/Mach対応）
        /// </summary>
        private float ComputeMilitary(float ias, float aoa, float g, float mach)
        {
            float bestAngle = 0f;
            float bestPriority = -1f;

            int length = Mathf.Min(
                scheduleFlapAngle.Length,
                Mathf.Min(scheduleSpeedMax.Length,
                Mathf.Min(scheduleAoaMin.Length,
                Mathf.Min(scheduleGMin.Length,
                Mathf.Min(scheduleMachMax.Length, schedulePriority.Length))))
            );

            if (enableDebugLog && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[AutoFlaps] ComputeMilitary - IAS:{ias:F1}, AoA:{aoa:F2}, G:{g:F2}, Mach:{mach:F2}, ScheduleLength:{length}");
            }

            for (int i = 0; i < length; i++)
            {
                bool match = true;

                if (scheduleSpeedMax[i] >= 0 && ias > scheduleSpeedMax[i]) match = false;
                if (scheduleAoaMin[i] >= 0 && aoa < scheduleAoaMin[i]) match = false;
                if (scheduleGMin[i] >= 0 && g < scheduleGMin[i]) match = false;
                if (scheduleMachMax[i] >= 0 && mach > scheduleMachMax[i]) match = false;

                if (enableDebugLog && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[AutoFlaps] ComputeMilitary - Entry[{i}]: Match={match}, FlapAngle:{scheduleFlapAngle[i]:F1}°, Priority:{schedulePriority[i]}, SpeedOK:{(scheduleSpeedMax[i] < 0 || ias <= scheduleSpeedMax[i])}, AoAOK:{(scheduleAoaMin[i] < 0 || aoa >= scheduleAoaMin[i])}, GOK:{(scheduleGMin[i] < 0 || g >= scheduleGMin[i])}");
                }

                if (match && schedulePriority[i] > bestPriority)
                {
                    bestPriority = schedulePriority[i];
                    bestAngle = scheduleFlapAngle[i];
                    if (enableDebugLog && Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[AutoFlaps] ComputeMilitary - New Best: FlapAngle:{bestAngle:F1}°, Priority:{bestPriority}");
                    }
                }
            }

            return bestAngle;
        }

        /// <summary>
        /// IDLCモード（統合デジタル飛行制御）
        /// </summary>
        private float ComputeIDLC()
        {
            float ias = GetIASKnots();

            // 高速時はCivilianにフォールバック
            if (ias > idlcFallbackIAS)
            {
                return ComputeCivilian(ias);
            }

            // 目標角度計算
            float targetAngle = idlcBaseAngle
                + GetPitchStickInput() * idlcPitchGain
                - GetThrottleDelta() * idlcThrottleGain;

            targetAngle = Mathf.Clamp(targetAngle, idlcAngleMin, idlcAngleMax);

            return targetAngle;
        }

        /// <summary>
        /// 過速度保護適用
        /// </summary>
        private float ApplyProtections(float targetAngle, float ias)
        {
            float vfe = GetVFEForAngle(targetAngle);
            if (vfe > 0 && ias > vfe - retractMarginKnots)
            {
                return GetMaxSafeAngle(ias);
            }
            return targetAngle;
        }

        /// <summary>
        /// 脚収納時の制限適用
        /// </summary>
        private float ApplyInhibits(float targetAngle)
        {
            if (inhibitOnGearUp && IsGearUp())
            {
                return Mathf.Min(targetAngle, inhibitMaxAngle);
            }
            return targetAngle;
        }

        /// <summary>
        /// デテント変更可能か判定
        /// </summary>
        private bool CanChange()
        {
            return Time.time - _lastChangeTime >= changeDebounceTime;
        }

        /// <summary>
        /// 指定角度に対応するVFE（制限速度）を取得
        /// </summary>
        private float GetVFEForAngle(float angle)
        {
            if (advancedFlaps == null || advancedFlaps.detents.Length == 0) return 0f;

            // 最も近いデテントを検索
            int closestIndex = FindClosestDetentIndex(angle);
            if (closestIndex >= 0 && closestIndex < advancedFlaps.speedLimits.Length)
            {
                return advancedFlaps.speedLimits[closestIndex];
            }
            return 0f;
        }

        /// <summary>
        /// 現在速度で安全な最大フラップ角度を取得
        /// </summary>
        private float GetMaxSafeAngle(float ias)
        {
            if (advancedFlaps == null || advancedFlaps.detents.Length == 0) return 0f;

            // 速度制限を満たす最大デテントを検索
            for (int i = advancedFlaps.speedLimits.Length - 1; i >= 0; i--)
            {
                if (ias <= advancedFlaps.speedLimits[i])
                {
                    return advancedFlaps.detents[i];
                }
            }
            return advancedFlaps.detents[0];
        }

        /// <summary>
        /// 指定角度に最も近いデテントIndexを取得
        /// </summary>
        private int FindClosestDetentIndex(float angle)
        {
            if (advancedFlaps == null || advancedFlaps.detents.Length == 0) return 0;

            int bestIndex = 0;
            float bestDiff = Mathf.Abs(advancedFlaps.detents[0] - angle);

            for (int i = 1; i < advancedFlaps.detents.Length; i++)
            {
                float diff = Mathf.Abs(advancedFlaps.detents[i] - angle);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// スロットル変化量を取得（IDLCモード用）
        /// </summary>
        private float GetThrottleDelta()
        {
            float current = GetCurrentThrottle();
            float delta = current - _prevThrottle;
            _prevThrottle = current;
            return delta;
        }

        // ========== SaccAirVehicle データ取得ヘルパー ==========

        private float GetIASKnots()
        {
            if (SAVControl == null) return 0f;
            float airSpeedMS = (float)SAVControl.GetProgramVariable("AirSpeed");
            return TSFEUtil.ToKnots(airSpeedMS);
        }

        private float GetAoADegrees()
        {
            if (SAVControl == null) return 0f;

            // 低速時はAoA計算が不安定なので無視
            float airSpeedMS = (float)SAVControl.GetProgramVariable("AirSpeed");
            if (airSpeedMS < 2.57f) return 0f; // 5 KIAS未満

            // HUDと同じAngleOfAttackPitchフィールドを使用（すでに度数で格納されている）
            object aoaPitch = SAVControl.GetProgramVariable("AngleOfAttackPitch");
            if (aoaPitch != null)
            {
                float aoaDeg = (float)aoaPitch;

                // 異常な値（±60度を超える場合）は無視
                // 戦闘機でも60度超は失速後機動や異常姿勢を示すため除外
                if (Mathf.Abs(aoaDeg) > 60f)
                {
                    if (enableDebugLog && Time.frameCount % 300 == 0)
                    {
                        Debug.Log($"[AutoFlaps] GetAoADegrees - Abnormal AoA ignored: {aoaDeg:F2}°, AirSpeed:{airSpeedMS:F2} m/s");
                    }
                    return 0f;
                }

                if (enableDebugLog && Time.frameCount % 300 == 0)
                {
                    Debug.Log($"[AutoFlaps] GetAoADegrees - AoA:{aoaDeg:F2}°, AirSpeed:{airSpeedMS:F2} m/s");
                }

                return aoaDeg;
            }
            return 0f;
        }

        private float GetGLoad()
        {
            if (SAVControl == null) return 1f;
            // SaccAirVehicleにGForcesフィールドがあるか確認
            object gForce = SAVControl.GetProgramVariable("GForces");
            if (gForce != null)
            {
                return (float)gForce;
            }
            return 1f;
        }

        private float GetMach()
        {
            if (SAVControl == null) return 0f;
            // Mach数を概算（簡易計算: IAS / 音速340m/s）
            float airSpeedMS = (float)SAVControl.GetProgramVariable("AirSpeed");
            return airSpeedMS / 340f;
        }

        private float GetPitchStickInput()
        {
            if (SAVControl == null) return 0f;
            Vector3 rotInputs = (Vector3)SAVControl.GetProgramVariable("RotationInputs");
            return rotInputs.x; // x軸がピッチ
        }

        private float GetCurrentThrottle()
        {
            if (SAVControl == null) return 0f;
            float throttle = (float)SAVControl.GetProgramVariable("ThrottleStrength");
            return Mathf.Clamp01(throttle);
        }

        private bool IsGearUp()
        {
            if (SAVControl == null) return false;
            // Taxiingがfalseで空中にいる場合、ギアアップと推定
            bool taxiing = (bool)SAVControl.GetProgramVariable("Taxiing");
            return !taxiing;
        }
    }
}
