using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps the version of every Build Profile equal to the Player Settings
/// version. A profile holds a full copy of Player Settings, so its
/// <c>bundleVersion</c> drifts when only the project one is bumped, and
/// <c>Application.version</c>, the build folder name, and the analytics report
/// then show the stale value. Runs when the Editor loads or scripts reload,
/// and from the <c>Monkobra</c> menu.
/// </summary>
[InitializeOnLoad]
public static class BuildVersionSync {
    // static init  ############################################################
    static BuildVersionSync() {
        // the asset database is not ready inside the static ctor
        EditorApplication.delayCall += SyncAll;
    }

    // private methods  ########################################################
    [MenuItem("Monkobra/Sync Build Profile Version")]
    private static void SyncAll() {
        string version = ReadProjectVersion();
        if (string.IsNullOrEmpty(version)) {
            Debug.LogWarning(
                "BuildVersionSync:\tno bundleVersion in " + PROJECT_SETTINGS
            );
            return;
        }

        bool isChanged = false;
        foreach (string guid in AssetDatabase.FindAssets(
            "t:BuildProfile",
            new[] { PROFILE_FOLDER }
        )) {
            isChanged |= SyncProfile(
                AssetDatabase.GUIDToAssetPath(guid),
                version
            );
        }

        if (isChanged) {
            AssetDatabase.Refresh();
        }
    }

    // Player Settings of the project itself, as no profile overrides it
    private static string ReadProjectVersion() {
        if (!File.Exists(PROJECT_SETTINGS)) {
            return string.Empty;
        }

        Match match = Regex.Match(
            File.ReadAllText(PROJECT_SETTINGS),
            @"^\s*bundleVersion:[ \t]*(\S+)[ \t]*\r?$",
            RegexOptions.Multiline
        );
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    // whether the profile file was rewritten
    private static bool SyncProfile(string path, string version) {
        string text = File.ReadAllText(path);
        string synced = Regex.Replace(
            text,
            @"(\|\s+bundleVersion:[ \t]*)[^'\r\n]*",
            "${1}" + version
        );
        if (synced == text) {
            return false;
        }

        File.WriteAllText(path, synced);
        Debug.Log("BuildVersionSync:\t" + path + " now " + version);
        return true;
    }

    // constants  ##############################################################
    private const string PROJECT_SETTINGS =
        "ProjectSettings/ProjectSettings.asset";
    private const string PROFILE_FOLDER = "Assets/Settings/Build Profiles";
}
