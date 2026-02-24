using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;
using TSFE.Utility;

namespace TSFE.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_WakeTurbulence : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public float minSpeed = 60;
        public float peakSpeed = 120;
        public float maxSpeed = 300;
        public float curve = 2.0f;

        [System.NonSerialized] public SaccEntity EntityControl;

        private Rigidbody vehicleRigidbody;
        private ParticleSystem[] particles;
        private float[] emissionRates;
        private bool hasPilot;
        private Vector3 prevPosition;

        public void SFEXT_L_EntityStart()
        {
            vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");

            particles = GetComponentsInChildren<ParticleSystem>(true);
            emissionRates = new float[particles.Length];
            for (var i = 0; i < particles.Length; i++)
            {
                emissionRates[i] = particles[i].emission.rateOverTimeMultiplier;
            }

            gameObject.SetActive(false);
        }

        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            prevPosition = vehicleRigidbody.position;
            gameObject.SetActive(true);
        }

        public void SFEXT_G_PilotExit() => hasPilot = false;

        private void Update()
        {
            var position = vehicleRigidbody.position;
            var velocity = (position - prevPosition) / Time.deltaTime;
            prevPosition = position;

            var wind = (Vector3)SAVControl.GetProgramVariable("Wind");
            var airspeed = Vector3.Distance(velocity, wind) * TSFEUtil.MS_TO_KNOTS;
            var strength = Mathf.Pow(TSFEUtil.Lerp3(0, 1, 0, airspeed, minSpeed, peakSpeed, maxSpeed), curve);

            var enabled = !Mathf.Approximately(strength, 0);
            for (var i = 0; i < particles.Length; i++)
            {
                var emission = particles[i].emission;
                if (enabled) emission.rateOverTimeMultiplier = emissionRates[i] * strength;
                if (enabled != emission.enabled) emission.enabled = enabled;
            }

            if (!hasPilot && !enabled) gameObject.SetActive(false);
        }
    }
}
