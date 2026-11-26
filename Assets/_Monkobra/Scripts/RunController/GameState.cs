/// <summary>Phases of one run, owned by <see cref="RunController"/>.</summary>
public enum GameState {
    /// <summary>Paused on the tutorial screen, Space starts the run.</summary>
    Tutorial,

    /// <summary>
    /// Run is live and the monkey can lose, ends when
    /// <see cref="RunController.EndStartingZone"/> is called, then
    /// becomes <see cref="Playing"/>.
    /// </summary>
    StartingZone,

    /// <summary>
    /// Run in progress, ends only on a loss. A pause
    /// (<see cref="RunController.IsPaused"/>) freezes it without leaving
    /// this state.
    /// </summary>
    Playing,

    /// <summary>Run lost, the lose screen waits for a restart.</summary>
    Lost,

    /// <summary>
    /// Run won by defeating the Cobra King; the boss fight's victory panel
    /// waits for a restart.
    /// </summary>
    Won,
}
