/// <summary>
/// what a <see cref="ScorePickup"/> is, so <see cref="ScoreManager"/> scores
/// and counts each kind on its own
/// </summary>
public enum PickupKind {
    /// <summary>
    /// the common fruit: <see cref="PlayerBalanceConfig.ScoreRewardPoints"/>
    /// and a stamina restore
    /// </summary>
    Banana,

    /// <summary>
    /// rare, higher up: <see cref="PlayerBalanceConfig.CoconutRewardPoints"/>,
    /// no stamina, kept to freeze the great cobra
    /// </summary>
    Coconut,
}
