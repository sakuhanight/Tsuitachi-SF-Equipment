using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;

namespace SFAdvEquipment.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_SeatsOnly : UdonSharpBehaviour
    {
        public SaccVehicleSeat[] seats = { };
        public bool excludeMode;
        public GameObject[] objects = { };

        [System.NonSerialized] public SaccEntity EntityControl;

        public void SFEXT_L_EntityStart() => SetActive(false);

        public void SFEXT_O_PilotEnter() => OnEnter();
        public void SFEXT_O_PilotExit() => OnExit();
        public void SFEXT_P_PassengerEnter() => OnEnter();
        public void SFEXT_P_PassengerExit() => OnExit();

        private void OnEnter()
        {
            var found = false;
            foreach (var seat in seats)
            {
                if (seat && EntityControl.MySeat == seat.ThisStationID)
                {
                    found = true;
                    break;
                }
            }

            SetActive(excludeMode ? !found : found);
        }

        private void OnExit()
        {
            SetActive(false);
        }

        private void SetActive(bool value)
        {
            gameObject.SetActive(value);
            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    if (obj) obj.SetActive(value);
                }
            }
        }
    }
}
