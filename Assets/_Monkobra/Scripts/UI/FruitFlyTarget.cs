/// <summary>
/// where a grabbed fruit's sprite flies to, see <see cref="FruitFlyEffect"/>
/// </summary>
public enum FruitFlyTarget {
    EnergyBar, // bottom-left stamina bar, the banana's
    // Todo: an anchor stands in for the top-right counter until it exists
    TopRightCounter, // the coconut's
    Monkey, // the monkey on screen, re-projected each frame; the pineapple's
}
