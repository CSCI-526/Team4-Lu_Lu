/// <summary>
/// where a grabbed fruit's sprite flies to, see <see cref="FruitFlyEffect"/>
/// </summary>
public enum FruitFlyTarget {
    EnergyBar, // bottom-left stamina bar, the banana's
    TopRightCounter, // top-right coconut HUD icon, the coconut's
    Monkey, // the monkey on screen, re-projected each frame; the pineapple's
}
