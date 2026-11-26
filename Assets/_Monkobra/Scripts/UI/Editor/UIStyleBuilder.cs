using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Applies the logo-style UI (docs/ui-style-doc.md) through Unity, never by
/// hand-editing YAML: builds the Baloo 2 font assets and the Title material,
/// makes Baloo 2 the TMP default, builds the MenuButton and IconButton
/// prefabs, and holds the styling helpers the Title Screen and Intro Comic
/// builders share. Each step can run again; a step keeps what it already
/// made.
/// </summary>
public static partial class UIStyleBuilder {
    // Public Members  #########################################################
    internal static TMP_FontAsset SemiBold => Load<TMP_FontAsset>(SEMIBOLD_SDF);
    internal static TMP_FontAsset ExtraBold =>
        Load<TMP_FontAsset>(EXTRABOLD_SDF);
    internal static Material TitleMaterial => Load<Material>(TITLE_MATERIAL);

    // a logo-style label: font, its own material, size, color
    internal static void StyleText(
        TMP_Text text, TMP_FontAsset font, float size, Color color,
        Material material = null
    ) {
        text.font = font;
        text.fontSharedMaterial = material != null ? material : font.material;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.fontStyle = FontStyles.Normal;
        text.color = color;
    }

    // an animated input icon's press clip writes its scale (1 to 0.92 and
    // back), so a size set by scale holds in the Editor but snaps back in
    // Play; the same size goes into its rect, at scale 1, so it lasts
    internal static void SizeAnimatedIcons(Transform parent) {
        foreach (Animator animator in parent.GetComponentsInChildren<Animator>(
            true
        )) {
            RectTransform icon = animator.transform as RectTransform;
            Vector3 scale = icon != null ? icon.localScale : Vector3.one;
            if (Mathf.Approximately(scale.x, 1f)
                && Mathf.Approximately(scale.y, 1f)) {
                continue;
            }
            icon.sizeDelta = new Vector2(
                icon.sizeDelta.x * scale.x, icon.sizeDelta.y * scale.y
            );
            icon.localScale = Vector3.one;
        }
    }

    // private methods  ########################################################
    // fonts  ------------------------------------------------------------------
    [MenuItem("Monkobra/Build/UI Fonts", false, 10)]
    private static void BuildFonts() {
        TMP_FontAsset semi = BuildFont(SEMIBOLD_TTF, SEMIBOLD_SDF);
        TMP_FontAsset extra = BuildFont(EXTRABOLD_TTF, EXTRABOLD_SDF);
        if (semi == null || extra == null) {
            return;
        }

        // a Bold style on SemiBold text draws the real ExtraBold glyphs
        semi.fontWeightTable[7].regularTypeface = extra;
        TMP_FontAsset fallback = Load<TMP_FontAsset>(FALLBACK_SDF);
        foreach (TMP_FontAsset font in new[] { semi, extra }) {
            // a font asset made in code starts without the list
            if (font.fallbackFontAssetTable == null) {
                font.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }
            if (fallback != null
                && !font.fallbackFontAssetTable.Contains(fallback)) {
                font.fallbackFontAssetTable.Add(fallback);
            }
            EditorUtility.SetDirty(font);
        }

        if (Load<Material>(TITLE_MATERIAL) == null) {
            Material title = new Material(extra.material) {
                name = System.IO.Path.GetFileNameWithoutExtension(
                    TITLE_MATERIAL
                ),
                shader = Shader.Find("TextMeshPro/Distance Field"),
            };
            title.SetColor(ShaderUtilities.ID_OutlineColor, UIStyle.Brown);
            title.SetFloat(ShaderUtilities.ID_OutlineWidth, TITLE_OUTLINE);
            title.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            title.SetColor(ShaderUtilities.ID_UnderlayColor, UIStyle.Shadow);
            title.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, TITLE_SHADOW.x);
            title.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, TITLE_SHADOW.y);
            title.SetFloat(ShaderUtilities.ID_UnderlayDilate, TITLE_OUTLINE);
            title.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
            AssetDatabase.CreateAsset(title, TITLE_MATERIAL);
        }

        // texts made in code without a font (boss fight HUD, key prompts)
        // take the TMP default
        TMP_Settings.defaultFontAsset = semi;
        EditorUtility.SetDirty(TMP_Settings.instance);
        AssetDatabase.SaveAssets();
        Debug.Log("UIStyleBuilder:\tfonts ready, TMP default is " + semi.name);
    }

    // a static atlas of the Latin set only, so a WebGL build stays small
    private static TMP_FontAsset BuildFont(string ttfPath, string assetPath) {
        TMP_FontAsset kept = Load<TMP_FontAsset>(assetPath);
        if (kept != null) {
            return kept; // a rebuild would change its GUID & break references
        }
        Font source = Load<Font>(ttfPath);
        if (source == null) {
            Debug.LogError("UIStyleBuilder:\tmissing font " + ttfPath);
            return null;
        }

        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(
            source, SAMPLING_SIZE, ATLAS_PADDING, GlyphRenderMode.SDFAA,
            ATLAS_SIZE, ATLAS_SIZE, AtlasPopulationMode.Dynamic, false
        );
        font.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (!font.TryAddCharacters(LatinSet(), out string missing)) {
            Debug.LogWarning(
                "UIStyleBuilder:\t" + font.name + " left out: " + missing
                    + " (raise ATLAS_SIZE or lower SAMPLING_SIZE)"
            );
        }
        font.atlasPopulationMode = AtlasPopulationMode.Static;

        AssetDatabase.CreateAsset(font, assetPath);
        Texture2D atlas = font.atlasTextures[0];
        atlas.name = font.name + " Atlas";
        AssetDatabase.AddObjectToAsset(atlas, font);
        font.material.name = font.name + " Material";
        AssetDatabase.AddObjectToAsset(font.material, font);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        return font;
    }

    private static string LatinSet() {
        StringBuilder chars = new StringBuilder();
        for (int c = 0x20; c < 0x7F; c++) {
            chars.Append((char)c);
        }
        for (int c = 0xA0; c <= 0xFF; c++) {
            chars.Append((char)c);
        }
        chars.Append("–—‘’“”•…€™");
        return chars.ToString();
    }

    // button prefabs  ---------------------------------------------------------
    [MenuItem("Monkobra/Build/UI Button Prefabs", false, 11)]
    private static void BuildButtons() {
        if (!AreFontsReady()) {
            return;
        }
        BuildButtonPrefab(MENU_BUTTON_PREFAB, "MenuButton", false);
        BuildButtonPrefab(ICON_BUTTON_PREFAB, "IconButton", true);
        Debug.Log("UIStyleBuilder:\tbuilt MenuButton and IconButton prefabs");
    }

    private static void BuildButtonPrefab(
        string path, string spriteSet, bool isIcon
    ) {
        GameObject root = new GameObject(
            System.IO.Path.GetFileNameWithoutExtension(path),
            typeof(RectTransform)
        );
        try {
            root.layer = UI_LAYER;
            root.AddComponent<CanvasRenderer>();
            root.AddComponent<Image>();
            root.AddComponent<Button>();
            root.AddComponent<MenuButtonHover>();
            if (isIcon) {
                StyleIconButton(root.GetComponent<Button>());
            } else {
                Image fill = root.GetComponent<Image>();
                TextMeshProUGUI label = NewChild("Label", root.transform)
                    .gameObject.AddComponent<TextMeshProUGUI>();
                label.text = "Button";
                StyleMenuButton(root.GetComponent<Button>());
                fill.raycastTarget = true;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally {
            Object.DestroyImmediate(root);
        }
    }

    // a long button in place: sprites, size, label, lock, feel
    internal static void StyleMenuButton(Button button) {
        RectTransform rect = (RectTransform)button.transform;
        rect.sizeDelta = new Vector2(
            MENU_FACE.x + MARGIN_LEFT + MARGIN_RIGHT,
            MENU_FACE.y + MARGIN_TOP + MARGIN_BOTTOM
        );
        ApplySprites(button, "MenuButton");

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) {
            StyleText(label, ExtraBold, BUTTON_SIZE, UIStyle.Brown);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            FitFace(label.rectTransform);
        }

        RectTransform lockIcon = FindOrCreate("LockIcon", button.transform);
        Image lockImage = GetOrAdd<Image>(lockIcon.gameObject);
        lockImage.sprite = Load<Sprite>(SPRITES + "Icon_Lock.png");
        lockImage.color = UIStyle.LockedInk;
        lockImage.raycastTarget = false;
        lockIcon.anchorMin = lockIcon.anchorMax = new Vector2(0.5f, 0.5f);
        lockIcon.sizeDelta = new Vector2(LOCK_SIZE, LOCK_SIZE);
        lockIcon.anchoredPosition = FaceCenter();
        lockIcon.gameObject.SetActive(false);

        MenuButtonPop pop = GetOrAdd<MenuButtonPop>(button.gameObject);
        SerializedObject so = new SerializedObject(pop);
        so.FindProperty("content").objectReferenceValue =
            label != null ? label.rectTransform : null;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("lockIcon").objectReferenceValue = lockIcon;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // a square button in place: sprites, size, glyph, feel
    internal static void StyleIconButton(
        Button button, string glyphSprite = "Icon_Resume"
    ) {
        RectTransform rect = (RectTransform)button.transform;
        rect.sizeDelta = new Vector2(
            ICON_FACE + MARGIN_LEFT + MARGIN_RIGHT,
            ICON_FACE + MARGIN_TOP + MARGIN_BOTTOM
        );
        ApplySprites(button, "IconButton");

        RectTransform glyph = FindOrCreate("Glyph", button.transform);
        Image glyphImage = GetOrAdd<Image>(glyph.gameObject);
        glyphImage.sprite = Load<Sprite>(SPRITES + glyphSprite + ".png");
        glyphImage.color = UIStyle.Brown;
        glyphImage.raycastTarget = false;
        glyph.anchorMin = glyph.anchorMax = new Vector2(0.5f, 0.5f);
        glyph.sizeDelta = new Vector2(GLYPH_SIZE, GLYPH_SIZE);
        glyph.anchoredPosition = FaceCenter();

        MenuButtonPop pop = GetOrAdd<MenuButtonPop>(button.gameObject);
        SerializedObject so = new SerializedObject(pop);
        so.FindProperty("content").objectReferenceValue = glyph;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // the four states are a Sprite Swap: selected doubles as highlighted,
    // since a hover selects (MenuButtonHover)
    private static void ApplySprites(Button button, string set) {
        Image image = button.GetComponent<Image>();
        image.sprite = Load<Sprite>(SPRITES + set + "_Normal.png");
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = Color.white;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        Sprite selected = Load<Sprite>(SPRITES + set + "_Selected.png");
        button.spriteState = new SpriteState {
            highlightedSprite = selected,
            selectedSprite = selected,
            pressedSprite = Load<Sprite>(SPRITES + set + "_Pressed.png"),
            disabledSprite = Load<Sprite>(SPRITES + set + "_Locked.png"),
        };
    }

    // the label sits on the face, inside the ring & shadow margins
    private static void FitFace(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(MARGIN_LEFT, MARGIN_BOTTOM);
        rect.offsetMax = new Vector2(-MARGIN_RIGHT, -MARGIN_TOP);
    }

    private static Vector2 FaceCenter() {
        return new Vector2(
            (MARGIN_LEFT - MARGIN_RIGHT) / 2f, (MARGIN_BOTTOM - MARGIN_TOP) / 2f
        );
    }

    // helpers  ----------------------------------------------------------------
    private static bool AreFontsReady() {
        if (SemiBold != null && ExtraBold != null && TitleMaterial != null) {
            return true;
        }
        Debug.LogError(
            "UIStyleBuilder:\trun Monkobra/Build/UI Fonts first"
        );
        return false;
    }

    private static T Load<T>(string path) where T : Object {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component {
        T found = go.GetComponent<T>();
        return found != null ? found : go.AddComponent<T>();
    }

    private static RectTransform NewChild(string childName, Transform parent) {
        GameObject go = new GameObject(childName, typeof(RectTransform));
        go.layer = UI_LAYER;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static RectTransform FindOrCreate(string childName,
        Transform parent) {
        Transform found = parent.Find(childName);
        return found != null ? (RectTransform)found
            : NewChild(childName, parent);
    }

    // constants  ##############################################################
    private const int UI_LAYER = 5;

    // fonts: 2 weights, Latin set, one 1024 atlas each
    private const string FONTS = "Assets/_Monkobra/Fonts/Baloo2/";
    private const string SEMIBOLD_TTF = FONTS + "Baloo2-SemiBold.ttf";
    private const string EXTRABOLD_TTF = FONTS + "Baloo2-ExtraBold.ttf";
    internal const string SEMIBOLD_SDF = FONTS + "Baloo2-SemiBold SDF.asset";
    internal const string EXTRABOLD_SDF = FONTS + "Baloo2-ExtraBold SDF.asset";
    private const string TITLE_MATERIAL =
        FONTS + "Baloo2-ExtraBold SDF - Title.mat";
    private const string FALLBACK_SDF =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans "
            + "SDF.asset";
    private const int SAMPLING_SIZE = 48;
    private const int ATLAS_PADDING = 8;
    private const int ATLAS_SIZE = 1024;

    // the Title look: dark brown outline, brown shadow down-right, in the
    // material's normalized units
    private const float TITLE_OUTLINE = 0.3f;
    private static readonly Vector2 TITLE_SHADOW = new Vector2(0.6f, -0.7f);

    // type scale, 1280x720 canvas units; see docs/ui-style-doc.md
    internal const float TITLE_SIZE = 64f;
    internal const float BUTTON_SIZE = 34f;
    internal const float BODY_SIZE = 30f;

    // buttons: the face, plus the ring & shadow margins baked in the sprites
    private const string SPRITES = "Assets/_Monkobra/Sprites/UI/";
    private static readonly Vector2 MENU_FACE = new Vector2(420f, 70f);
    private const float ICON_FACE = 64f;
    private const float GLYPH_SIZE = 36f;
    private const float LOCK_SIZE = 24f;
    private const float MARGIN_LEFT = 8f;
    private const float MARGIN_TOP = 8f;
    private const float MARGIN_RIGHT = 13f;
    private const float MARGIN_BOTTOM = 14f;

    private const string MENU_BUTTON_PREFAB =
        "Assets/_Monkobra/Prefabs/UI/MenuButton.prefab";
    private const string ICON_BUTTON_PREFAB =
        "Assets/_Monkobra/Prefabs/UI/IconButton.prefab";
}
