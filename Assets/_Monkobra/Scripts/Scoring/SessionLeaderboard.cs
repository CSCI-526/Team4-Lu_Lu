using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A fixed set of simulated rivals for one application launch. Scene reloads
/// reuse it; entering Play mode or launching a new WebGL player resets it.
/// </summary>
public sealed class SessionLeaderboard {
    // Public Members  #########################################################
    public readonly struct Entry {
        public readonly string Name;
        public readonly long Score;
        public readonly bool IsPlayer;
        internal readonly int Order;

        internal Entry(string name, long score, bool isPlayer, int order) {
            Name = name;
            Score = score;
            IsPlayer = isPlayer;
            Order = order;
        }
    }

    public int RivalCount => rivals.Length;

    // Public Methods  #########################################################
    /// <summary>
    /// The first call supplies this playtest's settings. Subsequent calls,
    /// including those from a freshly reloaded scene, reuse the same table.
    /// </summary>
    public static SessionLeaderboard ForCurrentPlaytest(
        int rivalCount, int minScore, int maxScore, string[] names
    ) {
        if (current == null) {
            current = new SessionLeaderboard(
                rivalCount, minScore, maxScore, names,
                Guid.NewGuid().GetHashCode()
            );
        }
        return current;
    }

    /// <summary>
    /// Builds an independent table with an explicit seed, also used by the
    /// Editor checks. Scores are inclusive of both ends of the range.
    /// </summary>
    public SessionLeaderboard(
        int rivalCount, int minScore, int maxScore, string[] names, int seed
    ) {
        if (rivalCount < 0) {
            throw new ArgumentOutOfRangeException(nameof(rivalCount));
        }
        if (minScore < 0 || maxScore < minScore) {
            throw new ArgumentOutOfRangeException(nameof(minScore));
        }

        // A private RNG never changes the random sequence used by hazards.
        System.Random random = new System.Random(seed);
        List<string> pool = BuildNamePool(names);
        for (int i = pool.Count - 1; i > 0; i--) {
            int other = random.Next(i + 1);
            string swap = pool[i];
            pool[i] = pool[other];
            pool[other] = swap;
        }

        rivals = new Entry[rivalCount];
        HashSet<string> used = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        );
        long range = (long)maxScore - minScore + 1L;
        for (int i = 0; i < rivals.Length; i++) {
            string name = i < pool.Count ? pool[i] : null;
            int suffix = i + 1;
            while (string.IsNullOrEmpty(name) || !used.Add(name)) {
                name = "Climber " + suffix.ToString("D2");
                suffix++;
            }
            long score = minScore + (long)(random.NextDouble() * range);
            rivals[i] = new Entry(name, score, false, i);
        }
    }

    /// <summary>
    /// Returns a fresh sorted result with exactly one current-run player
    /// entry. Earlier runs never become additional competitors.
    /// </summary>
    public Entry[] GetStandings(long playerScore) {
        Entry[] result = new Entry[rivals.Length + 1];
        Array.Copy(rivals, result, rivals.Length);
        result[rivals.Length] = new Entry(
            "YOU", Math.Max(0L, playerScore), true, rivals.Length
        );
        Array.Sort(result, CompareEntries);
        return result;
    }

    // Runtime Lifecycle  ######################################################
    // Runs once per player launch, not on SceneManager.LoadScene. Explicitly
    // reset so a new Editor playtest also works with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlaytest() {
        current = null;
    }

    // private members  ########################################################
    private static SessionLeaderboard current;
    private readonly Entry[] rivals;

    // private methods  ########################################################
    private static int CompareEntries(Entry a, Entry b) {
        int byScore = b.Score.CompareTo(a.Score);
        if (byScore != 0) {
            return byScore;
        }
        // Give the player the earlier place on a tie. Rivals tied with one
        // another retain their original order on every run.
        if (a.IsPlayer != b.IsPlayer) {
            return a.IsPlayer ? -1 : 1;
        }
        return a.Order.CompareTo(b.Order);
    }

    private static List<string> BuildNamePool(string[] names) {
        List<string> pool = new List<string>();
        HashSet<string> seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        );
        if (names == null) {
            return pool;
        }
        foreach (string value in names) {
            string name = value != null ? value.Trim() : string.Empty;
            if (name.Length > 0
                && !name.Equals("YOU", StringComparison.OrdinalIgnoreCase)
                && seen.Add(name)) {
                pool.Add(name);
            }
        }
        return pool;
    }
}
