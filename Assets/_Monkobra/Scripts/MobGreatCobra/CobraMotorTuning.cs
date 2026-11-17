using UnityEngine;

/// <summary>
/// Every Inspector-tuned value of the great cobra, as the Inspector
/// shows them. Read through the properties; no role writes one.
/// </summary>
[System.Serializable]
public sealed class CobraMotorTuning {
    // Public Members  #########################################################
    public Transform PathCenter => pathCenter;
    public Transform[] BodySegments => bodySegments;
    public CobraBalanceConfig CobraBalanceConfig => cobraBalanceConfig;
    public PlayerBalanceConfig PlayerBalanceConfig => playerBalanceConfig;
    public MonkeyClimbConfig MonkeyClimbConfig => monkeyClimbConfig;
    public float OrbitRadius => orbitRadius;
    public float StartHeight => startHeight;
    public float ClimbSpeed => climbSpeed;
    public float OrbitSpeedDegrees => orbitSpeedDegrees;
    public bool Clockwise => clockwise;
    public float CoilTurns => coilTurns;
    public float CoilPitchU => coilPitchU;
    public float MaxSegmentGapU => maxSegmentGapU;
    public float MaxThicknessU => maxThicknessU;
    public AnimationCurve ThicknessProfile => thicknessProfile;
    public float SegmentFill => segmentFill;
    public float HeadLengthU => headLengthU;
    public float HeadWidthRatio => headWidthRatio;
    public float NeckLengthU => neckLengthU;
    public float NeckLiftU => neckLiftU;
    public float WaveAmplitudeU => waveAmplitudeU;
    public float WaveLengthU => waveLengthU;
    public float WaveSpeedHz => waveSpeedHz;
    public CartoonCobraVisual.Settings CartoonAppearance => cartoonAppearance;
    public float HeadFollowSharpness => headFollowSharpness;
    public float HeadTurnSpeedDegrees => headTurnSpeedDegrees;
    public float HeadRaiseStartDistanceU => headRaiseStartDistanceU;
    public float HeadRaiseFullDistanceU => headRaiseFullDistanceU;
    public AudioClip ProximityHiss => proximityHiss;
    public float HissStartDistanceU => hissStartDistanceU;
    public float HissExitPaddingU => hissExitPaddingU;
    public float HissIntervalS => hissIntervalS;
    public float HissVolume => hissVolume;
    public float HissPitch => hissPitch;
    public float HissMinDistanceU => hissMinDistanceU;
    public float HissMaxDistanceU => hissMaxDistanceU;
    public float ChaseAlertHissVolume => chaseAlertHissVolume;
    public float EyeSizeU => eyeSizeU;
    public Vector2 PeekIntervalRangeS => peekIntervalRangeS;
    public float PeekRiseU => peekRiseU;
    public float PeekNeckU => peekNeckU;
    public float PeekCurlU => peekCurlU;
    public Vector3 PeekTimesS => peekTimesS;

    // Public Methods  #########################################################
    /// <summary>Clamps the values the Inspector can push out of
    /// range.</summary>
    public void Validate() {
        orbitRadius = Mathf.Max(0.01f, orbitRadius);
        climbSpeed = Mathf.Max(0f, climbSpeed);
        orbitSpeedDegrees = Mathf.Max(0f, orbitSpeedDegrees);
        coilTurns = Mathf.Max(1f, coilTurns);
        coilPitchU = Mathf.Max(0f, coilPitchU);
        maxSegmentGapU = Mathf.Max(0.05f, maxSegmentGapU);
        maxThicknessU = Mathf.Max(0.01f, maxThicknessU);
        segmentFill = Mathf.Clamp(segmentFill, 0.2f, 2f);
        headLengthU = Mathf.Max(0.01f, headLengthU);
        headWidthRatio = Mathf.Max(0.1f, headWidthRatio);
        neckLengthU = Mathf.Max(0.01f, neckLengthU);
        neckLiftU = Mathf.Max(0f, neckLiftU);
        waveAmplitudeU = Mathf.Max(0f, waveAmplitudeU);
        waveLengthU = Mathf.Max(0.01f, waveLengthU);
        eyeSizeU = Mathf.Max(0f, eyeSizeU);
    }

    // Inspector Fields  #######################################################
    [Header("References")]
    [SerializeField]
    [Tooltip("trunk axis the cobra orbits, its y is the path's zero height")]
    private Transform pathCenter;
    [SerializeField]
    [Tooltip(
        "body segments, head first; missing ones are cloned from the last "
            + "to fill the coil"
    )]
    private Transform[] bodySegments;
    [SerializeField]
    [Tooltip(
        "cobra balance tuning; catch-up when the monkey is far ahead, "
            + "none if unset"
    )]
    private CobraBalanceConfig cobraBalanceConfig;
    [SerializeField]
    [Tooltip(
        "player balance tuning; hit stun length, so one hit counts once; "
            + "none if unset"
    )]
    private PlayerBalanceConfig playerBalanceConfig;
    [SerializeField]
    [Tooltip(
        "monkey climb tuning; its top up speed is what the base speed "
            + "multipliers scale; inspector climb speed if unset"
    )]
    private MonkeyClimbConfig monkeyClimbConfig;
    [Header("Tree Path")]
    [SerializeField]
    [Tooltip("distance from path center to the body; U")]
    private float orbitRadius = 5.8f;
    [SerializeField]
    [Tooltip("head height above path center at start; U")]
    private float startHeight = 0.5f;
    [SerializeField]
    [Tooltip("head climb speed; U/s")]
    private float climbSpeed = 0.25f;
    [SerializeField]
    [Tooltip("head orbit speed round the trunk; deg/s")]
    private float orbitSpeedDegrees = 20f;
    [SerializeField]
    [Tooltip("orbit direction seen from above")]
    private bool clockwise = true;
    [Header("Coil Shape")]
    [SerializeField]
    [Tooltip(
        "full loops the body wraps round the trunk, head to tail; "
            + "keep at least 1 so no gap is left"
    )]
    private float coilTurns = 1.25f;
    [SerializeField]
    [Tooltip("height the body rises per full loop, head on top; U")]
    private float coilPitchU = 2.5f;
    [SerializeField]
    [Tooltip(
        "largest gap allowed between neighbouring segments along the coil; "
            + "more segments are cloned until it holds; U"
    )]
    private float maxSegmentGapU = 0.6f;
    [Header("Body Look")]
    [SerializeField]
    [Tooltip("body thickness at its thickest point; U")]
    private float maxThicknessU = 0.5f;
    [SerializeField]
    [Tooltip(
        "thickness along the body as a share of max thickness, "
            + "0 = head, 1 = tail tip"
    )]
    private AnimationCurve thicknessProfile = new AnimationCurve(
        new Keyframe(0f, 0.95f),
        new Keyframe(0.1f, 0.9f),
        new Keyframe(0.4f, 1f),
        new Keyframe(1f, 0.75f)
    );
    [SerializeField]
    [Tooltip(
        "how much of the spacing between segments each one fills; below 1 "
            + "leaves a visible gap, the hit shape still has none"
    )]
    private float segmentFill = 0.8f;
    [SerializeField]
    [Tooltip("head length; U")]
    private float headLengthU = 0.75f;
    [SerializeField]
    [Tooltip("head width as a multiple of its thickness, cobra-hood flat")]
    private float headWidthRatio = 1.3f;
    [Header("Neck & Slither")]
    [SerializeField]
    [Tooltip("body length from the head that rises off the coil; U")]
    private float neckLengthU = 2f;
    [SerializeField]
    [Tooltip(
        "how far the head rises above the coil; the coil sits this much "
            + "lower, so the head still stops at the player's height; U"
    )]
    private float neckLiftU = 0.8f;
    [SerializeField]
    [Tooltip("up-down slither wave height, faded out at the neck; U")]
    private float waveAmplitudeU = 0.12f;
    [SerializeField]
    [Tooltip("body length of one slither wave; U")]
    private float waveLengthU = 2.5f;
    [SerializeField]
    [Tooltip("slither waves passing down the body; 1/s")]
    private float waveSpeedHz = 0.4f;
    [Header("Cartoon Appearance")]
    [SerializeField]
    private CartoonCobraVisual.Settings cartoonAppearance = new();
    [Header("Chase Head Pose")]
    [SerializeField, Min(0.1f)]
    private float headFollowSharpness = 5f;
    [SerializeField, Min(0f)]
    private float headTurnSpeedDegrees = 180f;
    [SerializeField, Min(.1f)]
    private float headRaiseStartDistanceU = 9f;
    [SerializeField, Min(.1f)]
    private float headRaiseFullDistanceU = 3.5f;
    [Header("Close Chase Hiss")]
    [SerializeField]
    private AudioClip proximityHiss;
    [SerializeField, Min(.1f)]
    private float hissStartDistanceU = 7f;
    [SerializeField, Min(0f)]
    private float hissExitPaddingU = 1f;
    [SerializeField, Min(.1f)]
    private float hissIntervalS = 2.8f;
    [SerializeField, Range(0f, 1f)]
    private float hissVolume = .75f;
    [SerializeField, Range(.1f, 2f)]
    private float hissPitch = .82f;
    [SerializeField, Min(.1f)]
    private float hissMinDistanceU = 3f;
    [SerializeField, Min(.1f)]
    private float hissMaxDistanceU = 35f;
    [SerializeField, Range(0f, 1f)]
    private float chaseAlertHissVolume = .85f;
    [Header("Eyes")]
    [SerializeField]
    [Tooltip("eye diameter, eyes are skipped at 0; U")]
    private float eyeSizeU = 0.14f;
    [Header("Boss Arena Guard")]
    [SerializeField]
    [Tooltip("least & most time between two peeks while guarding; s")]
    private Vector2 peekIntervalRangeS = new Vector2(4f, 7f);
    [SerializeField]
    [Min(0f)]
    [Tooltip("how high the head pokes straight up over the coil; u")]
    private float peekRiseU = 3.5f;
    [SerializeField]
    [Min(0.1f)]
    [Tooltip("length of neck, from the head, that lifts to poke up; u")]
    private float peekNeckU = 3.5f;
    [SerializeField]
    [Min(0f)]
    [Tooltip("how far the lifting neck bows out from the trunk; u")]
    private float peekCurlU = 0.8f;
    [SerializeField]
    [Min(0.01f)]
    [Tooltip("peek: time up, held, & back down; s")]
    private Vector3 peekTimesS = new Vector3(0.25f, 0.4f, 0.5f);
}
