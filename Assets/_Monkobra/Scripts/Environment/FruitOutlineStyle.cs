using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Which fruit an outline is for; picks the hue.</summary>
public enum FruitKind {
    Banana,
    Coconut,
    Pineapple,
    Melon,
}

/// <summary>How strong a fruit outline is; concentration carries it.</summary>
public enum OutlineTier {
    Normal,
    Reachable,
    Grabbable,
}

/// <summary>
/// The one asset to retune every outline: world-unit widths, the tier
/// concentration (S and V), a hue per fruit kind, the monkey's look and the
/// see-through ghost. <see cref="GetTierMaterial"/> builds 1 shared hull
/// material per (fruit kind, tier) and caches it.
/// </summary>
[CreateAssetMenu(
    fileName = "FruitOutlineStyle",
    menuName = "Monkobra/Fruit Outline Style"
)]
public class FruitOutlineStyle: ScriptableObject {
    // Public API  #############################################################
    public Shader HullShader => hullShader;

    public Color MonkeyColor => monkeyColor;

    public float MonkeyWidthU => monkeyWidthU;

    public float GhostFillAlpha => ghostFillAlpha;

    public float GhostRimAlpha => ghostRimAlpha;

    public float GhostRimPower => ghostRimPower;

    public float GhostPushU => ghostPushU;

    /// <summary>World-unit outline width of a tier; u.</summary>
    public float GetWidthU(OutlineTier tier) {
        return widthU[(int)tier];
    }

    /// <summary>Tier color, HSV of the kind's hue at the tier's S and V.</summary>
    public Color GetTierColor(FruitKind kind, OutlineTier tier) {
        int i = (int)tier;
        return Color.HSVToRGB(GetHueDeg(kind) / 360f, sat[i], val[i]);
    }

    /// <summary>
    /// Shared hull material of a kind at a tier; built once, then cached.
    /// </summary>
    public Material GetTierMaterial(FruitKind kind, OutlineTier tier) {
        var key = (kind, tier);
        if (
            cache.TryGetValue(key, out Material mat)
            && mat != null
        ) {
            return mat;
        }

        if (hullShader == null) {
            Debug.LogError(
                "FruitOutlineStyle:\tmust assign Inspector Field: hullShader",
                this
            );
            return null;
        }

        mat = new Material(hullShader) {
            name = $"Outline_{kind}_{tier}",
            hideFlags = HideFlags.HideAndDontSave,
        };
        mat.SetColor(OUTLINE_COLOR, GetTierColor(kind, tier));
        mat.SetFloat(OUTLINE_WIDTH, GetWidthU(tier));
        cache[key] = mat;
        return mat;
    }

    // Inspector Fields  #######################################################
    [Header("Shader")]
    [SerializeField]
    [Tooltip("world-unit inverted-hull outline shader")]
    private Shader hullShader;

    [Header("Fruit Tiers (Normal, Reachable, Grabbable)")]
    [SerializeField]
    [Tooltip("outline width per tier, same for every fruit; u")]
    private float[] widthU = { 0.015f, 0.025f, 0.040f };

    [SerializeField]
    [Tooltip("HSV saturation per tier; lower = paler")]
    private float[] sat = { 0.35f, 0.70f, 1f };

    [SerializeField]
    [Tooltip("HSV value per tier")]
    private float[] val = { 0.85f, 1f, 1f };

    [Header("Fruit Hues")]
    [SerializeField]
    [Tooltip("hue per fruit kind; deg")]
    private FruitHue[] hues = {
        new FruitHue { kind = FruitKind.Banana, hueDeg = 48f },
        new FruitHue { kind = FruitKind.Coconut, hueDeg = 24f },
        new FruitHue { kind = FruitKind.Pineapple, hueDeg = 335f },
        new FruitHue { kind = FruitKind.Melon, hueDeg = 190f },
    };

    [Header("Monkey")]
    [SerializeField]
    [Tooltip("always-on monkey outline color, cream")]
    private Color monkeyColor = new Color(1f, 0.949f, 0.824f);

    [SerializeField]
    [Tooltip("monkey outline width; u")]
    private float monkeyWidthU = 0.025f;

    [Header("See-Through Ghost")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("fill alpha where something nearer covers the fruit")]
    private float ghostFillAlpha = 0.30f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("alpha of the Fresnel rim")]
    private float ghostRimAlpha = 0.90f;

    [SerializeField]
    [Tooltip("Fresnel rim power; higher = thinner rim")]
    private float ghostRimPower = 3f;

    [SerializeField]
    [Tooltip("shell pushed out so the rim hugs the silhouette; u")]
    private float ghostPushU = 0.008f;

    // private members  ########################################################
    [Serializable]
    private struct FruitHue {
        public FruitKind kind;
        public float hueDeg;
    }

    // names the boss golden fruit code also sets
    private static readonly int OUTLINE_COLOR =
        Shader.PropertyToID("_Outline_Color");
    private static readonly int OUTLINE_WIDTH =
        Shader.PropertyToID("_Outline_Width");

    [NonSerialized]
    private readonly Dictionary<(FruitKind, OutlineTier), Material> cache =
        new Dictionary<(FruitKind, OutlineTier), Material>();

    // private methods  ########################################################
    private float GetHueDeg(FruitKind kind) {
        foreach (FruitHue entry in hues) {
            if (entry.kind == kind) {
                return entry.hueDeg;
            }
        }
        Debug.LogWarning(
            $"FruitOutlineStyle:\tno hue for {kind}, using 0",
            this
        );
        return 0f;
    }
}
