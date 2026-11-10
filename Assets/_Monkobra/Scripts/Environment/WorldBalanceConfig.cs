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

    /// <returns>x: height above the boss zone end; world u; y: ramped
    /// difficulty from 1 up</returns>
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

    public float RoughBarkSpeedMultiplier => roughBarkSpeedMultiplier;
    public float RoughBarkBlendS => roughBarkBlendS;
    public int RoughBarkAttemptCount => roughBarkAttemptCount;
    public AnimationCurve RoughBarkSpawnProbabilityCurve =>
        roughBarkSpawnProbabilityCurve;

    /// <returns>web creation attempts rolled per segment</returns>
    public int WebAttemptCount => webAttemptCount;

    /// <returns>x: ramped difficulty 0~1; y: chance 0~1 that one attempt
    /// creates a web</returns>
    public AnimationCurve WebSpawnProbabilityCurve =>
        webSpawnProbabilityCurve;

    /// <returns>climbed height where the boss zone begins: the ramp reaches 1
    /// here and nothing unrelated to the fight is generated from here on; u
    /// </returns>
    public float BossZoneStartU => bossZoneStartU;

    /// <returns>height climbed since game start at which the boss fight
    /// begins; u</returns>
    public float BossTriggerHeightU => bossTriggerHeightU;

    /// <returns>climbed height where the boss zone ends: ordinary generation
    /// resumes and the ramp climbs above 1 from here; u</returns>
    public float BossZoneEndU => bossZoneEndU;

    /// <returns>health of the Cobra King</returns>
    public int BossMaxHealth => bossMaxHealth;

    /// <returns>health a thrown banana takes off the Cobra King per hit
    /// </returns>
    public int BossBananaDamage => bossBananaDamage;

    // Inspector Fields  #######################################################
    [Header("Ramped Difficulty")]
    [SerializeField]
    [Tooltip(
        "before the boss zone. x: progress 0~1 from the starting zone exit "
            + "to the boss zone start; y: ramped difficulty, 0 at the exit, "
            + "1 at the zone start"
    )]
    private AnimationCurve preBossRampCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [SerializeField]
    [Tooltip(
        "after the boss zone. x: height above the boss zone end; world u; "
            + "y: ramped difficulty, 1 at the zone end and rising; the "
            + "difficulty is a flat 1 inside the zone"
    )]
    private AnimationCurve postBossRampCurve = AnimationCurve.Linear(
        0f,
        1f,
        500f,
        2f
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
        "x: ramped difficulty 0~1; y: chance 0~1 that one attempt "
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

    // Editor Validation  ######################################################
    private void OnValidate() {
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
    }
}
