using UnityEditor;
using UnityEngine;

/// <summary>
/// One-off migration of the project-structure Suite (Movement 5). Deleted when
/// the group prefabs are gone. Run from the menu, or headless with
/// <c>-executeMethod PrefabMigration.ExtractHudEntities</c>.
/// </summary>
public static class PrefabMigration {
    // menu entries  ############################################################
    [MenuItem("Monkobra/Migrate/Extract HUD Entities")]
    public static void ExtractHudEntities() {
        GameObject root = PrefabUtility.LoadPrefabContents(IN_GAME_HUD);
        try {
            // FruitFlyEffect points at 2 sibling instances of the HUD, which a
            // prefab asset cannot store: read them now, re-make as overrides
            Object energyBar, counter;
            Transform layer = root.transform.Find("FruitFlyLayer");
            using (SerializedObject effect = new(
                layer.GetComponent<FruitFlyEffect>()
            )) {
                energyBar = effect.FindProperty("energyBarTarget")
                    .objectReferenceValue;
                counter = effect.FindProperty("topRightCounterTarget")
                    .objectReferenceValue;
            }
            if (energyBar == null || counter == null) {
                Debug.LogError("PrefabMigration:\tFruitFlyEffect targets empty");
                return;
            }

            Extract(root, "PauseButton", PAUSE_BUTTON);
            Extract(root, "FruitFlyLayer", FRUIT_FLY_LAYER);
            Extract(root, "CoconutMessage", COCONUT_MESSAGE);

            Transform moved = root.transform.Find("FruitFlyLayer");
            using (SerializedObject effect = new(
                moved.GetComponent<FruitFlyEffect>()
            )) {
                effect.FindProperty("energyBarTarget").objectReferenceValue
                    = energyBar;
                effect.FindProperty("topRightCounterTarget")
                    .objectReferenceValue = counter;
                effect.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, IN_GAME_HUD);
            Debug.Log("PrefabMigration:\textracted 3 HUD entities");
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }

    // private methods  #########################################################
    private static void Extract(GameObject root, string child, string path) {
        Transform found = root.transform.Find(child);
        if (found == null) {
            Debug.LogError("PrefabMigration:\tno " + child + " in InGameHUD");
            return;
        }
        string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(folder)) {
            AssetDatabase.CreateFolder(
                System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'),
                System.IO.Path.GetFileName(folder)
            );
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            found.gameObject, path, InteractionMode.AutomatedAction
        );
    }

    // constants  ###############################################################
    private const string PREFABS = "Assets/_Monkobra/Prefabs/";
    private const string IN_GAME_HUD = PREFABS + "UI/InGameHUD.prefab";
    private const string PAUSE_BUTTON = PREFABS + "PauseScreen/PauseButton.prefab";
    private const string FRUIT_FLY_LAYER = PREFABS + "FruitFly/FruitFlyLayer.prefab";
    private const string COCONUT_MESSAGE =
        PREFABS + "ItemCoconut/CoconutMessage.prefab";
}
