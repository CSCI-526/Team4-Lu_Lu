using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

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
public class SpiderFightController: MonoBehaviour {
    // Public Members  #########################################################
    public static SpiderFightController I {
        get; private set;
    }

    /// <returns>if the fight is on, from its start till the reward is
    /// taken</returns>
    public bool IsInArena => phase != Phase.Waiting && phase != Phase.Done
        && phase != Phase.Over;

    /// <returns>if this fight sets the camera framing now, so the Cobra
    /// King's leftover armor logic leaves it alone</returns>
    public bool IsFraming => IsInArena;

    /// <summary>raised when the fight begins</summary>
    public event System.Action FightStarted;

    /// <summary>raised when the reward is taken & the climb goes on</summary>
    public event System.Action FightEnded;

    // Public Methods  #########################################################
    /// <summary>
    /// Called by <see cref="CobraSensor"/> when the great cobra catches the
    /// monkey: in the fight it is a fatal hit, taken by the armor if worn.
    /// </summary>
    /// <returns>true if handled here, so the sensor must not end the run
    /// </returns>
    public bool TryAbsorbCobraCatch() {
        if (monkey == null) {
            return false;
        }
        if (
            phase == Phase.Finishing
            || phase == Phase.ReviveOffer
            || phase == Phase.WatchingAd
        ) {
            return true;
        }
        if (phase != Phase.Fighting) {
            return false;
        }
        if (Time.time < saveGraceEndTime) {
            return true;
        }
        if (TrySpendArmor()) {
            if (greatCobra != null) {
                greatCobra.KnockBack(0f);
            }
            ReturnToStart();
            return true;
        }
        StartFinisher(false);
        return true;
    }

    /// <summary>
    /// Called by <see cref="ArmController"/> on every grab: an arena melon
    /// (or a banana) restores stamina & is thrown at the spider.
    /// </summary>
    /// <returns>true if the grab was handled here, so the arm skips its own
    /// fly-to-the-bar effect</returns>
    public bool TryGrab(Collider fruit, Vector3 from) {
        if (phase != Phase.Fighting || fruit == null
            || !fruit.CompareTag(FRUIT_TAG)) {
            return false;
        }
        ScorePickup pickup = fruit.GetComponentInParent<ScorePickup>();
        if (pickup != null && pickup.Kind == PickupKind.Coconut) {
            return false;
        }
        // the golden melon: the armor back & full stamina, nothing thrown
        bool isGoldenGrab = (goldenMelon != null && goldenMelon.trigger == fruit)
            || (goldenTwin != null && goldenTwin.trigger == fruit);
        if (isGoldenGrab) {
            HideGoldenMelon();
            nextGoldenTime = Time.time + goldenIntervalS;
            StaminaBarController full = StaminaBarController.I;
            if (full != null) {
                full.AddStamina(full.MaxStamina);
            }
            GiveArmor();
            return true;
        }
        // a choice melon picks the mode, & is thrown too
        bool isEasyChoice = choiceEasy != null && choiceEasy.trigger == fruit;
        bool isNormalChoice = choiceNormal != null
            && choiceNormal.trigger == fruit;
        if (isEasyChoice || isNormalChoice) {
            LaunchMelon(fruit.gameObject, from);
            ChooseMode(isEasyChoice);
            return true;
        }
        int index = melons.FindIndex(m => m.trigger == fruit);
        if (index >= 0) {
            melons[index].backTime = Time.time + melonRespawnS;
        }
        StaminaBarController stamina = StaminaBarController.I;
        if (stamina != null) {
            stamina.AddStaminaByFruit();
        }
        LaunchMelon(fruit.gameObject, from);
        return true;
    }

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [Tooltip("zone & trigger heights")]
    [SerializeField]
    private WorldBalanceConfig worldBalanceConfig;

    [Tooltip("watches the ad / goes on after the reward, Player/Jump; also "
        + "the double tap of the parry")]
    [SerializeField]
    private InputActionReference confirmAction;

    [Tooltip("gives up the revive offer")]
    [SerializeField]
    private InputActionReference giveUpAction;

    [Tooltip("the climb's spider web, shot by the king & left on the trunk")]
    [SerializeField]
    private SpiderWebTrap webPrefab;

    [Tooltip("the climb's banana branch: bears the arena melons")]
    [SerializeField]
    private GameObject bananaBranchPrefab;

    [Tooltip("the melon model swapped onto the banana")]
    [SerializeField]
    private Mesh watermelonMesh;

    [Tooltip("local scale of the melon model on the banana's mesh object")]
    [SerializeField]
    private Vector3 watermelonScale = new Vector3(6f, 6f, 6f);

    [Tooltip("local offset of the melon model")]
    [SerializeField]
    private Vector3 watermelonOffset = new Vector3(0f, -0.629f, 0.06f);

    [Tooltip("the course page the mock ad's button opens")]
    [SerializeField]
    private string courseUrl =
        "https://classes.usc.edu/term-20263/course/csci-526";

    [Header("Mode Choice")]
    [Tooltip("after a revive the choice melons grow this far to each side of "
        + "where the fight began; deg")]
    [SerializeField]
    private float choiceSideDeg = 28f;

    [Tooltip("& this high above it; u")]
    [SerializeField]
    private float choiceAboveU = 1.5f;

    [Tooltip("easy mode: share of the health")]
    [SerializeField]
    [Range(0.1f, 1f)]
    private float easyHealthScale = 0.6f;

    [Tooltip("easy mode: scale on rests, wind ups & gaps, > 1 is slower")]
    [SerializeField]
    [Min(1f)]
    private float easyTimeScale = 1.4f;

    [Tooltip("easy mode: scale on the speed of its attacks & crawl")]
    [SerializeField]
    [Range(0.3f, 1f)]
    private float easySpeedScale = 0.85f;

    [Tooltip("a see-through lit material, eg the spider web's, copied for the "
        + "arena's top")]
    [SerializeField]
    private Material airWallMaterial;

    [Header("Golden Melon")]
    [Tooltip("the Cobra King's golden outline, copied & recolored")]
    [SerializeField]
    private Material goldenOutlineMaterial;

    [SerializeField]
    private Color goldenOutlineColor = new Color(1f, 0.75f, 0f);

    [SerializeField]
    [Min(1f)]
    private float goldenOutlineWidthScale = 2f;

    [Tooltip("seconds b/t two golden melons; one only shows w/ no armor on")]
    [SerializeField]
    [Min(1f)]
    private float goldenIntervalS = 10f;

    [Tooltip("seconds a golden melon stays b4 it goes")]
    [SerializeField]
    [Min(1f)]
    private float goldenShowS = 8f;

    [Header("Arena")]
    [Tooltip("test only: the fight begins once the monkey has climbed "
        + "testStartHeightU, w/o the Cobra King or the trigger height; "
        + "switch off for the real run")]
    [SerializeField]
    private bool isTestAtStart;

    [Tooltip("test only: climbed height the test fight begins at; u")]
    [SerializeField]
    [Min(0f)]
    private float testStartHeightU = 30f;

    [Tooltip("scale on the monkey's climb & orbit speed in the arena")]
    [SerializeField]
    [Min(0.5f)]
    private float monkeySpeedScale = 1.2f;

    [SerializeField]
    private string bossName = "SPIDER KING";

    [Tooltip("trunk radius; u")]
    [SerializeField]
    [Min(0.5f)]
    private float trunkRadiusU = 5f;

    [Tooltip("height of the arena above where the fight began; the climb is "
        + "capped there; u")]
    [SerializeField]
    [Min(4f)]
    private float arenaHeightU = 18f;

    [Tooltip("the guarding great cobra's head sits this far below the start; u")]
    [SerializeField]
    [Min(2f)]
    private float guardDepthU = 20f;

    [Tooltip("how much farther the camera pulls back in the fight, 1 = none")]
    [SerializeField]
    [Min(1f)]
    private float cameraDistanceScale = 1.6f;

    [Tooltip("radius the branch roots sit at, as the segments' branch ring; u")]
    [SerializeField]
    [Min(0.5f)]
    private float branchRadiusU = 6.6f;

    [Header("Melons")]
    [Tooltip("melon branches in the arena")]
    [SerializeField]
    [Min(1)]
    private int melonCount = 5;

    [Tooltip("seconds a grabbed melon takes to grow back")]
    [SerializeField]
    [Min(0.5f)]
    private float melonRespawnS = 7f;

    [Tooltip("flight of a thrown melon to the spider; s")]
    [SerializeField]
    [Min(0.1f)]
    private float melonFlyS = 0.55f;

    [Tooltip("health a melon takes off the spider")]
    [SerializeField]
    [Min(1)]
    private int melonDamage = 1;

    [Header("Laid Webs")]
    [Tooltip("the spider spins webs all the way as it crawls, one every this "
        + "far, a trail behind it; the arena starts w/o webs; u")]
    [SerializeField]
    [Min(0.5f)]
    private float webLayEveryU = 1.5f;

    [Tooltip("seconds a web stays on the trunk, its last second shrinking; "
        + "any number of them, each goes by itself")]
    [SerializeField]
    [Min(1f)]
    private float webLifeS = 10f;

    [Tooltip("min gap b/t two webs; u")]
    [SerializeField]
    [Min(0.3f)]
    private float webGapU = 1.4f;

    [Tooltip("a web is never laid this close to the monkey; u")]
    [SerializeField]
    [Min(0.5f)]
    private float webMonkeyGapU = 2.5f;

    [Header("Movement")]
    [Tooltip("crawl speed after the monkey; u/s")]
    [SerializeField]
    [Min(0.1f)]
    private float stalkSpeedU = 5.5f;

    [Tooltip("speed it rushes at to get in the camera's view, so its charge "
        + "can be seen; u/s")]
    [SerializeField]
    [Min(0.1f)]
    private float rushSpeedU = 9f;

    [Tooltip("it crawls after the monkey till this close; u")]
    [SerializeField]
    [Min(0.5f)]
    private float stalkDistanceU = 4.8f;

    [Tooltip("this close it dashes; u")]
    [SerializeField]
    [Min(0.5f)]
    private float dashStartU = 5f;

    [Header("Rhythm")]
    [Tooltip("seconds between attacks at full health")]
    [SerializeField]
    [Min(0.3f)]
    private float restS = 3.2f;

    [Tooltip("share of that rest left at no health: weaker, faster")]
    [SerializeField]
    [Range(0.1f, 1f)]
    private float minRestShare = 0.28f;

    [Header("Web Wave")]
    [Tooltip("the charge b4 a web wave: the spider rears up, its body swells "
        + "& a white orb gathers at its head; s")]
    [SerializeField]
    [Min(0.1f)]
    private float webShotWindUpS = 0.8f;

    [Tooltip("speed of the web wave, homing in on the monkey, so it can "
        + "only be parried, never dodged; u/s")]
    [SerializeField]
    [Min(1f)]
    private float webShotSpeedU = 24f;

    [Header("Dash")]
    [Tooltip("seconds after a dash b4 the next one")]
    [SerializeField]
    [Min(0f)]
    private float dashCooldownS = 3.2f;

    [Tooltip("wind up of the dash: the legs rear up & a red lane shows its "
        + "way on the bark; s")]
    [SerializeField]
    [Min(0.1f)]
    private float dashWindUpS = 0.9f;

    [Tooltip("the lane stops following the monkey this long b4 the dash, "
        + "the time to step off it; s")]
    [SerializeField]
    [Min(0.05f)]
    private float dashLockS = 0.45f;

    [Tooltip("speed of the dash; u/s")]
    [SerializeField]
    [Min(1f)]
    private float dashSpeedU = 22f;

    [Tooltip("the dash runs this far past where the monkey was; u")]
    [SerializeField]
    [Min(0f)]
    private float dashOvershootU = 3f;

    [Tooltip("the dash catches a monkey this close, fatal; u")]
    [SerializeField]
    [Min(0.3f)]
    private float dashHitU = 1.6f;

    [Header("Getting Harder")]
    [Tooltip("the fight gets harder in three stages as the spider's health "
        + "goes: under these shares of its health lost, stage 1, then 2, "
        + "then 3")]
    [SerializeField]
    private Vector2 stageLostShares = new Vector2(0.5f, 0.8f);

    [Tooltip("web bullets fired after one charge in stages 1, 2 & 3: in the "
        + "later ones a volley, each to bat back")]
    [SerializeField]
    private Vector3Int webShotsPerStage = new Vector3Int(1, 2, 3);

    [Tooltip("slashes in a row in stages 1, 2 & 3")]
    [SerializeField]
    private Vector3Int slashesPerStage = new Vector3Int(1, 2, 3);

    [Tooltip("the long gap b/t two bullets of a volley; s")]
    [SerializeField]
    [Min(0.05f)]
    private float volleySlowGapS = 1.1f;

    [Tooltip("the short gap b/t two bullets of a volley; s")]
    [SerializeField]
    [Min(0.05f)]
    private float volleyFastGapS = 0.7f;

    [Tooltip("at the last stage: scale on the speed of slashes, bullets, its "
        + "crawl & dash, lerped from 1 as its health goes")]
    [SerializeField]
    [Min(1f)]
    private float lateSpeedScale = 1.3f;

    [Tooltip("at the last stage: scale on its chase & circling only, lerped "
        + "from 1 as its health goes")]
    [SerializeField]
    [Min(1f)]
    private float lateChaseScale = 1.08f;

    [Tooltip("at the last stage: scale on wind ups & gaps, lerped from 1")]
    [SerializeField]
    [Range(0.3f, 1f)]
    private float lateTimeScale = 0.75f;

    [Tooltip("seconds b/t dashes at the last stage, lerped from "
        + "dashCooldownS")]
    [SerializeField]
    [Min(0f)]
    private float lateDashCooldownS = 1.5f;

    [Header("Slash")]
    [Tooltip("it never slashes from closer than this; u")]
    [SerializeField]
    [Min(0f)]
    private float slashMinRangeU = 4f;

    [Tooltip("chance an attack from afar is a slash, not a web shot")]
    [SerializeField]
    [Range(0f, 1f)]
    private float slashChance = 0.5f;

    [Tooltip("it slashes from this far at most, & a crescent flies about "
        + "this far; u")]
    [SerializeField]
    [Min(2f)]
    private float slashRangeU = 16f;

    [Tooltip("the charge b4 the slashes, seen like the web shot's: it rears up "
        + "& swells, its front legs high; s")]
    [SerializeField]
    [Min(0.1f)]
    private float slashChargeS = 0.8f;

    [Tooltip("wind up of each slash: the leg rears & the red aim line shows; s")]
    [SerializeField]
    [Min(0.05f)]
    private float slashWindUpS = 0.3f;

    [Tooltip("speed of a crescent along the bark; u/s")]
    [SerializeField]
    [Min(1f)]
    private float slashSpeedU = 26f;

    [Tooltip("width of a crescent across its way; u")]
    [SerializeField]
    [Min(0.5f)]
    private float slashWidthU = 4.5f;

    [Tooltip("the long gap after a slash b4 the next one winds up; s")]
    [SerializeField]
    [Min(0f)]
    private float slashSlowGapS = 0.75f;

    [Tooltip("the short gap after a slash b4 the next one winds up; s")]
    [SerializeField]
    [Min(0f)]
    private float slashFastGapS = 0.25f;

    [Header("Cocoon")]
    [Tooltip("crawl speed to a trapped monkey; u/s")]
    [SerializeField]
    [Min(0.1f)]
    private float cocoonSpeedU = 3f;

    [Tooltip("this close to a trapped monkey it wraps it, fatal; u")]
    [SerializeField]
    [Min(0.3f)]
    private float cocoonReachU = 1.6f;

    [Tooltip("scale on the crawl the first time the monkey is trapped")]
    [SerializeField]
    [Range(0.2f, 1f)]
    private float firstCocoonSpeedScale = 0.6f;

    [Header("Tail Parry")]
    [Tooltip("max seconds b/t the two Space taps of a parry")]
    [SerializeField]
    [Min(0.05f)]
    private float doubleTapS = 0.3f;

    [Tooltip("the tail swing's wind up: it draws back, nothing is batted yet; "
        + "real s")]
    [SerializeField]
    [Min(0f)]
    private float tailWindUpS = 0.06f;

    [Tooltip("the swing itself: only now a bullet or slash is batted back; "
        + "real s")]
    [SerializeField]
    [Min(0.05f)]
    private float tailActiveS = 0.2f;

    [Tooltip("the swing's follow through after a whiff: the tail swings back "
        + "& no new swing can start, so mashing never works, only timing; a "
        + "parry readies the tail at once; real s")]
    [SerializeField]
    [Min(0f)]
    private float tailRecoverS = 0.55f;

    [Tooltip("the wave turns gold this close to the monkey: the moment to "
        + "double tap; u")]
    [SerializeField]
    [Min(0.5f)]
    private float waveGoldU = 3.2f;

    [Tooltip("an armed parry bats the wave back this close to the monkey; u")]
    [SerializeField]
    [Min(0.3f)]
    private float parryReachU = 2f;

    [Tooltip("seconds the spider is stuck in its own batted back web")]
    [SerializeField]
    [Min(0.5f)]
    private float parryStunS = 3.5f;

    [Tooltip("real seconds of slow motion on a parry, the camera close in on "
        + "the monkey")]
    [SerializeField]
    [Min(0f)]
    private float parrySlowS = 0.75f;

    [Tooltip("time scale of that slow motion")]
    [SerializeField]
    [Range(0.05f, 1f)]
    private float parrySlowTimeScale = 0.2f;

    [Tooltip("how close the parry shot's camera comes to the monkey; u")]
    [SerializeField]
    [Min(1f)]
    private float parryCloseUpU = 4.6f;

    [Tooltip("how far the parry shot's camera swings round the monkey; deg")]
    [SerializeField]
    [Range(0f, 90f)]
    private float parryOrbitDeg = 32f;

    [Tooltip("the parry shot's tilt, a dutch angle; deg")]
    [SerializeField]
    [Range(0f, 30f)]
    private float parryRollDeg = 10f;

    [Tooltip("health a batted back slash takes off the spider")]
    [SerializeField]
    [Min(0)]
    private int slashParryDamage = 1;

    [Tooltip("health a batted back web bullet takes off the spider; 0: it "
        + "only sticks it")]
    [SerializeField]
    [Min(0)]
    private int webParryDamage = 0;

    [Header("Teaching")]
    [Tooltip("scale on wind ups in the lessons")]
    [SerializeField]
    [Min(1f)]
    private float teachSlowScale = 1.8f;

    [Tooltip("seconds b4 the next lesson attack")]
    [SerializeField]
    [Min(0.3f)]
    private float teachGapS = 1.6f;

    [Tooltip("seconds the last lesson's banner stays b4 the fight")]
    [SerializeField]
    [Min(0.5f)]
    private float finalS = 2f;

    [Tooltip("seconds of calm after a revive b4 the fight goes on")]
    [SerializeField]
    [Min(0.5f)]
    private float readyS = 3f;

    [Header("Loss & Revive")]
    [Tooltip("seconds after the armor saves the monkey that the same hit "
        + "can't count twice; no real grace: the next hit counts")]
    [SerializeField]
    [Min(0f)]
    private float saveGraceS = 0.25f;

    [Tooltip("time scale of the slow last blow")]
    [SerializeField]
    [Range(0.05f, 1f)]
    private float finisherTimeScale = 0.25f;

    [Tooltip("real seconds the slow last blow lasts")]
    [SerializeField]
    [Min(0.1f)]
    private float finisherRealS = 1.2f;

    [Tooltip("seconds the revive offer waits")]
    [SerializeField]
    [Min(1f)]
    private float reviveOfferS = 5f;

    [Tooltip("seconds of the mock ad b4 it can be skipped")]
    [SerializeField]
    [Min(0f)]
    private float adSkipAfterS = 3f;

    [Tooltip("speed of the flight back to the start; u/s")]
    [SerializeField]
    [Min(1f)]
    private float reviveReturnSpeedU = 20f;

    [Header("Win")]
    [Tooltip("seconds the beaten spider falls b4 the reward")]
    [SerializeField]
    [Min(0.2f)]
    private float defeatFallS = 2f;

    [Tooltip("web escape gain scale the silk gloves give")]
    [SerializeField]
    [Min(1f)]
    private float glovesEscapeGainScale = 2f;

    [Tooltip("how far down the great cobra restarts its chase after a win; u")]
    [SerializeField]
    [Min(0f)]
    private float winCobraDropU = 45f;

    [SerializeField]
    private string winTitle = "SPIDER KING DEFEATED!";

    [SerializeField]
    private string winLine = "You got the Silk Gloves";

    [SerializeField]
    private string winDetailLine = "Get out of webs 2x faster";

    [Header("Colors")]
    [SerializeField]
    private Color warnColor = new Color(0.95f, 0.2f, 0.15f);

    [SerializeField]
    private Color silkColor = new Color(0.92f, 0.92f, 0.95f);

    [SerializeField]
    private Color goldenColor = new Color(1f, 0.8f, 0.15f);


    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (worldBalanceConfig == null || webPrefab == null
            || bananaBranchPrefab == null) {
            Debug.LogError(
                "SpiderFightController:\tmust assign Inspector Fields: "
                    + "worldBalanceConfig, webPrefab, bananaBranchPrefab",
                this
            );
            enabled = false;
            return;
        }
        if (I != null && I != this) {
            Debug.LogWarning(
                "SpiderFightController:\tduplicate instance destroyed",
                this
            );
            Destroy(gameObject);
            return;
        }
        I = this;
        if (confirmAction == null || giveUpAction == null) {
            Debug.LogWarning(
                "SpiderFightController:\tmust assign Inspector Fields: "
                    + "confirmAction, giveUpAction",
                this
            );
        }

        body = GetComponent<SpiderKingBody>();
        hud = GetComponent<BossFightHud>();
        hud.WatchAdClicked += WatchAd;
        hud.GiveUpClicked += GiveUp;
        hud.CoursePageClicked += OpenCoursePage;
        hud.ContinueClicked += ContinueAfterReward;
        hud.SkipAdClicked += SkipAd;
        colorBlock = new MaterialPropertyBlock();
        if (Debug.isDebugBuild) {
            Debug.Log(
                "SpiderFightController:\tready, test at start " + isTestAtStart,
                this
            );
        }
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
        TreeSegmentController.BranchesGenerated -= OnBranchesGenerated;
    }

    // the actions are shared, so only switched on here, never off
    private void OnEnable() {
        confirmAction?.action.Enable();
        giveUpAction?.action.Enable();
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
    }

    private void OnDisable() {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;
        shotEndTime = 0f;
        if (
            phase == Phase.Finishing
            || phase == Phase.ReviveOffer
            || phase == Phase.WatchingAd
            || phase == Phase.Rewarding
            || isTimeBent
        ) {
            Time.timeScale = 1f;
        }
        if (monkeyController != null && IsInArena) {
            monkeyController.ClearHeightCeiling();
        }
        if (cameraFraming != null && IsInArena) {
            cameraFraming.SetBossFraming(0f, 1f);
        }
    }

    private void Update() {
        GameController game = GameController.I;
        if (game == null) {
            return;
        }
        if (phase == Phase.Waiting) {
            UpdatePreview(game);
            if (CanBegin(game)) {
                BeginFight();
            }
            return;
        }
        if (!game.IsRunLive || monkey == null
            || phase == Phase.Done || phase == Phase.Over) {
            return;
        }
        if (phase == Phase.Rewarding) {
            UpdateRewardPause(game);
            return;
        }

        hud.SpeechAnchor = body.HeadPosition;
        UpdateCamera();
        UpdateSparks();
        UpdateClashFx();
        if (phase == Phase.Fighting) {
            UpdateReturnToStart();
            UpdateMelons();
            UpdateGoldenMelon();
            UpdateSkyWeb();
            UpdateWebs();
            UpdateCrescents();
            UpdateParryInput(game);
            UpdateSpider();
            UpdateShots();
            UpdateThrows();
            UpdateTimeScale(game);
        } else if (phase == Phase.ReviveOffer || phase == Phase.WatchingAd) {
            UpdateRevive(game);
        } else if (phase == Phase.Finishing) {
            UpdateFinisher();
        } else if (phase == Phase.Defeated) {
            UpdateDefeat();
        }
    }

    // private methods  ########################################################
    // Setup  ------------------------------------------------------------------
    // after the Cobra King, never in the endless run, never mid leap
    private bool CanBegin(GameController game) {
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        if (!game.IsRunLive || game.IsLeaping || ramp == null
            || game.Mode == RunMode.Endless) {
            return false;
        }
        if (isTestAtStart) {
            return ramp.CurrentPlayerClimbedYDistance >= testStartHeightU;
        }
        BossFightController cobraKing = BossFightController.I;
        if (game.State != GameState.Playing
            || (cobraKing != null && !cobraKing.IsDone)) {
            return false;
        }
        return ramp.CurrentPlayerClimbedYDistance
            >= worldBalanceConfig.SpiderTriggerHeightU;
    }

    // from just below the spider jump height on, the Spider King already
    // waits idle above its trigger height, so the player sees the fight ahead
    private void UpdatePreview(GameController game) {
        if (isTestAtStart) {
            return;
        }
        if (body.IsBuilt) {
            body.Place(previewAngle, previewY, 0f);
            return;
        }
        if (!game.IsRunLive || game.Mode == RunMode.Endless) {
            return;
        }
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        if (ramp == null || ramp.CurrentPlayerClimbedYDistance
                < worldBalanceConfig.SpiderJumpHeightU - 5f) {
            return;
        }
        MonkeyController found = FindFirstObjectByType<MonkeyController>();
        if (found == null || found.transform.parent == null) {
            return;
        }
        trunkCenter = found.transform.parent.position;
        previewAngle = AngleOf(found.transform.position) + START_TURN_DEG;
        previewY = ramp.PlayerGameStartYPosition
            + worldBalanceConfig.SpiderTriggerHeightU + START_ABOVE_U;
        body.Build(trunkCenter, previewAngle, previewY);
    }

    private void BeginFight() {
        monkeyController = FindFirstObjectByType<MonkeyController>();
        if (monkeyController == null
            || monkeyController.transform.parent == null) {
            Debug.LogError(
                "SpiderFightController:\tno MonkeyController under a trunk "
                    + "root found",
                this
            );
            enabled = false;
            return;
        }
        monkey = monkeyController.transform;
        // a bit quicker in the arena, to dodge & reach the melons
        monkeyController.ArenaSpeedScale = monkeySpeedScale;
        GameController.I.EndStartingZone();
        trunkCenter = monkey.parent.position;
        entryY = monkey.position.y;
        entryPosition = monkey.position;
        entryRotation = monkey.rotation;
        phase = Phase.Fighting;
        FightStarted?.Invoke();

        MakeArenaBare();

        cameraFraming = FindFirstObjectByType<CameraClimbFraming>();
        greatCobra = FindFirstObjectByType<CobraMotor>();
        if (greatCobra != null) {
            greatCobra.StartGuard(entryY - guardDepthU);
        }
        // a web breaks in as many presses as in the Cobra King's arena
        struggle = FindFirstObjectByType<SpiderWebStruggle>();
        if (struggle != null) {
            BossFightController cobraArena = BossFightController.I;
            struggle.EscapeGainScale = cobraArena != null
                ? cobraArena.WebEscapeGainScale
                : 2f;
        }
        monkeyController.SetHeightCeiling(entryY + arenaHeightU);
        BuildSkyWeb();
        tail = FindChild(monkey, "Tail");
        if (tail == null) {
            Debug.LogWarning(
                "SpiderFightController:\tno Tail under the monkey, the parry "
                    + "swings nothing",
                this
            );
        }

        startAngle = body.IsBuilt
            ? previewAngle
            : AngleOf(monkey.position) + START_TURN_DEG;
        startY = entryY + START_ABOVE_U;
        spiderAngle = startAngle;
        spiderY = startY;
        maxHealth = worldBalanceConfig.SpiderMaxHealth;
        health = maxHealth;
        body.Build(trunkCenter, spiderAngle, spiderY);
        // the whole arena is there from the start, melons too
        SpawnMelons();
        ClearStickyWebs();
        ResetSpiderState();

        hud.Show(bossName);
        hud.SetHealth(1f);
        hud.Flash(warnColor);
        // only web bullets till the first parry is done, then the fight; the
        // first slash later teaches its own parry
        teach = hasTaughtWeb ? TeachStep.Done : TeachStep.Parry;
        Teach(bossName, "", warnColor, 2f);
        // the gold armor, as in the Cobra King's arena: it swirls on, no
        // words; worn already, it stays
        GiveArmor();
        hud.Say("My web, my rules!");
        if (Debug.isDebugBuild) {
            Debug.Log(
                "SpiderFightController:\tspider fight begins at y " + entryY,
                this
            );
        }
    }

    private void ResetSpiderState() {
        mode = Mode.Idle;
        modeTimeS = 0f;
        attack = Attack.None;
        attackTimeS = 0f;
        nextAttackTime = Time.time + 2f;
        stunEndTime = 0f;
        saveGraceEndTime = 0f;
        websBeforeSlash = 0;
        isDashing = false;
        ClearDashLane();
        volleyLeft = 0;
        lastStage = Stage;
        ClearSlash();
        nextDashTime = Time.time + 1f;
        parrySlowEndTime = 0f;
        tailTimeS = -1f;
        isTeachFrozen = false;
        frozenShot = null;
        frozenCrescent = null;
        ClearShots();
        ClearThrows();
        if (stuckWeb != null) {
            Destroy(stuckWeb);
            stuckWeb = null;
        }
        body.SetEyeGlow(0f);
        body.SetSwipe(0f);
        body.SetEating(false);
    }

    // Sky Web  ----------------------------------------------------------------
    // the arena's top, the climb's cap, shown as a see-through sheet round
    // the trunk: faint far off, clearer as the monkey nears it, rippling
    // where the monkey bumps it
    private void BuildSkyWeb() {
        ClearSkyWeb();
        skyDisc = CreatePrimitive(PrimitiveType.Cylinder, "AirWall",
            Vector3.one, silkColor).transform;
        Renderer discRenderer = skyDisc.GetComponent<Renderer>();
        if (airWallMaterial != null) {
            skyMaterial = new Material(airWallMaterial);
            // a plain sheet, no web picture on it
            if (skyMaterial.HasProperty(BASE_MAP)) {
                skyMaterial.SetTexture(BASE_MAP, null);
            }
            discRenderer.sharedMaterial = skyMaterial;
        }
        discRenderer.shadowCastingMode = ShadowCastingMode.Off;
        discRenderer.receiveShadows = false;
        discRenderer.enabled = false;
        // a clearer patch just round the monkey, so the far sheet stays thin
        skyPatch = CreatePrimitive(PrimitiveType.Cylinder, "AirWallPatch",
            Vector3.one, silkColor).transform;
        Renderer patchRenderer = skyPatch.GetComponent<Renderer>();
        if (skyMaterial != null) {
            patchRenderer.sharedMaterial = skyMaterial;
        }
        patchRenderer.shadowCastingMode = ShadowCastingMode.Off;
        patchRenderer.receiveShadows = false;
        patchRenderer.enabled = false;
    }

    private void ClearSkyWeb() {
        if (skyDisc != null) {
            Destroy(skyDisc.gameObject);
        }
        skyDisc = null;
        if (skyPatch != null) {
            Destroy(skyPatch.gameObject);
        }
        skyPatch = null;
        if (skyMaterial != null) {
            Destroy(skyMaterial);
        }
        skyMaterial = null;
    }

    private void UpdateSkyWeb() {
        if (skyDisc == null || monkey == null) {
            return;
        }
        float ceilingY = entryY + arenaHeightU;
        float wallY = ceilingY + 0.7f;
        float gap = ceilingY - monkey.position.y;
        float near = Mathf.Clamp01(1f - gap / SKY_SHOW_U);
        near = near * near * (3f - 2f * near);
        Renderer discRenderer = skyDisc.GetComponent<Renderer>();
        Renderer patchRenderer = skyPatch != null
            ? skyPatch.GetComponent<Renderer>() : null;
        // unseen till the monkey nears it, & never once the camera is up at
        // it, where it would veil the whole view
        Camera view = Camera.main;
        bool isUnderView = view == null
            || view.transform.position.y < wallY - 0.5f;
        bool isShown = near > 0.01f && isUnderView;
        discRenderer.enabled = isShown;
        if (patchRenderer != null) {
            patchRenderer.enabled = isShown;
        }
        // a sheet out to the horizon, as if it never ends
        skyDisc.SetPositionAndRotation(
            new Vector3(trunkCenter.x, wallY, trunkCenter.z),
            Quaternion.identity);
        skyDisc.localScale = new Vector3(SKY_WIDE_U, 0.02f, SKY_WIDE_U);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
        // the far sheet barely there, the patch round the monkey clearer
        float shimmer = 0.85f + 0.15f * pulse;
        SetColor(discRenderer,
            new Color(0.75f, 0.9f, 1f, near * 0.05f * shimmer));
        if (skyPatch != null) {
            float patchU = (HorizontalDistance(monkey.position) + 3f) * 2f;
            skyPatch.SetPositionAndRotation(
                new Vector3(trunkCenter.x, wallY + 0.02f, trunkCenter.z),
                Quaternion.identity);
            skyPatch.localScale = new Vector3(patchU, 0.02f, patchU);
            SetColor(patchRenderer,
                new Color(0.75f, 0.9f, 1f, near * 0.12f * shimmer));
        }
        // a bump: a ripple where the monkey's head meets it
        bool isAtCap = gap <= 0.35f;
        if (isAtCap && Time.time >= nextSkyRippleTime) {
            nextSkyRippleTime = Time.time + 0.45f;
            Vector3 at = PointOnTrunk(AngleOf(monkey.position), wallY,
                HorizontalDistance(monkey.position));
            AddRing(at, Color.white, 0f);
        }
    }

    // Bare Arena  -------------------------------------------------------------
    // the arena is a space of its own, cut off from the climb: the climb's
    // generators skip it (also in the test fight near the start) & what
    // they grew there b4 goes: branches w/ their fruit, webs, falling
    // cobras, rough bark
    private void MakeArenaBare() {
        // down past the guard: the whole boss area grows only the fight's
        bareBottomY = entryY - guardDepthU - 4f;
        bareTopY = entryY + arenaHeightU + 6f;
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        if (ramp != null) {
            ramp.SetExtraBossZone(bareBottomY, bareTopY);
        }
        foreach (BranchController branch in
            FindObjectsByType<BranchController>(FindObjectsSortMode.None)) {
            if (!branch.transform.IsChildOf(transform)) {
                RemoveIfInArena(branch.gameObject);
            }
        }
        foreach (SpiderWebTrap web in
            FindObjectsByType<SpiderWebTrap>(FindObjectsSortMode.None)) {
            if (!web.transform.IsChildOf(transform)) {
                RemoveIfInArena(web.gameObject);
            }
        }
        foreach (FallingCobraController cobra in
            FindObjectsByType<FallingCobraController>(FindObjectsSortMode.None)) {
            RemoveIfInArena(cobra.gameObject);
        }
        foreach (RoughBarkZone bark in
            FindObjectsByType<RoughBarkZone>(FindObjectsSortMode.None)) {
            RemoveIfInArena(bark.gameObject);
        }
        TreeSegmentController.BranchesGenerated += OnBranchesGenerated;
    }

    private void RemoveIfInArena(GameObject thing) {
        float y = thing.transform.position.y;
        if (y < bareBottomY || y > bareTopY) {
            return;
        }
        ForgetFruits(thing);
        // inactive first, Destroy only lands at the end of the frame
        thing.SetActive(false);
        Destroy(thing);
    }

    // a segment recycled into the arena grows nothing there
    private void OnBranchesGenerated(TreeSegmentController segment) {
        if (!IsInArena || segment == null) {
            return;
        }
        foreach (BranchController branch in
            segment.GetComponentsInChildren<BranchController>(true)) {
            RemoveIfInArena(branch.gameObject);
        }
    }

    private void EndBareArena() {
        TreeSegmentController.BranchesGenerated -= OnBranchesGenerated;
        if (monkeyController != null) {
            monkeyController.ArenaSpeedScale = 1f;
        }
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        if (ramp != null) {
            ramp.ClearExtraBossZone();
        }
    }

    // Melons & Webs  ---------------------------------------------------------
    // melon branches spread round the arena at a few heights
    private void SpawnMelons() {
        ClearMelons();
        float baseAngle = AngleOf(monkey.position);
        for (int i = 0; i < melonCount; i++) {
            float angle = baseAngle + 25f + i * 360f / melonCount
                + Random.Range(-15f, 15f);
            float y = entryY + Mathf.Lerp(1.5f, arenaHeightU - 3f,
                melonCount > 1 ? (i * 2 % melonCount) / (melonCount - 1f) : 0.5f);
            Melon melon = AddMelon(angle, y);
            melons.Add(melon);
            // no web right on a melon
            foreach (LaidWeb laid in stickyWebs) {
                if (laid.web != null && !IsWebHoldingMonkey(laid.web)
                    && Vector3.Distance(laid.web.transform.position,
                        melon.root.transform.position) < 2.5f) {
                    Destroy(laid.web.gameObject);
                }
            }
        }
        stickyWebs.RemoveAll(w => w.web == null);
    }

    // the climb's banana branch, its banana swapped for a melon model; the
    // collider & scripts stay, so the grab works as on any fruit
    private Melon AddMelon(float angle, float y) {
        GameObject branch = Instantiate(bananaBranchPrefab, transform);
        branch.transform.SetPositionAndRotation(
            PointOnTrunk(angle, y + BANANA_HANG_U, branchRadiusU),
            Quaternion.Euler(0f, 90f - angle, 0f)
        );
        BranchController controller = branch.GetComponent<BranchController>();
        if (controller != null) {
            controller.Setup(); // the banana branch prefab bears a banana
        }
        ScorePickup pickup = branch.GetComponentInChildren<ScorePickup>(true);
        GameObject fruit = pickup != null ? pickup.gameObject : branch;
        if (watermelonMesh != null) {
            foreach (MeshFilter filter in
                fruit.GetComponentsInChildren<MeshFilter>(true)) {
                filter.sharedMesh = watermelonMesh;
                filter.transform.localRotation = Quaternion.identity;
                filter.transform.localScale = watermelonScale;
                filter.transform.localPosition = watermelonOffset;
            }
        }
        Collider trigger = fruit.GetComponent<Collider>();
        if (trigger == null) {
            trigger = fruit.GetComponentInChildren<Collider>(true);
        }
        return new Melon {
            root = fruit,
            branch = branch,
            trigger = trigger,
            backTime = -1f,
        };
    }

    private void ClearMelons() {
        foreach (Melon melon in melons) {
            if (melon.branch != null) {
                ForgetFruits(melon.branch);
                Destroy(melon.branch);
            }
        }
        melons.Clear();
        if (goldenMelon != null && goldenMelon.branch != null) {
            ForgetFruits(goldenMelon.branch);
            Destroy(goldenMelon.branch);
        }
        goldenMelon = null;
        if (goldenTwin != null && goldenTwin.branch != null) {
            ForgetFruits(goldenTwin.branch);
            Destroy(goldenTwin.branch);
        }
        goldenTwin = null;
    }

    // the golden melon grows beside the monkey now & then while it has no
    // armor; a pulsing gold melon, gone again after goldenShowS
    private void UpdateGoldenMelon() {
        if (melons.Count == 0 || teach != TeachStep.Done) {
            // none hangs about in a lesson or the mode choice
            if (goldenMelon != null && goldenMelon.branch != null
                && goldenMelon.branch.activeSelf) {
                HideGoldenMelon();
            }
            return;
        }
        if (goldenMelon == null) {
            goldenMelon = AddMelon(0f, entryY);
            goldenMelon.isGolden = true;
            MakeGolden(goldenMelon.root);
            // two at a time, one either side, so one is always easy to reach
            goldenTwin = AddMelon(0f, entryY);
            goldenTwin.isGolden = true;
            MakeGolden(goldenTwin.root);
            HideGoldenMelon();
            // an armor lost b4 it was made keeps its "at once"
            if (HasArmor()) {
                nextGoldenTime = Time.time + GOLDEN_FIRST_S;
            }
        }
        if (goldenMelon.root == null || goldenTwin == null
            || goldenTwin.root == null) {
            return;
        }
        // shown is its branch out, the melon still on it
        if (goldenMelon.branch.activeSelf) {
            if (Time.time >= goldenHideTime) {
                // missed: the next after a short wait the first time, then
                // the usual interval
                HideGoldenMelon();
                goldenSinceArmor++;
                nextGoldenTime = Time.time + (goldenSinceArmor == 1
                    ? GOLDEN_AFTER_ARMOR_S : goldenIntervalS);
            }
            return;
        }
        // not while the monkey is thrown off: it shows once it lands
        if (Time.time < nextGoldenTime || HasArmor() || isReturningToStart) {
            return;
        }
        // beside the monkey, a bit up, in reach, as the Cobra King's
        float side = Random.value < 0.5f ? -1f : 1f;
        float angle = AngleOf(monkey.position) + side * 30f;
        float y = Mathf.Clamp(monkey.position.y + 3f, entryY + 1f,
            entryY + arenaHeightU - 2f);
        goldenMelon.branch.transform.SetPositionAndRotation(
            PointOnTrunk(angle, y + BANANA_HANG_U, branchRadiusU),
            Quaternion.Euler(0f, 90f - angle, 0f)
        );
        goldenMelon.branch.SetActive(true);
        goldenMelon.root.SetActive(true);
        // its twin on the other side of the monkey, a little lower
        float twinAngle = AngleOf(monkey.position) - side * 30f;
        float twinY = Mathf.Clamp(monkey.position.y + 1.5f, entryY + 1f,
            entryY + arenaHeightU - 2f);
        goldenTwin.branch.transform.SetPositionAndRotation(
            PointOnTrunk(twinAngle, twinY + BANANA_HANG_U, branchRadiusU),
            Quaternion.Euler(0f, 90f - twinAngle, 0f)
        );
        goldenTwin.branch.SetActive(true);
        goldenTwin.root.SetActive(true);
        Burst(goldenTwin.root.transform.position, goldenColor, 10);
        goldenHideTime = Time.time + goldenShowS;
        Burst(goldenMelon.root.transform.position, goldenColor, 10);
        hud.PopText("Golden watermelon!", goldenMelon.root.transform.position,
            goldenColor);
        hud.ShowBannerFor("Golden watermelon!", "Grab it for a Golden Armor",
            goldenColor, 3f);
    }

    // as the Cobra King's golden watermelon: its reach outline swapped for a
    // thick gold one, the melon tinted gold
    private void MakeGolden(GameObject melon) {
        foreach (FruitOutlineListener listener in
            melon.GetComponentsInChildren<FruitOutlineListener>(true)) {
            Destroy(listener);
        }
        Material outline = null;
        if (goldenOutlineMaterial != null) {
            outline = new Material(goldenOutlineMaterial);
            outline.SetColor(OUTLINE_COLOR, goldenOutlineColor);
            outline.SetFloat(OUTLINE_WIDTH,
                goldenOutlineMaterial.GetFloat(OUTLINE_WIDTH)
                    * goldenOutlineWidthScale);
        }
        foreach (Renderer part in melon.GetComponentsInChildren<Renderer>(true)) {
            Material baseMaterial = part.sharedMaterial;
            if (outline != null) {
                part.sharedMaterials = new[] { baseMaterial, outline };
            }
            SetColor(part, goldenColor);
        }
    }

    private void HideGoldenMelon() {
        if (goldenMelon == null || goldenMelon.branch == null) {
            return;
        }
        if (goldenRestScale == Vector3.zero) {
            goldenRestScale = goldenMelon.root.transform.localScale;
        }
        goldenMelon.root.transform.localScale = goldenRestScale;
        ForgetFruits(goldenMelon.branch);
        goldenMelon.branch.SetActive(false);
        // both go together: one grabbed, the other is gone too
        if (goldenTwin != null && goldenTwin.branch != null) {
            goldenTwin.root.transform.localScale = goldenRestScale;
            ForgetFruits(goldenTwin.branch);
            goldenTwin.branch.SetActive(false);
        }
    }

    private void UpdateMelons() {
        foreach (Melon melon in melons) {
            if (melon.root == null || melon.root.activeSelf
                || melon.backTime < 0f || Time.time < melon.backTime) {
                continue;
            }
            melon.root.SetActive(true);
            melon.backTime = -1f;
        }
    }

    // the arena starts full of sticky webs, kept off the melons & the
    // monkey's start spot, never overlapping
    // the spider spins all the way it crawls: a web every webLayEveryU
    // where it is, off the monkey, the melons & other webs; none in a dash
    private void LayWebs(float movedU) {
        if (teach != TeachStep.Done || isDashing) {
            return;
        }
        layDistanceU += movedU;
        if (layDistanceU < webLayEveryU) {
            return;
        }
        Vector3 spot = PointOnTrunk(spiderAngle, spiderY, trunkRadiusU + 0.02f);
        if (IsWebSpotFree(spot)) {
            layDistanceU = 0f;
            PlaceWeb(spiderAngle, spiderY);
        }
    }

    // each web goes by itself after webLifeS, shrinking its last second;
    // never the one holding the monkey
    private void UpdateWebs() {
        for (int i = stickyWebs.Count - 1; i >= 0; i--) {
            LaidWeb laid = stickyWebs[i];
            if (laid.web == null) {
                stickyWebs.RemoveAt(i);
                continue;
            }
            if (IsWebHoldingMonkey(laid.web)) {
                continue;
            }
            float leftS = laid.endTime - Time.time;
            if (leftS <= 0f) {
                laid.web.gameObject.SetActive(false);
                Destroy(laid.web.gameObject);
                stickyWebs.RemoveAt(i);
                continue;
            }
            if (leftS < 1f) {
                laid.web.transform.localScale = laid.restScale
                    * Mathf.Lerp(0.2f, 1f, leftS);
            }
        }
    }

    private bool IsWebSpotFree(Vector3 spot) {
        if (Vector3.Distance(spot, monkey.position) < webMonkeyGapU) {
            return false;
        }
        foreach (LaidWeb laid in stickyWebs) {
            if (laid.web != null
                && Vector3.Distance(laid.web.transform.position, spot) < webGapU) {
                return false;
            }
        }
        foreach (Melon melon in melons) {
            if (melon.root != null
                && Vector3.Distance(melon.root.transform.position, spot) < 2.5f) {
                return false;
            }
        }
        return true;
    }

    // as the segments' spawner: the web's face looks away from the trunk
    private void PlaceWeb(float angle, float y) {
        SpiderWebTrap web = Instantiate(
            webPrefab,
            PointOnTrunk(angle, y, trunkRadiusU + 0.02f),
            Quaternion.Euler(0f, -angle, 0f),
            transform
        );
        stickyWebs.Add(new LaidWeb {
            web = web,
            endTime = Time.time + webLifeS,
            restScale = web.transform.localScale,
        });
    }

    private bool IsWebHoldingMonkey(SpiderWebTrap web) {
        return struggle != null && struggle.IsTrapped && monkey != null
            && Vector3.Distance(web.transform.position, monkey.position) < 2f;
    }

    private void ClearStickyWebs() {
        foreach (LaidWeb laid in stickyWebs) {
            if (laid.web != null) {
                Destroy(laid.web.gameObject);
            }
        }
        stickyWebs.Clear();
    }

    // Mode Choice  ------------------------------------------------------------
    // the two choice melons beside where the fight began; the rest hidden
    private void SpawnChoices() {
        ClearChoices();
        foreach (Melon melon in melons) {
            if (melon.branch != null) {
                ForgetFruits(melon.branch);
                melon.branch.SetActive(false);
            }
        }
        float angle = AngleOf(entryPosition);
        float y = entryY + choiceAboveU;
        choiceEasy = AddMelon(angle - choiceSideDeg, y);
        choiceNormal = AddMelon(angle + choiceSideDeg, y);
        MakeGolden(choiceNormal.root);
    }

    private void ClearChoices() {
        foreach (Melon choice in new[] { choiceEasy, choiceNormal }) {
            if (choice != null && choice.branch != null) {
                ForgetFruits(choice.branch);
                Destroy(choice.branch);
            }
        }
        choiceEasy = null;
        choiceNormal = null;
        hud.SetChoiceTags(false, Vector3.zero, Vector3.zero);
    }

    private void UpdateChoiceTags() {
        if (teach != TeachStep.Choose || choiceEasy == null
            || choiceNormal == null) {
            return;
        }
        hud.SetChoiceTags(true, choiceEasy.root.transform.position,
            choiceNormal.root.transform.position);
    }

    // easy: less health, slower & gentler, never past the 2nd stage
    private void ChooseMode(bool isEasyPick) {
        isEasy = isEasyPick;
        ClearChoices();
        foreach (Melon melon in melons) {
            if (melon.branch != null) {
                melon.branch.SetActive(true);
                melon.root.SetActive(true);
                melon.backTime = -1f;
            }
        }
        maxHealth = isEasy
            ? Mathf.Max(1, Mathf.RoundToInt(worldBalanceConfig.SpiderMaxHealth
                * easyHealthScale))
            : worldBalanceConfig.SpiderMaxHealth;
        health = maxHealth;
        hud.SetHealth(1f);
        lastStage = 0;
        // the parry lesson again if it was never passed
        teach = hasTaughtWeb ? TeachStep.Ready : TeachStep.Parry;
        teachTimeS = 0f;
        Teach(isEasy ? "EASY" : "NORMAL", "", isEasy
            ? new Color(0.55f, 0.95f, 0.55f)
            : goldenColor, readyS);
    }

    // Throws  -----------------------------------------------------------------
    // a copy of the grabbed fruit arcs round the trunk to the spider,
    // spinning; on arrival it hits
    private void LaunchMelon(GameObject source, Vector3 from) {
        GameObject copy = Instantiate(source);
        copy.transform.localScale = source.transform.lossyScale;
        MakeDecor(copy);
        copy.SetActive(true);
        copy.transform.position = from;
        throws.Add(new Throw {
            transform = copy.transform,
            from = from,
        });
    }

    private void UpdateThrows() {
        for (int i = throws.Count - 1; i >= 0; i--) {
            Throw thrown = throws[i];
            if (thrown.transform == null) {
                throws.RemoveAt(i);
                continue;
            }
            thrown.ageS += Time.deltaTime;
            float t = Mathf.Clamp01(thrown.ageS / melonFlyS);
            thrown.transform.position = ArcPoint(thrown.from, body.HeadPosition,
                t, 2.5f);
            thrown.transform.Rotate(Vector3.right, 720f * Time.deltaTime,
                Space.World);
            if (t < 1f) {
                continue;
            }
            Destroy(thrown.transform.gameObject);
            throws.RemoveAt(i);
            OnMelonHit();
        }
    }

    private void ClearThrows() {
        foreach (Throw thrown in throws) {
            if (thrown.transform != null) {
                Destroy(thrown.transform.gameObject);
            }
        }
        throws.Clear();
    }

    private void OnMelonHit() {
        Hurt(melonDamage);
    }

    private static void GiveArmor() {
        BossFightController cobraKing = BossFightController.I;
        if (cobraKing != null) {
            cobraKing.GiveArmor();
        }
    }

    private static bool HasArmor() {
        BossFightController cobraKing = BossFightController.I;
        return cobraKing != null && cobraKing.HasArmor;
    }

    // a melon or a batted back slash
    private void Hurt(int damage) {
        if (phase != Phase.Fighting || damage <= 0) {
            return;
        }
        health = Mathf.Max(0, health - damage);
        hud.SetHealth(health / (float)maxHealth);
        hud.PopDamage("-" + damage, body.HeadPosition);
        Burst(body.HeadPosition, new Color(0.9f, 0.2f, 0.25f), 10);
        if (health <= 0) {
            StartFinisher(true);
            return;
        }
        int stage = Stage;
        if (stage > lastStage) {
            // a new stage: a roar of red rings, its eyes blazing
            lastStage = stage;
            // a stage of volleys opens w/ at least two of them, no slash
            // till then
            int volley = PerStage(webShotsPerStage, stage);
            if (volley >= 2) {
                websBeforeSlash = VOLLEYS_FIRST;
            }
            AddRing(body.HeadPosition, warnColor, 0f);
            AddRing(body.HeadPosition, warnColor, 0.1f);
            AddRing(body.HeadPosition, warnColor, 0.2f);
            // its volleys grow: called out once, from its head up to under
            // the health bar
            if (volley >= 2) {
                hud.CallOut(volley >= 3 ? "Triple fire ready!"
                    : "Rapid fire ready!", body.HeadPosition, warnColor);
                HoldForCallOut();
            } else {
                hud.Say(stage == 1 ? "You'll pay for that!" : "ENOUGH!");
            }
            BurstStreaks(body.HeadPosition, warnColor, 18, 4f, 10f);
            return;
        }
        hud.Say(health <= maxHealth / 3 ? "No! Stop it!" : "Ouch!");
    }

    // while it calls out it starts no new attack, only follows the monkey;
    // what it had begun is played out
    private void HoldForCallOut() {
        callOutEndTime = Time.unscaledTime + BossFightHud.CALL_OUT_S;
    }

    private bool IsCallingOut => Time.unscaledTime < callOutEndTime;

    // a point on an arc round the trunk, never through it
    private Vector3 ArcPoint(Vector3 from, Vector3 to, float t, float bulgeU) {
        float angleFrom = AngleOf(from);
        float angle = angleFrom + Mathf.DeltaAngle(angleFrom, AngleOf(to)) * t;
        float y = Mathf.Lerp(from.y, to.y, t);
        float radius = Mathf.Lerp(HorizontalDistance(from), HorizontalDistance(to),
            t) + Mathf.Sin(t * Mathf.PI) * bulgeU;
        return PointOnTrunk(angle, y, Mathf.Max(radius, trunkRadiusU + 0.3f));
    }

    // Spider  -----------------------------------------------------------------
    private void SetMode(Mode next) {
        mode = next;
        modeTimeS = 0f;
    }

    private void UpdateSpider() {
        modeTimeS += Time.deltaTime;
        float walk = 0f;
        bool isTrapped = struggle != null && struggle.IsTrapped;
        bool isStunned = Time.time < stunEndTime;
        UpdateStuckWeb(isStunned);

        // a trapped monkey is crawled to & wrapped up
        bool isDashCommitted = isDashing || attack == Attack.Dash;
        if (isTrapped && !isStunned && teach == TeachStep.Done
            && mode != Mode.Cocooning && !isDashCommitted && !isSlashing) {
            CancelAttack();
            SetMode(Mode.Cocooning);
            hud.Say("Hold still...");
        }

        float lastAngle = spiderAngle;
        float lastY = spiderY;
        if (isSlashing) {
            walk = UpdateSlash();
        } else if (isDashing) {
            walk = UpdateDash();
        } else if (mode == Mode.Cocooning) {
            walk = UpdateCocoon(isTrapped, isStunned);
        } else {
            walk = UpdateIdle(isStunned);
        }

        LayWebs(SurfaceDistance(lastAngle, lastY, spiderAngle, spiderY));

        // touching it is harmless; only its attacks hurt

        float glow = mode == Mode.Cocooning || isDashing || isSlashing
            ? 1f
            : AttackGlow();
        body.SetEyeGlow(isStunned ? 0f : glow);
        if (isSlashing && slashChargeLeftS > 0f && !isStunned) {
            body.SetEyeGlow(0.7f + 0.3f * Mathf.Sin(Time.time * 30f),
                warnColor);
        }
        if (Time.unscaledTime < biteEndTime) {
            float t = 1f - (biteEndTime - Time.unscaledTime) / BITE_S;
            body.SetSwipe(Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f);
            body.SetEyeGlow(1f);
        }
        body.Place(spiderAngle, spiderY, walk);
    }

    private float UpdateIdle(bool isStunned) {
        if (isStunned) {
            // stuck, it still fires out the volley it began
            if (teach == TeachStep.Done && !isReturningToStart) {
                UpdateVolley();
            }
            return 0f;
        }
        if (attack != Attack.None) {
            UpdateAttack();
            return 0f;
        }
        if (teach != TeachStep.Done) {
            return UpdateTeach();
        }
        if (isReturningToStart) {
            return 0f;
        }
        UpdateVolley();
        bool isBusy = HasLiveShots || IsCallingOut
            || (struggle != null && struggle.IsTrapped);
        // its charge must be seen: out of view it rushes at the monkey
        bool isDue = Time.time >= nextAttackTime && !isBusy;
        bool isRushing = isDue && !IsInView();
        float walk = isRushing ? Rush() : Stalk();
        // caught up: it always dashes at once, to be dodged (not while it
        // calls out)
        if (!isBusy && IsCloseEnoughToDash()) {
            BeginAttack(Attack.Dash, false);
            return 0f;
        }
        if (isDue && !isRushing) {
            StartAttack();
        }
        return walk;
    }

    private float Rush() {
        Vector3 at = monkey.position;
        bool hasArrived = MoveToward(AngleOf(at), at.y, rushSpeedU,
            stalkDistanceU);
        return hasArrived ? 0f : 1f;
    }

    // if the spider, its charge, is on screen & on the trunk's near side
    private bool IsInView() {
        Camera view = Camera.main;
        if (view == null) {
            return true;
        }
        Vector3 head = body.HeadPosition;
        Vector3 port = view.WorldToViewportPoint(head);
        bool isOnScreen = port.z > 0f
            && port.x > VIEW_EDGE && port.x < 1f - VIEW_EDGE
            && port.y > VIEW_EDGE && port.y < 1f - VIEW_EDGE;
        Vector3 toCamera = view.transform.position - trunkCenter;
        toCamera.y = 0f;
        float radians = spiderAngle * Mathf.Deg2Rad;
        Vector3 outward = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        bool isNearSide = toCamera.sqrMagnitude < 0.001f
            || Vector3.Dot(outward, toCamera.normalized) > 0.45f;
        return isOnScreen && isNearSide;
    }

    // crawls after the monkey round the arena
    private float Stalk() {
        Vector3 at = monkey.position;
        bool hasArrived = MoveToward(AngleOf(at), at.y, stalkSpeedU * ChaseScale,
            stalkDistanceU);
        if (!hasArrived) {
            return 0.6f;
        }
        Prowl(at);
        return 0.5f;
    }

    // never standing still b/t attacks: it circles the monkey at its
    // distance, now one way, now the other
    private void Prowl(Vector3 at) {
        if (Time.time >= nextProwlFlipTime) {
            prowlSign = -prowlSign;
            nextProwlFlipTime = Time.time + Random.Range(1.2f, 2.6f);
        }
        float arcU = Mathf.DeltaAngle(spiderAngle, AngleOf(at))
            * Mathf.Deg2Rad * trunkRadiusU;
        float dy = at.y - spiderY;
        float distance = Mathf.Sqrt(arcU * arcU + dy * dy);
        if (distance < 0.01f) {
            return;
        }
        // across the line to the monkey, & a pull back onto its distance
        float toU = arcU / distance;
        float toV = dy / distance;
        float speedU = stalkSpeedU * ChaseScale * PROWL_SHARE;
        float pull = Mathf.Clamp(distance - stalkDistanceU, -1f, 1f) * 2f;
        float stepU = (-toV * prowlSign * speedU + toU * pull) * Time.deltaTime;
        float stepV = (toU * prowlSign * speedU + toV * pull) * Time.deltaTime;
        float nextY = spiderY + stepV;
        // turned back at the arena's floor & top
        if (nextY < ArenaBottomY || nextY > entryY + arenaHeightU) {
            prowlSign = -prowlSign;
            return;
        }
        spiderAngle += stepU / trunkRadiusU * Mathf.Rad2Deg;
        spiderY = nextY;
    }

    private float UpdateCocoon(bool isTrapped, bool isStunned) {
        if (!isTrapped) {
            hasBeenTrapped = true;
            SetMode(Mode.Idle);
            nextAttackTime = Time.time + RestNow() * 0.6f;
            return 0f;
        }
        if (isStunned) {
            return 0f;
        }
        Vector3 at = monkey.position;
        float speedU = hasBeenTrapped
            ? cocoonSpeedU
            : cocoonSpeedU * firstCocoonSpeedScale;
        bool hasArrived = MoveToward(AngleOf(at), at.y, speedU, cocoonReachU);
        if (hasArrived) {
            LethalHit("Wrapped!");
            return 0f;
        }
        return 1f;
    }

    // moves along the bark, the shortest way round; true once within reach
    private bool MoveToward(
        float targetAngle,
        float targetY,
        float speedU,
        float reachU
    ) {
        targetY = Mathf.Clamp(targetY, ArenaBottomY, entryY + arenaHeightU);
        float arcU = Mathf.DeltaAngle(spiderAngle, targetAngle)
            * Mathf.Deg2Rad * trunkRadiusU;
        float dy = targetY - spiderY;
        float distance = Mathf.Sqrt(arcU * arcU + dy * dy);
        if (distance <= reachU) {
            return true;
        }
        float step = Mathf.Min(speedU * Time.deltaTime, distance - reachU);
        spiderAngle += arcU / distance * step / trunkRadiusU * Mathf.Rad2Deg;
        spiderY += dy / distance * step;
        return false;
    }

    // Attacks  ----------------------------------------------------------------
    // the weaker, the shorter the rests
    private float RestNow() {
        return restS * Mathf.Lerp(minRestShare, 1f, health / (float)maxHealth)
            * (isEasy ? easyTimeScale : 1f);
    }

    // share of its health lost, 0~1: the fight gets harder w/ it
    private float Rage => maxHealth > 0 ? 1f - health / (float)maxHealth : 0f;

    // 0, 1 or 2: the three stages of the fight
    private int Stage => Mathf.Min(isEasy ? 1 : 2,
        Rage < stageLostShares.x ? 0 : Rage < stageLostShares.y ? 1 : 2);

    // its chase's speed scale, 1 at full health up to lateChaseScale at none
    private float ChaseScale => teach == TeachStep.Done
        ? Mathf.Lerp(1f, lateChaseScale, Rage) * (isEasy ? easySpeedScale : 1f)
        : 1f;

    // speed scale, 1 at full health up to lateSpeedScale at none
    private float SpeedScale => teach == TeachStep.Done
        ? Mathf.Lerp(1f, lateSpeedScale, Rage) * (isEasy ? easySpeedScale : 1f)
        : 1f;

    // wind up & gap scale, 1 at full health down to lateTimeScale at none
    private float TimeScaleNow => teach == TeachStep.Done
        ? Mathf.Lerp(1f, lateTimeScale, Rage) * (isEasy ? easyTimeScale : 1f)
        : 1f;

    private static int PerStage(Vector3Int values, int stage) =>
        Mathf.Max(1, stage == 0 ? values.x : stage == 1 ? values.y : values.z);

    private float DistanceToMonkey() {
        Vector3 at = monkey.position;
        return SurfaceDistance(spiderAngle, spiderY, AngleOf(at), at.y);
    }

    private bool HasLiveShots => shots.Exists(s => !s.isCharging)
        || volleyLeft > 0;

    private float VolleyGap() {
        bool isSlow = (volleyGapIndex % 2 == 0) == isVolleySlowFirst;
        volleyGapIndex++;
        // never shorter than a parry & a new double tap take
        return Mathf.Max(MIN_VOLLEY_GAP_S,
            (isSlow ? volleySlowGapS : volleyFastGapS) * TimeScaleNow);
    }

    // the next bullet of a volley, its orb flashing at the head
    private void UpdateVolley() {
        if (volleyCharge != null && !shots.Contains(volleyCharge)) {
            volleyCharge = null;
        }
        if (volleyLeft <= 0) {
            return;
        }
        // the gap is its new charge: the orb swells at the head again
        if (volleyCharge == null) {
            volleyCharge = NewShot(ShotKind.Web, monkey.position);
            volleyChargeFromTime = Time.time;
        }
        float chargeS = Mathf.Max(0.05f, nextVolleyTime - volleyChargeFromTime);
        float progress = Mathf.Clamp01((Time.time - volleyChargeFromTime)
            / chargeS);
        volleyCharge.target = monkey.position;
        volleyCharge.fill = progress;
        body.SetCharge(progress);
        UpdateChargeSparks();
        if (Time.time < nextVolleyTime) {
            return;
        }
        volleyLeft--;
        nextVolleyTime = Time.time + VolleyGap();
        Shot bullet = volleyCharge;
        volleyCharge = null;
        body.SetCharge(0f);
        bullet.isCharging = false;
        bullet.from = body.HeadPosition;
        bullet.length = PathLength(bullet);
        if (volleyLeft == 0) {
            nextAttackTime = Mathf.Max(Time.time + RestNow(),
                stunEndTime + 0.5f);
        }
    }

    private bool IsCloseEnoughToDash() {
        return Time.time >= nextDashTime
            && DistanceToMonkey() <= dashStartU;
    }

    // from afar: a web shot or a slash
    private void StartAttack() {
        if (struggle != null && struggle.IsTrapped) {
            return;
        }
        float distance = DistanceToMonkey();
        bool canSlash = distance <= slashRangeU && distance >= slashMinRangeU;
        // far off a slash is likelier, it is what reaches
        float chance = distance > slashMinRangeU * 2f
            ? Mathf.Max(slashChance, 0.75f)
            : slashChance;
        // a stage of volleys opens w/ a couple of them b4 any slash
        if (canSlash && websBeforeSlash <= 0 && Random.value < chance) {
            StartSlash();
            return;
        }
        BeginAttack(Attack.Web, false);
    }

    // the wind up: the eyes glow; a web shot charges its orb, a dash rears
    // the legs up & shows its lane
    private void BeginAttack(Attack kind, bool isTeaching) {
        attack = kind;
        attackTimeS = 0f;
        float slow = isTeaching ? teachSlowScale : 1f;
        aimPoint = monkey.position;
        switch (kind) {
            case Attack.Web:
                attackWindUpS = webShotWindUpS * slow * TimeScaleNow;
                chargingShot = NewShot(ShotKind.Web, aimPoint);
                break;
            default:
                attackWindUpS = dashWindUpS * slow * TimeScaleNow;
                warningId = hud.AddWarning(aimPoint, "DASH!");
                break;
        }
        if (isTeaching) {
            ShowStepBanner();
        }
    }

    private float AttackGlow() {
        return attack == Attack.None
            ? 0f
            : Mathf.Clamp01(attackTimeS / attackWindUpS);
    }

    private void UpdateAttack() {
        attackTimeS += Time.deltaTime;
        float progress = Mathf.Clamp01(attackTimeS / attackWindUpS);
        aimPoint = monkey.position;
        if (chargingShot != null) {
            chargingShot.target = aimPoint;
            chargingShot.fill = progress;
        }
        if (warningId >= 0) {
            hud.UpdateWarning(warningId, monkey.position, progress);
        }
        if (attack == Attack.Dash) {
            body.SetSwipe(progress);
            bool isDashLocked = attackTimeS >= attackWindUpS - dashLockS;
            if (!isDashLocked) {
                AimDash();
            }
            DrawDashLane(isDashLocked);
        }
        if (attack == Attack.Web) {
            body.SetCharge(progress);
            UpdateChargeSparks();
        }
        if (attackTimeS < attackWindUpS) {
            return;
        }

        Attack fired = attack;
        Shot shot = chargingShot;
        chargingShot = null;
        CancelAttack(fired == Attack.Dash);
        if (fired != Attack.Dash) {
            nextAttackTime = Time.time
                + (teach == TeachStep.Done ? RestNow() : teachGapS);
        }
        switch (fired) {
            case Attack.Web:
                if (shot != null) {
                    shot.isCharging = false;
                    shot.from = body.HeadPosition;
                    shot.length = PathLength(shot);
                }
                // later a volley: the rest follow, slow then fast or fast
                // then slow, each to bat back
                if (teach == TeachStep.Done) {
                    websBeforeSlash = Mathf.Max(0, websBeforeSlash - 1);
                    volleyLeft = PerStage(webShotsPerStage, Stage) - 1;
                    volleyGapIndex = 0;
                    isVolleySlowFirst = Random.value < 0.5f;
                    nextVolleyTime = Time.time + VolleyGap();
                }
                break;
            case Attack.Dash:
                StartDash();
                break;
        }
    }

    private void CancelAttack(bool isKeepingLane = false) {
        attack = Attack.None;
        attackTimeS = 0f;
        if (warningId >= 0) {
            hud.RemoveWarning(warningId);
            warningId = -1;
        }
        body.SetSwipe(0f);
        body.SetCharge(0f);
        if (chargingShot != null) {
            RemoveShot(chargingShot);
            chargingShot = null;
        }
        if (!isKeepingLane) {
            ClearDashLane();
        }
    }

    // Dash  -------------------------------------------------------------------
    // a fast charge along the bark to where the monkey was, a bit past it;
    // touching the monkey on the way knocks it down
    // the dash runs to where the lane was locked
    private void StartDash() {
        isDashing = true;
        hasDashHit = false;
        dashTimeS = 0f;
        dashTravelU = 0f;
        body.SetSwipe(1f);
    }

    // the dash's end: where the monkey is, a bit past it; the start, way &
    // length are kept, so the dash runs exactly along the lane drawn
    private void AimDash() {
        Vector3 at = monkey.position;
        float arcU = Mathf.DeltaAngle(spiderAngle, AngleOf(at)) * Mathf.Deg2Rad
            * trunkRadiusU;
        float dy = at.y - spiderY;
        float distance = Mathf.Max(0.01f, Mathf.Sqrt(arcU * arcU + dy * dy));
        dashStartAngle = spiderAngle;
        dashStartY = spiderY;
        dashWayU = arcU / distance;
        dashWayV = dy / distance;
        dashLengthU = distance + dashOvershootU;
        // cut where it would leave the arena, so the lane is the whole run
        if (dashWayV > 0.001f) {
            dashLengthU = Mathf.Min(dashLengthU,
                (entryY + arenaHeightU - spiderY) / dashWayV);
        } else if (dashWayV < -0.001f) {
            dashLengthU = Mathf.Min(dashLengthU,
                (ArenaBottomY - spiderY) / dashWayV);
        }
        dashLengthU = Mathf.Max(0.5f, dashLengthU);
        dashAngle = spiderAngle
            + dashWayU * dashLengthU / trunkRadiusU * Mathf.Rad2Deg;
        dashY = spiderY + dashWayV * dashLengthU;
    }

    // a red lane on the bark from the spider to the dash's end, as wide as
    // the dash catches; dim while it aims, bright & flashing once locked
    // the lane: exactly where the dash catches, its run (dashLengthU) plus
    // the catch reach (dashHitU) past its end, dashHitU to either side
    private void DrawDashLane(bool isLocked) {
        if (dashLane == null) {
            dashLane = new Transform[PATH_PARTS * 2];
            for (int i = 0; i < dashLane.Length; i++) {
                dashLane[i] = CreatePrimitive(PrimitiveType.Cylinder, "DashLane",
                    Vector3.one * 0.1f, warnColor).transform;
            }
        }
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 30f);
        Color color = isLocked
            ? Color.Lerp(warnColor, Color.white, pulse * 0.5f)
            : warnColor * 0.6f;
        float thickness = isLocked ? 0.22f : 0.12f;
        float reach = dashLengthU + dashHitU;
        // across the way, on the bark
        float acrossU = -dashWayV * dashHitU;
        float acrossV = dashWayU * dashHitU;
        for (int edge = 0; edge < 2; edge++) {
            float sign = edge == 0 ? -1f : 1f;
            for (int i = 0; i < PATH_PARTS; i++) {
                float d0 = reach * i / PATH_PARTS;
                float d1 = reach * (i + 1) / PATH_PARTS;
                Vector3 from = BarkPoint(dashStartAngle, dashStartY,
                    dashWayU * d0 + acrossU * sign, dashWayV * d0 + acrossV * sign);
                Vector3 to = BarkPoint(dashStartAngle, dashStartY,
                    dashWayU * d1 + acrossU * sign, dashWayV * d1 + acrossV * sign);
                SetDashLanePart(edge * PATH_PARTS + i, from, to, thickness, color);
            }
        }
    }

    private void SetDashLanePart(int index, Vector3 from, Vector3 to,
        float thickness, Color color) {
        SetLine(dashLane[index], from, to, thickness);
        SetColor(dashLane[index].GetComponent<Renderer>(), color);
    }

    private void ClearDashLane() {
        if (dashLane == null) {
            return;
        }
        foreach (Transform part in dashLane) {
            if (part != null) {
                Destroy(part.gameObject);
            }
        }
        dashLane = null;
    }

    private float UpdateDash() {
        dashTimeS += Time.deltaTime;
        // straight along the locked lane, never turning; the arena's top or
        // bottom ends it early
        dashTravelU = Mathf.Min(dashLengthU,
            dashTravelU + dashSpeedU * SpeedScale * Time.deltaTime);
        float nextY = dashStartY + dashWayV * dashTravelU;
        bool isOutOfArena = nextY < ArenaBottomY - 0.01f
            || nextY > entryY + arenaHeightU + 0.01f;
        if (!isOutOfArena) {
            spiderAngle = dashStartAngle
                + dashWayU * dashTravelU / trunkRadiusU * Mathf.Rad2Deg;
            spiderY = nextY;
        }
        bool hasArrived = dashTravelU >= dashLengthU || isOutOfArena;
        if (!hasDashHit && DistanceToMonkey() < dashHitU
            && Time.time >= saveGraceEndTime) {
            hasDashHit = true;
            LethalHit("Caught!", false, true);
        }
        if (!hasArrived && dashTimeS < 1.5f) {
            return 1f;
        }
        isDashing = false;
        body.SetSwipe(0f);
        ClearDashLane();
        nextDashTime = Time.time + Mathf.Lerp(dashCooldownS, lateDashCooldownS,
            Rage);
        if (teach == TeachStep.Done) {
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 0.8f);
        }
        return 0f;
    }

    // Slash  ------------------------------------------------------------------
    // from afar, after a charge it is seen at, its front legs slash three
    // times, each first a red aim line on the bark, then a silk blade that
    // flies fast along the bark at where the monkey was; the gaps b/t
    // slashes go slow then fast or fast then slow. Fatal, unless dodged or
    // batted back w/ the tail as it turns gold, which hurts the spider
    private void StartSlash(bool isTeaching = false) {
        isSlashing = true;
        slashLeft = isTeaching ? 1 : PerStage(slashesPerStage, Stage);
        slashSlow = isTeaching ? teachSlowScale : 1f;
        slashChargeLeftS = slashChargeS * slashSlow;
        slashGapIndex = 0;
        isSlashSlowFirst = Random.value < 0.5f;
        nextSlashTime = Time.time;
        isSlashAiming = false;
    }

    private float UpdateSlash() {
        // the charge, seen like the web shot's
        if (slashChargeLeftS > 0f) {
            slashChargeLeftS -= Time.deltaTime;
            float charge = 1f - slashChargeLeftS / (slashChargeS * slashSlow);
            body.SetCharge(charge);
            body.SetSwipe(charge);
            UpdateSlashChargeFx(charge);
            if (slashChargeLeftS <= 0f) {
                body.SetCharge(0f);
                HideSlashSigil();
            }
            return 0f;
        }
        if (!isSlashAiming) {
            if (Time.time < nextSlashTime) {
                return 0f;
            }
            slashTimeS = 0f;
            isSlashAiming = true;
        }
        slashTimeS += Time.deltaTime;
        // the aim follows the monkey till the slash
        slashAimDeg = AimDegToMonkey();
        float windUpS = slashWindUpS * slashSlow * TimeScaleNow;
        float progress = Mathf.Clamp01(slashTimeS / windUpS);
        body.SetSwipe(progress);
        DrawSlashAim(progress);
        if (slashTimeS < windUpS) {
            return 0f;
        }

        isSlashAiming = false;
        body.SetSwipe(0f);
        ShowSlashAim(false);
        LaunchCrescent(slashAimDeg);
        slashLeft--;
        if (slashLeft <= 0) {
            isSlashing = false;
            nextAttackTime = Time.time
                + (teach == TeachStep.Done ? RestNow() : teachGapS);
            return 0f;
        }
        bool isSlow = (slashGapIndex % 2 == 0) == isSlashSlowFirst;
        slashGapIndex++;
        nextSlashTime = Time.time
            + (isSlow ? slashSlowGapS : slashFastGapS) * TimeScaleNow;
        return 0f;
    }

    // the direction from the spider to the monkey on the bark; deg, 0 round
    // the trunk, 90 up
    private float AimDegToMonkey() {
        Vector3 at = monkey.position;
        float arcU = Mathf.DeltaAngle(spiderAngle, AngleOf(at)) * Mathf.Deg2Rad
            * trunkRadiusU;
        return Mathf.Atan2(at.y - spiderY, arcU) * Mathf.Rad2Deg;
    }

    // the lowest the spider crawls: a bit below the entry, well above the
    // guard
    private float ArenaBottomY => entryY - 6f;

    // a point on the bark, u round the trunk & v up from a base spot
    private Vector3 BarkPoint(float baseAngle, float baseY, float u, float v) {
        return PointOnTrunk(baseAngle + u / trunkRadiusU * Mathf.Rad2Deg,
            baseY + v, trunkRadiusU + 0.12f);
    }

    // the aim: a thin red line along the bark out to the slash's reach,
    // brighter as the slash nears
    private void DrawSlashAim(float progress) {
        if (slashAim == null) {
            slashAim = new Transform[PATH_PARTS];
            for (int i = 0; i < slashAim.Length; i++) {
                slashAim[i] = CreatePrimitive(PrimitiveType.Cylinder, "SlashAim",
                    Vector3.one * 0.1f, warnColor).transform;
            }
        }
        ShowSlashAim(true);
        float radians = slashAimDeg * Mathf.Deg2Rad;
        float du = Mathf.Cos(radians);
        float dv = Mathf.Sin(radians);
        Color color = Color.Lerp(warnColor * 0.5f, warnColor, progress);
        for (int i = 0; i < PATH_PARTS; i++) {
            // dashed: every other part
            bool isShown = i % 2 == 0;
            slashAim[i].gameObject.SetActive(isShown);
            float d0 = slashRangeU * i / PATH_PARTS;
            float d1 = slashRangeU * (i + 1) / PATH_PARTS;
            SetLine(slashAim[i],
                BarkPoint(spiderAngle, spiderY, du * d0, dv * d0),
                BarkPoint(spiderAngle, spiderY, du * d1, dv * d1),
                Mathf.Lerp(0.06f, 0.14f, progress));
            SetColor(slashAim[i].GetComponent<Renderer>(), color);
        }
    }

    private void ShowSlashAim(bool isShown) {
        if (slashAim == null) {
            return;
        }
        foreach (Transform part in slashAim) {
            if (part != null) {
                part.gameObject.SetActive(isShown);
            }
        }
    }

    // a crescent bulging forward, as wide as slashWidthU, flat on the bark
    private void LaunchCrescent(float directionDeg) {
        Crescent crescent = new Crescent {
            originAngle = spiderAngle,
            originY = spiderY,
            directionDeg = directionDeg,
            distanceU = 0.8f,
            parts = new Transform[CRESCENT_STRANDS * CRESCENT_PARTS
                + CRESCENT_SPOKES],
        };
        for (int i = 0; i < crescent.parts.Length; i++) {
            crescent.parts[i] = CreatePrimitive(PrimitiveType.Cylinder,
                "SilkBlade", Vector3.one * 0.1f, silkColor).transform;
        }
        crescents.Add(crescent);
        Burst(body.HeadPosition, silkColor, 6);
    }

    private void UpdateCrescents() {
        for (int i = crescents.Count - 1; i >= 0; i--) {
            Crescent crescent = crescents[i];
            float speedU = slashSpeedU * SpeedScale;
            if (crescent.isReflected) {
                // the clash: it slows into the tail, sparks, holds, then
                // speeds off back to where it was slashed from, the spider
                crescent.clashS += Time.unscaledDeltaTime;
                float inShare = ClashIn(crescent.clashS);
                if (inShare < 1f) {
                    crescent.distanceU = Mathf.Lerp(crescent.parryDistanceU,
                        crescent.contactDistanceU, inShare * inShare);
                } else {
                    if (!crescent.hasClashed) {
                        crescent.hasClashed = true;
                        Clash(CrescentContact(crescent));
                    }
                    float outShare = ClashOut(crescent.clashS);
                    float kick = 1f - (1f - outShare) * (1f - outShare);
                    crescent.distanceU = Mathf.Lerp(crescent.contactDistanceU,
                        0.8f, kick);
                    if (outShare >= 1f) {
                        RemoveCrescent(crescent);
                        Burst(body.HeadPosition, goldenColor, 14);
                        Hurt(slashParryDamage);
                        if (phase != Phase.Fighting) {
                            break;
                        }
                        continue;
                    }
                }
                ClashTrail(crescent.clashS, CrescentPoint(crescent, 0f),
                    ref crescent.nextFxS);
                DrawCrescent(crescent, true);
                continue;
            }
            float lastDistanceU = crescent.distanceU;
            crescent.distanceU += speedU * Time.deltaTime;
            if (crescent.distanceU > slashRangeU + 4f) {
                RemoveCrescent(crescent);
                continue;
            }
            // to the monkey's near edge, the side the slash comes from
            float ahead = CrescentAhead(crescent, out float across)
                - MONKEY_EDGE_U;
            bool isOnWay = Mathf.Abs(across) <= slashWidthU * 0.5f + 0.6f;
            bool isGold = isOnWay && ahead >= -0.5f && ahead <= waveGoldU;
            DrawCrescent(crescent, isGold);
            if (isOnWay && ahead >= -0.5f && ahead <= parryReachU
                && IsParryArmed()) {
                OnCrescentParried(crescent);
                continue;
            }
            if (isGold && !hasTaughtSlash && !isTeachFrozen) {
                FreeForLesson();
                isTeachFrozen = true;
                frozenCrescent = crescent;
                lastTapTime = -10f;
                Teach("Now!", "Tap SPACE 2 times to bounce it back",
                    goldenColor, 0f);
            }
            if (IsHitByCrescent(crescent, lastDistanceU)
                && Time.time >= saveGraceEndTime) {
                RemoveCrescent(crescent);
                // a loss or the armor clears the list, so stop here
                LethalHit("Hit!");
                break;
            }
        }
    }

    // how far the monkey is ahead of the crescent's middle along its way,
    // & how far across it
    private float CrescentAhead(Crescent crescent, out float across) {
        Vector3 at = monkey.position;
        float mu = Mathf.DeltaAngle(crescent.originAngle, AngleOf(at))
            * Mathf.Deg2Rad * trunkRadiusU;
        float mv = at.y - crescent.originY;
        float radians = crescent.directionDeg * Mathf.Deg2Rad;
        float along = mu * Mathf.Cos(radians) + mv * Mathf.Sin(radians);
        across = -mu * Mathf.Sin(radians) + mv * Mathf.Cos(radians);
        return along - (crescent.distanceU + CRESCENT_BULGE_U);
    }

    // a point of the crescent: lateral from -1 to 1 across it, back that
    // far behind its front
    private Vector3 CrescentPoint(Crescent crescent, float lateral,
        float back = 0f) {
        float radians = crescent.directionDeg * Mathf.Deg2Rad;
        float du = Mathf.Cos(radians);
        float dv = Mathf.Sin(radians);
        float side = lateral * slashWidthU * 0.5f;
        float ahead = crescent.distanceU - back
            + CRESCENT_BULGE_U * (1f - lateral * lateral);
        return BarkPoint(crescent.originAngle, crescent.originY,
            du * ahead - dv * side, dv * ahead + du * side);
    }

    // a silk blade: three silk strands one behind the other, tied by short
    // spokes like a torn piece of web; white, gold & flashing when it can
    // be batted back, gold flying back
    private void DrawCrescent(Crescent crescent, bool isGold) {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 30f);
        Color color = crescent.isReflected
            ? goldenColor
            : isGold ? Color.Lerp(goldenColor, Color.white, pulse) : Color.white;
        int index = 0;
        for (int strand = 0; strand < CRESCENT_STRANDS; strand++) {
            float back = strand * 0.3f;
            float width = 1f - strand * 0.15f;
            for (int i = 0; i < CRESCENT_PARTS; i++) {
                float l0 = (-1f + 2f * i / CRESCENT_PARTS) * width;
                float l1 = (-1f + 2f * (i + 1) / CRESCENT_PARTS) * width;
                float middle = 1f - Mathf.Abs((l0 + l1) * 0.5f);
                SetCrescentPart(crescent, index++,
                    CrescentPoint(crescent, l0, back),
                    CrescentPoint(crescent, l1, back),
                    Mathf.Lerp(0.04f, strand == 0 ? 0.16f : 0.09f, middle),
                    color);
            }
        }
        for (int i = 0; i < CRESCENT_SPOKES; i++) {
            float lateral = -0.8f + 1.6f * i / (CRESCENT_SPOKES - 1f);
            SetCrescentPart(crescent, index++,
                CrescentPoint(crescent, lateral, 0f),
                CrescentPoint(crescent, lateral * 0.7f,
                    (CRESCENT_STRANDS - 1) * 0.3f),
                0.04f, color);
        }
    }

    private void SetCrescentPart(Crescent crescent, int index, Vector3 from,
        Vector3 to, float thickness, Color color) {
        SetLine(crescent.parts[index], from, to, thickness);
        SetColor(crescent.parts[index].GetComponent<Renderer>(), color);
    }

    // the monkey where the crescent is now, along it & across it
    // the monkey swept over by the crescent's front this step, from where it
    // was to where it is, so a fast one never skips past
    private bool IsHitByCrescent(Crescent crescent, float lastDistanceU) {
        Vector3 at = monkey.position;
        float mu = Mathf.DeltaAngle(crescent.originAngle, AngleOf(at))
            * Mathf.Deg2Rad * trunkRadiusU;
        float mv = at.y - crescent.originY;
        float radians = crescent.directionDeg * Mathf.Deg2Rad;
        float along = mu * Mathf.Cos(radians) + mv * Mathf.Sin(radians);
        float across = -mu * Mathf.Sin(radians) + mv * Mathf.Cos(radians);
        float halfWidth = slashWidthU * 0.5f;
        if (Mathf.Abs(across) > halfWidth) {
            return false;
        }
        float lateral = across / halfWidth;
        float bulge = CRESCENT_BULGE_U * (1f - lateral * lateral);
        float back = lastDistanceU + bulge - 0.8f;
        float front = crescent.distanceU + bulge + 0.8f;
        return along >= back && along <= front;
    }

    private void RemoveCrescent(Crescent crescent) {
        foreach (Transform part in crescent.parts) {
            if (part != null) {
                Destroy(part.gameObject);
            }
        }
        crescents.Remove(crescent);
    }

    // the slashes still to come stop; crescents in flight fly on
    private void StopSlashing() {
        HideSlashSigil();
        isSlashing = false;
        isSlashAiming = false;
        slashChargeLeftS = 0f;
        body.SetCharge(0f);
        ShowSlashAim(false);
        body.SetSwipe(0f);
    }

    // all of it gone, crescents too
    private void ClearSlash() {
        StopSlashing();
        if (slashAim != null) {
            foreach (Transform part in slashAim) {
                if (part != null) {
                    Destroy(part.gameObject);
                }
            }
        }
        slashAim = null;
        while (crescents.Count > 0) {
            RemoveCrescent(crescents[0]);
        }
    }

    // Shots  ------------------------------------------------------------------
    private Shot NewShot(ShotKind kind, Vector3 target) {
        Shot shot = new Shot {
            kind = kind,
            target = target,
            from = body.HeadPosition,
            isCharging = true,
        };
        // the ball is the charge orb at the head
        shot.ball = CreatePrimitive(PrimitiveType.Sphere, kind.ToString(),
            Vector3.one * 0.4f, silkColor).transform;
        shot.wave = BuildWave();
        shot.wave.gameObject.SetActive(false);
        shot.length = PathLength(shot);
        shots.Add(shot);
        return shot;
    }

    // a silk bullet: one long glowing capsule, flying along its length
    private Transform BuildWave() {
        Transform bullet = new GameObject("SilkBullet").transform;
        bullet.SetParent(transform, false);
        Transform core = CreatePrimitive(PrimitiveType.Capsule, "Bullet",
            Vector3.one, Color.white).transform;
        core.SetParent(bullet, false);
        core.localRotation = Quaternion.Euler(90f, 0f, 0f);
        core.localScale = new Vector3(0.45f, 0.75f, 0.45f);
        return bullet;
    }

    private void ColorWave(Transform wave, Color color) {
        foreach (Renderer part in wave.GetComponentsInChildren<Renderer>(true)) {
            SetColor(part, color);
        }
    }

    // white sparks & rings drawn in to the head while it charges
    // the web shot's charge: calm & white, silk drawn in to the orb
    private void UpdateChargeSparks() {
        chargeRingS -= Time.deltaTime;
        if (chargeRingS <= 0f) {
            chargeRingS = 0.16f;
            AddRing(body.HeadPosition, silkColor, 0f, true);
        }
        chargeSparkS -= Time.deltaTime;
        if (chargeSparkS > 0f) {
            return;
        }
        chargeSparkS = 0.03f;
        Vector3 head = body.HeadPosition;
        Vector3 start = head + Random.onUnitSphere * 2.2f;
        Transform spark = CreatePrimitive(PrimitiveType.Cube, "Spark",
            Vector3.one * 0.15f, Color.white).transform;
        spark.position = start;
        sparks.Add(new Spark {
            transform = spark,
            velocity = (head - start) / 0.25f,
            lifeS = 0.25f,
            isFloating = true,
        });
    }

    // the slash's charge: angry & red, nothing like the web shot's: a red
    // blade sign over its head grows & flickers, red rings burst out & red
    // streaks spit off it, its eyes blaze red
    private void UpdateSlashChargeFx(float charge) {
        Vector3 head = body.HeadPosition;
        chargeRingS -= Time.deltaTime;
        if (chargeRingS <= 0f) {
            chargeRingS = 0.2f;
            AddRing(head, warnColor, 0f);
        }
        chargeSparkS -= Time.deltaTime;
        if (chargeSparkS <= 0f) {
            chargeSparkS = 0.06f;
            BurstStreaks(head, warnColor, 2, 3f, 7f);
        }
        if (slashSigil == null) {
            slashSigil = new Transform[SIGIL_PARTS];
            for (int i = 0; i < SIGIL_PARTS; i++) {
                slashSigil[i] = CreatePrimitive(PrimitiveType.Cylinder,
                    "SlashSign", Vector3.one, warnColor).transform;
            }
        }
        // a crescent facing the camera, opening up over its head
        Quaternion facing = ViewRotation();
        Vector3 right = facing * Vector3.right;
        Vector3 up = facing * Vector3.up;
        float flicker = 0.75f + 0.25f * Mathf.Sin(Time.time * 45f);
        float radius = Mathf.Lerp(0.6f, 1.8f, charge);
        Vector3 center = head + up * (0.6f + 0.5f * charge);
        float spread = Mathf.Lerp(40f, 150f, charge) * Mathf.Deg2Rad;
        Color color = Color.Lerp(warnColor, Color.white,
            0.25f * (1f - flicker));
        for (int i = 0; i < SIGIL_PARTS; i++) {
            float a0 = -spread * 0.5f + spread * i / SIGIL_PARTS;
            float a1 = -spread * 0.5f + spread * (i + 1) / SIGIL_PARTS;
            Vector3 p0 = center + (right * Mathf.Sin(a0) + up * Mathf.Cos(a0))
                * radius;
            Vector3 p1 = center + (right * Mathf.Sin(a1) + up * Mathf.Cos(a1))
                * radius;
            // thick in the middle, sharp at the tips
            float mid = 1f - Mathf.Abs((i + 0.5f) / SIGIL_PARTS * 2f - 1f);
            slashSigil[i].gameObject.SetActive(true);
            SetLine(slashSigil[i], p0, p1,
                (0.05f + 0.2f * mid) * (0.5f + 0.5f * charge) * flicker);
            SetColor(slashSigil[i].GetComponent<Renderer>(), color);
        }
    }

    private void HideSlashSigil() {
        if (slashSigil == null) {
            return;
        }
        foreach (Transform part in slashSigil) {
            if (part != null) {
                Destroy(part.gameObject);
            }
        }
        slashSigil = null;
    }

    private void RemoveShot(Shot shot) {
        if (shot.ball != null) {
            Destroy(shot.ball.gameObject);
        }
        if (shot.wave != null) {
            Destroy(shot.wave.gameObject);
        }
        shots.Remove(shot);
    }

    private void ClearShots() {
        while (shots.Count > 0) {
            RemoveShot(shots[0]);
        }
        chargingShot = null;
    }

    // bullets home in on the monkey; a batted back one flies back from
    // where it was hit
    private Vector3 PathPoint(Shot shot, float t) {
        Vector3 to = shot.isReflected ? shot.target : monkey.position;
        return ArcPoint(shot.from, to, t, 1.5f);
    }

    private float PathLength(Shot shot) {
        float length = 0f;
        Vector3 last = PathPoint(shot, 0f);
        for (int i = 1; i <= PATH_PARTS; i++) {
            Vector3 next = PathPoint(shot, i / (float)PATH_PARTS);
            length += Vector3.Distance(last, next);
            last = next;
        }
        return Mathf.Max(0.1f, length);
    }

    private void UpdateShots() {
        for (int i = shots.Count - 1; i >= 0; i--) {
            Shot shot = shots[i];
            if (shot.ball == null) {
                shots.RemoveAt(i);
                continue;
            }
            if (shot.isCharging) {
                shot.from = body.HeadPosition;
                shot.length = PathLength(shot);
                // the orb swells & throbs at the head
                float throb = 1f + 0.15f * Mathf.Sin(Time.time * 40f);
                shot.ball.position = body.HeadPosition;
                shot.ball.localScale = Vector3.one
                    * (0.2f + 0.9f * shot.fill) * throb;
                continue;
            }
            if (shot.isReflected) {
                UpdateClashShot(shot);
                continue;
            }
            shot.length = PathLength(shot);
            float speedU = webShotSpeedU * SpeedScale;
            shot.t += speedU * Time.deltaTime / shot.length;
            shot.t = Mathf.Clamp01(shot.t);
            Vector3 at = PathPoint(shot, shot.t);
            shot.lastPosition = at;
            shot.ball.gameObject.SetActive(false);
            // to the monkey's near edge, the side the bullet comes from
            float toMonkey = Vector3.Distance(at, monkey.position)
                - MONKEY_EDGE_U;
            bool isGold = toMonkey <= waveGoldU;
            DrawWave(shot, at, isGold);
            if (toMonkey <= parryReachU && IsParryArmed()) {
                OnParry(shot, at);
                continue;
            }
            if (isGold && !hasTaughtWeb && !isTeachFrozen) {
                FreezeForParry(shot);
            }
            if (shot.t >= 1f) {
                RemoveShot(shot);
                Vector3 landing = monkey.position;
                PlaceWeb(AngleOf(landing), landing.y);
            }
        }
    }

    // the batted back bullet: slows into the tail, sparks, holds, then
    // speeds off back to the spider, sticking & hurting it
    private void UpdateClashShot(Shot shot) {
        shot.clashS += Time.unscaledDeltaTime;
        Vector3 contact = EdgeToward(shot.clashFrom);
        Vector3 at;
        float inShare = ClashIn(shot.clashS);
        if (inShare < 1f) {
            at = Vector3.Lerp(shot.clashFrom, contact, inShare * inShare);
        } else {
            if (!shot.hasClashed) {
                shot.hasClashed = true;
                Clash(contact);
            }
            float outShare = ClashOut(shot.clashS);
            float kick = 1f - (1f - outShare) * (1f - outShare);
            at = outShare <= 0f
                ? contact + Random.insideUnitSphere * 0.12f
                : ArcPoint(contact, body.HeadPosition, kick, 1.5f);
            if (outShare >= 1f) {
                RemoveShot(shot);
                Burst(body.HeadPosition, goldenColor, 14);
                WebTheSpider();
                Hurt(webParryDamage);
                return;
            }
        }
        ClashTrail(shot.clashS, at, ref shot.nextFxS);
        Vector3 heading = at - shot.lastPosition;
        shot.lastPosition = at;
        shot.wave.gameObject.SetActive(true);
        if (heading.sqrMagnitude > 0.000001f) {
            shot.wave.SetPositionAndRotation(at,
                Quaternion.LookRotation(heading.normalized, Vector3.up));
        } else {
            shot.wave.position = at;
        }
        shot.wave.localScale = Vector3.one * 1.2f;
        ColorWave(shot.wave, goldenColor);
    }

    // the bullet flies along its length; white, gold & flashing once close
    // enough to parry
    private void DrawWave(Shot shot, Vector3 at, bool isGold) {
        shot.wave.gameObject.SetActive(true);
        float step = shot.isReflected ? -0.05f : 0.05f;
        Vector3 ahead = PathPoint(shot, Mathf.Clamp01(shot.t + step));
        Vector3 heading = ahead - at;
        if (heading.sqrMagnitude < 0.0001f) {
            heading = monkey.position - at;
        }
        if (heading.sqrMagnitude > 0.0001f) {
            shot.wave.SetPositionAndRotation(at,
                Quaternion.LookRotation(heading.normalized, Vector3.up));
        } else {
            shot.wave.position = at;
        }
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 30f);
        Color color = shot.isReflected
            ? goldenColor
            : isGold ? Color.Lerp(goldenColor, Color.white, pulse) : Color.white;
        shot.wave.localScale = Vector3.one * (isGold ? 1.25f : 1f);
        ColorWave(shot.wave, color);
    }

    // Parry  ------------------------------------------------------------------
    // a double tap of Space swings the monkey's own tail: for a moment a gold
    // web wave close by is batted back, & the spider is stuck in it
    private void UpdateParryInput(GameController game) {
        UpdateTail();
        bool isCaught = struggle != null && struggle.IsTrapped;
        // in a lesson's stop the double tap is always the parry
        if (isCaught && !isTeachFrozen) {
            lastTapTime = -10f;
            return;
        }
        if (game.IsPaused || !WasPressed(confirmAction)) {
            return;
        }
        // only w/ something to bat on its way (or charging): a double tap
        // for a grab otherwise never swings the tail
        bool isComing = shots.Exists(s => !s.isReflected)
            || crescents.Exists(c => !c.isReflected) || isSlashing;
        if (!isComing) {
            lastTapTime = -10f;
            return;
        }
        float now = Time.unscaledTime;
        // a swing can't start till the last one is over, unless that one
        // hit: a parry readies the tail at once, so a chain of slashes can
        // all be batted back, but a whiff waits its whole follow through
        bool isSwinging = tailTimeS >= 0f && !hasSwingHit;
        if (now - lastTapTime <= doubleTapS && !isSwinging) {
            lastTapTime = -10f;
            StartTail();
            if (isTeachFrozen) {
                ParryFrozen();
            }
        } else {
            lastTapTime = now;
        }
    }

    // the lesson's frozen bullet or slash is batted back at once
    private void ParryFrozen() {
        isTeachFrozen = false;
        hud.SetPrompt(false, Vector3.zero);
        if (frozenShot != null && frozenShot.wave != null) {
            Shot shot = frozenShot;
            frozenShot = null;
            OnParry(shot, shot.wave.position);
        } else if (frozenCrescent != null) {
            Crescent crescent = frozenCrescent;
            frozenCrescent = null;
            OnCrescentParried(crescent);
        }
    }

    // a parry plays in slow motion w/ the camera close in on the monkey,
    // no flash: "Nice!" & gold sparks
    private void ParryEffect(Vector3 at) {
        // more on its way (a volley's next bullet, the next slash): only a
        // short beat, the camera barely in, so the next one is seen in time
        bool isMore = IsMoreComing();
        parrySlowEndTime = Time.unscaledTime
            + (isMore ? QUICK_SLOW_S : parrySlowS);
        shotMaxWeight = isMore ? QUICK_SHOT_WEIGHT : 1f;
        // a shot already on goes on w/o easing in again
        float now = Time.unscaledTime;
        bool isShotOn = now < shotEndTime;
        hasShotPose = isShotOn && hasShotPose;
        shotStartTime = isShotOn ? Mathf.Min(shotStartTime, now - SHOT_IN_S)
            : now;
        shotEndTime = parrySlowEndTime + SHOT_OUT_S;
        if (!isShotOn) {
            shotWeightCap = shotMaxWeight;
        }
        shotFrom = at;
        shotSide = isShotOn ? shotSide : (Random.value < 0.5f ? -1f : 1f);
        shotClashTime = -10f;
    }

    // anything still to bat or dodge: a bullet or slash coming, a volley or
    // a row of slashes not done
    private bool IsMoreComing() => shots.Exists(s => !s.isReflected)
        || crescents.Exists(c => !c.isReflected) || isSlashing
        || volleyLeft > 0 || (chargingShot != null);

    // a full parry shot is cut short the moment another attack comes, so it
    // never hides it
    private void CutShotIfMoreComes() {
        float now = Time.unscaledTime;
        if (now >= shotEndTime || shotMaxWeight < 1f || !IsMoreComing()) {
            return;
        }
        shotMaxWeight = QUICK_SHOT_WEIGHT;
        parrySlowEndTime = Mathf.Min(parrySlowEndTime, now + 0.1f);
        shotEndTime = Mathf.Min(shotEndTime, now + SHOT_OUT_S * 0.6f);
    }

    // The Parry Shot  ---------------------------------------------------------
    // a camera move laid over the follow camera just b4 it draws & taken off
    // after, so it never fights Cinemachine & runs in real time while time
    // crawls or stands: it swoops in close to the monkey, swinging round it
    // & tilting, slams in & shakes at the clash, pans after the thing kicked
    // back at the spider, then eases back out
    private void OnBeginCamera(ScriptableRenderContext context, Camera view) {
        if (view == null || view != Camera.main || monkey == null) {
            return;
        }
        float now = Time.unscaledTime;
        if (now >= shotEndTime) {
            return;
        }
        Transform eye = view.transform;
        float t = now - shotStartTime;
        float total = shotEndTime - shotStartTime;
        // eased down, not jumped, when cut short for a coming attack
        shotWeightCap = Mathf.MoveTowards(shotWeightCap, shotMaxWeight,
            Time.unscaledDeltaTime * 4f);
        float weight = Mathf.Min(Ease(t / SHOT_IN_S),
            Ease((shotEndTime - now) / SHOT_OUT_S)) * shotWeightCap;
        if (weight <= 0f) {
            return;
        }
        isShotApplied = true;
        restEyePosition = eye.position;
        restEyeRotation = eye.rotation;
        restFov = view.fieldOfView;

        Vector3 focus = monkey.position;
        Vector3 back = eye.position - focus;
        if (back.sqrMagnitude < 0.01f) {
            back = Vector3.back;
        }
        back.Normalize();
        // a slow swing round the monkey all through the slow motion
        float progress = Mathf.Clamp01(t / Mathf.Max(0.01f, total));
        float orbit = shotSide * parryOrbitDeg * (0.35f + 0.65f * progress);
        Vector3 way = Quaternion.AngleAxis(orbit, Vector3.up) * back;
        way = (way + Vector3.up * 0.15f).normalized;
        float distance = parryCloseUpU;
        float roll = shotSide * parryRollDeg;
        float fovScale = 1f;
        // the clash: a slam in, a roll kick, a shake
        float sinceClash = now - shotClashTime;
        Vector3 shake = Vector3.zero;
        if (sinceClash >= 0f && sinceClash < SHOT_SLAM_S) {
            float hit = 1f - sinceClash / SHOT_SLAM_S;
            hit *= hit;
            distance -= 1.1f * hit;
            roll -= shotSide * 6f * hit;
            fovScale -= 0.12f * hit;
            shake = Random.insideUnitSphere * 0.18f * hit;
        }
        // looking a bit toward where the thing came from, then after it as
        // it is kicked back at the spider
        Vector3 look = Vector3.Lerp(focus, shotFrom, 0.2f);
        if (sinceClash >= 0f && body != null) {
            float pan = Ease((sinceClash - CLASH_HOLD_S) / CLASH_OUT_S);
            look = Vector3.Lerp(look,
                Vector3.Lerp(focus, body.HeadPosition, 0.45f), pan);
            distance += 0.9f * pan;
        }
        Vector3 shotPosition = focus + way * distance + shake;
        Quaternion shotRotation = Quaternion.LookRotation(
            look - shotPosition, Vector3.up)
            * Quaternion.AngleAxis(roll, Vector3.forward);
        eye.SetPositionAndRotation(
            Vector3.Lerp(restEyePosition, shotPosition, weight),
            Quaternion.Slerp(restEyeRotation, shotRotation, weight));
        view.fieldOfView = Mathf.Lerp(restFov, restFov * fovScale, weight);
        shotViewRotation = eye.rotation;
        hasShotPose = true;
    }

    // which way the camera looks as drawn: the parry shot's while it is on
    private Quaternion ViewRotation() {
        if (Time.unscaledTime < shotEndTime && hasShotPose) {
            return shotViewRotation;
        }
        Camera view = Camera.main;
        return view != null ? view.transform.rotation : Quaternion.identity;
    }

    // the follow camera's own pose back after the draw
    private void OnEndCamera(ScriptableRenderContext context, Camera view) {
        if (!isShotApplied || view == null || view != Camera.main) {
            return;
        }
        isShotApplied = false;
        view.transform.SetPositionAndRotation(restEyePosition, restEyeRotation);
        view.fieldOfView = restFov;
    }

    private static float Ease(float share) {
        share = Mathf.Clamp01(share);
        return share * share * (3f - 2f * share);
    }

    // a batted back slash flies back at the spider & hurts it
    private void OnCrescentParried(Crescent crescent) {
        hasSwingHit = true;
        crescent.isReflected = true;
        crescent.clashS = 0f;
        crescent.hasClashed = false;
        crescent.parryDistanceU = crescent.distanceU;
        // its middle meets the monkey's near edge
        float ahead = CrescentAhead(crescent, out float _) - MONKEY_EDGE_U;
        crescent.contactDistanceU = crescent.distanceU + Mathf.Max(0f, ahead);
        ParryEffect(CrescentPoint(crescent, 0f));
        if (!hasTaughtSlash) {
            hasTaughtSlash = true;
            hud.HideBanner();
        }
    }

    // where a batted thing is met: the monkey's edge on the side it comes
    // from
    private Vector3 EdgeToward(Vector3 from) {
        Vector3 way = from - monkey.position;
        return way.sqrMagnitude > 0.0001f
            ? monkey.position + way.normalized * MONKEY_EDGE_U
            : monkey.position;
    }

    // a slash's: the monkey's edge facing back along the slash's way
    private Vector3 CrescentContact(Crescent crescent) {
        float radians = crescent.directionDeg * Mathf.Deg2Rad;
        Vector3 back = BarkPoint(AngleOf(monkey.position), monkey.position.y,
            -Mathf.Cos(radians) * MONKEY_EDGE_U,
            -Mathf.Sin(radians) * MONKEY_EDGE_U);
        back = monkey.position + (back - PointOnTrunk(AngleOf(monkey.position),
            monkey.position.y, trunkRadiusU + 0.12f));
        return back;
    }

    // the middle of the monkey's body, a hair out toward the camera side
    private Vector3 TailContact() {
        Vector3 outward = monkey.position - trunkCenter;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 0.001f ? outward.normalized
            : Vector3.forward;
        return monkey.position + outward * 0.25f;
    }

    // the clash: sparks flying off the tail where the thing meets it, &
    // "Nice!"
    private void Clash(Vector3 at) {
        clashStopEndTime = Time.unscaledTime + CLASH_STOP_S;
        shotClashTime = Time.unscaledTime;
        shakeEndTime = Time.unscaledTime + SHAKE_S;
        hud.PopText("Nice!", at, goldenColor);
        AddCross(at);
        BurstStreaks(at, new Color(1f, 0.55f, 0.1f), 26, 6f, 15f);
        BurstStreaks(at, new Color(1f, 0.9f, 0.4f), 16, 4f, 10f);
        Burst(at, Color.white, 3);
        AddRing(at, goldenColor, 0f);
        AddRing(at, new Color(1f, 0.55f, 0.1f), 0.07f);
        AddPop(at);
        tailFlashEndTime = Time.unscaledTime + 0.15f;
    }

    // two long white strokes crossing at the clash, a hard flash of it
    private void AddCross(Vector3 at) {
        Vector3 right = ViewRotation() * Vector3.right;
        Vector3 up = ViewRotation() * Vector3.up;
        for (int i = 0; i < 2; i++) {
            Vector3 way = (right + (i == 0 ? up : -up)).normalized;
            Transform stroke = CreatePrimitive(PrimitiveType.Cylinder, "Cross",
                Vector3.one, Color.white).transform;
            SetLine(stroke, at - way * 0.9f, at + way * 0.9f, 0.06f);
            pops.Add(new Spark {
                transform = stroke,
                lifeS = 0.12f,
                isStroke = true,
                size = 0.06f,
            });
        }
    }

    // sparks that streak: long thin bits along their flight
    private void BurstStreaks(Vector3 at, Color color, int count,
        float minSpeedU, float maxSpeedU) {
        for (int i = 0; i < count; i++) {
            Transform spark = CreatePrimitive(PrimitiveType.Cube, "Streak",
                Vector3.one * 0.1f, color).transform;
            spark.position = at;
            sparks.Add(new Spark {
                transform = spark,
                velocity = Random.onUnitSphere
                    * Random.Range(minSpeedU, maxSpeedU),
                lifeS = Random.Range(0.25f, 0.5f),
                isStreak = true,
            });
        }
    }

    // a still bead that fades, eg the trail of a batted back thing
    private void AddTrailBead(Vector3 at) {
        Transform bead = CreatePrimitive(PrimitiveType.Sphere, "TrailBead",
            Vector3.one * 0.3f, goldenColor).transform;
        bead.position = at;
        sparks.Add(new Spark {
            transform = bead,
            velocity = Vector3.zero,
            lifeS = 0.25f,
            isFloating = true,
            size = 0.35f,
        });
    }

    // a shock ring facing the camera, opening fast & thinning
    private void AddRing(Vector3 at, Color color, float delayS,
        bool isInward = false) {
        Ring ring = new Ring {
            center = at,
            color = color,
            ageS = -delayS,
            isInward = isInward,
            parts = new Transform[RING_PARTS],
        };
        for (int i = 0; i < RING_PARTS; i++) {
            ring.parts[i] = CreatePrimitive(PrimitiveType.Cylinder, "ShockRing",
                Vector3.one * 0.1f, color).transform;
            ring.parts[i].gameObject.SetActive(false);
        }
        rings.Add(ring);
    }

    // a white orb that swells & fades at the clash
    private void AddPop(Vector3 at) {
        Transform orb = CreatePrimitive(PrimitiveType.Sphere, "ClashPop",
            Vector3.one * 0.2f, Color.white).transform;
        orb.position = at;
        pops.Add(new Spark {
            transform = orb,
            lifeS = 0.18f,
            size = 0.7f,
        });
    }

    private void UpdateClashFx() {
        Quaternion facing = ViewRotation();
        for (int i = rings.Count - 1; i >= 0; i--) {
            Ring ring = rings[i];
            ring.ageS += Time.unscaledDeltaTime;
            float t = ring.ageS / RING_S;
            if (t >= 1f) {
                foreach (Transform part in ring.parts) {
                    if (part != null) {
                        Destroy(part.gameObject);
                    }
                }
                rings.RemoveAt(i);
                continue;
            }
            if (t < 0f) {
                continue;
            }
            float ease = 1f - (1f - t) * (1f - t);
            float radius = ring.isInward
                ? Mathf.Lerp(3f, 0.3f, ease)
                : Mathf.Lerp(0.2f, 1.6f, ease);
            float thickness = ring.isInward
                ? Mathf.Lerp(0.03f, 0.14f, t)
                : Mathf.Lerp(0.09f, 0.01f, t);
            // a drawn in ring follows the head it closes on
            if (ring.isInward && body.IsBuilt) {
                ring.center = body.HeadPosition;
            }
            Vector3 right = facing * Vector3.right;
            Vector3 up = facing * Vector3.up;
            for (int k = 0; k < RING_PARTS; k++) {
                float a0 = k * Mathf.PI * 2f / RING_PARTS;
                float a1 = (k + 1) * Mathf.PI * 2f / RING_PARTS;
                Vector3 p0 = ring.center + (right * Mathf.Cos(a0)
                    + up * Mathf.Sin(a0)) * radius;
                Vector3 p1 = ring.center + (right * Mathf.Cos(a1)
                    + up * Mathf.Sin(a1)) * radius;
                ring.parts[k].gameObject.SetActive(true);
                SetLine(ring.parts[k], p0, p1, thickness);
            }
        }
        for (int i = pops.Count - 1; i >= 0; i--) {
            Spark pop = pops[i];
            pop.ageS += Time.unscaledDeltaTime;
            float t = pop.ageS / pop.lifeS;
            if (pop.transform == null || t >= 1f) {
                if (pop.transform != null) {
                    Destroy(pop.transform.gameObject);
                }
                pops.RemoveAt(i);
                continue;
            }
            if (pop.isStroke) {
                // a stroke thins away along its length
                Vector3 scaleNow = pop.transform.localScale;
                scaleNow.x = scaleNow.z = pop.size * (1f - t);
                pop.transform.localScale = scaleNow;
                pops[i] = pop;
                continue;
            }
            // up fast, then gone
            float scale = t < 0.4f
                ? Mathf.Lerp(0.2f, pop.size, t / 0.4f)
                : Mathf.Lerp(pop.size, 0f, (t - 0.4f) / 0.6f);
            pop.transform.localScale = Vector3.one * scale;
            pops[i] = pop;
        }
        UpdateTailTint();
    }

    // the tail glows gold through its swing, white at a clash
    private void UpdateTailTint() {
        if (tail == null) {
            return;
        }
        bool isFlash = Time.unscaledTime < tailFlashEndTime;
        bool isLit = isFlash || IsParryArmed();
        if (!isLit) {
            if (isTailTinted) {
                isTailTinted = false;
                foreach (Renderer part in tail.GetComponentsInChildren<Renderer>(true)) {
                    part.SetPropertyBlock(null);
                }
            }
            return;
        }
        isTailTinted = true;
        Color color = isFlash ? Color.white : goldenColor;
        foreach (Renderer part in tail.GetComponentsInChildren<Renderer>(true)) {
            SetColor(part, color);
        }
    }

    // the grinding sparks while a batted thing is held on the tail, & the
    // gold trail as it flies back
    private void ClashTrail(float clashS, Vector3 at, ref float nextFxS) {
        bool isHeld = clashS >= CLASH_IN_S && clashS < CLASH_IN_S + CLASH_HOLD_S;
        bool isOut = clashS >= CLASH_IN_S + CLASH_HOLD_S;
        if ((!isHeld && !isOut) || clashS < nextFxS) {
            return;
        }
        nextFxS = clashS + (isHeld ? 0.03f : 0.025f);
        if (isHeld) {
            BurstStreaks(at, new Color(1f, 0.6f, 0.15f), 3, 2f, 6f);
        } else {
            AddTrailBead(at);
        }
    }

    // a clash's share 0~1 of its slowing in, its hold & its speeding off,
    // all in real time
    private static float ClashIn(float clashS) =>
        Mathf.Clamp01(clashS / CLASH_IN_S);

    private static float ClashOut(float clashS) =>
        Mathf.Clamp01((clashS - CLASH_IN_S - CLASH_HOLD_S) / CLASH_OUT_S);

    // the swing bats things back from the double tap to the end of its
    // active part: its wind up is only the look
    private bool IsParryArmed() => tailTimeS >= 0f
        && tailTimeS < tailWindUpS + tailActiveS;

    // a lesson's stop never finds the monkey stuck in a web, where Space
    // would only struggle & the lesson could never be passed
    private void FreeForLesson() {
        if (struggle != null && struggle.IsTrapped) {
            struggle.ForceRelease();
        }
    }

    // the lesson: time stops as the wave turns gold, till the double tap
    private void FreezeForParry(Shot shot) {
        FreeForLesson();
        isTeachFrozen = true;
        frozenShot = shot;
        lastTapTime = -10f;
        Teach("Now!", "Tap SPACE 2 times to bounce it back", goldenColor,
            0f);
    }

    // a hit stop, a gold flash & sparks; the ball turns gold & flies back
    private void OnParry(Shot shot, Vector3 at) {
        hasSwingHit = true;
        shot.isReflected = true;
        shot.clashS = 0f;
        shot.clashFrom = at;
        shot.hasClashed = false;
        ParryEffect(at);
        if (!hasTaughtWeb) {
            hasTaughtWeb = true;
            hud.HideBanner();
        }
        // the lesson passed: the fight is on once it breaks out
        if (teach == TeachStep.Parry) {
            teach = TeachStep.Done;
            nextAttackTime = Time.time + parryStunS + 1f;
        }
    }

    // the volley it began is still fired out
    private void WebTheSpider() {
        // already stuck: more batted back bullets never add to it
        if (Time.time < stunEndTime) {
            Burst(body.Position, silkColor, 6);
            return;
        }
        stunEndTime = Time.time + parryStunS;
        CancelAttack();
        isDashing = false;
        StopSlashing();
        if (mode == Mode.Cocooning) {
            SetMode(Mode.Idle);
        }
        nextAttackTime = Mathf.Max(nextAttackTime, stunEndTime + 0.5f);
        hud.PopText("STUCK!", body.HeadPosition, goldenColor);
        hud.Say("Ugh! My own web!");
        Burst(body.Position, silkColor, 14);
        if (stuckWeb == null) {
            stuckWeb = Instantiate(webPrefab, body.Position, Quaternion.identity,
                transform).gameObject;
            MakeDecor(stuckWeb);
            stuckWeb.transform.localScale *= 1.5f;
        }
    }

    // the web wrapped round the stuck spider, gone once it breaks out
    private void UpdateStuckWeb(bool isStunned) {
        if (stuckWeb == null) {
            return;
        }
        if (!isStunned) {
            Destroy(stuckWeb);
            stuckWeb = null;
            return;
        }
        stuckWeb.transform.SetPositionAndRotation(
            PointOnTrunk(spiderAngle, spiderY, trunkRadiusU + 1.7f),
            Quaternion.Euler(0f, -spiderAngle, 0f)
        );
    }

    // the swing of the monkey's own tail: a quick swish side to side w/ a
    // flick up, back to its rest pose after
    private void StartTail() {
        if (tail != null && tailTimeS < 0f) {
            tailRest = tail.localRotation;
        }
        hasSwingHit = false;
        tailTimeS = 0f;
        SetTailColliders(false);
    }

    // the swinging tail's colliders are off, so the swing never shoves the
    // body or the grab; back on at rest
    private void SetTailColliders(bool isOn) {
        if (tail == null) {
            return;
        }
        foreach (Collider part in tail.GetComponentsInChildren<Collider>(true)) {
            part.enabled = isOn;
        }
    }

    // draws back, swishes hard across w/ a flick up, then swings back
    private void UpdateTail() {
        if (tailTimeS < 0f) {
            return;
        }
        tailTimeS += Time.unscaledDeltaTime;
        float total = tailWindUpS + tailActiveS + tailRecoverS;
        if (tailTimeS >= total) {
            tailTimeS = -1f;
            if (tail != null) {
                tail.localRotation = tailRest;
            }
            SetTailColliders(true);
            return;
        }
        if (tail == null) {
            return;
        }
        float swish;
        float flick;
        if (tailTimeS < tailWindUpS) {
            float t = tailTimeS / Mathf.Max(0.001f, tailWindUpS);
            swish = -45f * t;
            flick = -15f * t;
        } else if (tailTimeS < tailWindUpS + tailActiveS) {
            float t = (tailTimeS - tailWindUpS) / tailActiveS;
            float ease = 1f - (1f - t) * (1f - t);
            swish = Mathf.Lerp(-45f, 95f, ease);
            flick = -15f - 45f * Mathf.Sin(t * Mathf.PI);
        } else {
            float t = (tailTimeS - tailWindUpS - tailActiveS)
                / Mathf.Max(0.001f, tailRecoverS);
            swish = Mathf.Lerp(95f, 0f, t * t * (3f - 2f * t));
            flick = Mathf.Lerp(-15f, 0f, t);
        }
        tail.localRotation = tailRest
            * Quaternion.AngleAxis(flick, Vector3.right)
            * Quaternion.AngleAxis(swish, Vector3.up);
    }

    // the slow motion of a parry, & the stops of the lessons
    private void UpdateTimeScale(GameController game) {
        if (game.IsPaused || phase != Phase.Fighting) {
            return;
        }
        float scale = isTeachFrozen || Time.unscaledTime < clashStopEndTime
            ? 0f
            : Time.unscaledTime < parrySlowEndTime ? parrySlowTimeScale : 1f;
        if (scale < 1f || isTimeBent) {
            Time.timeScale = scale;
            isTimeBent = scale < 1f;
        }
    }

    // Sparks  -----------------------------------------------------------------
    private void Burst(Vector3 at, Color color, int count) {
        for (int i = 0; i < count; i++) {
            Transform spark = CreatePrimitive(PrimitiveType.Cube, "Spark",
                Vector3.one * 0.18f, color).transform;
            spark.position = at;
            sparks.Add(new Spark {
                transform = spark,
                velocity = Random.onUnitSphere * Random.Range(3f, 7f),
                lifeS = Random.Range(0.4f, 0.8f),
            });
        }
    }

    private void UpdateSparks() {
        for (int i = sparks.Count - 1; i >= 0; i--) {
            Spark spark = sparks[i];
            spark.ageS += Time.unscaledDeltaTime;
            if (spark.transform == null || spark.ageS >= spark.lifeS) {
                if (spark.transform != null) {
                    Destroy(spark.transform.gameObject);
                }
                sparks.RemoveAt(i);
                continue;
            }
            if (!spark.isFloating) {
                spark.velocity += Vector3.down * 9f * Time.unscaledDeltaTime;
            }
            spark.transform.position += spark.velocity * Time.unscaledDeltaTime;
            float left = 1f - spark.ageS / spark.lifeS;
            if (spark.isStreak) {
                if (spark.velocity.sqrMagnitude > 0.0001f) {
                    spark.transform.rotation =
                        Quaternion.LookRotation(spark.velocity.normalized);
                }
                spark.transform.localScale = new Vector3(0.07f, 0.07f,
                    0.1f + 0.06f * spark.velocity.magnitude) * left;
            } else {
                float size = spark.size > 0f ? spark.size : 0.18f;
                spark.transform.localScale = Vector3.one * size * left;
            }
            sparks[i] = spark;
        }
    }

    // Teaching  ---------------------------------------------------------------
    // the one lesson, the parry: slowed waves till one is batted back; no
    // other attack meanwhile & a lesson never kills (only the guard below)
    private float UpdateTeach() {
        teachTimeS += Time.deltaTime;
        if (teach == TeachStep.Choose) {
            UpdateChoiceTags();
            return 0f;
        }
        bool isTrapped = struggle != null && struggle.IsTrapped;
        if (isTrapped) {
            nextAttackTime = Time.time + teachGapS;
            return 0f;
        }
        bool isFree = !HasLiveShots;
        switch (teach) {
            case TeachStep.Parry:
                if (Time.time >= nextAttackTime && isFree) {
                    BeginAttack(Attack.Web, true);
                }
                return 0f;
            case TeachStep.Slash:
                bool isSlashFree = !isSlashing
                    && !crescents.Exists(c => !c.isReflected);
                if (Time.time >= nextAttackTime && isFree && isSlashFree) {
                    Teach("Bounce it back too!", "", warnColor, 0f);
                    StartSlash(true);
                }
                return 0f;
            case TeachStep.Final:
            case TeachStep.Ready:
                float waitS = teach == TeachStep.Ready ? readyS : finalS;
                if (teachTimeS >= waitS) {
                    teach = TeachStep.Done;
                    hud.HideBanner();
                    nextAttackTime = Time.time + 1.5f;
                }
                return 0f;
        }
        return 0f;
    }

    private void AdvanceTeach(TeachStep next) {
        teach = next;
        teachTimeS = 0f;
        nextAttackTime = Time.time + teachGapS;
        hud.SetPrompt(false, Vector3.zero);
        ShowStepBanner();
    }

    private void ShowStepBanner() {
        switch (teach) {
            case TeachStep.Parry:
                Teach(bossName, "", warnColor, 2f);
                break;
        }
    }

    private void Teach(string title, string line, Color color, float seconds) {
        hud.ShowBannerFor(title, line, color, seconds);
    }

    private void LethalHit(string word, bool isTouch = false,
        bool isDash = false) {
        if (phase != Phase.Fighting || isReturningToStart
            || Time.time < saveGraceEndTime) {
            return;
        }
        // till the first parry is learnt nothing of the spider's hurts
        if (teach == TeachStep.Parry) {
            return;
        }
        // a touch shows it must not be touched; saved, w/ its words
        if (isTouch) {
            ShowTouchWarning(HasArmor());
        }
        // the armor saves as in the Cobra King's arena: "Saved!" & the usual
        // knock down; the spider goes on
        if (TrySpendArmor()) {
            if (mode == Mode.Cocooning) {
                SetMode(Mode.Idle);
                nextAttackTime = Time.time + RestNow();
            }
            if (isDash) {
                ThrowOffDash();
            } else if (isTouch) {
                ThrowOffSpider();
            } else {
                KnockDown(body.Contact);
            }
            return;
        }
        StartFinisher(false);
    }

    // it must never be touched: it bites, red rings & sparks burst off the
    // monkey & "Don't touch it!" shows
    private void ShowTouchWarning(bool isSaved) {
        biteEndTime = Time.unscaledTime + BITE_S;
        Vector3 at = monkey.position;
        AddRing(at, warnColor, 0f);
        AddRing(at, warnColor, 0.08f);
        BurstStreaks(at, warnColor, 16, 5f, 12f);
        if (isSaved) {
            hud.PopText("Don't touch it!", body.HeadPosition, warnColor);
        }
    }

    // saved from a touch: the monkey is thrown well off the spider, out of
    // any web, untouchable on the way
    private void ThrowOffSpider() {
        if (monkeyController == null) {
            return;
        }
        if (struggle != null) {
            struggle.ForceRelease();
        }
        Vector3 at = monkey.position;
        float awayDeg = Mathf.DeltaAngle(spiderAngle, AngleOf(at));
        float sign = awayDeg >= 0f ? 1f : -1f;
        float throwDeg = TOUCH_THROW_U / trunkRadiusU * Mathf.Rad2Deg;
        float y = at.y + Mathf.Sign(at.y - spiderY) * 2f;
        returnTarget = PointOnTrunk(AngleOf(at) + sign * throwDeg,
            Mathf.Clamp(y, entryY, entryY + arenaHeightU),
            HorizontalDistance(at));
        SetUntouchable(true);
        monkeyController.ClearStun();
        monkeyController.IsHeld = true;
        monkeyController.PullTo(returnTarget, reviveReturnSpeedU);
        isReturningToStart = true;
    }

    // saved from a dash: thrown out sideways off its lane, the side it was
    // caught on (the other if the arena's top or floor is in the way), so the
    // spider running on along it never touches it again
    private void ThrowOffDash() {
        if (monkeyController == null) {
            return;
        }
        if (struggle != null) {
            struggle.ForceRelease();
        }
        Vector3 at = monkey.position;
        float mu = Mathf.DeltaAngle(dashStartAngle, AngleOf(at))
            * Mathf.Deg2Rad * trunkRadiusU;
        float mv = at.y - dashStartY;
        float side = dashWayU * mv - dashWayV * mu >= 0f ? 1f : -1f;
        float lowY = entryY;
        float highY = entryY + arenaHeightU - 0.5f;
        float sideY = at.y + side * dashWayU * DASH_THROW_U;
        // too little room that way: the other side
        if (sideY < lowY || sideY > highY) {
            float otherY = at.y - side * dashWayU * DASH_THROW_U;
            if (otherY >= lowY && otherY <= highY) {
                side = -side;
            }
        }
        float throwU = -side * dashWayV * DASH_THROW_U;
        float throwV = side * dashWayU * DASH_THROW_U;
        returnTarget = PointOnTrunk(
            AngleOf(at) + throwU / trunkRadiusU * Mathf.Rad2Deg,
            Mathf.Clamp(at.y + throwV, lowY, highY),
            HorizontalDistance(at));
        SetUntouchable(true);
        monkeyController.ClearStun();
        monkeyController.IsHeld = true;
        monkeyController.PullTo(returnTarget, reviveReturnSpeedU);
        isReturningToStart = true;
    }

    // the Cobra King's leftover armor, if worn; frees the monkey too
    private bool TrySpendArmor() {
        BossFightController cobraKing = BossFightController.I;
        if (cobraKing == null || !cobraKing.TrySpendArmor()) {
            return false;
        }
        saveGraceEndTime = Time.time + saveGraceS;
        // the golden melon waits a short while after the armor goes, so it
        // never pops up at once beside the monkey, eg back at the start;
        // after that, unpicked, its full interval
        nextGoldenTime = Time.time;
        goldenSinceArmor = 0;
        if (struggle != null) {
            struggle.ForceRelease();
        }
        return true;
    }

    private void StartFinisher(bool isWin) {
        phase = Phase.Finishing;
        isFinisherWin = isWin;
        finisherTimeS = 0f;
        CancelAttack();
        hud.HideBanner();
        hud.ClearCallOut();
        hud.ClearWarnings();
        hud.SetPrompt(false, Vector3.zero);
        isTimeBent = false;
        isTeachFrozen = false;
        frozenShot = null;
        frozenCrescent = null;
        parrySlowEndTime = 0f;
        isDashing = false;
        ClearSlash();
        Time.timeScale = finisherTimeScale;
        if (isWin) {
            hud.Flash(Color.white);
            return;
        }
        hud.Flash(new Color(0.9f, 0.1f, 0.1f));
        hud.ShowBanner("K.O.", new Color(0.95f, 0.15f, 0.15f));
        SetUntouchable(true);
        if (struggle != null) {
            struggle.ForceRelease();
        }
        if (monkeyController != null) {
            monkeyController.ClearHeightCeiling();
            Vector3 outward = monkey.position - trunkCenter;
            outward.y = 0f;
            monkeyController.IsHeld = true;
            monkeyController.PullTo(
                monkey.position + outward.normalized * 6f + Vector3.down * 3f
            );
        }
    }

    // runs on unscaled time, since the game itself crawls meanwhile
    private void UpdateFinisher() {
        finisherTimeS += Time.unscaledDeltaTime;
        if (!isFinisherWin) {
            monkey.Rotate(Vector3.up, 900f * Time.unscaledDeltaTime);
        }
        if (finisherTimeS < finisherRealS) {
            return;
        }
        Time.timeScale = 1f;
        if (isFinisherWin) {
            Defeat();
            return;
        }
        OfferRevive();
    }

    private void OfferRevive() {
        phase = Phase.ReviveOffer;
        reviveLeftS = reviveOfferS;
        Time.timeScale = 0f;
        hud.HideBanner();
        hud.ShowReviveOffer();
        hud.SetReviveCountdown(Mathf.CeilToInt(reviveLeftS));
    }

    private void UpdateRevive(GameController game) {
        Time.timeScale = 0f;
        if (game.IsPaused) {
            return;
        }
        float stepS = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if (phase == Phase.WatchingAd) {
            adElapsedS += stepS;
            bool canSkip = adElapsedS >= adSkipAfterS;
            hud.SetAdSkippable(canSkip);
            if (canSkip && WasPressed(confirmAction)) {
                SkipAd();
            }
            return;
        }
        reviveLeftS -= stepS;
        hud.SetReviveCountdown(Mathf.CeilToInt(Mathf.Max(0f, reviveLeftS)));
        if (WasPressed(confirmAction)) {
            WatchAd();
        } else if (WasPressed(giveUpAction) || reviveLeftS <= 0f) {
            GiveUp();
        }
    }

    private void WatchAd() {
        if (phase != Phase.ReviveOffer) {
            return;
        }
        phase = Phase.WatchingAd;
        adElapsedS = 0f;
        hud.SetAdSkippable(false);
        hud.ShowAd();
    }

    private void GiveUp() {
        if (phase != Phase.ReviveOffer) {
            return;
        }
        hud.HideRevive();
        Time.timeScale = 1f;
        phase = Phase.Over;
        SetUntouchable(false);
        if (GameController.I != null) {
            GameController.I.LoseGame();
        }
    }

    private void SkipAd() {
        if (phase != Phase.WatchingAd || adElapsedS < adSkipAfterS) {
            return;
        }
        RestartFight();
    }

    private void OpenCoursePage() {
        if (phase == Phase.WatchingAd && !string.IsNullOrEmpty(courseUrl)) {
            Application.OpenURL(courseUrl);
        }
    }

    // after the ad the fight starts over at full health; a spent armor
    // stays spent
    private void RestartFight() {
        hud.HideRevive();
        Time.timeScale = 1f;
        phase = Phase.Fighting;
        // a new try starts in the armor too, swirled on
        GiveArmor();
        ClearThrows();
        health = maxHealth;
        hud.SetHealth(1f);
        foreach (Melon melon in melons) {
            if (melon.root != null) {
                melon.root.SetActive(true);
                melon.backTime = -1f;
            }
        }
        ClearStickyWebs();
        // the last try's golden melon goes; the next comes as at the start
        HideGoldenMelon();
        nextGoldenTime = Time.time + GOLDEN_FIRST_S;
        spiderAngle = startAngle;
        spiderY = startY;
        ResetSpiderState();
        teachTimeS = 0f;
        // as in the Cobra King's arena: two melons, a plain EASY & a golden
        // NORMAL; the first grabbed picks, & the fight waits till then
        isEasy = false;
        maxHealth = worldBalanceConfig.SpiderMaxHealth;
        health = maxHealth;
        teach = TeachStep.Choose;
        SpawnChoices();
        Teach("Easy or Normal?", "Grab a melon", goldenColor, 0f);
        StaminaBarController stamina = StaminaBarController.I;
        if (stamina != null) {
            stamina.AddStamina(stamina.MaxStamina);
        }
        if (greatCobra != null) {
            greatCobra.KnockBack(0f);
        }
        monkey.rotation = entryRotation;
        ReturnToStart();
        hud.Flash(silkColor);
        hud.Say("Back for more?");
    }

    private void ReturnToStart() {
        if (monkeyController == null) {
            return;
        }
        if (struggle != null) {
            struggle.ForceRelease();
        }
        SetUntouchable(true);
        monkeyController.SetHeightCeiling(entryY + arenaHeightU);
        monkeyController.ClearStun();
        monkeyController.IsHeld = true;
        returnTarget = entryPosition;
        monkeyController.PullTo(returnTarget, reviveReturnSpeedU);
        isReturningToStart = true;
    }

    private void UpdateReturnToStart() {
        if (!isReturningToStart || monkeyController == null) {
            return;
        }
        if (
            monkeyController.HasReachedPullTarget
            || (monkey.position - returnTarget).sqrMagnitude < 0.01f
        ) {
            isReturningToStart = false;
            monkeyController.SyncOrbitAngle();
            monkeyController.IsHeld = false;
            SetUntouchable(false);
        }
    }

    // Win  --------------------------------------------------------------------
    private void Defeat() {
        phase = Phase.Defeated;
        defeatTimeS = 0f;
        hud.SetHealth(0f);
        hud.ClearWarnings();
        ClearShots();
        ClearThrows();
        ClearStickyWebs();
        Burst(body.Position, silkColor, 20);
        hud.Say("My beautiful web...");
        if (Debug.isDebugBuild) {
            Debug.Log("SpiderFightController:\tSpider King defeated", this);
        }
    }

    private void UpdateDefeat() {
        defeatTimeS += Time.deltaTime;
        body.Fall(defeatTimeS);
        if (defeatTimeS < defeatFallS) {
            return;
        }
        body.Clear();
        phase = Phase.Rewarding;
        // the gloves: every web from now on breaks twice as fast
        if (struggle != null) {
            struggle.EscapeGainScale = glovesEscapeGainScale;
        }
        hud.HideBossBar();
        hud.Flash(silkColor);
        hud.ShowRewardPanel(winTitle, winLine, winDetailLine);
    }

    // time stops on the reward panel till the player goes on
    private void UpdateRewardPause(GameController game) {
        Time.timeScale = 0f;
        if (game.IsPaused) {
            return;
        }
        // only once the win has played in, so a Space mashed in the fight
        // never skips it
        if (hud.IsRewardReady && WasPressed(confirmAction)) {
            ContinueAfterReward();
        }
    }

    private void ContinueAfterReward() {
        if (phase != Phase.Rewarding) {
            return;
        }
        hud.HideRewardPanel();
        Time.timeScale = 1f;
        phase = Phase.Done;
        ClearMelons();
        ClearChoices();
        ClearSkyWeb();
        EndBareArena();
        if (greatCobra != null) {
            greatCobra.StopGuard();
            greatCobra.KnockBack(winCobraDropU);
        }
        if (monkeyController != null) {
            monkeyController.ClearHeightCeiling();
        }
        if (cameraFraming != null) {
            cameraFraming.SetBossFraming(0f, 1f);
        }
        hud.ShowBannerFor("The cobra is coming!", "Climb!",
            new Color(0.95f, 0.25f, 0.2f), 2.5f);
        FightEnded?.Invoke();
        if (Debug.isDebugBuild) {
            Debug.Log(
                "SpiderFightController:\twon, silk gloves given, climb on",
                this
            );
        }
    }

    // Camera & Helpers  -------------------------------------------------------
    // pulled back, aimed a little toward the spider, so both stay in view
    private void UpdateCamera() {
        // the parry close up is its own camera move (ParryShot), on top,
        // cut short once another attack comes
        CutShotIfMoreComes();
        if (cameraFraming == null) {
            return;
        }
        float focus = Mathf.Clamp((spiderY - monkey.position.y) * 0.3f, 0f, 3f);
        cameraFraming.SetBossFraming(focus, cameraDistanceScale);
    }

    // if the action is wired & went down this frame; it is shared, so it is
    // switched back on here at the moment of use
    private static bool WasPressed(InputActionReference reference) {
        if (reference == null) {
            return false;
        }
        InputAction action = reference.action;
        if (!action.enabled) {
            action.Enable();
        }
        return action.WasPressedThisFrame();
    }

    private static void SetUntouchable(bool isUntouchable) {
        if (GameController.I != null) {
            GameController.I.SetLeaping(isUntouchable);
        }
    }

    // the usual stun & drop w/ the heavier shake, never fatal
    private static void KnockDown(Collider source) {
        HitBranchSensor sensor = HitBranchSensor.I;
        if (sensor != null && source != null) {
            sensor.ReportHit(
                source,
                HitBranchSensor.HitKind.FallingCobraController
            );
        }
    }

    private static void ForgetFruits(GameObject branch) {
        UpwardFruitSensor sensor = UpwardFruitSensor.I;
        if (sensor == null || branch == null) {
            return;
        }
        foreach (Collider part in branch.GetComponentsInChildren<Collider>(true)) {
            sensor.RemoveFruit(part);
        }
    }

    private float SurfaceDistance(float angleA, float yA, float angleB, float yB) {
        float arcU = Mathf.DeltaAngle(angleA, angleB) * Mathf.Deg2Rad
            * trunkRadiusU;
        float dy = yB - yA;
        return Mathf.Sqrt(arcU * arcU + dy * dy);
    }

    private float HorizontalDistance(Vector3 position) {
        Vector3 offset = position - trunkCenter;
        offset.y = 0f;
        return offset.magnitude;
    }

    private float AngleOf(Vector3 position) {
        Vector3 offset = position - trunkCenter;
        return Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    private Vector3 PointOnTrunk(float angleDeg, float y, float radius) {
        float radians = angleDeg * Mathf.Deg2Rad;
        return new Vector3(
            trunkCenter.x + Mathf.Cos(radians) * radius,
            y,
            trunkCenter.z + Mathf.Sin(radians) * radius
        );
    }

    // a decor primitive in the body's lit material, no collider
    private GameObject CreatePrimitive(
        PrimitiveType type,
        string objectName,
        Vector3 scale,
        Color color
    ) {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = objectName;
        DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(transform, true);
        part.transform.localScale = scale;
        Renderer partRenderer = part.GetComponent<Renderer>();
        if (body.PrimitiveMaterial != null) {
            partRenderer.sharedMaterial = body.PrimitiveMaterial;
        }
        SetColor(partRenderer, color);
        return part;
    }

    private void SetColor(Renderer target, Color color) {
        if (target == null) {
            return;
        }
        target.GetPropertyBlock(colorBlock);
        colorBlock.SetColor(BASE_COLOR, color);
        colorBlock.SetColor(COLOR, color);
        target.SetPropertyBlock(colorBlock);
    }

    private static Transform FindChild(Transform root, string childName) {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) {
            if (child.name == childName) {
                return child;
            }
        }
        return null;
    }

    // a cylinder stretched b/t two world points
    private static void SetLine(Transform line, Vector3 from, Vector3 to,
        float thickness) {
        Vector3 span = to - from;
        if (span.sqrMagnitude < 0.000001f) {
            return;
        }
        line.SetPositionAndRotation(
            (from + to) * 0.5f,
            Quaternion.FromToRotation(Vector3.up, span.normalized)
        );
        line.localScale = new Vector3(thickness, span.magnitude * 0.5f,
            thickness);
    }

    // a prefab copy kept only as a picture: no collider, no script
    private static void MakeDecor(GameObject decor) {
        foreach (Collider part in decor.GetComponentsInChildren<Collider>(true)) {
            DestroyImmediate(part);
        }
        // at once, so none wakes up when the copy is switched on
        foreach (MonoBehaviour script in
            decor.GetComponentsInChildren<MonoBehaviour>(true)) {
            DestroyImmediate(script);
        }
    }

    // private members  ########################################################
    private const string FRUIT_TAG = "FruitCollider";
    private const int PATH_PARTS = 14;
    private const int RING_PARTS = 20;
    // how far the monkey's body edge is from its middle: a parry is timed
    // to the edge an attack comes at; u
    private const float MONKEY_EDGE_U = 0.7f;
    // the sky web shows once the monkey is this close below it; u
    private const float SKY_SHOW_U = 6f;
    // the air wall's width, out past the view, so it seems endless; u
    private const float SKY_WIDE_U = 1200f;
    // how far a touch the armor saves throws the monkey off; u
    private const float TOUCH_THROW_U = 7f;
    // how far a dash the armor saves throws the monkey off its lane; u
    private const float DASH_THROW_U = 8f;
    private const float MIN_VOLLEY_GAP_S = 0.6f;
    // its circling speed b/t attacks, a share of its stalk speed
    private const float PROWL_SHARE = 0.4f;
    // volleys a stage of volleys opens w/, b4 any slash
    private const int VOLLEYS_FIRST = 2;
    // how far in from the screen's edges the spider must be to count as in
    // view for a ranged attack, a share of the screen
    private const float VIEW_EDGE = 0.18f;
    // seconds into the fight b4 the first golden melon may show
    private const float GOLDEN_FIRST_S = 8f;
    // seconds after the armor goes b4 the golden melon may show
    private const float GOLDEN_AFTER_ARMOR_S = 5f;
    private const float BITE_S = 0.4f; // real s of the touch bite
    // the clash: time stops dead this long at the touch; the camera shakes
    private const float CLASH_STOP_S = 0.07f;
    private const float SHAKE_S = 0.22f;
    private const float RING_S = 0.35f; // real s a shock ring opens
    private const int CRESCENT_PARTS = 12;
    // the clash of a parry, real s: slowing into the tail, held, speeding off
    private const float CLASH_IN_S = 0.12f;
    private const float CLASH_HOLD_S = 0.14f;
    private const float CLASH_OUT_S = 0.3f;
    // the parry shot, real s: easing in, easing out after the slow motion,
    // & the slam at the clash
    private const float SHOT_IN_S = 0.14f;
    private const float SHOT_OUT_S = 0.35f;
    private const float SHOT_SLAM_S = 0.25f;
    // a parry w/ more coming: a short slow beat & the camera only this far
    // into its shot, 0~1
    private const float QUICK_SLOW_S = 0.25f;
    private const float QUICK_SHOT_WEIGHT = 0.3f;
    private const int CRESCENT_STRANDS = 3;
    private const int SIGIL_PARTS = 9; // the slash charge's red blade sign
    private const int CRESCENT_SPOKES = 5;
    // how far a crescent's middle runs ahead of its tips; u
    private const float CRESCENT_BULGE_U = 0.9f;
    // the banana hangs about this far below its branch root; u
    private const float BANANA_HANG_U = 1f;
    // the spider starts right above the monkey, this far up, in view
    private const float START_TURN_DEG = 0f;
    private const float START_ABOVE_U = 7f;
    private static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
    private static readonly int BASE_MAP = Shader.PropertyToID("_BaseMap");
    private static readonly int OUTLINE_COLOR =
        Shader.PropertyToID("_Outline_Color");
    private static readonly int OUTLINE_WIDTH =
        Shader.PropertyToID("_Outline_Width");
    private static readonly int COLOR = Shader.PropertyToID("_Color");

    private enum Phase {
        Waiting, // climbing to the trigger height
        Fighting,
        Finishing, // the slow last blow, either way
        ReviveOffer,
        WatchingAd,
        Defeated, // the beaten spider falls
        Rewarding, // the reward panel, time stopped
        Done, // won, the climb goes on
        Over, // lost w/o a revive
    }

    private enum Mode {
        Idle, // stalks the monkey & attacks
        Cocooning, // crawls to a trapped monkey
    }

    private enum Attack {
        None,
        Web,
        Dash,
    }

    private enum ShotKind {
        Web, // a fast white ball along a silk line w/ a gold end, parried
    }

    private enum TeachStep {
        Choose, // after a revive: EASY or NORMAL, nothing attacks
        Parry, // bat a slowed web bullet back
        Slash, // bat a slowed slash back
        Final, // "Go!", then the fight
        Ready, // the calm after a revive
        Done,
    }

    private class Crescent {
        public float originAngle; // where the spider slashed from
        public float originY;
        public float directionDeg; // its way on the bark, 0 round the trunk
        public float distanceU; // how far it has flown
        public Transform[] parts;
        public bool isReflected; // batted back at the spider
        public float clashS; // real s since it was batted back
        public bool hasClashed;
        public float nextFxS; // clash time of its next spark or trail bead
        public float parryDistanceU;
        public float contactDistanceU;
    }

    private class LaidWeb {
        public SpiderWebTrap web;
        public float endTime; // Time.time it goes
        public Vector3 restScale;
    }

    private class Melon {
        public bool isGolden;
        public GameObject root; // the grabbable fruit
        public GameObject branch;
        public Collider trigger;
        public float backTime; // Time.time it grows back, -1 if hanging
    }

    private class Throw {
        public Transform transform;
        public Vector3 from;
        public float ageS;
    }

    private class Shot {
        public ShotKind kind;
        public Transform ball;
        public Transform wave; // a web shot's silk bullet
        public Vector3 from;
        public Vector3 target;
        public float t; // 0 at the spider, 1 at the target
        public float length; // path length; u
        public float fill; // share of the wind up done
        public bool isCharging; // still winding up, not flying
        public bool isReflected; // batted back at the spider
        public float clashS; // real s since it was batted back
        public Vector3 clashFrom;
        public bool hasClashed;
        public float nextFxS;
        public Vector3 lastPosition;
    }

    private struct Spark {
        public Transform transform;
        public Vector3 velocity;
        public float ageS;
        public float lifeS;
        public bool isFloating; // no gravity, eg drawn in to the charge
        public bool isStreak; // long & thin along its flight
        public bool isStroke; // a clash stroke, thinning
        public float size; // its start size, 0 for the usual
    }

    private class Ring {
        public Vector3 center;
        public Color color;
        public float ageS; // < 0 while it waits to show
        public bool isInward; // closing in, eg on the charging head
        public Transform[] parts;
    }

    private SpiderKingBody body;
    private BossFightHud hud;
    private MaterialPropertyBlock colorBlock;
    private Transform monkey;
    private MonkeyController monkeyController;
    private CobraMotor greatCobra;
    private CameraClimbFraming cameraFraming;
    private SpiderWebStruggle struggle;
    private Vector3 trunkCenter;
    private float entryY;
    private Vector3 entryPosition;
    private Vector3 returnTarget; // where a fly back ends
    private Quaternion entryRotation;

    private Phase phase = Phase.Waiting;
    private Mode mode = Mode.Idle;
    private float modeTimeS;
    private Attack attack = Attack.None;
    private float attackTimeS;
    private float attackWindUpS = 1f;
    private float nextAttackTime;
    private Vector3 aimPoint;
    private int warningId = -1;
    private Shot chargingShot;
    private int health;
    private int maxHealth;

    private float previewAngle;
    private float previewY;
    private float startAngle;
    private float startY;
    private float spiderAngle;
    private float spiderY;
    private float stunEndTime;
    private float saveGraceEndTime;

    private readonly List<Melon> melons = new List<Melon>();
    private Melon goldenMelon;
    private Melon goldenTwin; // the second golden melon, shown w/ it
    private Melon choiceEasy;
    private Melon choiceNormal;
    private bool isEasy;
    private bool hasTaughtWeb; // the first bullet has stopped time to teach
    private bool hasTaughtSlash; // & the first slash
    private float nextGoldenTime;
    private float goldenHideTime;
    private Vector3 goldenRestScale;
    private readonly List<Throw> throws = new List<Throw>();
    private readonly List<Shot> shots = new List<Shot>();
    private readonly List<Spark> sparks = new List<Spark>();
    private readonly List<Spark> pops = new List<Spark>();
    private readonly List<Ring> rings = new List<Ring>();
    private float tailFlashEndTime;
    private Transform skyDisc; // the arena's top, null when none
    private Transform skyPatch; // its clearer patch round the monkey
    private Material skyMaterial;
    private float nextSkyRippleTime;
    private float clashStopEndTime; // real time the clash's dead stop ends
    private float shakeEndTime;
    private float biteEndTime; // real time the touch bite ends
    private bool isTailTinted;
    private readonly List<LaidWeb> stickyWebs = new List<LaidWeb>();
    private float layDistanceU;

    private bool isDashing;
    private bool hasDashHit;
    private float dashTimeS;
    private float nextDashTime;
    private float dashAngle;
    private float dashY;
    private Transform[] dashLane; // its red lane, null when none
    private float dashStartAngle;
    private float dashStartY;
    private float dashWayU; // unit way on the bark, round the trunk
    private float dashWayV; // & up
    private float dashLengthU;
    private float dashTravelU;
    private int volleyLeft;
    private int volleyGapIndex;
    private bool isVolleySlowFirst;
    private float nextVolleyTime;
    private int lastStage;
    private Shot volleyCharge; // a volley's next bullet, charging
    private float callOutEndTime; // real time it is done calling out
    private float volleyChargeFromTime;
    private int goldenSinceArmor; // golden melons missed since the armor went
    private int websBeforeSlash; // volleys still due b4 a slash may come
    private bool isSlashing;
    private bool isSlashAiming;
    private int slashLeft;
    private Transform[] slashSigil; // the slash charge's sign, null if none
    private int slashGapIndex;
    private bool isSlashSlowFirst;
    private float nextSlashTime;
    private float slashTimeS;
    private float slashAimDeg; // its aim on the bark, 0 round the trunk
    private Transform[] slashAim; // the red aim line, null when none
    private readonly List<Crescent> crescents = new List<Crescent>();
    private float slashChargeLeftS;
    private float slashSlow = 1f; // the lesson's slower slash
    private Crescent frozenCrescent; // the lesson's frozen slash

    private bool hasBeenTrapped;

    private float lastTapTime = -10f;
    private float parrySlowEndTime; // real time the parry slow motion ends
    private float prowlSign = 1f; // which way it circles the monkey
    private float nextProwlFlipTime;
    // the parry shot, real times
    private float shotStartTime;
    private float shotEndTime;
    private float shotMaxWeight = 1f; // how far in the shot may go, 0~1
    private float shotWeightCap = 1f; // eased toward that
    private float shotClashTime = -10f;
    private float shotSide = 1f; // which way it swings round the monkey
    private Vector3 shotFrom; // where the batted thing came from
    private bool isShotApplied; // the camera holds the shot till drawn
    private Quaternion shotViewRotation; // as last drawn in the shot
    private bool hasShotPose;
    private Vector3 restEyePosition;
    private Quaternion restEyeRotation;
    private float restFov;
    private Transform tail; // the monkey's own tail, swung for a parry
    private Quaternion tailRest;
    private float chargeSparkS;
    private float chargeRingS;
    private float tailTimeS = -1f;
    private bool hasSwingHit; // the current swing batted something back
    private GameObject stuckWeb;
    private bool isTimeBent;
    private bool isTeachFrozen; // the parry lesson stops time on a gold wave
    private Shot frozenShot;
    private float bareBottomY;
    private float bareTopY;

    private TeachStep teach = TeachStep.Parry;
    private float teachTimeS;

    private bool isFinisherWin;
    private float finisherTimeS;
    private float reviveLeftS;
    private float adElapsedS;
    private bool isReturningToStart;
    private float defeatTimeS;
}
