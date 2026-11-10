using UnityEngine;

/// <summary>
/// The one transform every gameplay camera tracks, in place of the monkey's
/// rigidbody. Sits on the monkey's rendered position, so the interpolated
/// body moves it smoothly, and turns only about the world up axis: its yaw
/// follows the monkey's angle round the trunk, eased and behind a dead zone,
/// so strafing swings the view gently instead of spinning the world, and a
/// spin of the body itself (eg the boss finisher) never reaches the camera.
/// <para>
/// Runs in <c>LateUpdate</c> before the <c>CinemachineBrain</c> (order 100),
/// so the cameras read this frame's pose.
/// </para>
/// </summary>
[DefaultExecutionOrder(-50)]
public class CameraAnchor: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>this frame's eased yaw round the trunk; deg</returns>
    public float YawDeg => yawDeg;

    // Public Methods  #########################################################
    /// <summary>
    /// Jumps to the monkey with no easing, eg after a teleport
    /// </summary>
    public void Snap() {
        if (monkey == null) {
            return;
        }
        yawDeg = CalcTargetYawDeg();
        yawVelocityDeg = 0f;
        ApplyPose();
    }

    // Inspector Fields  #######################################################
    [Tooltip("the monkey's body, its rendered position is followed")]
    [SerializeField]
    private Transform monkey;

    [Tooltip("yaw easing values, the Anchor section")]
    [SerializeField]
    private CameraConfig config;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (monkey == null) {
            Debug.LogWarning(
                "CameraAnchor:\tmust assign Inspector Field: monkey",
                this
            );
        }
        if (config == null) {
            Debug.LogWarning(
                "CameraAnchor:\tmust assign Inspector Field: config",
                this
            );
        }
        if (monkey == null || config == null) {
            enabled = false;
        }
    }

    private void OnEnable() {
        if (monkey != null && config != null) {
            Snap();
        }
    }

    private void LateUpdate() {
        float targetDeg = CalcTargetYawDeg();
        float errorDeg = Mathf.DeltaAngle(yawDeg, targetDeg);
        float absErrorDeg = Mathf.Abs(errorDeg);
        // inside the dead zone the view stays put
        float desiredDeg = absErrorDeg <= config.AnchorYawDeadZoneDeg
            ? yawDeg
            : targetDeg - Mathf.Sign(errorDeg) * config.AnchorYawDeadZoneDeg;
        yawDeg = Mathf.SmoothDampAngle(
            yawDeg,
            desiredDeg,
            ref yawVelocityDeg,
            config.AnchorYawSmoothTimeS,
            config.AnchorMaxYawSpeedDeg,
            Time.deltaTime
        );
        ApplyPose();
    }

    // private members  ########################################################
    private float yawDeg; // eased yaw round the trunk
    private float yawVelocityDeg; // SmoothDamp state

    // private methods  ########################################################
    // the monkey's angle round its orbit center, the same angle its
    // controller steers by; never read from the body's own rotation
    private float CalcTargetYawDeg() {
        Transform center = monkey.parent;
        Vector3 toMonkey = center != null
            ? monkey.position - center.position
            : monkey.position;
        if (new Vector2(toMonkey.x, toMonkey.z).sqrMagnitude < 1e-6f) {
            return yawDeg;
        }
        return Mathf.Atan2(toMonkey.x, toMonkey.z) * Mathf.Rad2Deg;
    }

    private void ApplyPose() {
        transform.SetPositionAndRotation(
            monkey.position,
            Quaternion.Euler(0f, yawDeg, 0f)
        );
    }
}
