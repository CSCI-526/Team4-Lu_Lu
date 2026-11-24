using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>
/// The second boss, the Spider King, high on the climb (or near the start
/// w/ <see cref="isTestAtStart"/>). The arena is full of sticky webs from
/// the start, & the spider chases the monkey round it. The player kills it
/// by grabbing watermelons, each thrown at the spider.
///
/// Attacks:
/// <list type="bullet">
/// <item>web shot: seen charging, it fires a fast homing silk bullet:
/// double tap Space as it turns gold to bat it back w/ the tail (the
/// spider is stuck a while), else the monkey is stuck in a web</item>
/// <item>dash: once it has crawled up close it rears up, then charges at
/// the monkey along a red lane; only dodged, fatal like any touch of it</item>
/// <item>slash: from afar, three slashes of its front legs, each a red
/// crescent flying fast along the bark at the monkey, the gaps slow then
/// fast or fast then slow; fatal, only dodged</item>
/// </list>
/// A trapped monkey is crawled to & wrapped (fatal) unless it breaks free
/// first. The fight opens w/ lessons, each passed only by doing it. The
/// Cobra King's gold armor, if still worn, saves one fatal hit; a win gives
/// the silk gloves. Built at runtime from primitives.
/// </summary>
[RequireComponent(typeof(SpiderKingBody))]
[RequireComponent(typeof(BossFightHud))]
public class SpiderKingFightController: MonoBehaviour {
    /// <returns>if the fight is on, from its start till the reward is
    /// taken</returns>
    public bool IsInArena => rig.IsInArena;

    public static SpiderKingFightController I {
        get;
        private set;
    }

    /// <returns>if this fight sets the camera framing now, so the Cobra
    /// King's leftover armor logic leaves it alone</returns>
    public bool IsFraming => rig.IsInArena;

    /// <returns>climbed height the fight starts at, for its trunk height
    /// marker; u, -1 when unknown</returns>
    public float TriggerHeightU => tuning.WorldBalanceConfig != null
        ? rig.TriggerClimbU
        : -1f;

    /// <returns>if its height marker is done w/: the fight has begun</returns>
    public bool IsMarkerDone => rig.phase != SpiderKingPhase.Waiting;

    /// <returns>a small look-alike of the spider for its height marker, or
    /// null w/o a body</returns>
    public Transform BuildMarkerFigure() {
        return rig.body != null ? rig.body.BuildMarkerFigure() : null;
    }

    /// <summary>raised when the fight begins</summary>
    public event System.Action FightStarted;

    /// <summary>raised when a revive restarts the fight</summary>
    public event System.Action Revived;

    /// <summary>raised when the reward is taken & the climb goes on</summary>
    public event System.Action FightEnded;

    /// <summary>raised as each attack strikes, after its charge or wind
    /// up: a web bullet fired, a slash flying, a dash going</summary>
    public event System.Action AttackStruck;

    /// <summary>raised each time the spider is hurt: a melon, a parried
    /// slash</summary>
    public event System.Action SpiderHit;

    /// <summary>raised when the beaten spider starts to fall</summary>
    public event System.Action Defeated;

    /// <summary>
    /// Called by <see cref="CobraSensor"/> when the great cobra catches the
    /// monkey: in the fight it is a fatal hit, taken by the armor if worn.
    /// </summary>
    /// <returns>true if handled here, so the sensor must not end the run
    /// </returns>
    public bool TryAbsorbCobraCatch() {
        if (rig.monkey == null) {
            return false;
        }
        if (
            rig.phase == SpiderKingPhase.Finishing
            || rig.phase == SpiderKingPhase.ReviveOffer
            || rig.phase == SpiderKingPhase.WatchingAd
        ) {
            return true;
        }
        if (rig.phase != SpiderKingPhase.Fighting) {
            return false;
        }
        if (Time.time < rig.saveGraceEndTime) {
            return true;
        }
        if (damage.TrySpendArmor()) {
            if (rig.greatCobra != null) {
                rig.greatCobra.KnockBack(0f);
            }
            ending.ReturnToStart();
            return true;
        }
        ending.StartFinisher(false);
        return true;
    }

    /// <summary>
    /// Called by <see cref="ArmController"/> on every grab: an arena melon
    /// (or a banana) restores stamina & is thrown at the spider.
    /// </summary>
    /// <returns>true if the grab was handled here, so the arm skips its own
    /// fly-to-the-bar effect</returns>
    public bool TryGrab(Collider fruit, Vector3 from) {
        if (rig.phase != SpiderKingPhase.Fighting || fruit == null
            || !fruit.CompareTag(FRUIT_TAG)) {
            return false;
        }
        ScorePickup pickup = fruit.GetComponentInParent<ScorePickup>();
        if (pickup != null && pickup.Kind == PickupKind.Coconut) {
            return false;
        }
        // the golden melon: the armor back & full stamina, nothing thrown
        bool isGoldenGrab =
            (rig.goldenMelon != null && rig.goldenMelon.trigger == fruit)
            || (rig.goldenTwin != null && rig.goldenTwin.trigger == fruit);
        if (isGoldenGrab) {
            arena.HideGoldenMelon();
            rig.nextGoldenTime = Time.time + tuning.GoldenIntervalS;
            StaminaBarController full = StaminaBarController.I;
            if (full != null) {
                full.AddStamina(full.MaxStamina);
            }
            SpiderKingDamage.GiveArmor();
            return true;
        }
        // a choice melon picks the mode, & is thrown too
        bool isEasyChoice = rig.choiceEasy != null
            && rig.choiceEasy.trigger == fruit;
        bool isNormalChoice = rig.choiceNormal != null
            && rig.choiceNormal.trigger == fruit;
        if (isEasyChoice || isNormalChoice) {
            melons.LaunchMelon(fruit.gameObject, from);
            arena.ChooseMode(isEasyChoice);
            return true;
        }
        int index = rig.melons.FindIndex(m => m.trigger == fruit);
        if (index >= 0) {
            rig.melons[index].backTime = Time.time + tuning.MelonRespawnS;
        }
        StaminaBarController stamina = StaminaBarController.I;
        if (stamina != null) {
            stamina.AddStaminaByFruit();
        }
        melons.LaunchMelon(fruit.gameObject, from);
        return true;
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    private SpiderKingTuning tuning = new();

    // Private Members  ########################################################
    private SpiderKingRig rig;
    private SpiderKingArena arena;
    private SpiderKingMelons melons;
    private SpiderKingMovement movement;
    private SpiderKingAttacks attacks;
    private SpiderKingShots shots;
    private SpiderKingParry parry;
    private SpiderKingTeaching teaching;
    private SpiderKingDamage damage;
    private SpiderKingEnding ending;
    private SpiderKingFeedback feedback;

    private void Awake() {
        rig = new SpiderKingRig(this, tuning);
        arena = new SpiderKingArena(rig);
        melons = new SpiderKingMelons(rig);
        movement = new SpiderKingMovement(rig);
        attacks = new SpiderKingAttacks(rig);
        shots = new SpiderKingShots(rig);
        parry = new SpiderKingParry(rig);
        teaching = new SpiderKingTeaching(rig);
        damage = new SpiderKingDamage(rig);
        ending = new SpiderKingEnding(rig, () => Revived?.Invoke(),
            () => FightEnded?.Invoke());
        feedback = new SpiderKingFeedback(rig);
        rig.Arena = arena;
        rig.Melons = melons;
        rig.Movement = movement;
        rig.Attacks = attacks;
        rig.Shots = shots;
        rig.Parry = parry;
        rig.Teaching = teaching;
        rig.Damage = damage;
        rig.Ending = ending;
        rig.Feedback = feedback;
        rig.onAttackStruck = () => AttackStruck?.Invoke();
        rig.onSpiderHit = () => SpiderHit?.Invoke();
        rig.onDefeated = () => Defeated?.Invoke();
        // Inspector Assignment Guard  -----------------------------------------
        if (tuning.WorldBalanceConfig == null || tuning.WebPrefab == null
            || tuning.BananaBranchPrefab == null) {
            Debug.LogError(
                "SpiderKingFightController:\tmust assign Inspector Fields: "
                    + "worldBalanceConfig, webPrefab, bananaBranchPrefab",
                this
            );
            enabled = false;
            return;
        }
        if (I != null && I != this) {
            Debug.LogWarning(
                "SpiderKingFightController:\tduplicate instance destroyed",
                this
            );
            Destroy(gameObject);
            return;
        }
        I = this;
        if (tuning.ConfirmAction == null || tuning.GiveUpAction == null) {
            Debug.LogWarning(
                "SpiderKingFightController:\tmust assign Inspector Fields: "
                    + "confirmAction, giveUpAction",
                this
            );
        }

        rig.body = GetComponent<SpiderKingBody>();
        rig.hud = GetComponent<BossFightHud>();
        rig.hud.WatchAdClicked += ending.WatchAd;
        rig.hud.GiveUpClicked += ending.GiveUp;
        rig.hud.CoursePageClicked += ending.OpenCoursePage;
        rig.hud.ContinueClicked += ending.ContinueAfterReward;
        rig.hud.SkipAdClicked += ending.SkipAd;
        rig.hud.ModeChosen += OnModeChosen;
        rig.colorBlock = new MaterialPropertyBlock();
        if (Debug.isDebugBuild) {
            Debug.Log(
                "SpiderKingFightController:\tready, test at start "
                    + tuning.IsTestAtStart,
                this
            );
        }
    }

    // the endless run: the boss rush's second fight, & every other one
    // after, is the spider's
    private void Start() {
        if (I == this && EndlessBossRush.IsOn) {
            rig.rushEncounter = 1;
        }
    }

    // the revive's difficulty card picked: time runs again & the fight goes
    // on in that mode
    private void OnModeChosen(bool isEasy) {
        if (rig.teach != SpiderKingTeachStep.Choose
            || rig.phase != SpiderKingPhase.Fighting) {
            return;
        }
        Time.timeScale = 1f;
        arena.ChooseMode(isEasy);
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
        // a duplicate or a domain reload in Play leaves no roles to unhook
        if (arena != null) {
            TreeSegmentController.BranchesGenerated -=
                arena.OnBranchesGenerated;
        }
    }

    // the actions are shared, so only switched on here, never off
    private void OnEnable() {
        tuning.ConfirmAction?.action.Enable();
        tuning.GiveUpAction?.action.Enable();
        // a domain reload in Play keeps the component but not its roles
        if (parry == null) {
            return;
        }
        RenderPipelineManager.beginCameraRendering += parry.OnBeginCamera;
        RenderPipelineManager.endCameraRendering += parry.OnEndCamera;
    }

    private void OnDisable() {
        if (parry == null || rig == null) {
            return;
        }
        RenderPipelineManager.beginCameraRendering -= parry.OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= parry.OnEndCamera;
        rig.shotEndTime = 0f;
        if (
            rig.phase == SpiderKingPhase.Finishing
            || rig.phase == SpiderKingPhase.ReviveOffer
            || rig.phase == SpiderKingPhase.WatchingAd
            || rig.phase == SpiderKingPhase.Rewarding
            || rig.isTimeBent
        ) {
            Time.timeScale = 1f;
        }
        if (rig.monkeyController != null && rig.IsInArena) {
            rig.monkeyController.ClearHeightCeiling();
        }
        if (rig.cameraFraming != null && rig.IsInArena) {
            rig.cameraFraming.SetBossFraming(0f, 1f);
        }
    }

    private void Update() {
        GameController game = GameController.I;
        if (game == null) {
            return;
        }
        if (rig.phase == SpiderKingPhase.Waiting) {
            // the endless run's rush keeps the coming arena bare; kept up
            // every frame, as the run's start y is only known after a Start
            arena.ReserveRushRange();
            UpdatePreview(game);
            if (CanBegin(game)) {
                BeginFight();
            }
            return;
        }
        if (!game.IsRunLive || rig.monkey == null
            || rig.phase == SpiderKingPhase.Done
                || rig.phase == SpiderKingPhase.Over) {
            return;
        }
        if (rig.phase == SpiderKingPhase.Rewarding) {
            ending.UpdateRewardPause(game);
            return;
        }
        // the revive's difficulty card holds time still till a pick
        if (rig.phase == SpiderKingPhase.Fighting
            && rig.teach == SpiderKingTeachStep.Choose
            && rig.hud.IsChoosingMode) {
            Time.timeScale = 0f;
            return;
        }

        rig.hud.SpeechAnchor = rig.body.HeadPosition;
        feedback.UpdateCamera();
        parry.UpdateSparks();
        parry.UpdateClashFx();
        if (rig.phase == SpiderKingPhase.Fighting) {
            ending.UpdateReturnToStart();
            arena.UpdateMelons();
            arena.UpdateGoldenMelon();
            arena.UpdateSkyWeb();
            arena.UpdateWebs();
            shots.UpdateCrescents();
            parry.UpdateParryInput(game);
            movement.UpdateSpider();
            shots.UpdateShots();
            melons.UpdateThrows();
            parry.UpdateTimeScale(game);
        } else if (rig.phase == SpiderKingPhase.ReviveOffer
            || rig.phase == SpiderKingPhase.WatchingAd) {
            ending.UpdateRevive(game);
        } else if (rig.phase == SpiderKingPhase.Finishing) {
            ending.UpdateFinisher();
        } else if (rig.phase == SpiderKingPhase.Defeated) {
            ending.UpdateDefeat();
        }
    }

    // Setup  ------------------------------------------------------------------
    // after the Cobra King, never mid leap; in the endless run at its turn
    // of the boss rush
    private bool CanBegin(GameController game) {
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        if (!game.IsRunLive || game.IsLeaping || ramp == null) {
            return false;
        }
        if (rig.IsRush) {
            return game.State == GameState.Playing
                && ramp.CurrentPlayerClimbedYDistance >= rig.TriggerClimbU;
        }
        if (game.Mode == RunMode.Endless) {
            return false;
        }
        if (tuning.IsTestAtStart) {
            return ramp.CurrentPlayerClimbedYDistance >=
                tuning.TestStartHeightU;
        }
        CobraKingFightController cobraKing = CobraKingFightController.I;
        if (game.State != GameState.Playing
            || (cobraKing != null && !cobraKing.IsDone)) {
            return false;
        }
        return ramp.CurrentPlayerClimbedYDistance
            >= tuning.WorldBalanceConfig.SpiderTriggerHeightU;
    }

    // from just below the spider jump height on, the Spider King already
    // waits idle above its trigger height, so the player sees the fight ahead
    // the monkey the waiting spider keeps above, once it is seen
    private Transform previewMonkey;
    // how fast the waiting spider crawls round after it; deg/s
    private const float PREVIEW_TURN_DEG_PER_S = 120f;

    private void UpdatePreview(GameController game) {
        if (tuning.IsTestAtStart) {
            return;
        }
        if (rig.body.IsBuilt) {
            // the waiting spider crawls round to stay right above the
            // climbing monkey, so the fight starts w/ it in plain view
            if (previewMonkey != null) {
                rig.previewAngle = Mathf.MoveTowardsAngle(rig.previewAngle,
                    rig.AngleOf(previewMonkey.position) + START_TURN_DEG,
                    PREVIEW_TURN_DEG_PER_S * Time.deltaTime);
            }
            rig.body.Place(rig.previewAngle, rig.previewY, 0f);
            return;
        }
        if (!game.IsRunLive
            || (game.Mode == RunMode.Endless && !rig.IsRush)) {
            return;
        }
        // seen waiting from a bit below: the jump height, or 50 u below the
        // fight in the endless run's rush
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        float seenFromU = rig.IsRush ? rig.TriggerClimbU - 50f
            : tuning.WorldBalanceConfig.SpiderJumpHeightU - 5f;
        if (ramp == null || ramp.CurrentPlayerClimbedYDistance < seenFromU) {
            return;
        }
        MonkeyController found = FindFirstObjectByType<MonkeyController>();
        if (found == null || found.transform.parent == null) {
            return;
        }
        rig.trunkCenter = found.transform.parent.position;
        previewMonkey = found.transform;
        rig.previewAngle =
            rig.AngleOf(found.transform.position) + START_TURN_DEG;
        rig.previewY = ramp.PlayerGameStartYPosition
            + rig.TriggerClimbU + START_ABOVE_U;
        rig.body.Build(rig.trunkCenter, rig.previewAngle, rig.previewY);
    }

    private void BeginFight() {
        rig.monkeyController = FindFirstObjectByType<MonkeyController>();
        if (rig.monkeyController == null
            || rig.monkeyController.transform.parent == null) {
            Debug.LogError(
                "SpiderKingFightController:\tno MonkeyController under a trunk "
                    + "root found",
                this
            );
            enabled = false;
            return;
        }
        rig.monkey = rig.monkeyController.transform;
        // a bit quicker in the arena, to dodge & reach the melons
        rig.monkeyController.ArenaSpeedScale = tuning.MonkeySpeedScale;
        GameController.I.EndStartingZone();
        rig.trunkCenter = rig.monkey.parent.position;
        rig.entryY = rig.monkey.position.y;
        rig.entryPosition = rig.monkey.position;
        rig.entryRotation = rig.monkey.rotation;
        rig.phase = SpiderKingPhase.Fighting;
        FightStarted?.Invoke();

        arena.MakeArenaBare();

        rig.cameraFraming = FindFirstObjectByType<CameraClimbFraming>();
        rig.greatCobra = FindFirstObjectByType<CobraMotor>();
        if (rig.greatCobra != null) {
            rig.greatCobra.StartGuard(rig.entryY - tuning.GuardDepthU);
        }
        // a web breaks in as many presses as in the Cobra King's arena
        rig.struggle = FindFirstObjectByType<SpiderWebStruggle>();
        if (rig.struggle != null) {
            CobraKingFightController cobraArena = CobraKingFightController.I;
            rig.struggle.EscapeGainScale = cobraArena != null
                ? cobraArena.WebEscapeGainScale
                : 2f;
        }
        rig.monkeyController.SetHeightCeiling(rig.entryY + tuning.ArenaHeightU);
        arena.BuildSkyWeb();
        rig.tail = SpiderKingFeedback.FindChild(rig.monkey, "Tail");
        if (rig.tail == null) {
            Debug.LogWarning(
                "SpiderKingFightController:\tno Tail under the monkey, the "
                    + "parry "
                    + "swings nothing",
                this
            );
        }

        // always right above the monkey as it enters, never across the trunk
        rig.startAngle = rig.AngleOf(rig.monkey.position) + START_TURN_DEG;
        rig.startY = rig.entryY + START_ABOVE_U;
        rig.spiderAngle = rig.startAngle;
        rig.spiderY = rig.startY;
        rig.maxHealth = rig.BaseMaxHealth;
        rig.health = rig.maxHealth;
        rig.isEasy = false;
        rig.lastStage = 0;
        rig.body.Build(rig.trunkCenter, rig.spiderAngle, rig.spiderY);
        // the whole arena is there from the start, melons too
        arena.SpawnMelons();
        arena.ClearStickyWebs();
        movement.ResetSpiderState();

        rig.hud.Show(tuning.BossName);
        rig.hud.SetHealth(rig.health, rig.maxHealth);
        // flashes as the Cobra King's: at the start, the last blow & a
        // spent armor; never on a revive or a reward
        rig.hud.Flash();
        // only web bullets till the first parry is done, then the fight; the
        // first slash later teaches its own parry
        rig.teach = rig.hasTaughtWeb ? SpiderKingTeachStep.Done
            : SpiderKingTeachStep.Parry;
        teaching.Teach(tuning.BossName, "", tuning.WarnColor, 2f);
        // the gold armor, as in the Cobra King's arena: it swirls on, no
        // words; worn already, it stays
        SpiderKingDamage.GiveArmor();
        rig.hud.Say("My web, my rules!");
        if (Debug.isDebugBuild) {
            Debug.Log(
                "SpiderKingFightController:\tspider fight begins at y "
                    + rig.entryY,
                this
            );
        }
    }

}
