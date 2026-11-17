using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static CobraKingRig;

/// <summary>
/// Runs the boss fight against the Cobra King. Once the monkey has climbed
/// <see cref="WorldBalanceConfig.BossTriggerHeightU"/> the screen flashes and
/// the run enters the boss arena: the king coils round the tree top above,
/// the great cobra stops chasing and guards below, and the spawners of webs,
/// falling cobras and wet bark stop. Banana branches grow on the trunk in
/// between; a grabbed banana still feeds the monkey and is also thrown in an
/// arc at the king's crown by itself, so the fight uses only the grab the
/// player already knows.
/// <para>
/// The fight loops: the king rests, then attacks; a banana hurts it at any
/// time. Before the first hit it only rests, and the first banana carries a
/// key prompt and a dotted line to the jewel. Its attacks grow with each lost
/// third of its health: a bite
/// at a marked spot, a tail sweep through a marked ring (both fatal, both
/// warned), and minions dropping on marked spots (a knock-down). A golden
/// banana refills stamina and gives a gold shield that turns one fatal hit
/// into a knock-down.
/// </para>
/// <para>
/// The monkey can never climb close to the king: a height ceiling holds it
/// below the chin however long up is held, and a monkey at the ceiling, or
/// touching the king, is butted down often ("Stay away from me!"), never
/// fatally. The great cobra guards below without moving, peeking up now &
/// then; a monkey below the arena hears "Nowhere to run!".
/// The camera tilts up so the king stays in view. At zero health the king
/// shatters into cubes, its crown flies onto the monkey's head and the run
/// is won (<see cref="GameController.WinGame"/>).
/// </para>
/// </summary>
[RequireComponent(typeof(CobraKingBody), typeof(BossFightHud))]
public class CobraKingFightController: MonoBehaviour {
    public static CobraKingFightController I {
        get;
        private set;
    }

    /// <returns>if the monkey is in the boss arena, fighting or done; never
    /// in the endless run, which skips the fight</returns>
    public bool IsInArena => rig.phase != CobraKingPhase.Waiting
        && !rig.isEndlessStart;

    /// <summary>
    /// Builds a small look-alike of the Cobra King for the trunk height
    /// marker at the fight's height, its face toward the parent's -z.
    /// </summary>
    /// <returns>the figure's root, unparented, or null</returns>
    public Transform BuildMarkerFigure() {
        return rig.body != null
            ? rig.body.BuildFigure(tuning.MarkerFigureScale) : null;
    }

    /// <returns>climbed height the fight starts at, eg for its trunk height
    /// marker; u, -1 when unknown</returns>
    public float TriggerHeightU => tuning.WorldBalanceConfig != null
        ? tuning.WorldBalanceConfig.BossTriggerHeightU
        : -1f;

    /// <summary>raised when the monkey enters the arena & the fight begins
    /// </summary>
    public event System.Action FightStarted;

    /// <summary>raised when the king is beaten, its reward taken and the
    /// climb goes on</summary>
    public event System.Action FightEnded;

    /// <summary>raised when the gold armor takes a great cobra catch</summary>
    public event System.Action ArmorAbsorbed;

    /// <summary>raised when a revive restarts the fight</summary>
    public event System.Action Revived;

    /// <returns>highest world y a pineapple leap may carry the monkey to:
    /// the fight's start height before the fight, so a leap stops at the
    /// arena instead of flying past it, the ceiling under the king during
    /// it, & no limit once it is over</returns>
    public float LeapTopY {
        get {
            if (!isActiveAndEnabled) {
                return float.PositiveInfinity;
            }
            if (rig.phase == CobraKingPhase.Waiting) {
                RampedDifficultyManager ramp = RampedDifficultyManager.I;
                // a hair over, so landing there surely starts the fight
                return ramp != null && tuning.WorldBalanceConfig != null
                    ? ramp.PlayerGameStartYPosition
                        + tuning.WorldBalanceConfig.BossTriggerHeightU + 0.05f
                    : float.PositiveInfinity;
            }
            bool isOver = rig.phase == CobraKingPhase.Defeated
                || rig.phase == CobraKingPhase.Rewarding
                || rig.phase == CobraKingPhase.Done
                || rig.phase == CobraKingPhase.Over;
            return isOver
                ? float.PositiveInfinity
                : rig.body.LowestY - tuning.CeilingGapU;
        }
    }

    /// <summary>
    /// Called by <see cref="CobraSensor"/> when the great cobra catches the
    /// monkey: the gold armor, worn in the fight or kept as the win reward,
    /// takes the bite once, throwing the cobra back down the trunk.
    /// </summary>
    /// <returns>true if the catch was handled here, so the sensor must not
    /// end the run: absorbed by the armor, or in the fight turned into a
    /// boss fight loss that offers a revive</returns>
    /// <returns>scale on each web escape press in the arena, also used by the
    /// Spider King's, so both shake a web off in as many presses</returns>
    public float WebEscapeGainScale => tuning.WebEscapeGainScale;

    /// <returns>if the Cobra King is beaten (or skipped) & the climb is on
    /// past it, eg so the Spider King may start</returns>
    public bool IsDone => rig.phase == CobraKingPhase.Done;

    /// <summary>if the fight has not begun yet</summary>
    public bool IsWaiting => rig.phase == CobraKingPhase.Waiting;

    /// <returns>if the monkey still wears the gold armor won from the Cobra
    /// King, not yet spent on the climb after</returns>
    public bool HasArmor => rig.hasShield
        && (rig.phase == CobraKingPhase.Done
            || rig.phase == CobraKingPhase.Waiting);

    /// <summary>
    /// Puts the gold armor on the monkey w/ the win's swirl, its badge
    /// showing as it lands, eg at the Spider King fight's start or from its
    /// golden melon; also before this fight (the Spider King test fight),
    /// where it is only worn & spent, never starts this fight.
    /// </summary>
    /// <returns>if the armor was put on, false if already worn</returns>
    public bool GiveArmor() {
        bool isIdle = rig.phase == CobraKingPhase.Done
            || rig.phase == CobraKingPhase.Waiting;
        if (!isIdle || rig.hasShield) {
            return false;
        }
        if (rig.monkey == null) {
            MonkeyController found = FindFirstObjectByType<MonkeyController>();
            if (found == null || found.transform.parent == null) {
                return false;
            }
            rig.monkey = found.transform;
            rig.monkeyController = found;
            rig.trunkCenter = rig.monkey.parent.position;
        }
        if (rig.armor == null) {
            armor.BuildShieldRing();
        }
        rig.hasShield = true;
        rig.armorRewardTimeS = 0f;
        rig.hasArmorLanded = false;
        return true;
    }

    /// <summary>
    /// Spends the kept gold armor on a fatal hit somewhere else, eg in the
    /// Spider King fight: the badge goes & "Saved!" pops at the monkey.
    /// </summary>
    /// <returns>if there was armor to spend</returns>
    public bool TrySpendArmor() {
        if (!HasArmor || rig.monkey == null) {
            return false;
        }
        rig.hasShield = false;
        ArmorAbsorbed?.Invoke();
        rig.hud.ShowArmorBadge(false);
        rig.hud.PopText("Saved!", rig.monkey.position, tuning.GoldenColor);
        rig.hud.Flash(tuning.GoldenColor);
        // the armor shatters off at once, never left hanging where it was
        if (rig.armor != null) {
            foreach (Transform piece in rig.armor) {
                throws.BurstAt(piece.position, tuning.GoldenColor);
                piece.gameObject.SetActive(false);
            }
        }
        rig.armorRewardTimeS = -1f;
        return true;
    }

    /// <summary>
    /// Test hook for the spider jump hack key: a fight not begun yet is
    /// skipped as if beaten, in the armor, like the endless start.
    /// </summary>
    public void SkipForTest() {
        if (rig.phase == CobraKingPhase.Waiting) {
            StartAsDone();
        }
    }

    public bool TryAbsorbCobraCatch() {
        if (rig.monkey == null) {
            return false;
        }
        // contacts keep coming for a moment after a save
        if (Time.time < rig.cobraSaveGraceEndTime) {
            return true;
        }
        // the finisher, revive offer & ad already handle the monkey's end
        if (
            rig.phase == CobraKingPhase.Finishing
            || rig.phase == CobraKingPhase.ReviveOffer
            || rig.phase == CobraKingPhase.WatchingAd
        ) {
            return true;
        }
        bool canSave = rig.phase == CobraKingPhase.Fighting
            || rig.phase == CobraKingPhase.Defeated
            || rig.phase == CobraKingPhase.Rewarding
            || rig.phase == CobraKingPhase.Done;
        if (!canSave) {
            return false;
        }
        if (!rig.hasShield) {
            // caught by the guard in the fight: a boss fight loss, w/ its
            // slow motion & the revive offer, not a plain end of the run
            if (rig.phase == CobraKingPhase.Fighting) {
                ending.StartFinisher(false);
                return true;
            }
            return false;
        }
        rig.hasShield = false;
        ArmorAbsorbed?.Invoke();
        rig.hud.ShowArmorBadge(false);
        rig.cobraSaveGraceEndTime = Time.time + 1f;
        rig.hud.PopText("Saved!", rig.monkey.position, tuning.GoldenColor);
        rig.hud.Flash(tuning.GoldenColor);
        // in the fight that is the guard below: its peek ends & the monkey
        // flies back up to where the fight began, never left under the guard,
        // where it could not climb back alive; no freeze, as in the fight
        // the armor only saves, the same "Saved!" as any fatal hit
        bool isGuardStill = rig.phase == CobraKingPhase.Fighting
            || rig.phase == CobraKingPhase.Defeated
            || rig.phase == CobraKingPhase.Rewarding;
        if (isGuardStill) {
            if (rig.greatCobra != null) {
                rig.greatCobra.KnockBack(0f);
            }
            if (rig.phase == CobraKingPhase.Fighting) {
                ending.ReturnToStart();
            }
            return true;
        }
        // the kept reward armor on the climb after: it shatters off w/ a gold
        // burst, & the cobra is thrown far down & frozen, more than a
        // coconut does
        if (rig.armor != null) {
            foreach (Transform piece in rig.armor) {
                throws.BurstAt(piece.position, tuning.GoldenColor);
                piece.gameObject.SetActive(false);
            }
        }
        if (rig.greatCobra != null) {
            rig.greatCobra.KnockBack(tuning.ArmorKnockBackU);
            rig.greatCobra.TryFreeze(tuning.ArmorCobraFreezeS);
        }
        return true;
    }

    /// <summary>
    /// Called by <see cref="ArmController"/> on every grab: in the arena a
    /// grabbed banana still feeds the monkey, & is thrown at the king too;
    /// the golden banana gives its shield instead.
    /// </summary>
    /// <returns>true if the grab was handled here, so the arm skips its own
    /// fly-to-the-bar effect</returns>
    public bool TryThrow(Collider fruit, Vector3 from) {
        if (rig.phase != CobraKingPhase.Fighting || fruit == null
            || !fruit.CompareTag(FRUIT_TAG)) {
            return false;
        }

        // the first throw picks the mode: plain banana easy, golden normal
        int choice = rig.choices.FindIndex(f => f.trigger == fruit);
        if (choice >= 0) {
            bool isEasyPick = !rig.choices[choice].isGolden;
            StaminaBarController staminaBar = StaminaBarController.I;
            if (staminaBar != null) {
                staminaBar.AddStaminaByFruit();
            }
            LaunchBanana(fruit.gameObject, from);
            arena.ChooseMode(isEasyPick);
            return true;
        }

        int index = rig.fruits.FindIndex(f => f.trigger == fruit);
        if (index >= 0) {
            CobraKingArenaFruit picked = rig.fruits[index];
            if (picked.isGolden) {
                armor.HideGolden();
                rig.nextGoldenTime = Time.time + tuning.GoldenIntervalS;
                armor.TakeGoldenBanana();
                return true;
            }
            picked.respawnTime = Time.time
                + tuning.BananaRespawnS * rig.BananaRespawnScale;
            rig.fruits[index] = picked;
        }

        // a banana on the tree's own branches flies too
        StaminaBarController stamina = StaminaBarController.I;
        if (stamina != null) {
            stamina.AddStaminaByFruit();
        }
        LaunchBanana(fruit.gameObject, from);
        return true;
    }

    // a copy of the grabbed banana arcs up at the king, spinning, with a
    // trail of fading dots, so the throw reads at a glance
    private void LaunchBanana(GameObject source, Vector3 from) {
        // the armor intro stays up until the first throw
        if (!rig.hasThrown) {
            rig.hasThrown = true;
            rig.hud.HideBanner();
        }
        GameObject copy = Instantiate(source, transform, true);
        // a bare look-alike: no scripts or colliders wake up on it
        foreach (MonoBehaviour script in
            copy.GetComponentsInChildren<MonoBehaviour>(true)) {
            DestroyImmediate(script);
        }
        foreach (Collider part in
            copy.GetComponentsInChildren<Collider>(true)) {
            DestroyImmediate(part);
        }
        copy.transform.position = from;
        copy.transform.localScale = source.transform.lossyScale
            * tuning.ThrownBananaScale;
        copy.SetActive(true);
        rig.projectiles.Add(new CobraKingProjectile {
            transform = copy.transform,
            start = from,
            ageS = 0f,
            nextTrailS = 0f
        });
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    private CobraKingTuning tuning = new();

    // Private Members  ########################################################
    private CobraKingRig rig;
    private CobraKingArena arena;
    private CobraKingArmor armor;
    private CobraKingThrows throws;
    private CobraKingAttacks attacks;
    private CobraKingMinions minions;
    private CobraKingEnding ending;
    private CobraKingFeedback feedback;

    private void Awake() {
        rig = new CobraKingRig(this, tuning);
        arena = new CobraKingArena(rig);
        armor = new CobraKingArmor(rig);
        throws = new CobraKingThrows(rig);
        attacks = new CobraKingAttacks(rig);
        minions = new CobraKingMinions(rig);
        ending = new CobraKingEnding(rig, () => Revived?.Invoke(),
            () => FightEnded?.Invoke());
        feedback = new CobraKingFeedback(rig);
        rig.Arena = arena;
        rig.Armor = armor;
        rig.Throws = throws;
        rig.Attacks = attacks;
        rig.Minions = minions;
        rig.Ending = ending;
        rig.Feedback = feedback;
        // Inspector Assignment Guard  -----------------------------------------
        if (tuning.WorldBalanceConfig == null) {
            Debug.LogError(
                "CobraKingFightController:\tmust assign Inspector Field: "
                    + "worldBalanceConfig",
                this
            );
            enabled = false;
            return;
        }

        if (tuning.BananaBranchPrefab == null) {
            Debug.LogError(
                "CobraKingFightController:\tmust assign Inspector Field: "
                    + "bananaBranchPrefab",
                this
            );
            enabled = false;
            return;
        }

        if (I != null && I != this) {
            Debug.LogWarning(
                "CobraKingFightController:\tduplicate instance destroyed",
                this
            );
            Destroy(gameObject);
            return;
        }
        I = this;

        bool hasSpiderFight = FindFirstObjectByType<SpiderKingFightController>(
            FindObjectsInactive.Include
        ) != null;
        if (tuning.SpiderFightPrefab != null && !hasSpiderFight) {
            Instantiate(tuning.SpiderFightPrefab);
        }

        if (tuning.ConfirmAction == null || tuning.GiveUpAction == null) {
            Debug.LogWarning(
                "CobraKingFightController:\tmust assign Inspector Fields: "
                    + "confirmAction, giveUpAction",
                this
            );
        }

        rig.body = GetComponent<CobraKingBody>();
        rig.body.MonkeyTouched += attacks.OnKingTouched;
        rig.hud = GetComponent<BossFightHud>();
        rig.hud.WatchAdClicked += ending.WatchAd;
        rig.hud.GiveUpClicked += ending.GiveUp;
        rig.hud.CoursePageClicked += ending.OpenCoursePage;
        rig.hud.ContinueClicked += ending.ContinueAfterReward;
        rig.hud.SkipAdClicked += ending.SkipAd;
        rig.colorBlock = new MaterialPropertyBlock();
    }

    // the endless run has no fight: it starts in the state right after one
    private void Start() {
        if (I == this && GameController.I != null
            && GameController.I.Mode == RunMode.Endless) {
            StartAsDone();
        }
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
        TreeSegmentController.BranchesGenerated -= arena.OnBranchesGenerated;
    }

    // the actions are shared with other scripts, so they are only switched
    // on here, never off
    private void OnEnable() {
        tuning.ConfirmAction?.action.Enable();
        tuning.GiveUpAction?.action.Enable();
    }

    private void OnDisable() {
        if (
            rig.phase == CobraKingPhase.Finishing
            || rig.phase == CobraKingPhase.ReviveOffer
            || rig.phase == CobraKingPhase.WatchingAd
            || rig.phase == CobraKingPhase.Rewarding
        ) {
            Time.timeScale = 1f;
        }
        if (rig.struggle != null) {
            rig.struggle.EscapeGainScale = 1f;
        }
        if (rig.monkeyController != null) {
            rig.monkeyController.ClearHeightCeiling();
        }
        if (rig.cameraFraming != null) {
            rig.cameraFraming.SetBossFraming(0f, 1f);
        }
    }

    private void Update() {
        GameController game = GameController.I;
        if (game == null) {
            return;
        }
        if (rig.phase == CobraKingPhase.Waiting) {
            // armor lent to the Spider King test fight b4 this one; the
            // ring update also hides it once spent
            if (rig.monkey != null && rig.armor != null) {
                armor.UpdateShieldRing();
                throws.UpdateSparks();
            }
            if (game.State == GameState.Playing && HasReachedTrigger()) {
                BeginFight();
            }
            return;
        }
        if (!game.IsRunLive || rig.monkey == null) {
            return;
        }
        // after a win only the reward armor is left to follow the monkey
        if (rig.phase == CobraKingPhase.Done) {
            armor.UpdateShieldRing();
            throws.UpdateSparks();
            // the Spider King fight frames the camera itself
            SpiderKingFightController spiderFight = SpiderKingFightController.I;
            if (spiderFight == null || !spiderFight.IsFraming) {
                feedback.UpdateCameraFocus();
            }
            attacks.UpdateChaseWarning();
            return;
        }
        if (rig.phase == CobraKingPhase.Rewarding) {
            armor.UpdateShieldRing();
            throws.UpdateSparks();
            feedback.UpdateCameraFocus();
            ending.UpdateRewardPause(game);
            return;
        }

        rig.body.Track(rig.monkey.position);
        rig.hud.SpeechAnchor = rig.body.HeadPosition;
        throws.UpdateProjectiles();
        arena.UpdateFruits();
        armor.UpdateGolden();
        armor.UpdateShieldRing();
        feedback.UpdateCameraFocus();
        if (rig.phase == CobraKingPhase.Fighting) {
            ending.UpdateReturnToStart();
            feedback.UpdatePendingLine();
            attacks.UpdateWarnings();
            arena.UpdateChoiceTags();
            throws.UpdateFirstThrowHint();
            attacks.UpdateFightLoop();
            minions.UpdateMinionStream();
        } else if (rig.phase == CobraKingPhase.ReviveOffer
            || rig.phase == CobraKingPhase.WatchingAd) {
            ending.UpdateRevive(game);
        } else if (rig.phase == CobraKingPhase.Finishing) {
            ending.UpdateFinisher();
        } else if (rig.phase == CobraKingPhase.Defeated) {
            ending.UpdateVictory();
        }
    }

    // Setup  ------------------------------------------------------------------
    private bool HasReachedTrigger() {
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        return ramp != null
            && ramp.CurrentPlayerClimbedYDistance
                >= tuning.WorldBalanceConfig.BossTriggerHeightU;
    }

    // the endless start: the armor worn, hazards on, the fight already over;
    // no arena, no FightStarted, and never marks the king beaten
    private void StartAsDone() {
        MonkeyController monkeyController =
            FindFirstObjectByType<MonkeyController>();
        if (monkeyController == null
            || monkeyController.transform.parent == null) {
            Debug.LogError(
                "CobraKingFightController:\tno MonkeyController under a trunk "
                    + "root found",
                this
            );
            enabled = false;
            return;
        }
        rig.monkey = monkeyController.transform;
        rig.monkeyController = monkeyController;
        rig.trunkCenter = rig.monkey.parent.position;
        rig.greatCobra = FindFirstObjectByType<CobraMotor>();
        rig.cameraFraming = FindFirstObjectByType<CameraClimbFraming>();
        rig.struggle = FindFirstObjectByType<SpiderWebStruggle>();

        rig.isEndlessStart = true;
        armor.BuildShieldRing();
        rig.hasShield = true;
        rig.phase = CobraKingPhase.Done;
        // never shown as a fight, so its UI is built here: "Saved!" & the
        // flash of a save need it
        rig.hud.BuildQuiet();
        // no chase call: the cobra is already after the monkey
        rig.hasWarnedChase = true;
        if (Debug.isDebugBuild) {
            Debug.Log("CobraKingFightController:\tendless start in armor",
                this);
        }
    }

    private void BeginFight() {
        MonkeyController monkeyController =
            FindFirstObjectByType<MonkeyController>();
        if (monkeyController == null
            || monkeyController.transform.parent == null) {
            Debug.LogError(
                "CobraKingFightController:\tno MonkeyController under a trunk "
                    + "root found",
                this
            );
            enabled = false;
            return;
        }
        rig.monkey = monkeyController.transform;
        rig.monkeyController = monkeyController;
        rig.trunkCenter = rig.monkey.parent.position;
        rig.entryY = rig.monkey.position.y;
        rig.entryPosition = rig.monkey.position;
        rig.entryRotation = rig.monkey.rotation;
        rig.coilBottomY = rig.entryY + tuning.NestHeightU;
        rig.maxHealth = tuning.WorldBalanceConfig.BossMaxHealth;
        rig.health = rig.maxHealth;
        rig.phase = CobraKingPhase.Fighting;
        FightStarted?.Invoke();
        rig.loop = CobraKingLoop.Intro;

        arena.WarnIfArenaLeavesZone();
        // the boss zone grows nothing but the fight; only a branch of a
        // segment reaching past the zone end can hang near the nest
        arena.ClearBranchesNearNest(
            FindObjectsByType<BranchController>(FindObjectsSortMode.None)
        );
        TreeSegmentController.BranchesGenerated += arena.OnBranchesGenerated;

        rig.cameraFraming = FindFirstObjectByType<CameraClimbFraming>();
        rig.greatCobra = FindFirstObjectByType<CobraMotor>();
        if (rig.greatCobra != null) {
            rig.greatCobra.StartGuard(rig.entryY - tuning.GuardDepthU);
        }

        rig.body.Build(rig.trunkCenter, rig.coilBottomY,
            rig.AngleOf(rig.monkey.position));
        rig.body.SetWeakSpotLit(true);
        // the first try is normal mode; the mode choice comes w/ a revive
        arena.SpawnFruits();
        arena.SpawnArenaBranches();
        arena.SpawnWebs(tuning.WebCount);
        arena.SpawnLowWebs();
        arena.SpawnRoughBark();
        rig.struggle = FindFirstObjectByType<SpiderWebStruggle>();
        if (rig.struggle != null) {
            rig.struggle.EscapeGainScale = tuning.WebEscapeGainScale;
        }
        armor.BuildDots();
        armor.BuildShieldRing();

        rig.hud.Show(tuning.BossName);
        rig.hud.Flash();
        // the monkey starts in the gold armor, a spare life against the
        // king; the golden banana brings it back once lost
        rig.hasShield = true;
        rig.hud.ShowBannerFor(tuning.ArmorIntroTitle, tuning.ArmorIntroLine,
            tuning.GoldenColor, 0f);
        if (Debug.isDebugBuild) {
            Debug.Log(
                "CobraKingFightController:\tboss fight begins at y "
                    + rig.entryY,
                this
            );
        }
    }

}
