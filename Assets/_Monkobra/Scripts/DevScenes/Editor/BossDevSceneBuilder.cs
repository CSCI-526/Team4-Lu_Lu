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
/// asset per scene whose boss heights put the boss under test's location at the
/// monkey's start, so its fight begins on the first live frame, points every
/// reference to the
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
    // places the kings' groups of heights: the boss under test has its
    // location at the player's start, so its fight begins at once; the other
    // king stays out of the way, the Spider King's above the Cobra King's,
    // the Cobra King's, if skipped, collapsed to 0; each group keeps its
    // spacing
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
        float spiderLocationU;
        if (boss == Boss.CobraKing) {
            PlaceLayout(serialized, "cobraKingArena",
                main.CobraKingLayout, 0f);
            // the Spider King zone starts a gap above the Cobra King's top
            spiderLocationU = main.CobraKingLayout.heightU * 0.5f
                + config.OtherKingGapU + main.SpiderKingLayout.heightU * 0.5f;
        } else {
            spiderLocationU = 0f;
            CollapseCobraLayout(serialized);
        }
        PlaceLayout(serialized, "spiderKingArena",
            main.SpiderKingLayout, spiderLocationU);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(copy);
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"BossDevSceneBuilder:\t{boss} balance, zone bottoms: Cobra King "
                + $"{copy.CobraKingLayout.BareBottomU} u, Spider King "
                + $"{copy.SpiderKingLayout.BareBottomU} u"
        );
    }

    // a boss placed by location, its height as in the main asset; a location
    // of 0 puts the bare zone half a height below the start, so the fight
    // trigger is met at once
    private static void PlaceLayout(
        SerializedObject serialized, string field, BossArenaLayout main,
        float locationU
    ) {
        serialized.FindProperty(field + ".heightU").floatValue = main.heightU;
        serialized.FindProperty(field + ".locationU").floatValue = locationU;
    }

    // a skipped Cobra King leaves a zone of 1 u at the start, so the Spider
    // King can sit low
    private static void CollapseCobraLayout(SerializedObject serialized) {
        serialized.FindProperty("cobraKingArena.heightU").floatValue = 1f;
        serialized.FindProperty("cobraKingArena.locationU").floatValue = 0.5f;
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

    // the Loop Override the scene at the path holds now, 1 when it has none
    private static int ReadLoopOverride(string path) {
        if (!File.Exists(path)) {
            return 1;
        }
        Scene old = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        int loop = 1;
        foreach (GameObject root in old.GetRootGameObjects()) {
            BossDevStart start = root.GetComponentInChildren<BossDevStart>();
            if (start != null) {
                loop = new SerializedObject(start)
                    .FindProperty("loopOverride").intValue;
                break;
            }
        }
        return loop;
    }

    // copies the main scene to the path, then adds the boss's start object,
    // moves the ramp base to the start & repoints the balance
    private static void BuildScene(
        string path, string worldCopyPath, Boss boss
    ) {
        // the scene's Loop Override outlives the rebuild
        int loopOverride = ReadLoopOverride(path);
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
        serialized.FindProperty("loopOverride").intValue = loopOverride;
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
