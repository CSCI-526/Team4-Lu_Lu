using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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

    /// <returns>the armor mark beside the hearts, where an armor sprite
    /// flies to; null before the fight's hearts show</returns>
    public static RectTransform ArmorMarkTarget { get; private set; }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("every tuned value of the boss fight HUD")]
    private BossFightHudConfig config;

    // private members  ########################################################
    private BossFightHudRig rig;
    private BossFightHudBuilder builder;
    private BossFightHudMotion motion;

    // MonoBehaviour Lifecycle  ################################################
    private void OnEnable() {
        MonkeyHearts.Changed += OnMonkeyHeartsChanged;
    }

    private void OnDisable() {
        MonkeyHearts.Changed -= OnMonkeyHeartsChanged;
    }

    private void OnDestroy() {
        if (rig != null && ArmorMarkTarget == rig.armorMark) {
            ArmorMarkTarget = null;
        }
    }

    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogError(
                "BossFightHud:\tmust assign Inspector Field: config",
                this
            );
            enabled = false;
            return;
        }

        rig = new BossFightHudRig(this, config);
        builder =
            new BossFightHudBuilder(rig, () => WatchAdClicked?.Invoke(),
                () => GiveUpClicked?.Invoke(),
                    () => CoursePageClicked?.Invoke(),
                        () => ContinueClicked?.Invoke(),
                            () => SkipAdClicked?.Invoke());
        motion = new BossFightHudMotion(rig);
        rig.Builder = builder;
        rig.Motion = motion;
        rig.LossLanded = OnLossLanded;
        rig.DamageLanded = OnDamageLanded;
    }

    /// <summary>
    /// Builds the UI and shows the health bar, full, & the boss name: big in
    /// the lower half of the screen first, then shrinking as it glides up to
    /// its place above the bar. It is never shown anywhere else.
    /// </summary>
    public void Show(string bossName) {
        if (!builder.TryBuild()) {
            return;
        }
        rig.rootRect.gameObject.SetActive(true);
        // a fight after a won one: its bar was hidden at that win
        if (rig.healthFill != null && rig.healthFill.parent != null) {
            rig.healthFill.parent.gameObject.SetActive(true);
        }
        rig.nameLabel.gameObject.SetActive(true);
        rig.nameLabel.text = bossName;
        CancelDamageFlights();
        rig.nameIntroAgeS = 0f;
        motion.UpdateNameIntro();
        SetHealth(1f);
        rig.heartHealth = -1;
    }

    /// <summary>
    /// Builds the UI w/o the name & health bar, eg for a fight skipped as
    /// beaten, so its armor badge, loss numbers & flash still show.
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

    /// <summary>
    /// Sets the health bar to <paramref name="health"/> of
    /// <paramref name="maxHealth"/>, its count shown on the heart left of
    /// the bar; the heart pops as the count drops.
    /// </summary>
    public void SetHealth(int health, int maxHealth) {
        CancelDamageFlights();
        ApplyHealth(health, maxHealth);
    }

    private void ApplyHealth(int health, int maxHealth) {
        SetHealth(maxHealth > 0 ? health / (float)maxHealth : 0f);
        if (rig.heartLabel == null) {
            return;
        }
        health = Mathf.Max(0, health);
        if (rig.heartHealth >= 0 && health < rig.heartHealth) {
            rig.heartPopLeftS = HEART_POP_S;
        }
        rig.heartHealth = health;
        rig.heartLabel.text = health.ToString();
    }

    // the heart's pop as the count drops: real s, its biggest scale
    private const float HEART_POP_S = 0.3f;
    private const float HEART_POP_SCALE = 1.35f;

    private void UpdateHeart() {
        if (rig.heart == null) {
            return;
        }
        rig.heartPopLeftS = Mathf.Max(0f,
            rig.heartPopLeftS - Time.unscaledDeltaTime);
        float t = rig.heartPopLeftS / HEART_POP_S;
        rig.heart.localScale =
            Vector3.one * Mathf.Lerp(1f, HEART_POP_SCALE, t * t);
    }

    /// <summary>Flashes the whole screen white, fading out.</summary>
    public void Flash() {
        Flash(Color.white);
    }

    /// <summary>Flashes the whole screen in <paramref name="color"/>.
    /// </summary>
    public void Flash(Color color) {
        rig.flashColor = color;
        rig.flashLeftS = config.FlashS;
    }

    /// <summary>Shows <paramref name="line"/> in a bubble over the head.
    /// </summary>
    public void Say(string line) {
        if (rig.speechLabel == null) {
            return;
        }
        rig.speechLabel.text = line;
        rig.speechLeftS = config.SpeechShowS;
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

    /// <summary>
    /// A deep red -<paramref name="damage"/> flies from a world point (the
    /// head or weak spot) up to the health bar; the bar & heart count drop
    /// to <paramref name="health"/> of <paramref name="maxHealth"/> only as
    /// it lands, so call this in place of <c>SetHealth</c>.
    /// </summary>
    public void PopDamage(
        int damage, int health, int maxHealth, Vector3 worldPosition
    ) {
        if (rig.rootRect == null || rig.healthFill == null) {
            ApplyHealth(health, maxHealth);
            return;
        }
        TextMeshProUGUI label = BossFightHudBuilder.CreateLabel(
            "Damage", rig.rootRect, "-" + damage, config.NumberFontSize,
            config.BossHitColor
        );
        BossFightHudBuilder.UseFont(label, config.WarningFont);
        label.outlineWidth = 0.25f;
        label.outlineColor = Color.white;
        label.rectTransform.sizeDelta = new Vector2(240f, 120f);
        rig.damageFlights.Add(new BossFightHudDamageFlight {
            label = label,
            worldPosition = worldPosition,
            health = health,
            maxHealth = maxHealth,
            ageS = 0f
        });
        motion.UpdateDamageFlights();
    }

    // a -n landed: the bar & heart count drop
    private void OnDamageLanded(BossFightHudDamageFlight flight) {
        ApplyHealth(flight.health, flight.maxHealth);
    }

    // a reset or a hidden bar leaves no stale drop to land later
    private void CancelDamageFlights() {
        foreach (BossFightHudDamageFlight flight in rig.damageFlights) {
            if (flight.label != null) {
                Destroy(flight.label.gameObject);
            }
        }
        rig.damageFlights.Clear();
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
            config.DamageColor, 140f);
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
        ShowBanner(text, config.DamageColor);
    }

    /// <summary>Shows a big banner in <paramref name="color"/>.</summary>
    public void ShowBanner(string text, Color color) {
        if (rig.bannerLabel == null) {
            return;
        }
        rig.bannerLabel.text = text;
        rig.bannerLabel.color = color;
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

    /// <summary>Hides the big banner.</summary>
    public void HideBanner() {
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
    /// monkey keeps the Golden Crown it won; it pops in when shown.
    /// </summary>
    public void ShowArmorBadge(bool isShown) {
        if (rig.rootRect == null) {
            return;
        }
        if (rig.armorBadge == null) {
            rig.armorBadge = builder.CreateArmorBadge();
        }
        rig.isArmorShown = isShown;
        // a golden crown still flying in shows as it lands
        if (isShown && rig.isArmorGainPending) {
            rig.armorBadge.gameObject.SetActive(false);
            if (rig.armorMark != null) {
                rig.armorMark.gameObject.SetActive(false);
            }
            return;
        }
        // in a fight the armor shows beside the hearts instead
        rig.armorBadge.gameObject.SetActive(isShown && !MonkeyHearts.IsActive);
        // a -1 still flying to the armor takes it away on arrival
        if (!isShown && HasArmorLossFlight()) {
            rig.armorBadge.gameObject.SetActive(!MonkeyHearts.IsActive);
            return;
        }
        rig.armorBadgePopS = isShown ? ARMOR_BADGE_POP_S : 0f;
        rig.armorBadge.localScale = Vector3.one;
        if (rig.armorMark != null) {
            rig.armorMark.gameObject.SetActive(isShown);
        }
    }

    // the heart row follows the fight's hearts: up while they count, a lost
    // one dimmed w/ a pop, the armor mark beside them while it is worn
    private void OnMonkeyHeartsChanged() {
        if (rig == null || rig.rootRect == null) {
            return;
        }
        bool isActive = MonkeyHearts.IsActive;
        if (rig.monkeyHeartRow == null) {
            if (!isActive) {
                return;
            }
            rig.monkeyHeartRow = builder.CreateMonkeyHeartRow();
        }
        rig.monkeyHeartRow.gameObject.SetActive(isActive);
        if (rig.armorBadge != null) {
            rig.armorBadge.gameObject.SetActive(
                rig.isArmorShown && !isActive);
        }
        if (!isActive) {
            rig.monkeyHeartsShown = -1;
            return;
        }
        // a lost heart waits for its -1 to land, or a short grace if none
        // comes; anything else syncs at once
        if (rig.monkeyHeartsShown > MonkeyHearts.Hearts) {
            rig.heartGraceLeftS = HEART_GRACE_S;
            builder.SyncMonkeyHearts(MonkeyHearts.MaxHearts,
                rig.monkeyHeartsShown);
        } else {
            rig.heartGraceLeftS = 0f;
            builder.SyncMonkeyHearts(MonkeyHearts.MaxHearts,
                MonkeyHearts.Hearts);
            rig.monkeyHeartsShown = MonkeyHearts.Hearts;
        }
        ArmorMarkTarget = rig.armorMark;
        if (!HasArmorLossFlight() && !rig.isArmorGainPending) {
            rig.armorMark.gameObject.SetActive(rig.isArmorShown);
        }
    }

    /// <summary>
    /// A gold crown sprite flies from <paramref name="fromWorld"/> (the
    /// golden watermelon) to the armor mark beside the hearts, as a banana
    /// flies to the energy bar. Call it as the armor is given: it counts as
    /// worn at once, but its mark only shows, w/ a pop, on landing.
    /// </summary>
    public void FlyArmor(Vector3 fromWorld) {
        if (rig.rootRect == null || FruitFlyEffect.I == null
            || ArmorMarkTarget == null) {
            return;
        }
        rig.isArmorGainPending = true;
        rig.armorGainDeadlineS = Time.unscaledTime + ARMOR_GAIN_MAX_S;
        Sprite sprite = config.ArmorSprite != null
            ? config.ArmorSprite : BossFightHudBuilder.DrawnCrown();
        FruitFlyEffect.I.Play(fromWorld, FruitFlyTarget.ArmorMark, sprite,
            OnArmorGainLanded, UIStyle.Gold);
    }

    // the armor sprite landed: its mark pops in
    private void OnArmorGainLanded() {
        if (!rig.isArmorGainPending) {
            return;
        }
        rig.isArmorGainPending = false;
        if (!rig.isArmorShown) {
            return;
        }
        ShowArmorBadge(true);
    }

    // a flight cut by a lost run never lands, so the mark shows anyway
    private void UpdateArmorGain() {
        if (rig.isArmorGainPending
            && Time.unscaledTime > rig.armorGainDeadlineS) {
            OnArmorGainLanded();
        }
    }

    // dims one more heart with its pop, down to the hearts left
    private void ShowHeartLoss(bool isFinal) {
        int shown = isFinal
            ? MonkeyHearts.Hearts
            : Mathf.Max(MonkeyHearts.Hearts, rig.monkeyHeartsShown - 1);
        rig.heartGraceLeftS = 0f;
        if (!MonkeyHearts.IsActive || shown >= rig.monkeyHeartsShown) {
            return;
        }
        builder.SyncMonkeyHearts(MonkeyHearts.MaxHearts, shown);
        rig.monkeyHeartLostIndex = shown;
        rig.monkeyHeartPopLeftS = MONKEY_HEART_POP_S;
        rig.monkeyHeartsShown = shown;
    }

    // a -1 still punching at its target has landed, so it is no flight
    private bool IsFlying(BossFightHudLossFlight flight) {
        return flight.ageS < Mathf.Max(0.05f, config.LossFlightS);
    }

    private bool HasArmorLossFlight() {
        foreach (BossFightHudLossFlight flight in rig.lossFlights) {
            if (flight.kind == BossFightHudLoss.Armor && IsFlying(flight)) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// A -1 appears at the monkey and flies to the heart row (red) or the
    /// armor mark (gold); only as it lands does a heart dim or the armor
    /// mark go. No words print.
    /// </summary>
    /// <param name="kind">which loss it stands for; call it right after the
    /// loss, before <see cref="ShowArmorBadge"/> hides the armor</param>
    /// <param name="monkeyWorld">where the -1 starts</param>
    public void PopLoss(BossFightHudLoss kind, Vector3 monkeyWorld) {
        if (rig.rootRect == null) {
            return;
        }
        bool isHeart = kind == BossFightHudLoss.Heart;
        TextMeshProUGUI label = BossFightHudBuilder.CreateLabel(
            "Loss", rig.rootRect, "-1", config.NumberFontSize,
            isHeart ? config.HeartLossColor : config.ArmorLossColor
        );
        BossFightHudBuilder.UseFont(label, config.WarningFont);
        label.outlineWidth = 0.25f;
        label.outlineColor = Color.black;
        label.rectTransform.sizeDelta = new Vector2(240f, 120f);
        rig.lossFlights.Add(new BossFightHudLossFlight {
            label = label,
            kind = kind,
            worldPosition = monkeyWorld,
            heartIndex = MonkeyHearts.Hearts,
            ageS = 0f
        });
        motion.UpdateLossFlights();
    }

    // a -1 landed: its heart dims or its armor goes
    private void OnLossLanded(BossFightHudLossFlight flight) {
        if (flight.kind == BossFightHudLoss.Heart) {
            ShowHeartLoss(false);
            return;
        }
        if (!rig.isArmorShown) {
            if (rig.armorMark != null) {
                rig.armorMark.gameObject.SetActive(false);
            }
            if (rig.armorBadge != null) {
                rig.armorBadge.gameObject.SetActive(false);
            }
        }
    }

    // a heart loss with no -1 coming dims after its grace
    private void UpdateHeartGrace() {
        if (rig.heartGraceLeftS <= 0f) {
            return;
        }
        rig.heartGraceLeftS -= Time.unscaledDeltaTime;
        if (rig.heartGraceLeftS > 0f) {
            return;
        }
        foreach (BossFightHudLossFlight flight in rig.lossFlights) {
            if (flight.kind == BossFightHudLoss.Heart && IsFlying(flight)) {
                rig.heartGraceLeftS = HEART_GRACE_S;
                return;
            }
        }
        ShowHeartLoss(true);
    }

    // a lost heart pops big & settles dim: real s
    private const float MONKEY_HEART_POP_S = 0.4f;

    private void UpdateMonkeyHearts() {
        if (rig.monkeyHeartPopLeftS <= 0f) {
            return;
        }
        rig.monkeyHeartPopLeftS = Mathf.Max(0f,
            rig.monkeyHeartPopLeftS - Time.unscaledDeltaTime);
        int i = rig.monkeyHeartLostIndex;
        if (i < 0 || i >= rig.monkeyHeartImages.Count) {
            return;
        }
        float left = rig.monkeyHeartPopLeftS / MONKEY_HEART_POP_S;
        rig.monkeyHeartImages[i].rectTransform.localScale =
            Vector3.one * (1f + 0.7f * left * left);
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
        rig.nameIntroAgeS = -1f;
        CancelDamageFlights();
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
                config.PromptOffset);
        }
    }

    /// <summary>
    /// Shows the sign of attack <paramref name="item"/> over
    /// <paramref name="worldPosition"/>, in the one standard look, the first
    /// few times this session only; <see cref="TutorialSkipManager"/> counts
    /// and limits them. The word comes from the config's attack table.
    /// </summary>
    /// <returns>the warning's id for <see cref="UpdateWarning"/>, -1 when
    /// the sign is skipped or has no word; every warning call ignores -1
    /// </returns>
    public int AddAttackWarning(TutorialItem item, Vector3 worldPosition) {
        TutorialSkipManager skip = TutorialSkipManager.I;
        if (skip != null && skip.ShouldSkip(item)) {
            return -1;
        }
        string word = config.GetAttackWord(item);
        if (string.IsNullOrEmpty(word)) {
            return -1;
        }
        if (skip != null) {
            skip.MarkSeen(item);
        }
        return AddWarning(worldPosition, word);
    }

    // a red sign with "!" and the word under it, its "!" shaking harder
    // & faster as its progress 0~1 nears the hit, the word still; every attack asks AddAttackWarning instead
    private int AddWarning(Vector3 worldPosition, string word) {
        if (rig.rootRect == null) {
            return -1;
        }
        RectTransform sign =
            BossFightHudBuilder.CreateRect("Warning", rig.rootRect);
        sign.sizeDelta = new Vector2(70f, 70f);

        // a red diamond with a white border & a big "!", shaking as one
        RectTransform markGroup =
            BossFightHudBuilder.CreateRect("MarkGroup", sign);
        BossFightHudBuilder.Stretch(markGroup);
        RectTransform border =
            BossFightHudBuilder.CreateRect("Border", markGroup);
        BossFightHudBuilder.Stretch(border);
        border.localRotation = Quaternion.Euler(0f, 0f, 45f);
        border.sizeDelta = Vector2.zero;
        BossFightHudBuilder.AddImage(border, Color.white);
        RectTransform face = BossFightHudBuilder.CreateRect("Face", border);
        BossFightHudBuilder.Stretch(face);
        face.offsetMin = new Vector2(5f, 5f);
        face.offsetMax = new Vector2(-5f, -5f);
        BossFightHudBuilder.AddImage(face, config.WarningColor);
        TextMeshProUGUI mark =
            BossFightHudBuilder.CreateLabel("Mark", markGroup, "!", 56f,
                Color.white);
        BossFightHudBuilder.Stretch(mark.rectTransform);
        mark.fontStyle = FontStyles.Bold;

        TextMeshProUGUI label = BossFightHudBuilder.CreateLabel(
            "Word", sign, word, 30f, config.WarningColor
        );
        label.fontStyle = FontStyles.Bold;
        // the standard sign font, never a fake bold on a heavy face
        if (config.WarningFont != null) {
            foreach (TextMeshProUGUI text in new[] { mark, label }) {
                text.font = config.WarningFont;
                text.fontSharedMaterial = config.WarningFont.material;
                text.fontStyle = FontStyles.Normal;
            }
        }
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
            mark = markGroup,
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
        }
        if (rig.speechRect != null) {
            rig.speechLeftS -= Time.deltaTime;
            bool isSpeaking = rig.speechLeftS > 0f;
            rig.speechRect.gameObject.SetActive(isSpeaking);
            if (isSpeaking) {
                motion.PlaceSpeech();
            }
        }
        motion.UpdatePops();
        motion.UpdateLossFlights();
        motion.UpdateDamageFlights();
        UpdateArmorGain();
        UpdateHeartGrace();
        motion.UpdateWarnings();
        motion.UpdateCallOut();
        motion.UpdateNameIntro();
        UpdateHeart();
        UpdateMonkeyHearts();
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
