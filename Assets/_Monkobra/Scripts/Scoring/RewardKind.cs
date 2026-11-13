/// <summary>
/// why <see cref="ScoreManager"/> awarded reward points, so the score popup
/// and the analytics can tell the good deeds apart. Not the same as
/// <see cref="PickupKind"/>, which is only what a fruit is.
/// </summary>
public enum RewardKind {
    /// <summary>a banana grab</summary>
    Banana,

    /// <summary>a coconut grab</summary>
    Coconut,

    /// <summary>a pineapple grab, which starts a leap</summary>
    Pineapple,

    /// <summary>a thrown coconut froze the great cobra</summary>
    CobraFreeze,

    /// <summary>the monkey passed a new height marker</summary>
    HeightMarker,

    /// <summary>the monkey broke free of a spider web</summary>
    WebEscape,
}
