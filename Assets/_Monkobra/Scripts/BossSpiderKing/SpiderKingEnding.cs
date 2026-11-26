using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>The finisher, the revive offer, the defeat, and the reward
/// flow.</summary>
public sealed class SpiderKingEnding {
    // private members  ########################################################
    private readonly SpiderKingRig rig;
    private readonly SpiderKingConfig config;
    private readonly System.Action onRevived;
    private readonly System.Action onFightEnded;

    public SpiderKingEnding(SpiderKingRig rig, System.Action onRevived,
        System.Action onFightEnded) {
        this.rig = rig;
        config = rig.config;
        this.onRevived = onRevived;
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
        Time.timeScale = config.FinisherTimeScale;
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
        if (rig.finisherTimeS < config.FinisherRealS) {
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
        rig.reviveLeftS = config.ReviveOfferS;
        Time.timeScale = 0f;
        rig.hud.HideBanner();
        rig.hud.ShowReviveOffer();
        rig.hud.SetReviveCountdown(Mathf.CeilToInt(rig.reviveLeftS));
    }

    public void UpdateRevive(RunController game) {
        Time.timeScale = 0f;
        if (game.IsPaused) {
            return;
        }
        float stepS = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if (rig.phase == SpiderKingPhase.WatchingAd) {
            rig.adElapsedS += stepS;
            bool canSkip = rig.adElapsedS >= config.AdSkipAfterS;
            rig.hud.SetAdSkippable(canSkip);
            if (canSkip
                && SpiderKingFeedback.WasPressed(config.ConfirmAction)) {
                SkipAd();
            }
            return;
        }
        rig.reviveLeftS -= stepS;
        rig.hud.SetReviveCountdown(Mathf.CeilToInt(Mathf.Max(0f,
            rig.reviveLeftS)));
        if (SpiderKingFeedback.WasPressed(config.ConfirmAction)) {
            WatchAd();
        } else if (SpiderKingFeedback.WasPressed(config.GiveUpAction)
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
        if (RunController.I != null) {
            RunController.I.LoseGame();
        }
    }

    public void SkipAd() {
        if (rig.phase != SpiderKingPhase.WatchingAd
            || rig.adElapsedS < config.AdSkipAfterS) {
            return;
        }
        RestartFight();
    }

    public void OpenCoursePage() {
        if (rig.phase == SpiderKingPhase.WatchingAd
            && !string.IsNullOrEmpty(config.CourseUrl)) {
            Application.OpenURL(config.CourseUrl);
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
        rig.hud.SetHealth(rig.health, rig.maxHealth);
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
        rig.ResetLoop();
        rig.maxHealth = rig.BaseMaxHealth;
        rig.health = rig.maxHealth;
        // a plain difficulty card, time stopped till a pick; the spider
        // waits meanwhile
        rig.teach = SpiderKingTeachStep.Choose;
        rig.hud.HideBanner();
        rig.hud.ShowModeChoice();
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
        onRevived?.Invoke();
    }

    public void ReturnToStart() {
        if (rig.monkeyController == null) {
            return;
        }
        if (rig.struggle != null) {
            rig.struggle.ForceRelease();
        }
        SpiderKingFeedback.SetUntouchable(true);
        rig.monkeyController.SetHeightCeiling(rig.entryY + config.ArenaHeightU);
        rig.monkeyController.ClearStun();
        rig.monkeyController.IsHeld = true;
        rig.returnTarget = rig.entryPosition;
        rig.monkeyController.PullTo(rig.returnTarget,
            config.ReviveReturnSpeedU);
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
        // only a win ends the lessons, so a lost fight teaches again
        if (TutorialSkipManager.I != null) {
            TutorialSkipManager.I.MarkSeen(TutorialItem.SpiderKingLesson);
        }
        rig.phase = SpiderKingPhase.Defeated;
        rig.onDefeated?.Invoke();
        rig.defeatTimeS = 0f;
        rig.hud.SetHealth(0, rig.maxHealth);
        rig.hud.ClearWarnings();
        rig.Shots.ClearShots();
        rig.Melons.ClearThrows();
        rig.Arena.ClearStickyWebs();
        rig.Parry.Burst(rig.body.Position, config.SilkColor, 20);
        rig.hud.Say("My beautiful web...");
        if (Debug.isDebugBuild) {
            Debug.Log("SpiderKingFightController:\tSpider King defeated",
                rig.owner);
        }
    }

    public void UpdateDefeat() {
        rig.defeatTimeS += Time.deltaTime;
        rig.body.Fall(rig.defeatTimeS);
        if (rig.defeatTimeS < config.DefeatFallS) {
            return;
        }
        rig.body.Clear();
        rig.phase = SpiderKingPhase.Rewarding;
        // the gloves: every web from now on breaks twice as fast
        if (rig.struggle != null) {
            rig.struggle.EscapeGainScale = config.GlovesEscapeGainScale;
        }
        rig.hud.HideBossBar();
        // the gloves are only their effect, nothing worn: straight to the
        // reward panel
        rig.hud.ShowRewardPanel(config.WinTitle, config.WinLine,
            config.WinDetailLine);
    }

    // time stops on the reward panel till the player goes on
    public void UpdateRewardPause(RunController game) {
        Time.timeScale = 0f;
        if (game.IsPaused) {
            return;
        }
        // only once the win has played in, so a Space mashed in the fight
        // never skips it
        if (rig.hud.IsRewardReady
            && SpiderKingFeedback.WasPressed(config.ConfirmAction)) {
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
            rig.greatCobra.KnockBack(rig.Layout.DropU);
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
        if (rig.IsRush) {
            ArmNextRush();
        }
    }

    // Hardcore Boss Rush  ------------------------------------------------------
    // the spider waits again two bosses on, the Cobra King's in b/t, harder;
    // the gloves' effect stays on
    public void ArmNextRush() {
        rig.rushEncounter += 2;
        rig.phase = SpiderKingPhase.Waiting;
        rig.hud.ClearCallOut();
        rig.ResetLoop();
        rig.lastStage = 0;
        rig.Arena.ReserveRushArena();
        if (Debug.isDebugBuild) {
            Debug.Log(
                "SpiderKingFightController:\tnext rush fight at "
                    + rig.TriggerClimbU + " u",
                rig.owner
            );
        }
    }

}
