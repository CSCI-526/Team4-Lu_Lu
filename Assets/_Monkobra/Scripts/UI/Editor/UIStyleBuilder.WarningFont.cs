using System.Collections.Generic;
using TMPro;
using UnityEditor;

/// <summary>
/// The boss attack signs' font, for <c>Monkobra/Build/Warning Font</c>: makes
/// the Anton font asset the sign's "!" and word wear, and the Creepster one
/// the boss name wears (see <see cref="BossFightHudConfig"/>), and assigns
/// both to every HUD config. It uses the same Latin set, atlas and
/// keep-what-exists rule as the UI fonts.
/// </summary>
public static partial class UIStyleBuilder {
    // Public Members  #########################################################
    internal static TMP_FontAsset Anton => Load<TMP_FontAsset>(ANTON_SDF);
    internal static TMP_FontAsset Creepster =>
        Load<TMP_FontAsset>(CREEPSTER_SDF);

    // private methods  ########################################################
    [MenuItem("Monkobra/Build/Warning Font", false, 12)]
    private static void BuildWarningFont() {
        TMP_FontAsset anton = BuildFont(ANTON_TTF, ANTON_SDF);
        TMP_FontAsset creepster = BuildFont(CREEPSTER_TTF, CREEPSTER_SDF);
        if (anton == null || creepster == null) {
            return;
        }

        // a glyph outside the Latin set falls back as the UI fonts do
        TMP_FontAsset fallback = Load<TMP_FontAsset>(FALLBACK_SDF);
        foreach (TMP_FontAsset font in new[] { anton, creepster }) {
            if (font.fallbackFontAssetTable == null) {
                font.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }
            if (fallback != null
                && !font.fallbackFontAssetTable.Contains(fallback)) {
                font.fallbackFontAssetTable.Add(fallback);
            }
            EditorUtility.SetDirty(font);
        }
        WireHudConfigs(anton, creepster);
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log(
            "UIStyleBuilder:	warning fonts ready, " + anton.name + " & "
                + creepster.name
        );
    }

    // both HUD configs wear the fonts, so no Inspector drag is needed
    private static void WireHudConfigs(
        TMP_FontAsset anton, TMP_FontAsset creepster
    ) {
        foreach (string guid in AssetDatabase.FindAssets(
            "t:BossFightHudConfig"
        )) {
            BossFightHudConfig config = AssetDatabase.LoadAssetAtPath<
                BossFightHudConfig
            >(AssetDatabase.GUIDToAssetPath(guid));
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("warningFont").objectReferenceValue = anton;
            so.FindProperty("bossNameFont").objectReferenceValue = creepster;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // private members  ########################################################
    private const string ANTON_FONTS = "Assets/_Monkobra/Fonts/Anton/";
    private const string ANTON_TTF = ANTON_FONTS + "Anton-Regular.ttf";
    internal const string ANTON_SDF = ANTON_FONTS + "Anton-Regular SDF.asset";

    private const string CREEPSTER_FONTS = "Assets/_Monkobra/Fonts/Creepster/";
    private const string CREEPSTER_TTF =
        CREEPSTER_FONTS + "Creepster-Regular.ttf";
    internal const string CREEPSTER_SDF =
        CREEPSTER_FONTS + "Creepster-Regular SDF.asset";
}
