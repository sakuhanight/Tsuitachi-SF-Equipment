using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;

namespace TSFE.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SFEXT_Chock : UdonSharpBehaviour
    {
        [Header("References")]
        [Tooltip("チョークイン状態を可視化するGameObject配列（複数のギアボーン配下に配置）")]
        public GameObject[] chockVisuals;

        [Header("Settings")]
        [Tooltip("ブレーキ強度 (m/s per second)")]
        public float brakeStrength = 6.0f;
        [Tooltip("ブレーキが有効な最大速度 (m/s)")]
        public float maxBrakeSpeed = 5.0f;

        [Header("Initial State")]
        [Tooltip("初期状態: チョークを有効にするか")]
        public bool initialActive = true;

        [Header("Debug")]
        [Tooltip("デバッグモード: 動作時にログ出力")]
        public bool debugMode = false;

        [System.NonSerialized] public SaccEntity EntityControl;
        public UdonSharpBehaviour SAVControl;

        private Rigidbody vehicleRigidbody;
        private bool initialized;
        private bool _lastBrakeApplied;
        private bool _lastUpdateConditionLogged;

        [UdonSynced][FieldChangeCallback(nameof(Active))] private bool _active = false;
        public bool Active
        {
            set
            {
                if (debugMode)
                {
                    Debug.Log($"[SFEXT_Chock] Active changed: {_active} → {value} on {gameObject.name}");
                    Debug.Log($"[SFEXT_Chock] initialized={initialized}, chockVisuals={(chockVisuals != null ? chockVisuals.Length.ToString() : "null")}");
                }

                _active = value;
                UpdateVisuals();
            }
            get => _active;
        }

        private void UpdateVisuals()
        {
            if (chockVisuals == null || chockVisuals.Length == 0)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[SFEXT_Chock] chockVisuals is {(chockVisuals == null ? "null" : "empty")}");
                }
                return;
            }

            int updatedCount = 0;
            foreach (var visual in chockVisuals)
            {
                if (visual != null)
                {
                    visual.SetActive(_active);
                    updatedCount++;
                    if (debugMode)
                    {
                        Debug.Log($"[SFEXT_Chock] Set {visual.name}.SetActive({_active})");
                    }
                }
                else if (debugMode)
                {
                    Debug.LogWarning($"[SFEXT_Chock] chockVisuals contains null element at index {updatedCount}");
                }
            }

            if (debugMode)
            {
                Debug.Log($"[SFEXT_Chock] Updated {updatedCount}/{chockVisuals.Length} visual(s) to Active={_active}");
            }
        }

        public void SFEXT_L_EntityStart()
        {
            if (debugMode)
            {
                Debug.Log($"[SFEXT_Chock] SFEXT_L_EntityStart on {gameObject.name}");
            }

            vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");

            if (debugMode)
            {
                Debug.Log($"[SFEXT_Chock] VehicleRigidbody: {(vehicleRigidbody != null ? "OK" : "null")}");
                Debug.Log($"[SFEXT_Chock] Initial Active setting: {initialActive}");
                Debug.Log($"[SFEXT_Chock] Chock visuals count: {(chockVisuals != null ? chockVisuals.Length : 0)}");
            }

            initialized = true;

            // 初期状態を適用
            if (Networking.IsOwner(gameObject))
            {
                _active = initialActive;
                if (debugMode)
                {
                    Debug.Log($"[SFEXT_Chock] Set initial Active state to {_active} (Owner)");
                }
            }

            if (debugMode)
            {
                Debug.Log($"[SFEXT_Chock] Applying initial visual state: {_active}");
            }
            UpdateVisuals();
        }

        private void Update()
        {
            // Chock有効時のブレーキ処理（Ownerのみ）
            bool shouldApplyBrake = Active && EntityControl != null && EntityControl.IsOwner;

            if (debugMode && shouldApplyBrake != _lastUpdateConditionLogged)
            {
                if (shouldApplyBrake)
                {
                    Debug.Log($"[SFEXT_Chock] Update: Brake conditions met - Active={Active}, EntityControl={(EntityControl != null ? "OK" : "null")}, IsOwner={(EntityControl != null ? EntityControl.IsOwner.ToString() : "N/A")}");
                }
                else
                {
                    Debug.Log($"[SFEXT_Chock] Update: Brake conditions NOT met - Active={Active}, EntityControl={(EntityControl != null ? "OK" : "null")}, IsOwner={(EntityControl != null ? EntityControl.IsOwner.ToString() : "N/A")}");
                }
                _lastUpdateConditionLogged = shouldApplyBrake;
            }

            if (shouldApplyBrake)
            {
                ApplyBrake();
            }
        }

        private void ApplyBrake()
        {
            if (vehicleRigidbody == null || SAVControl == null)
            {
                if (debugMode && _lastBrakeApplied)
                {
                    Debug.LogWarning($"[SFEXT_Chock] Cannot apply brake: VehicleRigidbody={vehicleRigidbody != null}, SAVControl={SAVControl != null}");
                    _lastBrakeApplied = false;
                }
                return;
            }

            // Taxiing状態を確認
            bool taxiing = (bool)SAVControl.GetProgramVariable("Taxiing");
            if (!taxiing)
            {
                if (debugMode && _lastBrakeApplied)
                {
                    Debug.Log($"[SFEXT_Chock] Brake not applied: Not taxiing");
                    _lastBrakeApplied = false;
                }
                return;
            }

            // 速度を確認
            float speed = (float)SAVControl.GetProgramVariable("Speed");
            if (speed < maxBrakeSpeed)
            {
                // Rigidbodyの速度を減速
                vehicleRigidbody.velocity = Vector3.MoveTowards(
                    vehicleRigidbody.velocity,
                    Vector3.zero,
                    brakeStrength * Time.deltaTime
                );

                if (debugMode && !_lastBrakeApplied)
                {
                    Debug.Log($"[SFEXT_Chock] Applying brake: Speed={speed:F2} m/s, MaxBrakeSpeed={maxBrakeSpeed:F2} m/s, BrakeStrength={brakeStrength:F2} m/s²");
                    _lastBrakeApplied = true;
                }
            }
            else
            {
                if (debugMode && _lastBrakeApplied)
                {
                    Debug.Log($"[SFEXT_Chock] Brake not applied: Speed={speed:F2} m/s exceeds MaxBrakeSpeed={maxBrakeSpeed:F2} m/s");
                    _lastBrakeApplied = false;
                }
            }
        }

        /// <summary>
        /// チョークのトグル処理
        /// TSFE_InteractProxyから呼び出される
        /// </summary>
        public void ToggleChock()
        {
            if (debugMode)
            {
                Debug.Log($"[SFEXT_Chock] ToggleChock called on {gameObject.name}");
            }

            // Ownershipを取得
            bool wasOwner = Networking.IsOwner(gameObject);
            if (!wasOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                if (debugMode)
                {
                    Debug.Log($"[SFEXT_Chock] Ownership taken by {Networking.LocalPlayer.displayName}");
                }
            }

            // トグル
            bool newState = !Active;
            if (debugMode)
            {
                Debug.Log($"[SFEXT_Chock] Toggling: {Active} → {newState}");
            }
            Active = newState;
            RequestSerialization();
        }

        /// <summary>
        /// Interactで呼ばれるトグル処理（直接配置時用）
        /// </summary>
        public override void Interact()
        {
            if (debugMode)
            {
                Debug.Log($"[SFEXT_Chock] Interact called on {gameObject.name}");
            }
            ToggleChock();
        }
    }
}
