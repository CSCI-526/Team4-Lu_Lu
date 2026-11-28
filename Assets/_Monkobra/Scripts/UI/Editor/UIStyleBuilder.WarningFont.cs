using System.Collections.Generic;
using TMPro;
using UnityEditor;

/// <summary>
/// The boss attack signs' font, for <c>Monkobra/Build/Warning Font</c>: makes
/// the Anton font asset the sign's "!" and word wear (see
/// <see cref="BossFightHudConfig"/>). It uses the same Latin set, atlas and
/// keep-what-exists rule as the UI fonts.
/// </summary>
public static partial class UIStyleBuilder {
    // Public Members  #########################################################
    internal static TMP_FontAsset Anton => Load<TMP_FontAsset>(ANTON_SDF);

    // private methods  ########################################################
    [MenuItem("Monkobra/Build/Warning Font", false, 12)]
    private static void BuildWarningFont() {
        TMP_FontAsset anton = BuildFont(ANTON_TTF, ANTON_SDF);
        if (anton == null) {
            return;
        }

        // a glyph outside the Latin set falls back as the UI fonts do
        TMP_FontAsset fallback = Load<TMP_FontAsset>(FALLBACK_SDF);
        if (anton.fallbackFontAssetTable == null) {
            anton.fallbackFontAssetTable = new List<TMP_FontAsset>();
        }
        if (fallback != null
            && !anton.fallbackFontAssetTable.Contains(fallback)) {
            anton.fallbackFontAssetTable.Add(fallback);
        }
        EditorUtility.SetDirty(anton);
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log(
            "UIStyleBuilder:\twarning font ready, " + anton.name
        );
    }

    // private members  ########################################################
    private const string ANTON_FONTS = "Assets/_Monkobra/Fonts/Anton/";
    private const string ANTON_TTF = ANTON_FONTS + "Anton-Regular.ttf";
    internal const string ANTON_SDF = ANTON_FONTS + "Anton-Regular SDF.asset";
}
