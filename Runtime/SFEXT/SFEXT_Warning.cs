using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;

namespace TSFE.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_Warning : UdonSharpBehaviour
    {
        [Header("Master Caution")]
        public GameObject[] masterCautionLights = { };
        public GameObject[] engineCautionLights = { };
        public GameObject[] hydroCautionLights = { };
        public GameObject[] fuelCautionLights = { };
        public GameObject[] engineOverheatLights = { };
        public GameObject[] apuCautionLight = { };

        [Header("Fire")]
        public GameObject[] engine1OverheatLights = { };
        public GameObject[] engine2OverheatLights = { };
        public GameObject[] engineFireLights = { };
        public GameObject[] engine1FireLights = { };
        public GameObject[] engine2FireLights = { };
        public AudioSource engineFireAlarm;

        public UdonSharpBehaviour SAVControl;
        public UdonSharpBehaviour engine1;
        public UdonSharpBehaviour engine2;
        public SFEXT_AuxiliaryPowerUnit apu;

        [System.NonSerialized] public SaccEntity EntityControl;

        private bool initialized;

        public void SFEXT_L_EntityStart()
        {
            gameObject.SetActive(false);
            initialized = true;
        }

        public void SFEXT_G_PilotEnter() { gameObject.SetActive(true); }
        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
            StopAlarm(engineFireAlarm);
        }

        private void Update()
        {
            if (!initialized) return;

            bool engine1Overheat = false, engine2Overheat = false;
            bool engine1Fire = false, engine2Fire = false;
            bool engine1Stall = false, engine2Stall = false;
            bool hydro1Low = false, hydro2Low = false;

            if (engine1)
            {
                engine1Overheat = (bool)engine1.GetProgramVariable("overheat");
                UpdateWarning(engine1Overheat, engine1OverheatLights, null);

                var ect = (float)engine1.GetProgramVariable("ect");
                var fireECT = (float)engine1.GetProgramVariable("fireECT");
                engine1Fire = ect > fireECT;
                UpdateWarning(engine1Fire, engine1FireLights, null);

                var n1 = (float)engine1.GetProgramVariable("n1");
                var idleN1 = (float)engine1.GetProgramVariable("idleN1");
                engine1Stall = n1 < idleN1 * 0.9f;
                hydro1Low = n1 < idleN1 * 0.8f;
            }

            if (engine2)
            {
                engine2Overheat = (bool)engine2.GetProgramVariable("overheat");
                UpdateWarning(engine2Overheat, engine2OverheatLights, null);

                var ect = (float)engine2.GetProgramVariable("ect");
                var fireECT = (float)engine2.GetProgramVariable("fireECT");
                engine2Fire = ect > fireECT;
                UpdateWarning(engine2Fire, engine2FireLights, null);

                var n1 = (float)engine2.GetProgramVariable("n1");
                var idleN1 = (float)engine2.GetProgramVariable("idleN1");
                engine2Stall = n1 < idleN1 * 0.9f;
                hydro2Low = n1 < idleN1 * 0.8f;
            }

            var engineCaution = engine1Stall || engine2Stall;
            UpdateWarning(engineCaution, engineCautionLights, null);

            var hydro = hydro1Low || hydro2Low;
            UpdateWarning(hydro, hydroCautionLights, null);

            var overheat = engine1Overheat || engine2Overheat;
            UpdateWarning(overheat, engineOverheatLights, null);

            var fire = engine1Fire || engine2Fire;
            UpdateWarning(fire, engineFireLights, engineFireAlarm);

            var fuelLow = false;
            if (SAVControl)
            {
                var fuel = (float)SAVControl.GetProgramVariable("Fuel");
                var fullFuel = (float)SAVControl.GetProgramVariable("FullFuel");
                fuelLow = fullFuel > 0 && fuel / fullFuel < 0.3f;
            }
            UpdateWarning(fuelLow, fuelCautionLights, null);

            var apuOperating = apu && !apu.terminated;
            UpdateWarning(apuOperating, apuCautionLight, null);

            UpdateWarning(engineCaution || hydro || overheat || fire || fuelLow || apuOperating, masterCautionLights, null);
        }

        private void UpdateWarning(bool state, GameObject[] lights, AudioSource alarm)
        {
            if (lights != null)
            {
                foreach (var light in lights)
                {
                    if (light && light.activeSelf != state) light.SetActive(state);
                }
            }

            if (alarm != null && alarm.isPlaying != state)
            {
                if (state) alarm.Play();
                else alarm.Stop();
            }
        }

        private void StopAlarm(AudioSource alarm)
        {
            if (alarm == null) return;
            alarm.Stop();
        }
    }
}
