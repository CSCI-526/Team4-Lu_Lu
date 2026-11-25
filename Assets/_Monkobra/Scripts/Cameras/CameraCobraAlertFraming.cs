using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Flies the cobra alert shot: a slow, smooth pull away from the monkey view
/// down to a view of the whole cobra, a long look, and the same path back. It
/// never relies on a Cinemachine blend: the shot is a hard cut to this camera,
/// which starts exactly on the live view and then moves itself.
/// <para>
/// The framed cobra view is the end of the move. The camera stands on the
/// head's own side of the trunk, a set horizontal distance out along the
/// head's angle at the monkey's ring, so the trunk never hides the head; w/o a
/// head it keeps the monkey's side & the anchor's yaw. It aims at the middle
/// of the cobra's own coil, wherever that is below or above the monkey, from a
/// height that makes the view look down by a set angle, and widens the field of
/// view just enough to fit the coil, up to a limit.
/// </para>
/// <para>
/// <see cref="BeginJourney"/> saves the live view (position, rotation, field
/// of view) as the start, then each frame the camera is that view blended
/// into the framed one by an eased progress: it speeds up, then slows to a
/// stop on arrival, holds, and leaves the same way in reverse, ending on the
/// saved view so the cut back finds the same picture. The camera's own
/// <c>CinemachineFollow</c> & composer are switched off, so this script owns
/// its pose; it sits idle outside a journey.
/// </para>
/// Sits on the cobra alert camera. Runs after the anchor (order -50) and
/// before the <c>CinemachineBrain</c> (order 100). The times are in
/// <see cref="CameraConfig"/>.
/// </summary>
[DefaultExecutionOrder(-30)]
[RequireComponent(typeof(CinemachineFollow))]
[RequireComponent(typeof(CinemachineCamera))]
public class CameraCobraAlertFraming: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>
    /// Starts a journey from the live view, call as the shot goes live
    /// </summary>
    /// <param name="inS">time to pull away to the cobra view; s</param>
    /// <param name="seeS">time to stay on the cobra view; s</param>
    /// <param name="outS">time to pull back to the start view; s</param>
    public void BeginJourney(float inS, float seeS, float outS) {
        Camera view = Camera.main;
        if (view == null) {
            Debug.LogWarning(
                "CameraCobraAlertFraming:\tno main camera, no journey", this);
            return;
        }
        startPosition = view.transform.position;
        startRotation = view.transform.rotation;
        startFovDeg = view.fieldOfView;
        travelInS = Mathf.Max(0.01f, inS);
        seeHoldS = Mathf.Max(0f, seeS);
        travelOutS = Mathf.Max(0.01f, outS);
        journeyStartTime = Time.time;
        isPrimed = false;
        isJourneyOn = true;
    }

    // Inspector Fields  #######################################################
    [Tooltip("distance, pitch, fit margin, field of view limit, smoothing")]
    [SerializeField]
    private CameraConfig config;

    [Tooltip("the camera anchor, the monkey's position and yaw")]
    [SerializeField]
    private Transform anchor;

    [Tooltip("empty transform marking the aim point, moved by this script")]
    [SerializeField]
    private Transform focus;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "CameraCobraAlertFraming:\tmust assign Inspector Field: config",
                this
            );
        }
        if (anchor == null) {
            Debug.LogWarning(
                "CameraCobraAlertFraming:\tmust assign Inspector Field: anchor",
                this
            );
        }
        if (focus == null) {
            Debug.LogWarning(
                "CameraCobraAlertFraming:\tmust assign Inspector Field: focus",
                this
            );
        }
        if (config == null || anchor == null || focus == null) {
            enabled = false;
            return;
        }

        vcam = GetComponent<CinemachineCamera>();
        baseFovDeg = vcam.Lens.FieldOfView;
        fovDeg = baseFovDeg;

        // this script owns the pose, so the camera's own tracking steps aside
        GetComponent<CinemachineFollow>().enabled = false;
        CinemachineRotationComposer composer =
            GetComponent<CinemachineRotationComposer>();
        if (composer != null) {
            composer.enabled = false;
        }
    }

    // the cobra lives in another prefab, so it is read after every Awake ran
    private void Start() {
        cobra = FindFirstObjectByType<CobraMotor>();
        focusY = anchor.position.y;
    }

    private void LateUpdate() {
        if (!isJourneyOn) {
            return;
        }

        float elapsedS = Time.time - journeyStartTime;
        float progress = GetProgress(elapsedS);
        if (elapsedS >= travelInS + seeHoldS + travelOutS) {
            // back on the start view: hold it till the shot is let go
            isJourneyOn = false;
        }

        GetFramedPose(
            out Vector3 endPosition,
            out Quaternion endRotation,
            out float endFovDeg
        );
        transform.SetPositionAndRotation(
            GetJourneyPosition(endPosition, progress),
            Quaternion.Slerp(startRotation, endRotation, progress)
        );
        vcam.Lens.FieldOfView = Mathf.Lerp(startFovDeg, endFovDeg, progress);
    }

    // private members  ########################################################
    private float baseFovDeg; // field of view as authored, the lower bound
    private float fovDeg; // eased field of view of the framed view
    private float fovVelocityDeg; // SmoothDamp state
    private float focusY; // eased height of the focus
    private float focusVelocityY; // SmoothDamp state
    private bool isPrimed; // eased values start from the first frame's target
    private bool isJourneyOn;
    private float journeyStartTime; // Time.time the journey began
    private float travelInS;
    private float seeHoldS;
    private float travelOutS;
    private Vector3 startPosition; // the live view the journey began on
    private Quaternion startRotation;
    private float startFovDeg;
    private Vector3 trunkAxis; // a point on the trunk axis, the orbit center
    private bool hasTrunkAxis; // whether the trunk axis is known

    // cached references  ------------------------------------------------------
    private CinemachineCamera vcam;
    private CobraMotor cobra;

    // private methods  ########################################################
    // 0 on the start view, 1 on the cobra view: eased up, held, eased back
    private float GetProgress(float elapsedS) {
        float raw;
        if (elapsedS < travelInS) {
            raw = elapsedS / travelInS;
        } else if (elapsedS < travelInS + seeHoldS) {
            raw = 1f;
        } else {
            raw = 1f - (elapsedS - travelInS - seeHoldS) / travelOutS;
        }
        raw = Mathf.Clamp01(raw);
        // smootherstep: speeds up, then slows to a stop, both ends gentle
        return raw * raw * raw * (raw * (raw * 6f - 15f) + 10f);
    }

    // where the camera is on its way from the start view to the cobra view:
    // it swings round the trunk, never across it, bulging outward half way so
    // it moves away from the tree; w/o a known trunk, a straight line
    private Vector3 GetJourneyPosition(Vector3 endPosition, float progress) {
        if (!hasTrunkAxis) {
            return Vector3.Lerp(startPosition, endPosition, progress);
        }
        Vector2 from = new Vector2(
            startPosition.x - trunkAxis.x, startPosition.z - trunkAxis.z);
        Vector2 to = new Vector2(
            endPosition.x - trunkAxis.x, endPosition.z - trunkAxis.z);
        if (from.sqrMagnitude < 0.0001f || to.sqrMagnitude < 0.0001f) {
            return Vector3.Lerp(startPosition, endPosition, progress);
        }
        float fromDeg = Mathf.Atan2(from.y, from.x) * Mathf.Rad2Deg;
        float toDeg = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
        float angleRad = (fromDeg + Mathf.DeltaAngle(fromDeg, toDeg) * progress)
            * Mathf.Deg2Rad;
        float distanceU = Mathf.Lerp(from.magnitude, to.magnitude, progress)
            + config.CobraAlertPullOutU * Mathf.Sin(Mathf.PI * progress);
        return new Vector3(
            trunkAxis.x + Mathf.Cos(angleRad) * distanceU,
            Mathf.Lerp(startPosition.y, endPosition.y, progress),
            trunkAxis.z + Mathf.Sin(angleRad) * distanceU
        );
    }

    // the view that shows the whole cobra, tracking it as it moves
    private void GetFramedPose(
        out Vector3 position, out Quaternion rotation, out float fov
    ) {
        float smoothS = config.CobraAlertSmoothTimeS;

        // the span to show: the whole cobra, the monkey is not part of it,
        // since the cobra may lie far below; without a cobra, the monkey
        Bounds span = new Bounds(anchor.position, Vector3.zero);
        if (cobra != null && cobra.TryGetBodyBounds(out Bounds body)) {
            span = body;
        }

        // the height eased; the config's y offset lifts the whole shot above
        // the coil's middle
        float targetFocusY = span.center.y + config.CobraAlertYOffsetU;
        if (!isPrimed) {
            focusY = targetFocusY;
            focusVelocityY = 0f;
        }
        focusY = Mathf.SmoothDamp(
            focusY,
            targetFocusY,
            ref focusVelocityY,
            smoothS
        );
        // on the head's own side of the trunk, out along its angle, so the
        // trunk never hides the head; w/o a head, the monkey's side
        Vector3 anchorPos = anchor.position;
        Vector3 place = anchorPos;
        Quaternion yaw = anchor.rotation;
        if (cobra != null
            && cobra.TryGetHead(out Vector3 head, out Vector3 axis)) {
            trunkAxis = axis;
            hasTrunkAxis = true;
            Vector3 headOut = head - axis;
            headOut.y = 0f;
            if (headOut.sqrMagnitude > 0.0001f) {
                headOut.Normalize();
                float ringU = new Vector2(
                    anchorPos.x - axis.x, anchorPos.z - axis.z).magnitude;
                place = axis + headOut * ringU;
                yaw = Quaternion.LookRotation(headOut, Vector3.up);
            }
        }
        Vector3 focusPos = new Vector3(place.x, focusY, place.z);
        focus.SetPositionAndRotation(focusPos, yaw);

        // close, out from the monkey along the anchor's yaw, and above the
        // focus by the height that makes the line of sight dip by the angle
        float distanceU = config.CobraAlertDistanceU;
        float heightU = distanceU
            * Mathf.Tan(config.CobraAlertPitchDeg * Mathf.Deg2Rad);
        position = focusPos + yaw * new Vector3(0f, heightU, distanceU);
        rotation = Quaternion.LookRotation(focusPos - position, Vector3.up);

        // horizontal distance from the camera to the middle of the coil,
        // and the coil's radius: the near side is closer than the middle by
        // that radius, the width shows at the middle
        Vector3 outward = yaw * Vector3.forward;
        Vector2 outwardXZ = new Vector2(outward.x, outward.z).normalized;
        Vector2 cameraXZ = new Vector2(place.x, place.z)
            + outwardXZ * distanceU;
        float middleU = Vector2.Distance(
            cameraXZ,
            new Vector2(span.center.x, span.center.z)
        );
        float radiusU = 0.5f * Mathf.Max(span.size.x, span.size.z);
        float nearU = Mathf.Max(0.5f, middleU - radiusU);
        float margin = config.CobraAlertFitMargin;
        float fitHeightDeg = 2f * Mathf.Atan(
            0.5f * span.size.y * margin / nearU
        ) * Mathf.Rad2Deg;
        float fitWidthDeg = 2f * Mathf.Atan(
            radiusU * margin / (Mathf.Max(0.5f, middleU) * GetAspect())
        ) * Mathf.Rad2Deg;
        float targetFovDeg = Mathf.Clamp(
            Mathf.Max(fitHeightDeg, fitWidthDeg),
            baseFovDeg,
            Mathf.Max(baseFovDeg, config.CobraAlertMaxFovDeg)
        );
        if (!isPrimed) {
            fovDeg = targetFovDeg;
            fovVelocityDeg = 0f;
            isPrimed = true;
        }
        fovDeg = Mathf.SmoothDamp(
            fovDeg,
            targetFovDeg,
            ref fovVelocityDeg,
            smoothS
        );
        fov = fovDeg;
    }

    private static float GetAspect() {
        Camera view = Camera.main;
        if (view != null) {
            return Mathf.Max(0.1f, view.aspect);
        }
        return Screen.height > 0 ? (float)Screen.width / Screen.height : 1.78f;
    }
}
