using UdonSharp;
using UnityEngine;
using SFAdvEquipment.DFUNC;
using SFAdvEquipment.Utility;

namespace SFAdvEquipment.Avionics
{
    [RequireComponent(typeof(AudioSource))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(1100)]
    public class GPWS : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;

        [Header("Gear/Flaps References")]
        [Tooltip("SFV standard DFUNC_Gear or null")]
        public UdonSharpBehaviour gear;
        [Tooltip("SFV standard DFUNC_Flaps or null")]
        public UdonSharpBehaviour flaps;
        [Tooltip("SF-AdvEquipment DFUNC_AdvancedFlaps or null")]
        public DFUNC_AdvancedFlaps advancedFlaps;

        [Header("Detection")]
        public LayerMask groundLayers = -1;
        public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        public Transform groundDetector;
        public Transform offsetTransform;

        [Header("Audio")]
        public AudioClip[] altitudeCallouts = { };
        public float[] altitudeThresholds = { 5, 10, 20, 30, 40, 50, 100, 200, 400, 500, 1000, 2500 };
        public AudioClip bankAngleSound, sinkRateSound, pullUpSound, terrainSound, dontSinkSound, tooLowGearSound, tooLowFlapsSound, tooLowTerrainSound;

        [Header("Parameters")]
        public float initialClimbThreshold = 1333;
        public float smoothing = 1.0f;
        public float startDelay = 30;

        public bool PullUpWarning { private set; get; }

        private float lastAlertTime;
        private Rigidbody vehicleRigidbody;
        private AudioSource audioSource;
        private float maxRange, seaLevel;

        private int _calloutIndex = -1;
        private void SetCalloutIndex(int value)
        {
            if (value < 0) value = altitudeCallouts.Length;
            if (value == _calloutIndex) return;
            if (value < _calloutIndex) AltitudeCallout(value);
            _calloutIndex = value;
        }

        private float enabledTime;
        private void OnEnable()
        {
            enabledTime = Time.time;
            SetCalloutIndex(-1);
        }

        private float offset;
        private void Start()
        {
            vehicleRigidbody = GetComponentInParent<Rigidbody>();
            audioSource = GetComponent<AudioSource>();
            maxRange = altitudeThresholds[altitudeThresholds.Length - 1] * 2;
            seaLevel = SAVControl ? (float)SAVControl.GetProgramVariable("SeaLevel") : 0;
            offset = Vector3.Dot(groundDetector.up, offsetTransform.position - groundDetector.position);
        }

        private float radioAltitude, barometricAltitude, prevBarometricAltitude;
        private Vector3 velocity, prevPosition;
        private bool prevLandingConfiguration;
        private bool initialClimbing;
        private float peekBarometricAltitude;

        public override void PostLateUpdate()
        {
            var position = vehicleRigidbody.position;
            var deltaTime = Time.deltaTime;
            var smoothingT = deltaTime / smoothing;

            radioAltitude = Mathf.Lerp(radioAltitude, GetRadioAltitude(), smoothingT);
            barometricAltitude = Mathf.Lerp(barometricAltitude, SFAEUtil.ToFeet(groundDetector.position.y - seaLevel), smoothingT);
            var barometricDescendRate = -(barometricAltitude - prevBarometricAltitude) / deltaTime * 60;
            prevBarometricAltitude = barometricAltitude;

            var wind = SAVControl ? (Vector3)SAVControl.GetProgramVariable("Wind") : Vector3.zero;
            velocity = Vector3.Lerp(velocity, position - prevPosition - wind, smoothingT);
            prevPosition = position;

            var airspeed = SFAEUtil.ToKnots(velocity.magnitude);

            var flapsDown = flaps ? (bool)flaps.GetProgramVariable("Flaps") : false;
            var advancedFlapsDown = advancedFlaps ? advancedFlaps.targetAngle > 0 : false;
            var anyFlapsDown = !flaps && !advancedFlaps || flapsDown || advancedFlapsDown;
            var gearDown = gear ? !(bool)gear.GetProgramVariable("GearUp") : true;

            var landingConfiguration = gearDown && anyFlapsDown;
            if (landingConfiguration) initialClimbing = false;
            else if (prevLandingConfiguration)
            {
                initialClimbing = true;
                peekBarometricAltitude = barometricAltitude;
            }
            else if (initialClimbing)
            {
                if (radioAltitude > initialClimbThreshold) initialClimbing = false;
                else peekBarometricAltitude = Mathf.Max(peekBarometricAltitude, barometricAltitude);
            }

            var barometricAltitudeLoss = peekBarometricAltitude - barometricAltitude;
            prevLandingConfiguration = landingConfiguration;

            SetCalloutIndex(GetAltitudeCalloutIndex(radioAltitude));

            var state = Mode1(barometricDescendRate, radioAltitude);
            state = Mathf.Min(state, Mode2(barometricDescendRate, radioAltitude, airspeed, barometricDescendRate, landingConfiguration));
            state = Mathf.Min(state, Mode3(barometricAltitudeLoss, radioAltitude, initialClimbing, landingConfiguration));
            if (!initialClimbing) state = Mathf.Min(state, Mode4(airspeed, radioAltitude, gearDown, anyFlapsDown));
            state = Mathf.Min(state, Mode6(radioAltitude));

            if (state == ALERT_PULL_UP) Alert(pullUpSound, 2);
            else if (state == ALERT_TOO_LOW_GEAR) Alert(tooLowGearSound, 2);
            else if (state == ALERT_TOO_LOW_FLAPS) Alert(tooLowFlapsSound, 2);
            else if (state == ALERT_TERRAIN) Alert(terrainSound, 2);
            else if (state == ALERT_SINK_RATE) Alert(sinkRateSound, 2);
            else if (state == ALERT_TOO_LOW_TERRAIN) Alert(tooLowTerrainSound, 2);
            else if (state == ALERT_BANK_ANGLE) Alert(bankAngleSound, 2);
            else if (state == ALERT_DONT_SINK) Alert(dontSinkSound, 2);

            PullUpWarning = state == ALERT_PULL_UP || state == ALERT_TERRAIN || state == ALERT_TOO_LOW_TERRAIN
                || state == ALERT_TOO_LOW_GEAR || state == ALERT_TOO_LOW_FLAPS || state == ALERT_SINK_RATE || state == ALERT_DONT_SINK;
        }

        private float GetRadioAltitude()
        {
            var position = groundDetector.position;
            RaycastHit hit;
            if (Physics.Raycast(position, Vector3.down, out hit, SFAEUtil.FromFeet(maxRange), groundLayers, queryTriggerInteraction))
            {
                return SFAEUtil.ToFeet(hit.distance + offset);
            }
            else
            {
                return SFAEUtil.ToFeet(position.y - seaLevel + offset);
            }
        }

        private int GetAltitudeCalloutIndex(float altitude)
        {
            for (var i = 0; i < altitudeThresholds.Length; i++)
            {
                if (altitude < altitudeThresholds[i]) return i;
            }
            return -1;
        }

        #region Alert Constants
        private const int ALERT_PULL_UP = 2;
        private const int ALERT_TERRAIN = 8;
        private const int ALERT_TOO_LOW_TERRAIN = 11;
        private const int ALERT_ALTITUDE_CALLOUTS = 13;
        private const int ALERT_TOO_LOW_GEAR = 14;
        private const int ALERT_TOO_LOW_FLAPS = 15;
        private const int ALERT_SINK_RATE = 16;
        private const int ALERT_DONT_SINK = 17;
        private const int ALERT_BANK_ANGLE = 21;
        private const int ALERT_NONE = 255;
        #endregion

        #region Mode 1 - Excessive Descent Rate
        private const float Mode1LowerLimit = 30;
        private const float Mode1UpperLimit = 2450;
        private const float Mode1SinkRateSlope = (Mode1UpperLimit - Mode1LowerLimit) / (5000 - 998);
        private const float Mode1SinkRateIntercept = Mode1LowerLimit - 998 * Mode1SinkRateSlope;
        private const float Mode1PullUpSlope1 = (284 - Mode1LowerLimit) / (1710 - 1482);
        private const float Mode1PullUpIntercept1 = Mode1LowerLimit - 1482 * Mode1PullUpSlope1;
        private const float Mode1PullUpSlope2 = (Mode1UpperLimit - 284) / (7125 - 1710);
        private const float Mode1PullUpIntercept2 = 284 - 1710 * Mode1PullUpSlope2;

        private int Mode1(float barometricDescendRate, float radioAltitude)
        {
            if (radioAltitude < Mode1LowerLimit || radioAltitude > Mode1UpperLimit) return ALERT_NONE;

            var pullUpRadioAltitude = Mathf.Min(
                barometricDescendRate * Mode1PullUpSlope1 + Mode1PullUpIntercept1,
                barometricDescendRate * Mode1PullUpSlope2 + Mode1PullUpIntercept2);
            if (radioAltitude < pullUpRadioAltitude) return ALERT_PULL_UP;

            var sinkRateRadioAltitude = barometricDescendRate * Mode1SinkRateSlope + Mode1SinkRateIntercept;
            if (radioAltitude < sinkRateRadioAltitude) return ALERT_SINK_RATE;

            return ALERT_NONE;
        }
        #endregion

        #region Mode 2 - Excessive Terrain Closure Rate
        private const float Mode2ALowerLimit = 30;
        private const float Mode2AUpperLimit1 = 1650;
        private const float Mode2AUpperLimit2 = 2450;
        private const float Mode2BLowerLimit1 = 30;
        private const float Mode2BLowerLimit2 = 200;
        private const float Mode2BUpperLimit1 = 600;
        private const float Mode2BUpperLimit2 = 789;
        private const float Mode2TerrainSlope1 = (1219 - Mode2ALowerLimit) / (3436 - 2000);
        private const float Mode2TerrainIntercept1 = Mode2ALowerLimit - 2000 * Mode2TerrainSlope1;
        private const float Mode2TerrainSlope2 = (Mode2AUpperLimit2 - 1219) / (6876 - 3436);
        private const float Mode2TerrainIntercept2 = 1219 - 3436 * Mode2TerrainSlope2;
        private const float Mode2PullUpSlope1 = (1219 - Mode2ALowerLimit) / (3684 - 2243);
        private const float Mode2PullUpIntercept1 = Mode2ALowerLimit - 2243 * Mode2PullUpSlope1;
        private const float Mode2PullUpSlope2 = (Mode2AUpperLimit2 - 1219) / (7125 - 3684);
        private const float Mode2PullUpIntercept2 = 1219 - 3684 * Mode2PullUpSlope2;

        private int Mode2A(float radioClosureRate, float radioAltitude, float airspeed)
        {
            var upperLimit = airspeed < 220 ? Mode2AUpperLimit1 : Mode2AUpperLimit2;
            if (radioAltitude < Mode2ALowerLimit || radioAltitude > upperLimit) return ALERT_NONE;

            var pullUpRadioAltitude = Mathf.Min(
                radioClosureRate * Mode2PullUpSlope1 + Mode2PullUpIntercept1,
                radioClosureRate * Mode2PullUpSlope2 + Mode2PullUpIntercept2);
            if (radioAltitude < pullUpRadioAltitude) return ALERT_PULL_UP;

            var terrainRadioAltitude = Mathf.Min(
                radioClosureRate * Mode2TerrainSlope1 + Mode2TerrainIntercept1,
                radioClosureRate * Mode2TerrainSlope2 + Mode2TerrainIntercept2);
            if (radioAltitude < terrainRadioAltitude) return ALERT_TERRAIN;

            return ALERT_NONE;
        }

        private int Mode2B(float radioClosureRate, float radioAltitude, float barometricDescendRate)
        {
            var lowerLimit = barometricDescendRate <= 400 ? Mode2BLowerLimit2 : Mode2BLowerLimit1;
            var upperLimit = barometricDescendRate >= 1000 ? Mode2BUpperLimit1 : Mode2BUpperLimit2;
            if (radioAltitude < lowerLimit || radioAltitude > upperLimit) return ALERT_NONE;

            var pullUpRadioAltitude = Mathf.Min(
                radioClosureRate * Mode2PullUpSlope1 + Mode2PullUpIntercept1,
                radioClosureRate * Mode2PullUpSlope2 + Mode2PullUpIntercept2);
            if (radioAltitude < pullUpRadioAltitude) return ALERT_PULL_UP;

            var terrainRadioAltitude = Mathf.Min(
                radioClosureRate * Mode2TerrainSlope1 + Mode2TerrainIntercept1,
                radioClosureRate * Mode2TerrainSlope2 + Mode2TerrainIntercept2);
            if (radioAltitude < terrainRadioAltitude) return ALERT_TERRAIN;

            return ALERT_NONE;
        }

        private int Mode2(float radioClosureRate, float radioAltitude, float airspeed, float barometricDescendRate, bool landingConfiguration)
        {
            return landingConfiguration
                ? Mode2B(radioClosureRate, radioAltitude, barometricDescendRate)
                : Mode2A(radioClosureRate, radioAltitude, airspeed);
        }
        #endregion

        #region Mode 3 - Altitude Loss After Takeoff
        private const float Mode3LowerLimit = 30;
        private const float Mode3UpperLimit = 1333;
        private const float Mode3Slope = (Mode3UpperLimit - Mode3LowerLimit) / (128 - 10);
        private const float Mode3Intercept = Mode3UpperLimit - Mode3Slope * 128;

        private int Mode3(float barometricAltitudeLoss, float radioAltitude, bool isInitialClimbing, bool landingConfiguration)
        {
            if (landingConfiguration || !isInitialClimbing || radioAltitude < Mode3LowerLimit || radioAltitude > Mode3UpperLimit) return ALERT_NONE;

            var dontSinkRadioAltitude = barometricAltitudeLoss * Mode3Slope + Mode3Intercept;
            if (radioAltitude < dontSinkRadioAltitude) return ALERT_DONT_SINK;

            return ALERT_NONE;
        }
        #endregion

        #region Mode 4 - Unsafe Terrain Clearance
        private const float Mode4ALowerLimit = 30;
        private const float Mode4AUpperLimit = 1000;
        private const float Mode4AGearUpperLimit = 500;
        private const float Mode4ATerrainSlope = (1000f - 500f) / (250f - 190f);
        private const float Mode4ATerrainIntercept = 500 - 190 * Mode4ATerrainSlope;

        private int Mode4A(float airspeed, float radioAltitude, bool gearDown)
        {
            if (gearDown || radioAltitude < Mode4ALowerLimit || radioAltitude > Mode4AUpperLimit) return ALERT_NONE;

            if (airspeed < 190 && radioAltitude < Mode4AGearUpperLimit) return ALERT_TOO_LOW_GEAR;

            var tooLowTerrainRadioAltitude = airspeed * Mode4ATerrainSlope + Mode4ATerrainIntercept;
            if (radioAltitude < tooLowTerrainRadioAltitude) return ALERT_TOO_LOW_TERRAIN;

            return ALERT_NONE;
        }

        private const float Mode4BLowerLimit = 30;
        private const float Mode4BUpperLimit = 1000;
        private const float Mode4BFlapsUpperLimit = 245;
        private const float Mode4BTerrainSlope = (1000f - 245f) / (250f - 159f);
        private const float Mode4BTerrainIntercept = 245 - 159 * Mode4BTerrainSlope;

        private int Mode4B(float airspeed, float radioAltitude, bool flapsDown)
        {
            if (flapsDown || radioAltitude < Mode4BLowerLimit || radioAltitude > Mode4BUpperLimit) return ALERT_NONE;

            if (airspeed < 159 && radioAltitude < Mode4BFlapsUpperLimit) return ALERT_TOO_LOW_FLAPS;

            var tooLowTerrainRadioAltitude = airspeed * Mode4BTerrainSlope + Mode4BTerrainIntercept;
            if (radioAltitude < tooLowTerrainRadioAltitude) return ALERT_TOO_LOW_TERRAIN;

            return ALERT_NONE;
        }

        private int Mode4(float airspeed, float radioAltitude, bool gearDown, bool flapsDown)
        {
            if (!gearDown) return Mode4A(airspeed, radioAltitude, gearDown);
            if (!flapsDown) return Mode4B(airspeed, radioAltitude, flapsDown);
            return ALERT_NONE;
        }
        #endregion

        #region Mode 6 - Advisory Callouts
        private int altitudeCalloutIndex = -1;
        private int Mode6AltitudeCallout(float altitude)
        {
            var nextIndex = GetAltitudeCalloutIndex(altitude);
            var callout = nextIndex >= 0 && (altitudeCalloutIndex < 0 || nextIndex < altitudeCalloutIndex);
            altitudeCalloutIndex = nextIndex;
            if (callout) return ALERT_ALTITUDE_CALLOUTS;
            return ALERT_NONE;
        }

        private int Mode6BankAngle(float altitude)
        {
            if (altitude < 30 || altitude > 2500) return ALERT_NONE;

            var right = transform.right;
            var angle = Mathf.Abs(Mathf.Atan2(right.y, Vector3.ProjectOnPlane(right, Vector3.up).magnitude) * Mathf.Rad2Deg);
            var limit = Mathf.Lerp(10, 40, (altitude - 30) / 120);
            return angle >= limit ? ALERT_BANK_ANGLE : ALERT_NONE;
        }

        private int Mode6(float altitude)
        {
            var altitudeCallout = Mode6AltitudeCallout(altitude);
            if (altitudeCallout != ALERT_NONE) return altitudeCallout;
            return Mode6BankAngle(altitude);
        }
        #endregion

        #region Audio
        private void AltitudeCallout(int index)
        {
            if (index < 0 || index >= altitudeCallouts.Length) return;
            PlayOneShot(altitudeCallouts[index]);
        }

        private void Alert(AudioClip clip, float interval)
        {
            var time = Time.time;
            if (time < lastAlertTime + interval) return;
            PlayOneShot(clip);
            lastAlertTime = time;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource == null || clip == null || Time.time < enabledTime + startDelay) return;
            audioSource.PlayOneShot(clip);
        }
        #endregion
    }
}
