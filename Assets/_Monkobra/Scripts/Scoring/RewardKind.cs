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

    /// <summary>the Cobra King was beaten & its reward taken</summary>
    CobraKingDefeat,

    /// <summary>the Spider King was beaten & its reward taken</summary>
    SpiderKingDefeat,

    /// <summary>a thrown fruit landed on the Cobra King's crown</summary>
    CrownHit,

    /// <summary>a melon or batted slash hurt the Spider King</summary>
    SpiderHit,

    /// <summary>the monkey parried a Spider King tail strike</summary>
    TailParry,

    /// <summary>the monkey dodged a Spider King dash</summary>
    DashDodge,
}
