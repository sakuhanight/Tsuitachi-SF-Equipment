using UdonSharp;
using UnityEngine;

namespace TSFE.SFEXT
{
    /// <summary>
    /// SFEXT_AdvancedEngine テスト用のモック SaccAirVehicle
    /// 実際の SaccAirVehicle なしでエンジンをテスト可能
    ///
    /// Inspector でリアルタイム値を確認・操作できます
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MockSAVControl : UdonSharpBehaviour
    {
        [Header("推力・スロットル")]
        [Tooltip("スロットル入力 (0-1)")]
        public float ThrottleInput = 0f;
        [Tooltip("現在の推力 (N) - エンジンが自動設定")]
        public float ThrottleStrength = 0f;

        [Header("速度・高度")]
        [Tooltip("対気速度 (m/s)")]
        public float AirSpeed = 0f;
        [Tooltip("対気速度ベクトル (m/s)")]
        public Vector3 AirVel = Vector3.zero;
        [Tooltip("高度 (メートル)")]
        public float Altitude = 0f;
        [Tooltip("大気密度 (0-1, 1=海面)")]
        [Range(0f, 1f)]
        public float Atmosphere = 1f;

        [Header("燃料")]
        [Tooltip("現在の燃料量 (kg)")]
        public float Fuel = 5000f;
        [Tooltip("満タン燃料量 (kg)")]
        public float FullFuel = 10000f;

        [Header("物理・状態")]
        [Tooltip("機体Rigidbody（質量計算用）")]
        public Rigidbody VehicleRigidbody;
        [Tooltip("追加抗力 - エンジン/フラップ等が加算")]
        public float ExtraDrag = 0f;
        [Tooltip("追加揚力 - エンジン/フラップ等が加算")]
        public float ExtraLift = 0f;
        [Tooltip("地上走行中")]
        public bool Taxiing = true;
        [Tooltip("水上浮遊中")]
        public bool Floating = false;
        [Tooltip("ピッチダウン状態")]
        public bool PitchDown = false;

        [Header("エンジン状態（SaccAirVehicle互換）")]
        [Tooltip("エンジン稼働中")]
        public bool EngineOn = false;
        [Tooltip("エンジン出力 (0-1)")]
        [Range(0f, 1f)]
        public float EngineOutput = 0f;

        [Header("機体状態（SaccAirVehicle互換）")]
        [Tooltip("機体耐久値 (0-100)")]
        [Range(0f, 100f)]
        public float Health = 100f;
        [Tooltip("操縦中")]
        public bool Piloting = false;

        [Header("VRChat互換フィールド")]
        [Tooltip("VRモード")]
        public bool InVR = false;
        [Tooltip("エディタモード")]
        public bool InEditor = true;
        [Tooltip("オーナー")]
        public bool IsOwner = true;

        [Header("アニメーション・制御")]
        public Animator VehicleAnimator;
        public Transform ControlsRoot;
        public UdonSharpBehaviour EntityControl;

        void Start()
        {
            // AirVelをAirSpeedから初期化
            if (AirVel == Vector3.zero && AirSpeed > 0f)
            {
                AirVel = transform.forward * AirSpeed;
            }

            // Rigidbodyがnullなら自動取得
            if (VehicleRigidbody == null)
            {
                VehicleRigidbody = GetComponent<Rigidbody>();
            }
        }

        void Update()
        {
            // AirSpeedとAirVelを同期
            // AirSpeedが外部から変更された場合、AirVelを更新
            float currentMagnitude = AirVel.magnitude;

            if (!Mathf.Approximately(AirSpeed, currentMagnitude))
            {
                // AirSpeedが変更された → AirVelを更新
                if (AirSpeed > 0f)
                {
                    AirVel = transform.forward * AirSpeed;
                }
                else
                {
                    AirVel = Vector3.zero;
                }
            }
        }
    }
}
