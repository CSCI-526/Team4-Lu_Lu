using UnityEngine;

/// <summary>
/// Tuning of a pineapple leap's rise: how far it goes and how fast over time.
/// Consumed by <see cref="PineappleLeapController"/>.
/// </summary>
[CreateAssetMenu(
    fileName = "PineappleLeapConfig",
    menuName = "Scriptable Objects/PineappleLeapConfig"
)]
public class PineappleLeapConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>height a leap skips; u</returns>
    public float DistanceU => distanceU;

    /// <returns>x: seconds since the pull started; y: speed of the pull; u/s
    /// </returns>
    public AnimationCurve SpeedCurve => speedCurve;

    /// <returns>time the hands take to rise before the pull; s</returns>
    public float ArmExtendS => armExtendS;

    /// <returns>time hazards stay off after a leap lands; s</returns>
    public float LandGraceS => landGraceS;

    // Inspector Fields  #######################################################
    [SerializeField]
    [Min(0f)]
    [Tooltip("height a leap skips; u")]
    private float distanceU = 20f;

    [SerializeField]
    [Tooltip(
        "x: seconds since pull started; y: speed of pull, u/s; end value "
            + "holds until the distance is covered"
    )]
    private AnimationCurve speedCurve = new AnimationCurve(
        new Keyframe(0f, 6f, 0f, 30f),
        new Keyframe(0.6f, 22f, 0f, 0f),
        new Keyframe(1.25f, 8f, -20f, 0f)
    );

    [SerializeField]
    [Min(0f)]
    [Tooltip("time hands take to rise b4 the pull; s")]
    private float armExtendS = 0.15f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("time hazards stay off after a leap lands; s")]
    private float landGraceS = 0.5f;
}
