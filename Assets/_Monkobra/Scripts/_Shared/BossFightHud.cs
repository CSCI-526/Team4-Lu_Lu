using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BossFightHudRig;

/// <summary>
/// Screen-space UI of the boss fight, built at runtime on the root Canvas
/// (below the pause, tutorial and lose screens): the entry flash, the Cobra
/// King's name and health bar, its speech bubble over the head, the first
/// throw's key prompt over a banana, damage numbers and the end banner.
/// Every piece is a plain uGUI Image or TMP label, no sprite needed.
/// </summary>
public class BossFightHud: MonoBehaviour {
    public const float CALL_OUT_S = BossFightHudRig.CALL_OUT_S;
    // Inspector Fields  #######################################################
    [SerializeField]
    private BossFightHudTuning tuning = new();

    // Private Members  ########################################################
    private BossFightHudRig rig;
    private BossFightHudBuilder builder;
    private BossFightHudMotion motion;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        rig = new BossFightHudRig(this, tuning);
        builder =
            new BossFightHudBuilder(rig, () => WatchAdClicked?.Invoke(),
                () => GiveUpClicked?.Invoke(),
                    () => CoursePageClicked?.Invoke(),
                        () => ContinueClicked?.Invoke(),
                            () => SkipAdClicked?.Invoke());
        motion = new BossFightHudMotion(rig);
        rig.Builder = builder;
        rig.Motion = motion;
    }

    /// <summary>Builds the UI and shows the health bar, full.</summary>
    public void Show(string bossName) {
        if (!builder.TryBuild()) {
            return;
        }
        rig.rootRect.gameObject.SetActive(true);
        rig.nameLabel.text = bossName;
        SetHealth(1f);
    }

    /// <summary>
    /// Builds the UI w/o the name & health bar, eg for a fight skipped as
    /// beaten, so its armor badge, "Saved!" & flash still show.
    /// </summary>
    public void BuildQuiet() {
        if (!builder.TryBuild()) {
            return;
        }
        rig.rootRect.gameObject.SetActive(true);
        HideBossBar();
    }

    /// <summary>Sets the health bar to <paramref name="fraction"/> 0~1.
    /// </summary>
    public void SetHealth(float fraction) {
        if (rig.healthFill == null) {
            return;
        }
        rig.healthFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    /// <summary>Flashes the whole screen white, fading out.</summary>
    public void Flash() {
        Flash(Color.white);
    }

    /// <summary>Flashes the whole screen in <paramref name="color"/>.
    /// </summary>
    public void Flash(Color color) {
        rig.flashColor = color;
        rig.flashLeftS = tuning.FlashS;
    }

    /// <summary>Shows <paramref name="line"/> in a bubble over the head.
    /// </summary>
    public void Say(string line) {
        if (rig.speechLabel == null) {
            return;
        }
        rig.speechLabel.text = line;
        rig.speechLeftS = tuning.SpeechShowS;
        // the bubble fits its line
        rig.speechRect.sizeDelta = new Vector2(
            Mathf.Max(200f, rig.speechLabel.GetPreferredValues(line).x + 48f),
            56f
        );
    }

    /// <summary>
    /// A line the boss calls out: it shows in a bubble at its head, then
    /// glides up to dock under the health bar & stays there till the next
    /// call out or <see cref="ClearCallOut"/>.
    /// </summary>
    public void CallOut(string line, Vector3 worldPosition, Color color) {
        if (rig.rootRect == null) {
            return;
        }
        ClearCallOut();
        rig.callOutRect =
            BossFightHudBuilder.CreateRect("CallOut", rig.rootRect);
        BossFightHudBuilder.AddImage(rig.callOutRect,
            BossFightHudBuilder.Tint(UIStyle.Panel, 0.85f));
        rig.callOutLabel =
            BossFightHudBuilder.CreateLabel("Line", rig.callOutRect, line, 34f,
                color);
        rig.callOutLabel.fontStyle = FontStyles.Bold;
        BossFightHudBuilder.Stretch(rig.callOutLabel.rectTransform);
        rig.callOutRect.sizeDelta = new Vector2(
            Mathf.Max(260f, rig.callOutLabel.GetPreferredValues(line).x + 56f),
            64f
        );
        rig.callOutWorld = worldPosition;
        rig.callOutAgeS = 0f;
    }

    /// <summary>Pops a rising damage number at a world point.</summary>
    public void PopDamage(string text, Vector3 worldPosition) {
        PopText(text, worldPosition, tuning.DamageColor);
    }

    /// <summary>Pops a rising word at a world point, eg "Blocked!".
    /// </summary>
    public void PopText(string text, Vector3 worldPosition, Color color) {
        if (rig.rootRect == null) {
            return;
        }
        TextMeshProUGUI label = BossFightHudBuilder.CreateLabel(
            "Pop", rig.rootRect, text, 46f, color
        );
        label.rectTransform.sizeDelta = new Vector2(160f, 60f);
        rig.pops.Add(new BossFightHudPop {
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
        if (rig.rootRect == null) {
            return;
        }
        SetPrompt(false, Vector3.zero);
        if (rig.speechRect != null) {
            rig.speechLeftS = 0f;
            rig.speechRect.gameObject.SetActive(false);
        }

        RectTransform panel =
            BossFightHudBuilder.CreateRect("Victory", rig.rootRect);
        BossFightHudBuilder.Stretch(panel);
        BossFightHudBuilder.AddImage(panel,
            BossFightHudBuilder.Tint(UIStyle.Background, 0.8f));
        BossFightHudBuilder.AddCenterLabel(panel, "Victory!", 90f,
            tuning.DamageColor, 140f);
        BossFightHudBuilder.AddCenterLabel(
            panel, "You defeated the Cobra King!", 36f, UIStyle.Cream, 50f
        );
        BossFightHudBuilder.AddCenterLabel(panel, "Final Score: " + score, 36f,
            UIStyle.Cream, -10f);
        BossFightHudBuilder.AddCenterLabel(
            panel, "Press Space to Play Again", 32f, UIStyle.Cream, -110f,
            ""
        );
    }

    /// <summary>Shows a big banner in the screen center.</summary>
    public void ShowBanner(string text) {
        ShowBanner(text, tuning.DamageColor);
    }

    /// <summary>Shows a big banner in <paramref name="color"/>.</summary>
    public void ShowBanner(string text, Color color) {
        if (rig.bannerLabel == null) {
            return;
        }
        rig.bannerLabel.text = text;
        rig.bannerLabel.color = color;
        ShowDoubleTapIcon(false);
        rig.bannerLabel.rectTransform.anchoredPosition =
            rig.bannerCenterPosition;
        rig.bannerLabel.gameObject.SetActive(true);
        rig.bannerHideTime = -1f;
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
        ShowBanner("<size=70%>" + text + "</size>\n<size=40%><color=#FFF2D2>"
            + subtitle + "</color></size>", color);
        // low on screen, clear of the key prompt & the action mid-screen
        rig.bannerLabel.rectTransform.anchoredPosition = rig.bannerLowPosition;
        rig.bannerHideTime = seconds > 0f ? Time.unscaledTime + seconds : -1f;
    }

    /// <summary>
    /// Shows the animated double tap icon under the low banner, the keycap
    /// of a keyboard or the finger of touch; it goes w/ the next banner or
    /// <see cref="HideBanner"/>. Call it after the banner it teaches under.
    /// </summary>
    public void ShowDoubleTapHint() {
        ShowDoubleTapIcon(true);
    }

    /// <summary>Hides the big banner.</summary>
    public void HideBanner() {
        ShowDoubleTapIcon(false);
        if (rig.bannerLabel == null) {
            return;
        }
        rig.bannerHideTime = -1f;
        rig.bannerLabel.gameObject.SetActive(false);
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

    /// <summary>Raised when the reward panel's Continue button is clicked.
    /// </summary>
    public event System.Action ContinueClicked;

    /// <summary>
    /// Shows the win's reward panel over a dimmed screen: a gold title, the
    /// reward line, a small detail line & a Continue (Space) button.
    /// </summary>
    public void ShowRewardPanel(string title, string line, string detail) {
        if (rig.rootRect == null) {
            return;
        }
        if (rig.rewardPanel == null) {
            rig.rewardPanel = builder.CreateRewardPanel();
        }
        rig.rewardTitle.text = title;
        rig.rewardLine.text = line;
        rig.rewardDetail.text = detail;
        rig.rewardPanel.SetActive(true);
        rig.rewardAgeS = 0f;
    }

    /// <returns>if the win's panel has played in far enough to be closed, so
    /// a Space still mashed from the fight never skips it</returns>
    public bool IsRewardReady => rig.rewardPanel != null
        && rig.rewardPanel.activeSelf
        && rig.rewardAgeS >= REWARD_READY_S;

    /// <summary>Hides the win's reward panel.</summary>
    public void HideRewardPanel() {
        if (rig.rewardPanel != null) {
            rig.rewardPanel.SetActive(false);
        }
    }

    /// <summary>Raised when the ad's Skip button is clicked.</summary>
    public event System.Action SkipAdClicked;

    /// <summary>Shows or hides the ad's Skip button.</summary>
    public void SetAdSkippable(bool canSkip) {
        if (rig.adSkipButton != null
            && rig.adSkipButton.activeSelf != canSkip) {
            rig.adSkipButton.SetActive(canSkip);
        }
    }

    /// <summary>
    /// Shows or hides the armor badge in the bottom right, kept up while the
    /// monkey keeps the Golden Armor it won; it pops in when shown.
    /// </summary>
    public void ShowArmorBadge(bool isShown) {
        if (rig.rootRect == null) {
            return;
        }
        if (rig.armorBadge == null) {
            rig.armorBadge = builder.CreateArmorBadge();
        }
        rig.armorBadge.gameObject.SetActive(isShown);
        rig.armorBadgePopS = isShown ? ARMOR_BADGE_POP_S : 0f;
        rig.armorBadge.localScale = Vector3.one;
    }

    /// <summary>Hides the king's name & health bar, eg once it is beaten.
    /// </summary>
    public void HideBossBar() {
        if (rig.healthFill != null && rig.healthFill.parent != null) {
            rig.healthFill.parent.gameObject.SetActive(false);
        }
        if (rig.nameLabel != null) {
            rig.nameLabel.gameObject.SetActive(false);
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
        if (rig.rootRect == null) {
            return;
        }
        if (rig.easyTag == null) {
            rig.easyTag = builder.CreateTag("EasyTag", "EASY",
                new Color(0.55f, 0.95f, 0.55f));
            rig.normalTag =
                builder.CreateTag("NormalTag", "NORMAL", UIStyle.Gold);
        }
        rig.easyTag.gameObject.SetActive(isShown);
        rig.normalTag.gameObject.SetActive(isShown);
        rig.easyTagWorld = easyWorld;
        rig.normalTagWorld = normalWorld;
    }

    /// <summary>
    /// Shows the revive offer: a card asking to watch an ad, with a
    /// countdown and Watch Ad / Give Up buttons.
    /// </summary>
    public void ShowReviveOffer() {
        if (!builder.BuildRevive()) {
            return;
        }
        rig.revivePanel.SetActive(true);
        rig.adPanel.SetActive(false);
    }

    /// <summary>Sets the offer's countdown to <paramref name="seconds"/>.
    /// </summary>
    public void SetReviveCountdown(int seconds) {
        if (rig.reviveCountLabel != null) {
            rig.reviveCountLabel.text = seconds.ToString();
        }
    }

    /// <summary>Swaps the offer for the full-screen mock ad.</summary>
    public void ShowAd() {
        if (!builder.BuildRevive()) {
            return;
        }
        rig.revivePanel.SetActive(false);
        rig.adPanel.SetActive(true);
    }

    /// <summary>Hides the revive offer and the ad.</summary>
    public void HideRevive() {
        if (rig.revivePanel != null) {
            rig.revivePanel.SetActive(false);
        }
        if (rig.adPanel != null) {
            rig.adPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Keeps the key prompt over <paramref name="worldPosition"/>, or hides
    /// it when <paramref name="isShown"/> is false.
    /// </summary>
    public void SetPrompt(bool isShown, Vector3 worldPosition) {
        if (rig.keyPrompt != null) {
            rig.keyPrompt.SetPrompt(isShown, worldPosition,
                tuning.PromptOffset);
        }
    }

    /// <summary>
    /// Shows an attack warning over <paramref name="worldPosition"/>: a red
    /// sign with "!" and <paramref name="word"/> under it, beating faster as
    /// <paramref name="progress"/> 0~1 nears the hit.
    /// </summary>
    /// <returns>the warning's id for <see cref="UpdateWarning"/></returns>
    public int AddWarning(Vector3 worldPosition, string word) {
        if (rig.rootRect == null) {
            return -1;
        }
        RectTransform sign =
            BossFightHudBuilder.CreateRect("Warning", rig.rootRect);
        sign.sizeDelta = new Vector2(70f, 70f);

        // a red diamond with a white border & a big "!"
        RectTransform border = BossFightHudBuilder.CreateRect("Border", sign);
        BossFightHudBuilder.Stretch(border);
        border.localRotation = Quaternion.Euler(0f, 0f, 45f);
        border.sizeDelta = Vector2.zero;
        BossFightHudBuilder.AddImage(border, Color.white);
        RectTransform face = BossFightHudBuilder.CreateRect("Face", border);
        BossFightHudBuilder.Stretch(face);
        face.offsetMin = new Vector2(5f, 5f);
        face.offsetMax = new Vector2(-5f, -5f);
        BossFightHudBuilder.AddImage(face, tuning.WarningColor);
        TextMeshProUGUI mark =
            BossFightHudBuilder.CreateLabel("Mark", sign, "!", 56f,
                Color.white);
        BossFightHudBuilder.Stretch(mark.rectTransform);
        mark.fontStyle = FontStyles.Bold;

        TextMeshProUGUI label = BossFightHudBuilder.CreateLabel(
            "Word", sign, word, 30f, tuning.WarningColor
        );
        label.fontStyle = FontStyles.Bold;
        label.outlineWidth = 0.25f;
        label.outlineColor = Color.white;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -14f);
        labelRect.sizeDelta = new Vector2(240f, 40f);

        rig.warnings.Add(new BossFightHudWarning {
            id = rig.nextWarningId,
            sign = sign,
            worldPosition = worldPosition,
            progress = 0f
        });
        return rig.nextWarningId++;
    }

    /// <summary>Moves warning <paramref name="id"/> & sets its progress 0~1.
    /// </summary>
    public void UpdateWarning(int id, Vector3 worldPosition, float progress) {
        int index = rig.warnings.FindIndex(w => w.id == id);
        if (index < 0) {
            return;
        }
        BossFightHudWarning warning = rig.warnings[index];
        warning.worldPosition = worldPosition;
        warning.progress = Mathf.Clamp01(progress);
        rig.warnings[index] = warning;
    }

    /// <summary>Removes the warning <paramref name="id"/>.</summary>
    public void RemoveWarning(int id) {
        int index = rig.warnings.FindIndex(w => w.id == id);
        if (index < 0) {
            return;
        }
        if (rig.warnings[index].sign != null) {
            Destroy(rig.warnings[index].sign.gameObject);
        }
        rig.warnings.RemoveAt(index);
    }

    /// <summary>Removes every attack warning.</summary>
    public void ClearWarnings() {
        foreach (BossFightHudWarning warning in rig.warnings) {
            if (warning.sign != null) {
                Destroy(warning.sign.gameObject);
            }
        }
        rig.warnings.Clear();
    }

    /// <summary>World point the speech bubble follows, eg the king's head.
    /// </summary>
    public Vector3 SpeechAnchor {
        set => rig.speechWorldPosition = value;
    }

    // the icon under the banner, on or off; a no-op w/o the icon
    private void ShowDoubleTapIcon(bool isShown) {
        if (rig.doubleTapIcon != null
            && rig.doubleTapIcon.gameObject.activeSelf != isShown) {
            rig.doubleTapIcon.gameObject.SetActive(isShown);
        }
    }

    private void Update() {
        if (rig.rootRect == null) {
            return;
        }

        motion.UpdateFlash();
        motion.UpdateAd();
        motion.UpdateArmorBadge();
        motion.UpdateRewardPanel();
        if (rig.bannerHideTime > 0f
            && Time.unscaledTime >= rig.bannerHideTime) {
            rig.bannerHideTime = -1f;
            rig.bannerLabel.gameObject.SetActive(false);
            ShowDoubleTapIcon(false);
        }
        if (rig.speechRect != null) {
            rig.speechLeftS -= Time.deltaTime;
            bool isSpeaking = rig.speechLeftS > 0f;
            rig.speechRect.gameObject.SetActive(isSpeaking);
            if (isSpeaking) {
                motion.PlaceSpeech();
            }
        }
        if (rig.easyTag != null && rig.easyTag.gameObject.activeSelf) {
            motion.Place(rig.easyTag, rig.easyTagWorld, rig.tagOffset);
            motion.Place(rig.normalTag, rig.normalTagWorld, rig.tagOffset);
        }
        motion.UpdatePops();
        motion.UpdateWarnings();
        motion.UpdateCallOut();
    }

    /// <summary>Takes the docked call out away.</summary>
    public void ClearCallOut() {
        if (rig.callOutRect != null) {
            Destroy(rig.callOutRect.gameObject);
        }
        rig.callOutRect = null;
        rig.callOutLabel = null;
    }

}
