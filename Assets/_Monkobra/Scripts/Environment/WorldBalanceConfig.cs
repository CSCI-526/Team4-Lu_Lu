using System;
using UnityEngine;
using UnityEngine.Serialization;

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

    /// <returns>the ramped difficulty the post boss curve ends on and holds
    /// forever: Normal Play's value past the Spider King, & Hardcore Mode's
    /// all climb long</returns>
    public float PlateauValue {
        get {
            Keyframe[] keys = postBossRampCurve.keys;
            return keys.Length == 0 ? 2f : keys[keys.Length - 1].value;
        }
    }

    /// <returns>no branch grows within this distance of the monkey while the
    /// starting zone lasts, so a restart never spawns the monkey into one;
    /// u</returns>
    public float StartingZoneBranchClearRadiusU =>
        startingZoneBranchClearRadiusU;

    /// <returns>expected banana branches per
    /// <see cref="TrunkScatter.DENSITY_SPAN_U"/> of trunk in the starting
    /// zone, in place of the banana density curve; the other kinds do not
    /// grow there</returns>
    public float StartingZoneBananaBranchDensity =>
        startingZoneBananaBranchDensity;

    /// <returns>how far, in degrees either way, a guaranteed coconut or
    /// pineapple branch may sit from the monkey's angle around the trunk when
    /// its segment rolls; 0 grows it exactly there; deg</returns>
    public float GuaranteedBranchYawSpreadDeg => guaranteedBranchYawSpreadDeg;

    /// <param name="kind">which of the 4 branch kinds</param>
    /// <returns>x: ramped difficulty; y: expected branches of that kind per
    /// <see cref="TrunkScatter.DENSITY_SPAN_U"/> of trunk</returns>
    public AnimationCurve GetBranchDensityCurve(BranchKind kind) {
        return kind switch {
            BranchKind.Empty => emptyBranchDensityCurve,
            BranchKind.Banana => bananaBranchDensityCurve,
            BranchKind.Coconut => coconutBranchDensityCurve,
            BranchKind.Pineapple => pineappleBranchDensityCurve,
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

    /// <returns>x: ramped difficulty; y: expected rough bark patches per
    /// <see cref="TrunkScatter.DENSITY_SPAN_U"/> of trunk</returns>
    public AnimationCurve RoughBarkDensityCurve => roughBarkDensityCurve;

    /// <returns>x: ramped difficulty 0~3; y: expected webs per
    /// <see cref="TrunkScatter.DENSITY_SPAN_U"/> of trunk</returns>
    public AnimationCurve WebDensityCurve => webDensityCurve;

    /// <returns>where the Cobra King's fight stands & how tall its bare
    /// arena is: its zone, marker & guard all derive from this</returns>
    public BossArenaLayout CobraKingLayout => cobraKingArena;

    /// <returns>health of the Cobra King on Normal & at Loop 1</returns>
    public int BossMaxHealth => bossMaxHealth;

    /// <returns>health a thrown banana takes off the Cobra King per hit
    /// </returns>
    public int BossBananaDamage => bossBananaDamage;

    /// <returns>where the Spider King's fight stands & how tall its bare
    /// arena is: its zone, marker, seen waiting height & guard all derive
    /// from this</returns>
    public BossArenaLayout SpiderKingLayout => spiderKingArena;

    /// <returns>health of the Spider King</returns>
    public int SpiderMaxHealth => spiderMaxHealth;

    /// <returns>hearts the monkey starts a boss fight with at Loop 0 & 1
    /// </returns>
    public int MonkeyHeartCount => monkeyHearts;

    /// <returns>how the Cobra King's features grow from Loop 2 on</returns>
    public CobraKingLoopGrowth CobraKingLoopGrowth => cobraKingLoopGrowth;

    /// <returns>how the Spider King's features grow from Loop 2 on</returns>
    public SpiderKingLoopGrowth SpiderKingLoopGrowth => spiderKingLoopGrowth;

    /// <returns>climbed height b/t two bosses in the hardcore run's boss
    /// rush; u</returns>
    public float HardcoreBossIntervalU => hardcoreBossIntervalU;

    /// <returns>height of every hardcore arena's bare zone, both kings, so
    /// the climb between two fights is the interval less this; u</returns>
    public float HardcoreArenaHeightU => hardcoreArenaHeightU;

    /// <returns>scale on the monkey's & the great cobra's climb in the
    /// hardcore run, outside a boss fight</returns>
    public float HardcoreSpeedScale => hardcoreSpeedScale;

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
            + "end; world u; y: ramped difficulty, 2 at the zone end, a "
            + "short ramp, then flat at its last key forever (the plateau); "
            + "the difficulty is a flat 2 inside the Spider zone"
    )]
    private AnimationCurve postBossRampCurve = AnimationCurve.Linear(
        0f,
        2f,
        300f,
        2.6f
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
    [Min(0f)]
    [Tooltip(
        "expected banana branches per 10 u of trunk in the starting zone, "
            + "below the exit trigger; replaces the banana density curve "
            + "there, and no other kind grows there"
    )]
    private float startingZoneBananaBranchDensity = 2f;

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
    [Tooltip(
        "x: ramped difficulty; y: expected branches per 10 u of "
            + "trunk, each bearing no fruit"
    )]
    private AnimationCurve emptyBranchDensityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 4f);

    [Header("Branch: Banana")]
    [SerializeField]
    [Tooltip(
        "x: ramped difficulty; y: expected branches per 10 u of "
            + "trunk, each bearing a banana"
    )]
    private AnimationCurve bananaBranchDensityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 3f);

    [Header("Branch: Coconut")]
    [SerializeField]
    [Tooltip(
        "climbed height, as the trunk markers show it, where one coconut "
            + "branch always grows, on top of the curve rolls; negative "
            + "turns it off; u"
    )]
    private float coconutGuaranteedHeightU = 250f;

    [SerializeField]
    [Tooltip(
        "x: ramped difficulty; y: expected branches per 10 u of "
            + "trunk, each bearing a coconut; 0 below the first appearance"
    )]
    private AnimationCurve coconutBranchDensityCurve = new(
        new Keyframe(0f, 0f),
        new Keyframe(0.495f, 0f),
        new Keyframe(0.55f, 0.1f),
        new Keyframe(2f, 0.1f)
    );

    [Header("Branch: Pineapple")]
    [SerializeField]
    [Tooltip(
        "climbed height, as the trunk markers show it, where one pineapple "
            + "branch always grows, on top of the curve rolls; negative "
            + "turns it off; u"
    )]
    private float pineappleGuaranteedHeightU = 150f;

    [SerializeField]
    [Tooltip(
        "x: ramped difficulty; y: expected branches per 10 u of "
            + "trunk, each bearing a pineapple"
    )]
    private AnimationCurve pineappleBranchDensityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 0.5f);

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

    [SerializeField]
    [Tooltip(
        "x: ramped difficulty; y: expected rough patches per 10 u of "
            + "trunk"
    )]
    private AnimationCurve roughBarkDensityCurve =
        AnimationCurve.Linear(0f, 0.2f, 1f, 1.4f);

    [Header("Spider Web")]
    [SerializeField]
    [Tooltip(
        "x: ramped difficulty 0~3; y: expected webs per 10 u of trunk"
    )]
    private AnimationCurve webDensityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Boss Fight")]
    [SerializeField]
    [Tooltip(
        "the Cobra King's arena: location is the climbed height the fight "
            + "starts at, where its height marker stands; height is the bare "
            + "zone around it, location +- height / 2; u"
    )]
    private BossArenaLayout cobraKingArena = new BossArenaLayout(525f, 50f);

    [SerializeField]
    [Tooltip("health of the Cobra King")]
    private int bossMaxHealth = 8;

    [SerializeField]
    [Tooltip("health a thrown banana takes off the Cobra King per hit")]
    private int bossBananaDamage = 1;

    [Header("Spider Fight")]
    [SerializeField]
    [Tooltip(
        "the Spider King's arena: location is the climbed height the fight "
            + "starts at, height is the bare zone around it, and it is seen "
            + "waiting from one height below the location; u"
    )]
    private BossArenaLayout spiderKingArena = new BossArenaLayout(1025f, 50f);

    [SerializeField]
    [Tooltip("health of the Spider King, one ripped silk knot takes 1 or 2")]
    private int spiderMaxHealth = 15;

    [Header("Monkey Hearts")]
    [SerializeField]
    [Min(1)]
    [Tooltip("hearts the monkey starts a boss fight with at Loop 0 & 1; a "
        + "fatal hit the gold armor does not take costs one")]
    private int monkeyHearts = 3;

    [Header("Loop Growth")]
    [SerializeField]
    [Tooltip("the Cobra King at Loop 2 and up: v(n) = clamp(round(v1 + step "
        + "* (n - 1)), limit), v1 its Loop 1 value")]
    private CobraKingLoopGrowth cobraKingLoopGrowth =
        CobraKingLoopGrowth.Default;

    [SerializeField]
    [Tooltip("the Spider King at Loop 2 and up: v(n) = clamp(round(v1 + step "
        + "* (n - 1)), limit), v1 its Loop 1 value")]
    private SpiderKingLoopGrowth spiderKingLoopGrowth =
        SpiderKingLoopGrowth.Default;

    [Header("Hardcore Boss Rush")]
    [SerializeField]
    [Tooltip("climbed height b/t two bosses in the hardcore run: the Cobra "
        + "King first, then the Spider King, taking turns; u")]
    [FormerlySerializedAs("endlessBossIntervalU")]
    private float hardcoreBossIntervalU = 100f;

    [SerializeField]
    [Min(1f)]
    [Tooltip("height of every hardcore arena's bare zone, both kings: 100 u "
        + "b/t two fights less this is real climbing; u")]
    private float hardcoreArenaHeightU = 40f;

    [SerializeField]
    [Tooltip("scale on the monkey's & the great cobra's climb in the hardcore "
        + "run, outside a boss fight")]
    [FormerlySerializedAs("endlessSpeedScale")]
    private float hardcoreSpeedScale = 1f;

    // Editor Validation  ######################################################
    private void OnValidate() {
        guaranteedBranchYawSpreadDeg = Mathf.Max(
            0f,
            guaranteedBranchYawSpreadDeg
        );
        roughBarkSpeedMultiplier = Mathf.Clamp(
            roughBarkSpeedMultiplier,
            0.1f,
            1f
        );
        roughBarkBlendS = Mathf.Max(0.01f, roughBarkBlendS);
        cobraKingLoopGrowth.Validate();
        spiderKingLoopGrowth.Validate();
        // the post boss ramp never loops or runs on: it clamps flat at its end
        postBossRampCurve.preWrapMode = WrapMode.ClampForever;
        postBossRampCurve.postWrapMode = WrapMode.ClampForever;
        // a layout keeps its bare zone above the start of the climb
        cobraKingArena.heightU = Mathf.Max(1f, cobraKingArena.heightU);
        cobraKingArena.locationU = Mathf.Max(
            cobraKingArena.heightU * 0.5f, cobraKingArena.locationU);
        bossMaxHealth = Mathf.Max(1, bossMaxHealth);
        bossBananaDamage = Mathf.Max(1, bossBananaDamage);
        // the spider zone sits above the cobra's
        spiderKingArena.heightU = Mathf.Max(1f, spiderKingArena.heightU);
        spiderKingArena.locationU = Mathf.Max(
            cobraKingArena.BareTopU + spiderKingArena.heightU * 0.5f,
            spiderKingArena.locationU);
        spiderMaxHealth = Mathf.Max(1, spiderMaxHealth);
        hardcoreBossIntervalU = Mathf.Max(50f, hardcoreBossIntervalU);
        hardcoreArenaHeightU = Mathf.Max(1f, hardcoreArenaHeightU);
        hardcoreSpeedScale = Mathf.Max(0.1f, hardcoreSpeedScale);
    }
}

/// <summary>How a boss feature's generated value is rounded.</summary>
public enum LoopRounding {
    None,
    Floor,
    Ceil,
}

/// <summary>
/// One feature's growth from Loop 2 on, anchored on its Loop 1 value v1:
/// v(n) = clamp(round(v1 + step * (n - 1)), limit). A rising feature caps at
/// its limit, a falling one floors at it.
/// </summary>
[Serializable]
public struct LoopGrowth {
    // Public Members  #########################################################
    [Tooltip("added per Loop past Loop 1; negative to fall")]
    public float step;

    [Tooltip("how the generated value is rounded to whole numbers")]
    public LoopRounding rounding;

    [Tooltip("whether the value stops at the limit")]
    public bool hasLimit;

    [Tooltip("a cap for a rising value, a floor for a falling one")]
    public float limit;

    public LoopGrowth(float step, LoopRounding rounding, bool hasLimit,
        float limit) {
        this.step = step;
        this.rounding = rounding;
        this.hasLimit = hasLimit;
        this.limit = limit;
    }

    /// <param name="v1">the feature's Loop 1 value</param>
    /// <param name="loop">the Loop number, 2 and up</param>
    /// <returns>the feature's value at that Loop</returns>
    public readonly float Evaluate(float v1, int loop) {
        float value = v1 + step * (Mathf.Max(loop, 1) - 1);
        value = rounding == LoopRounding.Floor ? Mathf.Floor(value)
            : rounding == LoopRounding.Ceil ? Mathf.Ceil(value) : value;
        if (!hasLimit) {
            return value;
        }
        return step >= 0f ? Mathf.Min(value, limit)
            : Mathf.Max(value, limit);
    }
}

/// <summary>The Cobra King's growth per feature past Loop 1.</summary>
[Serializable]
public struct CobraKingLoopGrowth {
    public LoopGrowth health;
    [Tooltip("scale on wind ups, warnings & rests, v1 = 1")]
    public LoopGrowth time;
    public LoopGrowth minionWaves;
    [Tooltip("monkey hearts per fight, v1 = monkeyHearts")]
    public LoopGrowth hearts;
    [Tooltip("scale on the defeat points, v1 = 1; the total is floored")]
    public LoopGrowth points;

    public static CobraKingLoopGrowth Default => new CobraKingLoopGrowth {
        health = new LoopGrowth(2.5f, LoopRounding.Ceil, false, 0f),
        time = new LoopGrowth(-0.12f, LoopRounding.None, true, 0.5f),
        minionWaves = new LoopGrowth(0.75f, LoopRounding.Ceil, true, 8f),
        hearts = new LoopGrowth(-0.5f, LoopRounding.Ceil, true, 1f),
        points = new LoopGrowth(0.5f, LoopRounding.None, false, 0f),
    };

    public void Validate() { }
}

/// <summary>The Spider King's growth per feature past Loop 1.</summary>
[Serializable]
public struct SpiderKingLoopGrowth {
    public LoopGrowth health;
    [Tooltip("scale on wind ups, gaps & rests, v1 = 1")]
    public LoopGrowth time;
    [Tooltip("scale on shot, slash & dash speeds, v1 = 1")]
    public LoopGrowth speed;
    public LoopGrowth slashesStage2;
    public LoopGrowth slashesStage3;
    [Tooltip("monkey hearts per fight, v1 = monkeyHearts")]
    public LoopGrowth hearts;
    [Tooltip("scale on the defeat points, v1 = 1; the total is floored")]
    public LoopGrowth points;

    public static SpiderKingLoopGrowth Default => new SpiderKingLoopGrowth {
        health = new LoopGrowth(5f, LoopRounding.Ceil, false, 0f),
        time = new LoopGrowth(-0.12f, LoopRounding.None, true, 0.5f),
        speed = new LoopGrowth(0.1f, LoopRounding.None, true, 1.5f),
        slashesStage2 = new LoopGrowth(0.5f, LoopRounding.Ceil, true, 5f),
        slashesStage3 = new LoopGrowth(1f, LoopRounding.Floor, true, 5f),
        hearts = new LoopGrowth(-0.5f, LoopRounding.Ceil, true, 1f),
        points = new LoopGrowth(0.5f, LoopRounding.None, false, 0f),
    };

    public void Validate() { }
}
