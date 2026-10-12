/// <summary>What ended a run, passed to <see cref="GameController.LoseGame"/>.
/// </summary>
public enum DeathCause {
    /// <summary>No loss recorded yet, or the caller gave no cause.</summary>
    Unknown,

    /// <summary>The monkey touched the great cobra's body.</summary>
    Cobra,
}
