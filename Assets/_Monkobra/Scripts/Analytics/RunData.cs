using System;
using System.Collections.Generic;

/// <summary>
/// how a run ended, or that it has not yet
/// </summary>
public enum RunStatus {
    InProgress,
    Lost,
    // the Cobra King defeated
    Won,
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

    // which kind of run the scene played, stamped when the run opens
    public RunMode Mode = RunMode.Full;

    // <see cref="UnityEngine.Time.time"/> at run start, base of elapsed secs
    public float StartTime;
    public DateTime StartClock;
    public readonly List<RunSample> Samples = new List<RunSample>();
    // top of the stamina bar, the y range of the stamina chart
    public float MaxStamina = 1f;

    // starting zone  -----------------------------------------------------------
    // if the run opened in the starting zone, so it holds the zone's events
    public bool StartedInZone;
    // if the monkey left the starting zone during the run
    public bool HasZoneExit;
    public EventStamp ZoneExit;

    // set once the run leaves InProgress  --------------------------------------
    public EventStamp End;
    public DeathCause Cause = DeathCause.Unknown;
    // highest point reached, as opposed to where the run ended (End.Height);
    // world u
    public float HighestHeight;
    public long TotalScore;
    public long HeightScore;
    public long RewardScore;
    // RewardScore split by RewardKind, indexed by the enum value
    public readonly long[] RewardScoreByKind =
        new long[Enum.GetValues(typeof(RewardKind)).Length];
    // seconds from start to end
    public float DurationS;

    // Public Methods  #########################################################
    /// <returns>stamps of the metric in time order, empty if it never
    /// happened; add to it to record an event</returns>
    public List<EventStamp> GetEvents(AnalyticsMetric metric) {
        if (!events.TryGetValue(metric, out List<EventStamp> stamps)) {
            stamps = new List<EventStamp>();
            events[metric] = stamps;
        }

        return stamps;
    }

    /// <returns>how many times the metric happened in the run</returns>
    public int CountOf(AnalyticsMetric metric) {
        return events.TryGetValue(metric, out List<EventStamp> stamps)
            ? stamps.Count
            : 0;
    }

    // private members  ########################################################
    private readonly Dictionary<AnalyticsMetric, List<EventStamp>> events =
        new Dictionary<AnalyticsMetric, List<EventStamp>>();
}
