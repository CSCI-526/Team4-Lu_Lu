/// <summary>
/// a dev/hack action bound to a key by <see cref="HackShortcutConfig"/>
/// </summary>
public enum HackAction {
    /// <summary>toggles <see cref="GameController.CobraNoClip"/></summary>
    NoClip,

    /// <summary>downloads the analytics session report in the browser</summary>
    DownloadReport,

    /// <summary>closes the analytics session and opens a new file</summary>
    RestartSession,

    /// <summary>saves a mock analytics session file, for layout checks
    /// </summary>
    MockSession,
}
