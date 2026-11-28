using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BossFightHudRig;

/// <summary>Builds the HUD elements in code: the quiet bar, the revive panel,
/// the reward panel, the armor badge, and the UI factory helpers.</summary>
public sealed class BossFightHudBuilder {
    // private members  ########################################################
    private readonly BossFightHudRig rig;
    private readonly BossFightHudConfig config;
    private readonly System.Action onWatchAdClicked;
    private readonly System.Action onGiveUpClicked;
    private readonly System.Action onCoursePageClicked;
    private readonly System.Action onContinueClicked;
    private readonly System.Action onSkipAdClicked;

    public BossFightHudBuilder(BossFightHudRig rig,
        System.Action onWatchAdClicked, System.Action onGiveUpClicked,
            System.Action onCoursePageClicked, System.Action onContinueClicked,
                System.Action onSkipAdClicked) {
        this.rig = rig;
        config = rig.config;
        this.onWatchAdClicked = onWatchAdClicked;
        this.onGiveUpClicked = onGiveUpClicked;
        this.onCoursePageClicked = onCoursePageClicked;
        this.onContinueClicked = onContinueClicked;
        this.onSkipAdClicked = onSkipAdClicked;
    }

    // Public Methods  #########################################################
    public bool TryBuild() {
        if (rig.rootRect != null) {
            return true;
        }
        Canvas canvas = FindRootCanvas();
        if (canvas == null) {
            Debug.LogError("BossFightHud:\tno root Canvas found", rig.owner);
            return false;
        }
        rig.canvasRect = (RectTransform)canvas.transform;
        rig.canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        rig.rootRect = CreateRect("BossFightHud", rig.canvasRect);
        Stretch(rig.rootRect);
        // under the screens, so pause, tutorial & lose panels cover it
        Transform screens = FindChildNamed(rig.canvasRect, "Screen");
        if (screens != null) {
            rig.rootRect.SetSiblingIndex(screens.GetSiblingIndex());
        }

        // name & health bar, top center
        RectTransform bar = CreateRect("HealthBar", rig.rootRect);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = new Vector2(0f, -54f);
        bar.sizeDelta = new Vector2(440f, 22f);
        AddImage(bar, Tint(UIStyle.Panel, 0.85f));
        rig.healthFill = CreateRect("Fill", bar);
        Stretch(rig.healthFill);
        rig.healthFill.offsetMin = new Vector2(3f, 3f);
        rig.healthFill.offsetMax = new Vector2(-3f, -3f);
        rig.healthFill.anchorMax = new Vector2(1f, 1f);
        AddImage(rig.healthFill, config.HealthColor);
        CreateHeart(bar);
        rig.nameLabel =
            CreateLabel("Name", rig.rootRect, "", 28f, UIStyle.Cream);
        RectTransform nameRect = rig.nameLabel.rectTransform;
        nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -16f);
        nameRect.sizeDelta = new Vector2(440f, 36f);

        // speech bubble
        rig.speechRect = CreateRect("Speech", rig.rootRect);
        rig.speechRect.sizeDelta = new Vector2(360f, 56f);
        AddImage(rig.speechRect, Tint(UIStyle.Panel, 0.85f));
        rig.speechLabel =
            CreateLabel("Line", rig.speechRect, "", 28f, UIStyle.Cream);
        Stretch(rig.speechLabel.rectTransform);
        rig.speechRect.gameObject.SetActive(false);

        // animated hold-and-release icon, one per control style
        rig.keyPrompt = KeyPromptWidget.Create(
            rig.rootRect, config.HintIconLibrary,
                config.PromptKeyboardController,
            config.PromptTouchController
        );

        // banner, screen center
        rig.bannerLabel = CreateLabel(
            "Banner", rig.rootRect, "", 80f, UIStyle.Gold
        );
        rig.bannerLabel.rectTransform.sizeDelta = new Vector2(1100f, 240f);
        rig.bannerLabel.rectTransform.anchoredPosition = new Vector2(0f, 120f);
        rig.bannerLabel.gameObject.SetActive(false);

        // entry flash, on top of the rest of the hud
        RectTransform flash = CreateRect("Flash", rig.rootRect);
        Stretch(flash);
        rig.flashImage = AddImage(flash, new Color(1f, 1f, 1f, 0f));
        return true;
    }

    // a heart just left of the bar, in its color w/ a dark brown rim, the
    // health left in cream on it; under the bar, so it hides w/ it
    private void CreateHeart(RectTransform bar) {
        rig.heart = CreateRect("Heart", bar);
        rig.heart.anchorMin = rig.heart.anchorMax = new Vector2(0f, 0.5f);
        rig.heart.pivot = new Vector2(1f, 0.5f);
        rig.heart.anchoredPosition = new Vector2(-HEART_GAP, 0f);
        rig.heart.sizeDelta = new Vector2(HEART_SIZE, HEART_SIZE);
        Sprite sprite = HeartSprite();
        Image rim = AddImage(rig.heart, UIStyle.Brown);
        rim.sprite = sprite;
        rim.preserveAspect = true;
        RectTransform fill = CreateRect("Fill", rig.heart);
        Stretch(fill);
        float inset = HEART_SIZE * HEART_RIM_SHARE;
        fill.offsetMin = new Vector2(inset, inset);
        fill.offsetMax = new Vector2(-inset, -inset);
        Image fillImage = AddImage(fill, config.HealthColor);
        fillImage.sprite = sprite;
        fillImage.preserveAspect = true;
        rig.heartLabel = CreateLabel("Count", rig.heart, "", HEART_FONT_SIZE,
            UIStyle.Cream);
        rig.heartLabel.fontStyle = FontStyles.Bold;
        RectTransform count = rig.heartLabel.rectTransform;
        Stretch(count);
        // a heart's middle sits a little above its box's
        count.offsetMin = new Vector2(0f, HEART_SIZE * 0.06f);
        count.offsetMax = new Vector2(0f, HEART_SIZE * 0.06f);
        rig.heartLabel.outlineColor = UIStyle.Brown;
        rig.heartLabel.outlineWidth = 0.25f;
    }

    // one white heart, its edge smoothed, made once: (x² + y² - 1)³ - x²y³
    // <= 0, 4 x 4 samples a pixel
    private static Sprite HeartSprite() {
        if (heartSprite != null) {
            return heartSprite;
        }
        const int size = 128;
        const int samples = 4;
        Color32[] pixels = new Color32[size * size];
        for (int py = 0; py < size; py++) {
            for (int px = 0; px < size; px++) {
                int inside = 0;
                for (int sy = 0; sy < samples; sy++) {
                    for (int sx = 0; sx < samples; sx++) {
                        float x = ((px + (sx + 0.5f) / samples) / size
                            - 0.5f) * 2.6f;
                        float y = ((py + (sy + 0.5f) / samples) / size
                            - 0.5f) * 2.6f + 0.1f;
                        float a = x * x + y * y - 1f;
                        if (a * a * a - x * x * y * y * y <= 0f) {
                            inside++;
                        }
                    }
                }
                pixels[py * size + px] = new Color32(255, 255, 255,
                    (byte)(255 * inside / (samples * samples)));
            }
        }
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32,
            false) {
            name = "Monkobra UI Heart",
            hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        heartSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        heartSprite.name = "Monkobra UI Heart";
        heartSprite.hideFlags = HideFlags.DontSave;
        return heartSprite;
    }

    private static Sprite heartSprite;
    // the heart's size & gap to the bar, its rim's share of its size, the
    // count's size; canvas units
    private const float HEART_SIZE = 56f;
    private const float HEART_GAP = 6f;
    private const float HEART_RIM_SHARE = 0.08f;
    private const float HEART_FONT_SIZE = 26f;

    public static Canvas FindRootCanvas() {
        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsSortMode.None
        )) {
            if (canvas.isRootCanvas
                && canvas.renderMode != RenderMode.WorldSpace) {
                return canvas;
            }
        }
        return null;
    }

    public static Transform FindChildNamed(Transform parent, string part) {
        foreach (Transform child in parent) {
            if (child.name.Contains(part)) {
                return child;
            }
        }
        return null;
    }

    // the revive offer card & the mock ad, built on first use
    public bool BuildRevive() {
        if (rig.revivePanel != null) {
            return true;
        }
        if (rig.rootRect == null) {
            return false;
        }
        Color gold = new Color(1f, 0.82f, 0.2f);

        RectTransform offer = CreateRect("ReviveOffer", rig.rootRect);
        Stretch(offer);
        AddImage(offer, Tint(UIStyle.Background, 0.75f));
        RectTransform card = CreateRect("Card", offer);
        card.sizeDelta = new Vector2(580f, 340f);
        AddImage(card, Tint(UIStyle.Panel, 0.96f));
        TextMeshProUGUI reviveTitle =
            CreateLabel("Title", card, "Revive?", 60f, UIStyle.Gold);
        reviveTitle.fontStyle = FontStyles.Bold;
        PlaceLabel(reviveTitle, new Vector2(0f, 110f), new Vector2(540f, 70f));
        PlaceLabel(CreateLabel(
                "Line", card, "Watch a short ad to keep fighting!", 26f,
                UIStyle.Cream
            ), new Vector2(0f, 50f), new Vector2(540f, 40f));
        rig.reviveCountLabel =
            CreateLabel("Count", card, "", 48f, UIStyle.Gold);
        PlaceLabel(rig.reviveCountLabel, new Vector2(0f, -10f),
            new Vector2(200f, 60f));
        CreateButton("WatchAd", card, "Watch Ad  [Space]",
            UIStyle.Gold, new Vector2(-135f, -105f),
            () => onWatchAdClicked?.Invoke(), "Watch Ad", UIStyle.Brown);
        CreateButton("GiveUp", card, "Give Up  [R]",
            UIStyle.Divider, new Vector2(135f, -105f),
            () => onGiveUpClicked?.Invoke(), "Give Up");
        rig.revivePanel = offer.gameObject;
        rig.revivePanel.SetActive(false);

        // the "ad" plugs the course this game is made in: cardinal &
        // gold, bobbing shapes, a Course Page button and a timer
        Color cardinal = new Color(0.6f, 0.05f, 0.08f);
        RectTransform ad = CreateRect("MockAd", rig.rootRect);
        Stretch(ad);
        AddImage(ad, cardinal);
        Color[] shapeColors = {
            gold, new Color(1f, 1f, 1f, 0.75f), new Color(1f, 0.6f, 0.2f),
        }
        ;
        for (int i = 0; i < 8; i++) {
            RectTransform shape = CreateRect("Shape" + i, ad);
            float size = 40f + 25f * (i % 3);
            shape.sizeDelta = new Vector2(size, size);
            shape.anchoredPosition = new Vector2(
                -520f + i * 150f, i % 2 == 0 ? 250f : -250f
            );
            AddImage(shape, shapeColors[i % shapeColors.Length]);
            rig.adShapes.Add(shape);
        }
        TextMeshProUGUI title = CreateLabel(
            "Title", ad, "Like this game? Make your own!", 64f, gold
        );
        PlaceLabel(title, new Vector2(0f, 110f), new Vector2(1150f, 90f));
        rig.adTitle = title.rectTransform;
        PlaceLabel(CreateLabel(
                "Course", ad, "USC CSCI 526", 52f, Color.white
            ), new Vector2(0f, 30f), new Vector2(1000f, 70f));
        PlaceLabel(CreateLabel(
                "Name", ad, "Advanced Mobile Devices and Game Consoles", 30f,
                Color.white
            ), new Vector2(0f, -25f), new Vector2(1100f, 44f));
        PlaceLabel(CreateLabel(
                "Pitch", ad,
                "Team up and ship a game in one semester, like Monkobra!",
                26f, new Color(1f, 0.92f, 0.7f)
            ), new Vector2(0f, -70f), new Vector2(1100f, 40f));
        CreateButton("CoursePage", ad, "Course Page", gold * 0.85f,
            new Vector2(0f, -150f), () => onCoursePageClicked?.Invoke(),
            "Course Page");
        RectTransform tag = CreateRect("AdTag", ad);
        tag.anchorMin = tag.anchorMax = tag.pivot = new Vector2(0f, 1f);
        tag.anchoredPosition = new Vector2(24f, -24f);
        tag.sizeDelta = new Vector2(70f, 40f);
        AddImage(tag, new Color(0f, 0f, 0f, 0.6f));
        TextMeshProUGUI tagLabel = CreateLabel("Label", tag, "AD", 24f,
            Color.white);
        Stretch(tagLabel.rectTransform);
        rig.adSkipButton = CreateButton("Skip", ad, "Skip  [Space]",
            new Color(0f, 0f, 0f, 0.55f), Vector2.zero,
            () => onSkipAdClicked?.Invoke(), "Skip");
        RectTransform skip = (RectTransform)rig.adSkipButton.transform;
        skip.anchorMin = skip.anchorMax = skip.pivot = new Vector2(1f, 1f);
        skip.anchoredPosition = new Vector2(-24f, -24f);
        rig.adSkipButton.SetActive(false);
        rig.adPanel = ad.gameObject;
        rig.adPanel.SetActive(false);
        return true;
    }

    // the win's pause: dimmed screen, title, lines & a beating Continue
    public GameObject CreateRewardPanel() {
        Color gold = UIStyle.Gold;
        RectTransform panel = CreateRect("RewardPanel", rig.rootRect);
        Stretch(panel);
        AddImage(panel, Tint(UIStyle.Background, 0.75f));
        RectTransform diamond = CreateRect("Diamond", panel);
        diamond.anchoredPosition = new Vector2(0f, 190f);
        diamond.sizeDelta = new Vector2(56f, 56f);
        diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
        AddImage(diamond, gold);
        rig.rewardTitle = CreateLabel("Title", panel, "", 84f, gold);
        rig.rewardTitle.fontStyle = FontStyles.Bold;
        PlaceLabel(rig.rewardTitle, new Vector2(0f, 100f),
            new Vector2(1100f, 100f));
        rig.rewardLine = CreateLabel("Line", panel, "", 42f, UIStyle.Cream);
        PlaceLabel(rig.rewardLine, new Vector2(0f, 20f),
            new Vector2(1100f, 60f));
        rig.rewardDetail = CreateLabel(
            "Detail", panel, "", 30f, Tint(UIStyle.Cream, 0.85f)
        );
        PlaceLabel(rig.rewardDetail, new Vector2(0f, -30f),
            new Vector2(1100f, 44f));
        GameObject button = CreateButton(
            "Continue", panel, "Continue  [Space]",
            gold, new Vector2(0f, -120f),
            () => onContinueClicked?.Invoke(), "Continue", UIStyle.Brown
        );
        rig.rewardButton = (RectTransform)button.transform;
        panel.gameObject.SetActive(false);
        return panel.gameObject;
    }

    // the difficulty choice after a revive: a short title & two big
    // buttons, EASY & NORMAL, side by side; the picked one rings in cream
    public GameObject CreateModeChoice(System.Action<bool> onPick) {
        RectTransform panel = CreateRect("ModeChoice", rig.rootRect);
        Stretch(panel);
        AddImage(panel, Tint(UIStyle.Background, 0.75f));
        RectTransform card = CreateRect("Card", panel);
        card.sizeDelta = new Vector2(620f, 320f);
        AddImage(card, Tint(UIStyle.Panel, 0.96f));
        TextMeshProUGUI title = CreateLabel("Title", card, "Difficulty", 56f,
            UIStyle.Gold);
        title.fontStyle = FontStyles.Bold;
        PlaceLabel(title, new Vector2(0f, 100f), new Vector2(580f, 70f));
        rig.easyRing = CreateRing("EasyRing", card, new Vector2(-140f, -5f));
        rig.normalRing = CreateRing("NormalRing", card,
            new Vector2(140f, -5f));
        rig.easyButton = CreateChoiceButton("Easy", card,
            new Vector2(-140f, -5f), EASY_COLOR, () => onPick(true));
        rig.normalButton = CreateChoiceButton("Normal", card,
            new Vector2(140f, -5f), UIStyle.Gold, () => onPick(false));
        TextMeshProUGUI hint = CreateLabel("Hint", card, "", 24f,
            Tint(UIStyle.Cream, 0.85f));
        PlaceLabel(hint, new Vector2(0f, -110f), new Vector2(580f, 36f));
        AddStyleText(hint, "A / D, then Space", "Tap one");
        panel.gameObject.SetActive(false);
        return panel.gameObject;
    }

    // the EASY signal green, as on the old choice tags
    private static readonly Color EASY_COLOR = new Color(0.55f, 0.95f, 0.55f);

    private static RectTransform CreateChoiceButton(string word,
        Transform parent, Vector2 position, Color face, System.Action onClick) {
        RectTransform rect = (RectTransform)CreateButton(word, parent, word,
            face, position, onClick, word, UIStyle.Brown).transform;
        rect.sizeDelta = new Vector2(240f, 96f);
        TextMeshProUGUI label = rect.GetComponentInChildren<TextMeshProUGUI>();
        label.fontSize = 40f;
        label.fontStyle = FontStyles.Bold;
        return rect;
    }

    // a cream ring behind a choice, shown while it is picked
    private static RectTransform CreateRing(string ringName, Transform parent,
        Vector2 position) {
        RectTransform ring = CreateRect(ringName, parent);
        ring.anchoredPosition = position;
        ring.sizeDelta = new Vector2(256f, 112f);
        AddImage(ring, UIStyle.Cream);
        return ring;
    }

    // a dark plate w/ a gold diamond & GOLDEN ARMOR
    public RectTransform CreateArmorBadge() {
        Color gold = UIStyle.Gold;
        RectTransform badge = CreateRect("ArmorBadge", rig.rootRect);
        badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(1f, 0f);
        // left of the progress bar along the right edge
        badge.anchoredPosition = new Vector2(-70f, 24f);
        badge.sizeDelta = new Vector2(250f, 54f);
        AddImage(badge, Tint(UIStyle.Panel, 0.85f));
        RectTransform diamond = CreateRect("Diamond", badge);
        diamond.anchorMin = diamond.anchorMax = new Vector2(0f, 0.5f);
        diamond.anchoredPosition = new Vector2(30f, 0f);
        diamond.sizeDelta = new Vector2(26f, 26f);
        diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
        AddImage(diamond, gold);
        TextMeshProUGUI label = CreateLabel(
            "Label", badge, "GOLDEN ARMOR", 24f, gold
        );
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(52f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);
        badge.gameObject.SetActive(false);
        return badge;
    }

    // the monkey's hearts in the bottom left, left-justified, w/ the armor
    // mark at its end
    public RectTransform CreateMonkeyHeartRow() {
        RectTransform row = CreateRect("MonkeyHearts", rig.rootRect);
        row.anchorMin = row.anchorMax = row.pivot = new Vector2(0f, 0f);
        row.anchoredPosition = new Vector2(40f, 40f);
        row.sizeDelta = new Vector2(300f, MONKEY_HEART_SIZE);
        RectTransform mark = CreateRect("ArmorMark", row);
        mark.anchorMin = mark.anchorMax = mark.pivot = new Vector2(0f, 0.5f);
        mark.sizeDelta = new Vector2(MONKEY_HEART_SIZE * 0.8f, MONKEY_HEART_SIZE * 0.8f);
        Image markImage = AddImage(mark, UIStyle.Gold);
        if (rig.config.ArmorSprite != null) {
            markImage.sprite = rig.config.ArmorSprite;
        } else {
            mark.localRotation = Quaternion.Euler(0f, 0f, 45f);
            mark.sizeDelta = new Vector2(MONKEY_HEART_SIZE * 0.55f, MONKEY_HEART_SIZE * 0.55f);
        }
        rig.armorMark = mark;
        row.gameObject.SetActive(false);
        return row;
    }

    // as many hearts as the fight gave, the lost ones dimmed
    public void SyncMonkeyHearts(int max, int left) {
        RectTransform row = rig.monkeyHeartRow;
        while (rig.monkeyHeartImages.Count < max) {
            int i = rig.monkeyHeartImages.Count;
            RectTransform heart = CreateRect("Heart" + i, row);
            heart.anchorMin = heart.anchorMax = heart.pivot =
                new Vector2(0f, 0.5f);
            heart.sizeDelta = new Vector2(MONKEY_HEART_SIZE, MONKEY_HEART_SIZE);
            Image image = AddImage(heart, Color.white);
            image.sprite = rig.config.HeartSprite != null
                ? rig.config.HeartSprite : DrawnHeart();
            image.preserveAspect = true;
            rig.monkeyHeartImages.Add(image);
        }
        for (int i = 0; i < rig.monkeyHeartImages.Count; i++) {
            Image image = rig.monkeyHeartImages[i];
            image.gameObject.SetActive(i < max);
            image.rectTransform.anchoredPosition =
                new Vector2(i * (MONKEY_HEART_SIZE + MONKEY_HEART_GAP), 0f);
            image.color = i < left ? MONKEY_HEART_FULL : MONKEY_HEART_LOST;
        }
        rig.armorMark.anchoredPosition = new Vector2(
            max * (MONKEY_HEART_SIZE + MONKEY_HEART_GAP) + MONKEY_HEART_SIZE * 0.2f, 0f);
    }

    private const float MONKEY_HEART_SIZE = 44f;
    private const float MONKEY_HEART_GAP = 6f;
    private static readonly Color MONKEY_HEART_FULL = new Color(0.92f, 0.2f, 0.27f);
    private static readonly Color MONKEY_HEART_LOST = new Color(0.25f, 0.25f, 0.3f, 0.55f);
    private static Sprite drawnHeart;

    // a plain heart drawn from its curve, until a sprite is assigned
    private static Sprite DrawnHeart() {
        if (drawnHeart != null) {
            return drawnHeart;
        }
        const int SIZE = 64;
        Texture2D texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32,
            false);
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < SIZE; y++) {
            for (int x = 0; x < SIZE; x++) {
                float u = (x + 0.5f) / SIZE * 3f - 1.5f;
                float v = (y + 0.5f) / SIZE * 3f - 1.3f;
                float a = u * u + v * v - 1f;
                float inside = a * a * a - u * u * v * v * v;
                float alpha = Mathf.Clamp01(0.5f - inside * 6f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        drawnHeart = Sprite.Create(texture, new Rect(0f, 0f, SIZE, SIZE),
            new Vector2(0.5f, 0.5f), SIZE);
        return drawnHeart;
    }

    // a dark plate w/ a colored word, placed over a world point
    public RectTransform CreateTag(string tagName, string text, Color color) {
        RectTransform tag = CreateRect(tagName, rig.rootRect);
        tag.sizeDelta = new Vector2(130f, 40f);
        AddImage(tag, Tint(UIStyle.Panel, 0.85f));
        TextMeshProUGUI label = CreateLabel("Label", tag, text, 26f, color);
        Stretch(label.rectTransform);
        tag.gameObject.SetActive(false);
        return tag;
    }

    public static void PlaceLabel(
        TextMeshProUGUI label, Vector2 position, Vector2 size
    ) {
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = size;
    }

    public static GameObject CreateButton(
        string buttonName,
        Transform parent,
        string text,
        Color color,
        Vector2 position,
        System.Action onClick,
        string touchText = null,
        Color? labelColor = null
    ) {
        RectTransform rect = CreateRect(buttonName, parent);
        rect.sizeDelta = new Vector2(240f, 60f);
        rect.anchoredPosition = position;
        Image image = AddImage(rect, color);
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => {
            // not left selected, or Space / Enter would press it again
            UnityEngine.EventSystems.EventSystem events =
                UnityEngine.EventSystems.EventSystem.current;
            if (events != null) {
                events.SetSelectedGameObject(null);
            }
            onClick();
        });
        TextMeshProUGUI label = CreateLabel("Label", rect, text, 24f,
            labelColor ?? UIStyle.Cream);
        Stretch(label.rectTransform);
        AddStyleText(label, text, touchText);
        return rect.gameObject;
    }

    // the key hint of a label is dropped in touch mode, where the label is a
    // tappable button or a prompt with no key to name
    public static void AddStyleText(
        TextMeshProUGUI label,
        string keyboardText,
        string touchText
    ) {
        if (touchText == null) {
            return;
        }
        label.gameObject.AddComponent<ControlStyleLabel>()
            .Configure(keyboardText, touchText);
    }

    public static RectTransform CreateRect(string rectName, Transform parent) {
        GameObject go = new GameObject(rectName, typeof(RectTransform));
        go.layer = 5;
        // UI
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    public static void AddCenterLabel(
        RectTransform parent,
        string text,
        float size,
        Color color,
        float y,
        string touchText = null
    ) {
        TextMeshProUGUI label = CreateLabel("Line", parent, text, size, color);
        AddStyleText(label, text, touchText);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);
        label.rectTransform.sizeDelta = new Vector2(1000f, size * 1.4f);
        label.rectTransform.anchoredPosition = new Vector2(0f, y);
    }

    public static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // a palette color at another alpha
    public static Color Tint(Color color, float alpha) {
        color.a = alpha;
        return color;
    }

    public static Image AddImage(RectTransform rect, Color color) {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI CreateLabel(
        string labelName,
        Transform parent,
        string text,
        float size,
        Color color
    ) {
        RectTransform rect = CreateRect(labelName, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

}
