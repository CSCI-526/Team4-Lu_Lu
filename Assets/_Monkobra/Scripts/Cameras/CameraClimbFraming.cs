using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Frames the climb view: the camera looks up by default, since the goal is
/// always above, and the monkey sits low on the screen. On top of that rest
/// aim it adds four eased offsets, all from <see cref="CameraConfig"/>:
/// <list type="bullet">
/// <item>a hold zoom-out: the field of view follows an animation curve of
/// degrees vs time the up input is held, from the base view to the final
/// view, so a long climb shows more tree, and it settles back once up is
/// released</item>
/// <item>a hold look-up: the longer the up input is held, the higher the
/// view looks, along an animation curve of height vs hold time up to its
/// last key, and it settles back once up is released</item>
/// <item>a lead that follows the real vertical speed of the tracked target,
/// not the raw input, behind a dead zone, so a blocked or trapped monkey
/// never tilts the view and a tap on the key never nods it</item>
/// <item>a sideways shift toward the fruit side while grab is held, the side
/// picked once at the press, so grab never cuts to another camera</item>
/// <item>an extra aim height and a pulled-back distance asked for by the boss
/// fight (<see cref="SetBossFraming"/>)</item>
/// </list>
/// Moves the <see cref="CinemachineRotationComposer"/> aim point and scales
/// the <see cref="CinemachineFollow"/> offset, so the camera changes its
/// angle and distance, and only the hold zoom-out changes its field of view.
/// Set the composer's Damping Y to 0, this script does the vertical
/// smoothing.
/// </summary>
[RequireComponent(typeof(CinemachineRotationComposer))]
public class CameraClimbFraming: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>
    /// Asks for the boss fight's framing; eased in and out
    /// </summary>
    /// <param name="aimU">extra aim height, eg to keep the boss above the
    /// monkey in view; u</param>
    /// <param name="distanceScale">scale on the follow offset, 1 is normal,
    /// above 1 pulls the camera back along its own direction</param>
    public void SetBossFraming(float aimU, float distanceScale) {
        bossAimTargetU = aimU;
        distanceTargetScale = Mathf.Max(0.1f, distanceScale);
    }

    // Inspector Fields  #######################################################
    [Tooltip("rest aim, lead, grab shift and smoothing values")]
    [SerializeField]
    private CameraConfig config;

    [Tooltip("shared input tuning, supplies the move action, only polled")]
    [SerializeField]
    private MonkeyInputConfig inputConfig;

    [Tooltip("the interact action; only polled here, never enabled")]
    [SerializeField]
    private InputActionReference interactAction;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "CameraClimbFraming:\tmust assign Inspector Field: config",
                this
            );
        }
        if (inputConfig == null || inputConfig.MoveAction == null) {
            Debug.LogWarning(
                "CameraClimbFraming:\tmust assign Inspector Field: "
                    + "inputConfig with a move action, no hold look-up",
                this
            );
        }
        if (interactAction == null) {
            Debug.LogWarning(
                "CameraClimbFraming:\tmust assign Inspector Field: "
                    + "interactAction, no grab shift",
                this
            );
        }

        composer = GetComponent<CinemachineRotationComposer>();
        follow = GetComponent<CinemachineFollow>();
        if (follow != null) {
            baseFollowOffset = follow.FollowOffset;
        }
        vcam = GetComponent<CinemachineCamera>();

        if (config == null) {
            enabled = false;
            return;
        }
        fovDeg = config.EvaluateHoldFovDeg(0f);
    }

    private void LateUpdate() {
        float dt = Time.deltaTime;
        if (dt <= 0f) {
            return;
        }

        UpdateVerticalSpeed(dt);
        UpdateGrabSide();
        UpdateUpHeld(dt);

        float leadTargetU = config.IsReduceMotion ? 0f : CalcLeadTargetU();
        leadU = Mathf.SmoothDamp(
            leadU,
            leadTargetU,
            ref leadVelocityU,
            config.LeadSmoothTimeS
        );
        float holdTargetU = config.IsReduceMotion
            ? 0f
            : config.EvaluateHoldLookUpU(upHeldS);
        holdU = Mathf.SmoothDamp(
            holdU,
            holdTargetU,
            ref holdVelocityU,
            config.HoldLookUpSmoothTimeS
        );
        float fovTargetDeg = config.EvaluateHoldFovDeg(
            config.IsReduceMotion ? 0f : upHeldS
        );
        fovDeg = Mathf.SmoothDamp(
            fovDeg,
            fovTargetDeg,
            ref fovVelocityDeg,
            config.HoldZoomSmoothTimeS
        );
        bossAimU = Mathf.SmoothDamp(
            bossAimU,
            bossAimTargetU,
            ref bossAimVelocityU,
            config.LeadSmoothTimeS
        );
        shiftU = Mathf.SmoothDamp(
            shiftU,
            grabSide * config.GrabShiftU,
            ref shiftVelocityU,
            config.GrabShiftSmoothTimeS
        );
        distanceScale = Mathf.SmoothDamp(
            distanceScale,
            distanceTargetScale,
            ref distanceVelocity,
            config.LeadSmoothTimeS
        );

        Vector3 offset = composer.TargetOffset;
        offset.x = shiftU;
        offset.y = config.RestAimOffsetU + leadU + holdU + bossAimU;
        composer.TargetOffset = offset;
        if (follow != null) {
            follow.FollowOffset = baseFollowOffset * distanceScale;
        }
        if (vcam != null) {
            vcam.Lens.FieldOfView = Mathf.Clamp(fovDeg, 5f, 120f);
        }
    }

    // private members  ########################################################
    // sign of the sideways shift: legacy side cameras aimed +x for the left
    // fruit and -x for the right one
    private const float LEFT_SIGN = 1f;
    private const float RIGHT_SIGN = -1f;
    private const float UP_INPUT_THRESHOLD = 0.1f;

    private float verticalSpeedU; // target climb speed, up positive; u/s
    private float lastTargetY;
    private bool hasLastTargetY;
    private float upHeldS; // time the up input has been held
    private float fovDeg; // eased field of view
    private float fovVelocityDeg; // SmoothDamp state
    private float holdU; // eased hold look-up
    private float holdVelocityU; // SmoothDamp state
    private float leadU; // eased lead
    private float leadVelocityU; // SmoothDamp state
    private float bossAimTargetU; // extra aim height asked for
    private float bossAimU; // extra aim height, eased
    private float bossAimVelocityU; // SmoothDamp state
    private float distanceTargetScale = 1f; // follow offset scale asked for
    private float distanceScale = 1f; // follow offset scale, eased
    private float distanceVelocity; // SmoothDamp state
    private float grabSide; // 0 none, else the sign of the shift, per press
    private float shiftU; // eased sideways aim shift
    private float shiftVelocityU; // SmoothDamp state
    private bool wasGrabHeld;
    private Vector3 baseFollowOffset; // follow offset as authored

    // cached references  ------------------------------------------------------
    private CinemachineRotationComposer composer;
    private CinemachineFollow follow; // optional, for the view distance
    private CinemachineCamera vcam;

    // private methods  ########################################################
    // the speed comes from the tracked target's own motion, so it is
    // zero whenever the monkey does not actually climb
    private void UpdateVerticalSpeed(float dt) {
        Transform target = vcam != null ? vcam.Follow : null;
        if (target == null) {
            verticalSpeedU = 0f;
            hasLastTargetY = false;
            return;
        }

        float y = target.position.y;
        verticalSpeedU = hasLastTargetY ? (y - lastTargetY) / dt : 0f;
        lastTargetY = y;
        hasLastTargetY = true;
    }

    // counts while up is held, from the input as the player presses it; a
    // release resets it, and the eased offset settles back on its own
    private void UpdateUpHeld(float dt) {
        float inputY = inputConfig != null && inputConfig.MoveAction != null
            ? inputConfig.MoveAction.action.ReadValue<Vector2>().y
            : 0f;
        upHeldS = inputY > UP_INPUT_THRESHOLD ? upHeldS + dt : 0f;
    }

    // up leads by the climb lead, down by the descent lead, both scaled by
    // how far the speed is past the dead zone toward full speed
    private float CalcLeadTargetU() {
        float speed = Mathf.Abs(verticalSpeedU);
        float t = Mathf.InverseLerp(
            config.LeadSpeedDeadZoneU,
            config.FullLeadSpeedU,
            speed
        );
        return verticalSpeedU >= 0f
            ? t * config.ClimbLeadU
            : t * config.DescendLeadU;
    }

    // the side is picked at the press and kept until the release, so the
    // view never flips while the key is held
    private void UpdateGrabSide() {
        bool isGrabHeld = interactAction != null
            && interactAction.action != null
            && interactAction.action.IsPressed();
        if (isGrabHeld && !wasGrabHeld) {
            grabSide = PickGrabSide();
        } else if (!isGrabHeld) {
            grabSide = 0f;
        }
        wasGrabHeld = isGrabHeld;
    }

    // right only if fruit is reachable there and not on the left; no shift
    // when neither side has fruit
    private static float PickGrabSide() {
        UpwardFruitSensor sensor = UpwardFruitSensor.I;
        if (sensor == null) {
            return 0f;
        }
        if (sensor.ReachForRight && !sensor.ReachForLeft) {
            return RIGHT_SIGN;
        }
        return sensor.ReachForLeft ? LEFT_SIGN : 0f;
    }
}
