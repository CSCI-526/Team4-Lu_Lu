using System;

/// <summary>
/// Polls the analytics dev keys of <see cref="HackShortcutConfig"/> and fires
/// the matching action; the keys work in every build, see
/// <see cref="HackShortcutConfig.IsActive"/>.
/// </summary>
public class AnalyticsHackKeys {
    // Public Methods  #########################################################
    public AnalyticsHackKeys(
        HackShortcutConfig shortcuts,
        Action onRestartSession,
        Action onDownloadReport,
        Action onMockSession
    ) {
        this.shortcuts = shortcuts;
        this.onRestartSession = onRestartSession;
        this.onDownloadReport = onDownloadReport;
        this.onMockSession = onMockSession;
    }

    /// <summary>
    /// call once per frame
    /// </summary>
    public void Poll() {
        if (shortcuts == null) {
            return;
        }

        if (shortcuts.WasPressedThisFrame(HackAction.RestartSession)) {
            onRestartSession();
        }
        if (shortcuts.WasPressedThisFrame(HackAction.DownloadReport)) {
            onDownloadReport();
        }
        if (shortcuts.WasPressedThisFrame(HackAction.MockSession)) {
            onMockSession();
        }
    }

    // private members  ########################################################
    private readonly HackShortcutConfig shortcuts;
    private readonly Action onRestartSession;
    private readonly Action onDownloadReport;
    private readonly Action onMockSession;
}
