using UnityEngine;

/// <summary>
/// on a fruit: where its sprite flies when an arm grabs it, and which sprite;
/// the coconut flies to the top-right counter, the rubber fruit to the
/// monkey. A fruit without one, the banana, flies the default sprite to the
/// energy bar, see
/// <see cref="FruitFlyEffect"/>
/// </summary>
[DisallowMultipleComponent]
public class FruitFlyOnGrab: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>where the grabbed fruit's sprite flies to</returns>
    public FruitFlyTarget Target => target;

    /// <returns>sprite to fly, null for the effect's default</returns>
    public Sprite Sprite => sprite;

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("where the grabbed fruit's sprite flies to")]
    private FruitFlyTarget target = FruitFlyTarget.EnergyBar;

    [SerializeField]
    [Tooltip("sprite to fly, empty for the effect's default")]
    private Sprite sprite;
}
