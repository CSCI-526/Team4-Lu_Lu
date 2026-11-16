using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>The finisher, the revive offer, the defeat, and the glove reward
/// flow.</summary>
public sealed class SpiderKingEnding {
    // Private Members  ########################################################
    private readonly SpiderKingRig rig;
    private readonly SpiderKingTuning tuning;
    private readonly System.Action onFightEnded;

    public SpiderKingEnding(SpiderKingRig rig, System.Action onFightEnded) {
        this.rig = rig;
        tuning = rig.tuning;
        this.onFightEnded = onFightEnded;
    }

    // Public Methods  #########################################################
    public void StartFinisher(bool isWin) {
        rig.phase = SpiderKingPhase.Finishing;
        rig.isFinisherWin = isWin;
        rig.finisherTimeS = 0f;
        rig.Attacks.CancelAttack();
        rig.hud.HideBanner();
        rig.hud.ClearCallOut();
        rig.hud.ClearWarnings();
        rig.hud.SetPrompt(false, Vector3.zero);
        rig.isTimeBent = false;
        rig.isTeachFrozen = false;
        rig.frozenShot = null;
        rig.frozenCrescent = null;
        rig.parrySlowEndTime = 0f;
        rig.isDashing = false;
        rig.Attacks.ClearSlash();
        Time.timeScale = tuning.FinisherTimeScale;
        if (isWin) {
            rig.hud.Flash(Color.white);
            return;
        }
        rig.hud.Flash(new Color(0.9f, 0.1f, 0.1f));
        rig.hud.ShowBanner("K.O.", new Color(0.95f, 0.15f, 0.15f));
        SpiderKingFeedback.SetUntouchable(true);
        if (rig.struggle != null) {
            rig.struggle.ForceRelease();
        }
        if (rig.monkeyController != null) {
            rig.monkeyController.ClearHeightCeiling();
            Vector3 outward = rig.monkey.position - rig.trunkCenter;
            outward.y = 0f;
            rig.monkeyController.IsHeld = true;
            rig.monkeyController.PullTo(
                rig.monkey.position + outward.normalized * 6f + Vector3.down
                    * 3f
            );
        }
    }

    // runs on unscaled time, since the game itself crawls meanwhile
    public void UpdateFinisher() {
        rig.finisherTimeS += Time.unscaledDeltaTime;
        if (!rig.isFinisherWin) {
            rig.monkey.Rotate(Vector3.up, 900f * Time.unscaledDeltaTime);
        }
        if (rig.finisherTimeS < tuning.FinisherRealS) {
            return;
        }
        Time.timeScale = 1f;
        if (rig.isFinisherWin) {
            Defeat();
            return;
        }
        OfferRevive();
    }

    public void OfferRevive() {
        rig.phase = SpiderKingPhase.ReviveOffer;
        rig.reviveLeftS = tuning.ReviveOfferS;
        Time.timeScale = 0f;
        rig.hud.HideBanner();
        rig.hud.ShowReviveOffer();
        rig.hud.SetReviveCountdown(Mathf.CeilToInt(rig.reviveLeftS));
    }

    public void UpdateRevive(GameController game) {
        Time.timeScale = 0f;
        if (game.IsPaused) {
            return;
        }
        float stepS = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if (rig.phase == SpiderKingPhase.WatchingAd) {
            rig.adElapsedS += stepS;
            bool canSkip = rig.adElapsedS >= tuning.AdSkipAfterS;
            rig.hud.SetAdSkippable(canSkip);
            if (canSkip
                && SpiderKingFeedback.WasPressed(tuning.ConfirmAction)) {
                SkipAd();
            }
            return;
        }
        rig.reviveLeftS -= stepS;
        rig.hud.SetReviveCountdown(Mathf.CeilToInt(Mathf.Max(0f,
            rig.reviveLeftS)));
        if (SpiderKingFeedback.WasPressed(tuning.ConfirmAction)) {
            WatchAd();
        } else if (SpiderKingFeedback.WasPressed(tuning.GiveUpAction)
            || rig.reviveLeftS <= 0f) {
            GiveUp();
        }
    }

    public void WatchAd() {
        if (rig.phase != SpiderKingPhase.ReviveOffer) {
            return;
        }
        rig.phase = SpiderKingPhase.WatchingAd;
        rig.adElapsedS = 0f;
        rig.hud.SetAdSkippable(false);
        rig.hud.ShowAd();
    }

    public void GiveUp() {
        if (rig.phase != SpiderKingPhase.ReviveOffer) {
            return;
        }
        rig.hud.HideRevive();
        Time.timeScale = 1f;
        rig.phase = SpiderKingPhase.Over;
        SpiderKingFeedback.SetUntouchable(false);
        if (GameController.I != null) {
            GameController.I.LoseGame();
        }
    }

    public void SkipAd() {
        if (rig.phase != SpiderKingPhase.WatchingAd
            || rig.adElapsedS < tuning.AdSkipAfterS) {
            return;
        }
        RestartFight();
    }

    public void OpenCoursePage() {
        if (rig.phase == SpiderKingPhase.WatchingAd
            && !string.IsNullOrEmpty(tuning.CourseUrl)) {
            Application.OpenURL(tuning.CourseUrl);
        }
    }

    // after the ad the fight starts over at full health; a spent armor
    // stays spent
    public void RestartFight() {
        rig.hud.HideRevive();
        Time.timeScale = 1f;
        rig.phase = SpiderKingPhase.Fighting;
        // a new try starts in the armor too, swirled on
        SpiderKingDamage.GiveArmor();
        rig.Melons.ClearThrows();
        rig.health = rig.maxHealth;
        rig.hud.SetHealth(1f);
        foreach (SpiderKingMelon melon in rig.melons) {
            if (melon.root != null) {
                melon.root.SetActive(true);
                melon.backTime = -1f;
            }
        }
        rig.Arena.ClearStickyWebs();
        // the last try's golden melon goes; the next comes as at the start
        rig.Arena.HideGoldenMelon();
        rig.nextGoldenTime = Time.time + GOLDEN_FIRST_S;
        rig.spiderAngle = rig.startAngle;
        rig.spiderY = rig.startY;
        rig.Movement.ResetSpiderState();
        rig.teachTimeS = 0f;
        // as in the Cobra King's arena: two melons, a plain EASY & a golden
        // NORMAL; the first grabbed picks, & the fight waits till then
        rig.isEasy = false;
        rig.maxHealth = tuning.WorldBalanceConfig.SpiderMaxHealth;
        rig.health = rig.maxHealth;
        rig.teach = SpiderKingTeachStep.Choose;
        rig.Arena.SpawnChoices();
        rig.Teaching.Teach("Easy or Normal?", "Grab a melon", UIStyle.Gold, 0f);
        StaminaBarController stamina = StaminaBarController.I;
        if (stamina != null) {
            stamina.AddStamina(stamina.MaxStamina);
        }
        if (rig.greatCobra != null) {
            rig.greatCobra.KnockBack(0f);
        }
        rig.monkey.rotation = rig.entryRotation;
        ReturnToStart();
        rig.hud.Say("Back for more?");
    }

    public void ReturnToStart() {
        if (rig.monkeyController == null) {
            return;
        }
        if (rig.struggle != null) {
            rig.struggle.ForceRelease();
        }
        SpiderKingFeedback.SetUntouchable(true);
        rig.monkeyController.SetHeightCeiling(rig.entryY + tuning.ArenaHeightU);
        rig.monkeyController.ClearStun();
        rig.monkeyController.IsHeld = true;
        rig.returnTarget = rig.entryPosition;
        rig.monkeyController.PullTo(rig.returnTarget,
            tuning.ReviveReturnSpeedU);
        rig.isReturningToStart = true;
    }

    public void UpdateReturnToStart() {
        if (!rig.isReturningToStart || rig.monkeyController == null) {
            return;
        }
        if (
            rig.monkeyController.HasReachedPullTarget
            || (rig.monkey.position - rig.returnTarget).sqrMagnitude < 0.01f
        ) {
            rig.isReturningToStart = false;
            rig.monkeyController.SyncOrbitAngle();
            rig.monkeyController.IsHeld = false;
            SpiderKingFeedback.SetUntouchable(false);
        }
    }

    // Win  --------------------------------------------------------------------
    public void Defeat() {
        rig.phase = SpiderKingPhase.Defeated;
        rig.defeatTimeS = 0f;
        rig.hud.SetHealth(0f);
        rig.hud.ClearWarnings();
        rig.Shots.ClearShots();
        rig.Melons.ClearThrows();
        rig.Arena.ClearStickyWebs();
        rig.Parry.Burst(rig.body.Position, tuning.SilkColor, 20);
        rig.hud.Say("My beautiful web...");
        if (Debug.isDebugBuild) {
            Debug.Log("SpiderKingFightController:\tSpider King defeated",
                rig.owner);
        }
    }

    public void UpdateDefeat() {
        rig.defeatTimeS += Time.deltaTime;
        rig.body.Fall(rig.defeatTimeS);
        if (rig.defeatTimeS < tuning.DefeatFallS) {
            return;
        }
        rig.body.Clear();
        rig.phase = SpiderKingPhase.Rewarding;
        // the gloves: every web from now on breaks twice as fast
        if (rig.struggle != null) {
            rig.struggle.EscapeGainScale = tuning.GlovesEscapeGainScale;
        }
        rig.hud.HideBossBar();
        // as the Cobra King's armor: the silk gloves swirl in & snap onto
        // the monkey's hands, then the reward panel
        StartGloveReward();
    }

    // Silk Gloves  ------------------------------------------------------------
    // two plain silver silk gloves, as simple as the armor, spiral in from
    // around the monkey, shrinking as they near, snap onto its hands w/ a
    // silk burst, a sleeve grows up each arm, they pulse bright, & stay on
    // for good, softly shining as the armor does
    public void StartGloveReward() {
        ArmController[] hands =
            UnityEngine.Object.FindObjectsByType<ArmController>(
            FindObjectsSortMode.None);
        rig.gloveHands = hands;
        rig.gloves = new Transform[hands.Length];
        rig.sleeves = new Transform[hands.Length];
        rig.gloveSizes = new float[hands.Length];
        for (int i = 0; i < hands.Length; i++) {
            Collider grip = hands[i].GetComponentInChildren<Collider>(true);
            float size = grip != null
                ? Mathf.Max(grip.bounds.extents.x, grip.bounds.extents.y,
                    grip.bounds.extents.z) * 2.3f
                : 0.45f;
            rig.gloveSizes[i] = Mathf.Clamp(size * 1.5f, 0.4f, 1.3f);
            rig.gloves[i] = BuildGlove();
        }
        rig.gloveRewardTimeS = 0f;
        rig.hasGloveLanded = false;
    }

    // a plain sleeve over the whole arm, from the glove to the shoulder, a
    // bit wider than the arm; it lives on the arm's stretched cube, so it
    // stretches w/ it
    public Transform BuildSleeve(Transform arm) {
        Transform sleeve = new GameObject("SilkSleeve").transform;
        sleeve.SetParent(arm, false);
        AddSleevePart(sleeve, PrimitiveType.Cube, Vector3.zero,
            new Vector3(1.3f, 1.3f, 1.02f), gloveColor);
        return sleeve;
    }

    public void AddSleevePart(Transform sleeve, PrimitiveType type,
        Vector3 at, Vector3 scale, Color color) {
        Transform part =
            rig.Feedback.CreatePrimitive(type, "SleevePart", Vector3.one,
            color).transform;
        part.SetParent(sleeve, false);
        part.localPosition = at;
        part.localRotation = Quaternion.identity;
        part.localScale = scale;
    }

    public Transform BuildGlove() {
        Transform glove = new GameObject("SilkGlove").transform;
        glove.SetParent(rig.host, false);
        AddSleevePart(glove, PrimitiveType.Sphere, Vector3.zero,
            new Vector3(1f, 0.85f, 1f), gloveColor);
        return glove;
    }

    // the swirl in, then the panel; time runs till the panel shows
    public void UpdateGloveReward() {
        rig.gloveRewardTimeS += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(rig.gloveRewardTimeS / GLOVE_REWARD_S);
        Vector3 outward = rig.monkey.position - rig.trunkCenter;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 0.001f ? outward.normalized
            : Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, outward);
        for (int i = 0; i < rig.gloves.Length; i++) {
            Transform glove = rig.gloves[i];
            Transform hand = rig.gloveHands[i] != null
                ? rig.gloveHands[i].transform
                : null;
            if (glove == null || hand == null) {
                continue;
            }
            float size = rig.gloveSizes[i];
            if (t < GLOVE_LAND_SHARE) {
                float fly = t / GLOVE_LAND_SHARE;
                float ease = 1f - Mathf.Pow(1f - fly, 3f);
                float radius = Mathf.Lerp(GLOVE_SWIRL_U, 0f, ease);
                float spin = (i * 180f + fly * 540f) * Mathf.Deg2Rad;
                glove.position = hand.position
                    + (outward * Mathf.Cos(spin) + side * Mathf.Sin(spin))
                        * radius
                    + Vector3.up * radius * 0.7f;
                glove.rotation = Quaternion.LookRotation(outward, Vector3.up)
                    * Quaternion.Euler(0f, fly * 720f, 0f);
                glove.localScale = Vector3.one * size * Mathf.Lerp(1.8f, 1f,
                    ease);
                continue;
            }
            if (!rig.hasGloveLanded) {
                // on: held by the hand from now on, for good, & its sleeve
                // grows up the arm
                glove.SetParent(hand, false);
                glove.localPosition = Vector3.zero;
                glove.localRotation = Quaternion.identity;
                Transform arm = rig.gloveHands[i].ArmVisual;
                if (arm != null) {
                    rig.sleeves[i] = BuildSleeve(arm);
                }
            }
            // the sleeve grows from the hand end up to the shoulder
            if (rig.sleeves[i] != null) {
                float grow = Mathf.Clamp01((t - GLOVE_LAND_SHARE) / 0.15f);
                grow = 1f - (1f - grow) * (1f - grow);
                rig.sleeves[i].localScale = new Vector3(1f, 1f,
                    Mathf.Max(0.01f, grow));
                rig.sleeves[i].localPosition = new Vector3(0f, 0f,
                    -0.5f + 0.5f * grow);
            }
            float pulse = Mathf.Sin((t - GLOVE_LAND_SHARE)
                / (1f - GLOVE_LAND_SHARE) * Mathf.PI);
            SetWorldScale(glove, size * (1f + 0.35f * pulse));
        }
        if (!rig.hasGloveLanded && t >= GLOVE_LAND_SHARE) {
            rig.hasGloveLanded = true;
            foreach (Transform glove in rig.gloves) {
                if (glove != null) {
                    rig.Parry.Burst(glove.position, tuning.SilkColor, 12);
                    rig.Parry.Burst(glove.position, tuning.GoldenColor, 6);
                }
            }
        }
        rig.Parry.UpdateSparks();
        UpdateGloveShine();
        if (t >= 1f) {
            rig.gloveRewardTimeS = -1f;
            rig.hud.ShowRewardPanel(tuning.WinTitle, tuning.WinLine,
                tuning.WinDetailLine);
        }
    }

    // the gloves & sleeves shine softly, as the armor does
    public void UpdateGloveShine() {
        if (rig.gloves == null) {
            return;
        }
        float shine = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
        Color color = Color.Lerp(gloveColor, Color.white, shine * 0.35f);
        for (int i = 0; i < rig.gloves.Length; i++) {
            TintAll(rig.gloves[i], color);
            if (rig.sleeves != null) {
                TintAll(rig.sleeves[i], color);
            }
        }
    }

    public void TintAll(Transform root, Color color) {
        if (root == null) {
            return;
        }
        foreach (Renderer part in root.GetComponentsInChildren<Renderer>()) {
            rig.Feedback.SetColor(part, color);
        }
    }

    // a world size kept whatever the hand's own scale
    public static void SetWorldScale(Transform target, float size) {
        Transform parent = target.parent;
        Vector3 lossy = parent != null ? parent.lossyScale : Vector3.one;
        target.localScale = new Vector3(
            size / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
            size / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
            size / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));
    }

    // time stops on the reward panel till the player goes on
    public void UpdateRewardPause(GameController game) {
        // the gloves swirl on first, time running
        if (rig.gloveRewardTimeS >= 0f) {
            UpdateGloveReward();
            return;
        }
        Time.timeScale = 0f;
        if (game.IsPaused) {
            return;
        }
        // only once the win has played in, so a Space mashed in the fight
        // never skips it
        if (rig.hud.IsRewardReady
            && SpiderKingFeedback.WasPressed(tuning.ConfirmAction)) {
            ContinueAfterReward();
        }
    }

    public void ContinueAfterReward() {
        if (rig.phase != SpiderKingPhase.Rewarding) {
            return;
        }
        rig.hud.HideRewardPanel();
        Time.timeScale = 1f;
        rig.phase = SpiderKingPhase.Done;
        rig.Arena.ClearMelons();
        rig.Arena.ClearChoices();
        rig.Arena.ClearSkyWeb();
        rig.Arena.EndBareArena();
        if (rig.greatCobra != null) {
            rig.greatCobra.StopGuard();
            rig.greatCobra.KnockBack(tuning.WinCobraDropU);
        }
        if (rig.monkeyController != null) {
            rig.monkeyController.ClearHeightCeiling();
        }
        if (rig.cameraFraming != null) {
            rig.cameraFraming.SetBossFraming(0f, 1f);
        }
        rig.hud.ShowBannerFor("The cobra is coming!", "Climb!",
            new Color(0.95f, 0.25f, 0.2f), 2.5f);
        onFightEnded?.Invoke();
        if (Debug.isDebugBuild) {
            Debug.Log(
                "SpiderKingFightController:\twon, silk gloves given, climb on",
                rig.owner
            );
        }
    }

}
