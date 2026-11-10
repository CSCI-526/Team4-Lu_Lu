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

    /// <summary>
    /// How far the aim rises for a time of holding up, from the hold curve
    /// </summary>
    /// <param name="heldS">time the up input has been held; s</param>
    /// <returns>extra aim height; u</returns>
    public float EvaluateHoldLookUpU(float heldS) {
        return Mathf.Max(0f, holdLookUpCurve.Evaluate(heldS));
    }

    /// <summary>
    /// The field of view for a time of holding up, from the zoom-out curve:
    /// its value at 0 s is the base view, at its last key the final view
    /// </summary>
    /// <param name="heldS">time the up input has been held; s</param>
    /// <returns>field of view; deg</returns>
    public float EvaluateHoldFovDeg(float heldS) {
        return holdFovCurve.Evaluate(heldS);
    }

    /// <returns>time the zoom-out takes to follow its curve, and to settle
    /// back once up is released; s</returns>
    public float HoldZoomSmoothTimeS => holdZoomSmoothTimeS;

    /// <returns>time the hold look-up takes to follow its curve, and to
    /// settle back once up is released; s</returns>
    public float HoldLookUpSmoothTimeS => holdLookUpSmoothTimeS;

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
    /// <returns>follows a thrown coconut down, and ends at the landing or
    /// at its duration, whichever comes first</returns>
    public ShotSettings CoconutShot => coconutShot;

    /// <returns>the view of the whole cobra when it first starts moving
    /// out of the starting zone</returns>
    public ShotSettings CobraAlertShot => cobraAlertShot;

    /// <returns>how far out from the monkey the cobra alert camera stands,
    /// horizontally, so it is close; u</returns>
    public float CobraAlertDistanceU => cobraAlertDistanceU;

    /// <returns>height added to the cobra alert's aim point and with it the
    /// camera, above the cobra's middle; raises the shot, so the cobra sits
    /// lower in the view; u</returns>
    public float CobraAlertYOffsetU => cobraAlertYOffsetU;

    /// <returns>how far the cobra alert looks down, the camera standing
    /// above its aim point; deg</returns>
    public float CobraAlertPitchDeg => cobraAlertPitchDeg;

    /// <returns>room kept round the cobra when the view is fitted to it;
    /// above 1 is looser</returns>
    public float CobraAlertFitMargin => cobraAlertFitMargin;

    /// <returns>widest field of view the cobra alert may use to fit the
    /// cobra; deg</returns>
    public float CobraAlertMaxFovDeg => cobraAlertMaxFovDeg;

    /// <returns>time the cobra alert's height and field of view take to
    /// follow the cobra; s</returns>
    public float CobraAlertSmoothTimeS => cobraAlertSmoothTimeS;

    /// <returns>looks up and pulls back while a pineapple leap runs</returns>
    public ShotSettings LeapShot => leapShot;

    /// <returns>extra aim height during a leap; u</returns>
    public float LeapAimOffsetU => leapAimOffsetU;

    /// <returns>scale on the follow distance during a leap, above 1 pulls
    /// back</returns>
    public float LeapDistanceScale => leapDistanceScale;

    /// <returns>camera height relative to the monkey during a leap, negative
    /// is below, so the view looks up from under; u</returns>
    public float LeapCameraHeightU => leapCameraHeightU;

    /// <returns>time the leap camera takes to catch up vertically with the
    /// monkey, so it trails the climb instead of tracking it; s</returns>
    public float LeapFollowLagS => leapFollowLagS;

    /// <returns>time the leap camera's aim takes to catch up; s</returns>
    public float LeapAimLagS => leapAimLagS;

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

    // cobra proximity vignette  -----------------------------------------------
    /// <returns>height gap to the great cobra below which the first stage
    /// (bottom half of the screen) shows; u</returns>
    public float CobraStage1DistanceU => cobraStage1DistanceU;

    /// <returns>height gap below which the second stage (full ring) shows;
    /// never above the first stage's; u</returns>
    public float CobraStage2DistanceU => cobraStage2DistanceU;

    /// <returns>extra gap a stage is kept for after crossing its distance,
    /// so a gap hovering on a boundary never flickers; u</returns>
    public float CobraStageHysteresisU => cobraStageHysteresisU;

    /// <returns>beats per s in the first stage</returns>
    public float CobraStage1PulseHz => cobraStage1PulseHz;

    /// <returns>beats per s in the second stage</returns>
    public float CobraStage2PulseHz => cobraStage2PulseHz;

    /// <returns>color of the cobra vignette; its alpha is ignored</returns>
    public Color CobraVignetteColor => cobraVignetteColor;

    /// <returns>alpha at the peak of a beat, 0 none, 1 solid</returns>
    public float CobraVignettePeakAlpha => cobraVignettePeakAlpha;

    /// <returns>ease from rest to peak in a beat; s</returns>
    public float CobraVignetteEaseInS => cobraVignetteEaseInS;

    /// <returns>ease from peak back to rest in a beat; s</returns>
    public float CobraVignetteEaseOutS => cobraVignetteEaseOutS;

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
    [Tooltip(
        "extra aim height (y, u) vs time the up input has been held "
            + "(x, s): the longer up is held the higher the view looks, up "
            + "to the last key's value. Releasing up settles back"
    )]
    private AnimationCurve holdLookUpCurve =
        AnimationCurve.EaseInOut(0f, 0f, 3f, 4f);

    [SerializeField]
    [Tooltip(
        "field of view (y, deg) vs time the up input has been held (x, s), "
            + "the zoom out: the value at 0 s is the base view, the last "
            + "key's value the final view. Releasing up settles back"
    )]
    private AnimationCurve holdFovCurve =
        AnimationCurve.EaseInOut(0f, 65f, 3f, 75f);

    [SerializeField]
    [Tooltip("time the zoom-out takes to follow its curve and to settle back "
        + "after release; s")]
    private float holdZoomSmoothTimeS = 0.5f;

    [SerializeField]
    [Tooltip("time the hold look-up takes to follow its curve and to settle "
        + "back after release; s")]
    private float holdLookUpSmoothTimeS = 0.3f;

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
    private ShotSettings coconutShot = new ShotSettings(1.2f, 0f, 0.2f, 30);

    [Header("Shot: Cobra Alert")]
    [SerializeField]
    private ShotSettings cobraAlertShot = new ShotSettings(1.5f, 0.8f, 4f, 20);

    [SerializeField]
    [Tooltip("how far out from the monkey the alert camera stands, "
        + "horizontally; u")]
    private float cobraAlertDistanceU = 5f;

    [SerializeField]
    [Tooltip("height added to the aim point and the camera above the cobra's "
        + "middle; raises the shot so the cobra sits lower in the view, "
        + "negative lowers it; u")]
    private float cobraAlertYOffsetU = 3f;

    [SerializeField]
    [Range(0f, 60f)]
    [Tooltip("how far the alert looks down; deg")]
    private float cobraAlertPitchDeg = 15f;

    [SerializeField]
    [Tooltip("room kept round the cobra when fitted; above 1 is looser")]
    private float cobraAlertFitMargin = 1.2f;

    [SerializeField]
    [Tooltip("widest field of view the alert may use to fit the cobra; deg")]
    private float cobraAlertMaxFovDeg = 100f;

    [SerializeField]
    [Tooltip("time the alert's height and field of view take to follow the "
        + "cobra; s")]
    private float cobraAlertSmoothTimeS = 0.3f;

    [Header("Shot: Pineapple Leap")]
    [SerializeField]
    private ShotSettings leapShot = new ShotSettings(5f, 0f, 0f, 40);

    [SerializeField]
    [Tooltip("extra aim height during a leap; u")]
    private float leapAimOffsetU = 6f;

    [SerializeField]
    [Tooltip("scale on the follow distance during a leap, above 1 pulls back")]
    private float leapDistanceScale = 1.5f;

    [SerializeField]
    [Tooltip("camera height vs the monkey during a leap, negative looks up "
        + "from below; u")]
    private float leapCameraHeightU = 0f;

    [SerializeField]
    [Tooltip("time the leap camera takes to catch up vertically, so it lags "
        + "behind the monkey; s")]
    private float leapFollowLagS = 1.2f;

    [SerializeField]
    [Tooltip("time the leap camera's aim takes to catch up; s")]
    private float leapAimLagS = 0.6f;

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

    [Header("Cobra Proximity Vignette")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("height gap to the great cobra below which stage 1 (bottom "
        + "half) shows; u")]
    private float cobraStage1DistanceU = 20f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("height gap below which stage 2 (full ring) shows, at most "
        + "the stage 1 gap; u")]
    private float cobraStage2DistanceU = 6f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("extra gap a stage is held for after crossing, no flicker; u")]
    private float cobraStageHysteresisU = 1f;

    [SerializeField]
    [Min(0.05f)]
    [Tooltip("beats per s in stage 1")]
    private float cobraStage1PulseHz = 0.6f;

    [SerializeField]
    [Min(0.05f)]
    [Tooltip("beats per s in stage 2")]
    private float cobraStage2PulseHz = 1.8f;

    [SerializeField]
    [Tooltip("vignette color, alpha ignored")]
    private Color cobraVignetteColor = new Color(0.1f, 0.85f, 0.2f, 1f);

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("alpha at the peak of a beat")]
    private float cobraVignettePeakAlpha = 0.55f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("ease from rest to peak in a beat; s")]
    private float cobraVignetteEaseInS = 0.12f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("ease from peak back to rest in a beat; s")]
    private float cobraVignetteEaseOutS = 0.3f;

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
        holdLookUpSmoothTimeS = Mathf.Max(0f, holdLookUpSmoothTimeS);
        holdZoomSmoothTimeS = Mathf.Max(0f, holdZoomSmoothTimeS);
        grabShiftSmoothTimeS = Mathf.Max(0f, grabShiftSmoothTimeS);
        cobraAlertDistanceU = Mathf.Max(0.5f, cobraAlertDistanceU);
        cobraAlertFitMargin = Mathf.Max(1f, cobraAlertFitMargin);
        cobraAlertMaxFovDeg = Mathf.Clamp(cobraAlertMaxFovDeg, 30f, 150f);
        cobraAlertSmoothTimeS = Mathf.Max(0f, cobraAlertSmoothTimeS);
        leapDistanceScale = Mathf.Max(1f, leapDistanceScale);
        leapFollowLagS = Mathf.Max(0f, leapFollowLagS);
        leapAimLagS = Mathf.Max(0f, leapAimLagS);
        roughBarkEaseInS = Mathf.Max(0f, roughBarkEaseInS);
        roughBarkEaseOutS = Mathf.Max(0f, roughBarkEaseOutS);
        lightHitScale = Mathf.Max(0f, lightHitScale);
        heavyHitScale = Mathf.Max(0f, heavyHitScale);
        easeInS = Mathf.Max(0f, easeInS);
        easeOutS = Mathf.Max(0f, easeOutS);
        retriggerCooldownS = Mathf.Max(0f, retriggerCooldownS);
        cobraStage1DistanceU = Mathf.Max(0f, cobraStage1DistanceU);
        cobraStage2DistanceU = Mathf.Clamp(
            cobraStage2DistanceU,
            0f,
            cobraStage1DistanceU
        );
        cobraStageHysteresisU = Mathf.Max(0f, cobraStageHysteresisU);
        cobraStage1PulseHz = Mathf.Max(0.05f, cobraStage1PulseHz);
        cobraStage2PulseHz = Mathf.Max(0.05f, cobraStage2PulseHz);
        cobraVignetteEaseInS = Mathf.Max(0f, cobraVignetteEaseInS);
        cobraVignetteEaseOutS = Mathf.Max(0f, cobraVignetteEaseOutS);
    }
}
