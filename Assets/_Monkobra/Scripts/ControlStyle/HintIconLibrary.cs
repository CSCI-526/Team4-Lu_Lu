using UnityEngine;

/// <summary>
/// The 2 icon entities every hint shows: a keycap for a keyboard, a finger
/// for touch. A hint holds no icon prefab of its own, only the Animator
/// controller that animates the entity, so the press, double, hold, and rapid
/// hints are 4 controllers of the same entity, see <see cref="HintIconSlot"/>.
/// </summary>
[CreateAssetMenu(
    fileName = "HintIconLibrary",
    menuName = "Scriptable Objects/HintIconLibrary"
)]
public class HintIconLibrary: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>the keycap entity shown while a keyboard is in use</returns>
    public GameObject KeyboardEntity => keyboardEntity;

    /// <returns>the finger entity shown while touch is in use</returns>
    public GameObject TouchEntity => touchEntity;

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("keycap icon; its Animator controller is set by each hint")]
    private GameObject keyboardEntity;

    [SerializeField]
    [Tooltip("finger icon; its Animator controller is set by each hint")]
    private GameObject touchEntity;
}
