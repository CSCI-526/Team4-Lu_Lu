using UnityEngine;

/// <summary>
/// Balance of the generated world: the ramped difficulty curve that every
/// hazard chance is read against, branch and fruit generation, rough bark,
/// spider webs and the boss fight. Player, scoring and coconut numbers live in
/// <see cref="PlayerBalanceConfig"/>, cobra numbers in
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

    /// <returns>branch creation attempts rolled per segment</returns>
    public int BranchGenerationAttemptCount => branchGenerationAttemptCount;

    /// <returns>no branch grows within this distance of the monkey while the
    /// starting zone lasts, so a restart never spawns the monkey into one;
    /// u</returns>
    public float StartingZoneBranchClearRadiusU =>
        startingZoneBranchClearRadiusU;

    /// <returns>chance 0~1 that one attempt creates a branch on a segment
    /// whose middle lies in the starting zone, in place of the branch
    /// curve</returns>
    public float StartingZoneBranchChance => startingZoneBranchChance;

    /// <returns>chance 0~1 that a branch grown in the starting zone bears a
    /// banana, in place of the banana curve</returns>
    public float StartingZoneBananaChance => startingZoneBananaChance;

    /// <returns>x: ramped difficulty 0~1; y: chance 0~1 that one attempt
    /// creates a branch</returns>
    public AnimationCurve BranchSpawnProbabilityCurve =>
        branchSpawnProbabilityCurve;

    /// <returns>x: ramped difficulty 0~1; y: chance 0~1 that a generated
    /// branch bears a banana</returns>
    public AnimationCurve BananaSpawnProbabilityCurve =>
        bananaSpawnProbabilityCurve;

    /// <returns>x: ramped difficulty 0~1; y: chance 0~1 that a generated
    /// branch bears a pineapple, rolled before the banana</returns>
    public AnimationCurve PineappleSpawnProbabilityCurve =>
        pineappleSpawnProbabilityCurve;

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
    [Min(0)]
    [Tooltip("branch creation attempts rolled per segment")]
    private int branchGenerationAttemptCount = 9;

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
        "chance that one branch attempt succeeds on a segment in the starting "
            + "zone, below the exit trigger; replaces the branch curve there"
    )]
    private float startingZoneBranchChance = 0.4f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "chance that a branch grown in the starting zone bears a banana; "
            + "replaces the banana curve there"
    )]
    private float startingZoneBananaChance = 1f;

    [SerializeField]
    [Tooltip(
        "x: ramped difficulty 0~1; y: chance 0~1 that one attempt "
            + "creates a branch"
    )]
    private AnimationCurve branchSpawnProbabilityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [SerializeField]
    [Tooltip(
        "x: ramped difficulty 0~1; y: chance 0~1 that a generated branch "
            + "bears a banana"
    )]
    private AnimationCurve bananaSpawnProbabilityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [SerializeField]
    [Tooltip(
        "x: ramped difficulty 0~1; y: chance 0~1 that a generated branch "
            + "bears a pineapple, rolled before the banana"
    )]
    private AnimationCurve pineappleSpawnProbabilityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 0.1f);

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
        branchGenerationAttemptCount = Mathf.Max(
            0,
            branchGenerationAttemptCount
        );
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
