using System.Collections.Generic;
using TMPro;
using UnityEditor;

/// <summary>
/// The boss attack signs' font, for <c>Monkobra/Build/Warning Font</c>: makes
/// the Impact font asset the sign's "!" and word wear (see
/// <see cref="BossFightHudConfig"/>). It uses the same Latin set, atlas and
/// keep-what-exists rule as the UI fonts.
/// </summary>
public static partial class UIStyleBuilder {
    // Public Members  #########################################################
    internal static TMP_FontAsset Impact => Load<TMP_FontAsset>(IMPACT_SDF);

    // private methods  ########################################################
    [MenuItem("Monkobra/Build/Warning Font", false, 12)]
    private static void BuildWarningFont() {
        TMP_FontAsset impact = BuildFont(IMPACT_TTF, IMPACT_SDF);
        if (impact == null) {
            return;
        }

        // a glyph outside the Latin set falls back as the UI fonts do
        TMP_FontAsset fallback = Load<TMP_FontAsset>(FALLBACK_SDF);
        if (impact.fallbackFontAssetTable == null) {
            impact.fallbackFontAssetTable = new List<TMP_FontAsset>();
        }
        if (fallback != null
            && !impact.fallbackFontAssetTable.Contains(fallback)) {
            impact.fallbackFontAssetTable.Add(fallback);
        }
        EditorUtility.SetDirty(impact);
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log(
            "UIStyleBuilder:\twarning font ready, " + impact.name
        );
    }

    // private members  ########################################################
    private const string IMPACT_FONTS = "Assets/_Monkobra/Fonts/Impact/";
    private const string IMPACT_TTF = IMPACT_FONTS + "impact.ttf";
    internal const string IMPACT_SDF = IMPACT_FONTS + "impact SDF.asset";
}
