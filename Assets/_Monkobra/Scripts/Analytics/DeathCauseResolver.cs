using System;
using System.Collections.Generic;

/// <summary>
/// infers a <see cref="DeathCause"/> from the events a run logged b4 its loss
/// </summary>
public static class DeathCauseResolver {
    // Public Methods  #########################################################
    /// <param name="run">run that was lost</param>
    /// <param name="now">moment of the loss</param>
    /// <param name="windowS">seconds before the loss in which an event counts
    /// </param>
    /// <returns>the cause of the latest branch hit or web encounter w/i the
    /// window, the great cobra if there is none</returns>
    public static DeathCause Resolve(
        RunData run,
        DateTime now,
        float windowS
    ) {
        DateTime branch = GetLatestInWindow(run.Branches, now, windowS);
        DateTime web = GetLatestInWindow(run.Webs, now, windowS);
        if (branch == DateTime.MinValue && web == DateTime.MinValue) {
            return DeathCause.GreatCobra;
        }

        return branch >= web ? DeathCause.Branch : DeathCause.Web;
    }

    /// <returns>label shown in the session file</returns>
    public static string GetDisplayName(DeathCause cause) {
        return cause == DeathCause.GreatCobra
            ? "Great Cobra"
            : cause.ToString();
    }

    // private methods  ########################################################
    // clock of the latest stamp w/i the window, MinValue w/o one
    private static DateTime GetLatestInWindow(
        List<EventStamp> stamps,
        DateTime now,
        float windowS
    ) {
        DateTime latest = DateTime.MinValue;
        for (int i = 0; i < stamps.Count; i++) {
            DateTime clock = stamps[i].Clock;
            if ((now - clock).TotalSeconds <= windowS && clock > latest) {
                latest = clock;
            }
        }

        return latest;
    }
}
