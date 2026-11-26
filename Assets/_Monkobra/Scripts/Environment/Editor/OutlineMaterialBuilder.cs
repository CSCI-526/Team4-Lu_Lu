using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu <c>Monkobra > Build Outline Materials</c>: writes the
/// <see cref="FruitOutlineStyle"/> asset with the Design Spec numbers and the
/// shared monkey hull material. Safe to run again: existing assets are kept
/// and only their shader and monkey look are refreshed.
/// Menu <c>Monkobra > Check Outline Style</c>: logs PASS or FAIL for
/// <see cref="FruitOutlineStyle.GetTierMaterial"/> of every fruit and tier.
/// </summary>
public static class OutlineMaterialBuilder {
    // constants  ##############################################################
    private const string BUILD_MENU = "Monkobra/Build Outline Materials";
    private const string CHECK_MENU = "Monkobra/Check Outline Style";

    private const string ROOT = "Assets/_Monkobra/";
    private const string SHADER_PATH = ROOT + "Shaders/Outline/OutlineHull.shader";
    private const string STYLE_PATH =
        ROOT + "Settings/FruitOutline/FruitOutlineStyle.asset";
    private const string GHOST_SHADER_PATH =
        ROOT + "Shaders/Outline/OutlineSeeThrough.shader";
    // base the boss golden fruit clones; its width is the Grabbable one
    private const string GOLDEN_BASE_MAT_PATH =
        ROOT + "Materials/Outline/Outline_Banana_Grabbable.mat";
    private const string MONKEY_MAT_PATH =
        ROOT + "Materials/Outline/Outline_Monkey.mat";

    // tolerance of the hex check; 1 byte of 255
    private const float COLOR_EPS = 1.5f / 255f;
    private const float WIDTH_EPS = 1e-5f;

    // Menu  ###################################################################
    [MenuItem(BUILD_MENU)]
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
        BuildGhostMaterials(style);
        BuildGoldenBaseMaterial(shader, style);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("OutlineMaterialBuilder:\toutline materials built");
    }

    [MenuItem(CHECK_MENU)]
    public static void Check() {
        var style = AssetDatabase.LoadAssetAtPath<FruitOutlineStyle>(STYLE_PATH);
        if (style == null) {
            Debug.LogError($"Outline style check FAIL: no asset {STYLE_PATH}");
            return;
        }

        int failCount = 0;
        failCount += CheckKind(style, FruitKind.Banana,
            "#CCBA70", "#FFF6D1", "#FFCC00");
        failCount += CheckKind(style, FruitKind.Coconut,
            "#CC9570", "#FFE3D1", "#FF6600");
        failCount += CheckKind(style, FruitKind.Pineapple,
            "#CC7096", "#FFD1E4", "#FF006A");

        if (failCount == 0) {
            Debug.Log("Outline style check PASS: 9 tier materials match");
        } else {
            Debug.LogError($"Outline style check FAIL: {failCount} mismatch");
        }
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
            mat.SetFloat("_Ghost_HaloPower", style.GhostHaloPower);
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

    private static int CheckKind(
        FruitOutlineStyle style, FruitKind kind,
        string normalHex, string reachableHex, string grabbableHex
    ) {
        string[] hexes = { normalHex, reachableHex, grabbableHex };
        float[] widths = { 0.018f, 0.065f, 0.090f };
        int failCount = 0;
        for (int i = 0; i < 3; i++) {
            var tier = (OutlineTier)i;
            Material mat = style.GetTierMaterial(kind, tier);
            ColorUtility.TryParseHtmlString(hexes[i], out Color want);
            Color got = mat.GetColor("_Outline_Color");
            float width = mat.GetFloat("_Outline_Width");
            bool isColorOk =
                Mathf.Abs(got.r - want.r) < COLOR_EPS
                && Mathf.Abs(got.g - want.g) < COLOR_EPS
                && Mathf.Abs(got.b - want.b) < COLOR_EPS;
            bool isWidthOk = Mathf.Abs(width - widths[i]) < WIDTH_EPS;
            if (!isColorOk || !isWidthOk) {
                failCount++;
                Debug.LogError(
                    $"Outline style check: {kind} {tier} got "
                        + $"#{ColorUtility.ToHtmlStringRGB(got)} {width}, "
                        + $"want {hexes[i]} {widths[i]}"
                );
            }
        }
        return failCount;
    }
}
