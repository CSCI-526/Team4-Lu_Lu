/// <summary>
/// a dev/hack action bound to a key by <see cref="HackShortcutConfig"/>
/// </summary>
public enum HackAction {
    /// <summary>toggles <see cref="RunController.CobraNoClip"/></summary>
    NoClip,

    /// <summary>toggles <see cref="StaminaBarController.IsMaxStamina"/>: the
    /// stamina bar stays at max</summary>
    MaxStamina,

    /// <summary>toggles <see cref="MonkeyController.IsSpeedBuff"/>: all monkey
    /// movement runs at 5 times its normal speed</summary>
    SpeedBuff,

    // the values below are the ones the config asset stores, so they stay
    // fixed: 3 & 12 belonged to the boss jump keys, removed for the boss dev
    // scenes, never reuse them
    /// <summary>spawns a pineapple branch above the monkey</summary>
    SpawnPineapple = 4,

    /// <summary>spawns a branch bearing a coconut above the monkey, to test
    /// the cobra freeze w/o climbing to the coconuts</summary>
    SpawnCoconut = 5,

    /// <summary>hangs a dormant falling cobra above the monkey</summary>
    SpawnFallingCobra = 6,

    /// <summary>sticks a spider web on the trunk above the monkey</summary>
    SpawnSpiderWeb = 7,

    /// <summary>downloads the analytics session report in the browser</summary>
    DownloadReport = 8,

    /// <summary>closes the analytics session and opens a new file</summary>
    RestartSession = 9,

    /// <summary>saves a mock analytics session file, for layout checks
    /// </summary>
    MockSession = 10,

    /// <summary>turns the hack layer on or off in a development build: the
    /// on-screen list of hack keys (<see cref="HackShortcutPanel"/>) and every
    /// other hack key, off when the game starts; never runs elsewhere
    /// </summary>
    ToggleHints = 11,
}
