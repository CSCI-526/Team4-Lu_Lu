using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Frames the spider king's intro shot so the monkey and the king both are in
/// view. The camera stands out from the monkey along the anchor's yaw, a bit
/// below it, and looks up: the aim is the middle of the span from the
/// monkey's feet to the king's top, and the field of view widens just enough
/// to fit that span, up to a limit. Idle until <see cref="SetSubject"/> names
/// the king, so a shot cut to w/o one stays on the anchor.
/// <para>
/// Sits on the spider intro camera, which tracks <c>focus</c>, an empty
/// transform this script moves. Runs every frame once it has a subject, also
/// while the shot is on standby, so a cut finds the camera already framed.
/// Runs after the anchor (order -50) and before the <c>CinemachineBrain</c>
/// (order 100).
/// </para>
/// </summary>
[DefaultExecutionOrder(-30)]
[RequireComponent(typeof(CinemachineFollow))]
[RequireComponent(typeof(CinemachineCamera))]
public class CameraSpiderIntroFraming: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>
    /// Starts framing a subject with the monkey
    /// </summary>
    /// <param name="newSubject">what to fit in the view beside the monkey,
    /// null to go idle</param>
    public void SetSubject(Transform newSubject) {
        subject = newSubject;
        isPrimed = false;
    }

    // Inspector Fields  #######################################################
    [Tooltip("distance, height below, fit margin, field of view limit, "
        + "smoothing")]
    [SerializeField]
    private CameraConfig config;

    [Tooltip("the camera anchor, the monkey's position and yaw")]
    [SerializeField]
    private Transform anchor;

    [Tooltip("empty transform this camera tracks, moved by this script")]
    [SerializeField]
    private Transform focus;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "CameraSpiderIntroFraming:\tmust assign Inspector Field: "
                    + "config",
                this
            );
        }
        if (anchor == null) {
            Debug.LogWarning(
                "CameraSpiderIntroFraming:\tmust assign Inspector Field: "
                    + "anchor",
                this
            );
        }
        if (focus == null) {
            Debug.LogWarning(
                "CameraSpiderIntroFraming:\tmust assign Inspector Field: "
                    + "focus",
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

    private void LateUpdate() {
        if (subject == null) {
            return;
        }

        // the span to show: the monkey's feet up to the king's top
        float bottomY = anchor.position.y - FEET_BELOW_ANCHOR_U;
        float topY = Mathf.Max(
            subject.position.y + SUBJECT_ABOVE_U,
            anchor.position.y + SUBJECT_ABOVE_U
        );
        float targetFocusY = 0.5f * (bottomY + topY);
        Vector3 anchorPos = anchor.position;
        if (!isPrimed) {
            focusY = targetFocusY;
            fovDeg = baseFovDeg;
            focusVelocityY = 0f;
            fovVelocityDeg = 0f;
            isPrimed = true;
        }
        focusY = Mathf.SmoothDamp(
            focusY,
            targetFocusY,
            ref focusVelocityY,
            config.SpiderIntroSmoothTimeS
        );
        focus.SetPositionAndRotation(
            new Vector3(anchorPos.x, focusY, anchorPos.z),
            anchor.rotation
        );

        // out along the anchor's yaw, below the monkey: the offset is
        // relative to the focus, which sits above the monkey
        float distanceU = config.SpiderIntroDistanceU;
        float cameraY = anchorPos.y - config.SpiderIntroBelowU;
        follow.FollowOffset = new Vector3(0f, cameraY - focusY, distanceU);

        // the angles to the span's ends, seen from the camera, against the
        // angle it looks at: the field of view must hold the wider side
        float lookDeg = Mathf.Atan2(focusY - cameraY, distanceU)
            * Mathf.Rad2Deg;
        float topDeg = Mathf.Atan2(topY - cameraY, distanceU) * Mathf.Rad2Deg;
        float bottomDeg = Mathf.Atan2(bottomY - cameraY, distanceU)
            * Mathf.Rad2Deg;
        float halfFitDeg = Mathf.Max(topDeg - lookDeg, lookDeg - bottomDeg)
            * config.SpiderIntroFitMargin;
        float targetFovDeg = Mathf.Clamp(
            2f * halfFitDeg,
            baseFovDeg,
            Mathf.Max(baseFovDeg, config.SpiderIntroMaxFovDeg)
        );
        fovDeg = Mathf.SmoothDamp(
            fovDeg,
            targetFovDeg,
            ref fovVelocityDeg,
            config.SpiderIntroSmoothTimeS
        );
        vcam.Lens.FieldOfView = fovDeg;
    }

    // constants  ##############################################################
    // room under the anchor for the monkey's feet, and over the subject for
    // its top; u
    private const float FEET_BELOW_ANCHOR_U = 1f;
    private const float SUBJECT_ABOVE_U = 1.5f;

    // private members  ########################################################
    private Transform subject;
    private bool isPrimed; // eased values start from the first frame's target
    private float baseFovDeg; // field of view as authored, the lower bound
    private float fovDeg; // eased field of view
    private float fovVelocityDeg; // SmoothDamp state
    private float focusY; // eased height of the focus
    private float focusVelocityY; // SmoothDamp state

    // cached references  ------------------------------------------------------
    private CinemachineFollow follow;
    private CinemachineCamera vcam;
}
