using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu <c>Monkobra > Wire Outline Prefabs</c>: points the prefabs that wear
/// an outline at the shared <see cref="FruitOutlineStyle"/> look, through
/// the serialized fields, never by hand in prefab YAML. Safe to run again.
/// </summary>
public static class OutlinePrefabWiring {
    // constants  ##############################################################
    private const string MENU_PATH = "Monkobra/Wire Outline Prefabs";
    private const string ROOT = "Assets/_Monkobra/";
    private const string MONKEY_PREFAB = ROOT + "Prefabs/Player/_Monkey.prefab";
    private const string MONKEY_MAT_PATH =
        ROOT + "Materials/Outline/Outline_Monkey.mat";

    // Menu  ###################################################################
    [MenuItem(MENU_PATH)]
    public static void Run() {
        WireMonkey();
        AssetDatabase.SaveAssets();
        Debug.Log("OutlinePrefabWiring:\toutline prefabs wired");
    }

    // private methods  ########################################################
    private static void WireMonkey() {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MONKEY_MAT_PATH);
        if (mat == null) {
            Debug.LogError(
                $"OutlinePrefabWiring:\tfail to load Material: {MONKEY_MAT_PATH}"
            );
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(MONKEY_PREFAB);
        try {
            MonkeyOutline outline =
                root.GetComponentInChildren<MonkeyOutline>(true);
            if (outline == null) {
                Debug.LogError(
                    "OutlinePrefabWiring:\tno MonkeyOutline in " + MONKEY_PREFAB
                );
                return;
            }
            var serialized = new SerializedObject(outline);
            serialized.FindProperty("outlineMat").objectReferenceValue = mat;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, MONKEY_PREFAB);
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
