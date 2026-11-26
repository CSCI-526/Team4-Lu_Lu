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
/// the menu copies the main <see cref="WorldBalanceConfig"/> into a DEV asset
/// whose boss heights are lowered per <see cref="BossDevStartConfig"/>, and
/// points every reference to the main one in the scenes at the copy. Run it
/// again after the main scene or the balance changes: each scene and the copy
/// are rebuilt from them, keeping their <c>.meta</c>. The main scene and the
/// main balance asset are never edited or saved. The dev scenes are listed,
/// disabled, in the Editor Build Settings.
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
        RefreshWorldCopy();
        BuildScene(COBRA_KING_SCENE_PATH, Boss.CobraKing);
        BuildScene(SPIDER_KING_SCENE_PATH, Boss.SpiderKing);
        ListInBuildSettings(COBRA_KING_SCENE_PATH, SPIDER_KING_SCENE_PATH);

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

    // makes the DEV balance asset a fresh copy of the main one, then lowers
    // each king's group of heights so its zone starts at the config's target;
    // the group keeps its spacing, the order cobra < spider is kept
    private static void RefreshWorldCopy() {
        var main =
            AssetDatabase.LoadAssetAtPath<WorldBalanceConfig>(WORLD_PATH);
        var config =
            AssetDatabase.LoadAssetAtPath<BossDevStartConfig>(CONFIG_PATH);
        var copy =
            AssetDatabase.LoadAssetAtPath<WorldBalanceConfig>(WORLD_COPY_PATH);
        if (copy == null) {
            copy = Object.Instantiate(main);
            AssetDatabase.CreateAsset(copy, WORLD_COPY_PATH);
            Debug.Log("BossDevSceneBuilder:\tcreated " + WORLD_COPY_PATH);
        } else {
            EditorUtility.CopySerialized(main, copy);
        }
        copy.name = Path.GetFileNameWithoutExtension(WORLD_COPY_PATH);

        float cobraShiftU = main.BossZoneStartU - config.CobraZoneStartU;
        float spiderShiftU = main.SpiderZoneStartU - config.SpiderZoneStartU;
        var serialized = new SerializedObject(copy);
        LowerHeight(serialized, "bossZoneStartU", cobraShiftU);
        LowerHeight(serialized, "bossTriggerHeightU", cobraShiftU);
        LowerHeight(serialized, "bossZoneEndU", cobraShiftU);
        LowerHeight(serialized, "spiderZoneStartU", spiderShiftU);
        LowerHeight(serialized, "spiderTriggerHeightU", spiderShiftU);
        LowerHeight(serialized, "spiderZoneEndU", spiderShiftU);
        LowerHeight(serialized, "spiderJumpHeightU", spiderShiftU);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(copy);
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"BossDevSceneBuilder:\tboss heights lowered, Cobra King by "
                + $"{cobraShiftU} u, Spider King by {spiderShiftU} u"
        );
    }

    private static void LowerHeight(
        SerializedObject serialized, string field, float shiftU
    ) {
        SerializedProperty prop = serialized.FindProperty(field);
        prop.floatValue = Mathf.Max(0f, prop.floatValue - shiftU);
    }

    // points each reference to the main balance asset, on any component of
    // the open scene, at the DEV copy; a prefab instance gets an override
    private static int RepointWorldConfig(Scene scene) {
        var main =
            AssetDatabase.LoadAssetAtPath<WorldBalanceConfig>(WORLD_PATH);
        var copy =
            AssetDatabase.LoadAssetAtPath<WorldBalanceConfig>(WORLD_COPY_PATH);
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

    // copies the main scene to the path, then adds the boss's start object
    private static void BuildScene(string path, Boss boss) {
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

        int count = RepointWorldConfig(scene);
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
    private const string WORLD_COPY_PATH =
        "Assets/_Monkobra/Settings/DevScenes/DEVBossWorldBalanceConfig.asset";
    private const string COBRA_KING_SCENE_PATH =
        "Assets/_Monkobra/Scenes/DEVBossCobraKingScene.unity";
    private const string SPIDER_KING_SCENE_PATH =
        "Assets/_Monkobra/Scenes/DEVBossSpiderKingScene.unity";
}
