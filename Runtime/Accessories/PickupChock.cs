using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace TSFE.Accessories
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class PickupChock : UdonSharpBehaviour
    {
        public LayerMask groundLayerMask = 0x0801;
        public float raycastDistance = 3.0f;
        public float raycastOffset = 1.0f;
        public float sleepTimeout = 3.0f;

        private Collider[] colliders;
        private bool[] colliderTriggerFlags;
        private float wakeUpTime;
        private bool moving;

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Position))] private Vector3 _position;
        private Vector3 Position
        {
            set
            {
                SetMoving(true);
                transform.position = value;
                _position = value;
            }
            get => _position;
        }

        [UdonSynced(UdonSyncMode.Smooth)] private float _angle;
        private float Angle
        {
            set
            {
                SetMoving(true);
                transform.rotation = Quaternion.AngleAxis(value, Vector3.up);
                _angle = value;
            }
            get => _angle;
        }

        private void Start()
        {
            colliders = GetComponentsInChildren<Collider>(true);
            colliderTriggerFlags = new bool[colliders.Length];
            for (var i = 0; i < colliders.Length; i++)
            {
                colliderTriggerFlags[i] = colliders[i].isTrigger;
            }

            SetMoving(false);
        }

        private void Update()
        {
            if (moving && Time.time > wakeUpTime + sleepTimeout)
            {
                SetMoving(false);
            }
        }

        public override void OnPickup()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RemoteOnPickup));
        }

        public override void OnDrop()
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position + Vector3.up * raycastOffset, Vector3.down, out hitInfo, raycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                Position = hitInfo.point;
            }
            else
            {
                Position = transform.position;
            }
            Angle = Vector3.SignedAngle(Vector3.forward, transform.forward, Vector3.up);
        }

        public void RemoteOnPickup()
        {
            SetMoving(true);
        }

        private void SetMoving(bool value)
        {
            if (value) wakeUpTime = Time.time;
            SetTriggerFlags(value);
            moving = value;
        }

        private void SetTriggerFlags(bool value)
        {
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliderTriggerFlags[i]) continue;
                colliders[i].isTrigger = value;
            }
        }
    }
}
