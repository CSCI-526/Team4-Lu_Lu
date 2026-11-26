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

    public Shader GhostShader => ghostShader;

    public Color MonkeyColor => monkeyColor;

    public float MonkeyWidthU => monkeyWidthU;

    public float GhostHaloAlpha => ghostHaloAlpha;

    public float GhostHaloPower => ghostHaloPower;

    public float GhostHaloReachU => ghostHaloReachU;

    /// <summary>World-unit outline width of a tier; u.</summary>
    public float GetWidthU(OutlineTier tier) {
        return widthU[(int)tier];
    }

    /// <summary>Tier color, HSV of the kind's hue at the tier's S and V.</summary>
    public Color GetTierColor(FruitKind kind, OutlineTier tier) {
        int i = (int)tier;
        FruitHue entry = GetEntry(kind);
        // the whitish Reachable tier keeps full value; the others darken
        float v = tier == OutlineTier.Reachable
            ? val[i]
            : val[i] * entry.valueScale;
        return Color.HSVToRGB(entry.hueDeg / 360f, sat[i], v);
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

    /// <summary>
    /// Shared see-through ghost material of a kind; it wears the Reachable
    /// color, or the Grabbable one while the fruit is grabbable.
    /// </summary>
    public Material GetGhostMaterial(FruitKind kind, bool isGrabbable) {
        var key = (kind, isGrabbable);
        if (
            ghostCache.TryGetValue(key, out Material mat)
            && mat != null
        ) {
            return mat;
        }

        if (ghostShader == null) {
            Debug.LogError(
                "FruitOutlineStyle:	must assign Inspector Field: ghostShader",
                this
            );
            return null;
        }

        OutlineTier tier =
            isGrabbable ? OutlineTier.Grabbable : OutlineTier.Reachable;
        mat = new Material(ghostShader) {
            name = $"Outline_Ghost_{kind}_{tier}",
            hideFlags = HideFlags.HideAndDontSave,
        };
        mat.SetColor(GHOST_COLOR, GetTierColor(kind, tier));
        mat.SetFloat(GHOST_HALO_ALPHA, ghostHaloAlpha);
        mat.SetFloat(GHOST_HALO_POWER, ghostHaloPower);
        mat.SetFloat(GHOST_HALO_REACH, ghostHaloReachU);
        ghostCache[key] = mat;
        return mat;
    }

    // Inspector Fields  #######################################################
    [Header("Shader")]
    [SerializeField]
    [Tooltip("world-unit inverted-hull outline shader")]
    private Shader hullShader;

    [SerializeField]
    [Tooltip("depth-Greater see-through ghost shader")]
    private Shader ghostShader;

    [Header("Fruit Tiers (Normal, Reachable, Grabbable)")]
    [SerializeField]
    [Tooltip("outline width per tier, same for every fruit; u")]
    private float[] widthU = { 0.018f, 0.065f, 0.090f };

    [SerializeField]
    [Tooltip("HSV saturation per tier; Reachable is whitish, Grabbable is full")]
    private float[] sat = { 0.45f, 0.18f, 1f };

    [SerializeField]
    [Tooltip("HSV value per tier")]
    private float[] val = { 0.80f, 1f, 1f };

    [Header("Fruit Hues")]
    [SerializeField]
    [Tooltip("hue per fruit kind, deg; valueScale darkens Normal & Grabbable")]
    private FruitHue[] hues = {
        new FruitHue { kind = FruitKind.Banana, hueDeg = 48f, valueScale = 1f },
        new FruitHue { kind = FruitKind.Coconut, hueDeg = 22f, valueScale = 0.65f },
        new FruitHue { kind = FruitKind.Pineapple, hueDeg = 35f, valueScale = 1f },
        new FruitHue { kind = FruitKind.Melon, hueDeg = 345f, valueScale = 1f },
    };

    [Header("Monkey")]
    [SerializeField]
    [Tooltip("always-on monkey outline color, cream")]
    private Color monkeyColor = new Color(1f, 0.949f, 0.824f);

    [SerializeField]
    [Tooltip("monkey outline width; u")]
    private float monkeyWidthU = 0.040f;

    [Header("See-Through Ghost (misty halo, fruit shape cut out)")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("halo alpha right next to the fruit")]
    private float ghostHaloAlpha = 0.55f;

    [SerializeField]
    [Tooltip("halo falloff power; higher = the mist fades sooner")]
    private float ghostHaloPower = 1.5f;

    [SerializeField]
    [Tooltip("how far the halo reaches beyond the fruit; u")]
    private float ghostHaloReachU = 0.12f;

    // private members  ########################################################
    [Serializable]
    private struct FruitHue {
        public FruitKind kind;
        public float hueDeg;
        public float valueScale;
    }

    // names the boss golden fruit code also sets
    private static readonly int OUTLINE_COLOR =
        Shader.PropertyToID("_Outline_Color");
    private static readonly int OUTLINE_WIDTH =
        Shader.PropertyToID("_Outline_Width");

    private static readonly int GHOST_COLOR =
        Shader.PropertyToID("_Ghost_Color");
    private static readonly int GHOST_HALO_ALPHA =
        Shader.PropertyToID("_Ghost_HaloAlpha");
    private static readonly int GHOST_HALO_POWER =
        Shader.PropertyToID("_Ghost_HaloPower");
    private static readonly int GHOST_HALO_REACH =
        Shader.PropertyToID("_Ghost_HaloReach");

    [NonSerialized]
    private readonly Dictionary<(FruitKind, bool), Material> ghostCache =
        new Dictionary<(FruitKind, bool), Material>();

    [NonSerialized]
    private readonly Dictionary<(FruitKind, OutlineTier), Material> cache =
        new Dictionary<(FruitKind, OutlineTier), Material>();

    // private methods  ########################################################
    private FruitHue GetEntry(FruitKind kind) {
        foreach (FruitHue entry in hues) {
            if (entry.kind == kind) {
                return entry;
            }
        }
        Debug.LogWarning(
            $"FruitOutlineStyle:\tno hue for {kind}, using 0",
            this
        );
        return new FruitHue { kind = kind, hueDeg = 0f, valueScale = 1f };
    }
}
