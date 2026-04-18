using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;

namespace TSFE.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_EngineFanDriver : UdonSharpBehaviour
    {
        public UdonSharpBehaviour[] engines;
        public Transform[] fanTransforms;
        public Vector3[] fanAxises = { Vector3.up };

        [System.NonSerialized] public SaccEntity EntityControl;

        private float[] fanAngles;
        private Vector3[] fanParentAxises;
        private Quaternion[] fanInitialRotations;
        private bool hasPilot;

        public void SFEXT_L_EntityStart()
        {
            var count = engines.Length;
            fanAngles = new float[count];
            fanParentAxises = new Vector3[count];
            fanInitialRotations = new Quaternion[count];

            for (var i = 0; i < count; i++)
            {
                var fan = fanTransforms[i];
                var axis = i < fanAxises.Length ? fanAxises[i] : Vector3.up;
                fanAngles[i] = 0;
                fanInitialRotations[i] = fan.localRotation;
                fanParentAxises[i] = fan.localRotation * axis;
            }

            gameObject.SetActive(false);
        }

        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit() { hasPilot = false; }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            var stopped = true;

            for (var i = 0; i < engines.Length; i++)
            {
                var engine = engines[i];
                var fan = fanTransforms[i];
                var n1 = (float)engine.GetProgramVariable("n1");

                var fanAngle = fanAngles[i];
                fanAngle += n1 * deltaTime * 360;
                fanAngles[i] = fanAngle % 360;
                fan.localRotation = Quaternion.AngleAxis(fanAngle, fanParentAxises[i]) * fanInitialRotations[i];

                if (n1 > 0) stopped = false;
            }

            if (!hasPilot && stopped) gameObject.SetActive(false);
        }
    }
}
