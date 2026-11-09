using System.Collections.Generic;

/// <summary>
/// one kind of event stamped into a run, listed in the order the report shows
/// them. To record a new metric: add an entry here and a row to
/// <see cref="AnalyticsMetrics"/>, then call
/// <see cref="AnalyticsManager"/>'s <c>RecordEvent</c> from one subscription;
/// the report renders it w/o any writer change.
/// </summary>
public enum AnalyticsMetric {
    Banana,
    Coconut,
    CoconutUsed,
    Web,
    Branch
}

/// <summary>
/// how the report words and lays out each <see cref="AnalyticsMetric"/>
/// </summary>
public static class AnalyticsMetrics {
    // Public Members  #########################################################
    /// <returns>every metric, in report order</returns>
    public static IReadOnlyList<AnalyticsMetric> All => all;

    // Public Methods  #########################################################
    /// <returns>heading of the metric's report section, before its count
    /// </returns>
    public static string GetTitle(AnalyticsMetric metric) {
        return GetInfo(metric).Title;
    }

    /// <returns>if the metric's table carries the height of each event
    /// </returns>
    public static bool HasHeight(AnalyticsMetric metric) {
        return GetInfo(metric).HasHeight;
    }

    // nested types  ###########################################################
    private readonly struct Info {
        public readonly string Title;
        public readonly bool HasHeight;

        public Info(string title, bool hasHeight) {
            Title = title;
            HasHeight = hasHeight;
        }
    }

    // private members  ########################################################
    // one row per metric; a metric missing here falls back to its enum name
    private static readonly Dictionary<AnalyticsMetric, Info> infos =
        new Dictionary<AnalyticsMetric, Info> {
            { AnalyticsMetric.Banana, new Info("Banana Count", true) },
            { AnalyticsMetric.Coconut, new Info("Coconut Count", true) },
            { AnalyticsMetric.CoconutUsed, new Info("Coconut Used", true) },
            { AnalyticsMetric.Web, new Info("Web Encounter", false) },
            { AnalyticsMetric.Branch, new Info("Branch Hit", true) }
        };

    private static readonly AnalyticsMetric[] all =
        (AnalyticsMetric[])System.Enum.GetValues(typeof(AnalyticsMetric));

    // private methods  ########################################################
    private static Info GetInfo(AnalyticsMetric metric) {
        return infos.TryGetValue(metric, out Info info)
            ? info
            : new Info(metric.ToString(), true);
    }
}
