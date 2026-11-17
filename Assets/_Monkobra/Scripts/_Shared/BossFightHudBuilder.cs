using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BossFightHudRig;

/// <summary>Builds the HUD elements in code: the quiet bar, the revive panel,
/// the reward panel, the armor badge, and the UI factory helpers.</summary>
public sealed class BossFightHudBuilder {
    // Private Members  ########################################################
    private readonly BossFightHudRig rig;
    private readonly BossFightHudTuning tuning;
    private readonly System.Action onWatchAdClicked;
    private readonly System.Action onGiveUpClicked;
    private readonly System.Action onCoursePageClicked;
    private readonly System.Action onContinueClicked;
    private readonly System.Action onSkipAdClicked;
    // the double tap icon's height; canvas units
    private const float DOUBLE_TAP_ICON_SIZE = 96f;

    public BossFightHudBuilder(BossFightHudRig rig,
        System.Action onWatchAdClicked, System.Action onGiveUpClicked,
            System.Action onCoursePageClicked, System.Action onContinueClicked,
                System.Action onSkipAdClicked) {
        this.rig = rig;
        tuning = rig.tuning;
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
        AddImage(rig.healthFill, tuning.HealthColor);
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
            rig.rootRect, tuning.HintIconLibrary,
                tuning.PromptKeyboardController,
            tuning.PromptTouchController
        );

        // banner, screen center
        rig.bannerLabel = CreateLabel(
            "Banner", rig.rootRect, "", 80f, UIStyle.Gold
        );
        rig.bannerLabel.rectTransform.sizeDelta = new Vector2(1100f, 240f);
        rig.bannerLabel.rectTransform.anchoredPosition = new Vector2(0f, 120f);
        rig.bannerLabel.gameObject.SetActive(false);

        // animated double tap icon under the low banner, shown by a lesson
        rig.doubleTapIcon = HintIconSlot.Create(
            rig.rootRect, tuning.HintIconLibrary,
            tuning.DoubleKeyboardController, tuning.DoubleTouchController,
            DOUBLE_TAP_ICON_SIZE
        );
        if (rig.doubleTapIcon != null) {
            rig.doubleTapIcon.anchorMin = rig.doubleTapIcon.anchorMax =
                new Vector2(0.5f, 0.5f);
            rig.doubleTapIcon.sizeDelta = new Vector2(
                DOUBLE_TAP_ICON_SIZE, DOUBLE_TAP_ICON_SIZE);
            rig.doubleTapIcon.anchoredPosition = rig.doubleTapIconPosition;
            rig.doubleTapIcon.gameObject.SetActive(false);
        }

        // entry flash, on top of the rest of the hud
        RectTransform flash = CreateRect("Flash", rig.rootRect);
        Stretch(flash);
        rig.flashImage = AddImage(flash, new Color(1f, 1f, 1f, 0f));
        return true;
    }

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
