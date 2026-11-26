using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu <c>Monkobra > Build > Outline Materials</c>: writes the
/// <see cref="FruitOutlineStyle"/> asset with the Design Spec numbers and the
/// shared monkey hull material. Safe to run again: existing assets are kept
/// and only their shader and monkey look are refreshed.
/// </summary>
public static class OutlineMaterialBuilder {
    // constants  ##############################################################
    private const string BUILD_MENU = "Monkobra/Build/Outline Materials";

    private const string ROOT = "Assets/_Monkobra/";
    private const string SHADER_PATH = ROOT + "Shaders/Outline/OutlineHull.shader";
    private const string STYLE_PATH =
        ROOT + "Settings/FruitOutline/FruitOutlineStyle.asset";
    private const string MASK_SHADER_PATH =
        ROOT + "Shaders/Outline/OutlineMask.shader";
    private const string GHOST_SHADER_PATH =
        ROOT + "Shaders/Outline/OutlineSeeThrough.shader";
    // base the boss golden fruit clones; its width is the Grabbable one
    private const string GOLDEN_BASE_MAT_PATH =
        ROOT + "Materials/Outline/Outline_Banana_Grabbable.mat";
    private const string MONKEY_MASK_MAT_PATH =
        ROOT + "Materials/Outline/Outline_MonkeyMask.mat";
    // stencil bit of the monkey; the fruit use 8, the ghost halo 4
    private const float MONKEY_STENCIL_BIT = 16f;
    private const string MONKEY_MAT_PATH =
        ROOT + "Materials/Outline/Outline_Monkey.mat";

    // Menu  ###################################################################
    [MenuItem(BUILD_MENU, false, 50)]
    public static void Build() {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(SHADER_PATH);
        if (shader == null) {
            Debug.LogError(
                $"OutlineMaterialBuilder:\tfail to load Shader: {SHADER_PATH}"
            );
            return;
        }

        FruitOutlineStyle style = LoadOrCreateStyle(shader);
        BuildMonkeyMaterial(shader, style);
        BuildMonkeyMaskMaterial(style);
        BuildGhostMaterials(style);
        BuildGoldenBaseMaterial(shader, style);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("OutlineMaterialBuilder:\toutline materials built");
    }

    // private methods  ########################################################
    private static FruitOutlineStyle LoadOrCreateStyle(Shader shader) {
        var style = AssetDatabase.LoadAssetAtPath<FruitOutlineStyle>(STYLE_PATH);
        if (style == null) {
            EnsureFolder(Path.GetDirectoryName(STYLE_PATH));
            style = ScriptableObject.CreateInstance<FruitOutlineStyle>();
            AssetDatabase.CreateAsset(style, STYLE_PATH);
        }

        var serialized = new SerializedObject(style);
        serialized.FindProperty("hullShader").objectReferenceValue = shader;
        serialized.FindProperty("maskShader").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Shader>(MASK_SHADER_PATH);
        serialized.FindProperty("ghostShader").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Shader>(GHOST_SHADER_PATH);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(style);
        return style;
    }

    private static void BuildMonkeyMaterial(
        Shader shader, FruitOutlineStyle style
    ) {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MONKEY_MAT_PATH);
        if (mat == null) {
            EnsureFolder(Path.GetDirectoryName(MONKEY_MAT_PATH));
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MONKEY_MAT_PATH);
        }
        mat.shader = shader;
        mat.SetColor("_Outline_Color", style.MonkeyColor);
        mat.SetFloat("_Outline_Width", style.MonkeyWidthU);
        mat.SetFloat("_Outline_StencilBit", MONKEY_STENCIL_BIT);
        EditorUtility.SetDirty(mat);
    }

    // the monkey's own mask, on its own stencil bit so a fruit and the
    // monkey never hide each other's outline
    private static void BuildMonkeyMaskMaterial(FruitOutlineStyle style) {
        if (style.MaskShader == null) {
            Debug.LogError(
                "OutlineMaterialBuilder:\tfail to load Shader: "
                    + MASK_SHADER_PATH
            );
            return;
        }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(
            MONKEY_MASK_MAT_PATH
        );
        if (mat == null) {
            mat = new Material(style.MaskShader);
            AssetDatabase.CreateAsset(mat, MONKEY_MASK_MAT_PATH);
        }
        mat.shader = style.MaskShader;
        mat.SetFloat("_Outline_StencilBit", MONKEY_STENCIL_BIT);
        EditorUtility.SetDirty(mat);
    }

    private static void BuildGoldenBaseMaterial(
        Shader shader, FruitOutlineStyle style
    ) {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(GOLDEN_BASE_MAT_PATH);
        if (mat == null) {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, GOLDEN_BASE_MAT_PATH);
        }
        mat.shader = shader;
        mat.SetColor(
            "_Outline_Color",
            style.GetTierColor(FruitKind.Banana, OutlineTier.Grabbable)
        );
        mat.SetFloat("_Outline_Width", style.GetWidthU(OutlineTier.Grabbable));
        EditorUtility.SetDirty(mat);
    }

    // 1 shared ghost material per fruit kind, in its Reachable color
    private static void BuildGhostMaterials(FruitOutlineStyle style) {
        var ghostShader = AssetDatabase.LoadAssetAtPath<Shader>(
            GHOST_SHADER_PATH
        );
        if (ghostShader == null) {
            Debug.LogError(
                "OutlineMaterialBuilder:	fail to load Shader: "
                    + GHOST_SHADER_PATH
            );
            return;
        }

        foreach (FruitKind kind in System.Enum.GetValues(typeof(FruitKind))) {
            string path = ROOT + $"Materials/Outline/Outline_Ghost_{kind}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) {
                mat = new Material(ghostShader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = ghostShader;
            mat.SetColor(
                "_Ghost_Color", style.GetTierColor(kind, OutlineTier.Reachable)
            );
            mat.SetFloat("_Ghost_HaloAlpha", style.GhostHaloAlpha);
            mat.SetFloat("_Ghost_HaloSigma", style.GhostHaloSigma);
            mat.SetFloat("_Ghost_HaloReach", style.GhostHaloReachU);
            EditorUtility.SetDirty(mat);
        }
    }

    // makes each missing folder of an Assets/ path, parent first
    private static void EnsureFolder(string path) {
        path = path.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(path)) {
            return;
        }
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
