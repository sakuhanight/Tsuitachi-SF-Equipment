using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_AdvancedFlaps : UdonSharpBehaviour
    {
        [Header("Specs")]
        public float[] detents = { 0, 1, 2, 5, 10, 15, 25, 30, 40 };
        [Tooltip("KIAS")] public float[] speedLimits = { 340, 250, 250, 250, 210, 200, 190, 175, 162 };
        public float dragMultiplier = 1.4f;
        public float liftMultiplier = 1.35f;
        public float response = 1f;

        [Header("Power/Hydraulic")]
        [Tooltip("電力バス（TSFE_PowerBus）または油圧バス（TSFE_HydraulicBus）")]
        public UdonSharpBehaviour powerSource;

        [Tooltip("powerSourceがGameObjectの場合に使用（後方互換性）")]
        public GameObject powerSourceLegacy;

        [Header("Inputs")]
        [Tooltip("VRコントローラーの感度（1デテント移動に必要な距離、メートル）")]
        public float controllerSensitivity = 0.02f;
        [Tooltip("VR入力軸（ControlsRoot座標系）- 前方がフラップ格納方向")]
        public Vector3 vrInputAxis = Vector3.forward;
        [Tooltip("デスクトップ操作キー")]
        public KeyCode desktopKey = KeyCode.F;

        [Header("Animator")]
        public string boolParameterName = "flaps";
        public string angleParameterName = "flapsangle";
        public string targetAngleParameterName = "flapstarget";
        public string brokenParameterName = "flapsbroken";

        [Header("Sounds")]
        public AudioSource[] audioSources = { };
        public float soundResponse = 1;
        public AudioSource[] breakingSounds = { };

        [Header("Faults")]
        public float meanTimeBetweenActuatorBrokenOnOverspeed = 120.0f;
        public float meanTimeBetweenWingBrokenOnOverspeed = 240.0f;
        public float overspeedDamageMultiplier = 10.0f;
        public float brokenDragMultiplier = 2.9f;
        public float brokenLiftMultiplier = 0.3f;

        [Header("Haptics")]
        [Range(0, 1)] public float hapticDuration = 0.2f;
        [Range(0, 1)] public float hapticAmplitude = 0.5f;
        [Range(0, 1)] public float hapticFrequency = 0.1f;

        [Header("Debug")]
        [Tooltip("デバッグモード: VR入力のログ出力")]
        public bool debugMode = false;

        public UdonSharpBehaviour SAVControl;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;

        [System.NonSerialized] public bool LeftDial = false;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public SaccEntity EntityControl;

        [HideInInspector] public int targetDetentIndex, detentIndex;
        [HideInInspector] public float detentAngle, targetDetentAngle, speedLimit, targetSpeedLimit, angle, maxAngle;

        private Animator vehicleAnimator;
        [System.NonSerialized][UdonSynced(UdonSyncMode.Smooth)] public float targetAngle;
        [UdonSynced] private bool actuatorBroken;
        [UdonSynced][FieldChangeCallback(nameof(WingBroken))] private bool _wingBroken;
        private bool WingBroken
        {
            set
            {
                if (value == _wingBroken) return;
                _wingBroken = value;
                if (vehicleAnimator) vehicleAnimator.SetBool(brokenParameterName, value);
                if (value)
                {
                    foreach (var src in breakingSounds)
                    {
                        if (src) src.PlayScheduled(Random.value * 0.1f);
                    }
                }
            }
            get => _wingBroken;
        }

        private VRCPlayerApi.TrackingDataType trackingTarget;
        private bool hasPilot, isPilot, isOwner, selected;
        private Transform controlsRoot;
        private float[] audioVolumes, audioPitches;

        public void DFUNC_LeftDial()
        {
            trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
            if (debugMode)
            {
                Debug.Log($"[AdvancedFlaps] DFUNC_LeftDial called");
            }
        }
        public void DFUNC_RightDial()
        {
            trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
            if (debugMode)
            {
                Debug.Log($"[AdvancedFlaps] DFUNC_RightDial called");
            }
        }

        public void SFEXT_L_EntityStart()
        {
            var entity = EntityControl;
            vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
            controlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
            if (!controlsRoot) controlsRoot = entity.transform;

            maxAngle = detents[detents.Length - 1];

            audioVolumes = new float[audioSources.Length];
            audioPitches = new float[audioSources.Length];
            for (var i = 0; i < audioSources.Length; i++)
            {
                var src = audioSources[i];
                if (!src) continue;
                audioVolumes[i] = src.volume;
                audioPitches[i] = src.pitch;
            }

            ResetStatus();
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            isOwner = true;
            selected = false;
            if (debugMode)
            {
                Debug.Log($"[AdvancedFlaps] SFEXT_O_PilotEnter: isPilot=true, isOwner=true, selected=false");
            }
        }
        public void SFEXT_O_PilotExit()
        {
            isPilot = false;
            if (debugMode)
            {
                Debug.Log($"[AdvancedFlaps] SFEXT_O_PilotExit: isPilot=false");
            }
        }
        public void SFEXT_O_TakeOwnership() { isOwner = true; }
        public void SFEXT_O_LoseOwnership() { isOwner = false; }

        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit() { hasPilot = false; }
        public void SFEXT_G_Explode() { ResetStatus(); }
        public void SFEXT_G_RespawnButton() { ResetStatus(); }

        public void DFUNC_Selected()
        {
            selected = true;

            // LeftDialの値に応じてtrackingTargetを設定
            // DFUNC_LeftDial/RightDialが呼ばれない場合の保険
            if (LeftDial)
            {
                trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
            }
            else
            {
                trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
            }

            if (debugMode)
            {
                Debug.Log($"[AdvancedFlaps] DFUNC_Selected: LeftDial={LeftDial}, trackingTarget={trackingTarget}");
            }
            // 非Ownerが選択した場合、Ownershipを取得
            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }
        public void DFUNC_Deselected()
        {
            selected = false;
            if (debugMode)
            {
                Debug.Log($"[AdvancedFlaps] DFUNC_Deselected");
            }
        }

        private float prevAngle, prevTargetAngle;
        private void Update()
        {
            var dt = Time.deltaTime;

            UpdateDetents();

            if (isOwner) ApplyDamage(dt);

            var actuatorMoving = !actuatorBroken && IsPowerAvailable();
            UpdateSounds(dt, actuatorMoving);

            if (actuatorMoving) angle = Mathf.MoveTowards(angle, targetAngle, response * dt);

            var flapsChanged = !Mathf.Approximately(angle, prevAngle);
            prevAngle = angle;

            var targetChanged = !Mathf.Approximately(targetAngle, prevTargetAngle);
            prevTargetAngle = targetAngle;

            if (flapsChanged)
            {
                if (vehicleAnimator)
                {
                    vehicleAnimator.SetFloat(angleParameterName, angle / maxAngle);
                    vehicleAnimator.SetBool(boolParameterName, !Mathf.Approximately(angle, 0));
                }
                ApplyParameters();
            }

            if (targetChanged)
            {
                if (vehicleAnimator) vehicleAnimator.SetFloat(targetAngleParameterName, targetAngle / maxAngle);
            }

            if (!hasPilot && !flapsChanged) gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[AdvancedFlaps] LateUpdate: isPilot={isPilot}, selected={selected}");
            }

            if (isPilot) HandleInput();
        }

        private void ResetStatus()
        {
            angle = targetAngle = 0;
            actuatorBroken = false;
            WingBroken = false;

            var sav = SAVControl;
            sav.SetProgramVariable("ExtraDrag", (float)sav.GetProgramVariable("ExtraDrag") - appliedExtraDrag);
            sav.SetProgramVariable("ExtraLift", (float)sav.GetProgramVariable("ExtraLift") - appliedExtraLift);
            appliedExtraDrag = 0;
            appliedExtraLift = 0;

            gameObject.SetActive(false);
        }

        private bool prevTrigger;
        private Vector3 trackingOrigin;
        private int targetDetentIndexOrigin;
        private float triggerPressTime;
        private void HandleInput()
        {
            if (debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[AdvancedFlaps] HandleInput called: selected={selected}");
            }

            // VR入力（selected 時のみ）
            if (selected)
            {
                if (debugMode && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[AdvancedFlaps] selected=true, IsUserInVR={Networking.LocalPlayer.IsUserInVR()}, LeftDial={LeftDial}");
                }

                var trigger = TSFEUtil.IsTriggerPressed(LeftDial);
                var triggerChanged = prevTrigger != trigger;
                prevTrigger = trigger;

                if (triggerChanged)
                {
                    if (trigger)
                    {
                        triggerPressTime = Time.time;
                        if (debugMode)
                        {
                            Debug.Log($"[AdvancedFlaps] Trigger PRESSED at time={triggerPressTime:F2}");
                        }
                    }
                    else
                    {
                        if (debugMode)
                        {
                            float duration = Time.time - triggerPressTime;
                            Debug.Log($"[AdvancedFlaps] Trigger RELEASED after {duration:F2}s");
                        }
                    }
                }

                if (trigger)
                {
                    var trackingPosition = controlsRoot.InverseTransformPoint(Networking.LocalPlayer.GetTrackingData(trackingTarget).position);
                    if (triggerChanged)
                    {
                        trackingOrigin = trackingPosition;
                        targetDetentIndexOrigin = targetDetentIndex;
                        if (debugMode)
                        {
                            Debug.Log($"[AdvancedFlaps] Trigger press start: origin={trackingOrigin.ToString("F3")}, originDetent={targetDetentIndexOrigin}, currentTargetDetent={targetDetentIndex}");
                        }
                    }
                    else
                    {
                        // 移動量を計算（負=フラップダウン、正=フラップアップ）
                        var delta = Vector3.Dot(trackingPosition - trackingOrigin, vrInputAxis);
                        // デテント数に変換（四捨五入）
                        // 例: 2cm設定時、±1cmまで変化なし、±1cm超えで1デテント変化
                        var detentDelta = Mathf.RoundToInt(-delta / controllerSensitivity);
                        // 新しいデテントインデックスを計算
                        var newDetentIndex = Mathf.Clamp(targetDetentIndexOrigin + detentDelta, 0, detents.Length - 1);

                        if (debugMode && Time.frameCount % 10 == 0)
                        {
                            float holdTime = Time.time - triggerPressTime;
                            Debug.Log($"[AdvancedFlaps] holdTime={holdTime:F2}s, delta={delta:F4}m ({delta * 100:F1}cm), detentDelta={detentDelta}, originDetent={targetDetentIndexOrigin}, newDetent={newDetentIndex}");
                        }

                        // targetAngleを更新
                        if (newDetentIndex != targetDetentIndex)
                        {
                            targetAngle = detents[newDetentIndex];
                            if (debugMode)
                            {
                                Debug.Log($"[AdvancedFlaps] Detent changed: {targetDetentIndex} → {newDetentIndex}, angle={targetAngle}");
                            }
                        }
                    }
                }
            }

            // デスクトップ入力
            if (Input.GetKeyDown(desktopKey))
            {
                targetAngle = detents[(targetDetentIndex + 1) % detents.Length];
            }
        }

        private void UpdateDetents()
        {
            while (detentIndex > 0 && detents[detentIndex] > angle) detentIndex--;
            while (detentIndex < detents.Length - 1 && detents[detentIndex] < angle) detentIndex++;
            detentAngle = detents[detentIndex];

            var prev = targetDetentIndex;
            while (targetDetentIndex > 0 && detents[targetDetentIndex] > targetAngle) targetDetentIndex--;
            while (targetDetentIndex < detents.Length - 1 && detents[targetDetentIndex] < targetAngle) targetDetentIndex++;

            if (debugMode && targetDetentIndex != prev)
            {
                Debug.Log($"[AdvancedFlaps] UpdateDetents: targetDetentIndex changed {prev} → {targetDetentIndex}, targetAngle={targetAngle}");
            }

            if (isPilot && targetDetentIndex != prev)
                TSFEUtil.PlayHaptics(LeftDial, hapticDuration, hapticAmplitude, hapticFrequency);

            targetDetentAngle = detents[targetDetentIndex];
            targetSpeedLimit = speedLimits[targetDetentIndex];
            speedLimit = speedLimits[detentIndex];
        }

        private void UpdateSounds(float dt, bool actuatorAvailable)
        {
            var moving = actuatorAvailable && !Mathf.Approximately(targetAngle, angle);
            for (var i = 0; i < audioSources.Length; i++)
            {
                var src = audioSources[i];
                if (!src) continue;
                var volume = Mathf.Lerp(src.volume, moving ? audioVolumes[i] : 0.0f, soundResponse * dt);
                if (Mathf.Approximately(volume, 0))
                {
                    if (src.isPlaying) { src.Stop(); src.volume = 0; src.pitch = 0.8f; }
                }
                else
                {
                    src.volume = volume;
                    src.pitch = Mathf.Lerp(src.pitch, (moving ? 1.0f : 0.8f) * audioPitches[i], soundResponse * dt);
                    if (!src.isPlaying) { src.loop = true; src.time = src.clip.length * (Random.value % 1.0f); src.Play(); }
                }
            }
        }

        private void ApplyDamage(float dt)
        {
            var airSpeed = TSFEUtil.ToKnots((float)SAVControl.GetProgramVariable("AirSpeed"));
            var damage = Mathf.Max(airSpeed - speedLimit, 0) / speedLimit * overspeedDamageMultiplier;
            if (damage > 0)
            {
                if (!actuatorBroken && TSFEUtil.CheckMTBFScaled(dt, meanTimeBetweenActuatorBrokenOnOverspeed, damage))
                {
                    actuatorBroken = true;
                }
                if (!WingBroken && TSFEUtil.CheckMTBFScaled(dt, meanTimeBetweenWingBrokenOnOverspeed, damage))
                {
                    WingBroken = true;
                    actuatorBroken = true;
                    ApplyParameters();
                }
            }
        }

        private float appliedExtraDrag, appliedExtraLift;
        private void ApplyParameters()
        {
            var normalizedPosition = angle / maxAngle;
            var extraDrag = WingBroken ? brokenDragMultiplier - 1 : (dragMultiplier - 1) * normalizedPosition;
            var extraLift = WingBroken ? brokenLiftMultiplier - 1 : (liftMultiplier - 1) * normalizedPosition;

            var sav = SAVControl;
            var currentDrag = (float)sav.GetProgramVariable("ExtraDrag");
            var currentLift = (float)sav.GetProgramVariable("ExtraLift");

            sav.SetProgramVariable("ExtraDrag", currentDrag + extraDrag - appliedExtraDrag);
            sav.SetProgramVariable("ExtraLift", currentLift + extraLift - appliedExtraLift);

            appliedExtraDrag = extraDrag;
            appliedExtraLift = extraLift;
        }

        public void NextDetent()
        {
            targetAngle = detents[Mathf.Clamp(targetDetentIndex + 1, 0, detents.Length - 1)];
            UpdateDetents();
        }

        public void PreviousDetent()
        {
            targetAngle = detents[Mathf.Clamp(targetDetentIndex - 1, 0, detents.Length - 1)];
            UpdateDetents();
        }

        private bool IsPowerAvailable()
        {
            // TSFE_PowerBus または TSFE_HydraulicBus をチェック
            if (powerSource != null)
            {
                var typeName = powerSource.GetType().Name;
                if (typeName == "TSFE_PowerBus")
                {
                    object powered = powerSource.GetProgramVariable("Powered");
                    if (powered == null) return false;
                    if (powered.GetType() != typeof(bool)) return false;
                    return (bool)powered;
                }
                else if (typeName == "TSFE_HydraulicBus")
                {
                    object pressurized = powerSource.GetProgramVariable("Pressurized");
                    if (pressurized == null) return false;
                    if (pressurized.GetType() != typeof(bool)) return false;
                    return (bool)pressurized;
                }
            }

            // 後方互換性: GameObject
            if (powerSourceLegacy != null)
            {
                return powerSourceLegacy.activeInHierarchy;
            }

            // 電源設定なし = 常時動作
            return true;
        }
    }
}
