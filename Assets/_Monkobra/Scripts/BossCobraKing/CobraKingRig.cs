using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CobraKingLinePriority {
    Normal,
    Key, // the minion call
    Urgent, // "Stay away from me!"
}

public enum CobraKingPhase {
    Waiting, // climbing to the trigger height
    Fighting,
    Finishing, // the last blow, in slow motion
    ReviveOffer, // time stopped, asking to watch an ad
    WatchingAd, // time stopped, the mock ad playing
    Defeated, // the king beaten, the crown on its way
    Over, // the monkey beaten
    Rewarding, // the armor reward, then a pause till the player goes on
    Done, // the king beaten & the climb going on, the armor kept
}

public enum CobraKingLoop {
    Intro, // before the first hit: no attacks
    Rest, // a pause between attack rounds
    Attack, // a round of attacks
}

public enum CobraKingAttack {
    Bite,
    Sweep,
    Minions,
}

public struct CobraKingArenaFruit {
    public GameObject root;
    public Collider trigger;
    public Renderer renderer;
    public bool isGolden;
    public GameObject branch;
    // the branch it grows on
    public float respawnTime;
    // Time.time it grows back, -1 if hanging
}

public struct CobraKingProjectile {
    public Transform transform;
    public Vector3 start;
    // world point it was thrown from
    public float ageS;
    public float nextTrailS;
    // age the next trail dot drops at
}

public struct CobraKingSpark {
    public Transform transform;
    public Vector3 velocity;
    public float size;
    public float ageS;
    public float lifeS;
}

public struct CobraKingMarker {
    public Transform rim;
    // full-size dark disc, null for the band
    public Transform fill;
    // grows with the warning's progress
    public float size;
    // disc diameter; u
    public int warning;
    // hud warning id, -1 if none
    public bool isBand;
    // the sweep's band round the trunk
    public bool isStrip;
    // a minion's vertical fall line
    public float topY;
    // strip top, world y
    public float length;
    // strip length; u
    public float angle;
    // strip angle round the trunk; deg
}

public struct CobraKingMinion {
    public float angle;
    // round the trunk; deg
    public Transform root;
    // null until it falls
    public bool isDone;
    // fallen, or the wave's gap
    public float dropDelayS;
    // wave time it starts to fall at
}

/// <summary>
/// The state every Cobra King role shares, plus the geometry queries
/// more than one role asks of it.
/// </summary>
public sealed class CobraKingRig {
    // constants  #############################################################
    public const string FRUIT_TAG = "FruitCollider";
    // the banana hangs about this far below its branch root; u
    public const float BANANA_HANG_U = 1f;
    // share of the win swirl spent flying in, the rest is the pulse
    public const float ARMOR_LAND_SHARE = 0.7f;
    public static readonly int OUTLINE_COLOR =
        Shader.PropertyToID("_Outline_Color");
    public static readonly int OUTLINE_WIDTH =
        Shader.PropertyToID("_Outline_Width");
    public static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
    public static readonly int COLOR = Shader.PropertyToID("_Color");

    // Shared State  ##########################################################
    public readonly MonoBehaviour owner;
    // the fight's moments for listeners such as its sounds, raised through
    // the controller's events; set by the controller
    public System.Action onAttackStruck;
    public System.Action onJewelHit;
    public System.Action onShattered;
    public readonly Transform host;
    public WorldBalanceConfig worldBalanceConfig;
    public readonly CobraKingConfig config;
    public CobraKingArena Arena;
    public CobraKingArmor Armor;
    public CobraKingThrows Throws;
    public CobraKingAttacks Attacks;
    public CobraKingMinions Minions;
    public CobraKingEnding Ending;
    public CobraKingFeedback Feedback;
    public CobraKingPhase phase = CobraKingPhase.Waiting;
    // the hardcore run's boss rush: this fight's encounter (0, 2, 4, ...), -1
    // outside the rush
    public int rushEncounter = -1;
    public CobraKingLoop loop;
    public CobraKingAttack attack;
    public readonly Queue<CobraKingAttack> attackQueue =
        new Queue<CobraKingAttack>();
    public float loopTimeS;
    // rest time left
    public float attackTimeS;
    // time into the running attack
    public float lockAngle;
    // monkey angle when the attack began; deg
    public float lockY;
    // monkey y when the attack began
    public float minionLockAngle;
    // monkey angle when the wave began; deg
    public float minionLockY;
    // monkey y when the wave began
    public bool isMinionStreaming;
    // normal finale: waves run on their own
    public bool isFinaleSweepNext;
    // the finale's next round is a sweep
    public bool isWaveRunning;
    // a minion wave is warning or falling
    public float nextWaveTime;
    // Time.time the stream's next wave starts
    public bool biteStarted;
    public int waveIndex;
    // minion wave under way
    public bool hasCalledMinions;
    // the minion call was said this game
    public CobraKingLinePriority heldPriority;
    // of the line held in the bubble
    public string pendingKeyLine;
    // a key line to say once the bubble frees
    public float waveTimeS;
    // time into the minion wave
    public Vector3 biteSpot;
    // world point the running bite dives at
    public bool tailHit;
    public int health;
    public bool hasHit;
    // the monkey has landed a first hit
    public bool hasShield;
    // the golden banana's shield is up
    public bool isHardcoreStart;
    // the hardcore run skips the fight
    public bool hasThrown;
    // the first banana has been thrown
    public bool isReturningToStart;
    // flying back to the fight's start
    public float reviveLeftS;
    // revive offer time left; unscaled s
    public float adElapsedS;
    // time into the mock ad; unscaled s
    public float cobraSaveGraceEndTime;
    // the armor's save ignores contacts
    public float chaseWarnTime;
    // Time.time the chase is called after a win
    public bool hasWarnedChase;
    public Vector3 entryPosition;
    // monkey's pose when the fight began
    public Quaternion entryRotation;
    public bool isChoosing;
    // waiting for the first throw to pick a mode
    public int bossLoop = 1;
    // if this fight plays Loop 0, the EASY fight
    public bool isEasy => bossLoop == 0;
    public int maxHealth;
    // the king's full health in the chosen mode
    public readonly List<CobraKingArenaFruit> choices =
        new List<CobraKingArenaFruit>();
    public bool hasWon;
    public bool isFinisherWin;
    public float finisherTimeS;
    // real time into the finisher; s
    public readonly List<SpiderWebTrap> webs = new List<SpiderWebTrap>();
    public readonly List<Vector3> arenaBranchSpots = new List<Vector3>();
    public readonly List<GameObject> arenaBranchFruits = new List<GameObject>();
    public bool isArenaFruitGone;
    // the fight is won, its fruit taken away
    public int goldenIndex = -1;
    // the golden banana in the fruit list
    public float nextGoldenTime;
    // Time.time the next visit starts
    public float goldenHideTime;
    // Time.time the current visit ends
    public float victoryTimeS;
    public float entryY;
    // monkey's y when the fight began
    public float coilBottomY;
    // world y of the king's coil
    public Vector3 trunkCenter;
    public string lastLine;
    public float keyLineEndTime;
    // Time.time a key line stops holding
    public float nextSameLineTime;
    public float nextPushTime;
    // Time.time b4 which no new butt starts
    public Vector3 crownStart;
    public Vector3 crownStartScale;
    public readonly List<CobraKingArenaFruit> fruits =
        new List<CobraKingArenaFruit>();
    public readonly List<CobraKingProjectile> projectiles =
        new List<CobraKingProjectile>();
    public readonly List<CobraKingSpark> sparks = new List<CobraKingSpark>();
    public readonly List<CobraKingMarker> markers = new List<CobraKingMarker>();
    public readonly List<CobraKingMinion> minions = new List<CobraKingMinion>();
    public Transform[] tail;
    public Transform[] dots;
    public Transform[] armor;
    public Vector3[] armorBaseScales;
    public float armorRewardTimeS = -1f;
    // into the win swirl, -1 if none
    public bool hasArmorLanded;
    // the swirl's pieces have settled
    public bool isArmorBadgeOn;
    // the hud's armor badge was last told to show
    public bool isRewardPanelShown;
    // the win's pause panel is up
    // cached references  ------------------------------------------------------
    public CobraKingBody body;
    public BossFightHud hud;
    public Transform monkey;
    public MonkeyController monkeyController;
    public SpiderWebStruggle struggle;
    public Transform crown;
    public CobraMotor greatCobra;
    public CameraClimbFraming cameraFraming;
    public MaterialPropertyBlock colorBlock;

    public CobraKingRig(MonoBehaviour owner, CobraKingConfig config) {
        this.owner = owner;
        host = owner.transform;
        this.config = config;
    }

    // Public Methods  #########################################################
    public string ChoiceLine =>
        config.ArmorIntroLine + "\n" + config.ChooseLine;

    public float BananaRespawnScale => Third >= 2
        ? config.BananaRespawnScaleByThird.y
        : Third >= 1
            ? config.BananaRespawnScaleByThird.x
            : 1f;

    // easy mode warns a notch longer: its last third as normal's start; a
    // later boss of the hardcore run's rush warns shorter
    public float WarnScale => (isEasy
        ? (Third >= 2 ? 1f : config.EasyWarnScale)
        : (Third >= 2 ? config.FinalWarnScale : 1f)) * LoopTimeScale;

    /// <returns>if this fight is a boss of the hardcore run's rush</returns>
    public bool IsRush => rushEncounter >= 0;

    /// <returns>the Loop a fight plays unless EASY is picked: 1 on Normal, its
    /// own in the hardcore run's rush</returns>
    public int NormalLoop => IsRush
        ? HardcoreBossRush.LoopOf(rushEncounter) : overrideLoop;

    // the Loop a dev scene forces on its boss, 1 in the game
    public int overrideLoop = 1;

    /// <returns>hearts the monkey has in this fight: the config's at Loop 0 &
    /// 1, fewer from Loop 2 on</returns>
    public int HeartsNow => bossLoop < 2 ? worldBalanceConfig.MonkeyHeartCount
        : Mathf.Max(1, Mathf.RoundToInt(
            worldBalanceConfig.CobraKingLoopGrowth.hearts
                .Evaluate(worldBalanceConfig.MonkeyHeartCount, bossLoop)));

    /// <returns>scale on this fight's defeat points: 1 at Loop 0 & 1, more
    /// from Loop 2 on</returns>
    public float PointsScale => bossLoop < 2 ? 1f
        : Mathf.Max(1f, worldBalanceConfig.CobraKingLoopGrowth.points.Evaluate(1f, bossLoop));

    /// <summary>Plays the Normal Loop of this fight again.</summary>
    public void ResetLoop() {
        bossLoop = NormalLoop;
    }

    /// <returns>scale on the warnings & rests at Loop 2 and up, 1 before
    /// </returns>
    public float LoopTimeScale => bossLoop < 2 ? 1f
        : worldBalanceConfig.CobraKingLoopGrowth.time.Evaluate(1f, bossLoop);

    /// <returns>the king's full health at this Loop: EASY's share of Loop 1,
    /// Loop 1's, or grown from Loop 2 on</returns>
    public int BaseMaxHealth {
        get {
            int loopOne = worldBalanceConfig.BossMaxHealth;
            if (bossLoop >= 2) {
                return Mathf.Max(1, Mathf.RoundToInt(
                    worldBalanceConfig.CobraKingLoopGrowth.health
                        .Evaluate(loopOne, bossLoop)));
            }
            return bossLoop == 0 ? Mathf.Max(1, Mathf.RoundToInt(
                loopOne * config.EasyHealthScale)) : loopOne;
        }
    }

    /// <returns>climbed height the fight begins at: the config's, or this
    /// boss's in the hardcore run's rush; u</returns>
    public float TriggerClimbU => Layout.locationU;

    /// <returns>this fight's arena: the config's, or this boss's in the
    /// hardcore run's rush</returns>
    public BossArenaLayout Layout => IsRush
        ? HardcoreBossRush.Layout(rushEncounter)
        : worldBalanceConfig.CobraKingLayout;

    /// <returns>this fight's bare zone, bottom & top in world y</returns>
    public Vector2 WorldBare {
        get {
            RampedDifficultyManager ramp = RampedDifficultyManager.I;
            return Layout.WorldBare(
                ramp != null ? ramp.PlayerGameStartYPosition : 0f);
        }
    }

    /// <returns>world y of the guard cobra's head: the config's depth below
    /// the start, never deeper than the layout allows</returns>
    public float GuardBottomY => entryY
        - Mathf.Min(config.GuardDepthU, Layout.heightU * 0.5f);

    public int MinionWaves => isEasy ? config.EasyMinionWaves
        : bossLoop >= 2 ? Mathf.RoundToInt(
            worldBalanceConfig.CobraKingLoopGrowth.minionWaves
                .Evaluate(config.MinionWaves, bossLoop))
            : config.MinionWaves;

    // normal mode's last third: minion waves stream on & on, alongside the
    // usual rounds of bites & sweeps, until the king or the monkey falls;
    // easy mode keeps minions as one attack of its rounds
    public bool IsMinionFinale => !isEasy && Third >= 2;

    // world y a falling minion is gone at, the bottom of its fall line
    public float MinionEndY => GuardBottomY;

    /// <returns>share of its health the king has lost, 0~1</returns>
    public float LostShare => maxHealth > 0
        ? 1f - health / (float)maxHealth
        : 0f;

    /// <returns>how many thirds of its health the king has lost: 0, 1, 2
    /// </returns>
    public int Third {
        get {
            float lost = 1f - health / (float)maxHealth;
            return Mathf.Clamp(Mathf.FloorToInt(lost * 3f), 0, 2);
        }
    }

    // horizontal distance from the trunk axis to the monkey
    public float MonkeyRadius() {
        Vector3 offset = monkey.position - trunkCenter;
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
