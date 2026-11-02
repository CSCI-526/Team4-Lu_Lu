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
}
