using UnityEngine;

/// <summary>
/// Balance of the player's run: stamina budget and drain, starting zone,
/// branch-hit penalty, score rules and the coconut item. The difficulty ramp,
/// branches, rough bark and webs are in <see cref="WorldBalanceConfig"/>, both
/// cobras in <see cref="CobraBalanceConfig"/>.
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerBalanceConfig",
    menuName = "Scriptable Objects/PlayerBalanceConfig"
)]
public class PlayerBalanceConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>stamina at level start, also the ceiling for restores</returns>
    public float MaxStamina => maxStamina;

    /// <returns>stamina lost per unit climbed upward</returns>
    public float StaminaUpwardDrainPerU => staminaUpwardDrainPerU;

    /// <returns>stamina lost per unit orbited round the trunk</returns>
    public float StaminaSidewayDrainPerU => staminaSidewayDrainPerU;

    /// <returns>stamina restored per grabbed fruit</returns>
    public float FruitStaminaRestore => fruitStaminaRestore;

    /// <returns>how long control is lost after a branch hit; s</returns>
    public float HitStunDurationS => hitStunDurationS;

    /// <returns>how far the monkey falls on a branch hit; u</returns>
    public float HitDropDistanceU => hitDropDistanceU;

    /// <returns>how fast the monkey falls on a branch hit; u/s</returns>
    public float HitDropSpeedU => hitDropSpeedU;

    /// <returns>points earned per meter of height</returns>
    public int PointsPerMeter => pointsPerMeter;

    /// <returns>Unity units per meter of height</returns>
    public float UnitsPerMeter => unitsPerMeter;

    /// <returns>points awarded per score reward pickup</returns>
    public int ScoreRewardPoints => scoreRewardPoints;

    /// <returns>fraction 0~1 of <see cref="MaxStamina"/> the monkey starts
    /// the level with, spent in the starting zone</returns>
    public float StartingZoneStartStaminaFraction =>
        startingZoneStartStaminaFraction;

    /// <returns>like <see cref="MonkeyClimbConfig.ExhaustedSpeedMultiplier"/>,
    /// but used while the starting zone lasts</returns>
    public float StartingZoneExhaustedSpeedMultiplier =>
        startingZoneExhaustedSpeedMultiplier;

    /// <returns>coconuts start hanging on branches once the monkey's climbed
    /// height (as on the trunk height markers) reaches this; u</returns>
    public float CoconutStartHeightU => coconutStartHeightU;

    /// <returns>chance 0~1 that a segment above the start height grows one
    /// coconut on a bare branch</returns>
    public float CoconutSpawnChancePerSegment => coconutSpawnChancePerSegment;

    /// <returns>coconut points as a multiple of
    /// <see cref="ScoreRewardPoints"/></returns>
    public float CoconutScoreMultiplier => coconutScoreMultiplier;

    /// <returns>points awarded per coconut: <see cref="ScoreRewardPoints"/>
    /// times <see cref="CoconutScoreMultiplier"/>, rounded</returns>
    public int CoconutRewardPoints => Mathf.RoundToInt(
        scoreRewardPoints * coconutScoreMultiplier
    );

    /// <returns>how long a used coconut freezes the great cobra; s</returns>
    public float CobraFreezeDurationS => cobraFreezeDurationS;

    /// <returns>how fast a thrown coconut falls; u per s</returns>
    public float CoconutThrowSpeedU => coconutThrowSpeedU;

    /// <returns>how far a thrown coconut falls b4 it freezes the cobra
    /// w/o a hit; u</returns>
    public float CoconutThrowDistanceU => coconutThrowDistanceU;

    // Inspector Fields  #######################################################
    [Header("Stamina")]
    [SerializeField]
    [Tooltip("stamina at level start, also the ceiling for restores")]
    private float maxStamina = 100f;

    [SerializeField]
    [Tooltip(
        "stamina lost per unit climbed upward, the costliest move; "
            + "descending is free"
    )]
    private float staminaUpwardDrainPerU = 0.5f;

    [SerializeField]
    [Tooltip(
        "stamina lost per unit travelled sideways round the trunk, "
            + "keep below the upward rate"
    )]
    private float staminaSidewayDrainPerU = 0.3f;

    [SerializeField]
    [Tooltip("stamina restored per grabbed fruit, capped at max stamina")]
    private float fruitStaminaRestore = 30f;

    [Header("Starting Zone")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "fraction of max stamina at level start, so the starting zone "
            + "teaches the stamina limit; 0.3 = 30%"
    )]
    private float startingZoneStartStaminaFraction = 0.3f;

    [SerializeField]
    [Tooltip(
        "upward climb speed multiplier while stamina is empty, used "
            + "during the starting zone only"
    )]
    private float startingZoneExhaustedSpeedMultiplier = 0.5f;

    [Header("Branch Hit")]
    [SerializeField]
    [Tooltip("control lost after a branch hit; s")]
    private float hitStunDurationS = 0.75f;

    [SerializeField]
    [Tooltip("distance fallen on a branch hit; u")]
    private float hitDropDistanceU = 10f;

    [SerializeField]
    [Tooltip("fall speed on a branch hit; u/s")]
    private float hitDropSpeedU = 8f;

    [Header("Score")]
    [SerializeField]
    [Min(0)]
    [Tooltip("points earned per meter of height")]
    private int pointsPerMeter = 1;

    [SerializeField]
    [Min(0.001f)]
    [Tooltip("Unity units per meter of height")]
    private float unitsPerMeter = 0.1f;

    [SerializeField]
    [Min(0)]
    [Tooltip("points awarded per score reward pickup")]
    private int scoreRewardPoints = 100;

    [Header("Coconut")]
    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "coconuts appear once the climbed height on the trunk markers reaches "
            + "this; u"
    )]
    private float coconutStartHeightU = 150f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "chance a segment above the start height grows one coconut on a "
            + "bare branch; 0.1 = about 1 per 100 u"
    )]
    private float coconutSpawnChancePerSegment = 0.1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("coconut points = score reward points x this")]
    private float coconutScoreMultiplier = 2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("how long a used coconut freezes the great cobra; s")]
    private float cobraFreezeDurationS = 10f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("how fast a thrown coconut falls straight down; u per s")]
    private float coconutThrowSpeedU = 30f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "a thrown coconut freezes the cobra on a hit, or once it has fallen "
            + "this far; u"
    )]
    private float coconutThrowDistanceU = 25f;

    // Editor Validation  ######################################################
    private void OnValidate() {
        maxStamina = Mathf.Max(1f, maxStamina);
        staminaUpwardDrainPerU = Mathf.Max(0f, staminaUpwardDrainPerU);
        staminaSidewayDrainPerU = Mathf.Max(0f, staminaSidewayDrainPerU);
        fruitStaminaRestore = Mathf.Max(0f, fruitStaminaRestore);
        hitStunDurationS = Mathf.Max(0f, hitStunDurationS);
        hitDropDistanceU = Mathf.Max(0f, hitDropDistanceU);
        hitDropSpeedU = Mathf.Max(0f, hitDropSpeedU);
        pointsPerMeter = Mathf.Max(0, pointsPerMeter);
        unitsPerMeter = Mathf.Max(0.001f, unitsPerMeter);
        scoreRewardPoints = Mathf.Max(0, scoreRewardPoints);
        coconutStartHeightU = Mathf.Max(0f, coconutStartHeightU);
        coconutSpawnChancePerSegment = Mathf.Clamp01(
            coconutSpawnChancePerSegment
        );
        coconutScoreMultiplier = Mathf.Max(0f, coconutScoreMultiplier);
        cobraFreezeDurationS = Mathf.Max(0f, cobraFreezeDurationS);
        coconutThrowSpeedU = Mathf.Max(0.1f, coconutThrowSpeedU);
        coconutThrowDistanceU = Mathf.Max(0f, coconutThrowDistanceU);
    }
}
