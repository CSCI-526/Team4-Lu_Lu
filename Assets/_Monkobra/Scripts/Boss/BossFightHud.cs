using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space UI of the boss fight, built at runtime on the root Canvas
/// (below the pause, tutorial and lose screens): the entry flash, the Cobra
/// King's name and health bar, its speech bubble over the head, the first
/// throw's key prompt over a banana, damage numbers and the end banner.
/// Every piece is a plain uGUI Image or TMP label, no sprite needed.
/// </summary>
public class BossFightHud: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>Builds the UI and shows the health bar, full.</summary>
    public void Show(string bossName) {
        if (!TryBuild()) {
            return;
        }
        rootRect.gameObject.SetActive(true);
        nameLabel.text = bossName;
        SetHealth(1f);
    }

    /// <summary>Sets the health bar to <paramref name="fraction"/> 0~1.
    /// </summary>
    public void SetHealth(float fraction) {
        if (healthFill == null) {
            return;
        }
        healthFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    /// <summary>Flashes the whole screen white, fading out.</summary>
    public void Flash() {
        Flash(Color.white);
    }

    /// <summary>Flashes the whole screen in <paramref name="color"/>.
    /// </summary>
    public void Flash(Color color) {
        flashColor = color;
        flashLeftS = flashS;
    }

    /// <summary>Shows <paramref name="line"/> in a bubble over the head.
    /// </summary>
    public void Say(string line) {
        if (speechLabel == null) {
            return;
        }
        speechLabel.text = line;
        speechLeftS = speechShowS;
        // the bubble fits its line
        speechRect.sizeDelta = new Vector2(
            Mathf.Max(200f, speechLabel.GetPreferredValues(line).x + 48f),
            56f
        );
    }

    /// <summary>Pops a rising damage number at a world point.</summary>
    public void PopDamage(string text, Vector3 worldPosition) {
        PopText(text, worldPosition, damageColor);
    }

    /// <summary>Pops a rising word at a world point, eg "Blocked!".
    /// </summary>
    public void PopText(string text, Vector3 worldPosition, Color color) {
        if (rootRect == null) {
            return;
        }
        TextMeshProUGUI label = CreateLabel(
            "Pop", rootRect, text, 46f, color
        );
        label.rectTransform.sizeDelta = new Vector2(160f, 60f);
        pops.Add(new Pop {
            label = label,
            worldPosition = worldPosition,
            ageS = 0f
        });
    }

    /// <summary>
    /// Shows the victory panel over the frozen game: the title, the final
    /// score and how to play again.
    /// </summary>
    public void ShowVictory(long score) {
        if (rootRect == null) {
            return;
        }
        SetPrompt(false, Vector3.zero);
        if (speechRect != null) {
            speechLeftS = 0f;
            speechRect.gameObject.SetActive(false);
        }

        RectTransform panel = CreateRect("Victory", rootRect);
        Stretch(panel);
        AddImage(panel, new Color(0f, 0f, 0f, 0.55f));
        AddCenterLabel(panel, "Victory!", 90f, damageColor, 140f);
        AddCenterLabel(
            panel, "You defeated the Cobra King!", 36f, Color.white, 50f
        );
        AddCenterLabel(panel, "Final Score: " + score, 36f, Color.white, -10f);
        AddCenterLabel(
            panel, "Press Space to Play Again", 32f, Color.white, -110f
        );
    }

    /// <summary>Shows a big banner in the screen center.</summary>
    public void ShowBanner(string text) {
        ShowBanner(text, damageColor);
    }

    /// <summary>Shows a big banner in <paramref name="color"/>.</summary>
    public void ShowBanner(string text, Color color) {
        if (bannerLabel == null) {
            return;
        }
        bannerLabel.text = text;
        bannerLabel.color = color;
        bannerLabel.rectTransform.anchoredPosition = bannerCenterPosition;
        bannerLabel.gameObject.SetActive(true);
        bannerHideTime = -1f;
    }

    /// <summary>
    /// Shows a big banner in <paramref name="color"/> with a smaller
    /// <paramref name="subtitle"/> under it, hidden after
    /// <paramref name="seconds"/>, or kept until <see cref="HideBanner"/>
    /// when it is 0; eg the armor intro.
    /// </summary>
    public void ShowBannerFor(
        string text, string subtitle, Color color, float seconds
    ) {
        ShowBanner("<size=70%>" + text + "</size>\n<size=40%><color=#FFFFFF>"
            + subtitle + "</color></size>", color);
        // low on screen, clear of the key prompt & the action mid-screen
        bannerLabel.rectTransform.anchoredPosition = bannerLowPosition;
        bannerHideTime = seconds > 0f ? Time.unscaledTime + seconds : -1f;
    }

    /// <summary>Hides the big banner.</summary>
    public void HideBanner() {
        if (bannerLabel == null) {
            return;
        }
        bannerHideTime = -1f;
        bannerLabel.gameObject.SetActive(false);
    }

    /// <summary>Raised when the revive offer's Watch Ad button is clicked.
    /// </summary>
    public event System.Action WatchAdClicked;

    /// <summary>Raised when the revive offer's Give Up button is clicked.
    /// </summary>
    public event System.Action GiveUpClicked;

    /// <summary>Raised when the ad's Course Page button is clicked.
    /// </summary>
    public event System.Action CoursePageClicked;

    /// <summary>Raised when the ad's Skip button is clicked.</summary>
    public event System.Action SkipAdClicked;

    /// <summary>Shows or hides the ad's Skip button.</summary>
    public void SetAdSkippable(bool canSkip) {
        if (adSkipButton != null && adSkipButton.activeSelf != canSkip) {
            adSkipButton.SetActive(canSkip);
        }
    }

    /// <summary>
    /// Shows or hides the armor badge in the bottom right, kept up while the
    /// monkey keeps the Golden Armor it won; it pops in when shown.
    /// </summary>
    public void ShowArmorBadge(bool isShown) {
        if (rootRect == null) {
            return;
        }
        if (armorBadge == null) {
            armorBadge = CreateArmorBadge();
        }
        armorBadge.gameObject.SetActive(isShown);
        armorBadgePopS = isShown ? ARMOR_BADGE_POP_S : 0f;
        armorBadge.localScale = Vector3.one;
    }

    /// <summary>Hides the king's name & health bar, eg once it is beaten.
    /// </summary>
    public void HideBossBar() {
        if (healthFill != null && healthFill.parent != null) {
            healthFill.parent.gameObject.SetActive(false);
        }
        if (nameLabel != null) {
            nameLabel.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Shows EASY & NORMAL tags over the two mode-choice bananas at
    /// <paramref name="easyWorld"/> & <paramref name="normalWorld"/>, or
    /// hides them.
    /// </summary>
    public void SetChoiceTags(
        bool isShown, Vector3 easyWorld, Vector3 normalWorld
    ) {
        if (rootRect == null) {
            return;
        }
        if (easyTag == null) {
            easyTag = CreateTag("EasyTag", "EASY",
                new Color(0.55f, 0.95f, 0.55f));
            normalTag = CreateTag("NormalTag", "NORMAL",
                new Color(1f, 0.82f, 0.2f));
        }
        easyTag.gameObject.SetActive(isShown);
        normalTag.gameObject.SetActive(isShown);
        easyTagWorld = easyWorld;
        normalTagWorld = normalWorld;
    }

    /// <summary>
    /// Shows the revive offer: a card asking to watch an ad, with a
    /// countdown and Watch Ad / Give Up buttons.
    /// </summary>
    public void ShowReviveOffer() {
        if (!BuildRevive()) {
            return;
        }
        revivePanel.SetActive(true);
        adPanel.SetActive(false);
    }

    /// <summary>Sets the offer's countdown to <paramref name="seconds"/>.
    /// </summary>
    public void SetReviveCountdown(int seconds) {
        if (reviveCountLabel != null) {
            reviveCountLabel.text = seconds.ToString();
        }
    }

    /// <summary>Swaps the offer for the full-screen mock ad.</summary>
    public void ShowAd() {
        if (!BuildRevive()) {
            return;
        }
        revivePanel.SetActive(false);
        adPanel.SetActive(true);
    }

    /// <summary>Hides the revive offer and the ad.</summary>
    public void HideRevive() {
        if (revivePanel != null) {
            revivePanel.SetActive(false);
        }
        if (adPanel != null) {
            adPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Keeps the key prompt over <paramref name="worldPosition"/>, or hides
    /// it when <paramref name="isShown"/> is false.
    /// </summary>
    public void SetPrompt(bool isShown, Vector3 worldPosition) {
        if (keyPrompt != null) {
            keyPrompt.SetPrompt(isShown, worldPosition, promptOffset);
        }
    }

    /// <summary>
    /// Shows an attack warning over <paramref name="worldPosition"/>: a red
    /// sign with "!" and <paramref name="word"/> under it, beating faster as
    /// <paramref name="progress"/> 0~1 nears the hit.
    /// </summary>
    /// <returns>the warning's id for <see cref="UpdateWarning"/></returns>
    public int AddWarning(Vector3 worldPosition, string word) {
        if (rootRect == null) {
            return -1;
        }
        RectTransform sign = CreateRect("Warning", rootRect);
        sign.sizeDelta = new Vector2(70f, 70f);

        // a red diamond with a white border & a big "!"
        RectTransform border = CreateRect("Border", sign);
        Stretch(border);
        border.localRotation = Quaternion.Euler(0f, 0f, 45f);
        border.sizeDelta = Vector2.zero;
        AddImage(border, Color.white);
        RectTransform face = CreateRect("Face", border);
        Stretch(face);
        face.offsetMin = new Vector2(5f, 5f);
        face.offsetMax = new Vector2(-5f, -5f);
        AddImage(face, warningColor);
        TextMeshProUGUI mark = CreateLabel("Mark", sign, "!", 56f, Color.white);
        Stretch(mark.rectTransform);
        mark.fontStyle = FontStyles.Bold;

        TextMeshProUGUI label = CreateLabel(
            "Word", sign, word, 30f, warningColor
        );
        label.fontStyle = FontStyles.Bold;
        label.outlineWidth = 0.25f;
        label.outlineColor = Color.white;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -14f);
        labelRect.sizeDelta = new Vector2(240f, 40f);

        warnings.Add(new Warning {
            id = nextWarningId,
            sign = sign,
            worldPosition = worldPosition,
            progress = 0f
        });
        return nextWarningId++;
    }

    /// <summary>Moves warning <paramref name="id"/> & sets its progress 0~1.
    /// </summary>
    public void UpdateWarning(int id, Vector3 worldPosition, float progress) {
        int index = warnings.FindIndex(w => w.id == id);
        if (index < 0) {
            return;
        }
        Warning warning = warnings[index];
        warning.worldPosition = worldPosition;
        warning.progress = Mathf.Clamp01(progress);
        warnings[index] = warning;
    }

    /// <summary>Removes the warning <paramref name="id"/>.</summary>
    public void RemoveWarning(int id) {
        int index = warnings.FindIndex(w => w.id == id);
        if (index < 0) {
            return;
        }
        if (warnings[index].sign != null) {
            Destroy(warnings[index].sign.gameObject);
        }
        warnings.RemoveAt(index);
    }

    /// <summary>Removes every attack warning.</summary>
    public void ClearWarnings() {
        foreach (Warning warning in warnings) {
            if (warning.sign != null) {
                Destroy(warning.sign.gameObject);
            }
        }
        warnings.Clear();
    }

    /// <summary>World point the speech bubble follows, eg the king's head.
    /// </summary>
    public Vector3 SpeechAnchor {
        set => speechWorldPosition = value;
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("how long the entry flash takes to fade; s")]
    private float flashS = 0.8f;

    [SerializeField]
    [Tooltip("how long a speech line stays up; s")]
    private float speechShowS = 2.2f;

    [SerializeField]
    [Tooltip("bubble offset over the head, in canvas units")]
    private Vector2 speechOffset = new Vector2(0f, 90f);

    [SerializeField]
    [Tooltip("prompt offset over the banana, in canvas units")]
    private Vector2 promptOffset = new Vector2(0f, 80f);

    [SerializeField]
    private Color healthColor = new Color(0.85f, 0.15f, 0.15f);

    [SerializeField]
    private Color damageColor = new Color(1f, 0.85f, 0.2f);

    [SerializeField]
    private Color warningColor = new Color(0.95f, 0.15f, 0.15f);

    [SerializeField]
    [Tooltip("warning sign offset over its zone, in canvas units")]
    private Vector2 warningOffset = new Vector2(0f, 70f);

    // MonoBehaviour Lifecycle  ################################################
    private void Update() {
        if (rootRect == null) {
            return;
        }

        UpdateFlash();
        UpdateAd();
        UpdateArmorBadge();
        if (bannerHideTime > 0f && Time.unscaledTime >= bannerHideTime) {
            bannerHideTime = -1f;
            bannerLabel.gameObject.SetActive(false);
        }
        if (speechRect != null) {
            speechLeftS -= Time.deltaTime;
            bool isSpeaking = speechLeftS > 0f;
            speechRect.gameObject.SetActive(isSpeaking);
            if (isSpeaking) {
                PlaceSpeech();
            }
        }
        if (easyTag != null && easyTag.gameObject.activeSelf) {
            Place(easyTag, easyTagWorld, tagOffset);
            Place(normalTag, normalTagWorld, tagOffset);
        }
        UpdatePops();
        UpdateWarnings();
    }

    // signs beat faster & grow as the hit nears
    private void UpdateWarnings() {
        foreach (Warning warning in warnings) {
            if (warning.sign == null) {
                continue;
            }
            Place(warning.sign, warning.worldPosition, warningOffset);
            float beatHz = Mathf.Lerp(2f, 9f, warning.progress);
            float beat = 0.5f + 0.5f * Mathf.Sin(Time.time * beatHz * 2f * Mathf.PI);
            float scale = Mathf.Lerp(0.9f, 1.3f, warning.progress)
                * (1f + 0.12f * beat);
            warning.sign.localScale = Vector3.one * scale;
        }
    }

    // private methods  ########################################################
    private bool TryBuild() {
        if (rootRect != null) {
            return true;
        }
        Canvas canvas = FindRootCanvas();
        if (canvas == null) {
            Debug.LogError("BossFightHud:\tno root Canvas found", this);
            return false;
        }
        canvasRect = (RectTransform)canvas.transform;
        canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        rootRect = CreateRect("BossFightHud", canvasRect);
        Stretch(rootRect);
        // under the screens, so pause, tutorial & lose panels cover it
        Transform screens = FindChildNamed(canvasRect, "Screens");
        if (screens != null) {
            rootRect.SetSiblingIndex(screens.GetSiblingIndex());
        }

        // name & health bar, top center
        RectTransform bar = CreateRect("HealthBar", rootRect);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = new Vector2(0f, -54f);
        bar.sizeDelta = new Vector2(440f, 22f);
        AddImage(bar, new Color(0f, 0f, 0f, 0.6f));
        healthFill = CreateRect("Fill", bar);
        Stretch(healthFill);
        healthFill.offsetMin = new Vector2(3f, 3f);
        healthFill.offsetMax = new Vector2(-3f, -3f);
        healthFill.anchorMax = new Vector2(1f, 1f);
        AddImage(healthFill, healthColor);
        nameLabel = CreateLabel("Name", rootRect, "", 28f, Color.white);
        RectTransform nameRect = nameLabel.rectTransform;
        nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -16f);
        nameRect.sizeDelta = new Vector2(440f, 36f);

        // speech bubble
        speechRect = CreateRect("Speech", rootRect);
        speechRect.sizeDelta = new Vector2(360f, 56f);
        AddImage(speechRect, new Color(0f, 0f, 0f, 0.75f));
        speechLabel = CreateLabel("Line", speechRect, "", 28f, Color.white);
        Stretch(speechLabel.rectTransform);
        speechRect.gameObject.SetActive(false);

        // Same keycap component as the fruit tutorial.
        keyPrompt = KeyPromptUI.Create(rootRect);

        // banner, screen center
        bannerLabel = CreateLabel(
            "Banner", rootRect, "", 80f, new Color(1f, 0.85f, 0.2f)
        );
        bannerLabel.rectTransform.sizeDelta = new Vector2(1100f, 240f);
        bannerLabel.rectTransform.anchoredPosition = new Vector2(0f, 120f);
        bannerLabel.gameObject.SetActive(false);

        // entry flash, on top of the rest of the hud
        RectTransform flash = CreateRect("Flash", rootRect);
        Stretch(flash);
        flashImage = AddImage(flash, new Color(1f, 1f, 1f, 0f));
        return true;
    }

    private void UpdateFlash() {
        if (flashImage == null) {
            return;
        }
        if (flashLeftS > 0f) {
            flashLeftS -= Time.unscaledDeltaTime;
        }
        float alpha = flashS > 0f ? Mathf.Clamp01(flashLeftS / flashS) : 0f;
        flashImage.color = new Color(
            flashColor.r, flashColor.g, flashColor.b, alpha
        );
        flashImage.enabled = alpha > 0f;
    }

    private void UpdatePops() {
        for (int i = pops.Count - 1; i >= 0; i--) {
            Pop pop = pops[i];
            pop.ageS += Time.deltaTime;
            float t = pop.ageS / POP_S;
            if (t >= 1f || pop.label == null) {
                if (pop.label != null) {
                    Destroy(pop.label.gameObject);
                }
                pops.RemoveAt(i);
                continue;
            }
            Place(
                pop.label.rectTransform,
                pop.worldPosition,
                new Vector2(0f, 40f + t * 80f)
            );
            pop.label.alpha = 1f - t * t;
            pops[i] = pop;
        }
    }

    // keeps a UI rect over a world point, hidden behind the camera
    private void Place(RectTransform target, Vector3 world, Vector2 offset) {
        Camera cam = Camera.main;
        if (cam == null || canvasRect == null) {
            return;
        }
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z < 0f) {
            target.anchoredPosition = new Vector2(0f, -10000f);
            return;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screen, canvasCamera, out Vector2 local
        );
        target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = local + offset;
    }

    // like Place, but kept inside the screen: a head off screen or behind
    // the camera still shows its line, pinned to the nearest edge
    // over the king's head while it is well in view; otherwise, eg the
    // camera turned away or the head off screen, docked under the health bar
    // with the king's name, so a line is never missed
    private void PlaceSpeech() {
        Camera cam = Camera.main;
        Vector3 view = cam != null
            ? cam.WorldToViewportPoint(speechWorldPosition)
            : Vector3.back;
        bool isHeadInView = view.z > 0f
            && view.x > 0.1f && view.x < 0.9f
            && view.y > 0.12f && view.y < 0.75f;
        if (isHeadInView) {
            PlaceOnScreen(speechRect, speechWorldPosition, speechOffset);
            return;
        }
        speechRect.anchorMin = speechRect.anchorMax = new Vector2(0.5f, 1f);
        speechRect.pivot = new Vector2(0.5f, 1f);
        speechRect.anchoredPosition = new Vector2(0f, -96f);
    }

    private void PlaceOnScreen(
        RectTransform target,
        Vector3 world,
        Vector2 offset
    ) {
        Camera cam = Camera.main;
        if (cam == null || canvasRect == null) {
            return;
        }
        Vector2 half = canvasRect.rect.size * 0.5f;
        Vector2 box = target.rect.size * 0.5f + new Vector2(16f, 16f);
        Vector2 local;
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z < 0f) {
            // behind the camera: the top of the screen, where the king is
            local = new Vector2(0f, half.y);
        } else {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screen, canvasCamera, out local
            );
            local += offset;
        }
        // stay clear of the name & health bar at the top
        float top = half.y - box.y - 90f;
        local.x = Mathf.Clamp(local.x, -half.x + box.x, half.x - box.x);
        local.y = Mathf.Clamp(local.y, -half.y + box.y, top);
        target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = local;
    }

    private static Canvas FindRootCanvas() {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(
            FindObjectsSortMode.None
        )) {
            if (canvas.isRootCanvas
                && canvas.renderMode != RenderMode.WorldSpace) {
                return canvas;
            }
        }
        return null;
    }

    private static Transform FindChildNamed(Transform parent, string part) {
        foreach (Transform child in parent) {
            if (child.name.Contains(part)) {
                return child;
            }
        }
        return null;
    }

    // the revive offer card & the mock ad, built on first use
    private bool BuildRevive() {
        if (revivePanel != null) {
            return true;
        }
        if (rootRect == null) {
            return false;
        }
        Color gold = new Color(1f, 0.82f, 0.2f);

        RectTransform offer = CreateRect("ReviveOffer", rootRect);
        Stretch(offer);
        AddImage(offer, new Color(0f, 0f, 0f, 0.6f));
        RectTransform card = CreateRect("Card", offer);
        card.sizeDelta = new Vector2(580f, 340f);
        AddImage(card, new Color(0.13f, 0.1f, 0.2f, 0.95f));
        PlaceLabel(CreateLabel("Title", card, "Revive?", 60f, gold),
            new Vector2(0f, 110f), new Vector2(540f, 70f));
        PlaceLabel(CreateLabel(
                "Line", card, "Watch a short ad to keep fighting!", 26f,
                Color.white
            ), new Vector2(0f, 50f), new Vector2(540f, 40f));
        reviveCountLabel = CreateLabel("Count", card, "", 48f, gold);
        PlaceLabel(reviveCountLabel, new Vector2(0f, -10f),
            new Vector2(200f, 60f));
        CreateButton("WatchAd", card, "Watch Ad  [Space]",
            new Color(0.2f, 0.7f, 0.3f), new Vector2(-135f, -105f),
            () => WatchAdClicked?.Invoke());
        CreateButton("GiveUp", card, "Give Up  [R]",
            new Color(0.4f, 0.4f, 0.45f), new Vector2(135f, -105f),
            () => GiveUpClicked?.Invoke());
        revivePanel = offer.gameObject;
        revivePanel.SetActive(false);

        // the "ad" plugs the course this game is made in: cardinal &
        // gold, bobbing shapes, a Course Page button and a timer
        Color cardinal = new Color(0.6f, 0.05f, 0.08f);
        RectTransform ad = CreateRect("MockAd", rootRect);
        Stretch(ad);
        AddImage(ad, cardinal);
        Color[] shapeColors = {
            gold, new Color(1f, 1f, 1f, 0.75f), new Color(1f, 0.6f, 0.2f),
        };
        for (int i = 0; i < 8; i++) {
            RectTransform shape = CreateRect("Shape" + i, ad);
            float size = 40f + 25f * (i % 3);
            shape.sizeDelta = new Vector2(size, size);
            shape.anchoredPosition = new Vector2(
                -520f + i * 150f, i % 2 == 0 ? 250f : -250f
            );
            AddImage(shape, shapeColors[i % shapeColors.Length]);
            adShapes.Add(shape);
        }
        TextMeshProUGUI title = CreateLabel(
            "Title", ad, "Like this game? Make your own!", 64f, gold
        );
        PlaceLabel(title, new Vector2(0f, 110f), new Vector2(1150f, 90f));
        adTitle = title.rectTransform;
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
        CreateButton("CoursePage", ad, "Course Page  [Enter]", gold * 0.85f,
            new Vector2(0f, -150f), () => CoursePageClicked?.Invoke());
        RectTransform tag = CreateRect("AdTag", ad);
        tag.anchorMin = tag.anchorMax = tag.pivot = new Vector2(0f, 1f);
        tag.anchoredPosition = new Vector2(24f, -24f);
        tag.sizeDelta = new Vector2(70f, 40f);
        AddImage(tag, new Color(0f, 0f, 0f, 0.6f));
        TextMeshProUGUI tagLabel = CreateLabel("Label", tag, "AD", 24f,
            Color.white);
        Stretch(tagLabel.rectTransform);
        adSkipButton = CreateButton("Skip", ad, "Skip  [Space]",
            new Color(0f, 0f, 0f, 0.55f), Vector2.zero,
            () => SkipAdClicked?.Invoke());
        RectTransform skip = (RectTransform)adSkipButton.transform;
        skip.anchorMin = skip.anchorMax = skip.pivot = new Vector2(1f, 1f);
        skip.anchoredPosition = new Vector2(-24f, -24f);
        adSkipButton.SetActive(false);
        adPanel = ad.gameObject;
        adPanel.SetActive(false);
        return true;
    }

    // the ad's title throbs and its shapes bob & spin; time stands still
    // under it, so this runs on unscaled time
    private void UpdateAd() {
        if (adPanel == null || !adPanel.activeSelf) {
            return;
        }
        float t = Time.unscaledTime;
        adTitle.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 6f));
        for (int i = 0; i < adShapes.Count; i++) {
            adShapes[i].localRotation = Quaternion.Euler(
                0f, 0f, t * (40f + i * 12f) * (i % 2 == 0 ? 1f : -1f)
            );
            adShapes[i].localScale = Vector3.one
                * (1f + 0.15f * Mathf.Sin(t * 3f + i));
        }
    }

    // a dark plate w/ a gold diamond & GOLDEN ARMOR
    private RectTransform CreateArmorBadge() {
        Color gold = new Color(1f, 0.82f, 0.2f);
        RectTransform badge = CreateRect("ArmorBadge", rootRect);
        badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(1f, 0f);
        // left of the progress bar along the right edge
        badge.anchoredPosition = new Vector2(-70f, 24f);
        badge.sizeDelta = new Vector2(250f, 54f);
        AddImage(badge, new Color(0f, 0f, 0f, 0.65f));
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

    // the badge pops in big & settles; unscaled, so a pause never stalls it
    private void UpdateArmorBadge() {
        if (armorBadge == null || armorBadgePopS <= 0f) {
            return;
        }
        armorBadgePopS = Mathf.Max(0f, armorBadgePopS - Time.unscaledDeltaTime);
        float left = armorBadgePopS / ARMOR_BADGE_POP_S;
        armorBadge.localScale = Vector3.one * (1f + 0.8f * left * left);
    }

    // a dark plate w/ a colored word, placed over a world point
    private RectTransform CreateTag(string tagName, string text, Color color) {
        RectTransform tag = CreateRect(tagName, rootRect);
        tag.sizeDelta = new Vector2(130f, 40f);
        AddImage(tag, new Color(0f, 0f, 0f, 0.7f));
        TextMeshProUGUI label = CreateLabel("Label", tag, text, 26f, color);
        Stretch(label.rectTransform);
        tag.gameObject.SetActive(false);
        return tag;
    }

    private static void PlaceLabel(
        TextMeshProUGUI label, Vector2 position, Vector2 size
    ) {
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = size;
    }

    private static GameObject CreateButton(
        string buttonName,
        Transform parent,
        string text,
        Color color,
        Vector2 position,
        System.Action onClick
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
            Color.white);
        Stretch(label.rectTransform);
        return rect.gameObject;
    }

    private static RectTransform CreateRect(string rectName, Transform parent) {
        var go = new GameObject(rectName, typeof(RectTransform));
        go.layer = 5; // UI
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void AddCenterLabel(
        RectTransform parent,
        string text,
        float size,
        Color color,
        float y
    ) {
        TextMeshProUGUI label = CreateLabel("Line", parent, text, size, color);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);
        label.rectTransform.sizeDelta = new Vector2(1000f, size * 1.4f);
        label.rectTransform.anchoredPosition = new Vector2(0f, y);
    }

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Image AddImage(RectTransform rect, Color color) {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateLabel(
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

    // private members  ########################################################
    private const float POP_S = 0.9f; // damage number lifetime; s

    private struct Pop {
        public TextMeshProUGUI label;
        public Vector3 worldPosition;
        public float ageS;
    }

    private struct Warning {
        public int id;
        public RectTransform sign;
        public Vector3 worldPosition;
        public float progress; // 0 when shown, 1 at the hit
    }

    private readonly List<Pop> pops = new List<Pop>();
    private readonly List<Warning> warnings = new List<Warning>();
    private int nextWarningId;
    private float flashLeftS;
    private Color flashColor = Color.white;
    private float speechLeftS;
    private Vector3 speechWorldPosition;

    // cached references  ------------------------------------------------------
    private RectTransform canvasRect;
    private Camera canvasCamera; // null for an overlay canvas
    private RectTransform rootRect;
    private RectTransform healthFill;
    private TextMeshProUGUI nameLabel;
    private RectTransform speechRect;
    private TextMeshProUGUI speechLabel;
    private KeyPromptUI keyPrompt;
    private TextMeshProUGUI bannerLabel;
    private GameObject revivePanel;
    private TextMeshProUGUI reviveCountLabel;
    private GameObject adPanel;
    private RectTransform adTitle;
    private readonly List<RectTransform> adShapes = new List<RectTransform>();
    private GameObject adSkipButton;
    private RectTransform armorBadge;
    private float armorBadgePopS; // pop-in time left; unscaled s
    private const float ARMOR_BADGE_POP_S = 0.45f;
    private RectTransform easyTag;
    private RectTransform normalTag;
    private Vector3 easyTagWorld;
    private Vector3 normalTagWorld;
    private readonly Vector2 tagOffset = new Vector2(0f, 55f);
    private readonly Vector2 bannerCenterPosition = new Vector2(0f, 120f);
    private readonly Vector2 bannerLowPosition = new Vector2(0f, -150f);
    private float bannerHideTime = -1f; // unscaled time a timed banner goes
    private Image flashImage;
}
