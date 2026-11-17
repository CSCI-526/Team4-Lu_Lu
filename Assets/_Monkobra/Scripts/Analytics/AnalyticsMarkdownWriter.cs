using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// The data visualization half of analytics: renders <see cref="SessionData"/>
/// to markdown text, tables and Mermaid charts. Reads only the data it is
/// given, holds no state, and touches no file or scene; the collection half is
/// <see cref="AnalyticsManager"/>, the file half is
/// <see cref="AnalyticsSessionFile"/>.
/// </summary>
public static class AnalyticsMarkdownWriter {
    // Public Methods  #########################################################
    /// <summary>
    /// one run entry, <paramref name="number"/> is its 1-based place in the
    /// session
    /// </summary>
    public static string BuildRun(RunData run, int number) {
        // a run still open ends at this very moment
        EventStamp end = run.Status == RunStatus.InProgress
            ? new EventStamp(DateTime.Now, run.End.Height)
            : run.End;
        float duration = run.Status == RunStatus.InProgress
            ? (float)(end.Clock - run.StartClock).TotalSeconds
            : run.DurationS;

        StringBuilder sb = new StringBuilder();
        sb.Append("## Run ").Append(number).Append("\n\n");
        sb.Append("- Mode: ").Append(run.Mode).Append('\n');
        if (run.Status != RunStatus.Lost) {
            sb.Append("- Status: ").Append(run.Status).Append('\n');
        }
        sb.Append("- Highest Height: ").Append(FormatHeight(run.HighestHeight));
        sb.Append('\n');
        if (run.Status == RunStatus.Lost) {
            sb.Append("- Death Height: ").Append(FormatHeight(end.Height));
            sb.Append('\n');
            sb.Append("- Death Cause: ").Append(
                DeathCauseResolver.GetDisplayName(run.Cause)
            ).Append('\n');
        }
        sb.Append('\n');

        sb.Append("#### Run Duration: ");
        sb.Append(duration.ToString("F1", CultureInfo.InvariantCulture));
        sb.Append(" s\n\n");
        sb.Append("- Time Start: ").Append(FormatClock(run.StartClock));
        sb.Append('\n');
        sb.Append("- Time Stop: ").Append(FormatClock(end.Clock));
        sb.Append('\n');
        if (run.HasZoneExit) {
            sb.Append("- Starting Zone Exit: +");
            sb.Append((run.ZoneExit.Clock - run.StartClock).ToString(
                @"hh\:mm\:ss",
                CultureInfo.InvariantCulture
            ));
            sb.Append(", y ").Append(FormatHeight(run.ZoneExit.Height));
            sb.Append('\n');
        }
        sb.Append('\n');
        AppendHeightCurve(sb, run.Samples);

        sb.Append("#### Final Score: ").Append(run.TotalScore).Append("\n\n");
        sb.Append("- Score from Height: ").Append(run.HeightScore);
        sb.Append('\n');
        // the reward score is split by kind: banana and coconut always show,
        // the other kinds only once they scored
        foreach (RewardKind kind in Enum.GetValues(typeof(RewardKind))) {
            long kindScore = run.RewardScoreByKind[(int)kind];
            bool isFruit = kind == RewardKind.Banana
                || kind == RewardKind.Coconut;
            if (kindScore <= 0 && !isFruit) {
                continue;
            }
            AnalyticsMetric? counter = RewardCountMetric(kind);
            sb.Append("- Score from ").Append(RewardLabel(kind)).Append(": ");
            sb.Append(FormatPickupScore(
                kindScore,
                counter.HasValue ? run.CountOf(counter.Value) : 0
            ));
            sb.Append('\n');
        }
        sb.Append('\n');
        AppendScoreSplit(sb, run.HeightScore, run.RewardScoreByKind);

        // every metric gets one section, a new metric needs no code here
        foreach (AnalyticsMetric metric in AnalyticsMetrics.All) {
            List<EventStamp> stamps = run.GetEvents(metric);
            sb.Append("#### ").Append(AnalyticsMetrics.GetTitle(metric));
            sb.Append(": ").Append(stamps.Count).Append("\n\n");
            AppendClockList(
                sb,
                stamps,
                run.StartClock,
                AnalyticsMetrics.HasHeight(metric)
            );
            // stamina is refilled by bananas, so its curve sits under them
            if (metric == AnalyticsMetric.Banana) {
                AppendStaminaCurve(sb, run.Samples, run.MaxStamina);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// whole session file: header, summary of the
    /// <see cref="RunStatus.Lost"/> runs, then every run
    /// </summary>
    public static string BuildSession(SessionData session) {
        StringBuilder sb = new StringBuilder();
        sb.Append("# MonKobra Analytics: Session: ").Append(session.Title);
        sb.Append("\n\n");
        sb.Append("MonKobra v").Append(session.AppVersion).Append("\n\n");

        List<DeathCause> causes = session.GetLostDeathCauses();
        if (causes.Count > 0) {
            sb.Append("## Session Summary\n\n");
            if (session.HasLostRunsOfBothModes()) {
                // one summary per mode, as their difficulty differs
                foreach (RunMode mode in Enum.GetValues(typeof(RunMode))) {
                    List<DeathCause> modeCauses =
                        session.GetLostDeathCauses(mode);
                    if (modeCauses.Count == 0) {
                        continue;
                    }
                    sb.Append("### ").Append(mode).Append(" Runs\n\n");
                    AppendDeathCauses(sb, modeCauses, " (" + mode + ")");
                    AppendLostRunBars(sb, session, mode, " (" + mode + ")");
                }
            } else {
                AppendDeathCauses(sb, causes, string.Empty);
                AppendLostRunBars(sb, session, null, string.Empty);
            }
        }
        for (int i = 0; i < session.Runs.Count; i++) {
            sb.Append(BuildRun(session.Runs[i], i + 1));
        }

        return sb.ToString();
    }

    // constants  ##############################################################
    private const string CLOCK_FORMAT = "HH:mm:ss";

    // private methods  ########################################################
    // pie of what ended each lost run of the session
    private static void AppendDeathCauses(
        StringBuilder sb,
        List<DeathCause> causes,
        string titleSuffix
    ) {
        sb.Append("#### Death Causes").Append(titleSuffix);
        sb.Append("\n\n```mermaid\npie showData\n");
        sb.Append("    title Death Causes").Append(titleSuffix).Append('\n');
        foreach (DeathCause cause in Enum.GetValues(typeof(DeathCause))) {
            int count = causes.FindAll(c => c == cause).Count;
            if (count > 0) {
                sb.Append("    \"");
                sb.Append(DeathCauseResolver.GetDisplayName(cause));
                sb.Append("\" : ");
                sb.Append(count).Append('\n');
            }
        }
        sb.Append("```\n\n");
    }

    // one bar chart per measure over the lost runs, x is the run number
    // mode null charts every lost run, else only that mode's
    private static void AppendLostRunBars(
        StringBuilder sb,
        SessionData session,
        RunMode? mode,
        string titleSuffix
    ) {
        List<string> labels = new List<string>();
        List<string> scores = new List<string>();
        List<string> highests = new List<string>();
        List<string> heights = new List<string>();
        List<string> durations = new List<string>();
        for (int i = 0; i < session.Runs.Count; i++) {
            RunData run = session.Runs[i];
            if (run.Status != RunStatus.Lost
                || (mode != null && run.Mode != mode)) {
                continue;
            }

            labels.Add("\"" + (i + 1) + "\"");
            scores.Add(run.TotalScore.ToString(CultureInfo.InvariantCulture));
            highests.Add(FormatNumber(run.HighestHeight));
            heights.Add(FormatNumber(run.End.Height));
            durations.Add(FormatNumber(run.DurationS));
        }

        AppendRunBars(
            sb, "Final Score vs Run" + titleSuffix, "Score", labels, scores
        );
        AppendRunBars(
            sb, "Highest Height vs Run" + titleSuffix, "Height (u)", labels,
            highests
        );
        AppendRunBars(
            sb, "Death Height vs Run" + titleSuffix, "Height (u)", labels,
            heights
        );
        AppendRunBars(
            sb, "Duration vs Run" + titleSuffix, "Duration (s)", labels,
            durations
        );
    }

    private static void AppendRunBars(
        StringBuilder sb,
        string title,
        string yLabel,
        List<string> labels,
        List<string> values
    ) {
        sb.Append("#### ").Append(title).Append("\n\n```mermaid\n");
        sb.Append("xychart-beta\n    title \"").Append(title).Append("\"\n");
        sb.Append("    x-axis \"Run\" [")
            .Append(string.Join(", ", labels)).Append("]\n");
        sb.Append("    y-axis \"").Append(yLabel).Append("\"\n");
        sb.Append("    bar [").Append(string.Join(", ", values));
        sb.Append("]\n```\n\n");
    }

    private static string FormatNumber(float value) {
        return value.ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static string FormatClock(DateTime clock) {
        return clock.ToString(CLOCK_FORMAT, CultureInfo.InvariantCulture);
    }

    private static string FormatSeconds(double seconds) {
        return seconds.ToString("F1", CultureInfo.InvariantCulture);
    }

    private static string FormatHeight(float height) {
        return height.ToString("+0.0;-0.0;+0.0", CultureInfo.InvariantCulture);
    }

    // "unit * count = total" when every pickup of the kind paid the same, else
    // total only
    private static string FormatPickupScore(long total, int count) {
        if (count > 0 && total % count == 0) {
            return (total / count) + " * " + count + " = " + total;
        }

        return total.ToString(CultureInfo.InvariantCulture);
    }

    // each stamp as time elapsed since the run started, then its height
    // iff hasHeight
    private static void AppendClockList(
        StringBuilder sb,
        List<EventStamp> stamps,
        DateTime start,
        bool hasHeight
    ) {
        if (stamps.Count == 0) {
            return;
        }

        sb.Append(hasHeight ? "| # | Time | y |\n" : "| # | Time |\n");
        sb.Append(hasHeight ? "|---|---|---|\n" : "|---|---|\n");
        for (int i = 0; i < stamps.Count; i++) {
            TimeSpan elapsed = stamps[i].Clock - start;
            sb.Append("| ").Append(i + 1).Append(" | +");
            sb.Append(elapsed.ToString(
                @"hh\:mm\:ss",
                CultureInfo.InvariantCulture
            ));
            sb.Append(" |");
            if (hasHeight) {
                sb.Append(' ').Append(stamps[i].Height.ToString(
                    "0.0",
                    CultureInfo.InvariantCulture
                )).Append(" |");
            }
            sb.Append('\n');
        }
        sb.Append('\n');
    }

    // line of sampled stamina over the run, skipped w/o 2 samples
    private static void AppendStaminaCurve(
        StringBuilder sb,
        List<RunSample> samples,
        float staminaCap
    ) {
        if (samples.Count < 2) {
            return;
        }

        int maxStamina = Mathf.Max(1, Mathf.CeilToInt(staminaCap));
        List<string> times = new List<string>();
        List<string> values = new List<string>();
        for (int i = 0; i < samples.Count; i++) {
            times.Add(FormatSeconds(samples[i].Elapsed));
            values.Add(samples[i].Stamina.ToString(
                "F1",
                CultureInfo.InvariantCulture
            ));
        }

        sb.Append("```mermaid\nxychart-beta\n");
        sb.Append("    title \"Stamina vs Time\"\n");
        sb.Append("    x-axis \"Elapsed (s)\" [")
            .Append(string.Join(", ", times)).Append("]\n");
        sb.Append("    y-axis \"Stamina\" 0 --> ").Append(maxStamina);
        sb.Append('\n');
        sb.Append("    line [").Append(string.Join(", ", values));
        sb.Append("]\n```\n\n");
    }

    // pie of height score against banana & coconut score, skipped w/o any
    // score
    private static void AppendScoreSplit(
        StringBuilder sb,
        long heightScore,
        long[] rewardScores
    ) {
        long total = heightScore;
        foreach (long score in rewardScores) {
            total += score;
        }
        if (total <= 0) {
            return;
        }

        sb.Append("```mermaid\npie showData\n");
        sb.Append("    title Score Split\n");
        if (heightScore > 0) {
            sb.Append("    \"Height\" : ").Append(heightScore).Append('\n');
        }
        foreach (RewardKind kind in Enum.GetValues(typeof(RewardKind))) {
            long score = rewardScores[(int)kind];
            if (score > 0) {
                sb.Append("    \"").Append(RewardLabel(kind)).Append("\" : ");
                sb.Append(score).Append('\n');
            }
        }
        sb.Append("```\n\n");
    }

    // how the report words a reward kind
    private static string RewardLabel(RewardKind kind) {
        switch (kind) {
            case RewardKind.CobraFreeze:
                return "Cobra Freeze";
            case RewardKind.HeightMarker:
                return "Height Marker";
            case RewardKind.WebEscape:
                return "Web Escape";
            default:
                return kind.ToString();
        }
    }

    // the metric whose count equals how often a kind scored, none when no
    // metric matches (a cobra freeze also counts the boss armor's freezes)
    private static AnalyticsMetric? RewardCountMetric(RewardKind kind) {
        switch (kind) {
            case RewardKind.Banana:
                return AnalyticsMetric.Banana;
            case RewardKind.Coconut:
                return AnalyticsMetric.Coconut;
            case RewardKind.Pineapple:
                return AnalyticsMetric.PineappleLeap;
            case RewardKind.HeightMarker:
                return AnalyticsMetric.HeightMarker;
            case RewardKind.WebEscape:
                return AnalyticsMetric.WebEscape;
            default:
                return null;
        }
    }

    // line of sampled height over the run, skipped w/o 2 samples; the x axis
    // is categorical, so even spacing relies on the regular sample interval
    private static void AppendHeightCurve(
        StringBuilder sb,
        List<RunSample> samples
    ) {
        if (samples.Count < 2) {
            return;
        }

        List<string> times = new List<string>();
        List<string> heights = new List<string>();
        float minHeight = 0f;
        float maxHeight = 1f;
        for (int i = 0; i < samples.Count; i++) {
            times.Add(FormatSeconds(samples[i].Elapsed));
            heights.Add(samples[i].Height.ToString(
                "F1",
                CultureInfo.InvariantCulture
            ));
            minHeight = Mathf.Min(minHeight, samples[i].Height);
            maxHeight = Mathf.Max(maxHeight, samples[i].Height);
        }

        sb.Append("```mermaid\nxychart-beta\n");
        sb.Append("    title \"Height vs Time\"\n");
        sb.Append("    x-axis \"Elapsed (s)\" [")
            .Append(string.Join(", ", times)).Append("]\n");
        sb.Append("    y-axis \"Height (u)\" ")
            .Append(Mathf.FloorToInt(minHeight)).Append(" --> ")
            .Append(Mathf.CeilToInt(maxHeight)).Append('\n');
        sb.Append("    line [").Append(string.Join(", ", heights));
        sb.Append("]\n```\n\n");
    }
}
