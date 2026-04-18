using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_AdvancedSpeedBrake : UdonSharpBehaviour
    {
        public float liftMultiplier = 0.6f;
        public float dragMultiplier = 1.4f;
        public float response = 1.0f;

        [Header("Debug")]
        public bool enableDebugLog = false;

        [Header("Inputs")]
        public float vrInputDistance = 0.1f;
        public float incrementStep = 0.5f;
        public KeyCode desktopKey = KeyCode.B;

        [Header("Animation")]
        public string floatParameterName = "speedbrake";
        public string floatInputParameterName = "speedbrakeinput";

        public UdonSharpBehaviour SAVControl;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;

        [System.NonSerialized] public bool LeftDial = false;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public SaccEntity EntityControl;

        // 標準状態変数
        private bool isPilot, isOwner, selected, hasPilot;
        private VRCPlayerApi.TrackingDataType trackingTarget;
        private Transform controlsRoot;

        // コンポーネント固有の状態
        private Animator vehicleAnimator;
        private float lastDrag = 0f;  // 前フレームで追加したDrag値（標準DFUNC_Brakeと同じパターン）
        private float lastLift = 0f;  // 前フレームで追加したLift値

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(TargetAngle))] private float _targetAngle;
        public float TargetAngle
        {
            private set
            {
                var clamped = Mathf.Clamp01(value);
                if (vehicleAnimator) vehicleAnimator.SetFloat(floatInputParameterName, clamped);
                TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, clamped > 0);
                _targetAngle = clamped;
            }
            get => _targetAngle;
        }

        private float _angle;
        public float Angle
        {
            private set
            {
                var clamped = Mathf.Clamp01(value);
                if (vehicleAnimator) vehicleAnimator.SetFloat(floatParameterName, clamped);
                _angle = clamped;
            }
            get => _angle;
        }

        private Vector3 prevHandPosition;
        private bool _triggerState;
        private bool TriggerState
        {
            set
            {
                if (value && !_triggerState) OnTriggerDown();
                _triggerState = value;
            }
            get => _triggerState;
        }

        public void DFUNC_LeftDial()
        {
            trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        public void DFUNC_RightDial()
        {
            trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
        }
        public void DFUNC_Selected()
        {
            selected = true;

            // LeftDialに応じてtrackingTargetを設定（保険）
            trackingTarget = LeftDial
                ? VRCPlayerApi.TrackingDataType.LeftHand
                : VRCPlayerApi.TrackingDataType.RightHand;

            // 非Ownerが選択した場合、Ownershipを取得
            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }

        public void DFUNC_Deselected() { selected = false; }

        public void SFEXT_L_EntityStart()
        {
            vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
            controlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
            if (!controlsRoot) controlsRoot = EntityControl.transform;

            isOwner = Networking.IsOwner(gameObject);
            ResetStatus();
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            isOwner = true;
            selected = false;
        }

        public void SFEXT_O_PilotExit()
        {
            isPilot = false;
            selected = false;
            lastDrag = 0;
            lastLift = 0;
        }

        public void SFEXT_O_TakeOwnership() { isOwner = true; }
        public void SFEXT_O_LoseOwnership() { isOwner = false; }

        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            gameObject.SetActive(true);
        }

        public void SFEXT_G_PilotExit()
        {
            hasPilot = false;
            gameObject.SetActive(false);
        }

        public void SFEXT_G_Explode() { ResetStatus(); }
        public void SFEXT_G_RespawnButton() { ResetStatus(); }

        private void ResetStatus()
        {
            TargetAngle = 0;
            Angle = 0;
            lastDrag = 0;
            lastLift = 0;
        }

        private void Update()
        {
            if (!isOwner) return;  // 標準DFUNC_Brakeと同じパターン

            float deltaTime = Time.deltaTime;

            // isPilot（左席Owner）または selected（ダイヤル選択中）なら入力処理
            if (isPilot || selected)
            {
                TriggerState = selected && TSFEUtil.IsTriggerPressed(LeftDial);
                if (Input.GetKeyDown(desktopKey)) TargetAngle = 1.0f;
                else if (Input.GetKeyUp(desktopKey)) TargetAngle = 0.0f;
            }

            // Angleの更新
            if (!Mathf.Approximately(TargetAngle, Angle))
            {
                Angle = Mathf.MoveTowards(Angle, TargetAngle, response * deltaTime);
            }

            // 毎フレームExtraDrag/ExtraLiftを更新（標準DFUNC_Brakeと同じパターン）
            if (SAVControl)
            {
                var sav = SAVControl;
                bool piloting = (bool)sav.GetProgramVariable("Piloting");

                if (piloting)
                {
                    // Drag: 前フレームの値を引いて、新しい値を足す
                    float extraDrag = (float)sav.GetProgramVariable("ExtraDrag");
                    float newDrag = Angle * dragMultiplier;
                    float dragToAdd = -lastDrag + newDrag;
                    extraDrag += dragToAdd;
                    lastDrag = newDrag;
                    sav.SetProgramVariable("ExtraDrag", extraDrag);

                    // Lift: 同様に差分方式
                    float extraLift = (float)sav.GetProgramVariable("ExtraLift");
                    float newLift = Angle * liftMultiplier;
                    float liftToAdd = -lastLift + newLift;
                    extraLift += liftToAdd;
                    lastLift = newLift;
                    sav.SetProgramVariable("ExtraLift", extraLift);

                    if (enableDebugLog)
                    {
                        Debug.Log($"[SpeedBrake] Piloting={piloting}, Angle={Angle:F3}, ExtraDrag={extraDrag:F3} (added={dragToAdd:F3})");
                    }
                }
                else if (enableDebugLog)
                {
                    Debug.Log($"[SpeedBrake] NOT Piloting (isPilot={isPilot})");
                }
            }
        }

        private Vector3 GetLocalHandPosition()
        {
            return controlsRoot.InverseTransformPoint(Networking.LocalPlayer.GetTrackingData(trackingTarget).position);
        }

        private void OnTriggerDown()
        {
            prevHandPosition = GetLocalHandPosition();
        }

        public override void PostLateUpdate()
        {
            if (isPilot && TriggerState)
            {
                var handPos = GetLocalHandPosition();
                TargetAngle -= Vector3.Dot(handPos - prevHandPosition, Vector3.forward) / vrInputDistance;
                prevHandPosition = handPos;
            }
        }

        public void IncreaseAngle() { TargetAngle += incrementStep; }
        public void DecreaseAngle() { TargetAngle -= incrementStep; }
    }
}
