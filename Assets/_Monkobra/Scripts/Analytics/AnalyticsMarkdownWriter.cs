using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// renders analytics data to markdown, holds no state of its own
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

        var sb = new StringBuilder();
        sb.Append("## Run ").Append(number).Append("\n\n");
        if (run.Status != RunStatus.Lost) {
            sb.Append("- Status: ").Append(run.Status).Append('\n');
        }
        sb.Append("- Final Height: ").Append(run.FinalHeight).Append('\n');
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
        sb.Append("\n\n");

        sb.Append("#### Final Score: ").Append(run.TotalScore).Append("\n\n");
        sb.Append("- Score from Height: ").Append(run.HeightScore);
        sb.Append('\n');
        sb.Append("- Score from Banana: ");
        sb.Append(FormatBananaScore(run.RewardScore, run.Bananas.Count));
        sb.Append("\n\n");

        sb.Append("#### Banana Count: ").Append(run.Bananas.Count);
        sb.Append("\n\n");
        AppendClockList(sb, run.Bananas, run.StartClock, true);

        sb.Append("#### Web Encounter: ").Append(run.Webs.Count);
        sb.Append("\n\n");
        AppendClockList(sb, run.Webs, run.StartClock, false);

        sb.Append("#### Branch Hit: ").Append(run.Branches.Count);
        sb.Append("\n\n");
        AppendClockList(sb, run.Branches, run.StartClock, true);

        List<TimelineEntry> timeline = BuildTimeline(run, end);
        AppendScoreSplit(sb, run.HeightScore, run.RewardScore);
        AppendHeightOverTime(sb, timeline);
        AppendStaminaCurve(sb, run.StaminaSamples);
        AppendRunTimeline(sb, timeline);
        return sb.ToString();
    }

    /// <summary>
    /// whole session file: header, summary of the
    /// <see cref="RunStatus.Lost"/> runs, then every run
    /// </summary>
    public static string BuildSession(SessionData session) {
        var sb = new StringBuilder();
        sb.Append("# MonKobra Analytics: Session: ").Append(session.Title);
        sb.Append("\n\n");
        sb.Append("MonKobra v").Append(session.AppVersion).Append("\n\n");

        List<DeathCause> causes = session.GetLostDeathCauses();
        if (causes.Count > 0) {
            sb.Append("## Session Summary\n\n");
            AppendDeathCauses(sb, causes);
        }
        for (int i = 0; i < session.Runs.Count; i++) {
            sb.Append(BuildRun(session.Runs[i], i + 1));
        }

        return sb.ToString();
    }

    /// <summary>
    /// whether <paramref name="text"/> was written to
    /// <paramref name="path"/>, a failure logs a warning
    /// </summary>
    public static bool WriteFile(string path, string text) {
        try {
            File.WriteAllText(path, text, new UTF8Encoding(false));
        } catch (Exception e) {
            Debug.LogWarning("Analytics:\tfail to write session: " + e);
            return false;
        }

        return true;
    }

    // constants  ##############################################################
    private const string CLOCK_FORMAT = "HH:mm:ss";

    // nested types  ###########################################################
    // one point on the run's timeline, ordered by seconds since run start
    private readonly struct TimelineEntry {
        public readonly double Elapsed;
        public readonly string Label;
        // height above the run start y; world u
        public readonly float Height;
        public readonly bool HasHeight;

        public TimelineEntry(
            double elapsed,
            string label,
            float height,
            bool hasHeight
        ) {
            Elapsed = elapsed;
            Label = label;
            Height = height;
            HasHeight = hasHeight;
        }
    }

    // private methods  ########################################################
    // pie of what ended each lost run of the session
    private static void AppendDeathCauses(
        StringBuilder sb,
        List<DeathCause> causes
    ) {
        sb.Append("#### Death Causes\n\n```mermaid\npie showData\n");
        sb.Append("    title Death Causes\n");
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

    private static string FormatClock(DateTime clock) {
        return clock.ToString(CLOCK_FORMAT, CultureInfo.InvariantCulture);
    }

    private static string FormatSeconds(double seconds) {
        return seconds.ToString("F1", CultureInfo.InvariantCulture);
    }

    private static string FormatHeight(float height) {
        return height.ToString("+0.0;-0.0;+0.0", CultureInfo.InvariantCulture);
    }

    // "unit * count = total" when every banana paid the same, else total only
    private static string FormatBananaScore(long total, int count) {
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
        for (int i = 0; i < stamps.Count; i++) {
            TimeSpan elapsed = stamps[i].Clock - start;
            sb.Append(i + 1).Append(". +");
            sb.Append(elapsed.ToString(
                @"hh\:mm\:ss",
                CultureInfo.InvariantCulture
            ));
            if (hasHeight) {
                sb.Append("    y=");
                sb.Append(stamps[i].Height.ToString(
                    "+0.0;-0.0;+0.0",
                    CultureInfo.InvariantCulture
                ));
            }
            sb.Append('\n');
        }
        if (stamps.Count > 0) {
            sb.Append('\n');
        }
    }

    // line of sampled stamina over the run w/ a bar at each banana pickup,
    // skipped w/o 2 samples
    private static void AppendStaminaCurve(
        StringBuilder sb,
        List<StaminaSample> samples
    ) {
        if (samples.Count < 2) {
            return;
        }

        StaminaBarController bar = StaminaBarController.I;
        int maxStamina = Mathf.Max(
            1,
            Mathf.CeilToInt(bar != null ? bar.MaxStamina : 0f)
        );
        var times = new List<string>();
        var values = new List<string>();
        var markers = new List<string>();
        for (int i = 0; i < samples.Count; i++) {
            times.Add(FormatSeconds(samples[i].Elapsed));
            values.Add(samples[i].Stamina.ToString(
                "F1",
                CultureInfo.InvariantCulture
            ));
            markers.Add(
                samples[i].IsBanana
                    ? maxStamina.ToString(CultureInfo.InvariantCulture)
                    : "0"
            );
        }

        sb.Append("#### Stamina Curve\n\nBars mark banana pickups.\n\n");
        sb.Append("```mermaid\nxychart-beta\n");
        sb.Append("    title \"Stamina Curve\"\n");
        sb.Append("    x-axis \"Elapsed (s)\" [")
            .Append(string.Join(", ", times)).Append("]\n");
        sb.Append("    y-axis \"Stamina\" 0 --> ").Append(maxStamina);
        sb.Append('\n');
        sb.Append("    bar [").Append(string.Join(", ", markers));
        sb.Append("]\n");
        sb.Append("    line [").Append(string.Join(", ", values));
        sb.Append("]\n```\n\n");
    }

    // every event of the run, plus start & its end, sorted by time
    private static List<TimelineEntry> BuildTimeline(
        RunData run,
        EventStamp end
    ) {
        string endLabel;
        switch (run.Status) {
            case RunStatus.Lost:
                endLabel = "Game Over";
                break;
            case RunStatus.Interrupted:
                endLabel = "Interrupted";
                break;
            default:
                endLabel = "Now";
                break;
        }

        var entries = new List<TimelineEntry> {
            new TimelineEntry(0.0, "Start", 0f, true)
        };
        AddTimelineEntries(
            entries,
            run.Bananas,
            run.StartClock,
            "Banana",
            true
        );
        AddTimelineEntries(entries, run.Webs, run.StartClock, "Web", false);
        AddTimelineEntries(
            entries,
            run.Branches,
            run.StartClock,
            "Branch",
            true
        );
        entries.Add(new TimelineEntry(
            (end.Clock - run.StartClock).TotalSeconds,
            endLabel,
            end.Height,
            true
        ));
        entries.Sort((a, b) => a.Elapsed.CompareTo(b.Elapsed));
        return entries;
    }

    private static void AddTimelineEntries(
        List<TimelineEntry> entries,
        List<EventStamp> stamps,
        DateTime start,
        string name,
        bool hasHeight
    ) {
        for (int i = 0; i < stamps.Count; i++) {
            entries.Add(new TimelineEntry(
                (stamps[i].Clock - start).TotalSeconds,
                name + " " + (i + 1),
                stamps[i].Height,
                hasHeight
            ));
        }
    }

    // pie of height score against banana score, skipped w/o any score
    private static void AppendScoreSplit(
        StringBuilder sb,
        long heightScore,
        long rewardScore
    ) {
        if (heightScore <= 0 && rewardScore <= 0) {
            return;
        }

        sb.Append("#### Score Split\n\n```mermaid\npie showData\n");
        sb.Append("    title Score Split\n");
        if (heightScore > 0) {
            sb.Append("    \"Height\" : ").Append(heightScore).Append('\n');
        }
        if (rewardScore > 0) {
            sb.Append("    \"Banana\" : ").Append(rewardScore).Append('\n');
        }
        sb.Append("```\n\n");
    }

    // line of height at each event that carries one, x axis is categorical so
    // spacing is per event, not per second
    private static void AppendHeightOverTime(
        StringBuilder sb,
        List<TimelineEntry> entries
    ) {
        var times = new List<string>();
        var heights = new List<string>();
        float minHeight = 0f;
        float maxHeight = 1f;
        for (int i = 0; i < entries.Count; i++) {
            if (!entries[i].HasHeight) {
                continue;
            }

            times.Add(FormatSeconds(entries[i].Elapsed));
            heights.Add(entries[i].Height.ToString(
                "F1",
                CultureInfo.InvariantCulture
            ));
            minHeight = Mathf.Min(minHeight, entries[i].Height);
            maxHeight = Mathf.Max(maxHeight, entries[i].Height);
        }

        sb.Append("#### Height Over Time\n\n```mermaid\nxychart-beta\n");
        sb.Append("    title \"Height Over Time\"\n");
        sb.Append("    x-axis \"Elapsed (s)\" [")
            .Append(string.Join(", ", times)).Append("]\n");
        sb.Append("    y-axis \"Height (u)\" ")
            .Append(Mathf.FloorToInt(minHeight)).Append(" --> ")
            .Append(Mathf.CeilToInt(maxHeight)).Append('\n');
        sb.Append("    line [").Append(string.Join(", ", heights));
        sb.Append("]\n```\n\n");
    }

    private static void AppendRunTimeline(
        StringBuilder sb,
        List<TimelineEntry> entries
    ) {
        sb.Append("#### Run Timeline\n\n```mermaid\ntimeline\n");
        sb.Append("    title Run Timeline\n");
        for (int i = 0; i < entries.Count; i++) {
            sb.Append("    +").Append(FormatSeconds(entries[i].Elapsed));
            sb.Append(" s : ").Append(entries[i].Label);
            if (entries[i].HasHeight) {
                sb.Append(", y ").Append(FormatHeight(entries[i].Height));
            }
            sb.Append('\n');
        }
        sb.Append("```\n\n");
    }
}
