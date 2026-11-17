using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// Every Inspector-tuned value of the Cobra King fight, as the Inspector
/// shows them. Read through the properties; no role writes one.
/// </summary>
[System.Serializable]
public sealed class SpiderKingTuning {
    // Public Members  #########################################################
    public WorldBalanceConfig WorldBalanceConfig => worldBalanceConfig;
    public InputActionReference ConfirmAction => confirmAction;
    public InputActionReference GiveUpAction => giveUpAction;
    public SpiderWebTrap WebPrefab => webPrefab;
    public GameObject BananaBranchPrefab => bananaBranchPrefab;
    public Mesh WatermelonMesh => watermelonMesh;
    public Vector3 WatermelonScale => watermelonScale;
    public Vector3 WatermelonOffset => watermelonOffset;
    public string CourseUrl => courseUrl;
    public float ChoiceSideDeg => choiceSideDeg;
    public float ChoiceAboveU => choiceAboveU;
    public float EasyHealthScale => easyHealthScale;
    public float EasyTimeScale => easyTimeScale;
    public float EasySpeedScale => easySpeedScale;
    public Material AirWallMaterial => airWallMaterial;
    public Material GoldenOutlineMaterial => goldenOutlineMaterial;
    public Color GoldenOutlineColor => goldenOutlineColor;
    public float GoldenOutlineWidthScale => goldenOutlineWidthScale;
    public float GoldenIntervalS => goldenIntervalS;
    public float GoldenShowS => goldenShowS;
    public bool IsTestAtStart => isTestAtStart;
    public float TestStartHeightU => testStartHeightU;
    public float MonkeySpeedScale => monkeySpeedScale;
    public string BossName => bossName;
    public float TrunkRadiusU => trunkRadiusU;
    public float ArenaHeightU => arenaHeightU;
    public float GuardDepthU => guardDepthU;
    public float CameraDistanceScale => cameraDistanceScale;
    public float BranchRadiusU => branchRadiusU;
    public int MelonCount => melonCount;
    public float MelonRespawnS => melonRespawnS;
    public float MelonFlyS => melonFlyS;
    public int MelonDamage => melonDamage;
    public float WebLayEveryU => webLayEveryU;
    public float WebLifeS => webLifeS;
    public float WebGapU => webGapU;
    public float WebMonkeyGapU => webMonkeyGapU;
    public float StalkSpeedU => stalkSpeedU;
    public float RushSpeedU => rushSpeedU;
    public float StalkDistanceU => stalkDistanceU;
    public float DashStartU => dashStartU;
    public float RestS => restS;
    public float MinRestShare => minRestShare;
    public float WebShotWindUpS => webShotWindUpS;
    public float WebShotSpeedU => webShotSpeedU;
    public float DashCooldownS => dashCooldownS;
    public float DashWindUpS => dashWindUpS;
    public float DashLockS => dashLockS;
    public float DashSpeedU => dashSpeedU;
    public float DashOvershootU => dashOvershootU;
    public float DashHitU => dashHitU;
    public Vector2 StageLostShares => stageLostShares;
    public Vector3Int WebShotsPerStage => webShotsPerStage;
    public Vector3Int SlashesPerStage => slashesPerStage;
    public float VolleySlowGapS => volleySlowGapS;
    public float VolleyFastGapS => volleyFastGapS;
    public float LateSpeedScale => lateSpeedScale;
    public float LateChaseScale => lateChaseScale;
    public float LateTimeScale => lateTimeScale;
    public float LateDashCooldownS => lateDashCooldownS;
    public float SlashMinRangeU => slashMinRangeU;
    public float SlashChance => slashChance;
    public float SlashRangeU => slashRangeU;
    public float SlashChargeS => slashChargeS;
    public float SlashWindUpS => slashWindUpS;
    public float SlashSpeedU => slashSpeedU;
    public float SlashWidthU => slashWidthU;
    public float SlashSlowGapS => slashSlowGapS;
    public float SlashFastGapS => slashFastGapS;
    public float CocoonSpeedU => cocoonSpeedU;
    public float CocoonReachU => cocoonReachU;
    public float FirstCocoonSpeedScale => firstCocoonSpeedScale;
    public float DoubleTapS => doubleTapS;
    public float TailWindUpS => tailWindUpS;
    public float TailActiveS => tailActiveS;
    public float TailRecoverS => tailRecoverS;
    public float WaveGoldU => waveGoldU;
    public float ParryReachU => parryReachU;
    public float ParryStunS => parryStunS;
    public float ParrySlowS => parrySlowS;
    public float ParrySlowTimeScale => parrySlowTimeScale;
    public float ParryCloseUpU => parryCloseUpU;
    public float ParryOrbitDeg => parryOrbitDeg;
    public float ParryRollDeg => parryRollDeg;
    public int SlashParryDamage => slashParryDamage;
    public int WebParryDamage => webParryDamage;
    public float TeachSlowScale => teachSlowScale;
    public float TeachGapS => teachGapS;
    public float FinalS => finalS;
    public float ReadyS => readyS;
    public float SaveGraceS => saveGraceS;
    public float FinisherTimeScale => finisherTimeScale;
    public float FinisherRealS => finisherRealS;
    public float ReviveOfferS => reviveOfferS;
    public float AdSkipAfterS => adSkipAfterS;
    public float ReviveReturnSpeedU => reviveReturnSpeedU;
    public float DefeatFallS => defeatFallS;
    public float GlovesEscapeGainScale => glovesEscapeGainScale;
    public float WinCobraDropU => winCobraDropU;
    public string WinTitle => winTitle;
    public string WinLine => winLine;
    public string WinDetailLine => winDetailLine;
    public Color WarnColor => warnColor;
    public Color SilkColor => silkColor;
    public Color GoldenColor => goldenColor;

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
    [Tooltip("the guarding great cobra's head sits this far below the start; "
        + "u")]
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
    [Tooltip("wind up of each slash: the leg rears & the red aim line shows; "
        + "s")]
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
    private float finisherTimeScale = 0.2f;
    [Tooltip("real seconds the slow last blow lasts")]
    [SerializeField]
    [Min(0.1f)]
    private float finisherRealS = 1.4f;
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
}
