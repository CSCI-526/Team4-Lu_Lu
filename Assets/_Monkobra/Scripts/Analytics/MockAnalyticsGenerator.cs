using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Builds a fully mocked session and saves it as its own markdown file, so
/// the file layout can be checked w/o playing. The live session is untouched.
/// </summary>
public static class MockAnalyticsGenerator {
    // Public Methods  #########################################################
    /// <param name="folder">analytics folder to save into</param>
    /// <param name="now">moment the session is stamped w/ and the last run
    /// ends at</param>
    /// <returns>path of the file written, empty if the write failed</returns>
    public static string SaveMockSession(string folder, DateTime now) {
        string stamp = now.ToString(
            "yyyy-MM-dd_HH_mm_ss",
            CultureInfo.InvariantCulture
        );
        string path = Path.Combine(folder, "0" + stamp + "-mock.md");
        try {
            Directory.CreateDirectory(folder);
        } catch (Exception e) {
            Debug.LogWarning("Analytics:\tfail to open mock folder: " + e);
            return string.Empty;
        }

        SessionData session = BuildMockSession(now, new System.Random());
        bool isWritten = AnalyticsMarkdownWriter.WriteFile(
            path,
            AnalyticsMarkdownWriter.BuildSession(session)
        );

        return isWritten ? path : string.Empty;
    }

    /// <returns>session of <see cref="RUN_COUNT"/> runs, all lost but the
    /// last, which is interrupted; the final run ends at
    /// <paramref name="now"/></returns>
    public static SessionData BuildMockSession(
        DateTime now,
        System.Random rng
    ) {
        // runs laid back to back, so the session starts this long b4 now
        var durations = new List<float>();
        float total = 0f;
        for (int i = 0; i < RUN_COUNT; i++) {
            float duration = Range(rng, 20f, 90f);
            durations.Add(duration);
            total += duration + RUN_GAP_S;
        }

        DateTime sessionStart = now.AddSeconds(-total);
        var session = new SessionData {
            StartClock = sessionStart,
            Title = sessionStart.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            ) + " (mock)",
            AppVersion = Application.version
        };

        DateTime cursor = sessionStart;
        for (int i = 0; i < RUN_COUNT; i++) {
            bool isLast = i == RUN_COUNT - 1;
            session.Runs.Add(BuildMockRun(
                cursor,
                durations[i],
                isLast ? RunStatus.Interrupted : RunStatus.Lost,
                rng
            ));
            cursor = cursor.AddSeconds(durations[i] + RUN_GAP_S);
        }

        return session;
    }

    // constants  ##############################################################
    private const int RUN_COUNT = 5;
    private const float RUN_GAP_S = 5f;
    // world u climbed per second
    private const float CLIMB_RATE = 0.8f;
    private const float FALLBACK_MAX_STAMINA = 100f;

    // private methods  ########################################################
    private static RunData BuildMockRun(
        DateTime start,
        float durationS,
        RunStatus status,
        System.Random rng
    ) {
        float height = durationS * CLIMB_RATE;
        var run = new RunData {
            Status = status,
            StartClock = start,
            DurationS = durationS,
            FinalHeight = Mathf.FloorToInt(height)
        };
        run.End = new EventStamp(start.AddSeconds(durationS), height);

        AddEvents(run.Bananas, start, durationS, rng.Next(2, 7), rng);
        AddEvents(run.Webs, start, durationS, rng.Next(0, 3), rng);
        AddEvents(run.Branches, start, durationS, rng.Next(1, 5), rng);
        AddStaminaSamples(run, rng);

        if (status == RunStatus.Lost) {
            // the death cause comes from the same rule as a real run
            run.Cause = DeathCauseResolver.Resolve(run, run.End.Clock, 2f);
        }

        run.HeightScore = run.FinalHeight * 10L;
        run.RewardScore = run.Bananas.Count * 50L;
        run.TotalScore = run.HeightScore + run.RewardScore;

        return run;
    }

    // random stamps spread over the run, ordered by time
    private static void AddEvents(
        List<EventStamp> into,
        DateTime start,
        float durationS,
        int count,
        System.Random rng
    ) {
        var offsets = new List<float>();
        for (int i = 0; i < count; i++) {
            offsets.Add(Range(rng, 1f, durationS));
        }
        offsets.Sort();

        foreach (float offset in offsets) {
            into.Add(new EventStamp(
                start.AddSeconds(offset),
                offset * CLIMB_RATE
            ));
        }
    }

    // draining curve w/ a refill on each banana
    private static void AddStaminaSamples(RunData run, System.Random rng) {
        StaminaBarController bar = StaminaBarController.I;
        float max = bar != null ? bar.MaxStamina : FALLBACK_MAX_STAMINA;
        float stamina = max;
        float drainPerS = max / 40f;
        int nextBanana = 0;

        for (float t = 0f; t <= run.DurationS; t += 2f) {
            stamina = Mathf.Max(0f, stamina - drainPerS * 2f);
            bool isBanana = false;
            DateTime clock = run.StartClock.AddSeconds(t);
            while (
                nextBanana < run.Bananas.Count
                && run.Bananas[nextBanana].Clock <= clock
            ) {
                stamina = Mathf.Min(max, stamina + max * 0.25f);
                isBanana = true;
                nextBanana++;
            }
            run.StaminaSamples.Add(new StaminaSample(t, stamina, isBanana));
        }
    }

    private static float Range(System.Random rng, float min, float max) {
        return min + (float)rng.NextDouble() * (max - min);
    }
}
