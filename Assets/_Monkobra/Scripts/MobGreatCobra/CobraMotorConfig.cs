using UnityEngine;

/// <summary>
/// Every tuned value of the great cobra, grouped as the Inspector shows
/// them. Read-only at runtime, so a value changed in Play mode persists
/// in the asset.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Objects/CobraMotorConfig")]
public class CobraMotorConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>distance from path center to the body; u</returns>
    public float OrbitRadiusU => orbitRadiusU;

    /// <returns>head height above path center at start; u</returns>
    public float StartHeightU => startHeightU;

    /// <returns>head climb speed; u/s</returns>
    public float ClimbSpeedUPerS => climbSpeedUPerS;

    /// <returns>head orbit speed round the trunk; deg/s</returns>
    public float OrbitSpeedDegPerS => orbitSpeedDegPerS;

    /// <returns>orbit direction seen from above</returns>
    public bool Clockwise => clockwise;

    /// <returns>full loops the body wraps round the trunk, head to
    /// tail; keep at least 1 so no gap is left</returns>
    public float CoilTurns => coilTurns;

    /// <returns>height the body rises per full loop, head on top; u</returns>
    public float CoilPitchU => coilPitchU;

    /// <returns>largest gap allowed between neighbouring segments
    /// along the coil; more segments are cloned until it holds; u</returns>
    public float MaxSegmentGapU => maxSegmentGapU;

    /// <returns>body thickness at its thickest point; u</returns>
    public float MaxThicknessU => maxThicknessU;

    /// <returns>thickness along the body as a share of max thickness,
    /// 0 = head, 1 = tail tip</returns>
    public AnimationCurve ThicknessProfile => thicknessProfile;

    /// <returns>how much of the spacing between segments each one
    /// fills; below 1 leaves a visible gap, the hit shape still has
    /// none</returns>
    public float SegmentFill => segmentFill;

    /// <returns>head length; u</returns>
    public float HeadLengthU => headLengthU;

    /// <returns>head width as a multiple of its thickness, cobra-hood
    /// flat</returns>
    public float HeadWidthRatio => headWidthRatio;

    /// <returns>body length from the head that rises off the coil; u</returns>
    public float NeckLengthU => neckLengthU;

    /// <returns>how far the head rises above the coil; the coil sits
    /// this much lower, so the head still stops at the player's
    /// height; u</returns>
    public float NeckLiftU => neckLiftU;

    /// <returns>up-down slither wave height, faded out at the neck; u</returns>
    public float WaveAmplitudeU => waveAmplitudeU;

    /// <returns>body length of one slither wave; u</returns>
    public float WaveLengthU => waveLengthU;

    /// <returns>slither waves passing down the body; 1/s</returns>
    public float WaveSpeedHz => waveSpeedHz;

    /// <returns>look of the cartoon head & body</returns>
    public CartoonCobraVisual.Settings CartoonAppearance => cartoonAppearance;

    /// <returns>how fast the head follows its target; higher is
    /// quicker</returns>
    public float HeadFollowSharpness => headFollowSharpness;

    /// <returns>head turn speed; deg/s</returns>
    public float HeadTurnSpeedDegPerS => headTurnSpeedDegPerS;

    /// <returns>monkey distance at which the head starts to rise; u</returns>
    public float HeadRaiseStartDistanceU => headRaiseStartDistanceU;

    /// <returns>monkey distance at which the head is fully raised; u</returns>
    public float HeadRaiseFullDistanceU => headRaiseFullDistanceU;

    /// <returns>time the head takes to rear up for the camera's look, & to
    /// lower again at its end; s</returns>
    public float AlertRearEaseS => alertRearEaseS;

    /// <returns>hiss played while the monkey is close</returns>
    public AudioClip ProximityHiss => proximityHiss;

    /// <returns>monkey distance at which the close hiss starts; u</returns>
    public float HissStartDistanceU => hissStartDistanceU;

    /// <returns>extra distance before the close hiss stops; u</returns>
    public float HissExitPaddingU => hissExitPaddingU;

    /// <returns>time between close hisses; s</returns>
    public float HissIntervalS => hissIntervalS;

    /// <returns>volume of the close hiss; 0~1</returns>
    public float HissVolume => hissVolume;

    /// <returns>pitch of the close hiss; 0.1~2</returns>
    public float HissPitch => hissPitch;

    /// <returns>close hiss is at full volume inside this distance; u</returns>
    public float HissMinDistanceU => hissMinDistanceU;

    /// <returns>close hiss is silent beyond this distance; u</returns>
    public float HissMaxDistanceU => hissMaxDistanceU;

    /// <returns>volume of the alert hiss as the chase starts; 0~1</returns>
    public float ChaseAlertHissVolume => chaseAlertHissVolume;

    /// <returns>eye diameter, eyes are skipped at 0; u</returns>
    public float EyeSizeU => eyeSizeU;

    /// <returns>least & most time between two peeks while guarding; s</returns>
    public Vector2 PeekIntervalRangeS => peekIntervalRangeS;

    /// <returns>how high the head pokes straight up over the coil; u</returns>
    public float PeekRiseU => peekRiseU;

    /// <returns>length of neck, from the head, that lifts to poke up;
    /// u</returns>
    public float PeekNeckU => peekNeckU;

    /// <returns>how far the lifting neck bows out from the trunk; u</returns>
    public float PeekCurlU => peekCurlU;

    /// <returns>peek: time up, held, & back down; s</returns>
    public Vector3 PeekTimesS => peekTimesS;

    // Inspector Fields  #######################################################
    [Header("Tree Path")]
    [SerializeField]
    [Tooltip("distance from path center to the body; u")]
    private float orbitRadiusU = 5.8f;

    [SerializeField]
    [Tooltip("head height above path center at start; u")]
    private float startHeightU = 0.5f;

    [SerializeField]
    [Tooltip("head climb speed; u/s")]
    private float climbSpeedUPerS = 0.25f;

    [SerializeField]
    [Tooltip("head orbit speed round the trunk; deg/s")]
    private float orbitSpeedDegPerS = 20f;

    [SerializeField]
    [Tooltip("orbit direction seen from above")]
    private bool clockwise = true;

    [Header("Coil Shape")]
    [SerializeField]
    [Tooltip("full loops the body wraps round the trunk, head to tail; keep "
        + "at least 1 so no gap is left")]
    private float coilTurns = 1.25f;

    [SerializeField]
    [Tooltip("height the body rises per full loop, head on top; u")]
    private float coilPitchU = 2.5f;

    [SerializeField]
    [Tooltip("largest gap allowed between neighbouring segments along the "
        + "coil; more segments are cloned until it holds; u")]
    private float maxSegmentGapU = 0.6f;

    [Header("Body Look")]
    [SerializeField]
    [Tooltip("body thickness at its thickest point; u")]
    private float maxThicknessU = 0.5f;

    [SerializeField]
    [Tooltip("thickness along the body as a share of max thickness, 0 = head, "
        + "1 = tail tip")]
    private AnimationCurve thicknessProfile = new AnimationCurve(
        new Keyframe(0f, 0.95f),
        new Keyframe(0.1f, 0.9f),
        new Keyframe(0.4f, 1f),
        new Keyframe(1f, 0.75f)
    );

    [SerializeField]
    [Tooltip("how much of the spacing between segments each one fills; below "
        + "1 leaves a visible gap, the hit shape still has none")]
    private float segmentFill = 0.8f;

    [SerializeField]
    [Tooltip("head length; u")]
    private float headLengthU = 0.75f;

    [SerializeField]
    [Tooltip("head width as a multiple of its thickness, cobra-hood flat")]
    private float headWidthRatio = 1.3f;

    [Header("Neck & Slither")]
    [SerializeField]
    [Tooltip("body length from the head that rises off the coil; u")]
    private float neckLengthU = 2f;

    [SerializeField]
    [Tooltip("how far the head rises above the coil; the coil sits this much "
        + "lower, so the head still stops at the player's height; u")]
    private float neckLiftU = 0.8f;

    [SerializeField]
    [Tooltip("up-down slither wave height, faded out at the neck; u")]
    private float waveAmplitudeU = 0.12f;

    [SerializeField]
    [Tooltip("body length of one slither wave; u")]
    private float waveLengthU = 2.5f;

    [SerializeField]
    [Tooltip("slither waves passing down the body; 1/s")]
    private float waveSpeedHz = 0.4f;

    [Header("Cartoon Appearance")]
    [SerializeField]
    [Tooltip("look of the cartoon head & body")]
    private CartoonCobraVisual.Settings cartoonAppearance = new();

    [Header("Chase Head Pose")]
    [SerializeField, Min(0.1f)]
    [Tooltip("how fast the head follows its target; higher is quicker")]
    private float headFollowSharpness = 5f;

    [SerializeField, Min(0f)]
    [Tooltip("head turn speed; deg/s")]
    private float headTurnSpeedDegPerS = 180f;

    [SerializeField, Min(.1f)]
    [Tooltip("monkey distance at which the head starts to rise; u")]
    private float headRaiseStartDistanceU = 9f;

    [SerializeField, Min(.1f)]
    [Tooltip("monkey distance at which the head is fully raised; u")]
    private float headRaiseFullDistanceU = 3.5f;

    [SerializeField, Min(.01f)]
    [Tooltip(
        "while the camera cuts to the cobra as the chase starts, the head "
            + "rears up, fully raised, over this time, & lowers over it at the end; s"
    )]
    private float alertRearEaseS = .4f;

    [Header("Close Chase Hiss")]
    [SerializeField]
    [Tooltip("hiss played while the monkey is close")]
    private AudioClip proximityHiss;

    [SerializeField, Min(.1f)]
    [Tooltip("monkey distance at which the close hiss starts; u")]
    private float hissStartDistanceU = 7f;

    [SerializeField, Min(0f)]
    [Tooltip("extra distance before the close hiss stops; u")]
    private float hissExitPaddingU = 1f;

    [SerializeField, Min(.1f)]
    [Tooltip("time between close hisses; s")]
    private float hissIntervalS = 2.8f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("volume of the close hiss; 0~1")]
    private float hissVolume = .75f;

    [SerializeField, Range(.1f, 2f)]
    [Tooltip("pitch of the close hiss; 0.1~2")]
    private float hissPitch = .82f;

    [SerializeField, Min(.1f)]
    [Tooltip("close hiss is at full volume inside this distance; u")]
    private float hissMinDistanceU = 3f;

    [SerializeField, Min(.1f)]
    [Tooltip("close hiss is silent beyond this distance; u")]
    private float hissMaxDistanceU = 35f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("volume of the alert hiss as the chase starts; 0~1")]
    private float chaseAlertHissVolume = .85f;

    [Header("Eyes")]
    [SerializeField]
    [Tooltip("eye diameter, eyes are skipped at 0; u")]
    private float eyeSizeU = 0.14f;

    [Header("Boss Arena Guard")]
    [SerializeField]
    [Tooltip("least & most time between two peeks while guarding; s")]
    private Vector2 peekIntervalRangeS = new Vector2(4f, 7f);

    [SerializeField]
    [Tooltip("how high the head pokes straight up over the coil; u")]
    [Min(0f)]
    private float peekRiseU = 3.5f;

    [SerializeField]
    [Tooltip("length of neck, from the head, that lifts to poke up; u")]
    [Min(0.1f)]
    private float peekNeckU = 3.5f;

    [SerializeField]
    [Tooltip("how far the lifting neck bows out from the trunk; u")]
    [Min(0f)]
    private float peekCurlU = 0.8f;

    [SerializeField]
    [Tooltip("peek: time up, held, & back down; s")]
    [Min(0.01f)]
    private Vector3 peekTimesS = new Vector3(0.25f, 0.4f, 0.5f);

    // Editor Validation  ######################################################
    private void OnValidate() {
        orbitRadiusU = Mathf.Max(0.01f, orbitRadiusU);
        climbSpeedUPerS = Mathf.Max(0f, climbSpeedUPerS);
        orbitSpeedDegPerS = Mathf.Max(0f, orbitSpeedDegPerS);
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
}
