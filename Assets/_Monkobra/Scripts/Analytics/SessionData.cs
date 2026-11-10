using System;
using System.Collections.Generic;

/// <summary>
/// all data of one session (one game launch), filled by
/// <see cref="AnalyticsManager"/> and rendered by
/// <see cref="AnalyticsMarkdownWriter"/>
/// </summary>
public class SessionData {
    // Public Members  #########################################################
    public DateTime StartClock;
    // heading text of the session file
    public string Title = string.Empty;
    public string AppVersion = string.Empty;
    public readonly List<RunData> Runs = new List<RunData>();

    // Public Methods  #########################################################
    /// <summary>
    /// death causes of the <see cref="RunStatus.Lost"/> runs, in run order
    /// </summary>
    /// <param name="mode">only the runs of this mode, all runs if null
    /// </param>
    public List<DeathCause> GetLostDeathCauses(RunMode? mode = null) {
        List<DeathCause> causes = new List<DeathCause>();
        for (int i = 0; i < Runs.Count; i++) {
            if (Runs[i].Status == RunStatus.Lost
                && (mode == null || Runs[i].Mode == mode)) {
                causes.Add(Runs[i].Cause);
            }
        }

        return causes;
    }

    /// <returns>if <see cref="RunStatus.Lost"/> runs of both modes
    /// occurred</returns>
    public bool HasLostRunsOfBothModes() {
        return GetLostDeathCauses(RunMode.Full).Count > 0
            && GetLostDeathCauses(RunMode.Endless).Count > 0;
    }
}
