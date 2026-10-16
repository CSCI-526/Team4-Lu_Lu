/// <summary>
/// what ended a run, inferred by <see cref="DeathCauseResolver"/>; the player
/// always dies to the great cobra, so the others name what hit it just before
/// </summary>
public enum DeathCause {
    /// <summary>no loss recorded yet</summary>
    Unknown,

    /// <summary>no branch hit or web encounter w/i the window b4 the loss
    /// </summary>
    GreatCobra,

    /// <summary>a branch hit w/i the window b4 the loss</summary>
    Branch,

    /// <summary>a web encounter w/i the window b4 the loss</summary>
    Web,
}
