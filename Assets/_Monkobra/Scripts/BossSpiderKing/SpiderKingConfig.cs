using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// Every tuned value of the Spider King fight, grouped as the Inspector shows
/// them. Read-only at runtime, so a value changed in Play mode
/// persists in the asset.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Objects/SpiderKingConfig")]
public class SpiderKingConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>watches the ad / goes on after the reward,
    /// Player/Jump; also the double tap of the parry</returns>
    public InputActionReference ConfirmAction => confirmAction;

    /// <returns>gives up the revive offer</returns>
    public InputActionReference GiveUpAction => giveUpAction;

    /// <returns>the climb's spider web, shot by the king & left on
    /// the trunk</returns>
    public SpiderWebTrap WebPrefab => webPrefab;

    /// <returns>the climb's banana branch: bears the arena melons</returns>
    public GameObject BananaBranchPrefab => bananaBranchPrefab;

    /// <returns>the melon model swapped onto the banana</returns>
    public Mesh WatermelonMesh => watermelonMesh;

    /// <returns>local scale of the melon model on the banana's mesh
    /// object</returns>
    public Vector3 WatermelonScale => watermelonScale;

    /// <returns>local offset of the melon model</returns>
    public Vector3 WatermelonOffset => watermelonOffset;

    /// <returns>the course page the mock ad's button opens</returns>
    public string CourseUrl => courseUrl;

    /// <returns>after a revive the choice melons grow this far to
    /// each side of where the fight began; deg</returns>
    public float ChoiceSideDeg => choiceSideDeg;

    /// <returns>& this high above it; u</returns>
    public float ChoiceAboveU => choiceAboveU;

    /// <returns>easy mode: share of the health</returns>
    public float EasyHealthScale => easyHealthScale;

    /// <returns>easy mode: scale on rests, wind ups & gaps, > 1 is
    /// slower</returns>
    public float EasyTimeScale => easyTimeScale;

    /// <returns>easy mode: scale on the speed of its attacks & crawl</returns>
    public float EasySpeedScale => easySpeedScale;

    /// <returns>easy mode: scale on a bullet's & a slash's gold &
    /// parry reach, > 1 is an easier parry</returns>
    public float EasyReachScale => easyReachScale;

    /// <returns>a see-through lit material, eg the spider web's,
    /// copied for the arena's top</returns>
    public Material AirWallMaterial => airWallMaterial;

    /// <returns>the Cobra King's golden outline, copied & recolored</returns>
    public Material GoldenOutlineMaterial => goldenOutlineMaterial;

    /// <returns>outline tint of the golden banana</returns>
    public Color GoldenOutlineColor => goldenOutlineColor;

    /// <returns>outline width of the golden banana, as a multiple of
    /// the normal width</returns>
    public float GoldenOutlineWidthScale => goldenOutlineWidthScale;

    /// <returns>seconds b/t two golden melons; one only shows w/ no
    /// armor on</returns>
    public float GoldenIntervalS => goldenIntervalS;

    /// <returns>seconds a golden melon stays b4 it goes</returns>
    public float GoldenShowS => goldenShowS;

    /// <returns>test only: the fight begins once the monkey has
    /// climbed testStartHeightU, w/o the Cobra King or the trigger
    /// height; switch off for the real run</returns>
    public bool IsTestAtStart => isTestAtStart;

    /// <returns>test only: climbed height the test fight begins at; u</returns>
    public float TestStartHeightU => testStartHeightU;

    /// <returns>scale on the monkey's climb & orbit speed in the
    /// arena</returns>
    public float MonkeySpeedScale => monkeySpeedScale;

    /// <returns>name shown over the health bar</returns>
    public string BossName => bossName;

    /// <returns>trunk radius; u</returns>
    public float TrunkRadiusU => trunkRadiusU;

    /// <returns>height of the arena above where the fight began; the
    /// climb is capped there; u</returns>
    public float ArenaHeightU => arenaHeightU;

    /// <returns>the guarding great cobra's head sits this far below
    /// the start; u</returns>
    public float GuardDepthU => guardDepthU;

    /// <returns>how much farther the camera pulls back in the fight,
    /// 1 = none</returns>
    public float CameraDistanceScale => cameraDistanceScale;

    /// <returns>radius the branch roots sit at, as the segments'
    /// branch ring; u</returns>
    public float BranchRadiusU => branchRadiusU;

    /// <returns>melon branches in the arena</returns>
    public int MelonCount => melonCount;

    /// <returns>seconds a grabbed melon takes to grow back</returns>
    public float MelonRespawnS => melonRespawnS;

    /// <returns>flight of a thrown melon to the spider; s</returns>
    public float MelonFlyS => melonFlyS;

    /// <returns>health a melon takes off the spider</returns>
    public int MelonDamage => melonDamage;

    /// <returns>the spider spins webs all the way as it crawls, one
    /// every this far, a trail behind it; the arena starts w/o webs;
    /// u</returns>
    public float WebLayEveryU => webLayEveryU;

    /// <returns>seconds a web stays on the trunk, its last second
    /// shrinking; any number of them, each goes by itself</returns>
    public float WebLifeS => webLifeS;

    /// <returns>min gap b/t two webs; u</returns>
    public float WebGapU => webGapU;

    /// <returns>a web is never laid this close to the monkey; u</returns>
    public float WebMonkeyGapU => webMonkeyGapU;

    /// <returns>crawl speed after the monkey; u/s</returns>
    public float StalkSpeedU => stalkSpeedU;

    /// <returns>speed it rushes at to get in the camera's view, so
    /// its charge can be seen; u/s</returns>
    public float RushSpeedU => rushSpeedU;

    /// <returns>it crawls after the monkey till this close; u</returns>
    public float StalkDistanceU => stalkDistanceU;

    /// <returns>this close it dashes; u</returns>
    public float DashStartU => dashStartU;

    /// <returns>seconds between attacks at full health</returns>
    public float RestS => restS;

    /// <returns>share of that rest left at no health: weaker, faster</returns>
    public float MinRestShare => minRestShare;

    /// <returns>the charge b4 a web wave: the spider rears up, its
    /// body swells & a white orb gathers at its head; s</returns>
    public float WebShotWindUpS => webShotWindUpS;

    /// <returns>speed of the web wave, homing in on the monkey, so it
    /// can only be parried, never dodged; u/s</returns>
    public float WebShotSpeedU => webShotSpeedU;

    /// <returns>seconds after a dash b4 the next one</returns>
    public float DashCooldownS => dashCooldownS;

    /// <returns>wind up of the dash: the legs rear up & a red lane
    /// shows its way on the bark; s</returns>
    public float DashWindUpS => dashWindUpS;

    /// <returns>the lane stops following the monkey this long b4 the
    /// dash, the time to step off it; s</returns>
    public float DashLockS => dashLockS;

    /// <returns>speed of the dash; u/s</returns>
    public float DashSpeedU => dashSpeedU;

    /// <returns>the dash runs this far past where the monkey was; u</returns>
    public float DashOvershootU => dashOvershootU;

    /// <returns>the dash catches a monkey this close, fatal; u</returns>
    public float DashHitU => dashHitU;

    /// <returns>the fight gets harder in three stages as the spider's
    /// health goes: under these shares of its health lost, stage 1,
    /// then 2, then 3</returns>
    public Vector2 StageLostShares => stageLostShares;

    /// <returns>web bullets fired after one charge in stages 1, 2 &
    /// 3: in the later ones a volley, each to bat back</returns>
    public Vector3Int WebShotsPerStage => webShotsPerStage;

    /// <returns>slashes in a row in stages 1, 2 & 3; one at a time, a
    /// row of them only in the hardcore run's later fights</returns>
    public Vector3Int SlashesPerStage => slashesPerStage;

    /// <returns>the long gap b/t two bullets of a volley; s</returns>
    public float VolleySlowGapS => volleySlowGapS;

    /// <returns>the short gap b/t two bullets of a volley; s</returns>
    public float VolleyFastGapS => volleyFastGapS;

    /// <returns>at the last stage: scale on the speed of slashes,
    /// bullets, its crawl & dash, lerped from 1 as its health goes</returns>
    public float LateSpeedScale => lateSpeedScale;

    /// <returns>at the last stage: scale on its chase & circling
    /// only, lerped from 1 as its health goes</returns>
    public float LateChaseScale => lateChaseScale;

    /// <returns>at the last stage: scale on wind ups & gaps, lerped
    /// from 1</returns>
    public float LateTimeScale => lateTimeScale;

    /// <returns>seconds b/t dashes at the last stage, lerped from
    /// dashCooldownS</returns>
    public float LateDashCooldownS => lateDashCooldownS;

    /// <returns>it never slashes from closer than this; u</returns>
    public float SlashMinRangeU => slashMinRangeU;

    /// <returns>chance an attack from afar is a slash, not a web shot</returns>
    public float SlashChance => slashChance;

    /// <returns>it slashes from this far at most, & a crescent flies
    /// about this far; u</returns>
    public float SlashRangeU => slashRangeU;

    /// <returns>the charge b4 the slashes, seen like the web shot's:
    /// it rears up & swells, its front legs high; s</returns>
    public float SlashChargeS => slashChargeS;

    /// <returns>wind up of each slash: the leg rears & the red aim
    /// line shows; s</returns>
    public float SlashWindUpS => slashWindUpS;

    /// <returns>speed of a crescent along the bark; u/s</returns>
    public float SlashSpeedU => slashSpeedU;

    /// <returns>width of a crescent across its way; u</returns>
    public float SlashWidthU => slashWidthU;

    /// <returns>the long gap after a slash b4 the next one winds up;
    /// s</returns>
    public float SlashSlowGapS => slashSlowGapS;

    /// <returns>the short gap after a slash b4 the next one winds up;
    /// s</returns>
    public float SlashFastGapS => slashFastGapS;

    /// <returns>crawl speed to a trapped monkey; u/s</returns>
    public float CocoonSpeedU => cocoonSpeedU;

    /// <returns>this close to a trapped monkey it wraps it, fatal; u</returns>
    public float CocoonReachU => cocoonReachU;

    /// <returns>scale on the crawl the first time the monkey is
    /// trapped</returns>
    public float FirstCocoonSpeedScale => firstCocoonSpeedScale;

    /// <returns>max seconds b/t the two Space taps of a parry</returns>
    public float DoubleTapS => doubleTapS;

    /// <returns>the tail swing's wind up: it draws back, nothing is
    /// batted yet; real s</returns>
    public float TailWindUpS => tailWindUpS;

    /// <returns>the swing itself: only now a bullet or slash is
    /// batted back; real s</returns>
    public float TailActiveS => tailActiveS;

    /// <returns>the swing's follow through after a whiff: the tail
    /// swings back & no new swing can start, so mashing never works,
    /// only timing; a parry readies the tail at once; real s</returns>
    public float TailRecoverS => tailRecoverS;

    /// <returns>the wave turns gold this close to the monkey: the
    /// moment to double tap; u</returns>
    public float WaveGoldU => waveGoldU;

    /// <returns>an armed parry bats the wave back this close to the
    /// monkey; u</returns>
    public float ParryReachU => parryReachU;

    /// <returns>seconds the spider is stuck in its own batted back
    /// web</returns>
    public float ParryStunS => parryStunS;

    /// <returns>real seconds of slow motion on a parry, the camera
    /// close in on the monkey</returns>
    public float ParrySlowS => parrySlowS;

    /// <returns>time scale of that slow motion</returns>
    public float ParrySlowTimeScale => parrySlowTimeScale;

    /// <returns>how close the parry shot's camera comes to the
    /// monkey; u</returns>
    public float ParryCloseUpU => parryCloseUpU;

    /// <returns>how far the parry shot's camera swings round the
    /// monkey; deg</returns>
    public float ParryOrbitDeg => parryOrbitDeg;

    /// <returns>the parry shot's tilt, a dutch angle; deg</returns>
    public float ParryRollDeg => parryRollDeg;

    /// <returns>health a batted back slash takes off the spider</returns>
    public int SlashParryDamage => slashParryDamage;

    /// <returns>health a batted back web bullet takes off the spider;
    /// 0: it only sticks it</returns>
    public int WebParryDamage => webParryDamage;

    /// <returns>scale on wind ups in the lessons</returns>
    public float TeachSlowScale => teachSlowScale;

    /// <returns>seconds b4 the next lesson attack</returns>
    public float TeachGapS => teachGapS;

    /// <returns>seconds the last lesson's banner stays b4 the fight</returns>
    public float FinalS => finalS;

    /// <returns>seconds of calm after a revive b4 the fight goes on</returns>
    public float ReadyS => readyS;

    /// <returns>seconds after the armor saves the monkey that the
    /// same hit can't count twice; no real grace: the next hit counts</returns>
    public float SaveGraceS => saveGraceS;

    /// <returns>time scale of the slow last blow</returns>
    public float FinisherTimeScale => finisherTimeScale;

    /// <returns>real seconds the slow last blow lasts</returns>
    public float FinisherRealS => finisherRealS;

    /// <returns>seconds the revive offer waits</returns>
    public float ReviveOfferS => reviveOfferS;

    /// <returns>seconds of the mock ad b4 it can be skipped</returns>
    public float AdSkipAfterS => adSkipAfterS;

    /// <returns>speed of the flight back to the start; u/s</returns>
    public float ReviveReturnSpeedU => reviveReturnSpeedU;

    /// <returns>seconds the beaten spider falls b4 the reward</returns>
    public float DefeatFallS => defeatFallS;

    /// <returns>web escape gain scale the silk gloves give</returns>
    public float GlovesEscapeGainScale => glovesEscapeGainScale;

    /// <returns>title of the victory panel</returns>
    public string WinTitle => winTitle;

    /// <returns>reward line of the victory panel</returns>
    public string WinLine => winLine;

    /// <returns>detail line under the reward</returns>
    public string WinDetailLine => winDetailLine;

    /// <returns>tint of an attack warning</returns>
    public Color WarnColor => warnColor;

    /// <returns>tint of the spider's silk</returns>
    public Color SilkColor => silkColor;

    /// <returns>tint of the golden armor & banana</returns>
    public Color GoldenColor => goldenColor;

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("watches the ad / goes on after the reward, Player/Jump; also "
        + "the double tap of the parry")]
    private InputActionReference confirmAction;

    [SerializeField]
    [Tooltip("gives up the revive offer")]
    private InputActionReference giveUpAction;

    [SerializeField]
    [Tooltip("the climb's spider web, shot by the king & left on the trunk")]
    private SpiderWebTrap webPrefab;

    [SerializeField]
    [Tooltip("the climb's banana branch: bears the arena melons")]
    private GameObject bananaBranchPrefab;

    [SerializeField]
    [Tooltip("the melon model swapped onto the banana")]
    private Mesh watermelonMesh;

    [SerializeField]
    [Tooltip("local scale of the melon model on the banana's mesh object")]
    private Vector3 watermelonScale = new Vector3(6f, 6f, 6f);

    [SerializeField]
    [Tooltip("local offset of the melon model")]
    private Vector3 watermelonOffset = new Vector3(0f, -0.629f, 0.06f);

    [SerializeField]
    [Tooltip("the course page the mock ad's button opens")]
    private string courseUrl =
        "https://classes.usc.edu/term-20263/course/csci-526";

    [Header("Mode Choice")]
    [SerializeField]
    [Tooltip("after a revive the choice melons grow this far to each side of "
        + "where the fight began; deg")]
    private float choiceSideDeg = 28f;

    [SerializeField]
    [Tooltip("& this high above it; u")]
    private float choiceAboveU = 1.5f;

    [SerializeField]
    [Tooltip("easy mode: share of the health")]
    [Range(0.1f, 1f)]
    private float easyHealthScale = 0.45f;

    [SerializeField]
    [Tooltip("easy mode: scale on rests, wind ups & gaps, > 1 is slower")]
    [Min(1f)]
    private float easyTimeScale = 1.7f;

    [SerializeField]
    [Tooltip("easy mode: scale on the speed of its attacks & crawl")]
    [Range(0.3f, 1f)]
    private float easySpeedScale = 0.7f;

    [SerializeField]
    [Tooltip("easy mode: scale on a bullet's & a slash's gold & parry reach, "
        + "> 1 is an easier parry")]
    [Min(1f)]
    private float easyReachScale = 1.3f;

    [SerializeField]
    [Tooltip("a see-through lit material, eg the spider web's, copied for the "
        + "arena's top")]
    private Material airWallMaterial;

    [Header("Golden Melon")]
    [SerializeField]
    [Tooltip("the Cobra King's golden outline, copied & recolored")]
    private Material goldenOutlineMaterial;

    [SerializeField]
    [Tooltip("outline tint of the golden banana")]
    private Color goldenOutlineColor = new Color(1f, 0.75f, 0f);

    [SerializeField]
    [Tooltip("outline width of the golden banana, as a multiple of the normal "
        + "width")]
    [Min(1f)]
    private float goldenOutlineWidthScale = 2f;

    [SerializeField]
    [Tooltip("seconds b/t two golden melons; one only shows w/ no armor on")]
    [Min(1f)]
    private float goldenIntervalS = 10f;

    [SerializeField]
    [Tooltip("seconds a golden melon stays b4 it goes")]
    [Min(1f)]
    private float goldenShowS = 8f;

    [Header("Arena")]
    [SerializeField]
    [Tooltip("test only: the fight begins once the monkey has climbed "
        + "testStartHeightU, w/o the Cobra King or the trigger height; "
        + "switch off for the real run")]
    private bool isTestAtStart;

    [SerializeField]
    [Tooltip("test only: climbed height the test fight begins at; u")]
    [Min(0f)]
    private float testStartHeightU = 30f;

    [SerializeField]
    [Tooltip("scale on the monkey's climb & orbit speed in the arena")]
    [Min(0.5f)]
    private float monkeySpeedScale = 1.2f;

    [SerializeField]
    [Tooltip("name shown over the health bar")]
    private string bossName = "SPIDER KING";

    [SerializeField]
    [Tooltip("trunk radius; u")]
    [Min(0.5f)]
    private float trunkRadiusU = 5f;

    [SerializeField]
    [Tooltip("height of the arena above where the fight began; the climb is "
        + "capped there; u")]
    [Min(4f)]
    private float arenaHeightU = 18f;

    [SerializeField]
    [Tooltip("the guarding great cobra's head sits this far below the start; "
        + "u")]
    [Min(2f)]
    private float guardDepthU = 20f;

    [SerializeField]
    [Tooltip("how much farther the camera pulls back in the fight, 1 = none")]
    [Min(1f)]
    private float cameraDistanceScale = 1.6f;

    [SerializeField]
    [Tooltip("radius the branch roots sit at, as the segments' branch ring; u")]
    [Min(0.5f)]
    private float branchRadiusU = 6.6f;

    [Header("Melons")]
    [SerializeField]
    [Tooltip("melon branches in the arena")]
    [Min(1)]
    private int melonCount = 5;

    [SerializeField]
    [Tooltip("seconds a grabbed melon takes to grow back")]
    [Min(0.5f)]
    private float melonRespawnS = 7f;

    [SerializeField]
    [Tooltip("flight of a thrown melon to the spider; s")]
    [Min(0.1f)]
    private float melonFlyS = 0.55f;

    [SerializeField]
    [Tooltip("health a melon takes off the spider")]
    [Min(1)]
    private int melonDamage = 1;

    [Header("Laid Webs")]
    [SerializeField]
    [Tooltip("the spider spins webs all the way as it crawls, one every this "
        + "far, a trail behind it; the arena starts w/o webs; u")]
    [Min(0.5f)]
    private float webLayEveryU = 1.5f;

    [SerializeField]
    [Tooltip("seconds a web stays on the trunk, its last second shrinking; "
        + "any number of them, each goes by itself")]
    [Min(1f)]
    private float webLifeS = 10f;

    [SerializeField]
    [Tooltip("min gap b/t two webs; u")]
    [Min(0.3f)]
    private float webGapU = 1.4f;

    [SerializeField]
    [Tooltip("a web is never laid this close to the monkey; u")]
    [Min(0.5f)]
    private float webMonkeyGapU = 2.5f;

    [Header("Movement")]
    [SerializeField]
    [Tooltip("crawl speed after the monkey; u/s")]
    [Min(0.1f)]
    private float stalkSpeedU = 4.8f;

    [SerializeField]
    [Tooltip("speed it rushes at to get in the camera's view, so its charge "
        + "can be seen; u/s")]
    [Min(0.1f)]
    private float rushSpeedU = 9f;

    [SerializeField]
    [Tooltip("it crawls after the monkey till this close; u")]
    [Min(0.5f)]
    private float stalkDistanceU = 4.8f;

    [SerializeField]
    [Tooltip("this close it dashes; u")]
    [Min(0.5f)]
    private float dashStartU = 5f;

    [Header("Rhythm")]
    [SerializeField]
    [Tooltip("seconds between attacks at full health")]
    [Min(0.3f)]
    private float restS = 3.2f;

    [SerializeField]
    [Tooltip("share of that rest left at no health: weaker, faster")]
    [Range(0.1f, 1f)]
    private float minRestShare = 0.4375f;

    [Header("Web Wave")]
    [SerializeField]
    [Tooltip("the charge b4 a web wave: the spider rears up, its body swells "
        + "& a white orb gathers at its head; s")]
    [Min(0.1f)]
    private float webShotWindUpS = 0.8f;

    [SerializeField]
    [Tooltip("speed of the web wave, homing in on the monkey, so it can only "
        + "be parried, never dodged; u/s")]
    [Min(1f)]
    private float webShotSpeedU = 20f;

    [Header("Dash")]
    [SerializeField]
    [Tooltip("seconds after a dash b4 the next one")]
    [Min(0f)]
    private float dashCooldownS = 4.2f;

    [SerializeField]
    [Tooltip("wind up of the dash: the legs rear up & a red lane shows its "
        + "way on the bark; s")]
    [Min(0.1f)]
    private float dashWindUpS = 0.9f;

    [SerializeField]
    [Tooltip("the lane stops following the monkey this long b4 the dash, the "
        + "time to step off it; s")]
    [Min(0.05f)]
    private float dashLockS = 0.45f;

    [SerializeField]
    [Tooltip("speed of the dash; u/s")]
    [Min(1f)]
    private float dashSpeedU = 22f;

    [SerializeField]
    [Tooltip("the dash runs this far past where the monkey was; u")]
    [Min(0f)]
    private float dashOvershootU = 3f;

    [SerializeField]
    [Tooltip("the dash catches a monkey this close, fatal; u")]
    [Min(0.3f)]
    private float dashHitU = 1.6f;

    [Header("Getting Harder")]
    [SerializeField]
    [Tooltip("the fight gets harder in three stages as the spider's health "
        + "goes: under these shares of its health lost, stage 1, then 2, "
        + "then 3")]
    private Vector2 stageLostShares = new Vector2(0.5f, 0.8f);

    [SerializeField]
    [Tooltip("web bullets fired after one charge in stages 1, 2 & 3: in the "
        + "later ones a volley, each to bat back")]
    private Vector3Int webShotsPerStage = new Vector3Int(1, 2, 2);

    [SerializeField]
    [Tooltip("slashes in a row in stages 1, 2 & 3; one at a time, a row of "
        + "them only in the hardcore run's later fights")]
    private Vector3Int slashesPerStage = new Vector3Int(1, 1, 1);

    [SerializeField]
    [Tooltip("the long gap b/t two bullets of a volley; s")]
    [Min(0.05f)]
    private float volleySlowGapS = 1.1f;

    [SerializeField]
    [Tooltip("the short gap b/t two bullets of a volley; s")]
    [Min(0.05f)]
    private float volleyFastGapS = 0.7f;

    [SerializeField]
    [Tooltip("at the last stage: scale on the speed of slashes, bullets, its "
        + "crawl & dash, lerped from 1 as its health goes")]
    [Min(1f)]
    private float lateSpeedScale = 1.25f;

    [SerializeField]
    [Tooltip("at the last stage: scale on its chase & circling only, lerped "
        + "from 1 as its health goes")]
    [Min(1f)]
    private float lateChaseScale = 1.04f;

    [SerializeField]
    [Tooltip("at the last stage: scale on wind ups & gaps, lerped from 1")]
    [Range(0.3f, 1f)]
    private float lateTimeScale = 0.85f;

    [SerializeField]
    [Tooltip("seconds b/t dashes at the last stage, lerped from dashCooldownS")]
    [Min(0f)]
    private float lateDashCooldownS = 2.4f;

    [Header("Slash")]
    [SerializeField]
    [Tooltip("it never slashes from closer than this; u")]
    [Min(0f)]
    private float slashMinRangeU = 4f;

    [SerializeField]
    [Tooltip("chance an attack from afar is a slash, not a web shot")]
    [Range(0f, 1f)]
    private float slashChance = 0.5f;

    [SerializeField]
    [Tooltip("it slashes from this far at most, & a crescent flies about this "
        + "far; u")]
    [Min(2f)]
    private float slashRangeU = 16f;

    [SerializeField]
    [Tooltip("the charge b4 the slashes, seen like the web shot's: it rears "
        + "up & swells, its front legs high; s")]
    [Min(0.1f)]
    private float slashChargeS = 0.8f;

    [SerializeField]
    [Tooltip("wind up of each slash: the leg rears & the red aim line shows; "
        + "s")]
    [Min(0.05f)]
    private float slashWindUpS = 0.3f;

    [SerializeField]
    [Tooltip("speed of a crescent along the bark; u/s")]
    [Min(1f)]
    private float slashSpeedU = 18f;

    [SerializeField]
    [Tooltip("width of a crescent across its way; u")]
    [Min(0.5f)]
    private float slashWidthU = 4.5f;

    [SerializeField]
    [Tooltip("the long gap after a slash b4 the next one winds up; s")]
    [Min(0f)]
    private float slashSlowGapS = 0.75f;

    [SerializeField]
    [Tooltip("the short gap after a slash b4 the next one winds up; s")]
    [Min(0f)]
    private float slashFastGapS = 0.25f;

    [Header("Cocoon")]
    [SerializeField]
    [Tooltip("crawl speed to a trapped monkey; u/s")]
    [Min(0.1f)]
    private float cocoonSpeedU = 3f;

    [SerializeField]
    [Tooltip("this close to a trapped monkey it wraps it, fatal; u")]
    [Min(0.3f)]
    private float cocoonReachU = 1.6f;

    [SerializeField]
    [Tooltip("scale on the crawl the first time the monkey is trapped")]
    [Range(0.2f, 1f)]
    private float firstCocoonSpeedScale = 0.6f;

    [Header("Tail Parry")]
    [SerializeField]
    [Tooltip("max seconds b/t the two Space taps of a parry")]
    [Min(0.05f)]
    private float doubleTapS = 0.3f;

    [SerializeField]
    [Tooltip("the tail swing's wind up: it draws back, nothing is batted yet; "
        + "real s")]
    [Min(0f)]
    private float tailWindUpS = 0.06f;

    [SerializeField]
    [Tooltip("the swing itself: only now a bullet or slash is batted back; "
        + "real s")]
    [Min(0.05f)]
    private float tailActiveS = 0.2f;

    [SerializeField]
    [Tooltip("the swing's follow through after a whiff: the tail swings back "
        + "& no new swing can start, so mashing never works, only timing; "
        + "a parry readies the tail at once; real s")]
    [Min(0f)]
    private float tailRecoverS = 0.45f;

    [SerializeField]
    [Tooltip("the wave turns gold this close to the monkey: the moment to "
        + "double tap; u")]
    [Min(0.5f)]
    private float waveGoldU = 3.2f;

    [SerializeField]
    [Tooltip("an armed parry bats the wave back this close to the monkey; u")]
    [Min(0.3f)]
    private float parryReachU = 2f;

    [SerializeField]
    [Tooltip("seconds the spider is stuck in its own batted back web")]
    [Min(0.5f)]
    private float parryStunS = 3.5f;

    [SerializeField]
    [Tooltip("real seconds of slow motion on a parry, the camera close in on "
        + "the monkey")]
    [Min(0f)]
    private float parrySlowS = 0.75f;

    [SerializeField]
    [Tooltip("time scale of that slow motion")]
    [Range(0.05f, 1f)]
    private float parrySlowTimeScale = 0.2f;

    [SerializeField]
    [Tooltip("how close the parry shot's camera comes to the monkey; u")]
    [Min(1f)]
    private float parryCloseUpU = 4.6f;

    [SerializeField]
    [Tooltip("how far the parry shot's camera swings round the monkey; deg")]
    [Range(0f, 90f)]
    private float parryOrbitDeg = 32f;

    [SerializeField]
    [Tooltip("the parry shot's tilt, a dutch angle; deg")]
    [Range(0f, 30f)]
    private float parryRollDeg = 10f;

    [SerializeField]
    [Tooltip("health a batted back slash takes off the spider")]
    [Min(0)]
    private int slashParryDamage = 1;

    [SerializeField]
    [Tooltip("health a batted back web bullet takes off the spider; 0: it "
        + "only sticks it")]
    [Min(0)]
    private int webParryDamage = 0;

    [Header("Teaching")]
    [SerializeField]
    [Tooltip("scale on wind ups in the lessons")]
    [Min(1f)]
    private float teachSlowScale = 1.8f;

    [SerializeField]
    [Tooltip("seconds b4 the next lesson attack")]
    [Min(0.3f)]
    private float teachGapS = 1.6f;

    [SerializeField]
    [Tooltip("seconds the last lesson's banner stays b4 the fight")]
    [Min(0.5f)]
    private float finalS = 2f;

    [SerializeField]
    [Tooltip("seconds of calm after a revive b4 the fight goes on")]
    [Min(0.5f)]
    private float readyS = 3f;

    [Header("Loss & Revive")]
    [SerializeField]
    [Tooltip("seconds after the armor saves the monkey that the same hit "
        + "can't count twice; no real grace: the next hit counts")]
    [Min(0f)]
    private float saveGraceS = 0.25f;

    [SerializeField]
    [Tooltip("time scale of the slow last blow")]
    [Range(0.05f, 1f)]
    private float finisherTimeScale = 0.2f;

    [SerializeField]
    [Tooltip("real seconds the slow last blow lasts")]
    [Min(0.1f)]
    private float finisherRealS = 1.4f;

    [SerializeField]
    [Tooltip("seconds the revive offer waits")]
    [Min(1f)]
    private float reviveOfferS = 5f;

    [SerializeField]
    [Tooltip("seconds of the mock ad b4 it can be skipped")]
    [Min(0f)]
    private float adSkipAfterS = 3f;

    [SerializeField]
    [Tooltip("speed of the flight back to the start; u/s")]
    [Min(1f)]
    private float reviveReturnSpeedU = 20f;

    [Header("Win")]
    [SerializeField]
    [Tooltip("seconds the beaten spider falls b4 the reward")]
    [Min(0.2f)]
    private float defeatFallS = 2f;

    [SerializeField]
    [Tooltip("web escape gain scale the silk gloves give")]
    [Min(1f)]
    private float glovesEscapeGainScale = 2f;

    [SerializeField]
    [Tooltip("title of the victory panel")]
    private string winTitle = "SPIDER KING DEFEATED!";

    [SerializeField]
    [Tooltip("reward line of the victory panel")]
    private string winLine = "You got the Silk Gloves";

    [SerializeField]
    [Tooltip("detail line under the reward")]
    private string winDetailLine = "Get out of webs 2x faster";

    [Header("Colors")]
    [SerializeField]
    [Tooltip("tint of an attack warning")]
    private Color warnColor = new Color(0.95f, 0.2f, 0.15f);

    [SerializeField]
    [Tooltip("tint of the spider's silk")]
    private Color silkColor = new Color(0.92f, 0.92f, 0.95f);

    [SerializeField]
    [Tooltip("tint of the golden armor & banana")]
    private Color goldenColor = new Color(1f, 0.8f, 0.15f);
}
