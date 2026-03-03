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

        [Header("N1 RPM")]
        public float idleN1 = 879.6f;
        public float referenceN1 = 4397f;
        public float takeOffN1 = 4586f;

        [Header("N2 RPM")]
        public float idleN2 = 8583.5f;
        public float referenceN2 = 17167f;
        public float takeOffN2 = 20171f;

        public UdonSharpBehaviour SAVControl;
        [System.NonSerialized] public SaccEntity EntityControl;

        [UdonSynced(UdonSyncMode.None)] public bool reversing = false;
        [UdonSynced(UdonSyncMode.None)] public bool starter = false;
        [UdonSynced(UdonSyncMode.None)] public bool fuel = false;
        [UdonSynced(UdonSyncMode.Smooth)] public float N1 = 0f;
        [UdonSynced(UdonSyncMode.Smooth)] public float N2 = 0f;

        public void SFEXT_L_EntityStart() { }
    }
}
