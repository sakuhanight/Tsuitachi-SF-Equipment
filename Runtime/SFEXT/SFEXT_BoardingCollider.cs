using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;

namespace TSFE.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_BoardingCollider : UdonSharpBehaviour
    {
        public bool enableOnWater = true;

        [System.NonSerialized] public SaccEntity EntityControl;

        private Quaternion localRotation;
        private Transform entityTransform;
        private Vector3 localPosition;
        private bool onBoarding;
        private bool onGround;
        private Vector3 prevPosition;
        private Quaternion prevRotation;
        private int playerEnterCount;

        public void SFEXT_L_EntityStart()
        {
            entityTransform = EntityControl.transform;
            localPosition = entityTransform.InverseTransformPoint(transform.position);
            localRotation = Quaternion.Inverse(entityTransform.rotation) * transform.rotation;

            onBoarding = false;
            onGround = true;
            playerEnterCount = 0;
            CheckState();

            SendCustomEventDelayedSeconds(nameof(_LateStart), 1.0f);
        }

        public void _LateStart()
        {
            gameObject.name = $"{entityTransform.gameObject.name}_{gameObject.name}";
            transform.SetParent(entityTransform.parent, true);
        }

        public void SFEXT_O_PilotEnter() => SetOnBoarding(true);
        public void SFEXT_O_PilotExit() => SetOnBoarding(false);
        public void SFEXT_P_PassengerEnter() => SetOnBoarding(true);
        public void SFEXT_P_PassengerExit() => SetOnBoarding(false);

        public void SFEXT_G_TakeOff() => SetOnGround(false);
        public void SFEXT_G_TouchDown() => SetOnGround(true);
        public void SFEXT_G_TouchDownWater() => SetOnGround(enableOnWater);
        public void SFEXT_G_ReAppear() { SetPlayerEnterCount(0); }

        public override void PostLateUpdate()
        {
            if (!entityTransform) return;

            var position = entityTransform.TransformPoint(localPosition);
            var rotation = entityTransform.rotation * localRotation;
            transform.position = position;
            transform.rotation = rotation;

            if (playerEnterCount > 0)
            {
                var localPlayer = Networking.LocalPlayer;
                var playerPosition = localPlayer.GetPosition();
                var playerRotation = localPlayer.GetRotation();

                var positionDiff = position - prevPosition;
                var rotationDiff = Quaternion.Inverse(prevRotation) * rotation;

                var nextPlayerPosition = rotationDiff * (playerPosition - position) + position + positionDiff;
                if (!Mathf.Approximately(Vector3.Distance(nextPlayerPosition, playerPosition), 0.0f))
                {
                    localPlayer.TeleportTo(nextPlayerPosition, playerRotation * Quaternion.Slerp(rotationDiff, Quaternion.identity, 0.5f));
                }
            }

            prevPosition = position;
            prevRotation = rotation;
        }

        private void SetOnBoarding(bool value)
        {
            if (value) playerEnterCount = 0;
            onBoarding = value;
            CheckState();
        }

        private void SetOnGround(bool value)
        {
            onGround = value;
            CheckState();
        }

        private void SetPlayerEnterCount(int value)
        {
            var prevStay = playerEnterCount > 0;
            var nextStay = value > 0;
            playerEnterCount = Mathf.Max(value, 0);

            if (prevStay != nextStay)
            {
                EntityControl.SendEventToExtensions(nextStay ? "SFEXT_L_BoardingEnter" : "SFEXT_L_BoardingExit");
            }
            CheckState();
        }

        private void CheckState()
        {
            var active = !onBoarding && onGround || playerEnterCount > 0;
            if (active != gameObject.activeSelf)
            {
                gameObject.SetActive(active);
                if (!active) playerEnterCount = 0;
            }
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal) SetPlayerEnterCount(playerEnterCount + 1);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player.isLocal) SetPlayerEnterCount(playerEnterCount - 1);
        }
    }
}
