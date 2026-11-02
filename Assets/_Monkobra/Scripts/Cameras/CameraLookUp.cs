using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tilts the behind camera up while the player climbs and down while the
/// player descends. Moves the <see cref="CinemachineRotationComposer"/> aim
/// point above or below the tracked target, found on the same GameObject, in
/// proportion to vertical move input, so the camera changes its angle and
/// never its position. Leaves center fast and settles back slowly.
/// <para>
/// Reads the move action from the shared <see cref="MonkeyConfig"/> asset,
/// the same one the body and arms read. Set the composer's own Damping Y to 0,
/// this script does the smoothing.
/// </para>
/// </summary>
[RequireComponent(typeof(CinemachineRotationComposer))]
public class CameraLookUp: MonoBehaviour {
    // Public Members  #########################################################
    /// <summary>
    /// Extra aim height added on top of the input tilt, eg to keep the boss
    /// above the monkey in view; eased in & out; u
    /// </summary>
    public float ExtraAimOffsetU {
        set => extraTargetU = value;
    }

    /// <summary>
    /// Scale on the follow offset, 1 = normal; above 1 pulls the camera back
    /// along its own direction to show more, eg during the boss fight; eased
    /// in & out
    /// </summary>
    public float ViewDistanceScale {
        set => distanceTargetScale = Mathf.Max(0.1f, value);
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("shared input tuning, supplies the move action")]
    private MonkeyConfig config;

    [SerializeField]
    [Tooltip("aim point height above the target at full up input; u")]
    private float maxLookUpOffsetU = 6f;

    [SerializeField]
    [Tooltip("aim point offset at full down input, negative is below; u")]
    private float maxLookDownOffsetU = -1f;

    [SerializeField]
    [Tooltip("how fast the aim point leaves center while moving up/down; s")]
    private float riseSmoothTimeS = 0.12f;

    [SerializeField]
    [Tooltip("how fast the aim point settles back once vertical input ends; s")]
    private float settleSmoothTimeS = 0.5f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "CameraLookUp:\tmust assign Inspector Field: config",
                this
            );
        }
        else if (config.MoveAction == null) {
            Debug.LogWarning(
                "CameraLookUp:\tmust assign MonkeyConfig field: moveAction",
                config
            );
        }

        follow = GetComponent<CinemachineFollow>();
        if (follow != null) {
            baseFollowOffset = follow.FollowOffset;
        }

        composer = GetComponent<CinemachineRotationComposer>();
        if (composer == null) {
            Debug.LogError(
                "CameraLookUp:\tfail to get Component: "
                    + "CinemachineRotationComposer",
                this
            );
        }
    }

    private void LateUpdate() {
        if (composer == null || config == null || config.MoveAction == null) {
            return;
        }

        float verticalInput = config.MoveAction.action.ReadValue<Vector2>().y;
        float targetOffsetU =
            verticalInput >= 0f
                ? verticalInput * maxLookUpOffsetU
                : -verticalInput * maxLookDownOffsetU;
        // moving away from center rises fast, returning to center settles slow
        float smoothTimeS =
            Mathf.Abs(targetOffsetU) > Mathf.Abs(offsetU)
                ? riseSmoothTimeS
                : settleSmoothTimeS;
        offsetU = Mathf.SmoothDamp(
            offsetU,
            targetOffsetU,
            ref offsetVelocityU,
            smoothTimeS
        );

        extraOffsetU = Mathf.SmoothDamp(
            extraOffsetU,
            extraTargetU,
            ref extraVelocityU,
            settleSmoothTimeS
        );

        Vector3 offset = composer.TargetOffset;
        offset.y = offsetU + extraOffsetU;
        composer.TargetOffset = offset;

        distanceScale = Mathf.SmoothDamp(
            distanceScale,
            distanceTargetScale,
            ref distanceVelocity,
            settleSmoothTimeS
        );
        if (follow != null) {
            follow.FollowOffset = baseFollowOffset * distanceScale;
        }
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        maxLookUpOffsetU = Mathf.Max(0f, maxLookUpOffsetU);
        maxLookDownOffsetU = Mathf.Min(0f, maxLookDownOffsetU);
        riseSmoothTimeS = Mathf.Max(0f, riseSmoothTimeS);
        settleSmoothTimeS = Mathf.Max(0f, settleSmoothTimeS);
    }

    // private members  ########################################################
    private float offsetU; // current aim point height above target
    private float offsetVelocityU; // SmoothDamp state
    private float extraTargetU; // extra aim height asked for, eg by the boss
    private float extraOffsetU; // extra aim height, eased
    private float extraVelocityU; // SmoothDamp state
    private float distanceTargetScale = 1f; // follow offset scale asked for
    private float distanceScale = 1f; // follow offset scale, eased
    private float distanceVelocity; // SmoothDamp state
    private Vector3 baseFollowOffset; // follow offset as authored

    // cached references  ------------------------------------------------------
    private CinemachineRotationComposer composer;
    private CinemachineFollow follow; // optional, for the view distance
}
