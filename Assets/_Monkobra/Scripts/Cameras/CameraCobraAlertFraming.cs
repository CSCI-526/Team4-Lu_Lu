using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Frames the cobra alert shot so the whole cobra is in view, its head
/// first. The camera stands on the head's own side of the trunk, a set
/// horizontal distance out along the head's angle at the monkey's ring, so
/// the trunk never hides the head (the shot is cut to, so the turn from the
/// climb view never shows); w/o a head it keeps the monkey's side & the
/// anchor's yaw. It aims at the middle of the cobra's
/// own coil, wherever that is below or above the monkey, from a height that
/// makes the view look down by a set angle, and widens the field of view just
/// enough to fit the coil, up to a limit.
/// <para>
/// Sits on the cobra alert camera, which tracks <c>focus</c>, an empty
/// transform this script moves. Runs every frame, also while the shot is on
/// standby, so a cut finds the camera already framed. Runs after the anchor
/// (order -50) and before the <c>CinemachineBrain</c> (order 100).
/// </para>
/// </summary>
[DefaultExecutionOrder(-30)]
[RequireComponent(typeof(CinemachineFollow))]
[RequireComponent(typeof(CinemachineCamera))]
public class CameraCobraAlertFraming: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Tooltip("distance, pitch, fit margin, field of view limit, smoothing")]
    [SerializeField]
    private CameraConfig config;

    [Tooltip("the camera anchor, the monkey's position and yaw")]
    [SerializeField]
    private Transform anchor;

    [Tooltip("empty transform this camera tracks, moved only in height")]
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

        follow = GetComponent<CinemachineFollow>();
        vcam = GetComponent<CinemachineCamera>();
        baseFovDeg = vcam.Lens.FieldOfView;
        fovDeg = baseFovDeg;
    }

    // the cobra lives in another prefab, so it is read after every Awake ran
    private void Start() {
        cobra = FindFirstObjectByType<CobraMotor>();
        focusY = anchor.position.y;
    }

    private void LateUpdate() {
        float smoothS = config.CobraAlertSmoothTimeS;

        // the span to show: the whole cobra, the monkey is not part of it,
        // since the cobra may lie far below; without a cobra, the monkey
        Bounds span = new Bounds(anchor.position, Vector3.zero);
        if (cobra != null && cobra.TryGetBodyBounds(out Bounds body)) {
            span = body;
        }

        // the height eased; the config's y offset lifts the whole shot above
        // the coil's middle
        focusY = Mathf.SmoothDamp(
            focusY,
            span.center.y + config.CobraAlertYOffsetU,
            ref focusVelocityY,
            smoothS
        );
        // on the head's own side of the trunk, out along its angle, so the
        // trunk never hides the head; w/o a head, the monkey's side
        Vector3 anchorPos = anchor.position;
        Vector3 place = anchorPos;
        Quaternion yaw = anchor.rotation;
        if (cobra != null && cobra.TryGetHead(out Vector3 head, out Vector3 axis)) {
            Vector3 headOut = head - axis;
            headOut.y = 0f;
            if (headOut.sqrMagnitude > 0.0001f) {
                headOut.Normalize();
                float ringU = new Vector2(anchorPos.x - axis.x, anchorPos.z - axis.z).magnitude;
                place = axis + headOut * ringU;
                yaw = Quaternion.LookRotation(headOut, Vector3.up);
            }
        }
        focus.SetPositionAndRotation(
            new Vector3(place.x, focusY, place.z),
            yaw
        );

        // close, out from the monkey along the anchor's yaw, and above the
        // focus by the height that makes the line of sight dip by the angle
        float distanceU = config.CobraAlertDistanceU;
        float heightU = distanceU
            * Mathf.Tan(config.CobraAlertPitchDeg * Mathf.Deg2Rad);
        follow.FollowOffset = new Vector3(0f, heightU, distanceU);

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
        fovDeg = Mathf.SmoothDamp(
            fovDeg,
            targetFovDeg,
            ref fovVelocityDeg,
            smoothS
        );
        vcam.Lens.FieldOfView = fovDeg;
    }

    // private members  ########################################################
    private float baseFovDeg; // field of view as authored, the lower bound
    private float fovDeg; // eased field of view
    private float fovVelocityDeg; // SmoothDamp state
    private float focusY; // eased height of the focus
    private float focusVelocityY; // SmoothDamp state

    // cached references  ------------------------------------------------------
    private CinemachineFollow follow;
    private CinemachineCamera vcam;
    private CobraMotor cobra;

    // private methods  ########################################################
    private static float GetAspect() {
        Camera view = Camera.main;
        if (view != null) {
            return Mathf.Max(0.1f, view.aspect);
        }
        return Screen.height > 0 ? (float)Screen.width / Screen.height : 1.78f;
    }
}
