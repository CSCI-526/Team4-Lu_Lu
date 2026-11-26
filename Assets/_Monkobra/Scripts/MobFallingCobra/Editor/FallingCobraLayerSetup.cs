using UnityEditor;
using UnityEngine;

/// <summary>
/// <c>Monkobra/Set Up Falling Cobra Layers</c> gives the monkey and the
/// falling cobra their own physics layers, so a cobra's trigger tests only
/// the monkey, never the trunk, branches or fruit: adds the layers
/// <c>Player</c> and <c>FallingCobra</c>, lets <c>FallingCobra</c> collide
/// with <c>Player</c> only in the collision matrix, puts every object of the
/// <c>_Monkey</c> and <c>FallingCobra</c> prefabs on its layer, and lowers
/// the Maximum Allowed Timestep so a slow frame runs fewer physics steps.
/// Safe to run again. Batch mode: <c>-executeMethod
/// FallingCobraLayerSetup.Run</c>. Run the menu, never edit these settings or
/// the two prefabs' layers by hand.
/// </summary>
public static class FallingCobraLayerSetup {
    // Public Methods  #########################################################
    /// <summary>applies the setup; in batch mode exits with its result
    /// </summary>
    public static void Run() {
        bool isSucceeded = Apply();
        if (Application.isBatchMode) {
            EditorApplication.Exit(isSucceeded ? 0 : 1);
        }
    }

    // private methods  ########################################################
    [MenuItem("Monkobra/Set Up Falling Cobra Layers")]
    private static void RunFromMenu() {
        Apply();
    }

    private static bool Apply() {
        int player = EnsureLayer(PLAYER_LAYER);
        int cobra = EnsureLayer(COBRA_LAYER);
        if (player < 0 || cobra < 0) {
            Debug.LogError("FallingCobraLayerSetup:\tno free layer slot");
            return false;
        }
        SetMaximumTimestep();
        SetCollisionMatrix(player, cobra);
        bool isPrefabsSet = SetPrefabLayer(PLAYER_PREFAB, player)
            & SetPrefabLayer(COBRA_PREFAB, cobra);
        AssetDatabase.SaveAssets();
        Debug.Log("FallingCobraLayerSetup:\tlayers " + PLAYER_LAYER + "="
            + player + ", " + COBRA_LAYER + "=" + cobra + ", timestep "
            + MAX_TIMESTEP_S + " s, prefabs set " + isPrefabsSet);
        return isPrefabsSet;
    }

    private static SerializedObject OpenSettings(string path) {
        return new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath(path)[0]);
    }

    // the layer's index, adding it to the first free user slot if missing;
    // -1 when none is free
    private static int EnsureLayer(string layerName) {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) {
            return existing;
        }
        SerializedObject tags = OpenSettings(TAG_MANAGER);
        SerializedProperty layers = tags.FindProperty("layers");
        for (int i = FIRST_USER_LAYER; i < layers.arraySize; i++) {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue)) {
                slot.stringValue = layerName;
                tags.ApplyModifiedProperties();
                return i;
            }
        }
        return -1;
    }

    private static void SetMaximumTimestep() {
        SerializedObject time = OpenSettings(TIME_MANAGER);
        time.FindProperty("Maximum Allowed Timestep").floatValue =
            MAX_TIMESTEP_S;
        time.ApplyModifiedProperties();
    }

    // the cobra layer collides with the player layer only; the matrix is
    // symmetric, so a bit is set on both rows
    private static void SetCollisionMatrix(int player, int cobra) {
        SerializedObject dynamics = OpenSettings(DYNAMICS_MANAGER);
        SerializedProperty matrix =
            dynamics.FindProperty("m_LayerCollisionMatrix");
        for (int layer = 0; layer < matrix.arraySize; layer++) {
            SetCollides(matrix, cobra, layer, layer == player);
        }
        dynamics.ApplyModifiedProperties();
    }

    private static void SetCollides(SerializedProperty matrix, int a, int b,
        bool isColliding) {
        SetBit(matrix.GetArrayElementAtIndex(a), b, isColliding);
        SetBit(matrix.GetArrayElementAtIndex(b), a, isColliding);
    }

    private static void SetBit(SerializedProperty row, int bit,
        bool isSet) {
        uint mask = 1u << bit;
        row.uintValue = isSet ? row.uintValue | mask : row.uintValue & ~mask;
    }

    // every object of the prefab, children included, on the layer
    private static bool SetPrefabLayer(string path, int layer) {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) {
            Debug.LogError("FallingCobraLayerSetup:\tmissing prefab " + path);
            return false;
        }
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try {
            foreach (Transform part in
                root.GetComponentsInChildren<Transform>(true)) {
                part.gameObject.layer = layer;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return true;
    }

    // constants  ##############################################################
    private const string PLAYER_LAYER = "Player";
    private const string COBRA_LAYER = "FallingCobra";
    private const string PLAYER_PREFAB =
        "Assets/_Monkobra/Prefabs/Player/_Monkey.prefab";
    private const string COBRA_PREFAB =
        "Assets/_Monkobra/Prefabs/MobFallingCobra/FallingCobra.prefab";
    private const string TAG_MANAGER = "ProjectSettings/TagManager.asset";
    private const string TIME_MANAGER = "ProjectSettings/TimeManager.asset";
    private const string DYNAMICS_MANAGER =
        "ProjectSettings/DynamicsManager.asset";
    private const int FIRST_USER_LAYER = 6;
    private const float MAX_TIMESTEP_S = 0.1f;
}
