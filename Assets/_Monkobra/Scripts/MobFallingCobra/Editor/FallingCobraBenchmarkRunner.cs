using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// <c>Monkobra/Run Falling Cobra Benchmark</c> builds the Windows development
/// player of the build scenes into <c>builds/Benchmark/</c>, the player the
/// <see cref="FallingCobraBenchmark"/> argument drives. Also callable from
/// batch mode: <c>-executeMethod FallingCobraBenchmarkRunner.Run</c>. It only
/// builds; launch the player with
/// <c>-fallingCobraBenchmark &lt;outputFolder&gt;</c>.
/// </summary>
public static class FallingCobraBenchmarkRunner {
    // Public Methods  #########################################################
    /// <summary>builds the player; in batch mode exits with its result
    /// </summary>
    public static void Run() {
        bool isSucceeded = BuildPlayer();
        if (Application.isBatchMode) {
            EditorApplication.Exit(isSucceeded ? 0 : 1);
        }
    }

    // private methods  ########################################################
    [MenuItem("Monkobra/Run Falling Cobra Benchmark")]
    private static void RunFromMenu() {
        BuildPlayer();
    }

    private static bool BuildPlayer() {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
            if (scene.enabled) {
                scenes.Add(scene.path);
            }
        }
        string projectRoot =
            Directory.GetParent(Application.dataPath).FullName;
        string folder = Path.Combine(projectRoot, OUTPUT_FOLDER);
        Directory.CreateDirectory(folder);
        BuildPlayerOptions options = new BuildPlayerOptions {
            scenes = scenes.ToArray(),
            locationPathName = Path.Combine(folder, PLAYER_FILE),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log("FallingCobraBenchmarkRunner:\tbuild "
            + summary.result + " in " + summary.totalTime.TotalSeconds
            .ToString("F0") + " s, " + options.locationPathName);
        return summary.result == BuildResult.Succeeded;
    }

    // constants  ##############################################################
    private const string OUTPUT_FOLDER = "builds/Benchmark";
    private const string PLAYER_FILE = "FallingCobraBenchmark.exe";
}
