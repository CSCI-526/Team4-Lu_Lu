using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Every tuned value of the Cobra King fight, grouped as the Inspector shows
/// them. Read-only at runtime, so a value changed in Play mode
/// persists in the asset.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Objects/CobraKingConfig")]
public class CobraKingConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>the second boss, spawned at start if no scene has
    /// one, so every scene w/ this fight also gets the Spider King</returns>
    public SpiderKingFightController SpiderFightPrefab => spiderFightPrefab;

    /// <returns>confirms the revive offer, the ad skip & the reward
    /// panel, the Player/Jump action (Space, E, Enter)</returns>
    public InputActionReference ConfirmAction => confirmAction;

    /// <returns>gives up the revive offer, the
    /// Player/RestartAfterLose action (R)</returns>
    public InputActionReference GiveUpAction => giveUpAction;

    /// <returns>name shown over the health bar</returns>
    public string BossName => bossName;

    /// <returns>height of the king's coil above where the monkey
    /// entered; u</returns>
    public float NestHeightU => nestHeightU;

    /// <returns>the great cobra's head is held this far below the
    /// entry; u</returns>
    public float GuardDepthU => guardDepthU;

    /// <returns>a monkey this far below the entry hears the king's
    /// warning; u</returns>
    public float RetreatLimitU => retreatLimitU;

    /// <returns>hazards & branches this close below the coil are
    /// cleared, so none pokes through the king; u</returns>
    public float NestClearanceU => nestClearanceU;

    /// <returns>trunk radius, matching the tree segments; u</returns>
    public float TrunkRadiusU => trunkRadiusU;

    /// <returns>the camera pulls back to this many times its normal
    /// distance in the arena, to show the king & the monkey together</returns>
    public float CameraDistanceScale => cameraDistanceScale;

    /// <returns>share of the height from the monkey up to the king
    /// the camera aims up by; kept low so a knocked-down monkey stays
    /// in view</returns>
    public float CameraFocusShare => cameraFocusShare;

    /// <returns>most the camera aims up toward the king; u</returns>
    public float MaxCameraFocusU => maxCameraFocusU;

    /// <returns>the monkey's climb is capped this far below the
    /// king's chin; u</returns>
    public float CeilingGapU => ceilingGapU;

    /// <returns>a monkey within this below the ceiling gets butted
    /// down; u</returns>
    public float PushRangeU => pushRangeU;

    /// <returns>the head rears back this long before a butt; s</returns>
    public float PushWindUpS => pushWindUpS;

    /// <returns>least time between two butts; s</returns>
    public float PushCooldownS => pushCooldownS;

    /// <returns>the tail sweep joins the rounds once the king has
    /// lost this share of its health, a little b4 the first third</returns>
    public float SweepFromLostShare => sweepFromLostShare;

    /// <returns>rest with the jewel lit between attack rounds; s</returns>
    public float RestS => restS;

    /// <returns>rest in the last third of the king's health; s</returns>
    public float RestFinalS => restFinalS;

    /// <returns>warnings in the last third last this share of the
    /// normal; 0~1</returns>
    public float FinalWarnScale => finalWarnScale;

    /// <returns>the target is marked this long before the bite lands;
    /// s</returns>
    public float BiteWarnS => biteWarnS;

    /// <returns>a monkey within this of the marked spot is bitten; u</returns>
    public float BiteRadiusU => biteRadiusU;

    /// <returns>the ring is marked this long before the tail sweeps;
    /// s</returns>
    public float SweepWarnS => sweepWarnS;

    /// <returns>time the tail takes for a full loop round the trunk;
    /// s</returns>
    public float SweepTimeS => sweepTimeS;

    /// <returns>a monkey within this above or below the ring is
    /// swept; u</returns>
    public float SweepBandHalfU => sweepBandHalfU;

    /// <returns>beads in the sweeping tail</returns>
    public int TailBeadCount => tailBeadCount;

    /// <returns>small snakes per wave, one of them left out as the
    /// gap</returns>
    public int MinionCount => minionCount;

    /// <returns>angle between neighbouring minions round the trunk;
    /// deg</returns>
    public float MinionSpreadDeg => minionSpreadDeg;

    /// <returns>delay between neighbouring minions in a wave, so the
    /// wave sweeps across; s</returns>
    public float MinionStaggerS => minionStaggerS;

    /// <returns>normal mode's finale: pause between one minion wave
    /// ending & the next one's warning; s</returns>
    public float MinionStreamGapS => minionStreamGapS;

    /// <returns>normal mode's finale: a minion wave & a bite or sweep
    /// take turns, their dangerous parts overlapping at most this
    /// long; s</returns>
    public float FinaleOverlapS => finaleOverlapS;

    /// <returns>waves per minion attack, each re-aimed at the monkey
    /// w/ a new gap</returns>
    public int MinionWaves => minionWaves;

    /// <returns>drop spots are marked this long before the minions
    /// fall; s</returns>
    public float MinionWarnS => minionWarnS;

    /// <returns>minion fall speed; u/s</returns>
    public float MinionFallSpeedU => minionFallSpeedU;

    /// <returns>a minion this close knocks the monkey down; u</returns>
    public float MinionHitRadiusU => minionHitRadiusU;

    /// <returns>the web prefab the tree segments use</returns>
    public SpiderWebTrap WebPrefab => webPrefab;

    /// <returns>webs on the trunk when the fight begins</returns>
    public int WebCount => webCount;

    /// <returns>webs added each time the king loses another third of
    /// its health</returns>
    public int WebsPerThird => websPerThird;

    /// <returns>webs may hang this far below the entry, as low as a
    /// knocked-down monkey; u</returns>
    public float WebBelowEntryU => webBelowEntryU;

    /// <returns>least gap b/t two webs' centers, so none overlap; u</returns>
    public float WebSeparationU => webSeparationU;

    /// <returns>least gap b/t a web & a branch's root on the trunk; u</returns>
    public float WebBranchGapU => webBranchGapU;

    /// <returns>the climb's web attempts per 10 u, below the arena
    /// floor from the guard up, as it had there b4 the boss zone</returns>
    public int LowWebAttemptsPer10U => lowWebAttemptsPer10U;

    /// <returns>chance one such attempt grows a web, the climb's
    /// curve there</returns>
    public float LowWebChance => lowWebChance;

    /// <returns>in the arena each escape press counts this many times
    /// the normal, so a web is shaken off within the king's attack
    /// gaps</returns>
    public float WebEscapeGainScale => webEscapeGainScale;

    /// <returns>webs keep this far round the trunk from any fruit;
    /// deg</returns>
    public float WebFruitGapDeg => webFruitGapDeg;

    /// <returns>the tree segments' banana branch prefab</returns>
    public GameObject BananaBranchPrefab => bananaBranchPrefab;

    /// <returns>bare branch prefab, the tree's empty kind, for the
    /// arena's own obstacle branches</returns>
    public GameObject BranchEmptyPrefab => branchEmptyPrefab;

    /// <returns>the watermelon branch prefab, a variant of the banana
    /// branch, bearing every arena fruit; built by
    /// <c>Monkobra > Build > Watermelon Prefab</c></returns>
    public GameObject WatermelonBranchPrefab => watermelonBranchPrefab;

    /// <returns>watermelon mesh, an input of the watermelon prefab
    /// builder; the arena never swaps it at runtime</returns>
    public Mesh WatermelonMesh => watermelonMesh;

    /// <returns>local scale of the watermelon on its branch</returns>
    public Vector3 WatermelonScale => watermelonScale;

    /// <returns>local offset of the watermelon on its branch; u</returns>
    public Vector3 WatermelonOffset => watermelonOffset;

    /// <returns>banana branches in the arena</returns>
    public int BananaCount => bananaCount;

    /// <returns>banana branches added each time the king loses
    /// another third of its health</returns>
    public int BananasPerThird => bananasPerThird;

    /// <returns>lowest & highest banana height above the entry; u</returns>
    public Vector2 FruitHeightRangeU => fruitHeightRangeU;

    /// <returns>radius of a fruit branch's root, as the tree
    /// segments' BranchFruitPlacementConfig.branchRadius; u</returns>
    public float BranchRadiusU => branchRadiusU;

    /// <returns>branch attempts per 10 u of arena height; the boss
    /// zone grows no tree branches, so the fight grows its own, as
    /// many as the climb had there b4 the zone</returns>
    public int ArenaBranchAttemptsPer10U => arenaBranchAttemptsPer10U;

    /// <returns>chance one attempt grows a branch: the climb's branch
    /// curve at the fight's height b4 the boss zone</returns>
    public float ArenaBranchChance => arenaBranchChance;

    /// <returns>chance an arena branch bears fruit, the climb's
    /// banana chance there; it is thrown at the king like any arena
    /// fruit, but never grows back</returns>
    public float ArenaBranchFruitChance => arenaBranchFruitChance;

    /// <returns>least gap b/t two branches, the climb's own; u</returns>
    public float ArenaBranchSeparationU => arenaBranchSeparationU;

    /// <returns>no arena branch grows this close to the monkey's
    /// start spot, so the fight never opens w/ a hit; u</returns>
    public float ArenaBranchStartClearU => arenaBranchStartClearU;

    /// <returns>a taken banana grows back on its branch after this; s</returns>
    public float BananaRespawnS => bananaRespawnS;

    /// <returns>banana grow-back time as a share of the normal, once
    /// 1 and 2 thirds of the king's health are lost</returns>
    public Vector2 BananaRespawnScaleByThird => bananaRespawnScaleByThird;

    /// <returns>time a thrown banana takes to reach the crown; s</returns>
    public float ThrowTimeS => throwTimeS;

    /// <returns>how high the throw arcs over the straight line; u</returns>
    public float ThrowArcU => throwArcU;

    /// <returns>thrown banana size as a share of the grabbed one</returns>
    public float ThrownBananaScale => thrownBananaScale;

    /// <returns>tint of a thrown banana's trail & sparks</returns>
    public Color TrailColor => trailColor;

    /// <returns>size of the king look-alike on the fight's trunk
    /// height marker, as a share of the real king</returns>
    public float MarkerFigureScale => markerFigureScale;

    /// <returns>material of the arena's rough bark patches, the same
    /// the RoughBarkSystem uses</returns>
    public Material RoughBarkMaterial => roughBarkMaterial;

    /// <returns>rough bark attempts per 10 u of arena height, as the
    /// climb's RoughBarkSpawner had there b4 the boss zone</returns>
    public int RoughBarkAttemptsPer10U => roughBarkAttemptsPer10U;

    /// <returns>chance one attempt grows a patch: the climb's rough
    /// bark curve at the fight's height b4 the boss zone</returns>
    public float RoughBarkChance => roughBarkChance;

    /// <returns>patch height range, the climb's own; u</returns>
    public Vector2 RoughBarkHeightRangeU => roughBarkHeightRangeU;

    /// <returns>patch width round the trunk, the climb's own; deg</returns>
    public Vector2 RoughBarkArcRangeDeg => roughBarkArcRangeDeg;

    /// <returns>banner title when the golden watermelon shows up</returns>
    public string GoldenHintTitle => goldenHintTitle;

    /// <returns>line under it</returns>
    public string GoldenHintLine => goldenHintLine;

    /// <returns>how long that banner stays; s</returns>
    public float GoldenHintS => goldenHintS;

    /// <returns>outline material copied for the golden banana's thick
    /// border, eg the bananas' grabbable outline</returns>
    public Material GoldenOutlineMaterial => goldenOutlineMaterial;

    /// <returns>outline tint of the golden banana</returns>
    public Color GoldenOutlineColor => goldenOutlineColor;

    /// <returns>golden banana outline width, times the usual grab
    /// outline's, so it follows the banana model's scale</returns>
    public float GoldenOutlineWidthScale => goldenOutlineWidthScale;

    /// <returns>banner title as the fight begins in the gold armor;
    /// it stays up until the first banana is thrown</returns>
    public string ArmorIntroTitle => armorIntroTitle;

    /// <returns>line under the banner: what the armor does</returns>
    public string ArmorIntroLine => armorIntroLine;

    /// <returns>every loss in the boss fight offers a revive for an
    /// ad</returns>
    public bool CanRevive => canRevive;

    /// <returns>how long the revive offer waits before it gives up; s</returns>
    public float ReviveOfferS => reviveOfferS;

    /// <returns>the ad's Skip button shows up after this long; s</returns>
    public float AdSkipAfterS => adSkipAfterS;

    /// <returns>speed the revived monkey flies back to where the
    /// fight began; u/s</returns>
    public float ReviveReturnSpeedU => reviveReturnSpeedU;

    /// <returns>page the ad's Course Page button opens: the CSCI 526
    /// listing in the USC Schedule of Classes; update the term as
    /// needed</returns>
    public string CourseUrl => courseUrl;

    /// <returns>the two choice bananas sit this far left & right of
    /// the monkey round the trunk; deg</returns>
    public float ChoiceSideDeg => choiceSideDeg;

    /// <returns>the choice bananas hang this far above the monkey; u</returns>
    public float ChoiceAboveU => choiceAboveU;

    /// <returns>line under the armor banner after a revive: how to
    /// pick a mode</returns>
    public string ChooseLine => chooseLine;

    /// <returns>easy mode: the king's health as a share of the normal</returns>
    public float EasyHealthScale => easyHealthScale;

    /// <returns>easy mode: warnings last this many times the normal</returns>
    public float EasyWarnScale => easyWarnScale;

    /// <returns>easy mode: rest between attack rounds; s</returns>
    public float EasyRestS => easyRestS;

    /// <returns>easy mode: rest in the last third of the king's
    /// health; s</returns>
    public float EasyRestFinalS => easyRestFinalS;

    /// <returns>easy mode: waves per minion attack</returns>
    public int EasyMinionWaves => easyMinionWaves;

    /// <returns>banner title once the king is beaten</returns>
    public string WinTitle => winTitle;

    /// <returns>line under the win title</returns>
    public string WinLine => winLine;

    /// <returns>small line under it: what the armor does</returns>
    public string WinDetailLine => winDetailLine;

    /// <returns>when the armor takes the great cobra's bite, its head
    /// is thrown this far back down the trunk; u</returns>
    public float ArmorKnockBackU => armorKnockBackU;

    /// <returns>...& it is frozen this long after, longer than a
    /// coconut's freeze, as the armor is the boss fight's one-off
    /// reward; s</returns>
    public float ArmorCobraFreezeS => armorCobraFreezeS;

    /// <returns>the golden banana first shows up this long after the
    /// first hit on the king; s</returns>
    public float GoldenFirstDelayS => goldenFirstDelayS;

    /// <returns>time between two golden banana visits, once one is
    /// gone; s</returns>
    public float GoldenIntervalS => goldenIntervalS;

    /// <returns>how long a golden banana stays before it vanishes; s</returns>
    public float GoldenShowS => goldenShowS;

    /// <returns>it grows this far round the trunk from the monkey, so
    /// in view; deg</returns>
    public float GoldenSideDeg => goldenSideDeg;

    /// <returns>it grows this far above the monkey, kept under the
    /// ceiling; u</returns>
    public float GoldenAboveMonkeyU => goldenAboveMonkeyU;

    /// <returns>the armor's back plate sits this far out from the
    /// monkey's center; u</returns>
    public float ArmorOutU => armorOutU;

    /// <returns>the armor's helmet sits this far above the monkey's
    /// center; u</returns>
    public float ArmorHelmetU => armorHelmetU;

    /// <returns>win reward: the armor pieces swirl in onto the monkey
    /// & pulse for this long; s</returns>
    public float ArmorRewardS => armorRewardS;

    /// <returns>win reward: the armor pieces start this far out; u</returns>
    public float ArmorRewardRadiusU => armorRewardRadiusU;

    /// <returns>time scale of the last blow's slow motion, either way</returns>
    public float FinisherTimeScale => finisherTimeScale;

    /// <returns>real time the slow motion lasts; s</returns>
    public float FinisherRealS => finisherRealS;

    /// <returns>time the crown takes to fly onto the monkey; s</returns>
    public float CrownFlyS => crownFlyS;

    /// <returns>crown size on the monkey, as a share of the king's</returns>
    public float CrownScaleOnMonkey => crownScaleOnMonkey;

    /// <returns>crown height above the monkey's center; u</returns>
    public float CrownAboveMonkeyU => crownAboveMonkeyU;

    /// <returns>pause after the crown lands before the victory panel;
    /// s</returns>
    public float VictoryDelayS => victoryDelayS;

    /// <returns>dots from the first banana to the crown</returns>
    public int DotCount => dotCount;

    /// <returns>dot radius; u</returns>
    public float DotRadiusU => dotRadiusU;

    /// <returns>tint of the golden armor & banana</returns>
    public Color GoldenColor => goldenColor;

    /// <returns>tint of an attack warning at its brightest</returns>
    public Color WarningColor => warningColor;

    /// <returns>tint of an attack warning at its dimmest</returns>
    public Color WarningDimColor => warningDimColor;

    /// <returns>tint of the minions</returns>
    public Color MinionColor => minionColor;

    /// <returns>tint of the king's tail</returns>
    public Color TailColor => tailColor;

    /// <returns>line spoken as the king shoves off a monkey that
    /// comes too close</returns>
    public string ApproachLine => approachLine;

    /// <returns>line spoken when the monkey drops below the arena
    /// entry</returns>
    public string RetreatLine => retreatLine;

    /// <returns>line spoken at the first hit on the king</returns>
    public string FirstHitLine => firstHitLine;

    /// <returns>said once a game, the first time the minions come</returns>
    public string MinionCallLine => minionCallLine;

    /// <returns>the same line is not repeated within this; s</returns>
    public float SpeechCooldownS => speechCooldownS;

    /// <returns>a key line, eg the minion call, holds the bubble this
    /// long; s</returns>
    public float KeyLineHoldS => keyLineHoldS;

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("the second boss, spawned at start if no scene has one, so every "
        + "scene w/ this fight also gets the Spider King")]
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
    [Tooltip("the camera pulls back to this many times its normal distance in "
        + "the arena, to show the king & the monkey together")]
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
    [Tooltip("the tail sweep joins the rounds once the king has lost this "
        + "share of its health, a little b4 the first third")]
    [Range(0f, 1f)]
    private float sweepFromLostShare = 0.2f;

    [SerializeField]
    [Tooltip("rest with the jewel lit between attack rounds; s")]
    private float restS = 2f;

    [SerializeField]
    [Tooltip("rest in the last third of the king's health; s")]
    private float restFinalS = 1.5f;

    [SerializeField]
    [Tooltip("warnings in the last third last this share of the normal; 0~1")]
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
    [Tooltip("beads in the sweeping tail")]
    [Min(1)]
    private int tailBeadCount = 7;

    [Header("Minions (knock-down)")]
    [SerializeField]
    [Tooltip("small snakes per wave, one of them left out as the gap")]
    [Min(1)]
    private int minionCount = 5;

    [SerializeField]
    [Tooltip("angle between neighbouring minions round the trunk; deg")]
    private float minionSpreadDeg = 28f;

    [SerializeField]
    [Tooltip("delay between neighbouring minions in a wave, so the wave "
        + "sweeps across; s")]
    [Min(0f)]
    private float minionStaggerS = 0.18f;

    [SerializeField]
    [Tooltip("normal mode's finale: pause between one minion wave ending & "
        + "the next one's warning; s")]
    [Min(0f)]
    private float minionStreamGapS = 3.5f;

    [SerializeField]
    [Tooltip("normal mode's finale: a minion wave & a bite or sweep take "
        + "turns, their dangerous parts overlapping at most this long; s")]
    [Min(0f)]
    private float finaleOverlapS = 0.6f;

    [SerializeField]
    [Tooltip("waves per minion attack, each re-aimed at the monkey w/ a new "
        + "gap")]
    [Min(1)]
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
    [Tooltip("webs on the trunk when the fight begins")]
    [Min(0)]
    private int webCount = 3;

    [SerializeField]
    [Tooltip("webs added each time the king loses another third of its health")]
    [Min(0)]
    private int websPerThird = 2;

    [SerializeField]
    [Tooltip("webs may hang this far below the entry, as low as a "
        + "knocked-down monkey; u")]
    private float webBelowEntryU = 4f;

    [SerializeField]
    [Tooltip("least gap b/t two webs' centers, so none overlap; u")]
    private float webSeparationU = 4.5f;

    [SerializeField]
    [Tooltip("least gap b/t a web & a branch's root on the trunk; u")]
    private float webBranchGapU = 3f;

    [SerializeField]
    [Tooltip("the climb's web attempts per 10 u, below the arena floor from "
        + "the guard up, as it had there b4 the boss zone")]
    [Min(0)]
    private int lowWebAttemptsPer10U = 3;

    [SerializeField]
    [Tooltip("chance one such attempt grows a web, the climb's curve there")]
    [Range(0f, 1f)]
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
    [Tooltip("bare branch prefab, the tree's empty kind, for the arena's own "
        + "obstacle branches")]
    private GameObject branchEmptyPrefab;

    [SerializeField]
    [Tooltip("the watermelon branch prefab, a variant of the banana branch, "
        + "bearing every arena fruit; from Monkobra > Build > Watermelon "
        + "Prefab")]
    private GameObject watermelonBranchPrefab;

    [SerializeField]
    [Tooltip("watermelon mesh, an input of the watermelon prefab builder")]
    private Mesh watermelonMesh;

    [SerializeField]
    [Tooltip("local scale of the watermelon on its branch")]
    private Vector3 watermelonScale = new Vector3(6f, 6f, 6f);

    [SerializeField]
    [Tooltip("local offset of the watermelon on its branch; u")]
    private Vector3 watermelonOffset = new Vector3(0f, -0.629f, 0.06f);

    [SerializeField]
    [Tooltip("banana branches in the arena")]
    [Min(1)]
    private int bananaCount = 3;

    [SerializeField]
    [Tooltip("banana branches added each time the king loses another third of "
        + "its health")]
    [Min(0)]
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
    [Tooltip("branch attempts per 10 u of arena height; the boss zone grows "
        + "no tree branches, so the fight grows its own, as many as the "
        + "climb had there b4 the zone")]
    [Min(0)]
    private int arenaBranchAttemptsPer10U = 10;

    [SerializeField]
    [Tooltip("chance one attempt grows a branch: the climb's branch curve at "
        + "the fight's height b4 the boss zone")]
    [Range(0f, 1f)]
    private float arenaBranchChance = 0.47f;

    [SerializeField]
    [Tooltip("chance an arena branch bears fruit, the climb's banana chance "
        + "there; it is thrown at the king like any arena fruit, but never "
        + "grows back")]
    [Range(0f, 1f)]
    private float arenaBranchFruitChance = 0.16f;

    [SerializeField]
    [Tooltip("least gap b/t two branches, the climb's own; u")]
    private float arenaBranchSeparationU = 3f;

    [SerializeField]
    [Tooltip("no arena branch grows this close to the monkey's start spot, so "
        + "the fight never opens w/ a hit; u")]
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
    [Tooltip("tint of a thrown banana's trail & sparks")]
    private Color trailColor = new Color(0.3f, 0.8f, 0.3f);

    [Header("Height Marker")]
    [SerializeField]
    [Tooltip("size of the king look-alike on the fight's trunk height marker, "
        + "as a share of the real king")]
    [Min(0.05f)]
    private float markerFigureScale = 0.3f;

    [Header("Rough Bark")]
    [SerializeField]
    [Tooltip("material of the arena's rough bark patches, the same the "
        + "RoughBarkSystem uses")]
    private Material roughBarkMaterial;

    [SerializeField]
    [Tooltip("rough bark attempts per 10 u of arena height, as the climb's "
        + "RoughBarkSpawner had there b4 the boss zone")]
    [Min(0)]
    private int roughBarkAttemptsPer10U = 2;

    [SerializeField]
    [Tooltip("chance one attempt grows a patch: the climb's rough bark curve "
        + "at the fight's height b4 the boss zone")]
    [Range(0f, 1f)]
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
    [Tooltip("how long that banner stays; s")]
    [Min(0.5f)]
    private float goldenHintS = 3f;

    [Header("Golden Banana")]
    [SerializeField]
    [Tooltip("outline material copied for the golden banana's thick border, "
        + "eg the bananas' grabbable outline")]
    private Material goldenOutlineMaterial;

    [SerializeField]
    [Tooltip("outline tint of the golden banana")]
    private Color goldenOutlineColor = new Color(1f, 0.75f, 0f);

    [SerializeField]
    [Tooltip("golden banana outline width, times the usual grab outline's, so "
        + "it follows the banana model's scale")]
    private float goldenOutlineWidthScale = 2f;

    [Header("Starting Armor")]
    [SerializeField]
    [Tooltip("banner title as the fight begins in the gold armor; it stays up "
        + "until the first banana is thrown")]
    private string armorIntroTitle = "You get a Golden Armor!";

    [SerializeField]
    [Tooltip("line under the banner: what the armor does")]
    private string armorIntroLine = "Blocks 1 fatal hit";

    [Header("Revive")]
    [SerializeField]
    [Tooltip("every loss in the boss fight offers a revive for an ad")]
    private bool canRevive = true;

    [SerializeField]
    [Tooltip("how long the revive offer waits before it gives up; s")]
    [Min(1f)]
    private float reviveOfferS = 8f;

    [SerializeField]
    [Tooltip("the ad's Skip button shows up after this long; s")]
    [Min(0f)]
    private float adSkipAfterS = 3f;

    [SerializeField]
    [Tooltip("speed the revived monkey flies back to where the fight began; "
        + "u/s")]
    private float reviveReturnSpeedU = 20f;

    [SerializeField]
    [Tooltip("page the ad's Course Page button opens: the CSCI 526 listing in "
        + "the USC Schedule of Classes; update the term as needed")]
    private string courseUrl =
        "https://classes.usc.edu/term-20263/course/csci-526";

    [Header("Mode Choice")]
    [SerializeField]
    [Tooltip("the two choice bananas sit this far left & right of the monkey "
        + "round the trunk; deg")]
    private float choiceSideDeg = 28f;

    [SerializeField]
    [Tooltip("the choice bananas hang this far above the monkey; u")]
    private float choiceAboveU = 1.5f;

    [SerializeField]
    [Tooltip("line under the armor banner after a revive: how to pick a mode")]
    private string chooseLine = "Grab one: EASY or NORMAL";

    [SerializeField]
    [Tooltip("easy mode: the king's health as a share of the normal")]
    [Range(0.1f, 1f)]
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
    [Tooltip("easy mode: waves per minion attack")]
    [Min(1)]
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
    [Tooltip("when the armor takes the great cobra's bite, its head is thrown "
        + "this far back down the trunk; u")]
    private float armorKnockBackU = 20f;

    [SerializeField]
    [Tooltip("...& it is frozen this long after, longer than a coconut's "
        + "freeze, as the armor is the boss fight's one-off reward; s")]
    [Min(0f)]
    private float armorCobraFreezeS = 15f;

    [SerializeField]
    [Tooltip("the golden banana first shows up this long after the first hit "
        + "on the king; s")]
    private float goldenFirstDelayS = 8f;

    [SerializeField]
    [Tooltip("time between two golden banana visits, once one is gone; s")]
    private float goldenIntervalS = 15f;

    [SerializeField]
    [Tooltip("how long a golden banana stays before it vanishes; s")]
    private float goldenShowS = 8f;

    [SerializeField]
    [Tooltip("it grows this far round the trunk from the monkey, so in view; "
        + "deg")]
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
    [Tooltip("win reward: the armor pieces swirl in onto the monkey & pulse "
        + "for this long; s")]
    [Min(0.3f)]
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
    [Tooltip("dots from the first banana to the crown")]
    [Min(2)]
    private int dotCount = 12;

    [SerializeField]
    [Tooltip("dot radius; u")]
    private float dotRadiusU = 0.12f;

    [Header("Colors")]
    [SerializeField]
    [Tooltip("tint of the golden armor & banana")]
    private Color goldenColor = new Color(1f, 0.82f, 0.15f);

    [SerializeField]
    [Tooltip("tint of an attack warning at its brightest")]
    private Color warningColor = new Color(1f, 0.15f, 0.15f);

    [SerializeField]
    [Tooltip("tint of an attack warning at its dimmest")]
    private Color warningDimColor = new Color(0.45f, 0.05f, 0.05f);

    [SerializeField]
    [Tooltip("tint of the minions")]
    private Color minionColor = new Color(0.45f, 0.8f, 0.45f);

    [SerializeField]
    [Tooltip("tint of the king's tail")]
    private Color tailColor = new Color(0.72f, 0.56f, 0.95f);

    [Header("Speech")]
    [SerializeField]
    [Tooltip("line spoken as the king shoves off a monkey that comes too "
        + "close")]
    private string approachLine = "Stay away from me!";

    [SerializeField]
    [Tooltip("line spoken when the monkey drops below the arena entry")]
    private string retreatLine = "Nowhere to run!";

    [SerializeField]
    [Tooltip("line spoken at the first hit on the king")]
    private string firstHitLine = "How dare you!";

    [SerializeField]
    [Tooltip("said once a game, the first time the minions come")]
    private string minionCallLine = "Minions, help me!";

    [SerializeField]
    [Tooltip("the same line is not repeated within this; s")]
    private float speechCooldownS = 4f;

    [SerializeField]
    [Tooltip("a key line, eg the minion call, holds the bubble this long; s")]
    private float keyLineHoldS = 2.2f;
}
