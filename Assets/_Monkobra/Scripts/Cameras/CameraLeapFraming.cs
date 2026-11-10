using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Frames the pineapple leap shot: the aim point rises above the monkey to
/// show the landing area, and the camera pulls back along its own direction,
/// so the view widens by distance and never by field of view. Both values
/// come from <see cref="CameraConfig"/>; the comfort switch turns the
/// pull-back off. Sits on the leap camera, next to its
/// <see cref="CinemachineRotationComposer"/> and
/// <see cref="CinemachineFollow"/>. The shot is cut to and from by
/// <see cref="CameraRigManager"/> and eased by the brain's blend, so the
/// values here stay constant.
/// </summary>
[RequireComponent(typeof(CinemachineRotationComposer))]
[RequireComponent(typeof(CinemachineFollow))]
public class CameraLeapFraming: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Tooltip("leap aim height and pull-back scale")]
    [SerializeField]
    private CameraConfig config;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "CameraLeapFraming:\tmust assign Inspector Field: config",
                this
            );
            enabled = false;
            return;
        }

        composer = GetComponent<CinemachineRotationComposer>();
        follow = GetComponent<CinemachineFollow>();
        baseFollowOffset = follow.FollowOffset;
    }

    private void LateUpdate() {
        Vector3 offset = composer.TargetOffset;
        offset.y = config.LeapAimOffsetU;
        composer.TargetOffset = offset;

        float scale = config.IsReduceMotion ? 1f : config.LeapDistanceScale;
        follow.FollowOffset = baseFollowOffset * scale;
    }

    // private members  ########################################################
    private Vector3 baseFollowOffset; // follow offset as authored

    // cached references  ------------------------------------------------------
    private CinemachineRotationComposer composer;
    private CinemachineFollow follow;
}
