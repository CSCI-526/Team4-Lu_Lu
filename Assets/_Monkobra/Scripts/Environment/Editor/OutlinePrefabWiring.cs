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

    private const string STYLE_PATH =
        ROOT + "Settings/FruitOutline/FruitOutlineStyle.asset";

    // prefab of each fruit, and the kind its listener is set to
    private static readonly (string path, FruitKind kind)[] FRUITS = {
        (ROOT + "Prefabs/Environment/_BranchBase.prefab", FruitKind.Banana),
        (ROOT + "Prefabs/ItemCoconut/Coconut.prefab", FruitKind.Coconut),
        (ROOT + "Prefabs/ItemPineapple/Pineapple.prefab", FruitKind.Pineapple),
    };

    // Menu  ###################################################################
    [MenuItem(MENU_PATH)]
    public static void Run() {
        WireMonkey();
        WireFruits();
        AssetDatabase.SaveAssets();
        Debug.Log("OutlinePrefabWiring:\toutline prefabs wired");
    }

    // private methods  ########################################################
    private static void WireFruits() {
        var style = AssetDatabase.LoadAssetAtPath<FruitOutlineStyle>(STYLE_PATH);
        if (style == null) {
            Debug.LogError(
                $"OutlinePrefabWiring:	fail to load style: {STYLE_PATH}"
            );
            return;
        }

        foreach ((string path, FruitKind kind) in FRUITS) {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                FruitOutlineListener[] listeners =
                    root.GetComponentsInChildren<FruitOutlineListener>(true);
                if (listeners.Length == 0) {
                    Debug.LogError(
                        "OutlinePrefabWiring:	no FruitOutlineListener in "
                            + path
                    );
                    continue;
                }
                foreach (FruitOutlineListener listener in listeners) {
                    var serialized = new SerializedObject(listener);
                    serialized.FindProperty("style").objectReferenceValue =
                        style;
                    serialized.FindProperty("kind").enumValueIndex =
                        (int)kind;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            } finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

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
