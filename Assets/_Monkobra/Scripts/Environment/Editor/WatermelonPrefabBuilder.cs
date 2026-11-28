using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu <c>Monkobra > Build > Watermelon Prefab</c>: makes
/// <c>Branch_Watermelon</c>, a Prefab Variant of <c>Branch_Banana</c> whose
/// fruit wears the watermelon mesh and the <see cref="FruitKind.Melon"/>
/// outline, so both boss arenas plant the melon as a prefab, never by a
/// runtime mesh swap. Mesh, scale & offset come from the Cobra King config.
/// Safe to run again: an existing prefab is updated in place.
/// </summary>
public static class WatermelonPrefabBuilder {
    // constants  ##############################################################
    private const string MENU_PATH = "Monkobra/Build/Watermelon Prefab";
    private const string ROOT = "Assets/_Monkobra/";
    private const string BANANA_PREFAB =
        ROOT + "Prefabs/Environment/Branch_Banana.prefab";

    private const string WATERMELON_PREFAB =
        ROOT + "Prefabs/Environment/Branch_Watermelon.prefab";

    private const string KING_CONFIG =
        ROOT + "Settings/BossCobraKing/CobraKingConfig.asset";

    // Menu  ###################################################################
    [MenuItem(MENU_PATH, false, 52)]
    public static void Run() {
        var config =
            AssetDatabase.LoadAssetAtPath<CobraKingConfig>(KING_CONFIG);
        if (config == null || config.WatermelonMesh == null) {
            Debug.LogError(
                "WatermelonPrefabBuilder:\tfail to load watermelon mesh "
                    + "from " + KING_CONFIG
            );
            return;
        }
        var banana = AssetDatabase.LoadAssetAtPath<GameObject>(BANANA_PREFAB);
        if (banana == null) {
            Debug.LogError(
                "WatermelonPrefabBuilder:\tfail to load Prefab: "
                    + BANANA_PREFAB
            );
            return;
        }

        // first run saves a variant of the banana branch, later runs reopen it
        if (AssetDatabase.LoadAssetAtPath<GameObject>(WATERMELON_PREFAB)
            == null) {
            var instance =
                (GameObject)PrefabUtility.InstantiatePrefab(banana);
            instance.name = "Branch_Watermelon";
            try {
                PrefabUtility.SaveAsPrefabAsset(instance, WATERMELON_PREFAB);
            } finally {
                Object.DestroyImmediate(instance);
            }
        }

        GameObject root = PrefabUtility.LoadPrefabContents(WATERMELON_PREFAB);
        try {
            if (!DressFruit(root, config)) {
                return;
            }
            PrefabUtility.SaveAsPrefabAsset(root, WATERMELON_PREFAB);
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("WatermelonPrefabBuilder:\twatermelon prefab built");
    }

    // private methods  ########################################################
    // mesh, scale & offset onto the fruit's meshes, the Melon kind onto its
    // outline listeners; false when the branch bears no fruit
    private static bool DressFruit(GameObject root, CobraKingConfig config) {
        ScorePickup pickup = root.GetComponentInChildren<ScorePickup>(true);
        if (pickup == null) {
            Debug.LogError(
                "WatermelonPrefabBuilder:\tno ScorePickup in "
                    + WATERMELON_PREFAB
            );
            return false;
        }
        foreach (MeshFilter filter in
            pickup.GetComponentsInChildren<MeshFilter>(true)) {
            filter.sharedMesh = config.WatermelonMesh;
            filter.transform.localRotation = Quaternion.identity;
            filter.transform.localScale = config.WatermelonScale;
            filter.transform.localPosition = config.WatermelonOffset;
        }
        FruitOutlineListener[] listeners =
            root.GetComponentsInChildren<FruitOutlineListener>(true);
        if (listeners.Length == 0) {
            Debug.LogError(
                "WatermelonPrefabBuilder:\tno FruitOutlineListener in "
                    + WATERMELON_PREFAB
            );
            return false;
        }
        foreach (FruitOutlineListener listener in listeners) {
            var serialized = new SerializedObject(listener);
            serialized.FindProperty("kind").enumValueIndex =
                (int)FruitKind.Melon;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        return true;
    }
}
