using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// <c>Monkobra/Build Boss Dev Scenes</c> makes the 2 boss dev scenes, one
/// per boss, as copies of <c>GameplayScene</c> with a
/// <see cref="BossDevStart"/> object added, so each plays like the player's
/// first meeting with that boss. The boss is moved down, never the monkey up:
/// the menu copies the main <see cref="WorldBalanceConfig"/> into one DEV
/// asset per scene whose boss heights put the boss under test just above the
/// player per <see cref="BossDevStartConfig"/>, points every reference to the
/// main one in the scene at its copy, and moves the ramp base to the
/// monkey's start so the scene has no starting zone. Run it again after the
/// main scene or the balance changes: each scene and copy are rebuilt from
/// them, keeping their <c>.meta</c>. The main scene and the main balance
/// asset are never edited or saved. The dev scenes are listed, disabled, in
/// the Editor Build Settings.
/// </summary>
public static class BossDevSceneBuilder {
    // private methods  ########################################################
    [MenuItem("Monkobra/Build Boss Dev Scenes")]
    private static void Build() {
        if (!File.Exists(MAIN_SCENE_PATH)) {
            Debug.LogError("BossDevSceneBuilder:\tno " + MAIN_SCENE_PATH);
            return;
        }
        WorldBalanceConfig world =
            AssetDatabase.LoadAssetAtPath<WorldBalanceConfig>(WORLD_PATH);
        if (world == null) {
            Debug.LogError("BossDevSceneBuilder:\tno asset at " + WORLD_PATH);
            return;
        }
        // asks before the open scenes are replaced; no is a clean stop
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
            Debug.LogWarning("BossDevSceneBuilder:\tcancelled");
            return;
        }

        EnsureConfig();
        BuildScene(
            COBRA_KING_SCENE_PATH, COBRA_KING_WORLD_PATH, Boss.CobraKing
        );
        BuildScene(
            SPIDER_KING_SCENE_PATH, SPIDER_KING_WORLD_PATH, Boss.SpiderKing
        );
        ListInBuildSettings(COBRA_KING_SCENE_PATH, SPIDER_KING_SCENE_PATH);
        // the single balance copy of the first version, no scene uses it now
        if (AssetDatabase.DeleteAsset(OLD_WORLD_COPY_PATH)) {
            Debug.Log("BossDevSceneBuilder:\tdeleted " + OLD_WORLD_COPY_PATH);
        }

        // back to the main scene, so the Editor is left where it was found
        EditorSceneManager.OpenScene(MAIN_SCENE_PATH, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log(
            "BossDevSceneBuilder:\tbuilt " + COBRA_KING_SCENE_PATH + " & "
                + SPIDER_KING_SCENE_PATH
        );
    }

    // creates the config asset w/ its defaults the first time only, so a
    // tuned asset survives a rebuild
    private static void EnsureConfig() {
        if (AssetDatabase.LoadAssetAtPath<BossDevStartConfig>(CONFIG_PATH)
            != null) {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(CONFIG_PATH));
        AssetDatabase.CreateAsset(
            ScriptableObject.CreateInstance<BossDevStartConfig>(),
            CONFIG_PATH
        );
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(CONFIG_PATH);
        Debug.Log("BossDevSceneBuilder:\tcreated " + CONFIG_PATH);
    }

    // makes the scene's DEV balance asset a fresh copy of the main one, then
    // places the kings' groups of heights: the boss under test has its zone
    // start just above the player; the other king stays out of the way, the
    // Spider King's above the Cobra King's, the Cobra King's, if skipped,
    // collapsed to 0; each group keeps its spacing
    private static void RefreshWorldCopy(string copyPath, Boss boss) {
        var main =
            AssetDatabase.LoadAssetAtPath<WorldBalanceConfig>(WORLD_PATH);
        var config =
            AssetDatabase.LoadAssetAtPath<BossDevStartConfig>(CONFIG_PATH);
        var copy =
            AssetDatabase.LoadAssetAtPath<WorldBalanceConfig>(copyPath);
        if (copy == null) {
            copy = Object.Instantiate(main);
            AssetDatabase.CreateAsset(copy, copyPath);
            Debug.Log("BossDevSceneBuilder:\tcreated " + copyPath);
        } else {
            EditorUtility.CopySerialized(main, copy);
        }
        copy.name = Path.GetFileNameWithoutExtension(copyPath);

        var serialized = new SerializedObject(copy);
        float cobraStartU;
        float spiderStartU;
        if (boss == Boss.CobraKing) {
            cobraStartU = config.BossZoneStartU;
            float cobraEndU = cobraStartU
                + main.BossZoneEndU - main.BossZoneStartU;
            spiderStartU = cobraEndU + config.OtherKingGapU;
            PlaceCobraGroup(serialized, main, cobraStartU);
        } else {
            spiderStartU = config.BossZoneStartU;
            CollapseCobraGroup(serialized);
        }
        PlaceSpiderGroup(serialized, main, spiderStartU);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(copy);
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"BossDevSceneBuilder:\t{boss} balance, zone starts: Cobra King "
                + $"{copy.BossZoneStartU} u, Spider King "
                + $"{copy.SpiderZoneStartU} u"
        );
    }

    // the Cobra King's start, trigger & end, spaced as in the main asset
    private static void PlaceCobraGroup(
        SerializedObject serialized, WorldBalanceConfig main, float startU
    ) {
        float baseU = main.BossZoneStartU;
        SetHeight(serialized, "bossZoneStartU", startU);
        SetHeight(serialized, "bossTriggerHeightU",
            startU + main.BossTriggerHeightU - baseU);
        SetHeight(serialized, "bossZoneEndU",
            startU + main.BossZoneEndU - baseU);
    }

    // a skipped Cobra King leaves no zone, so the Spider King can sit low
    private static void CollapseCobraGroup(SerializedObject serialized) {
        SetHeight(serialized, "bossZoneStartU", 0f);
        SetHeight(serialized, "bossTriggerHeightU", 0f);
        SetHeight(serialized, "bossZoneEndU", 0f);
    }

    // the Spider King's start, jump height, trigger & end, spaced as in the
    // main asset
    private static void PlaceSpiderGroup(
        SerializedObject serialized, WorldBalanceConfig main, float startU
    ) {
        float baseU = main.SpiderZoneStartU;
        SetHeight(serialized, "spiderZoneStartU", startU);
        SetHeight(serialized, "spiderJumpHeightU",
            startU + main.SpiderJumpHeightU - baseU);
        SetHeight(serialized, "spiderTriggerHeightU",
            startU + main.SpiderTriggerHeightU - baseU);
        SetHeight(serialized, "spiderZoneEndU",
            startU + main.SpiderZoneEndU - baseU);
    }

    // a height never goes under the player's start
    private static void SetHeight(
        SerializedObject serialized, string field, float heightU
    ) {
        serialized.FindProperty(field).floatValue = Mathf.Max(0f, heightU);
    }

    // there is no starting zone in a dev scene: the ramp base, where the
    // starting zone ends, sits at the monkey's start, so nothing is below it
    private static void MoveRampBaseToStart() {
        var ramp = Object.FindFirstObjectByType<RampedDifficultyManager>();
        var monkey = Object.FindFirstObjectByType<MonkeyController>();
        if (ramp == null || monkey == null) {
            Debug.LogError(
                "BossDevSceneBuilder:\tno RampedDifficultyManager or "
                    + "MonkeyController in the scene"
            );
            return;
        }

        var go = new GameObject(RAMP_BASE_NAME);
        go.transform.position = new Vector3(
            0f, monkey.transform.position.y, 0f
        );
        var serialized = new SerializedObject(ramp);
        serialized.FindProperty("rampBase").objectReferenceValue =
            go.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // points each reference to the main balance asset, on any component of
    // the open scene, at the DEV copy; a prefab instance gets an override
    private static int RepointWorldConfig(Scene scene, string copyPath) {
        var main =
            AssetDatabase.LoadAssetAtPath<WorldBalanceConfig>(WORLD_PATH);
        var copy =
            AssetDatabase.LoadAssetAtPath<WorldBalanceConfig>(copyPath);
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects()) {
            foreach (
                Component component
                in root.GetComponentsInChildren<Component>(true)
            ) {
                if (component == null) {
                    continue;
                }
                var serialized = new SerializedObject(component);
                SerializedProperty prop = serialized.GetIterator();
                bool isChanged = false;
                while (prop.NextVisible(true)) {
                    if (
                        prop.propertyType == SerializedPropertyType
                            .ObjectReference
                        && prop.objectReferenceValue == main
                    ) {
                        prop.objectReferenceValue = copy;
                        isChanged = true;
                        count++;
                    }
                }
                if (isChanged) {
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
        return count;
    }

    // copies the main scene to the path, then adds the boss's start object,
    // moves the ramp base to the start & repoints the balance
    private static void BuildScene(
        string path, string worldCopyPath, Boss boss
    ) {
        Scene main = EditorSceneManager.OpenScene(
            MAIN_SCENE_PATH, OpenSceneMode.Single
        );
        // a copy overwrites the file in place, so its guid & meta stay
        if (!EditorSceneManager.SaveScene(main, path, true)) {
            Debug.LogError("BossDevSceneBuilder:\tcould not write " + path);
            return;
        }
        AssetDatabase.ImportAsset(path);

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        // loaded after the scene opens: an asset held across the open can be
        // unloaded, which saves a null reference
        var config =
            AssetDatabase.LoadAssetAtPath<BossDevStartConfig>(CONFIG_PATH);
        var go = new GameObject(nameof(BossDevStart));
        BossDevStart start = go.AddComponent<BossDevStart>();
        var serialized = new SerializedObject(start);
        serialized.FindProperty("boss").enumValueIndex = (int)boss;
        serialized.FindProperty("config").objectReferenceValue = config;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        RefreshWorldCopy(worldCopyPath, boss);
        MoveRampBaseToStart();
        // the scene must have been opened before the copy is loaded for it
        int count = RepointWorldConfig(scene, worldCopyPath);
        if (count == 0) {
            Debug.LogError(
                "BossDevSceneBuilder:\tno balance reference to repoint in "
                    + path
            );
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log(
            $"BossDevSceneBuilder:\t{boss} scene, {count} balance "
                + "references moved to the DEV copy"
        );
    }

    // adds each path to the build list, disabled, unless it is there already
    private static void ListInBuildSettings(params string[] paths) {
        var scenes = new List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes
        );
        foreach (string path in paths) {
            if (!scenes.Exists(s => s.path == path)) {
                scenes.Add(new EditorBuildSettingsScene(path, false));
            }
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // constants  ##############################################################
    private const string MAIN_SCENE_PATH =
        "Assets/_Monkobra/Scenes/GameplayScene.unity";
    private const string WORLD_PATH =
        "Assets/_Monkobra/Settings/Environment/WorldBalanceConfig.asset";
    private const string CONFIG_PATH =
        "Assets/_Monkobra/Settings/DevScenes/DEVBossStartConfig.asset";
    private const string COBRA_KING_WORLD_PATH =
        "Assets/_Monkobra/Settings/DevScenes/"
        + "DEVBossCobraKingWorldBalanceConfig.asset";
    private const string SPIDER_KING_WORLD_PATH =
        "Assets/_Monkobra/Settings/DevScenes/"
        + "DEVBossSpiderKingWorldBalanceConfig.asset";
    private const string OLD_WORLD_COPY_PATH =
        "Assets/_Monkobra/Settings/DevScenes/DEVBossWorldBalanceConfig.asset";
    private const string RAMP_BASE_NAME = "DEVRampBase";
    private const string COBRA_KING_SCENE_PATH =
        "Assets/_Monkobra/Scenes/DEVBossCobraKingScene.unity";
    private const string SPIDER_KING_SCENE_PATH =
        "Assets/_Monkobra/Scenes/DEVBossSpiderKingScene.unity";
}
