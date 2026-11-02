/// <summary>Phases of one run, owned by <see cref="GameController"/>.</summary>
public enum GameState {
    /// <summary>Paused on the tutorial screen, any key starts the run.</summary>
    Tutorial,

    /// <summary>
    /// Run is live and the monkey can lose, ends when
    /// <see cref="GameController.EndStartingZone"/> is called, then
    /// becomes <see cref="Playing"/>.
    /// </summary>
    StartingZone,

    /// <summary>
    /// Run in progress, ends only on a loss. A pause
    /// (<see cref="GameController.IsPaused"/>) freezes it without leaving
    /// this state.
    /// </summary>
    Playing,

    /// <summary>Run lost, the lose screen waits for a restart.</summary>
    Lost,
}
