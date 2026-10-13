using System;
using System.Collections.Generic;

/// <summary>
/// how a run ended, or that it has not yet
/// </summary>
public enum RunStatus {
    InProgress,
    Lost,
    // cut short w/o a game over, e.g. Play mode stopped
    Interrupted
}

/// <summary>
/// snapshot of the moment and the height climbed above the run start
/// </summary>
public readonly struct EventStamp {
    public readonly DateTime Clock;
    // height above the run start y; world u
    public readonly float Height;

    public EventStamp(DateTime clock, float height) {
        Clock = clock;
        Height = height;
    }
}

/// <summary>
/// one timed point of the height vs time & stamina vs time charts
/// </summary>
public readonly struct RunSample {
    // seconds since the run started
    public readonly float Elapsed;
    // height above the run start y; world u
    public readonly float Height;
    public readonly float Stamina;

    public RunSample(float elapsed, float height, float stamina) {
        Elapsed = elapsed;
        Height = height;
        Stamina = stamina;
    }
}

/// <summary>
/// all data of one run, filled by <see cref="AnalyticsManager"/> and rendered
/// by <see cref="AnalyticsMarkdownWriter"/>
/// </summary>
public class RunData {
    // Public Members  #########################################################
    public RunStatus Status = RunStatus.InProgress;

    // <see cref="UnityEngine.Time.time"/> at run start, base of elapsed secs
    public float StartTime;
    public DateTime StartClock;
    public readonly List<EventStamp> Bananas = new List<EventStamp>();
    public readonly List<EventStamp> Webs = new List<EventStamp>();
    public readonly List<EventStamp> Branches = new List<EventStamp>();
    public readonly List<RunSample> Samples = new List<RunSample>();

    // set once the run leaves InProgress  --------------------------------------
    public EventStamp End;
    public DeathCause Cause = DeathCause.Unknown;
    // highest point reached, as opposed to where the run ended (End.Height);
    // world u
    public float HighestHeight;
    public long TotalScore;
    public long HeightScore;
    public long RewardScore;
    // seconds from start to end
    public float DurationS;
}
