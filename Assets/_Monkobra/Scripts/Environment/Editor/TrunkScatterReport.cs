using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu <c>Monkobra > Report Trunk Scatter</c>: runs the pure
/// <see cref="ScatterStream"/> math over a Normal run with the live balance
/// configs and logs, per channel, the count per 10 u (mean, standard
/// deviation), the largest gap and the first height. Needs no scene and no
/// physics. Boss zones are left out, since nothing grows there.
/// </summary>
public static class TrunkScatterReport {
    // constants  ##############################################################
    private const string MENU_PATH = "Monkobra/Report Trunk Scatter";
    private const int SEED = 12345;

    // climbed height the report covers, a Normal run; u
    private const float RUN_HEIGHT_U = 1500f;

    // climbed height of the starting zone exit, where the ramp starts at 0;
    // the report assumes the monkey starts on it, so no starting zone; u
    private const float EXIT_CLIMBED_U = 0f;

    // world y of the stack bottom under the monkey's start, as the pool lays
    // it out; u
    private const float STACK_BOTTOM_U = -45f;

    // Menu  ###################################################################
    [MenuItem(MENU_PATH)]
    public static void Run() {
        WorldBalanceConfig world = LoadAsset<WorldBalanceConfig>(
            "Settings/Environment/WorldBalanceConfig.asset"
        );
        CobraBalanceConfig cobra = LoadAsset<CobraBalanceConfig>(
            "Settings/MobGreatCobra/CobraBalanceConfig.asset"
        );
        TrunkScatterConfig scatter = LoadAsset<TrunkScatterConfig>(
            "Settings/Environment/TrunkScatterConfig.asset"
        );
        if (world == null || cobra == null || scatter == null) {
            Debug.LogError(
                "TrunkScatterReport:\tmissing WorldBalanceConfig, "
                    + "CobraBalanceConfig or TrunkScatterConfig asset"
            );
            return;
        }

        StringBuilder report = new();
        report.AppendLine(
            $"TrunkScatterReport:\tseed {SEED}, 0~{RUN_HEIGHT_U} u climbed, "
                + "boss zones left out"
        );
        report.AppendLine(
            "channel          items  mean/10u  sd/10u  err/10u  rand/10u  "
                + "maxGap u  first u"
        );
        foreach (ScatterChannel channel in
            System.Enum.GetValues(typeof(ScatterChannel))
        ) {
            report.AppendLine(
                DescribeChannel(channel, world, cobra, scatter)
            );
        }
        Debug.Log(report.ToString());
    }

    // private methods  ########################################################
    private static T LoadAsset<T>(string pathSuffix) where T: Object {
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}")) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(pathSuffix)) {
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
        }
        return null;
    }

    private static string DescribeChannel(
        ScatterChannel channel,
        WorldBalanceConfig world,
        CobraBalanceConfig cobra,
        TrunkScatterConfig scatter
    ) {
        ScatterStream stream = new(
            SEED,
            channel,
            STACK_BOTTOM_U,
            scatter.JitterFraction,
            scatter.YawJitterDeg,
            scatter.IntegrationStepU
        );
        List<ScatterItem> items = new();
        stream.GetItems(
            EXIT_CLIMBED_U,
            RUN_HEIGHT_U,
            y => GetDensity(channel, y, world, cobra),
            items
        );

        // 10 u bins and gaps, only where the ground is free of boss zones
        int binCount = Mathf.CeilToInt(RUN_HEIGHT_U / 10f);
        int[] counts = new int[binCount];
        int kept = 0;
        float maxGap = 0f;
        float firstY = float.NaN;
        float previousY = float.NaN;
        foreach (ScatterItem item in items) {
            if (IsInBossZone(item.Y, world)) {
                previousY = float.NaN;
                continue;
            }
            kept++;
            if (float.IsNaN(firstY)) {
                firstY = item.Y;
            }
            counts[Mathf.Min(binCount - 1, (int)(item.Y / 10f))]++;
            if (
                !float.IsNaN(previousY)
                && !IsZoneBetween(previousY, item.Y, world)
            ) {
                maxGap = Mathf.Max(maxGap, item.Y - previousY);
            }
            previousY = item.Y;
        }

        // bins from the first item on: a channel that starts late is not
        // penalised for the bare ground below its first height
        // err: rms miss of a bin's count against the curve's own expected
        // count there; rand: the same for independent random rolls, ie
        // sqrt(expected), the old per-segment rolls' scale
        double sum = 0;
        double sumSquares = 0;
        double sumMissSquares = 0;
        double sumExpected = 0;
        int binsUsed = 0;
        for (int i = 0; i < binCount; i++) {
            float binMid = i * 10f + 5f;
            if (
                IsInBossZone(binMid, world)
                || float.IsNaN(firstY)
                || binMid < firstY
            ) {
                continue;
            }
            float expected = GetDensity(channel, binMid, world, cobra);
            sum += counts[i];
            sumMissSquares += (counts[i] - expected) * (counts[i] - expected);
            sumExpected += expected;
            sumSquares += (double)counts[i] * counts[i];
            binsUsed++;
        }
        double mean = binsUsed > 0 ? sum / binsUsed : 0;
        double variance = binsUsed > 0
            ? Mathf.Max(0f, (float)(sumSquares / binsUsed - mean * mean))
            : 0;
        double miss = binsUsed > 0
            ? System.Math.Sqrt(sumMissSquares / binsUsed) : 0;
        double rand = binsUsed > 0
            ? System.Math.Sqrt(sumExpected / binsUsed) : 0;
        return $"{channel,-16} {kept,5}  {mean,8:F2}  "
            + $"{System.Math.Sqrt(variance),6:F2}  {miss,7:F2}  "
            + $"{rand,8:F2}  {maxGap,8:F1}  "
            + (float.IsNaN(firstY) ? "    none" : $"{firstY,8:F0}");
    }

    private static float GetDensity(
        ScatterChannel channel,
        float climbedY,
        WorldBalanceConfig world,
        CobraBalanceConfig cobra
    ) {
        float ramped = GetRampedValue(climbedY, world);
        AnimationCurve curve = channel switch {
            ScatterChannel.EmptyBranch =>
                world.GetBranchDensityCurve(BranchKind.Empty),
            ScatterChannel.BananaBranch =>
                world.GetBranchDensityCurve(BranchKind.Banana),
            ScatterChannel.CoconutBranch =>
                world.GetBranchDensityCurve(BranchKind.Coconut),
            ScatterChannel.PineappleBranch =>
                world.GetBranchDensityCurve(BranchKind.Pineapple),
            ScatterChannel.Web => world.WebDensityCurve,
            ScatterChannel.FallingCobra => cobra.FallingCobraDensityCurve,
            _ => world.RoughBarkDensityCurve
        };
        return Mathf.Max(0f, curve.Evaluate(ramped));
    }

    // RampedDifficultyManager.GetRampedValueFromYPosition for a Normal run
    // starting on the exit; kept in step by hand, it is not callable w/o a
    // scene
    private static float GetRampedValue(
        float climbedY,
        WorldBalanceConfig world
    ) {
        float zoneStartU = world.BossZoneStartU;
        if (climbedY < EXIT_CLIMBED_U || zoneStartU <= EXIT_CLIMBED_U) {
            return climbedY < zoneStartU ? 0f : GetBossStagesValue(
                climbedY,
                world
            );
        }
        if (climbedY < zoneStartU) {
            float progress =
                (climbedY - EXIT_CLIMBED_U) / (zoneStartU - EXIT_CLIMBED_U);
            return Mathf.Max(0f, world.PreBossRampCurve.Evaluate(progress));
        }
        return GetBossStagesValue(climbedY, world);
    }

    private static float GetBossStagesValue(
        float climbedY,
        WorldBalanceConfig world
    ) {
        if (climbedY <= world.BossZoneEndU) {
            return 1f;
        }
        if (climbedY < world.SpiderZoneStartU) {
            float progress = (climbedY - world.BossZoneEndU)
                / (world.SpiderZoneStartU - world.BossZoneEndU);
            return Mathf.Max(1f, world.MidBossRampCurve.Evaluate(progress));
        }
        if (climbedY <= world.SpiderZoneEndU) {
            return 2f;
        }
        return Mathf.Max(
            2f,
            world.PostBossRampCurve.Evaluate(climbedY - world.SpiderZoneEndU)
        );
    }

    private static bool IsInBossZone(float climbedY, WorldBalanceConfig world) {
        return (climbedY >= world.BossZoneStartU
                && climbedY <= world.BossZoneEndU)
            || (climbedY >= world.SpiderZoneStartU
                && climbedY <= world.SpiderZoneEndU);
    }

    // iff a boss zone lies wholly or partly b/t the 2 heights
    private static bool IsZoneBetween(
        float lowY,
        float highY,
        WorldBalanceConfig world
    ) {
        return (lowY < world.BossZoneEndU && highY > world.BossZoneStartU)
            || (lowY < world.SpiderZoneEndU && highY > world.SpiderZoneStartU);
    }
}
