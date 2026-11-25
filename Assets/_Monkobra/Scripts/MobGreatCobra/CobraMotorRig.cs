using UnityEngine;

/// <summary>
/// The state every great cobra role shares, plus the path geometry
/// more than one role asks of it.
/// </summary>
public sealed class CobraMotorRig {
    // Shared State  ##########################################################
    public readonly MonoBehaviour owner;
    public readonly Transform host;
    public readonly CobraMotorConfig config;
    public CobraMotorChase Chase;
    public CobraMotorSurge Surge;
    public CobraMotorGuard Guard;
    public CobraMotorHiss Hiss;
    public CobraMotorCoil Coil;
    public Transform pathCenter;
    public Transform[] bodySegments;
    public CobraBalanceConfig cobraBalanceConfig;
    public PlayerBalanceConfig playerBalanceConfig;
    public MonkeyClimbConfig monkeyClimbConfig;
    public AudioSource proximityHissSource;
    public AudioSource chaseAlertHissSource;
    public CapsuleCollider[] hissBodyShapes;
    public float nextProximityHissTime;
    public bool wasInHissRange;
    public Quaternion cartoonHeadRotation;
    public bool cartoonHeadRotationInitialized;
    public CartoonCobraVisual cartoonVisual;
    public float[] cartoonRadii;
    public Vector3[] cartoonOutward;
    public float headAngle;
    public float headHeight;
    public bool isGuarding;
    // the head reared up for the camera's look, from & till; Time.time
    public float alertRearStartTime = -1f;
    public float alertRearEndTime = -1f;
    // holding below the boss arena, never chasing
    public float nextPeekTime;
    // Time.time the next guard peek starts
    public float guardPeek;
    // 0~1 how far the head pokes up right now
    public float guardHeadHeight;
    // head height held while guarding
    public float currentClimbSpeed;
    // eased toward the target; u/s
    public bool isCatchingUp;
    public int surgeStacks;
    // mistakes stacked in the running surge
    public float surgeEndTime;
    // Time.time the running surge ends
    public float exitBoostEndTime;
    // Time.time the zone exit boost ends
    public GameState lastGameState;
    // state seen on the previous Update
    public float nextCountedHitTime;
    // Time.time b4 which hits add nothing
    public bool isFrozen;
    // held still by a coconut
    public float freezeEndTime;
    // Time.time the running freeze ends
    public float freezeDurationS;
    // length of the running or last freeze; s
    public float frozenTimeS;
    // total time spent frozen, kept off the wave; s
    public bool isStoryHeld;
    // climb stopped by a story event, orbit & slither go on
    public bool isStartHold;
    // the running story hold is the chase's start look
    public bool isStartEasing;
    // easing up slowly after the start hold, till the target speed
    public float exitBoostBankedS;
    // exit boost time kept through a start hold; s
    public HitBranchSensor playerSensor;
    public SpiderWebStruggle playerStruggle;
    public Transform player;
    public Vector3[] segmentPositions;

    public CobraMotorRig(MonoBehaviour owner, CobraMotorConfig config) {
        this.owner = owner;
        host = owner.transform;
        this.config = config;
    }

    // Public Methods  #########################################################
    public static bool IsInStartingZone =>
        GameController.I != null
        && GameController.I.State == GameState.StartingZone;

    public float Direction => config.Clockwise ? -1f : 1f;

    public int LastIndex => Mathf.Max(1, bodySegments.Length - 1);

    /// <returns>angle between neighbouring segments so the whole body spans
    /// <see cref="coilTurns"/> loops; deg</returns>
    public float SegmentAngleStepDeg => config.CoilTurns * 360f / LastIndex;

    /// <returns>height drop between neighbouring segments; U</returns>
    public float SegmentHeightStepU =>
        config.CoilTurns * config.CoilPitchU / LastIndex;

    /// <returns>body length of the whole coil, head to tail; U</returns>
    public float CoilLengthU => Mathf.Sqrt(
        Mathf.Pow(config.CoilTurns * 2f * Mathf.PI * config.OrbitRadiusU, 2f)
            + Mathf.Pow(config.CoilTurns * config.CoilPitchU, 2f)
    );

    /// <returns>body length between neighbouring segments; U</returns>
    public float SegmentArcStepU => CoilLengthU / LastIndex;

    public Vector3 GetPathPosition(float angleDegrees, float height) {
        float radians = angleDegrees * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * config.OrbitRadiusU,
            height,
            Mathf.Sin(radians) * config.OrbitRadiusU
        );

        return pathCenter.position + offset;
    }

}
