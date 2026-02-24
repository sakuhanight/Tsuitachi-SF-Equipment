using System;
using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;

namespace SFAdvEquipment.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_PassengerOnly : UdonSharpBehaviour
    {
        public bool moveToSeat = true;
        public SaccVehicleSeat[] excludes = { };

        [NonSerialized] public SaccEntity EntityControl;

        public void SFEXT_L_EntityStart()
        {
            gameObject.SetActive(false);
        }

        public void SFEXT_P_PassengerEnter()
        {
            var mySeat = EntityControl.MySeat;
            if (mySeat < 0) return;

            var station = EntityControl.VehicleStations[mySeat];
            if (!station) return;

            var seat = station.GetComponent<SaccVehicleSeat>();
            if (seat.IsPilotSeat || excludes != null && Array.IndexOf(excludes, seat) >= 0) return;

            if (moveToSeat) transform.SetPositionAndRotation(seat.transform.position, seat.transform.rotation);

            gameObject.SetActive(true);
        }

        public void SFEXT_P_PassengerExit()
        {
            gameObject.SetActive(false);
        }
    }
}
