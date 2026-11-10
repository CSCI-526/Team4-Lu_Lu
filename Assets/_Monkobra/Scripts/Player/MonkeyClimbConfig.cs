using UnityEngine;

/// <summary>
/// Climb and orbit tuning for the monkey: how speed builds up while the climb
/// key is held, the exhaustion slow down, the idle stamina recovery, the
/// orbit speed round the trunk and the deceleration. Arms are in
/// <see cref="MonkeyArmConfig"/>, input actions in
/// <see cref="MonkeyInputConfig"/>, balance numbers in
/// <see cref="PlayerBalanceConfig"/>. Consumed by
/// <see cref="MonkeyController"/>.
/// </summary>
[CreateAssetMenu(
    fileName = "MonkeyClimbConfig",
    menuName = "Scriptable Objects/MonkeyClimbConfig"
)]
public class MonkeyClimbConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>x: seconds the up key has been held; y: climb speed moving
    /// up; u/s</returns>
    public AnimationCurve ClimbUpSpeedCurve => climbUpSpeedCurve;

    /// <returns>top climb speed moving up, the end value of the up speed
    /// curve; 0 if the curve is empty; u/s</returns>
    public float TopClimbUpSpeedU =>
        climbUpSpeedCurve.length == 0
            ? 0f
            : climbUpSpeedCurve[climbUpSpeedCurve.length - 1].value;

    /// <returns>x: seconds the down key has been held; y: climb speed moving
    /// down; u/s</returns>
    public AnimationCurve ClimbDownSpeedCurve => climbDownSpeedCurve;

    /// <returns>multiplier on upward climb speed while stamina is empty
    /// </returns>
    public float ExhaustedSpeedMultiplier => exhaustedSpeedMultiplier;

    /// <returns>stamina regained per s while the monkey gives no movement
    /// input</returns>
    public float IdleStaminaRecoveryPerS => idleStaminaRecoveryPerS;

    /// <returns>orbit speed around the trunk; deg/s</returns>
    public float RotationSpeedDeg => rotationSpeedDeg;

    /// <returns>ramp rate toward a stop once the climb key is released;
    /// u/s²</returns>
    public float DecelerationU => decelerationU;

    // Inspector Fields  #######################################################
    [Header("Climb Build-Up")]
    [SerializeField]
    [Tooltip(
        "x: seconds the up key has been held; y: climb speed moving up, "
            + "u/s; its end value is the top speed"
    )]
    private AnimationCurve climbUpSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 1.5f, 4.5f, 4.5f),
        new Keyframe(1f, 6f, 4.5f, 4.5f)
    );

    [SerializeField]
    [Tooltip(
        "x: seconds the down key has been held; y: climb speed moving "
            + "down, u/s; its end value is the top speed"
    )]
    private AnimationCurve climbDownSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 3f, 15f, 15f),
        new Keyframe(0.6f, 12f, 15f, 15f)
    );

    [Header("Exhaustion")]
    [SerializeField]
    [Tooltip("upward climb speed multiplier while stamina is empty")]
    private float exhaustedSpeedMultiplier = 0.2f;

    [Header("Recovery")]
    [SerializeField]
    [Tooltip("stamina regained per s while no movement key is pressed")]
    private float idleStaminaRecoveryPerS = 2.5f;

    [Header("Orbit")]
    [SerializeField]
    [Tooltip("orbit speed around the trunk; deg/s")]
    private float rotationSpeedDeg = 50f;

    [SerializeField]
    [Tooltip("ramp rate toward a stop once the climb key is released; u/s²")]
    private float decelerationU = 30f;

    // Editor Validation  ######################################################
    private void OnValidate() {
        exhaustedSpeedMultiplier = Mathf.Clamp01(exhaustedSpeedMultiplier);
        idleStaminaRecoveryPerS = Mathf.Max(0f, idleStaminaRecoveryPerS);
        rotationSpeedDeg = Mathf.Max(0f, rotationSpeedDeg);
        decelerationU = Mathf.Max(0f, decelerationU);
    }
}
