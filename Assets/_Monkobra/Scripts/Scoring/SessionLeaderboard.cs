using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SessionLeaderboard {
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

    private static SessionLeaderboard current;
    private readonly Entry[] rivals;

    // 同一次 playtest 只创建一份，重新加载关卡时继续使用。
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

    public SessionLeaderboard(
        int rivalCount, int minScore, int maxScore, string[] names, int seed
    ) {
        if (rivalCount < 0) {
            throw new ArgumentOutOfRangeException(nameof(rivalCount));
        }

        if (minScore < 0 || maxScore < minScore) {
            throw new ArgumentOutOfRangeException(nameof(minScore));
        }

        // 使用独立的随机数，不影响关卡中障碍物的随机生成。
        System.Random random = new System.Random(seed);
        List<string> pool = BuildNamePool(names);

        // 打乱名字池。
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

            // 名字不足或重复时，补一个不重复的名字。
            while (string.IsNullOrEmpty(name) || !used.Add(name)) {
                name = "Climber " + suffix.ToString("D2");
                suffix++;
            }

            long score = minScore + (long)(random.NextDouble() * range);
            rivals[i] = new Entry(name, score, false, i);
        }
    }

    // 每次只插入本局玩家成绩，之前几局的成绩不会累积。
    public Entry[] GetStandings(long playerScore) {
        Entry[] result = new Entry[rivals.Length + 1];
        Array.Copy(rivals, result, rivals.Length);

        result[rivals.Length] = new Entry(
            "YOU", Math.Max(0L, playerScore), true, rivals.Length
        );

        Array.Sort(result, CompareEntries);
        return result;
    }

    // 新打开游戏或重新进入 Play mode 时重置。
    // 普通的关卡重开不会调用这个方法。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlaytest() {
        current = null;
    }

    private static int CompareEntries(Entry a, Entry b) {
        int byScore = b.Score.CompareTo(a.Score);

        if (byScore != 0) {
            return byScore;
        }

        // 同分时，玩家排前面。
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