using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TSFE.Utility
{
    /// <summary>
    /// 電力バスシステム
    /// バッテリーとバス電源を独立して管理
    /// - Battery: ユーザーが手動でON/OFF（APU、計器、照明などで使用）
    /// - Bus Power: APU/エンジン/GPUのいずれかが稼働中（フラップ、油圧など）
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TSFE_PowerBus : UdonSharpBehaviour
    {
        [Header("バッテリーシステム（手動制御）")]
        [Tooltip("バッテリーON時に有効化するGameObject（APU始動、計器、照明用）\nnull=バッテリー機能無効")]
        public GameObject batteryPoweredIndicator;

        [Tooltip("バッテリーの初期状態（起動時/リスポーン時）")]
        public bool batteryInitialState = false;

        [Header("バス電源入力（自動判定）")]
        [Tooltip("APU起動状態GameObject（APUが起動中に有効化されているGameObject）")]
        public GameObject apuRunningIndicator;

        [Tooltip("エンジン起動状態GameObject配列（エンジンが起動中に有効化されているGameObject）")]
        public GameObject[] engineRunningIndicators;

        [Tooltip("GPU（地上電源）GameObject（接続中に有効化）")]
        public GameObject gpuObject;

        [Header("バス電源出力")]
        [Tooltip("更新間隔（秒）")]
        public float updateInterval = 0.1f;

        [Tooltip("バス電力供給中に有効化するGameObject（フラップ、油圧、空調用）")]
        public GameObject busPoweredIndicator;

        /// <summary>
        /// バッテリースイッチ状態（同期）
        /// </summary>
        [UdonSynced, FieldChangeCallback(nameof(BatteryOn))]
        private bool _batteryOn = false;
        public bool BatteryOn
        {
            get => _batteryOn;
            set
            {
                _batteryOn = value;
                // インジケータの更新はUpdatePowerState()で一元管理
                Debug.Log($"[PowerBus] Battery: {(value ? "ON" : "OFF")}");
            }
        }

        /// <summary>
        /// バス電力が供給されているか（読み取り専用）
        /// APU/エンジン/GPUのいずれかが稼働中
        /// </summary>
        [System.NonSerialized] public bool BusPowered = false;

        private float lastUpdateTime = 0f;
        private bool isOwner = false;

        void Start()
        {
            isOwner = Networking.IsOwner(gameObject);

            // バッテリーを初期状態に設定
            if (batteryPoweredIndicator != null)
            {
                _batteryOn = batteryInitialState;
            }

            // 初期状態を更新
            UpdatePowerState();
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            isOwner = Networking.IsOwner(gameObject);
        }

        public void SFEXT_G_RespawnButton()
        {
            ResetToInitialState();
        }

        public void SFEXT_G_Explode()
        {
            ResetToInitialState();
        }

        private void ResetToInitialState()
        {
            if (batteryPoweredIndicator != null && isOwner)
            {
                BatteryOn = batteryInitialState;
                RequestSerialization();
                Debug.Log($"[PowerBus] Reset to initial state: Battery={batteryInitialState}");
            }
        }

        void Update()
        {
            if (Time.time - lastUpdateTime < updateInterval)
                return;

            UpdatePowerState();
            lastUpdateTime = Time.time;
        }

        /// <summary>
        /// バッテリーをトグル（ON/OFF切り替え）
        /// </summary>
        public void ToggleBattery()
        {
            if (batteryPoweredIndicator == null) return;
            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            BatteryOn = !BatteryOn;
            RequestSerialization();
        }

        /// <summary>
        /// バッテリーをON
        /// </summary>
        public void SetBatteryOn()
        {
            if (batteryPoweredIndicator == null) return;
            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            BatteryOn = true;
            RequestSerialization();
        }

        /// <summary>
        /// バッテリーをOFF
        /// </summary>
        public void SetBatteryOff()
        {
            if (batteryPoweredIndicator == null) return;
            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            BatteryOn = false;
            RequestSerialization();
        }

        private void UpdatePowerState()
        {
            bool busPowered = false;

            // 1. APUチェック（最優先）
            if (apuRunningIndicator != null && apuRunningIndicator.activeInHierarchy)
            {
                busPowered = true;
            }

            // 2. エンジンチェック（1つでも動作していればOK）
            if (!busPowered && engineRunningIndicators != null)
            {
                foreach (var engineIndicator in engineRunningIndicators)
                {
                    if (engineIndicator == null) continue;
                    if (engineIndicator.activeInHierarchy)
                    {
                        busPowered = true;
                        break;
                    }
                }
            }

            // 3. GPUチェック（地上電源）
            if (!busPowered && gpuObject != null && gpuObject.activeInHierarchy)
            {
                busPowered = true;
            }

            BusPowered = busPowered;

            // バス電源インジケータGameObject更新
            if (busPoweredIndicator != null)
            {
                busPoweredIndicator.SetActive(busPowered);
            }

            // バッテリーインジケータ更新
            // バス電源が有効な場合は、バッテリースイッチOFFでもバッテリーインジケータを有効化
            // これにより、「バッテリーまたはバス電源」を簡単に判定できる
            if (batteryPoweredIndicator != null)
            {
                bool batteryIndicatorActive = BatteryOn || busPowered;
                batteryPoweredIndicator.SetActive(batteryIndicatorActive);
            }
        }

        /// <summary>
        /// 手動でバス電力状態を取得
        /// </summary>
        public bool IsBusPowered()
        {
            UpdatePowerState();
            return BusPowered;
        }

        /// <summary>
        /// バッテリー状態を取得
        /// </summary>
        public bool IsBatteryOn()
        {
            return BatteryOn;
        }

        /// <summary>
        /// バッテリーまたはバス電源のいずれかが使用可能か
        /// </summary>
        public bool IsAnyPowerAvailable()
        {
            UpdatePowerState();
            return BatteryOn || BusPowered;
        }
    }
}
