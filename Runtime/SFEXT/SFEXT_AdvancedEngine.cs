using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(1000)]
    public class SFEXT_AdvancedEngine : UdonSharpBehaviour
    {
        [Header("動力系")]
        public float maxThrust = 130408.51f;
        public float thrustCurve = 2.0f;

        [Header("N1 (低圧) RPM")]
        public float idleN1 = 879.6f;
        public float referenceN1 = 4397f;
        public float takeOffN1 = 4586f;
        [Tooltip("N1上昇応答速度 (Idle→TakeOff: 約5秒)")]
        public float n1Response = 0.2f;
        [Tooltip("N1減少応答速度 (TakeOff→Idle: 約8秒)")]
        public float n1DecreaseResponse = 0.125f;
        [Tooltip("N1始動時応答速度 (未使用)")]
        public float n1StartupResponse = 0.01f;

        [Header("N2 (高圧) RPM")]
        public float idleN2 = 8583.5f;
        public float referenceN2 = 17167f;
        public float takeOffN2 = 20171f;
        [Tooltip("N2応答速度 (Fuel ON→Idle: 約20秒)")]
        public float n2Response = 0.05f;
        [Tooltip("N2減少応答速度")]
        public float n2DecreaseResponse = 0.04f;
        [Tooltip("N2始動応答速度 (Starter→30%: 約10秒)")]
        public float n2StartupResponse = 0.1f;

        [Header("温度 (°C)")]
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

        [Header("故障 MTBF (秒)")]
        public float mtbFireAtContinuous = 2592000f;
        public float mtbFireAtOverheat = 90f;
        public float mtbMeltdownOnFire = 90f;

        [Header("コンポーネント")]
        public UdonSharpBehaviour SAVControl;
        public Animator vehicleAnimator;
        public string n1ParameterName = "n1";
        public string n2ParameterName = "n2";
        public string reverserParameterName = "reverser";
        public string fireParameterName = "fire";

        [Header("サウンド")]
        public AudioSource idleSound;
        public AudioSource insideSound;
        public AudioSource thrustSound;
        public AudioSource takeoffSound;
        public AudioSource fireSound;

        [Header("エフェクト")]
        public ParticleSystem jetBlastParticle;
        public Transform intakeTransform;
        public Transform exhaustTransform;
        public float intakeHazardRadius = 2f;
        public float exhaustHazardRadius = 3f;
        public float exhaustHazardDistance = 10f;

        [System.NonSerialized] public SaccEntity EntityControl;

        [UdonSynced(UdonSyncMode.None)] public bool reversing = false;
        [UdonSynced(UdonSyncMode.None)] public bool starter = false;
        [UdonSynced(UdonSyncMode.None)] public bool fuel = false;
        [UdonSynced(UdonSyncMode.Smooth)] public float N1 = 0f;
        [UdonSynced(UdonSyncMode.Smooth)] public float N2 = 0f;
        [UdonSynced(UdonSyncMode.Smooth)] public float EGT = 0f;
        [UdonSynced(UdonSyncMode.Smooth)] public float ECT = 0f;
        [UdonSynced(UdonSyncMode.None)] public bool fire = false;

        private bool isOwner, engineOn, meltdown;
        private float throttleInput, reverserPosition, appliedThrust;
        private float idleVol, insideVol, thrustVol, takeoffVol;
        private float idlePit, insidePit, thrustPit, takeoffPit;
        private ParticleSystem.MainModule jetBlastMain;
        private float jetBlastInitialSpeed;

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

            if (idleSound) { idleVol = idleSound.volume; idlePit = idleSound.pitch; }
            if (insideSound) { insideVol = insideSound.volume; insidePit = insideSound.pitch; }
            if (thrustSound) { thrustVol = thrustSound.volume; thrustPit = thrustSound.pitch; }
            if (takeoffSound) { takeoffVol = takeoffSound.volume; takeoffPit = takeoffSound.pitch; }

            if (jetBlastParticle)
            {
                jetBlastMain = jetBlastParticle.main;
                jetBlastInitialSpeed = jetBlastMain.startSpeed.constant;
            }

            ResetEngine();
        }

        public void SFEXT_O_TakeOwnership() { isOwner = true; }
        public void SFEXT_O_LoseOwnership() { isOwner = false; }
        public void SFEXT_G_Explode() { ResetEngine(); }
        public void SFEXT_G_RespawnButton() { ResetEngine(); }

        private void ResetEngine()
        {
            N1 = N2 = EGT = ECT = reverserPosition = 0f;
            reversing = starter = fuel = fire = meltdown = engineOn = false;

            if (SAVControl != null && appliedThrust != 0f)
            {
                float t = (float)SAVControl.GetProgramVariable("ThrottleStrength");
                SAVControl.SetProgramVariable("ThrottleStrength", t - appliedThrust);
                appliedThrust = 0f;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (isOwner)
            {
                throttleInput = (float)SAVControl.GetProgramVariable("ThrottleInput");
                UpdateEngine(dt);
                UpdateDamage(dt);
            }

            UpdateAnimation();
            UpdateSound();
            UpdateEffects();
            UpdatePlayerHazards();
        }

        private void UpdateEngine(float dt)
        {
            engineOn = N2 >= idleN2 * 0.5f;

            // N2更新
            if (starter && !meltdown)
            {
                float target = fuel ? idleN2 : idleN2 * 0.3f;
                float resp = fuel ? n2StartupResponse : n2StartupResponse * 0.5f;
                N2 = Mathf.MoveTowards(N2, target, resp * Mathf.Abs(target - N2) * dt);
            }
            else if (engineOn && !meltdown)
            {
                float target = TSFEUtil.ClampedRemap(N1, idleN1, referenceN1, idleN2, referenceN2);
                N2 = Mathf.MoveTowards(N2, target, n2Response * Mathf.Abs(target - N2) * dt);
            }
            else
            {
                N2 = Mathf.MoveTowards(N2, 0f, n2DecreaseResponse * N2 * dt);
            }

            // N1更新
            if (engineOn && !meltdown)
            {
                float target = Mathf.Lerp(idleN1, takeOffN1, throttleInput);
                target = Mathf.Min(target, TSFEUtil.ClampedRemap(N2, idleN2, takeOffN2, idleN1, takeOffN1));
                float resp = target > N1 ? n1Response : n1DecreaseResponse;
                N1 = Mathf.MoveTowards(N1, target, resp * Mathf.Abs(target - N1) * dt);
            }
            else
            {
                N1 = Mathf.MoveTowards(N1, 0f, n1DecreaseResponse * N1 * dt);
            }

            // 温度更新
            float targetEGT = engineOn && !meltdown
                ? TSFEUtil.ClampedRemap(N1, idleN1, takeOffN1, idleEGT, takeOffEGT)
                : (fire ? fireEGT : 0f);
            EGT = Mathf.Lerp(EGT, targetEGT, egtResponse * dt);

            float targetECT = TSFEUtil.ClampedRemap(N1, idleN1, referenceN1, idleECT, continuousECT);
            if (fire) targetECT = fireECT;
            float ectResp = ECT > targetECT ? ectOverheatResponse : ectResponse;
            ECT = Mathf.Lerp(ECT, targetECT, ectResp * dt);

            // 逆噴射更新
            float revTarget = reversing ? 1f : 0f;
            float revResp = reversing ? reverserExtractResponse : reverserRetractResponse;
            reverserPosition = Mathf.MoveTowards(reverserPosition, revTarget, revResp * dt);

            // 推力計算
            float n1Norm = Mathf.Clamp01((N1 - idleN1) / (referenceN1 - idleN1));
            float thrust = maxThrust * Mathf.Pow(n1Norm, thrustCurve);
            if (reverserPosition > 0f) thrust *= -(reverserRatio * reverserPosition);

            // 推力適用
            if (SAVControl != null)
            {
                float t = (float)SAVControl.GetProgramVariable("ThrottleStrength");
                SAVControl.SetProgramVariable("ThrottleStrength", t - appliedThrust + thrust);
                appliedThrust = thrust;
            }
        }

        private void UpdateDamage(float dt)
        {
            if (meltdown) return;

            if (!fire)
            {
                if (ECT >= overheatECT)
                {
                    float damage = (ECT - overheatECT) / (fireECT - overheatECT);
                    if (TSFEUtil.CheckMTBFScaled(dt, mtbFireAtOverheat, damage))
                    {
                        fire = true;
                        if (fireSound) fireSound.Play();
                    }
                }
                else if (ECT >= continuousECT)
                {
                    if (TSFEUtil.CheckMTBF(dt, mtbFireAtContinuous))
                    {
                        fire = true;
                        if (fireSound) fireSound.Play();
                    }
                }
            }

            if (fire && TSFEUtil.CheckMTBF(dt, mtbMeltdownOnFire))
            {
                meltdown = true;
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
            }
        }

        private void UpdateSound()
        {
            float n1Norm = N1 / takeOffN1;
            float n2Norm = N2 / takeOffN2;

            if (idleSound)
            {
                idleSound.volume = idleVol * Mathf.Clamp01(n2Norm * 2f);
                idleSound.pitch = idlePit * (0.5f + n2Norm * 0.5f);
            }

            if (insideSound)
            {
                insideSound.volume = insideVol * n2Norm;
                insideSound.pitch = insidePit * (0.8f + n2Norm * 0.2f);
            }

            if (thrustSound)
            {
                thrustSound.volume = thrustVol * n1Norm;
                thrustSound.pitch = thrustPit * (0.7f + n1Norm * 0.3f);
            }

            if (takeoffSound)
            {
                takeoffSound.volume = takeoffVol * Mathf.Max(0f, (n1Norm - 0.8f) * 5f);
                takeoffSound.pitch = takeoffPit * (0.9f + n1Norm * 0.1f);
            }
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
            if (!engineOn) return;

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

        public bool EngineOn => engineOn;
    }
}
