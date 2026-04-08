using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_AdvancedParkingBrake : UdonSharpBehaviour
    {
        public KeyCode desktopControl = KeyCode.N;
        public string parameterName = "parkingbrake";
        [Tooltip("地上ブレーキ強度 (m/s per second)")]
        public float groundBrakeStrength = 6f;
        [Tooltip("ブレーキが有効な最大速度 (m/s)")]
        public float groundBrakeSpeed = 40f;

        public UdonSharpBehaviour SAVControl;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;

        [System.NonSerialized] public bool LeftDial = false;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public SaccEntity EntityControl;

        [System.NonSerialized][UdonSynced][FieldChangeCallback(nameof(State))] private bool _state = false;
        public bool State
        {
            private set
            {
                _state = value;
                if (!initialized) return;
                if (vehicleAnimator) vehicleAnimator.SetBool(parameterName, value);
                TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, value);
                // SFEXT_AdvancedGearにも通知（オプション）
                if (advancedGears != null)
                {
                    foreach (var gear in advancedGears) gear.SetProgramVariable("Brake", value);
                }
            }
            get => _state;
        }

        private Animator vehicleAnimator;
        private Rigidbody vehicleRigidbody;
        private UdonSharpBehaviour[] advancedGears;
        private bool initialized, isPilot, selected, isOwner;
        private bool _triggerLastFrame;

        public void DFUNC_LeftDial() { }
        public void DFUNC_RightDial() { }
        public void DFUNC_Selected()
        {
            selected = true;
            // 非Ownerが選択した場合、Ownershipを取得
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }
        public void DFUNC_Deselected() { selected = false; }

        public void SFEXT_L_EntityStart()
        {
            vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
            vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");

            // SFEXT_AdvancedGearを検出（オプション連携用）
            var entity = EntityControl;
            var extBehaviours = entity.gameObject.GetComponentsInChildren<UdonSharpBehaviour>(true);
            var gearList = new UdonSharpBehaviour[extBehaviours.Length];
            int count = 0;
            foreach (var ext in extBehaviours)
            {
                var typeName = ext.GetType().Name;
                if (typeName == "SFEXT_AdvancedGear")
                {
                    gearList[count++] = ext;
                }
            }
            advancedGears = new UdonSharpBehaviour[count];
            System.Array.Copy(gearList, advancedGears, count);

            isOwner = EntityControl.IsOwner;
            gameObject.SetActive(false);
            initialized = true;
            State = false;
        }

        private void Toggle()
        {
            State = !State;
            RequestSerialization();
        }

        public void KeyboardInput() { Toggle(); }

        private void Update()
        {
            // 入力処理（isPilot または selected 時のみ）
            if (isPilot || selected)
            {
                if (Input.GetKeyDown(desktopControl)) Toggle();

                if (selected && Networking.LocalPlayer.IsUserInVR())
                {
                    var trigger = TSFEUtil.IsTriggerPressed(LeftDial);
                    if (trigger && !_triggerLastFrame) Toggle();
                    _triggerLastFrame = trigger;
                }
            }

            // パーキングブレーキ適用（Owner のみ）
            if (isOwner && State && vehicleRigidbody != null)
            {
                bool taxiing = (bool)SAVControl.GetProgramVariable("Taxiing");
                if (taxiing)
                {
                    float speed = (float)SAVControl.GetProgramVariable("Speed");
                    if (speed < groundBrakeSpeed)
                    {
                        // 地上ブレーキ: 速度をゼロに向けて減速
                        vehicleRigidbody.velocity = Vector3.MoveTowards(
                            vehicleRigidbody.velocity,
                            Vector3.zero,
                            groundBrakeStrength * Time.deltaTime
                        );
                    }
                }
            }
        }

        public void SFEXT_O_PilotEnter()
        {
            selected = false;
            isPilot = true;
        }
        public void SFEXT_O_PilotExit()
        {
            selected = false;
            isPilot = false;
        }
        public void SFEXT_O_TakeOwnership()
        {
            isOwner = true;
        }
        public void SFEXT_O_LoseOwnership()
        {
            isOwner = false;
        }
        public void SFEXT_G_PilotEnter() { gameObject.SetActive(true); }
        public void SFEXT_G_PilotExit() { gameObject.SetActive(false); }
        public void SFEXT_G_Explode() { ResetStatus(); }
        public void SFEXT_G_RespawnButton() { ResetStatus(); }

        private void ResetStatus()
        {
            State = false;
            RequestSerialization();
        }

        public void Set() { State = true; RequestSerialization(); }
        public void Release() { State = false; RequestSerialization(); }
    }
}
