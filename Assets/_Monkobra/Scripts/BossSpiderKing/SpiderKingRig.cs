using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public enum SpiderKingPhase {
    Waiting, // climbing to the trigger height
    Fighting,
    Finishing, // the slow last blow, either way
    ReviveOffer,
    WatchingAd,
    Defeated, // the beaten spider falls
    Rewarding, // the reward panel, time stopped
    Done, // won, the climb goes on
    Over, // lost w/o a revive
}

public enum SpiderKingMode {
    Idle, // stalks the monkey & attacks
    Cocooning, // crawls to a trapped monkey
}

public enum SpiderKingAttack {
    None,
    Web,
    Dash,
}

public enum SpiderKingShotKind {
    Web, // a fast white ball along a silk line w/ a gold end, parried
}

public enum SpiderKingTeachStep {
    Choose, // after a revive: EASY or NORMAL, nothing attacks
    Parry, // bat a slowed web bullet back
    Slash, // bat a slowed slash back
    Final, // "Go!", then the fight
    Ready, // the calm after a revive
    Done,
}

public class SpiderKingCrescent {
    public float originAngle;
    // where the spider slashed from
    public float originY;
    public float directionDeg;
    // its way on the bark, 0 round the trunk
    public float distanceU;
    // how far it has flown
    public Transform[] parts;
    public bool isReflected;
    // batted back at the spider
    public float clashS;
    // real s since it was batted back
    public bool hasClashed;
    public float nextFxS;
    // clash time of its next spark or trail bead
    public float parryDistanceU;
    public float contactDistanceU;
}

public class SpiderKingLaidWeb {
    public SpiderWebTrap web;
    public float endTime;
    // Time.time it goes
    public Vector3 restScale;
}

public class SpiderKingMelon {
    public bool isGolden;
    public GameObject root;
    // the grabbable fruit
    public GameObject branch;
    public Collider trigger;
    public float backTime;
    // Time.time it grows back, -1 if hanging
}

public class SpiderKingThrow {
    public Transform transform;
    public Vector3 from;
    public float ageS;
}

public class SpiderKingShot {
    public SpiderKingShotKind kind;
    public Transform ball;
    public Transform wave;
    // a web shot's silk bullet
    public Vector3 from;
    public Vector3 target;
    public float t;
    // 0 at the spider, 1 at the target
    public float length;
    // path length; u
    public float fill;
    // share of the wind up done
    public bool isCharging;
    // still winding up, not flying
    public bool isReflected;
    // batted back at the spider
    public float clashS;
    // real s since it was batted back
    public Vector3 clashFrom;
    public bool hasClashed;
    public float nextFxS;
    public Vector3 lastPosition;
}

public struct SpiderKingSpark {
    public Transform transform;
    public Vector3 velocity;
    public float ageS;
    public float lifeS;
    public bool isFloating;
    // no gravity, eg drawn in to the charge
    public bool isStreak;
    // long & thin along its flight
    public bool isStroke;
    // a clash stroke, thinning
    public float size;
    // its start size, 0 for the usual
}

public class SpiderKingRing {
    public Vector3 center;
    public Color color;
    public float ageS;
    // < 0 while it waits to show
    public bool isInward;
    // closing in, eg on the charging head
    public Transform[] parts;
}

/// <summary>
/// The state every Cobra King role shares, plus the geometry queries
/// more than one role asks of it.
/// </summary>
public sealed class SpiderKingRig {
    // Constants  #############################################################
    public const string FRUIT_TAG = "FruitCollider";
    public const int PATH_PARTS = 14;
    public const int RING_PARTS = 20;
    // how far the monkey's body edge is from its middle: a parry is timed
    // to the edge an attack comes at; u
    public const float MONKEY_EDGE_U = 0.7f;
    // the sky web shows once the monkey is this close below it; u
    public const float SKY_SHOW_U = 6f;
    // the air wall's width, out past the view, so it seems endless; u
    public const float SKY_WIDE_U = 1200f;
    // how far a touch the armor saves throws the monkey off; u
    public const float TOUCH_THROW_U = 7f;
    // how far a dash the armor saves throws the monkey off its lane; u
    public const float DASH_THROW_U = 8f;
    public const float MIN_VOLLEY_GAP_S = 0.6f;
    // its circling speed b/t attacks, a share of its stalk speed
    public const float PROWL_SHARE = 0.4f;
    // volleys a stage of volleys opens w/, b4 any slash
    public const int VOLLEYS_FIRST = 2;
    // how far in from the screen's edges the spider must be to count as in
    // view for a ranged attack, a share of the screen
    public const float VIEW_EDGE = 0.18f;
    // seconds into the fight b4 the first golden melon may show
    public const float GOLDEN_FIRST_S = 8f;
    // seconds after the armor goes b4 the golden melon may show
    public const float GOLDEN_AFTER_ARMOR_S = 5f;
    public const float BITE_S = 0.4f;
    // the clash: time stops dead this long at the touch; the camera shakes
    public const float CLASH_STOP_S = 0.07f;
    public const float SHAKE_S = 0.22f;
    public const float RING_S = 0.35f;
    public const int CRESCENT_PARTS = 12;
    // the clash of a parry, real s: slowing into the tail, held, speeding off
    public const float CLASH_IN_S = 0.12f;
    public const float CLASH_HOLD_S = 0.14f;
    public const float CLASH_OUT_S = 0.3f;
    // the parry shot, real s: easing in, easing out after the slow motion,
    // & the slam at the clash
    public const float SHOT_IN_S = 0.14f;
    public const float SHOT_OUT_S = 0.35f;
    public const float SHOT_SLAM_S = 0.25f;
    // a parry w/ more coming: a short slow beat & the camera only this far
    // into its shot, 0~1
    public const float QUICK_SLOW_S = 0.25f;
    public const float QUICK_SHOT_WEIGHT = 0.3f;
    public const int CRESCENT_STRANDS = 3;
    public const int SIGIL_PARTS = 9;
    // the silk gloves' swirl on, real s; its share spent flying in; how far
    // out it starts; u
    public const float GLOVE_REWARD_S = 1.6f;
    public const float GLOVE_LAND_SHARE = 0.7f;
    public const float GLOVE_SWIRL_U = 2.5f;
    public const int CRESCENT_SPOKES = 5;
    // how far a crescent's middle runs ahead of its tips; u
    public const float CRESCENT_BULGE_U = 0.9f;
    // the banana hangs about this far below its branch root; u
    public const float BANANA_HANG_U = 1f;
    // the spider starts right above the monkey, this far up, in view
    public const float START_TURN_DEG = 0f;
    public const float START_ABOVE_U = 7f;
    public static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
    public static readonly int BASE_MAP = Shader.PropertyToID("_BaseMap");
    public static readonly int OUTLINE_COLOR =
        Shader.PropertyToID("_Outline_Color");
    public static readonly int OUTLINE_WIDTH =
        Shader.PropertyToID("_Outline_Width");
    public static readonly int COLOR = Shader.PropertyToID("_Color");
    // the silk gloves' silver
    public static readonly Color gloveColor = new Color(0.82f, 0.86f, 0.95f);

    // Shared State  ##########################################################
    public readonly MonoBehaviour owner;
    public readonly Transform host;
    public readonly SpiderKingTuning tuning;
    public SpiderKingArena Arena;
    public SpiderKingMelons Melons;
    public SpiderKingMovement Movement;
    public SpiderKingAttacks Attacks;
    public SpiderKingShots Shots;
    public SpiderKingParry Parry;
    public SpiderKingTeaching Teaching;
    public SpiderKingDamage Damage;
    public SpiderKingEnding Ending;
    public SpiderKingFeedback Feedback;
    public SpiderKingBody body;
    public BossFightHud hud;
    public MaterialPropertyBlock colorBlock;
    public Transform monkey;
    public MonkeyController monkeyController;
    public CobraMotor greatCobra;
    public CameraClimbFraming cameraFraming;
    public SpiderWebStruggle struggle;
    public Vector3 trunkCenter;
    public float entryY;
    public Vector3 entryPosition;
    public Vector3 returnTarget;
    // where a fly back ends
    public Quaternion entryRotation;
    public SpiderKingPhase phase = SpiderKingPhase.Waiting;
    // the endless run's boss rush: this fight's encounter (1, 3, 5, ...), -1
    // outside the rush
    public int rushEncounter = -1;
    public SpiderKingMode mode = SpiderKingMode.Idle;
    public float modeTimeS;
    public SpiderKingAttack attack = SpiderKingAttack.None;
    public float attackTimeS;
    public float attackWindUpS = 1f;
    public float nextAttackTime;
    public Vector3 aimPoint;
    public int warningId = -1;
    public SpiderKingShot chargingShot;
    public int health;
    public int maxHealth;
    public float previewAngle;
    public float previewY;
    public float startAngle;
    public float startY;
    public float spiderAngle;
    public float spiderY;
    public float stunEndTime;
    public float saveGraceEndTime;
    public readonly List<SpiderKingMelon> melons = new List<SpiderKingMelon>();
    public SpiderKingMelon goldenMelon;
    public SpiderKingMelon goldenTwin;
    // the second golden melon, shown w/ it
    public SpiderKingMelon choiceEasy;
    public SpiderKingMelon choiceNormal;
    public bool isEasy;
    public bool hasTaughtWeb;
    // the first bullet has stopped time to teach
    public bool hasTaughtSlash;
    // & the first slash
    public float nextGoldenTime;
    public float goldenHideTime;
    public Vector3 goldenRestScale;
    public readonly List<SpiderKingThrow> throws = new List<SpiderKingThrow>();
    public readonly List<SpiderKingShot> shots = new List<SpiderKingShot>();
    public readonly List<SpiderKingSpark> sparks = new List<SpiderKingSpark>();
    public readonly List<SpiderKingSpark> pops = new List<SpiderKingSpark>();
    public readonly List<SpiderKingRing> rings = new List<SpiderKingRing>();
    public float tailFlashEndTime;
    public Transform skyDisc;
    // the arena's top, null when none
    public Transform skyPatch;
    // its clearer patch round the monkey
    public Material skyMaterial;
    public float nextSkyRippleTime;
    public float clashStopEndTime;
    // real time the clash's dead stop ends
    public float shakeEndTime;
    public float biteEndTime;
    // real time the touch bite ends
    public bool isTailTinted;
    public readonly List<SpiderKingLaidWeb> stickyWebs =
        new List<SpiderKingLaidWeb>();
    public float layDistanceU;
    public bool isDashing;
    public bool hasDashHit;
    public float dashTimeS;
    public float nextDashTime;
    public float dashAngle;
    public float dashY;
    public Transform[] dashLane;
    // its red lane, null when none
    public float dashStartAngle;
    public float dashStartY;
    public float dashWayU;
    // unit way on the bark, round the trunk
    public float dashWayV;
    // & up
    public float dashLengthU;
    public float dashTravelU;
    public int volleyLeft;
    public int volleyGapIndex;
    public bool isVolleySlowFirst;
    public float nextVolleyTime;
    public int lastStage;
    // the silk gloves' swirl on: -1 when none plays
    public float gloveRewardTimeS = -1f;
    public bool hasGloveLanded;
    public Transform[] gloves;
    public Transform[] sleeves;
    // over the arms, once the gloves are on
    public ArmController[] gloveHands;
    public float[] gloveSizes;
    public SpiderKingShot volleyCharge;
    // a volley's next bullet, charging
    public float callOutEndTime;
    // real time it is done calling out
    public float volleyChargeFromTime;
    public int goldenSinceArmor;
    // golden melons missed since the armor went
    public int websBeforeSlash;
    // volleys still due b4 a slash may come
    public bool isSlashing;
    public bool isSlashAiming;
    public int slashLeft;
    public Transform[] slashSigil;
    // the slash charge's sign, null if none
    public int slashGapIndex;
    public bool isSlashSlowFirst;
    public float nextSlashTime;
    public float slashTimeS;
    public float slashAimDeg;
    // its aim on the bark, 0 round the trunk
    public Transform[] slashAim;
    // the red aim line, null when none
    public readonly List<SpiderKingCrescent> crescents =
        new List<SpiderKingCrescent>();
    public float slashChargeLeftS;
    public float slashSlow = 1f;
    // the lesson's slower slash
    public SpiderKingCrescent frozenCrescent;
    // the lesson's frozen slash
    public bool hasBeenTrapped;
    public float lastTapTime = -10f;
    public float parrySlowEndTime;
    // real time the parry slow motion ends
    public float prowlSign = 1f;
    // which way it circles the monkey
    public float nextProwlFlipTime;
    // the parry shot, real times
    public float shotStartTime;
    public float shotEndTime;
    public float shotMaxWeight = 1f;
    // how far in the shot may go, 0~1
    public float shotWeightCap = 1f;
    // eased toward that
    public float shotClashTime = -10f;
    public float shotSide = 1f;
    // which way it swings round the monkey
    public Vector3 shotFrom;
    // where the batted thing came from
    public bool isShotApplied;
    // the camera holds the shot till drawn
    public Quaternion shotViewRotation;
    // as last drawn in the shot
    public bool hasShotPose;
    public Vector3 restEyePosition;
    public Quaternion restEyeRotation;
    public float restFov;
    public Transform tail;
    // the monkey's own tail, swung for a parry
    public Quaternion tailRest;
    public float chargeSparkS;
    public float chargeRingS;
    public float tailTimeS = -1f;
    public bool hasSwingHit;
    // the current swing batted something back
    public GameObject stuckWeb;
    public bool isTimeBent;
    public bool isTeachFrozen;
    // the parry lesson stops time on a gold wave
    public SpiderKingShot frozenShot;
    public float bareBottomY;
    public float bareTopY;
    public SpiderKingTeachStep teach = SpiderKingTeachStep.Parry;
    public float teachTimeS;
    public bool isFinisherWin;
    public float finisherTimeS;
    public float reviveLeftS;
    public float adElapsedS;
    public bool isReturningToStart;
    public float defeatTimeS;

    public SpiderKingRig(MonoBehaviour owner, SpiderKingTuning tuning) {
        this.owner = owner;
        host = owner.transform;
        this.tuning = tuning;
    }

    // Public Methods  #########################################################
    /// <returns>if the fight is on, from its start till the reward is
    /// taken</returns>
    public bool IsInArena => phase != SpiderKingPhase.Waiting
        && phase != SpiderKingPhase.Done
        && phase != SpiderKingPhase.Over;

    public bool IsCallingOut => Time.unscaledTime < callOutEndTime;

    // share of its health lost, 0~1: the fight gets harder w/ it
    public float Rage => maxHealth > 0 ? 1f - health / (float)maxHealth : 0f;

    // 0, 1 or 2: the three stages of the fight
    public int Stage => Mathf.Min(isEasy ? 1 : 2,
        Rage < tuning.StageLostShares.x ? 0 : Rage < tuning.StageLostShares.y
            ? 1 : 2);

    // its chase's speed scale, 1 at full health up to lateChaseScale at none;
    // an endless boss keeps up w/ the faster monkey
    public float ChaseScale => teach == SpiderKingTeachStep.Done
        ? Mathf.Lerp(1f, tuning.LateChaseScale, Rage)
            * (isEasy ? tuning.EasySpeedScale : 1f) * RushSpeedScale
        : RushSpeedScale;

    // an endless boss's bullets & slashes come faster, so their gold & parry
    // reach grow w/ them: the time to bat one back stays the same
    public float ReachScale => teach == SpiderKingTeachStep.Done
        ? RushSpeedScale : 1f;

    public float WaveGoldNowU => tuning.WaveGoldU * ReachScale;

    public float ParryReachNowU => tuning.ParryReachU * ReachScale;

    // an endless boss's blades are wider, as the monkey is quicker to step
    // out of their way
    public float SlashWidthNowU => tuning.SlashWidthU
        * (IsRush ? tuning.EndlessSlashWidthScale : 1f);

    // speed scale, 1 at full health up to lateSpeedScale at none; a later
    // boss of the endless run's rush faster still
    public float SpeedScale => teach == SpiderKingTeachStep.Done
        ? Mathf.Lerp(1f, tuning.LateSpeedScale, Rage)
            * (isEasy ? tuning.EasySpeedScale : 1f) * RushSpeedScale
        : 1f;

    // wind up & gap scale, 1 at full health down to lateTimeScale at none; a
    // later boss of the endless run's rush shorter still
    public float TimeScaleNow => teach == SpiderKingTeachStep.Done
        ? Mathf.Lerp(1f, tuning.LateTimeScale, Rage)
            * (isEasy ? tuning.EasyTimeScale : 1f) * RushTimeScale
        : 1f;

    /// <returns>if this fight is a boss of the endless run's rush</returns>
    public bool IsRush => rushEncounter >= 0;

    /// <returns>scale on shot, slash & dash speeds of a boss of the endless
    /// run's rush, 1 outside it</returns>
    public float RushSpeedScale => IsRush
        ? EndlessBossRush.SpeedScale(rushEncounter) : 1f;

    /// <returns>scale on wind ups, gaps & rests of a boss of the endless
    /// run's rush, 1 outside it</returns>
    public float RushTimeScale => IsRush
        ? EndlessBossRush.TimeScale(rushEncounter) : 1f;

    /// <returns>the spider's full health on normal: more for a later boss of
    /// the endless run's rush</returns>
    public int BaseMaxHealth => Mathf.Max(1, Mathf.RoundToInt(
        tuning.WorldBalanceConfig.SpiderMaxHealth
            * (IsRush ? EndlessBossRush.HealthScale(rushEncounter) : 1f)));

    /// <returns>climbed height the fight begins at: the config's, or this
    /// boss's in the endless run's rush; u</returns>
    public float TriggerClimbU => IsRush
        ? EndlessBossRush.TriggerU(rushEncounter)
        : tuning.WorldBalanceConfig.SpiderTriggerHeightU;

    /// <returns>slashes in a row now: one at a time, except in the endless
    /// run's rush, where each later Spider King fight adds one per stage, up
    /// to the endless most</returns>
    public int SlashesNow {
        get {
            int normal = SpiderKingMovement.PerStage(tuning.SlashesPerStage,
                Stage);
            if (!IsRush) {
                return normal;
            }
            int most = SpiderKingMovement.PerStage(
                tuning.EndlessSlashesPerStage, Stage);
            int later = Mathf.Max(0, (rushEncounter - 1) / 2);
            return Mathf.Min(most, normal + later);
        }
    }

    public bool HasLiveShots => shots.Exists(s => !s.isCharging)
        || volleyLeft > 0;

    // the lowest the spider crawls: a bit below the entry, well above the
    // guard
    public float ArenaBottomY => entryY - 6f;

    public float SurfaceDistance(float angleA, float yA, float angleB,
        float yB) {
        float arcU = Mathf.DeltaAngle(angleA, angleB) * Mathf.Deg2Rad
            * tuning.TrunkRadiusU;
        float dy = yB - yA;
        return Mathf.Sqrt(arcU * arcU + dy * dy);
    }

    public float HorizontalDistance(Vector3 position) {
        Vector3 offset = position - trunkCenter;
        offset.y = 0f;
        return offset.magnitude;
    }

    public float AngleOf(Vector3 position) {
        Vector3 offset = position - trunkCenter;
        return Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    public Vector3 PointOnTrunk(float angleDeg, float y, float radius) {
        float radians = angleDeg * Mathf.Deg2Rad;
        return new Vector3(
            trunkCenter.x + Mathf.Cos(radians) * radius,
            y,
            trunkCenter.z + Mathf.Sin(radians) * radius
        );
    }

}
