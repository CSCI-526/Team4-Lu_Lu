using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Every Inspector-tuned value of the Cobra King fight, as the Inspector
/// shows them. Read through the properties; no role writes one.
/// </summary>
[System.Serializable]
public sealed class CobraKingTuning {
    // Public Members  #########################################################
    public WorldBalanceConfig WorldBalanceConfig => worldBalanceConfig;
    public SpiderKingFightController SpiderFightPrefab => spiderFightPrefab;
    public InputActionReference ConfirmAction => confirmAction;
    public InputActionReference GiveUpAction => giveUpAction;
    public string BossName => bossName;
    public float NestHeightU => nestHeightU;
    public float GuardDepthU => guardDepthU;
    public float RetreatLimitU => retreatLimitU;
    public float NestClearanceU => nestClearanceU;
    public float TrunkRadiusU => trunkRadiusU;
    public float CameraDistanceScale => cameraDistanceScale;
    public float CameraFocusShare => cameraFocusShare;
    public float MaxCameraFocusU => maxCameraFocusU;
    public float CeilingGapU => ceilingGapU;
    public float PushRangeU => pushRangeU;
    public float PushWindUpS => pushWindUpS;
    public float PushCooldownS => pushCooldownS;
    public float SweepFromLostShare => sweepFromLostShare;
    public float RestS => restS;
    public float RestFinalS => restFinalS;
    public float FinalWarnScale => finalWarnScale;
    public float BiteWarnS => biteWarnS;
    public float BiteRadiusU => biteRadiusU;
    public float SweepWarnS => sweepWarnS;
    public float SweepTimeS => sweepTimeS;
    public float SweepBandHalfU => sweepBandHalfU;
    public int TailBeadCount => tailBeadCount;
    public int MinionCount => minionCount;
    public float MinionSpreadDeg => minionSpreadDeg;
    public float MinionStaggerS => minionStaggerS;
    public float MinionStreamGapS => minionStreamGapS;
    public float FinaleOverlapS => finaleOverlapS;
    public int MinionWaves => minionWaves;
    public float MinionWarnS => minionWarnS;
    public float MinionFallSpeedU => minionFallSpeedU;
    public float MinionHitRadiusU => minionHitRadiusU;
    public SpiderWebTrap WebPrefab => webPrefab;
    public int WebCount => webCount;
    public int WebsPerThird => websPerThird;
    public float WebBelowEntryU => webBelowEntryU;
    public float WebSeparationU => webSeparationU;
    public float WebBranchGapU => webBranchGapU;
    public int LowWebAttemptsPer10U => lowWebAttemptsPer10U;
    public float LowWebChance => lowWebChance;
    public float WebEscapeGainScale => webEscapeGainScale;
    public float WebFruitGapDeg => webFruitGapDeg;
    public GameObject BananaBranchPrefab => bananaBranchPrefab;
    public GameObject BranchEmptyPrefab => branchEmptyPrefab;
    public Mesh WatermelonMesh => watermelonMesh;
    public Vector3 WatermelonScale => watermelonScale;
    public Vector3 WatermelonOffset => watermelonOffset;
    public int BananaCount => bananaCount;
    public int BananasPerThird => bananasPerThird;
    public Vector2 FruitHeightRangeU => fruitHeightRangeU;
    public float BranchRadiusU => branchRadiusU;
    public int ArenaBranchAttemptsPer10U => arenaBranchAttemptsPer10U;
    public float ArenaBranchChance => arenaBranchChance;
    public float ArenaBranchFruitChance => arenaBranchFruitChance;
    public float ArenaBranchSeparationU => arenaBranchSeparationU;
    public float ArenaBranchStartClearU => arenaBranchStartClearU;
    public float BananaRespawnS => bananaRespawnS;
    public Vector2 BananaRespawnScaleByThird => bananaRespawnScaleByThird;
    public float ThrowTimeS => throwTimeS;
    public float ThrowArcU => throwArcU;
    public float ThrownBananaScale => thrownBananaScale;
    public Color TrailColor => trailColor;
    public float MarkerFigureScale => markerFigureScale;
    public Material RoughBarkMaterial => roughBarkMaterial;
    public int RoughBarkAttemptsPer10U => roughBarkAttemptsPer10U;
    public float RoughBarkChance => roughBarkChance;
    public Vector2 RoughBarkHeightRangeU => roughBarkHeightRangeU;
    public Vector2 RoughBarkArcRangeDeg => roughBarkArcRangeDeg;
    public string GoldenHintTitle => goldenHintTitle;
    public string GoldenHintLine => goldenHintLine;
    public float GoldenHintS => goldenHintS;
    public Material GoldenOutlineMaterial => goldenOutlineMaterial;
    public Color GoldenOutlineColor => goldenOutlineColor;
    public float GoldenOutlineWidthScale => goldenOutlineWidthScale;
    public string ArmorIntroTitle => armorIntroTitle;
    public string ArmorIntroLine => armorIntroLine;
    public bool CanRevive => canRevive;
    public float ReviveOfferS => reviveOfferS;
    public float AdSkipAfterS => adSkipAfterS;
    public float ReviveReturnSpeedU => reviveReturnSpeedU;
    public string CourseUrl => courseUrl;
    public float ChoiceSideDeg => choiceSideDeg;
    public float ChoiceAboveU => choiceAboveU;
    public string ChooseLine => chooseLine;
    public float EasyHealthScale => easyHealthScale;
    public float EasyWarnScale => easyWarnScale;
    public float EasyRestS => easyRestS;
    public float EasyRestFinalS => easyRestFinalS;
    public int EasyMinionWaves => easyMinionWaves;
    public string WinTitle => winTitle;
    public string WinLine => winLine;
    public string WinDetailLine => winDetailLine;
    public float ArmorKnockBackU => armorKnockBackU;
    public float ArmorCobraFreezeS => armorCobraFreezeS;
    public float WinCobraDropU => winCobraDropU;
    public float GoldenFirstDelayS => goldenFirstDelayS;
    public float GoldenIntervalS => goldenIntervalS;
    public float GoldenShowS => goldenShowS;
    public float GoldenSideDeg => goldenSideDeg;
    public float GoldenAboveMonkeyU => goldenAboveMonkeyU;
    public float ArmorOutU => armorOutU;
    public float ArmorHelmetU => armorHelmetU;
    public float ArmorRewardS => armorRewardS;
    public float ArmorRewardRadiusU => armorRewardRadiusU;
    public float FinisherTimeScale => finisherTimeScale;
    public float FinisherRealS => finisherRealS;
    public float CrownFlyS => crownFlyS;
    public float CrownScaleOnMonkey => crownScaleOnMonkey;
    public float CrownAboveMonkeyU => crownAboveMonkeyU;
    public float VictoryDelayS => victoryDelayS;
    public int DotCount => dotCount;
    public float DotRadiusU => dotRadiusU;
    public Color GoldenColor => goldenColor;
    public Color WarningColor => warningColor;
    public Color WarningDimColor => warningDimColor;
    public Color MinionColor => minionColor;
    public Color TailColor => tailColor;
    public string ApproachLine => approachLine;
    public string RetreatLine => retreatLine;
    public string FirstHitLine => firstHitLine;
    public string MinionCallLine => minionCallLine;
    public float SpeechCooldownS => speechCooldownS;
    public float KeyLineHoldS => keyLineHoldS;

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("balance numbers: trigger height, king health, banana damage")]
    private WorldBalanceConfig worldBalanceConfig;
    [Tooltip("the second boss, spawned at start if no scene has one, so "
        + "every scene w/ this fight also gets the Spider King")]
    [SerializeField]
    private SpiderKingFightController spiderFightPrefab;
    [SerializeField]
    [Tooltip("confirms the revive offer, the ad skip & the reward panel, the "
        + "Player/Jump action (Space, E, Enter)")]
    private InputActionReference confirmAction;
    [SerializeField]
    [Tooltip("gives up the revive offer, the Player/RestartAfterLose action "
        + "(R)")]
    private InputActionReference giveUpAction;
    [Header("Arena")]
    [SerializeField]
    [Tooltip("name shown over the health bar")]
    private string bossName = "COBRA KING";
    [SerializeField]
    [Tooltip("height of the king's coil above where the monkey entered; u")]
    private float nestHeightU = 11f;
    [SerializeField]
    [Tooltip("the great cobra's head is held this far below the entry; u")]
    private float guardDepthU = 12f;
    [SerializeField]
    [Tooltip("a monkey this far below the entry hears the king's warning; u")]
    private float retreatLimitU = 6f;
    [SerializeField]
    [Tooltip("hazards & branches this close below the coil are cleared, so "
        + "none pokes through the king; u")]
    private float nestClearanceU = 2f;
    [SerializeField]
    [Tooltip("trunk radius, matching the tree segments; u")]
    private float trunkRadiusU = 5f;
    [SerializeField]
    [Tooltip("the camera pulls back to this many times its normal distance "
        + "in the arena, to show the king & the monkey together")]
    private float cameraDistanceScale = 2f;
    [SerializeField]
    [Tooltip("share of the height from the monkey up to the king the camera "
        + "aims up by; kept low so a knocked-down monkey stays in view")]
    private float cameraFocusShare = 0.3f;
    [SerializeField]
    [Tooltip("most the camera aims up toward the king; u")]
    private float maxCameraFocusU = 3f;
    [Header("Keep Away (ceiling & head butt)")]
    [SerializeField]
    [Tooltip("the monkey's climb is capped this far below the king's chin; u")]
    private float ceilingGapU = 1.5f;
    [SerializeField]
    [Tooltip("a monkey within this below the ceiling gets butted down; u")]
    private float pushRangeU = 1.2f;
    [SerializeField]
    [Tooltip("the head rears back this long before a butt; s")]
    private float pushWindUpS = 0.25f;
    [SerializeField]
    [Tooltip("least time between two butts; s")]
    private float pushCooldownS = 0.8f;
    [Header("Fight Loop")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "the tail sweep joins the rounds once the king has lost this share "
            + "of its health, a little b4 the first third"
    )]
    private float sweepFromLostShare = 0.2f;
    [SerializeField]
    [Tooltip("rest with the jewel lit between attack rounds; s")]
    private float restS = 2f;
    [SerializeField]
    [Tooltip("rest in the last third of the king's health; s")]
    private float restFinalS = 1.5f;
    [SerializeField]
    [Tooltip("warnings in the last third last this share of the normal; "
        + "0~1")]
    private float finalWarnScale = 0.75f;
    [Header("Bite (fatal)")]
    [SerializeField]
    [Tooltip("the target is marked this long before the bite lands; s")]
    private float biteWarnS = 1.2f;
    [SerializeField]
    [Tooltip("a monkey within this of the marked spot is bitten; u")]
    private float biteRadiusU = 2.2f;
    [Header("Tail Sweep (fatal)")]
    [SerializeField]
    [Tooltip("the ring is marked this long before the tail sweeps; s")]
    private float sweepWarnS = 1.3f;
    [SerializeField]
    [Tooltip("time the tail takes for a full loop round the trunk; s")]
    private float sweepTimeS = 0.9f;
    [SerializeField]
    [Tooltip("a monkey within this above or below the ring is swept; u")]
    private float sweepBandHalfU = 1.2f;
    [SerializeField]
    [Min(1)]
    [Tooltip("beads in the sweeping tail")]
    private int tailBeadCount = 7;
    [Header("Minions (knock-down)")]
    [SerializeField]
    [Min(1)]
    [Tooltip("small snakes per wave, one of them left out as the gap")]
    private int minionCount = 5;
    [SerializeField]
    [Tooltip("angle between neighbouring minions round the trunk; deg")]
    private float minionSpreadDeg = 28f;
    [SerializeField]
    [Min(0f)]
    [Tooltip("delay between neighbouring minions in a wave, so the wave "
        + "sweeps across; s")]
    private float minionStaggerS = 0.18f;
    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "normal mode's finale: pause between one minion wave ending & the "
            + "next one's warning; s"
    )]
    private float minionStreamGapS = 3.5f;
    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "normal mode's finale: a minion wave & a bite or sweep take turns, "
            + "their dangerous parts overlapping at most this long; s"
    )]
    private float finaleOverlapS = 0.6f;
    [SerializeField]
    [Min(1)]
    [Tooltip("waves per minion attack, each re-aimed at the monkey w/ a new "
        + "gap")]
    private int minionWaves = 2;
    [SerializeField]
    [Tooltip("drop spots are marked this long before the minions fall; s")]
    private float minionWarnS = 0.9f;
    [SerializeField]
    [Tooltip("minion fall speed; u/s")]
    private float minionFallSpeedU = 9f;
    [SerializeField]
    [Tooltip("a minion this close knocks the monkey down; u")]
    private float minionHitRadiusU = 1.1f;
    [Header("Spider Webs")]
    [SerializeField]
    [Tooltip("the web prefab the tree segments use")]
    private SpiderWebTrap webPrefab;
    [SerializeField]
    [Min(0)]
    [Tooltip("webs on the trunk when the fight begins")]
    private int webCount = 3;
    [SerializeField]
    [Min(0)]
    [Tooltip("webs added each time the king loses another third of its "
        + "health")]
    private int websPerThird = 2;
    [SerializeField]
    [Tooltip("webs may hang this far below the entry, as low as a knocked-"
        + "down monkey; u")]
    private float webBelowEntryU = 4f;
    [SerializeField]
    [Tooltip("least gap b/t two webs' centers, so none overlap; u")]
    private float webSeparationU = 4.5f;
    [SerializeField]
    [Tooltip("least gap b/t a web & a branch's root on the trunk; u")]
    private float webBranchGapU = 3f;
    [SerializeField]
    [Min(0)]
    [Tooltip(
        "the climb's web attempts per 10 u, below the arena floor from the "
            + "guard up, as it had there b4 the boss zone"
    )]
    private int lowWebAttemptsPer10U = 3;
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("chance one such attempt grows a web, the climb's curve there")]
    private float lowWebChance = 0.44f;
    [SerializeField]
    [Tooltip("in the arena each escape press counts this many times the "
        + "normal, so a web is shaken off within the king's attack gaps")]
    private float webEscapeGainScale = 2f;
    [SerializeField]
    [Tooltip("webs keep this far round the trunk from any fruit; deg")]
    private float webFruitGapDeg = 25f;
    [Header("Bananas")]
    [SerializeField]
    [Tooltip("the tree segments' banana branch prefab")]
    private GameObject bananaBranchPrefab;
    [SerializeField]
    [Tooltip(
        "bare branch prefab, the tree's empty kind, for the arena's own "
            + "obstacle branches"
    )]
    private GameObject branchEmptyPrefab;
    [SerializeField]
    [Tooltip("mesh shown in place of the banana on every arena fruit, "
        + "grabbed & thrown; the watermelon")]
    private Mesh watermelonMesh;
    [SerializeField]
    [Tooltip("local scale of the watermelon on its branch")]
    private Vector3 watermelonScale = new Vector3(6f, 6f, 6f);
    [SerializeField]
    [Tooltip("local offset of the watermelon on its branch; u")]
    private Vector3 watermelonOffset = new Vector3(0f, -0.629f, 0.06f);
    [SerializeField]
    [Min(1)]
    [Tooltip("banana branches in the arena")]
    private int bananaCount = 3;
    [SerializeField]
    [Min(0)]
    [Tooltip("banana branches added each time the king loses another third "
        + "of its health")]
    private int bananasPerThird = 1;
    [SerializeField]
    [Tooltip("lowest & highest banana height above the entry; u")]
    private Vector2 fruitHeightRangeU = new Vector2(2f, 5.5f);
    [SerializeField]
    [Tooltip("radius of a fruit branch's root, as the tree segments' "
        + "BranchFruitPlacementConfig.branchRadius; u")]
    private float branchRadiusU = 6.6f;
    [Header("Arena Branches")]
    [SerializeField]
    [Min(0)]
    [Tooltip(
        "branch attempts per 10 u of arena height; the boss zone grows no "
            + "tree branches, so the fight grows its own, as many as the climb "
            + "had there b4 the zone"
    )]
    private int arenaBranchAttemptsPer10U = 10;
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "chance one attempt grows a branch: the climb's branch curve at the "
            + "fight's height b4 the boss zone"
    )]
    private float arenaBranchChance = 0.47f;
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "chance an arena branch bears fruit, the climb's banana chance there; "
            + "it is thrown at the king like any arena fruit, but never grows "
            + "back"
    )]
    private float arenaBranchFruitChance = 0.16f;
    [SerializeField]
    [Tooltip("least gap b/t two branches, the climb's own; u")]
    private float arenaBranchSeparationU = 3f;
    [SerializeField]
    [Tooltip(
        "no arena branch grows this close to the monkey's start spot, so the "
            + "fight never opens w/ a hit; u"
    )]
    private float arenaBranchStartClearU = 6f;
    [SerializeField]
    [Tooltip("a taken banana grows back on its branch after this; s")]
    private float bananaRespawnS = 9f;
    [SerializeField]
    [Tooltip("banana grow-back time as a share of the normal, once 1 and 2 "
        + "thirds of the king's health are lost")]
    private Vector2 bananaRespawnScaleByThird = new Vector2(0.75f, 0.5f);
    [Header("Throw")]
    [SerializeField]
    [Tooltip("time a thrown banana takes to reach the crown; s")]
    private float throwTimeS = 0.7f;
    [SerializeField]
    [Tooltip("how high the throw arcs over the straight line; u")]
    private float throwArcU = 3f;
    [SerializeField]
    [Tooltip("thrown banana size as a share of the grabbed one")]
    private float thrownBananaScale = 1.6f;
    [SerializeField]
    private Color trailColor = new Color(0.3f, 0.8f, 0.3f);
    [Header("Height Marker")]
    [SerializeField]
    [Min(0.05f)]
    [Tooltip(
        "size of the king look-alike on the fight's trunk height marker, as "
            + "a share of the real king"
    )]
    private float markerFigureScale = 0.3f;
    [Header("Rough Bark")]
    [SerializeField]
    [Tooltip(
        "material of the arena's rough bark patches, the same the "
            + "RoughBarkSystem uses"
    )]
    private Material roughBarkMaterial;
    [SerializeField]
    [Min(0)]
    [Tooltip(
        "rough bark attempts per 10 u of arena height, as the climb's "
            + "RoughBarkSpawner had there b4 the boss zone"
    )]
    private int roughBarkAttemptsPer10U = 2;
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "chance one attempt grows a patch: the climb's rough bark curve at "
            + "the fight's height b4 the boss zone"
    )]
    private float roughBarkChance = 0.35f;
    [SerializeField]
    [Tooltip("patch height range, the climb's own; u")]
    private Vector2 roughBarkHeightRangeU = new Vector2(5f, 8.5f);
    [SerializeField]
    [Tooltip("patch width round the trunk, the climb's own; deg")]
    private Vector2 roughBarkArcRangeDeg = new Vector2(110f, 170f);
    [Header("Golden Hint")]
    [SerializeField]
    [Tooltip("banner title when the golden watermelon shows up")]
    private string goldenHintTitle = "Golden watermelon!";
    [SerializeField]
    [Tooltip("line under it")]
    private string goldenHintLine = "Grab it for a Golden Armor";
    [SerializeField]
    [Min(0.5f)]
    [Tooltip("how long that banner stays; s")]
    private float goldenHintS = 3f;
    [Header("Golden Banana")]
    [SerializeField]
    [Tooltip("outline material copied for the golden banana's thick border, "
        + "eg the bananas' grabbable outline")]
    private Material goldenOutlineMaterial;
    [SerializeField]
    private Color goldenOutlineColor = new Color(1f, 0.75f, 0f);
    [SerializeField]
    [Tooltip(
        "golden banana outline width, times the usual grab outline's, so it "
            + "follows the banana model's scale"
    )]
    private float goldenOutlineWidthScale = 2f;
    [Header("Starting Armor")]
    [SerializeField]
    [Tooltip(
        "banner title as the fight begins in the gold armor; it stays up "
            + "until the first banana is thrown"
    )]
    private string armorIntroTitle = "You get a Golden Armor!";
    [SerializeField]
    [Tooltip("line under the banner: what the armor does")]
    private string armorIntroLine = "Blocks 1 fatal hit";
    [Header("Revive")]
    [SerializeField]
    [Tooltip("every loss in the boss fight offers a revive for an ad")]
    private bool canRevive = true;
    [SerializeField]
    [Min(1f)]
    [Tooltip("how long the revive offer waits before it gives up; s")]
    private float reviveOfferS = 8f;
    [SerializeField]
    [Min(0f)]
    [Tooltip("the ad's Skip button shows up after this long; s")]
    private float adSkipAfterS = 3f;
    [SerializeField]
    [Tooltip(
        "speed the revived monkey flies back to where the fight began; u/s"
    )]
    private float reviveReturnSpeedU = 20f;
    [SerializeField]
    [Tooltip(
        "page the ad's Course Page button opens: the CSCI 526 listing in the "
            + "USC Schedule of Classes; update the term as needed"
    )]
    private string courseUrl =
        "https://classes.usc.edu/term-20263/course/csci-526";
    [Header("Mode Choice")]
    [SerializeField]
    [Tooltip("the two choice bananas sit this far left & right of the "
        + "monkey round the trunk; deg")]
    private float choiceSideDeg = 28f;
    [SerializeField]
    [Tooltip("the choice bananas hang this far above the monkey; u")]
    private float choiceAboveU = 1.5f;
    [SerializeField]
    [Tooltip("line under the armor banner after a revive: how to pick a mode")]
    private string chooseLine = "Grab one: EASY or NORMAL";
    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip("easy mode: the king's health as a share of the normal")]
    private float easyHealthScale = 0.6f;
    [SerializeField]
    [Tooltip("easy mode: warnings last this many times the normal")]
    private float easyWarnScale = 1.35f;
    [SerializeField]
    [Tooltip("easy mode: rest between attack rounds; s")]
    private float easyRestS = 3.25f;
    [SerializeField]
    [Tooltip("easy mode: rest in the last third of the king's health; s")]
    private float easyRestFinalS = 2.35f;
    [SerializeField]
    [Min(1)]
    [Tooltip("easy mode: waves per minion attack")]
    private int easyMinionWaves = 1;
    [Header("Win Reward")]
    [SerializeField]
    [Tooltip("banner title once the king is beaten")]
    private string winTitle = "You win!";
    [SerializeField]
    [Tooltip("line under the win title")]
    private string winLine = "Golden Armor is yours to keep!";
    [SerializeField]
    [Tooltip("small line under it: what the armor does")]
    private string winDetailLine = "It blocks 1 fatal hit";
    [SerializeField]
    [Tooltip(
        "when the armor takes the great cobra's bite, its head is thrown "
            + "this far back down the trunk; u"
    )]
    private float armorKnockBackU = 20f;
    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "...& it is frozen this long after, longer than a coconut's freeze, "
            + "as the armor is the boss fight's one-off reward; s"
    )]
    private float armorCobraFreezeS = 15f;
    [SerializeField]
    [Tooltip(
        "after a win the great cobra chases again from this far further "
            + "down the trunk than its guard spot, a head start; u"
    )]
    private float winCobraDropU = 45f;
    [SerializeField]
    [Tooltip("the golden banana first shows up this long after the first "
        + "hit on the king; s")]
    private float goldenFirstDelayS = 8f;
    [SerializeField]
    [Tooltip("time between two golden banana visits, once one is gone; s")]
    private float goldenIntervalS = 15f;
    [SerializeField]
    [Tooltip("how long a golden banana stays before it vanishes; s")]
    private float goldenShowS = 8f;
    [SerializeField]
    [Tooltip("it grows this far round the trunk from the monkey, so in "
        + "view; deg")]
    private float goldenSideDeg = 30f;
    [SerializeField]
    [Tooltip("it grows this far above the monkey, kept under the ceiling; u")]
    private float goldenAboveMonkeyU = 3f;
    [SerializeField]
    [Tooltip("the armor's back plate sits this far out from the monkey's "
        + "center; u")]
    private float armorOutU = 0.45f;
    [SerializeField]
    [Tooltip("the armor's helmet sits this far above the monkey's center; u")]
    private float armorHelmetU = 0.95f;
    [SerializeField]
    [Min(0.3f)]
    [Tooltip(
        "win reward: the armor pieces swirl in onto the monkey & pulse for "
            + "this long; s"
    )]
    private float armorRewardS = 1.6f;
    [SerializeField]
    [Tooltip("win reward: the armor pieces start this far out; u")]
    private float armorRewardRadiusU = 3f;
    [Header("Finisher")]
    [SerializeField]
    [Tooltip("time scale of the last blow's slow motion, either way")]
    private float finisherTimeScale = 0.2f;
    [SerializeField]
    [Tooltip("real time the slow motion lasts; s")]
    private float finisherRealS = 1.4f;
    [Header("Victory")]
    [SerializeField]
    [Tooltip("time the crown takes to fly onto the monkey; s")]
    private float crownFlyS = 1.2f;
    [SerializeField]
    [Tooltip("crown size on the monkey, as a share of the king's")]
    private float crownScaleOnMonkey = 0.55f;
    [SerializeField]
    [Tooltip("crown height above the monkey's center; u")]
    private float crownAboveMonkeyU = 1.3f;
    [SerializeField]
    [Tooltip("pause after the crown lands before the victory panel; s")]
    private float victoryDelayS = 0.8f;
    [Header("Dotted Line")]
    [SerializeField]
    [Min(2)]
    [Tooltip("dots from the first banana to the crown")]
    private int dotCount = 12;
    [SerializeField]
    [Tooltip("dot radius; u")]
    private float dotRadiusU = 0.12f;
    [Header("Colors")]
    [SerializeField]
    private Color goldenColor = new Color(1f, 0.82f, 0.15f);
    [SerializeField]
    private Color warningColor = new Color(1f, 0.15f, 0.15f);
    [SerializeField]
    private Color warningDimColor = new Color(0.45f, 0.05f, 0.05f);
    [SerializeField]
    private Color minionColor = new Color(0.45f, 0.8f, 0.45f);
    [SerializeField]
    private Color tailColor = new Color(0.72f, 0.56f, 0.95f);
    [Header("Speech")]
    [SerializeField]
    private string approachLine = "Stay away from me!";
    [SerializeField]
    private string retreatLine = "Nowhere to run!";
    [SerializeField]
    private string firstHitLine = "How dare you!";
    [SerializeField]
    [Tooltip("said once a game, the first time the minions come")]
    private string minionCallLine = "Minions, help me!";
    [SerializeField]
    [Tooltip("the same line is not repeated within this; s")]
    private float speechCooldownS = 4f;
    [SerializeField]
    [Tooltip("a key line, eg the minion call, holds the bubble this long; "
        + "s")]
    private float keyLineHoldS = 2.2f;
}
