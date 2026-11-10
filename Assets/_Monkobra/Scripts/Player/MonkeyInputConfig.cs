using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The input actions every monkey consumer reads: the directional move
/// action and the interact action. Arm tuning is in
/// <see cref="MonkeyArmConfig"/>, climb and orbit tuning in
/// <see cref="MonkeyClimbConfig"/>, balance numbers in
/// <see cref="PlayerBalanceConfig"/>. Consumed by
/// <see cref="ArmController"/>, <see cref="MonkeyController"/>,
/// <see cref="SpiderWebStruggle"/>.
/// </summary>
[CreateAssetMenu(
    fileName = "MonkeyInputConfig",
    menuName = "Scriptable Objects/MonkeyInputConfig"
)]
public class MonkeyInputConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>directional input action every consumer reads, a Vector2
    /// where x orbits and y climbs</returns>
    public InputActionReference MoveAction => moveAction;

    /// <returns>action held to reach for fruit, also read by the camera rig
    /// </returns>
    public InputActionReference InteractAction => interactAction;

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip(
        "directional input action shared by the body and both arms, a "
            + "Vector2 where x orbits the trunk and y climbs"
    )]
    private InputActionReference moveAction;

    [SerializeField]
    [Tooltip(
        "action held to reach an arm out for fruit, the same action "
            + "CameraClimbFraming polls"
    )]
    private InputActionReference interactAction;
}
