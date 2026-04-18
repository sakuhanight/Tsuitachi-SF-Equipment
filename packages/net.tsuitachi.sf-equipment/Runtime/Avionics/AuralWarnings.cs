using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;
using TSFE.DFUNC;
using TSFE.Utility;

namespace TSFE.Avionics
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AuralWarnings : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public DFUNC_AdvancedFlaps advancedFlaps;

        [Tooltip("KIAS")] public float defaultVmo = 340;
        [Tooltip("Degree")] public float stickShakerStartAoA = 10;
        [Tooltip("Degree")] public float stickShakerMaxAoA = 24;
        public AudioSource overspeed;
        public AudioSource stickShaker;
        public int updateInterval = 30;
        public float velocitySmooth = 1;

        [System.NonSerialized] public SaccEntity EntityControl;

        private Transform origin;
        private int updateOffset;
        private float prevTime;
        private Vector3 prevPosition, velocity;

        private void OnEnable()
        {
            updateOffset = Random.Range(0, updateInterval);
            prevTime = Time.time;
            prevPosition = transform.position;
        }

        public void SFEXT_L_EntityStart()
        {
            var vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
            origin = vehicleRigidbody.transform;
        }

        private void Update()
        {
            if ((Time.frameCount + updateOffset) % updateInterval != 0) return;

            var time = Time.time;
            var deltaTime = time - prevTime;
            prevTime = time;

            var position = origin.position;
            velocity = Vector3.Lerp(velocity, (position - prevPosition) / deltaTime, deltaTime / velocitySmooth);
            prevPosition = position;

            var wind = SAVControl ? (Vector3)SAVControl.GetProgramVariable("Wind") : Vector3.zero;
            var airVel = velocity - wind;

            var ias = Mathf.Max(Vector3.Dot(origin.forward, airVel), 0) * TSFEUtil.MS_TO_KNOTS;

            var vmo = defaultVmo;
            if (advancedFlaps) vmo = Mathf.Min(advancedFlaps.targetSpeedLimit, advancedFlaps.speedLimit);
            var playOverspeed = ias > 1.0f && ias > vmo;
            SetVolume(overspeed, playOverspeed ? 1.0f : 0.0f);

            var aoa = ias > 10f ? Mathf.Atan2(Vector3.Dot(origin.up, airVel), Vector3.Dot(origin.forward, airVel)) * Mathf.Rad2Deg : 0.0f;
            var stickShakerVolume = Mathf.Pow(TSFEUtil.ClampedRemap01(-aoa, stickShakerStartAoA, stickShakerMaxAoA), 0.1f);
            SetVolume(stickShaker, stickShakerVolume);
        }

        private void SetVolume(AudioSource audioSource, float volume)
        {
            if (!audioSource) return;
            var play = !Mathf.Approximately(volume, 0);
            if (play) audioSource.volume = volume;
            if (audioSource.isPlaying != play)
            {
                if (play) audioSource.Play();
                else audioSource.Stop();
            }
        }
    }
}
