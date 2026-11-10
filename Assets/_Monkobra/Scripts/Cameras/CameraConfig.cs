using System;
using UnityEngine;

/// <summary>
/// The one tuning asset of the camera system: how the anchor eases the view,
/// how the climb view frames the monkey, when the event shots (coconut,
/// cobra alert, pineapple leap) cut in and for how long, the shake, the
/// low-stamina vignette, and the comfort switches. Cinemachine owns blend
/// curves (<c>CinemachineBlenderSettings</c>), impulse shapes (the two
/// <c>CinemachineImpulseSource</c> components) and the noise profile, so
/// those stay in their own assets and this one scales them.
/// </summary>
[CreateAssetMenu(
    fileName = "CameraConfig",
    menuName = "Scriptable Objects/CameraConfig"
)]
public class CameraConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <summary>
    /// how one event shot behaves once asked for
    /// </summary>
    [Serializable]
    public class ShotSettings {
        /// <returns>how long the shot stays live; s</returns>
        public float DurationS => durationS;

        /// <returns>shortest time it stays live once cut to, even if its
        /// reason ends sooner; s</returns>
        public float MinHoldS => minHoldS;

        /// <returns>wait after it ends before it may start again; s
        /// </returns>
        public float CooldownS => cooldownS;

        /// <returns>wins over a live shot of a lower priority</returns>
        public int Priority => priority;

        public ShotSettings(
            float durationS,
            float minHoldS,
            float cooldownS,
            int priority
        ) {
            this.durationS = durationS;
            this.minHoldS = minHoldS;
            this.cooldownS = cooldownS;
            this.priority = priority;
        }

        [SerializeField]
        [Tooltip("how long the shot stays live; s")]
        private float durationS;

        [SerializeField]
        [Tooltip("shortest time live once cut to; s")]
        private float minHoldS;

        [SerializeField]
        [Tooltip("wait after it ends before it may start again; s")]
        private float cooldownS;

        [SerializeField]
        [Tooltip("wins over a live shot of a lower priority")]
        private int priority;
    }

    // anchor  -----------------------------------------------------------------
    /// <returns>time the view takes to catch up with the orbit angle; s
    /// </returns>
    public float AnchorYawSmoothTimeS => anchorYawSmoothTimeS;

    /// <returns>orbit angle the monkey may lead by before the view turns; deg
    /// </returns>
    public float AnchorYawDeadZoneDeg => anchorYawDeadZoneDeg;

    /// <returns>fastest the view may turn; deg/s</returns>
    public float AnchorMaxYawSpeedDeg => anchorMaxYawSpeedDeg;

    // climb framing  ----------------------------------------------------------
    /// <returns>aim height above the monkey at rest, the permanent upward
    /// bias; u</returns>
    public float RestAimOffsetU => restAimOffsetU;

    /// <returns>extra aim height at full climb speed; u</returns>
    public float ClimbLeadU => climbLeadU;

    /// <returns>extra aim offset at full descent speed, negative is below; u
    /// </returns>
    public float DescendLeadU => descendLeadU;

    /// <returns>vertical speed that counts as full lead; u/s</returns>
    public float FullLeadSpeedU => fullLeadSpeedU;

    /// <returns>vertical speed below which the lead stays at rest; u/s
    /// </returns>
    public float LeadSpeedDeadZoneU => leadSpeedDeadZoneU;

    /// <returns>time the lead takes to follow the speed; s</returns>
    public float LeadSmoothTimeS => leadSmoothTimeS;

    /// <returns>sideways aim shift toward the fruit while grab is held; u
    /// </returns>
    public float GrabShiftU => grabShiftU;

    /// <returns>time the grab shift takes to ease in and out; s</returns>
    public float GrabShiftSmoothTimeS => grabShiftSmoothTimeS;

    // shots  ------------------------------------------------------------------
    /// <returns>follows a thrown coconut down</returns>
    public ShotSettings CoconutShot => coconutShot;

    /// <returns>extra time the coconut shot stays after the landing; s
    /// </returns>
    public float CoconutLandHoldS => coconutLandHoldS;

    /// <returns>looks down at the cobra after a cobra event</returns>
    public ShotSettings CobraAlertShot => cobraAlertShot;

    /// <returns>cobra head distance below the monkey that raises an alert,
    /// 0 turns it off; u</returns>
    public float CobraProximityU => cobraProximityU;

    /// <returns>looks up and pulls back while a pineapple leap runs</returns>
    public ShotSettings LeapShot => leapShot;

    /// <returns>extra aim height during a leap; u</returns>
    public float LeapAimOffsetU => leapAimOffsetU;

    /// <returns>scale on the follow distance during a leap, above 1 pulls
    /// back</returns>
    public float LeapDistanceScale => leapDistanceScale;

    // shake  ------------------------------------------------------------------
    /// <returns>scale on every shake, 0 silences all of it</returns>
    public float ShakeScale => shakeScale;

    /// <returns>noise amplitude gain while moving on rough bark</returns>
    public float RoughBarkNoiseGain => roughBarkNoiseGain;

    /// <returns>noise frequency gain while moving on rough bark</returns>
    public float RoughBarkNoiseFrequency => roughBarkNoiseFrequency;

    /// <returns>time the bark shake takes to rise; s</returns>
    public float RoughBarkEaseInS => roughBarkEaseInS;

    /// <returns>time the bark shake takes to fade; s</returns>
    public float RoughBarkEaseOutS => roughBarkEaseOutS;

    /// <returns>scale on the light hit impulse</returns>
    public float LightHitScale => lightHitScale;

    /// <returns>scale on the heavy hit impulse</returns>
    public float HeavyHitScale => heavyHitScale;

    // stamina pulse  ----------------------------------------------------------
    /// <returns>vignette intensity at the peak for a move attempt below the
    /// low-stamina fraction; 0 none, 1 full black edge</returns>
    public float LowVignetteIntensity => lowVignetteIntensity;

    /// <returns>vignette intensity at the peak for a move attempt on an empty
    /// bar</returns>
    public float EmptyVignetteIntensity => emptyVignetteIntensity;

    /// <returns>ease from rest to peak; s</returns>
    public float EaseInS => easeInS;

    /// <returns>ease from peak back to rest; s</returns>
    public float EaseOutS => easeOutS;

    /// <returns>wait after a pulse ends before the next may start; s</returns>
    public float RetriggerCooldownS => retriggerCooldownS;

    // comfort  ----------------------------------------------------------------
    /// <returns>if the camera should move as little as it can: no shake, no
    /// continuous noise, no look-ahead lead, no extra pull-back</returns>
    public bool IsReduceMotion => reduceMotion;

    // Inspector Fields  #######################################################
    [Header("Anchor")]
    [SerializeField]
    [Tooltip("time the view takes to catch up with the orbit angle; s")]
    private float anchorYawSmoothTimeS = 0.35f;

    [SerializeField]
    [Tooltip("orbit angle the monkey may lead by before the view turns; deg")]
    private float anchorYawDeadZoneDeg = 4f;

    [SerializeField]
    [Tooltip("fastest the view may turn; deg/s")]
    private float anchorMaxYawSpeedDeg = 90f;

    [Header("Climb Framing")]
    [SerializeField]
    [Tooltip("aim height above the monkey at rest, looks up by default; u")]
    private float restAimOffsetU = 2.5f;

    [SerializeField]
    [Tooltip("extra aim height at full climb speed; u")]
    private float climbLeadU = 2f;

    [SerializeField]
    [Tooltip("extra aim offset at full descent speed, negative is below; u")]
    private float descendLeadU = -1.5f;

    [SerializeField]
    [Tooltip("vertical speed that counts as full lead; u/s")]
    private float fullLeadSpeedU = 6f;

    [SerializeField]
    [Tooltip("vertical speed below which the lead stays at rest; u/s")]
    private float leadSpeedDeadZoneU = 0.3f;

    [SerializeField]
    [Tooltip("time the lead takes to follow the speed; s")]
    private float leadSmoothTimeS = 0.6f;

    [SerializeField]
    [Tooltip("sideways aim shift toward the fruit while grab is held; u")]
    private float grabShiftU = 1.5f;

    [SerializeField]
    [Tooltip("time the grab shift takes to ease in and out; s")]
    private float grabShiftSmoothTimeS = 0.4f;

    [Header("Shot: Coconut Track")]
    [SerializeField]
    private ShotSettings coconutShot = new ShotSettings(2f, 0.8f, 0.2f, 30);

    [SerializeField]
    [Tooltip("extra time the shot stays after the coconut landed; s")]
    private float coconutLandHoldS = 0.8f;

    [Header("Shot: Cobra Alert")]
    [SerializeField]
    private ShotSettings cobraAlertShot = new ShotSettings(1.5f, 0.8f, 4f, 20);

    [SerializeField]
    [Tooltip(
        "cobra head distance below the monkey that raises an alert, "
            + "0 turns it off; u"
    )]
    private float cobraProximityU = 0f;

    [Header("Shot: Pineapple Leap")]
    [SerializeField]
    private ShotSettings leapShot = new ShotSettings(3f, 1f, 0f, 40);

    [SerializeField]
    [Tooltip("extra aim height during a leap; u")]
    private float leapAimOffsetU = 4f;

    [SerializeField]
    [Tooltip("scale on the follow distance during a leap, above 1 pulls back")]
    private float leapDistanceScale = 1.5f;

    [Header("Shake")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("scale on every shake, 0 silences all of it")]
    private float shakeScale = 1f;

    [SerializeField]
    [Tooltip("noise amplitude gain while moving on rough bark")]
    private float roughBarkNoiseGain = 2f;

    [SerializeField]
    [Tooltip("noise frequency gain while moving on rough bark")]
    private float roughBarkNoiseFrequency = 1.5f;

    [SerializeField]
    [Tooltip("time the bark shake takes to rise; s")]
    private float roughBarkEaseInS = 0.25f;

    [SerializeField]
    [Tooltip("time the bark shake takes to fade; s")]
    private float roughBarkEaseOutS = 0.5f;

    [SerializeField]
    [Tooltip("scale on the light hit impulse")]
    private float lightHitScale = 1f;

    [SerializeField]
    [Tooltip("scale on the heavy hit impulse")]
    private float heavyHitScale = 0.6f;

    [Header("Stamina Pulse")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("vignette peak, move attempt below the low fraction, bar not empty")]
    private float lowVignetteIntensity = 0.4f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("vignette peak, move attempt on an empty bar")]
    private float emptyVignetteIntensity = 0.7f;

    [SerializeField]
    [Tooltip("ease from rest to peak; s")]
    private float easeInS = 0.1f;

    [SerializeField]
    [Tooltip("ease from peak back to rest; s")]
    private float easeOutS = 1.2f;

    [SerializeField]
    [Tooltip("wait after a pulse ends before the next may start; s")]
    private float retriggerCooldownS = 0.5f;

    [Header("Comfort")]
    [SerializeField]
    [Tooltip("move as little as possible: no shake, noise, lead or pull-back")]
    private bool reduceMotion;

    // Editor Validation  ######################################################
    private void OnValidate() {
        anchorYawSmoothTimeS = Mathf.Max(0f, anchorYawSmoothTimeS);
        anchorYawDeadZoneDeg = Mathf.Max(0f, anchorYawDeadZoneDeg);
        anchorMaxYawSpeedDeg = Mathf.Max(1f, anchorMaxYawSpeedDeg);
        restAimOffsetU = Mathf.Max(0f, restAimOffsetU);
        climbLeadU = Mathf.Max(0f, climbLeadU);
        descendLeadU = Mathf.Min(0f, descendLeadU);
        fullLeadSpeedU = Mathf.Max(0.1f, fullLeadSpeedU);
        leadSpeedDeadZoneU = Mathf.Clamp(
            leadSpeedDeadZoneU,
            0f,
            fullLeadSpeedU
        );
        leadSmoothTimeS = Mathf.Max(0f, leadSmoothTimeS);
        grabShiftSmoothTimeS = Mathf.Max(0f, grabShiftSmoothTimeS);
        coconutLandHoldS = Mathf.Max(0f, coconutLandHoldS);
        cobraProximityU = Mathf.Max(0f, cobraProximityU);
        leapDistanceScale = Mathf.Max(1f, leapDistanceScale);
        roughBarkEaseInS = Mathf.Max(0f, roughBarkEaseInS);
        roughBarkEaseOutS = Mathf.Max(0f, roughBarkEaseOutS);
        lightHitScale = Mathf.Max(0f, lightHitScale);
        heavyHitScale = Mathf.Max(0f, heavyHitScale);
        easeInS = Mathf.Max(0f, easeInS);
        easeOutS = Mathf.Max(0f, easeOutS);
        retriggerCooldownS = Mathf.Max(0f, retriggerCooldownS);
    }
}
