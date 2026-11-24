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
/// prefabs, and restyles the level selection in place, so every script
/// reference and scene override survives. Each step can run again; a step
/// keeps what it already made.
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

    // a ControlStyleSwitch picks its group when it is enabled, so the Editor
    // shows every group at once, one row over another; leave the keyboard
    // group showing, the one a run starts with on a desktop
    internal static void ShowKeyboardGroups(GameObject root) {
        foreach (ControlStyleSwitch styleSwitch in
            root.GetComponentsInChildren<ControlStyleSwitch>(true)) {
            SerializedObject so = new SerializedObject(styleSwitch);
            SetGroupActive(so.FindProperty("keyboardObjects"), true);
            SetGroupActive(so.FindProperty("touchObjects"), false);
        }
    }

    private static void SetGroupActive(SerializedProperty group, bool isOn) {
        for (int i = 0; group != null && i < group.arraySize; i++) {
            GameObject member =
                group.GetArrayElementAtIndex(i).objectReferenceValue
                    as GameObject;
            if (member != null) {
                member.SetActive(isOn);
            }
        }
    }

    // private methods  ########################################################
    [MenuItem("Monkobra/UI Style/Build All (Fonts, Buttons, Level Select)")]
    private static void BuildAll() {
        BuildFonts();
        BuildButtons();
        StyleLevelSelect();
    }

    // fonts  ------------------------------------------------------------------
    [MenuItem("Monkobra/UI Style/1. Build Fonts")]
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
    [MenuItem("Monkobra/UI Style/2. Build Button Prefabs")]
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

    // level selection  --------------------------------------------------------
    [MenuItem("Monkobra/UI Style/3. Style Level Select")]
    private static void StyleLevelSelect() {
        if (!AreFontsReady()) {
            return;
        }
        GameObject root = PrefabUtility.LoadPrefabContents(LEVEL_SELECT);
        try {
            Transform panel = root.transform.Find("Panel");
            Transform buttons = panel == null ? null : panel.Find("Buttons");
            if (buttons == null) {
                Debug.LogError(
                    "UIStyleBuilder:\tno Panel/Buttons in " + LEVEL_SELECT
                );
                return;
            }

            // the loading page's vines, behind everything
            TitleScreenLayoutBuilder.BuildVines(panel).SetSiblingIndex(0);

            TMP_Text heading = panel.Find("Heading")?.GetComponent<TMP_Text>();
            if (heading != null) {
                StyleText(
                    heading, ExtraBold, TITLE_SIZE, UIStyle.Cream, TitleMaterial
                );
            }

            foreach (Button button in buttons.GetComponentsInChildren<Button>(
                true
            )) {
                StyleMenuButton(button);
            }

            // the unlock hint goes right under the Endless button, inside
            // the column, so it never overlaps a button
            Transform endless = buttons.Find("EndlessModeButton");
            Transform hint = panel.Find("EndlessLockedHint")
                ?? buttons.Find("EndlessLockedHint");
            if (hint != null && endless != null) {
                hint.SetParent(buttons, false);
                hint.SetSiblingIndex(endless.GetSiblingIndex() + 1);
                RectTransform hintRect = (RectTransform)hint;
                hintRect.sizeDelta = new Vector2(
                    MENU_FACE.x + MARGIN_LEFT + MARGIN_RIGHT, HINT_HEIGHT
                );
                TMP_Text hintText = hint.GetComponent<TMP_Text>();
                StyleText(hintText, SemiBold, HINT_SIZE, UIStyle.Cream);
                hintText.text = "Defeat Cobra King to Unlock";
                hintText.alignment = TextAlignmentOptions.Center;
                hintText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            // the column: faces 20 apart, the margins already add 22
            VerticalLayoutGroup column =
                buttons.GetComponent<VerticalLayoutGroup>();
            column.spacing = BUTTON_GAP - (MARGIN_TOP + MARGIN_BOTTOM);
            float height = 0f;
            int count = 0;
            foreach (RectTransform child in buttons) {
                height += child.sizeDelta.y;
                count++;
            }
            ((RectTransform)buttons).sizeDelta = new Vector2(
                MENU_FACE.x + MARGIN_LEFT + MARGIN_RIGHT,
                height + column.spacing * (count - 1)
            );
            ShowKeyboardGroups(root);

            PrefabUtility.SaveAsPrefabAsset(root, LEVEL_SELECT);
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }

        StyleHint(HINT_KEYBOARD);
        StyleHint(HINT_TOUCH);
        Debug.Log("UIStyleBuilder:\tstyled the level selection");
    }

    // a key hint row: keycaps, Baloo 2 words sized to fit
    private static void StyleHint(string path) {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try {
            HorizontalLayoutGroup row =
                root.GetComponent<HorizontalLayoutGroup>();
            if (row != null) {
                row.spacing = HINT_ROW_GAP;
            }

            // W and S side by side, in line with the words
            Transform keys = root.transform.Find("Keys_WS");
            if (keys != null) {
                VerticalLayoutGroup stack =
                    keys.GetComponent<VerticalLayoutGroup>();
                if (stack != null) {
                    Object.DestroyImmediate(stack);
                }
                HorizontalLayoutGroup pair = GetOrAdd<HorizontalLayoutGroup>(
                    keys.gameObject
                );
                pair.spacing = KEY_PAIR_GAP;
                pair.childAlignment = TextAnchor.MiddleCenter;
                pair.childControlWidth = pair.childControlHeight = false;
                pair.childForceExpandWidth = pair.childForceExpandHeight =
                    false;
                float keyWidth = 0f;
                foreach (RectTransform key in keys) {
                    keyWidth += key.sizeDelta.x;
                }
                ((RectTransform)keys).sizeDelta = new Vector2(
                    keyWidth + KEY_PAIR_GAP * (keys.childCount - 1),
                    ((RectTransform)keys.GetChild(0)).sizeDelta.y
                );
            }

            foreach (Image icon in root.GetComponentsInChildren<Image>(true)) {
                if (icon.name.StartsWith("Icon_")) {
                    MakeKeycap(icon);
                }
            }
            foreach (TMP_Text word in root.GetComponentsInChildren<TMP_Text>(
                true
            )) {
                StyleText(word, SemiBold, HINT_WORD_SIZE, UIStyle.Cream);
                word.textWrappingMode = TextWrappingModes.NoWrap;
                RectTransform wordRect = word.rectTransform;
                wordRect.sizeDelta = new Vector2(
                    Mathf.Ceil(word.GetPreferredValues(word.text).x) + 4f,
                    wordRect.sizeDelta.y
                );
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // an outline key icon on a dark brown keycap: the icon's own Image takes
    // the keycap fill & casts the brown shadow, a child draws the outline
    // in cream on top, so the layout keeps the same element
    internal static void MakeKeycap(Image icon) {
        if (icon.transform.Find("Outline") != null) {
            return; // already a keycap
        }
        Sprite outline = icon.sprite;
        if (outline == null) {
            return;
        }
        string keycapName = outline.name.Replace("_outline", "") + "_keycap";
        Sprite keycap = Load<Sprite>(KEYCAPS + keycapName + ".png");
        if (keycap == null) {
            Debug.LogWarning(
                "UIStyleBuilder:\tno keycap sprite " + keycapName, icon
            );
            return;
        }

        RectTransform child = NewChild("Outline", icon.transform);
        child.anchorMin = Vector2.zero;
        child.anchorMax = Vector2.one;
        child.offsetMin = child.offsetMax = Vector2.zero;
        Image outlineImage = child.gameObject.AddComponent<Image>();
        outlineImage.sprite = outline;
        outlineImage.preserveAspect = icon.preserveAspect;
        outlineImage.color = UIStyle.Cream;
        outlineImage.raycastTarget = false;

        icon.sprite = keycap;
        icon.color = UIStyle.Brown;
        AddShadow(icon.gameObject, UIStyle.Shadow, KEYCAP_SHADOW);
    }

    // helpers  ----------------------------------------------------------------
    private static bool AreFontsReady() {
        if (SemiBold != null && ExtraBold != null && TitleMaterial != null) {
            return true;
        }
        Debug.LogError(
            "UIStyleBuilder:\trun Monkobra/UI Style/1. Build Fonts first"
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

    // Outline is a Shadow, so each is found by its exact type, never the
    // other's
    private static T GetOrAddExact<T>(GameObject go) where T : Shadow {
        foreach (T found in go.GetComponents<T>()) {
            if (found.GetType() == typeof(T)) {
                return found;
            }
        }
        return go.AddComponent<T>();
    }

    // the logo's brown shadow, down-right, under a graphic
    internal static void AddShadow(GameObject go, Color color, Vector2 offset) {
        Shadow shadow = GetOrAddExact<Shadow>(go);
        shadow.effectColor = color;
        shadow.effectDistance = offset;
        shadow.useGraphicAlpha = true;
    }

    // a thin rim around a graphic, eg a plate's border
    internal static void AddOutline(GameObject go, Color color, float width) {
        Outline outline = GetOrAddExact<Outline>(go);
        outline.effectColor = color;
        outline.effectDistance = new Vector2(width, -width);
        outline.useGraphicAlpha = true;
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
    private const float HINT_WORD_SIZE = 28f;
    private const float HINT_SIZE = 22f;

    // buttons: the face, plus the ring & shadow margins baked in the sprites
    private const string SPRITES = "Assets/_Monkobra/Sprites/UI/";
    private const string KEYCAPS = "Assets/_Monkobra/Sprites/InputIcons/";
    private static readonly Vector2 MENU_FACE = new Vector2(420f, 70f);
    private const float ICON_FACE = 64f;
    private const float GLYPH_SIZE = 36f;
    private const float LOCK_SIZE = 24f;
    private const float MARGIN_LEFT = 8f;
    private const float MARGIN_TOP = 8f;
    private const float MARGIN_RIGHT = 13f;
    private const float MARGIN_BOTTOM = 14f;
    private const float BUTTON_GAP = 20f;
    private const float HINT_HEIGHT = 40f;
    private const float HINT_ROW_GAP = 10f;
    private const float KEY_PAIR_GAP = 6f;
    private static readonly Vector2 KEYCAP_SHADOW = new Vector2(2f, -3f);

    private const string MENU_BUTTON_PREFAB =
        "Assets/_Monkobra/Prefabs/UI/MenuButton.prefab";
    private const string ICON_BUTTON_PREFAB =
        "Assets/_Monkobra/Prefabs/UI/IconButton.prefab";
    private const string LEVEL_SELECT =
        "Assets/_Monkobra/Prefabs/LevelSelectScreen/LevelSelectScreen.prefab";
    private const string HINT_KEYBOARD =
        "Assets/_Monkobra/Prefabs/UI/MenuHint_Keyboard.prefab";
    private const string HINT_TOUCH =
        "Assets/_Monkobra/Prefabs/UI/MenuHint_Touch.prefab";
}
