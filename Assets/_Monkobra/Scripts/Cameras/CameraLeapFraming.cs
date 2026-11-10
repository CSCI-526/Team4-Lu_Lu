using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

/// <summary>
/// Frames the pineapple leap shot: the aim point rises above the monkey to
/// show the landing area, the camera drops below the monkey so it looks up
/// from under, and it pulls back from the tree, so the view widens by
/// distance and never by field of view. The camera also trails the
/// monkey's climb, with a long vertical follow damping, so the monkey rises
/// ahead of the view instead of being tracked strictly. Both values
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
        baseDamping = follow.TrackerSettings.PositionDamping;
        baseAimDamping = composer.Damping;
    }

    private void LateUpdate() {
        Vector3 offset = composer.TargetOffset;
        offset.y = config.LeapAimOffsetU;
        composer.TargetOffset = offset;

        // comfort: keep the authored height and distance, only aim higher
        Vector3 followOffset = baseFollowOffset;
        if (!config.IsReduceMotion) {
            followOffset.y = config.LeapCameraHeightU;
            followOffset.z *= config.LeapDistanceScale;
        }
        follow.FollowOffset = followOffset;

        // the camera trails the monkey's climb instead of tracking it
        // strictly; comfort keeps the authored damping
        TrackerSettings tracker = follow.TrackerSettings;
        Vector3 damping = baseDamping;
        Vector2 aimDamping = baseAimDamping;
        if (!config.IsReduceMotion) {
            damping.y = Mathf.Max(damping.y, config.LeapFollowLagS);
            aimDamping.y = Mathf.Max(aimDamping.y, config.LeapAimLagS);
        }
        tracker.PositionDamping = damping;
        follow.TrackerSettings = tracker;
        composer.Damping = aimDamping;
    }

    // private members  ########################################################
    private Vector3 baseFollowOffset; // follow offset as authored
    private Vector3 baseDamping; // follow damping as authored
    private Vector2 baseAimDamping; // composer damping as authored

    // cached references  ------------------------------------------------------
    private CinemachineRotationComposer composer;
    private CinemachineFollow follow;
}
