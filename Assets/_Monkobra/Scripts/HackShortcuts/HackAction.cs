/// <summary>
/// a dev/hack action bound to a key by <see cref="HackShortcutConfig"/>
/// </summary>
public enum HackAction {
    /// <summary>toggles <see cref="GameController.CobraNoClip"/></summary>
    NoClip,

    /// <summary>toggles <see cref="StaminaBarController.IsMaxStamina"/>: the
    /// stamina bar stays at max</summary>
    MaxStamina,

    /// <summary>toggles <see cref="MonkeyController.IsSpeedBuff"/>: all monkey
    /// movement runs at 5 times its normal speed</summary>
    SpeedBuff,

    /// <summary>teleports the monkey to the boss zone start, ending the
    /// starting zone if it is still on</summary>
    BossJump,

    /// <summary>spawns a pineapple branch above the monkey</summary>
    SpawnPineapple,

    /// <summary>spawns a branch bearing a coconut above the monkey, to test
    /// the cobra freeze w/o climbing to the coconuts</summary>
    SpawnCoconut,

    /// <summary>hangs a dormant falling cobra above the monkey</summary>
    SpawnFallingCobra,

    /// <summary>sticks a spider web on the trunk above the monkey</summary>
    SpawnSpiderWeb,

    /// <summary>downloads the analytics session report in the browser</summary>
    DownloadReport,

    /// <summary>closes the analytics session and opens a new file</summary>
    RestartSession,

    /// <summary>saves a mock analytics session file, for layout checks
    /// </summary>
    MockSession,

    /// <summary>turns the hack layer on or off in a development build: the
    /// on-screen list of hack keys (<see cref="HackShortcutPanel"/>) and every
    /// other hack key, off when the game starts; never runs elsewhere
    /// </summary>
    ToggleHints,

    /// <summary>teleports the monkey to just below the Spider King's
    /// trigger height, the Cobra King counted as beaten</summary>
    SpiderJump,
}
