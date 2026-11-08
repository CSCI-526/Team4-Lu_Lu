using UnityEngine;

/// <summary>
/// Balance of the generated world: the ramped difficulty curve that every
/// hazard chance is read against, branch and fruit generation, rough bark and
/// spider webs. Player, scoring and coconut numbers live in
/// <see cref="PlayerBalanceConfig"/>, cobra numbers in
/// <see cref="CobraBalanceConfig"/>.
/// </summary>
[CreateAssetMenu(
    fileName = "WorldBalanceConfig",
    menuName = "Scriptable Objects/WorldBalanceConfig"
)]
public class WorldBalanceConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>x: player y above start y; world u; y: ramped difficulty
    /// 0~1</returns>
    public AnimationCurve RampedDifficultyCurve => rampedDifficultyCurve;

    /// <returns>branch creation attempts rolled per segment</returns>
    public int BranchGenerationAttemptCount => branchGenerationAttemptCount;

    /// <returns>no branch grows within this distance of the monkey while the
    /// starting zone lasts, so a restart never spawns the monkey into one;
    /// u</returns>
    public float StartingZoneBranchClearRadiusU =>
        startingZoneBranchClearRadiusU;

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

    public int RoughBarkStartScore => roughBarkStartScore;
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

    // Inspector Fields  #######################################################
    [Header("Ramped Difficulty")]
    [SerializeField]
    [Tooltip("x: player y above start y; world u; y: ramped difficulty 0~1")]
    private AnimationCurve rampedDifficultyCurve =
        AnimationCurve.Linear(20f, 0f, 100f, 1f);

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
    [SerializeField, Min(0)]
    [Tooltip("minimum total HUD score before rough patches begin appearing")]
    private int roughBarkStartScore = 3000;

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
    [Tooltip("x: height difficulty 0~1; y: rough patch chance 0~1")]
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

    // Editor Validation  ######################################################
    private void OnValidate() {
        branchGenerationAttemptCount = Mathf.Max(
            0,
            branchGenerationAttemptCount
        );
        roughBarkStartScore = Mathf.Max(0, roughBarkStartScore);
        roughBarkSpeedMultiplier = Mathf.Clamp(
            roughBarkSpeedMultiplier,
            0.1f,
            1f
        );
        roughBarkBlendS = Mathf.Max(0.01f, roughBarkBlendS);
        roughBarkAttemptCount = Mathf.Max(0, roughBarkAttemptCount);
        webAttemptCount = Mathf.Max(0, webAttemptCount);
    }
}
