using UnityEngine;

/// <summary>
/// Balance of the generated world: the ramped difficulty curves that every
/// hazard chance is read against, the 4 branch kinds (empty, banana, coconut,
/// pineapple), rough bark, spider webs and the boss zone & fight. Player and
/// scoring numbers live in <see cref="PlayerBalanceConfig"/>, cobra numbers in
/// <see cref="CobraBalanceConfig"/>.
/// </summary>
[CreateAssetMenu(
    fileName = "WorldBalanceConfig",
    menuName = "Scriptable Objects/WorldBalanceConfig"
)]
public class WorldBalanceConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>x: progress 0~1 from the starting zone exit to the boss zone
    /// start; y: ramped difficulty 0~1</returns>
    public AnimationCurve PreBossRampCurve => preBossRampCurve;

    /// <returns>x: progress 0~1 from the Cobra King zone end to the Spider
    /// King zone start; y: ramped difficulty 1~2</returns>
    public AnimationCurve MidBossRampCurve => midBossRampCurve;

    /// <returns>x: height above the Spider King zone end; world u; y: ramped
    /// difficulty from 2 up</returns>
    public AnimationCurve PostBossRampCurve => postBossRampCurve;

    /// <returns>no branch grows within this distance of the monkey while the
    /// starting zone lasts, so a restart never spawns the monkey into one;
    /// u</returns>
    public float StartingZoneBranchClearRadiusU =>
        startingZoneBranchClearRadiusU;

    /// <returns>chance 0~1 that one banana branch attempt succeeds on a
    /// segment whose middle lies in the starting zone, in place of the banana
    /// branch curve; the other kinds do not grow there</returns>
    public float StartingZoneBananaBranchChance =>
        startingZoneBananaBranchChance;

    /// <returns>how far, in degrees either way, a guaranteed coconut or
    /// pineapple branch may sit from the monkey's angle around the trunk when
    /// its segment rolls; 0 grows it exactly there; deg</returns>
    public float GuaranteedBranchYawSpreadDeg => guaranteedBranchYawSpreadDeg;

    /// <param name="kind">which of the 4 branch kinds</param>
    /// <returns>attempts rolled per segment for that kind, the most one
    /// segment can hold of it</returns>
    public int GetBranchAttemptCount(BranchKind kind) {
        return kind switch {
            BranchKind.Empty => emptyBranchAttemptCount,
            BranchKind.Banana => bananaBranchAttemptCount,
            BranchKind.Coconut => coconutBranchAttemptCount,
            BranchKind.Pineapple => pineappleBranchAttemptCount,
            _ => 0
        };
    }

    /// <param name="kind">which of the 4 branch kinds</param>
    /// <returns>x: ramped difficulty; y: chance 0~1 that one attempt grows a
    /// branch of that kind</returns>
    public AnimationCurve GetBranchProbabilityCurve(BranchKind kind) {
        return kind switch {
            BranchKind.Empty => emptyBranchProbabilityCurve,
            BranchKind.Banana => bananaBranchProbabilityCurve,
            BranchKind.Coconut => coconutBranchProbabilityCurve,
            BranchKind.Pineapple => pineappleBranchProbabilityCurve,
            _ => null
        };
    }

    /// <param name="kind">which of the 4 branch kinds</param>
    /// <returns>climbed height where one branch of that kind always grows,
    /// whatever its chance curve rolls; negative for a kind w/o one; u
    /// </returns>
    public float GetGuaranteedBranchHeightU(BranchKind kind) {
        return kind switch {
            BranchKind.Coconut => coconutGuaranteedHeightU,
            BranchKind.Pineapple => pineappleGuaranteedHeightU,
            _ => -1f
        };
    }

    public float RoughBarkSpeedMultiplier => roughBarkSpeedMultiplier;
    public float RoughBarkBlendS => roughBarkBlendS;
    public int RoughBarkAttemptCount => roughBarkAttemptCount;
    public AnimationCurve RoughBarkSpawnProbabilityCurve =>
        roughBarkSpawnProbabilityCurve;

    /// <returns>web creation attempts rolled per segment</returns>
    public int WebAttemptCount => webAttemptCount;

    /// <returns>x: ramped difficulty 0~3; y: chance 0~1 that one attempt
    /// creates a web</returns>
    public AnimationCurve WebSpawnProbabilityCurve =>
        webSpawnProbabilityCurve;

    /// <returns>climbed height where the Cobra King zone begins: the ramp
    /// reaches 1 here and nothing unrelated to the fight is generated from
    /// here on; u</returns>
    public float BossZoneStartU => bossZoneStartU;

    /// <returns>height climbed since game start at which the boss fight
    /// begins; u</returns>
    public float BossTriggerHeightU => bossTriggerHeightU;

    /// <returns>climbed height where the Cobra King zone ends: ordinary
    /// generation resumes and the ramp climbs from 1 toward 2 from here; u
    /// </returns>
    public float BossZoneEndU => bossZoneEndU;

    /// <returns>health of the Cobra King</returns>
    public int BossMaxHealth => bossMaxHealth;

    /// <returns>health a thrown banana takes off the Cobra King per hit
    /// </returns>
    public int BossBananaDamage => bossBananaDamage;

    /// <returns>climbed height where the Spider King's zone begins: the ramp
    /// reaches 2 here and only its fight generates anything from here to its
    /// zone end; u</returns>
    public float SpiderZoneStartU => spiderZoneStartU;

    /// <returns>climbed height at which the Spider King fight begins; u
    /// </returns>
    public float SpiderTriggerHeightU => spiderTriggerHeightU;

    /// <returns>climbed height where the Spider King's zone ends: ordinary
    /// generation resumes and the ramp climbs above 2 from here; u</returns>
    public float SpiderZoneEndU => spiderZoneEndU;

    /// <returns>climbed height from which the Spider King is seen waiting;
    /// u</returns>
    public float SpiderJumpHeightU => spiderJumpHeightU;

    /// <returns>health of the Spider King</returns>
    public int SpiderMaxHealth => spiderMaxHealth;

    /// <returns>climbed height b/t two bosses in the endless run's boss
    /// rush; u</returns>
    public float EndlessBossIntervalU => endlessBossIntervalU;

    /// <returns>scale on the monkey's & the great cobra's climb in the
    /// endless run, outside a boss fight</returns>
    public float EndlessSpeedScale => endlessSpeedScale;

    /// <returns>a boss's extra health per earlier boss in the endless run, a
    /// share of its base health</returns>
    public float EndlessHealthStep => endlessHealthStep;

    /// <returns>how much shorter a boss's wind ups, warnings & rests get per
    /// earlier boss in the endless run, a share</returns>
    public float EndlessTimeStep => endlessTimeStep;

    /// <returns>the shortest those times get in the endless run, a share
    /// </returns>
    public float EndlessMinTimeScale => endlessMinTimeScale;

    /// <returns>how much faster a boss's shots & dashes get per earlier boss
    /// in the endless run, a share</returns>
    public float EndlessSpeedStep => endlessSpeedStep;

    /// <returns>the fastest they get in the endless run, a scale</returns>
    public float EndlessMaxSpeedScale => endlessMaxSpeedScale;

    /// <returns>every endless boss's health, a scale, before the per boss
    /// step: the monkey is 3x as fast in its fights too</returns>
    public float EndlessFightHealthScale => endlessFightHealthScale;

    /// <returns>every endless boss's wind ups, warnings & rests, a scale,
    /// before the per boss step</returns>
    public float EndlessFightTimeScale => endlessFightTimeScale;

    /// <returns>every endless boss's crawl, shot, slash & dash speeds, a
    /// scale, before the per boss step</returns>
    public float EndlessFightSpeedScale => endlessFightSpeedScale;

    // Inspector Fields  #######################################################
    [Header("Ramped Difficulty")]
    [SerializeField]
    [Tooltip(
        "before the Cobra King zone. x: progress 0~1 from the starting zone "
            + "exit to the Cobra King zone start; y: ramped difficulty, 0 at "
            + "the exit, 1 at the zone start"
    )]
    private AnimationCurve preBossRampCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [SerializeField]
    [Tooltip(
        "between the 2 boss zones. x: progress 0~1 from the Cobra King zone "
            + "end to the Spider King zone start; y: ramped difficulty, 1 at "
            + "the Cobra zone end, 2 at the Spider zone start; the "
            + "difficulty is a flat 1 inside the Cobra zone"
    )]
    private AnimationCurve midBossRampCurve =
        AnimationCurve.Linear(0f, 1f, 1f, 2f);

    [SerializeField]
    [Tooltip(
        "after the Spider King zone. x: height above the Spider King zone "
            + "end; world u; y: ramped difficulty, 2 at the zone end and "
            + "rising; the difficulty is a flat 2 inside the Spider zone"
    )]
    private AnimationCurve postBossRampCurve = AnimationCurve.Linear(
        0f,
        2f,
        500f,
        3f
    );

    [Header("Branch")]
    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "no branch is placed within this distance of the monkey while the "
            + "starting zone lasts; 0 turns it off; u"
    )]
    private float startingZoneBranchClearRadiusU = 6f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "chance that one banana branch attempt succeeds on a segment in the "
            + "starting zone, below the exit trigger; replaces the banana "
            + "branch curve there, and no other kind grows there"
    )]
    private float startingZoneBananaBranchChance = 0.4f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "a guaranteed coconut or pineapple branch grows within this many "
            + "degrees, either way, of the monkey's angle around the trunk "
            + "when its segment rolls, so the first of each is on the "
            + "monkey's side; 0 grows it exactly above the monkey; deg"
    )]
    private float guaranteedBranchYawSpreadDeg = 15f;

    [Header("Branch: Empty")]
    [SerializeField]
    [Min(0)]
    [Tooltip("empty branch attempts rolled per segment")]
    private int emptyBranchAttemptCount = 10;

    [SerializeField]
    [Tooltip(
        "x: ramped difficulty; y: chance 0~1 that one attempt grows a "
            + "branch bearing no fruit"
    )]
    private AnimationCurve emptyBranchProbabilityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 0.4f);

    [Header("Branch: Banana")]
    [SerializeField]
    [Min(0)]
    [Tooltip("banana branch attempts rolled per segment")]
    private int bananaBranchAttemptCount = 10;

    [SerializeField]
    [Tooltip(
        "x: ramped difficulty; y: chance 0~1 that one attempt grows a "
            + "branch bearing a banana"
    )]
    private AnimationCurve bananaBranchProbabilityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 0.3f);

    [Header("Branch: Coconut")]
    [SerializeField]
    [Min(0)]
    [Tooltip("coconut branch attempts rolled per segment")]
    private int coconutBranchAttemptCount = 1;

    [SerializeField]
    [Tooltip(
        "climbed height, as the trunk markers show it, where one coconut "
            + "branch always grows, on top of the curve rolls; negative "
            + "turns it off; u"
    )]
    private float coconutGuaranteedHeightU = 250f;

    [SerializeField]
    [Tooltip(
        "x: ramped difficulty; y: chance 0~1 that one attempt grows a "
            + "branch bearing a coconut; 0 below the first appearance"
    )]
    private AnimationCurve coconutBranchProbabilityCurve = new(
        new Keyframe(0f, 0f),
        new Keyframe(0.495f, 0f),
        new Keyframe(0.55f, 0.1f),
        new Keyframe(2f, 0.1f)
    );

    [Header("Branch: Pineapple")]
    [SerializeField]
    [Min(0)]
    [Tooltip("pineapple branch attempts rolled per segment")]
    private int pineappleBranchAttemptCount = 10;

    [SerializeField]
    [Tooltip(
        "climbed height, as the trunk markers show it, where one pineapple "
            + "branch always grows, on top of the curve rolls; negative "
            + "turns it off; u"
    )]
    private float pineappleGuaranteedHeightU = 150f;

    [SerializeField]
    [Tooltip(
        "x: ramped difficulty; y: chance 0~1 that one attempt grows a "
            + "branch bearing a pineapple"
    )]
    private AnimationCurve pineappleBranchProbabilityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 0.05f);

    [Header("Rough Bark")]
    [SerializeField, Range(0.1f, 1f)]
    [Tooltip(
        "movement speed on rough bark, applied equally to climbing and "
            + "circling"
    )]
    private float roughBarkSpeedMultiplier = 0.35f;

    [SerializeField, Min(0.01f)]
    [Tooltip("time to blend into or out of the slowdown; s")]
    private float roughBarkBlendS = 0.18f;

    [SerializeField, Min(0)]
    [Tooltip("rough patch attempts per recycled tree segment")]
    private int roughBarkAttemptCount = 2;

    [SerializeField]
    [Tooltip("x: ramped difficulty; y: rough patch chance 0~1")]
    private AnimationCurve roughBarkSpawnProbabilityCurve =
        AnimationCurve.Linear(0f, 0.1f, 1f, 0.7f);

    [Header("Spider Web")]
    [SerializeField]
    [Min(0)]
    [Tooltip("web creation attempts rolled per segment")]
    private int webAttemptCount = 2;

    [SerializeField]
    [Tooltip(
        "x: ramped difficulty 0~3; y: chance 0~1 that one attempt "
            + "creates a web"
    )]
    private AnimationCurve webSpawnProbabilityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 0.5f);

    [Header("Boss Fight")]
    [SerializeField]
    [Tooltip(
        "climbed height, as the trunk markers show it, where the boss zone "
            + "begins: the ramp reaches 1 here and only the boss fight "
            + "generates anything from here to the zone end; u"
    )]
    private float bossZoneStartU = 500f;

    [SerializeField]
    [Tooltip(
        "height climbed since game start at which the boss fight begins, "
            + "inside the boss zone; lower the zone to test; u"
    )]
    private float bossTriggerHeightU = 525f;

    [SerializeField]
    [Tooltip(
        "climbed height where the boss zone ends: ordinary generation "
            + "resumes and the ramp climbs above 1 from here; u"
    )]
    private float bossZoneEndU = 550f;

    [SerializeField]
    [Tooltip("health of the Cobra King")]
    private int bossMaxHealth = 12;

    [SerializeField]
    [Tooltip("health a thrown banana takes off the Cobra King per hit")]
    private int bossBananaDamage = 1;

    [Header("Spider Fight")]
    [SerializeField]
    [Tooltip(
        "climbed height where the Spider King's zone begins: only its fight "
            + "generates anything from here to its zone end; u"
    )]
    private float spiderZoneStartU = 975f;

    [SerializeField]
    [Tooltip("climbed height at which the Spider King fight begins; u")]
    private float spiderTriggerHeightU = 1000f;

    [SerializeField]
    [Tooltip("climbed height where the Spider King's zone ends; u")]
    private float spiderZoneEndU = 1050f;

    [SerializeField]
    [Tooltip("climbed height from which the Spider King is seen waiting "
        + "above its trigger height; u")]
    private float spiderJumpHeightU = 950f;

    [SerializeField]
    [Tooltip("health of the Spider King, one ripped silk knot takes 1 or 2")]
    private int spiderMaxHealth = 15;

    [Header("Endless Boss Rush")]
    [SerializeField]
    [Tooltip("climbed height b/t two bosses in the endless run: the Cobra "
        + "King first, then the Spider King, taking turns; u")]
    private float endlessBossIntervalU = 300f;

    [SerializeField]
    [Tooltip("scale on the monkey's & the great cobra's climb in the endless "
        + "run, outside a boss fight")]
    private float endlessSpeedScale = 2f;

    [SerializeField]
    [Tooltip("a boss's extra health per earlier boss in the endless run, a "
        + "share of its base health")]
    private float endlessHealthStep = 0.2f;

    [SerializeField]
    [Tooltip("how much shorter a boss's wind ups, warnings & rests get per "
        + "earlier boss in the endless run, a share")]
    private float endlessTimeStep = 0.06f;

    [SerializeField]
    [Tooltip("the shortest a boss's wind ups, warnings & rests get in the "
        + "endless run, a share of normal")]
    private float endlessMinTimeScale = 0.6f;

    [SerializeField]
    [Tooltip("how much faster a boss's shots & dashes get per earlier boss in "
        + "the endless run, a share")]
    private float endlessSpeedStep = 0.05f;

    [SerializeField]
    [Tooltip("the fastest a boss's shots & dashes get in the endless run, a "
        + "scale of normal")]
    private float endlessMaxSpeedScale = 1.5f;

    [SerializeField]
    [Tooltip("every endless boss's health, a scale, before the per boss "
        + "step: the monkey is as fast in its fights as on the climb")]
    private float endlessFightHealthScale = 1.3f;

    [SerializeField]
    [Tooltip("every endless boss's wind ups, warnings & rests, a scale, "
        + "before the per boss step")]
    private float endlessFightTimeScale = 0.8f;

    [SerializeField]
    [Tooltip("every endless boss's crawl, shot, slash & dash speeds, a scale, "
        + "before the per boss step")]
    private float endlessFightSpeedScale = 1.4f;

    // Editor Validation  ######################################################
    private void OnValidate() {
        guaranteedBranchYawSpreadDeg = Mathf.Max(
            0f,
            guaranteedBranchYawSpreadDeg
        );
        emptyBranchAttemptCount = Mathf.Max(0, emptyBranchAttemptCount);
        bananaBranchAttemptCount = Mathf.Max(0, bananaBranchAttemptCount);
        coconutBranchAttemptCount = Mathf.Max(0, coconutBranchAttemptCount);
        pineappleBranchAttemptCount = Mathf.Max(0, pineappleBranchAttemptCount);
        roughBarkSpeedMultiplier = Mathf.Clamp(
            roughBarkSpeedMultiplier,
            0.1f,
            1f
        );
        roughBarkBlendS = Mathf.Max(0.01f, roughBarkBlendS);
        roughBarkAttemptCount = Mathf.Max(0, roughBarkAttemptCount);
        webAttemptCount = Mathf.Max(0, webAttemptCount);
        // the zone is ordered: start <= trigger <= end
        bossZoneStartU = Mathf.Max(0f, bossZoneStartU);
        bossTriggerHeightU = Mathf.Max(bossZoneStartU, bossTriggerHeightU);
        bossZoneEndU = Mathf.Max(bossTriggerHeightU, bossZoneEndU);
        bossMaxHealth = Mathf.Max(1, bossMaxHealth);
        bossBananaDamage = Mathf.Max(1, bossBananaDamage);
        // the spider zone sits above the cobra's, ordered the same way
        spiderZoneStartU = Mathf.Max(bossZoneEndU, spiderZoneStartU);
        spiderTriggerHeightU = Mathf.Max(
            spiderZoneStartU, spiderTriggerHeightU);
        spiderZoneEndU = Mathf.Max(spiderTriggerHeightU, spiderZoneEndU);
        spiderJumpHeightU = Mathf.Clamp(
            spiderJumpHeightU, bossZoneEndU, spiderTriggerHeightU
        );
        spiderMaxHealth = Mathf.Max(1, spiderMaxHealth);
        endlessBossIntervalU = Mathf.Max(50f, endlessBossIntervalU);
        endlessSpeedScale = Mathf.Max(0.1f, endlessSpeedScale);
        endlessHealthStep = Mathf.Max(0f, endlessHealthStep);
        endlessTimeStep = Mathf.Max(0f, endlessTimeStep);
        endlessMinTimeScale = Mathf.Clamp(endlessMinTimeScale, 0.1f, 1f);
        endlessSpeedStep = Mathf.Max(0f, endlessSpeedStep);
        endlessMaxSpeedScale = Mathf.Max(1f, endlessMaxSpeedScale);
        endlessFightHealthScale = Mathf.Max(0.1f, endlessFightHealthScale);
        endlessFightTimeScale = Mathf.Clamp(endlessFightTimeScale, 0.1f, 2f);
        endlessFightSpeedScale = Mathf.Max(0.1f, endlessFightSpeedScale);
    }
}
