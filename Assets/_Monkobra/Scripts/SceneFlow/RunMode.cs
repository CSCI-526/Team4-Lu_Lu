/// <summary>which kind of run the gameplay scene plays</summary>
public enum RunMode {
    /// <summary>the normal run: starting zone, boss fight, win or lose
    /// </summary>
    Full,

    /// <summary>post-boss start, no boss fight, hazards keep ramping</summary>
    Endless,
}

/// <summary>
/// the mode the next gameplay scene load plays, set by
/// <see cref="SceneNames.Load(string, RunMode)"/> and kept across a restart,
/// so a restart replays the same mode
/// </summary>
public static class RunModeSelection {
    // Public Members  #########################################################
    public static RunMode Selected { get; set; } = RunMode.Full;
}
