using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SFAdvEquipment.Utility
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFAEUtil : UdonSharpBehaviour
    {
        // ============================================================
        // Unit Conversion Constants
        // ============================================================
        public const float MS_TO_KNOTS = 1.94384f;
        public const float KNOTS_TO_MS = 0.514444f;
        public const float METERS_TO_FEET = 3.28084f;
        public const float FEET_TO_METERS = 0.3048f;
        public const float FPM_TO_MS = 0.00508f;

        // ============================================================
        // Math Utilities
        // ============================================================

        public static float Remap01(float value, float oldMin, float oldMax)
        {
            return (value - oldMin) / (oldMax - oldMin);
        }

        public static float ClampedRemap01(float value, float oldMin, float oldMax)
        {
            return Mathf.Clamp01(Remap01(value, oldMin, oldMax));
        }

        public static float ClampedRemap(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            return ClampedRemap01(value, oldMin, oldMax) * (newMax - newMin) + newMin;
        }

        public static float Lerp3(float a, float b, float c, float t, float tMin, float tMid, float tMax)
        {
            return Mathf.Lerp(a,
                Mathf.Lerp(b, c, Remap01(t, tMid, tMax)),
                Remap01(t, tMin, tMid));
        }

        public static float Lerp4(float a, float b, float c, float d, float t, float tMin, float tMid1, float tMid2, float tMax)
        {
            return Mathf.Lerp(a,
                Mathf.Lerp(b, Mathf.Lerp(c, d, Remap01(t, tMid2, tMax)), Remap01(t, tMid1, tMid2)),
                Remap01(t, tMin, tMid1));
        }

        // ============================================================
        // MTBF (Mean Time Between Failures)
        // ============================================================

        public static bool CheckMTBF(float deltaTime, float mtbf)
        {
            return Random.value < deltaTime / mtbf;
        }

        public static bool CheckMTBFScaled(float deltaTime, float mtbf, float damageMultiplier)
        {
            return Random.value < damageMultiplier * deltaTime / mtbf;
        }

        // ============================================================
        // DFUNC Helpers
        // ============================================================

        public static float GetTriggerInput(bool leftDial)
        {
            return Input.GetAxisRaw(leftDial
                ? "Oculus_CrossPlatform_PrimaryIndexTrigger"
                : "Oculus_CrossPlatform_SecondaryIndexTrigger");
        }

        public static bool IsTriggerPressed(bool leftDial)
        {
            return GetTriggerInput(leftDial) > 0.75f;
        }

        public static void SetDialFuncon(GameObject dialFuncon, GameObject[] dialFunconArray, bool active)
        {
            if (dialFuncon) dialFuncon.SetActive(active);
            if (dialFunconArray != null)
            {
                for (int i = 0; i < dialFunconArray.Length; i++)
                {
                    if (dialFunconArray[i]) dialFunconArray[i].SetActive(active);
                }
            }
        }

        public static void PlayHaptics(bool leftDial, float duration, float amplitude, float frequency)
        {
            Networking.LocalPlayer.PlayHapticEventInHand(
                leftDial ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right,
                duration, amplitude, frequency);
        }

        // ============================================================
        // Unit Conversion Helpers
        // ============================================================

        public static float ToKnots(float ms) { return ms * MS_TO_KNOTS; }
        public static float FromKnots(float knots) { return knots * KNOTS_TO_MS; }
        public static float ToFeet(float meters) { return meters * METERS_TO_FEET; }
        public static float FromFeet(float feet) { return feet * FEET_TO_METERS; }
    }
}
