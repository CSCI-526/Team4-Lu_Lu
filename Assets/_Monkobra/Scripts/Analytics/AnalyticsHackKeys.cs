using System;
using UnityEngine.InputSystem;

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
        if (shortcuts == null || !HackShortcutConfig.IsActive) {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) {
            return;
        }

        if (IsPressed(keyboard, HackAction.RestartSession)) {
            onRestartSession();
        }
        if (IsPressed(keyboard, HackAction.DownloadReport)) {
            onDownloadReport();
        }
        if (IsPressed(keyboard, HackAction.MockSession)) {
            onMockSession();
        }
    }

    // private members  ########################################################
    private readonly HackShortcutConfig shortcuts;
    private readonly Action onRestartSession;
    private readonly Action onDownloadReport;
    private readonly Action onMockSession;

    // private methods  ########################################################
    private bool IsPressed(Keyboard keyboard, HackAction action) {
        Key key = shortcuts.GetKey(action);
        return key != Key.None && keyboard[key].wasPressedThisFrame;
    }
}
