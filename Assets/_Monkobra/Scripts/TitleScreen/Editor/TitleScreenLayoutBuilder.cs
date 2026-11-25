using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the picture of the TitleScreen prefab from one 1280x720 layout,
/// the same one the WebGL loading page draws as its last frame (the
/// <c>LAYOUT</c> block of <c>WebGLTemplates/FitWindow/index.html</c>), so the
/// browser hands over to the title without a visible jump. Run
/// <c>Monkobra/Build Title Screen</c> again after changing the layout here and
/// there; it replaces the picture and keeps the root, the background and the
/// whole start prompt (its CanvasGroup, which <see cref="TitleScreen"/>
/// references, and the input icons the scene overrides), only moving it.
/// </summary>
public static class TitleScreenLayoutBuilder {
    // Public Members  #########################################################
    // the loading page's vines on a full-screen panel, replacing any there;
    // the level selection uses them too, so both screens hang them alike
    internal static RectTransform BuildVines(Transform panel) {
        Transform old = panel.Find("Vines");
        if (old != null) {
            Object.DestroyImmediate(old.gameObject);
        }
        RectTransform vines = Group(panel, "Vines");
        foreach (Item item in VINES) {
            Picture(vines, item);
        }
        return vines;
    }

    // private methods  ########################################################
    [MenuItem("Monkobra/Build Title Screen")]
    private static void Build() {
        if (!AreSpritesFound()) {
            Debug.LogError(
                "TitleScreenLayoutBuilder:\tmissing art in " + SPRITE_FOLDER
            );
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB);
        try {
            Transform panel = root.transform.Find("Panel");
            Transform prompt = panel == null ? null : panel.Find("Prompt");
            if (prompt == null) {
                Debug.LogError(
                    "TitleScreenLayoutBuilder:\tno Panel/Prompt in " + PREFAB
                );
                return;
            }

            foreach (string name in REPLACED_CHILDREN) {
                Transform old = panel.Find(name);
                if (old != null) {
                    Object.DestroyImmediate(old.gameObject);
                }
            }
            panel.GetComponent<Image>().color = BACKGROUND;

            RectTransform vines = BuildVines(panel);
            Picture(panel, LOGO);
            BuildRunway(Group(panel, "Runway"));
            MovePrompt((RectTransform)prompt);

            // back to front: vines, logo, runway, prompt
            vines.SetSiblingIndex(0);
            panel.Find(LOGO.Name).SetSiblingIndex(1);
            panel.Find("Runway").SetSiblingIndex(2);
            prompt.SetSiblingIndex(3);

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
            Debug.Log("TitleScreenLayoutBuilder:\tbuilt " + PREFAB);
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // the branch, then the fully grown cobra and the monkey at the end, as the
    // loading page leaves them at 100%
    private static void BuildRunway(RectTransform runway) {
        Picture(runway, BRANCH);
        RectTransform body = Group(runway, "Body");
        for (int k = 0; k <= DOT_COUNT; k++) {
            float x = DOT_X0 + (k - 1) * SPACING;
            float d = SEG_D;
            if (k < TAIL.Length) {
                x = TAIL[k].x;
                d = TAIL[k].y;
            }
            Picture(body, Item.Centred(
                "Segment_" + k.ToString("00"), "CobraSegment",
                x, SURFACE_Y + SEG_DROP - d / 2, d, d, CENTRE
            ));
        }

        float frontX = DOT_X0 + (DOT_COUNT - 1) * SPACING;
        float segY = SURFACE_Y + SEG_DROP - SEG_D / 2;
        Picture(runway, Item.Centred(
            "CobraHead", "CobraHead", frontX + HEAD_AHEAD, segY - HEAD_LIFT,
            HEAD_W, HEAD_H, CENTRE
        ));
        Picture(runway, Item.Centred(
            "Monkey", "MonkeyHead_Open", frontX + MONKEY_GAP,
            SURFACE_Y + MONKEY_SINK - MONKEY_H / 2, MONKEY_W, MONKEY_H, CENTRE
        ));
    }

    // the start prompt (input icons and words, laid out by its own group)
    // keeps its layout & is drawn at PROMPT_SCALE; it only moves to where the
    // loading page writes "Loading", and its words take that label's font,
    // size and color (Baloo 2 SemiBold, cream), so they change in place at
    // the hand-over
    private static void MovePrompt(RectTransform prompt) {
        Vector2 size = prompt.sizeDelta;
        Place(prompt, Item.Centred(
            prompt.name, "", LABEL.X + LABEL.W / 2, LABEL.Y + LABEL.H / 2,
            size.x, size.y, CENTRE
        ));
        prompt.localScale = new Vector3(PROMPT_SCALE, PROMPT_SCALE, 1f);

        TMPro.TMP_FontAsset font = UIStyleBuilder.SemiBold;
        if (font == null) {
            Debug.LogWarning(
                "TitleScreenLayoutBuilder:\tprompt words kept their font, run "
                    + "Monkobra/UI Style/1. Build Fonts first"
            );
            return;
        }
        foreach (TMPro.TMP_Text word in
            prompt.GetComponentsInChildren<TMPro.TMP_Text>(true)) {
            UIStyleBuilder.StyleText(word, font, LABEL_SIZE, UIStyle.Cream);
        }
        UIStyleBuilder.SizeAnimatedIcons(prompt);
    }

    private static RectTransform Group(Transform parent, string name) {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = UI_LAYER;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void Picture(Transform parent, Item item) {
        GameObject go = new GameObject(
            item.Name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        go.layer = UI_LAYER;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Place(rect, item);
        Image image = go.GetComponent<Image>();
        image.sprite = LoadSprite(item.Sprite);
        image.raycastTarget = false;
    }

    // a rect given top-left, y down on the 1280x720 page, into Unity anchors
    private static void Place(RectTransform rect, Item item) {
        rect.anchorMin = item.Anchor;
        rect.anchorMax = item.Anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(item.W, item.H);
        rect.anchoredPosition = new Vector2(
            item.X + item.W / 2 - item.Anchor.x * WIDTH,
            HEIGHT - (item.Y + item.H / 2) - item.Anchor.y * HEIGHT
        );
    }

    private static Sprite LoadSprite(string name) {
        return AssetDatabase.LoadAssetAtPath<Sprite>(
            SPRITE_FOLDER + "/" + name + ".png"
        );
    }

    private static bool AreSpritesFound() {
        string[] names = {
            "Vine_A", "Vine_Curl", "Vine_B", "MonkobraLogo", "Branch",
            "CobraSegment", "CobraHead", "MonkeyHead_Open",
        };
        foreach (string name in names) {
            if (LoadSprite(name) == null) {
                return false;
            }
        }
        return true;
    }

    // a picture on the 1280x720 page: top-left x, y (y down), size, anchor
    private readonly struct Item {
        public Item(
            string name, string sprite, float x, float y, float w, float h,
            Vector2 anchor
        ) {
            Name = name;
            Sprite = sprite;
            X = x;
            Y = y;
            W = w;
            H = h;
            Anchor = anchor;
        }

        public static Item Centred(
            string name, string sprite, float cx, float cy, float w, float h,
            Vector2 anchor
        ) {
            return new Item(name, sprite, cx - w / 2, cy - h / 2, w, h, anchor);
        }

        public readonly string Name;
        public readonly string Sprite;
        public readonly float X;
        public readonly float Y;
        public readonly float W;
        public readonly float H;
        public readonly Vector2 Anchor;
    }

    // constants  ##############################################################
    private const string PREFAB =
        "Assets/_Monkobra/Prefabs/TitleScreen/TitleScreen.prefab";
    private const string SPRITE_FOLDER = "Assets/_Monkobra/Sprites/TitleScreen";
    private const int UI_LAYER = 5;
    private const float WIDTH = 1280f;
    private const float HEIGHT = 720f;

    // everything this builder makes, plus what it once made or replaced: the
    // old TMP title, and the footer logos, now on the WebGL team logo screen
    private static readonly string[] REPLACED_CHILDREN = {
        "Title", "Vines", "MonkobraLogo", "Runway", "Footer",
    };

    private static readonly Color BACKGROUND = new Color32(10, 31, 18, 255);
    // the loading page's "Loading" label size, the prompt words' size
    private const float LABEL_SIZE = 40f;
    // the prompt drawn at 0.7 its size, its words so at the label's 28 px
    private const float PROMPT_SCALE = 0.7f;

    private static readonly Vector2 CENTRE = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 TOP_LEFT = new Vector2(0f, 1f);
    private static readonly Vector2 TOP_RIGHT = new Vector2(1f, 1f);

    // keep equal to LAYOUT in WebGLTemplates/FitWindow/index.html  ------------
    // the logo 1.2 times its first size, a bit above the middle; the branch,
    // the cobra, the monkey & the prompt 0.7 times theirs, the middle of that
    // block moved down to (640, 585)
    private const int DOT_COUNT = 20;
    private const float DOT_X0 = 423f;
    private const float SPACING = 21f;
    private const float SEG_D = 25.2f;
    private const float SURFACE_Y = 565.4f;
    private const float SEG_DROP = 2.1f;
    private const float HEAD_AHEAD = 9.8f;
    private const float HEAD_LIFT = 8.4f;
    private const float HEAD_W = 43.4f;
    private const float HEAD_H = 42f;
    private const float MONKEY_GAP = 58.8f;
    private const float MONKEY_SINK = 2.8f;
    private const float MONKEY_W = 44.8f;
    private const float MONKEY_H = 42f;

    // first body segments, tapering to the tail tip: x, size
    private static readonly Vector2[] TAIL = {
        new Vector2(417.4f, 14f), new Vector2(429.3f, 18.2f), new Vector2(445.4f, 22.4f),
    };

    private static readonly Item[] VINES = {
        new Item("Vine_A", "Vine_A", 40, -40, 94, 298, TOP_LEFT),
        new Item("Vine_Curl", "Vine_Curl", 138, -30, 52, 234, TOP_LEFT),
        new Item("Vine_B", "Vine_B", 1150, -40, 78, 286, TOP_RIGHT),
    };
    private static readonly Item LOGO =
        new Item("MonkobraLogo", "MonkobraLogo", 220, 240, 840, 149, CENTRE);
    private static readonly Item BRANCH =
        new Item("Branch", "Branch", 332, 519.9f, 616, 84, CENTRE);
    private static readonly Item LABEL =
        new Item("Prompt", "", 500, 615.1f, 280, 35, CENTRE);
}
