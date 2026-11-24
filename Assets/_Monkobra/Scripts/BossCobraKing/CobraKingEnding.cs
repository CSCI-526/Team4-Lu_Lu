using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static CobraKingRig;

/// <summary>The finisher, the loss, the revive offer, the victory, and the
/// reward flow.</summary>
public sealed class CobraKingEnding {
    // private members  ########################################################
    private readonly CobraKingRig rig;
    private readonly CobraKingConfig config;
    private readonly System.Action onRevived;
    private readonly System.Action onFightEnded;

    public CobraKingEnding(CobraKingRig rig, System.Action onRevived,
        System.Action onFightEnded) {
        this.rig = rig;
        config = rig.config;
        this.onRevived = onRevived;
        this.onFightEnded = onFightEnded;
    }

    // Public Methods  #########################################################
    // Finisher  ---------------------------------------------------------------
    // the last blow either way plays in slow motion: a win flashes the
    // screen white before the king shatters; a loss flashes red & spins the
    // monkey off the tree under a K.O. banner
    public void StartFinisher(bool isWin) {
        rig.phase = CobraKingPhase.Finishing;
        rig.isFinisherWin = isWin;
        rig.finisherTimeS = 0f;
        rig.Feedback.ClearMarkers();
        rig.hud.SetPrompt(false, Vector3.zero);
        Time.timeScale = config.FinisherTimeScale;

        if (isWin) {
            rig.hud.Flash(Color.white);
            return;
        }
        rig.hud.Flash(new Color(0.9f, 0.1f, 0.1f));
        rig.hud.ShowBanner("K.O.", new Color(0.95f, 0.15f, 0.15f));
        // from the K.O. till a revive lands, no web, cobra or branch may
        // touch the monkey: the same untouchable state as a pineapple leap
        CobraKingAttacks.SetUntouchable(true);
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
        if (config.CanRevive) {
            OfferRevive();
            return;
        }
        EndAsLoss();
    }

    public void EndAsLoss() {
        rig.phase = CobraKingPhase.Over;
        CobraKingAttacks.SetUntouchable(false);
        if (GameController.I != null) {
            GameController.I.LoseGame();
        }
    }

    // Revive  -----------------------------------------------------------------
    // once a game a loss stops on an offer: watch a (mock) ad to fight on.
    // Time stands still meanwhile, so all of it runs on unscaled time
    public void OfferRevive() {
        rig.phase = CobraKingPhase.ReviveOffer;
        rig.reviveLeftS = config.ReviveOfferS;
        Time.timeScale = 0f;
        rig.hud.HideBanner();
        rig.hud.ShowReviveOffer();
        rig.hud.SetReviveCountdown(Mathf.CeilToInt(rig.reviveLeftS));
    }

    public void UpdateRevive(GameController game) {
        // a pause & resume would run the game under the offer otherwise
        Time.timeScale = 0f;
        // the clock holds while paused; the step is capped so a long time
        // away, eg on the course page, skips nothing. Input is read even
        // without focus, as coming back from the page may leave the game
        // unfocused till a click
        if (game.IsPaused) {
            return;
        }
        float stepS = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        // the ad runs till skipped: no countdown, only a Skip button after
        // a short while
        if (rig.phase == CobraKingPhase.WatchingAd) {
            rig.adElapsedS += stepS;
            bool canSkip = rig.adElapsedS >= config.AdSkipAfterS;
            rig.hud.SetAdSkippable(canSkip);
            if (canSkip && WasPressed(config.ConfirmAction)) {
                SkipAd();
            }
            return;
        }

        rig.reviveLeftS -= stepS;

        rig.hud.SetReviveCountdown(Mathf.CeilToInt(Mathf.Max(0f,
            rig.reviveLeftS)));
        if (WasPressed(config.ConfirmAction)) {
            WatchAd();
        } else if (WasPressed(config.GiveUpAction) || rig.reviveLeftS <= 0f) {
            GiveUp();
        }
    }

    public void WatchAd() {
        if (rig.phase != CobraKingPhase.ReviveOffer) {
            return;
        }
        rig.phase = CobraKingPhase.WatchingAd;
        rig.adElapsedS = 0f;
        rig.hud.SetAdSkippable(false);
        rig.hud.ShowAd();
    }

    public void GiveUp() {
        if (rig.phase != CobraKingPhase.ReviveOffer) {
            return;
        }
        rig.hud.HideRevive();
        Time.timeScale = 1f;
        EndAsLoss();
    }

    // after the ad the fight starts over: the monkey flies back to where it
    // began, in the armor, full stamina, a full-health king & the mode
    // choice again
    public void RestartFight() {
        rig.hud.HideRevive();
        Time.timeScale = 1f;
        rig.phase = CobraKingPhase.Fighting;
        onRevived?.Invoke();

        rig.body.CancelStrike();
        rig.body.SetWeakSpotLit(true);
        rig.Feedback.ClearAttackObjects();
        rig.Feedback.ClearMarkers();
        rig.attackQueue.Clear();
        rig.Arena.ClearFruits();
        foreach (SpiderWebTrap web in rig.webs) {
            if (web != null) {
                UnityEngine.Object.Destroy(web.gameObject);
            }
        }
        rig.webs.Clear();

        rig.loop = CobraKingLoop.Intro;
        rig.isMinionStreaming = false;
        rig.isWaveRunning = false;
        rig.isFinaleSweepNext = false;
        rig.hasHit = false;
        rig.hasThrown = false;
        // a new try, so the minion call is heard again
        rig.hasCalledMinions = false;
        rig.isEasy = false;
        rig.maxHealth = rig.BaseMaxHealth;
        rig.health = rig.maxHealth;
        rig.hud.SetHealth(rig.health, rig.maxHealth);
        rig.hasShield = true;
        StaminaBarController stamina = StaminaBarController.I;
        if (stamina != null) {
            stamina.AddStamina(stamina.MaxStamina);
        }

        // the guard below ends any peek, so it is not up on the way back
        if (rig.greatCobra != null) {
            rig.greatCobra.KnockBack(0f);
        }
        if (rig.struggle != null) {
            rig.struggle.ForceRelease();
        }
        CobraKingAttacks.SetUntouchable(true);
        rig.monkey.rotation = rig.entryRotation;
        if (rig.monkeyController != null) {
            rig.monkeyController.ClearStun();
            rig.monkeyController.IsHeld = true;
            rig.monkeyController.PullTo(rig.entryPosition,
                config.ReviveReturnSpeedU);
            rig.isReturningToStart = true;
        }
        rig.Arena.SpawnWebs(config.WebCount);
        rig.Arena.SpawnLowWebs();
        // a plain difficulty card, time stopped till a pick
        rig.isChoosing = true;
        rig.hud.ShowModeChoice();
        if (Debug.isDebugBuild) {
            Debug.Log(
                "CobraKingFightController:\trevived after the ad, fight "
                    + "restarts",
                rig.owner
            );
        }
    }

    public void SkipAd() {
        if (rig.phase != CobraKingPhase.WatchingAd
            || rig.adElapsedS < config.AdSkipAfterS) {
            return;
        }
        RestartFight();
    }

    public void OpenCoursePage() {
        if (rig.phase == CobraKingPhase.WatchingAd
            && !string.IsNullOrEmpty(config.CourseUrl)) {
            Application.OpenURL(config.CourseUrl);
        }
    }

    // flies the monkey back to where the fight began, untouchable & out of
    // any web on the way, as after a revive
    public void ReturnToStart() {
        if (rig.monkeyController == null) {
            return;
        }
        if (rig.struggle != null) {
            rig.struggle.ForceRelease();
        }
        CobraKingAttacks.SetUntouchable(true);
        rig.monkeyController.ClearStun();
        rig.monkeyController.IsHeld = true;
        rig.monkeyController.PullTo(rig.entryPosition,
            config.ReviveReturnSpeedU);
        rig.isReturningToStart = true;
    }

    // lets go of the monkey once it is back at the fight's start
    public void UpdateReturnToStart() {
        if (!rig.isReturningToStart || rig.monkeyController == null) {
            return;
        }
        if (
            rig.monkeyController.HasReachedPullTarget
            || (rig.monkey.position - rig.entryPosition).sqrMagnitude < 0.01f
        ) {
            rig.isReturningToStart = false;
            // back on the ring at the fight's start, not where it fell
            rig.monkeyController.SyncOrbitAngle();
            rig.monkeyController.IsHeld = false;
            CobraKingAttacks.SetUntouchable(false);
        }
    }

    // Victory  ----------------------------------------------------------------
    public void Defeat() {
        rig.phase = CobraKingPhase.Defeated;
        ProgressStore.MarkBossBeaten();
        rig.Feedback.ClearMarkers();
        rig.Feedback.ClearAttackObjects();
        rig.Arena.RemoveArenaFruit();
        if (rig.monkeyController != null) {
            rig.monkeyController.ClearHeightCeiling();
        }
        rig.hud.SetPrompt(false, Vector3.zero);
        rig.hud.SetHealth(0, rig.maxHealth);

        // a later boss of the endless run's rush: the last crown makes way
        if (rig.crown != null) {
            UnityEngine.Object.Destroy(rig.crown.gameObject);
            rig.crown = null;
        }
        rig.crown = rig.body.Shatter();
        rig.onShattered?.Invoke();
        if (rig.crown != null) {
            rig.crownStart = rig.crown.position;
            rig.crownStartScale = rig.crown.localScale;
        }
        rig.victoryTimeS = 0f;
        if (Debug.isDebugBuild) {
            Debug.Log("CobraKingFightController:\tCobra King defeated",
                rig.owner);
        }
    }

    // the crown flies in an arc onto the monkey's head, then the run is won
    public void UpdateVictory() {
        rig.victoryTimeS += Time.deltaTime;
        if (rig.crown != null) {
            float t = Mathf.Clamp01(rig.victoryTimeS / config.CrownFlyS);
            Vector3 end =
                rig.monkey.position + Vector3.up * config.CrownAboveMonkeyU;
            Vector3 position = Vector3.Lerp(rig.crownStart, end, t)
                + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2.5f;
            rig.crown.position = position;
            rig.crown.localScale = rig.crownStartScale
                * Mathf.Lerp(1.4f, config.CrownScaleOnMonkey, t);
            rig.crown.rotation = Quaternion.Euler(0f, t * 720f, 0f);
            if (t >= 1f) {
                rig.crown.SetParent(rig.monkey, true);
            }
        }
        if (!rig.hasWon
            && rig.victoryTimeS >= config.CrownFlyS + config.VictoryDelayS) {
            rig.hasWon = true;
            FinishWithReward();
        }
    }

    // the run goes on: the gold armor is the reward, the hazards & the great
    // cobra's chase come back, & the climb is free again
    public void FinishWithReward() {
        rig.phase = CobraKingPhase.Rewarding;
        rig.hasShield = true;
        rig.isRewardPanelShown = false;
        // the armor swirls in onto the monkey, then a badge keeps it in view
        // & the game pauses on the reward till the player goes on
        rig.armorRewardTimeS = 0f;
        rig.hasArmorLanded = false;
        rig.hud.HideBossBar();
        rig.hud.SetPrompt(false, Vector3.zero);
    }

    // once the swirl is done, time stops on the reward panel; the confirm
    // action or its button goes on
    public void UpdateRewardPause(GameController game) {
        if (!rig.isRewardPanelShown) {
            if (rig.armorRewardTimeS >= 0f) {
                return;
            }
            rig.isRewardPanelShown = true;
            rig.hud.ShowRewardPanel(config.WinTitle, config.WinLine,
                config.WinDetailLine);
        }
        // a pause & resume would run the game under the panel otherwise
        Time.timeScale = 0f;
        if (game.IsPaused) {
            return;
        }
        // only once the win has played in, so a Space mashed in the fight
        // never skips it
        if (rig.hud.IsRewardReady && WasPressed(config.ConfirmAction)) {
            ContinueAfterReward();
        }
    }

    // if the action is wired and went down this frame; the action is shared
    // (Player/Jump is also switched off by SpiderWebStruggle), so it is
    // switched back on here at the moment of use
    public static bool WasPressed(InputActionReference reference) {
        if (reference == null) {
            return false;
        }

        InputAction action = reference.action;
        if (!action.enabled) {
            action.Enable();
        }
        return action.WasPressedThisFrame();
    }

    public void ContinueAfterReward() {
        if (rig.phase != CobraKingPhase.Rewarding || !rig.isRewardPanelShown) {
            return;
        }
        rig.hud.HideRewardPanel();
        Time.timeScale = 1f;
        rig.phase = CobraKingPhase.Done;
        onFightEnded?.Invoke();

        TreeSegmentController.BranchesGenerated -=
            rig.Arena.OnBranchesGenerated;
        // a head start: the cobra chases again from far down the trunk,
        // called out a moment after
        if (rig.greatCobra != null) {
            rig.greatCobra.StopGuard();
            rig.greatCobra.KnockBack(config.WinCobraDropU);
        }
        rig.chaseWarnTime = Time.time + 1f;
        rig.hasWarnedChase = false;
        if (rig.monkeyController != null) {
            rig.monkeyController.ClearHeightCeiling();
        }
        if (rig.struggle != null) {
            rig.struggle.EscapeGainScale = 1f;
        }
        if (Debug.isDebugBuild) {
            Debug.Log(
                "CobraKingFightController:\twon, armor given, the climb goes "
                    + "on",
                rig.owner
            );
        }
        if (rig.IsRush) {
            ArmNextRush();
        }
    }

    // Endless Boss Rush  ------------------------------------------------------
    // the king waits again two bosses on, the Spider King's in b/t, harder;
    // the won armor stays on & the crown on the monkey's head
    public void ArmNextRush() {
        rig.Arena.ReleaseRushRange();
        rig.rushEncounter += 2;
        rig.phase = CobraKingPhase.Waiting;
        rig.body.Clear();
        rig.Feedback.ClearAttackObjects();
        rig.Feedback.ClearMarkers();
        rig.Arena.ClearFruits();
        foreach (SpiderWebTrap web in rig.webs) {
            if (web != null) {
                UnityEngine.Object.Destroy(web.gameObject);
            }
        }
        rig.webs.Clear();
        rig.attackQueue.Clear();
        rig.loop = CobraKingLoop.Intro;
        rig.isMinionStreaming = false;
        rig.isWaveRunning = false;
        rig.isFinaleSweepNext = false;
        rig.hasHit = false;
        rig.hasThrown = false;
        rig.hasCalledMinions = false;
        rig.hasWon = false;
        rig.isFinisherWin = false;
        rig.isEasy = false;
        rig.isChoosing = false;
        rig.isArenaFruitGone = false;
        rig.arenaBranchSpots.Clear();
        rig.arenaBranchFruits.Clear();
        rig.goldenIndex = -1;
        rig.isRewardPanelShown = false;
        rig.isReturningToStart = false;
        rig.hasWarnedChase = true;
        if (rig.cameraFraming != null) {
            rig.cameraFraming.SetBossFraming(0f, 1f);
        }
        rig.Arena.ReserveRushRange();
        if (Debug.isDebugBuild) {
            Debug.Log(
                "CobraKingFightController:\tnext rush fight at "
                    + rig.TriggerClimbU + " u",
                rig.owner
            );
        }
    }

}
