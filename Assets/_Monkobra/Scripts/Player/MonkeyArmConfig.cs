using UnityEngine;

/// <summary>
/// Tuning of the monkey's arms: stroke range and speed, reach, resting
/// splay and visual thickness. Climb and orbit numbers are in
/// <see cref="MovementConfig"/>, the input actions in
/// <see cref="MonkeyConfig"/>. Consumed by <see cref="ArmController"/>.
/// <para>
/// <see cref="ArmController"/> takes nothing physical from here, springs,
/// limits, anchors and axes are authored on its
/// <see cref="ConfigurableJoint"/>.
/// </para>
/// </summary>
[CreateAssetMenu(
    fileName = "MonkeyArmConfig",
    menuName = "Scriptable Objects/MonkeyArmConfig"
)]
public class MonkeyArmConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>bottom of the stroke, arm fully pulled down, in deg</returns>
    public float MinAngleDeg => minAngleDeg;

    /// <returns>top of the stroke, arm fully reached up, in deg</returns>
    public float MaxAngleDeg => maxAngleDeg;

    /// <returns>stroke phase speed; deg/s, 360 is 1 stroke per s</returns>
    public float StrokeSpeedDeg => strokeSpeedDeg;

    /// <returns><see cref="StrokeSpeedDeg"/> as strokes per s, for a 0..1
    /// phase</returns>
    public float StrokeSpeedCycles => strokeSpeedDeg / 360f;

    /// <returns>speed the arm stretches out while interact is held; u/s
    /// </returns>
    public float ReachExtendSpeedU => reachExtendSpeedU;

    /// <returns>speed the held aim turns toward the fruit; deg/s, 0 snaps
    /// to it</returns>
    public float ReachAimSpeedDeg => reachAimSpeedDeg;

    /// <returns>if the linear drive target is negated to push the arm
    /// outward, the usual joint axis setup</returns>
    public bool InvertDriveAxis => invertDriveAxis;

    /// <returns>resting outward lean of each arm; deg</returns>
    public float SplayDeg => splayDeg;

    /// <returns>arm cube thickness across its 2 short axes; u</returns>
    public float ArmWidthU => armWidthU;

    // Inspector Fields  #######################################################
    [Header("Arm - Stroke")]
    [SerializeField]
    [Tooltip("bottom of the stroke, arm fully pulled down, in deg")]
    private float minAngleDeg = -15f;

    [SerializeField]
    [Tooltip("top of the stroke, arm fully reached up, in deg")]
    private float maxAngleDeg = 30f;

    [SerializeField]
    [Tooltip("stroke phase speed, in deg/s, 360 is 1 full stroke per sec")]
    private float strokeSpeedDeg = 240f;

    [Header("Arm - Reach")]
    [SerializeField]
    [Tooltip("arm stretch speed while interact is held; u/s")]
    private float reachExtendSpeedU = 4f;

    [SerializeField]
    [Tooltip(
        "speed the arm turns toward the fruit while interact is held, in "
            + "deg/s, 0 snaps to it"
    )]
    private float reachAimSpeedDeg = 360f;

    [SerializeField]
    [Tooltip("uncheck if an arm telescopes inward instead of outward")]
    private bool invertDriveAxis = true;

    [Header("Arm - Climb Cycle")]
    [SerializeField]
    [Tooltip(
        "resting outward lean of each arm, keeps the 2 off each other; deg"
    )]
    private float splayDeg = 20f;

    [Header("Arm - Visual")]
    [SerializeField]
    [Tooltip(
        "thickness of the arm cube across both axes it is not stretched "
            + "along, the authored scale is overwritten every step; u"
    )]
    private float armWidthU = 0.2f;

    // Editor Validation  ######################################################
    private void OnValidate() {
        maxAngleDeg = Mathf.Max(minAngleDeg, maxAngleDeg);
        strokeSpeedDeg = Mathf.Max(0f, strokeSpeedDeg);
        reachExtendSpeedU = Mathf.Max(0f, reachExtendSpeedU);
        reachAimSpeedDeg = Mathf.Max(0f, reachAimSpeedDeg);
        armWidthU = Mathf.Max(0.001f, armWidthU);
    }
}
