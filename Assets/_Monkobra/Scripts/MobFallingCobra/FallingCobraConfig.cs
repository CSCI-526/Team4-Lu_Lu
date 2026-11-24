using UnityEngine;

/// <summary>
/// Every tuned value of a falling cobra, grouped as the Inspector shows
/// them: one asset every falling cobra reads. Read-only at runtime, so
/// a value changed in Play mode persists in the asset.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Objects/FallingCobraConfig")]
public class FallingCobraConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>3D distance to the monkey below which the cobra
    /// wakes; u</returns>
    public float TriggerDistanceU => triggerDistanceU;

    /// <returns>head-up hiss duration after spotting the monkey,
    /// before attacking; s</returns>
    public float WarningDurationS => warningDurationS;

    /// <returns>extra distance past the trigger before the cobra
    /// stands down; u</returns>
    public float AlertExitPaddingU => alertExitPaddingU;

    /// <returns>hiss played while the cobra watches the monkey</returns>
    public AudioClip WarningHiss => warningHiss;

    /// <returns>volume of the watch hiss; 0~1</returns>
    public float WarningHissVolume => warningHissVolume;

    /// <returns>time between watch hisses; s</returns>
    public float WarningHissIntervalS => warningHissIntervalS;

    /// <returns>watch hiss is at full volume inside this distance; u</returns>
    public float WarningHissMinDistanceU => warningHissMinDistanceU;

    /// <returns>how far the head tilts down while it watches; deg</returns>
    public float WaitingLookDownDeg => waitingLookDownDeg;

    /// <returns>how far the head lifts while it waits; u</returns>
    public float WaitingHeadLiftU => waitingHeadLiftU;

    /// <returns>height of the face while standing; u</returns>
    public float StandingFaceHeightU => standingFaceHeightU;

    /// <returns>neck length while standing; u</returns>
    public float StandingNeckLengthU => standingNeckLengthU;

    /// <returns>how far the chest lifts while standing; u</returns>
    public float StandingChestLiftU => standingChestLiftU;

    /// <returns>body twist along its length while standing; deg/u</returns>
    public float BodyTwistDegPerU => bodyTwistDegPerU;

    /// <returns>steepest belly angle the supporting coil may take;
    /// deg</returns>
    public float MaximumSupportBellyAngleDeg => maximumSupportBellyAngleDeg;

    /// <returns>height of the standing base; u</returns>
    public float StandingBaseHeightU => standingBaseHeightU;

    /// <returns>outer radius of the standing coil; u</returns>
    public float StandingCoilOuterRadiusU => standingCoilOuterRadiusU;

    /// <returns>inner radius of the standing coil; u</returns>
    public float StandingCoilInnerRadiusU => standingCoilInnerRadiusU;

    /// <returns>turns of the standing coil</returns>
    public float StandingCoilTurns => standingCoilTurns;

    /// <returns>length of branch the coil spans; u</returns>
    public float BranchCoilSpanU => branchCoilSpanU;

    /// <returns>turns the body wraps round the branch</returns>
    public float BranchCoilTurns => branchCoilTurns;

    /// <returns>sitting coil radius along the branch; u</returns>
    public float SittingCoilRadiusAlongU => sittingCoilRadiusAlongU;

    /// <returns>sitting coil radius across the branch; u</returns>
    public float SittingCoilRadiusAcrossU => sittingCoilRadiusAcrossU;

    /// <returns>inner coil size as a share of the outer; 0.1~0.9</returns>
    public float SittingCoilInnerScale => sittingCoilInnerScale;

    /// <returns>height each coil layer rises; u</returns>
    public float SittingCoilLayerRiseU => sittingCoilLayerRiseU;

    /// <returns>gap between the body skin & the branch; u</returns>
    public float BranchSkinClearanceU => branchSkinClearanceU;

    /// <returns>how far the head sits forward along the branch; u</returns>
    public float BranchHeadForwardU => branchHeadForwardU;

    /// <returns>how far the hood neck sits back from the head; u</returns>
    public float HoodNeckSetbackU => hoodNeckSetbackU;

    /// <returns>neck length kept clear of the branch near the hood; u</returns>
    public float HoodNeckClearanceLengthU => hoodNeckClearanceLengthU;

    /// <returns>clearance the body keeps from the branch as it
    /// leaves; u</returns>
    public float BranchEscapeClearanceU => branchEscapeClearanceU;

    /// <returns>how far the head sits to the branch side; u</returns>
    public float BranchHeadSideU => branchHeadSideU;

    /// <returns>how fast the perched head turns to aim; higher is
    /// quicker</returns>
    public float PerchedAimSharpness => perchedAimSharpness;

    /// <returns>thinnest the neck may get, as a share of body
    /// thickness; 0.6~1</returns>
    public float MinimumNeckThicknessShare => minimumNeckThicknessShare;

    /// <returns>sway of the free neck; u</returns>
    public float FreeNeckSwayU => freeNeckSwayU;

    /// <returns>how far the body drops to settle on the bark; u</returns>
    public float BodyBarkSettleDropU => bodyBarkSettleDropU;

    /// <returns>soft edge where the body meets the bark; u</returns>
    public float BodyBarkContactFeatherU => bodyBarkContactFeatherU;

    /// <returns>time the cobra takes to uncoil from its stand; s</returns>
    public float StandReleaseDurationS => standReleaseDurationS;

    /// <returns>how far the head reaches out as it uncoils; u</returns>
    public float StandReleaseReachU => standReleaseReachU;

    /// <returns>how fast the head eases into a pose; higher is
    /// quicker</returns>
    public float HeadPoseSharpness => headPoseSharpness;

    /// <returns>length of the neck collar; u</returns>
    public float NeckCollarLengthU => neckCollarLengthU;

    /// <returns>attack travel distance b4 the cobra switches itself
    /// off; u</returns>
    public float MaxFallDistanceU => maxFallDistanceU;

    /// <returns>longest an attack lasts before the cobra switches
    /// off; s</returns>
    public float MaximumFallDurationS => maximumFallDurationS;

    /// <returns>time the attack takes to reach full speed; s</returns>
    public float AttackSpeedRampS => attackSpeedRampS;

    /// <returns>distance over which the sideways slide brakes; u</returns>
    public float AttackSideBrakeDistanceU => attackSideBrakeDistanceU;

    /// <returns>trunk radius the cobra swims along; u</returns>
    public float TreeSurfaceRadiusU => treeSurfaceRadiusU;

    /// <returns>gap kept between the body & the trunk; u</returns>
    public float TreeSurfaceClearanceU => treeSurfaceClearanceU;

    /// <returns>how deep the belly presses into the bark; u</returns>
    public float BellyPressDepthU => bellyPressDepthU;

    /// <returns>time of the wind-up before a strike; s</returns>
    public float StrikeWindupDurationS => strikeWindupDurationS;

    /// <returns>how far the head pulls back in the wind-up; u</returns>
    public float StrikePullbackU => strikePullbackU;

    /// <returns>how much the body compresses in the wind-up; u</returns>
    public float StrikeCompressionU => strikeCompressionU;

    /// <returns>how fast the strike pose is reached; higher is
    /// quicker</returns>
    public float StrikePoseSharpness => strikePoseSharpness;

    /// <returns>time the launch takes to build speed; s</returns>
    public float StrikeLaunchRampS => strikeLaunchRampS;

    /// <returns>time of the speed burst of a strike; s</returns>
    public float StrikeBurstDurationS => strikeBurstDurationS;

    /// <returns>speed of the strike burst, as a multiple of the fall
    /// speed</returns>
    public float StrikeSpeedMultiplier => strikeSpeedMultiplier;

    /// <returns>share of the head motion the body follows in a
    /// strike; 0.2~1</returns>
    public float StrikeHeadFollowScale => strikeHeadFollowScale;

    /// <returns>smoothing time of the body trailing the head; s</returns>
    public float HeadTrailSmoothTimeS => headTrailSmoothTimeS;

    /// <returns>how far the head drops as it turns; u</returns>
    public float HeadTurnDropU => headTurnDropU;

    /// <returns>distance within which the head tracks the monkey; u</returns>
    public float HeadTrackingRangeU => headTrackingRangeU;

    /// <returns>extra distance before head tracking ends; u</returns>
    public float HeadTrackingExitPaddingU => headTrackingExitPaddingU;

    /// <returns>head turn speed while looking; deg/s</returns>
    public float HeadLookTurnSpeedDegPerS => headLookTurnSpeedDegPerS;

    /// <returns>head pitch on a flat surface; deg</returns>
    public float FlatHeadPitchDeg => flatHeadPitchDeg;

    /// <returns>head pitch when aiming upward; deg</returns>
    public float UpwardHeadPitchDeg => upwardHeadPitchDeg;

    /// <returns>head turn allowed along the surface; deg</returns>
    public float SurfaceHeadTurnDeg => surfaceHeadTurnDeg;

    /// <returns>gap kept between the head & the bark; u</returns>
    public float HeadBarkGapU => headBarkGapU;

    /// <returns>radius of the head hit contact; u</returns>
    public float HeadContactRadiusU => headContactRadiusU;

    /// <returns>hit sections along the body</returns>
    public int BodyContactSections => bodyContactSections;

    /// <returns>distance inside which the aim is held steady; u</returns>
    public float SteadyAimDistanceU => steadyAimDistanceU;

    /// <returns>aim sway kept when close; 0~1</returns>
    public float CloseAimSwayScale => closeAimSwayScale;

    /// <returns>how fast the head steers; higher is quicker</returns>
    public float HeadSteeringSharpness => headSteeringSharpness;

    /// <returns>slowest approach, as a share of the fall speed; 0.2~1</returns>
    public float MinimumApproachFallScale => minimumApproachFallScale;

    /// <returns>range over which the approach slows; u</returns>
    public float ApproachDropRangeU => approachDropRangeU;

    /// <returns>how strongly the head aims at its target</returns>
    public float HeadAimGain => headAimGain;

    /// <returns>damping of the head aim</returns>
    public float HeadAimDamping => headAimDamping;

    /// <returns>turn allowed along the tracking surface; deg</returns>
    public float TrackingSurfaceTurnDeg => trackingSurfaceTurnDeg;

    /// <returns>margin above the target height where the fall stops;
    /// u</returns>
    public float TargetHeightStopMarginU => targetHeightStopMarginU;

    /// <returns>how strongly the fall eases into the target height</returns>
    public float VerticalArrivalGain => verticalArrivalGain;

    /// <returns>furthest the head may stretch in a strike; u</returns>
    public float MaximumStrikeReachU => maximumStrikeReachU;

    /// <returns>distance at which a strike is reached for; u</returns>
    public float StrikeReachDistanceU => strikeReachDistanceU;

    /// <returns>distance over which the support blends out of the
    /// coil; u</returns>
    public float UncoilSupportBlendU => uncoilSupportBlendU;

    /// <returns>whether the eye anchors below are filled in</returns>
    public bool EyeAnchorsReady => eyeAnchorsReady;

    /// <returns>left eye position on the head surface, local; u</returns>
    public Vector3 LeftEyeSurfaceLocal => leftEyeSurfaceLocal;

    /// <returns>right eye position on the head surface, local; u</returns>
    public Vector3 RightEyeSurfaceLocal => rightEyeSurfaceLocal;

    /// <returns>left eye surface normal, local</returns>
    public Vector3 LeftEyeNormalLocal => leftEyeNormalLocal;

    /// <returns>right eye surface normal, local</returns>
    public Vector3 RightEyeNormalLocal => rightEyeNormalLocal;

    /// <returns>radius of an eye white; u</returns>
    public float EyeWhiteRadiusU => eyeWhiteRadiusU;

    /// <returns>radius of a pupil; u</returns>
    public float EyePupilRadiusU => eyePupilRadiusU;

    /// <returns>gap b/t body segments; u</returns>
    public float SegmentSpacingU => segmentSpacingU;

    /// <returns>hit radius of each segment; u</returns>
    public float HitRadiusU => hitRadiusU;

    /// <returns>look of the cartoon head & body</returns>
    public CartoonCobraVisual.Settings CartoonAppearance => cartoonAppearance;

    /// <returns>scale of the snake against its reference size</returns>
    public float ReferenceSizeMultiplier => referenceSizeMultiplier;

    /// <returns>face direction of the reference head, local</returns>
    public Vector3 ReferenceFaceDirection => referenceFaceDirection;

    /// <returns>speed of the body wave; Hz</returns>
    public float BodyWaveSpeedHz => bodyWaveSpeedHz;

    /// <returns>wave cycles along the body</returns>
    public float BodyWaveCycles => bodyWaveCycles;

    /// <returns>how fast the swim path turns; higher is quicker</returns>
    public float SwimTurnSharpness => swimTurnSharpness;

    /// <returns>length of the smooth body; u</returns>
    public float SmoothBodyLengthU => smoothBodyLengthU;

    /// <returns>radius of the smooth body; u</returns>
    public float SmoothBodyRadiusU => smoothBodyRadiusU;

    /// <returns>most the body bends while swimming; deg</returns>
    public float BodyBendDeg => bodyBendDeg;

    /// <returns>side to side head sway; u</returns>
    public float HeadSwayAmplitudeU => headSwayAmplitudeU;

    /// <returns>length of one swim wave; u</returns>
    public float SwimWavelengthU => swimWavelengthU;

    /// <returns>distance over which the sway fades in; u</returns>
    public float HeadSwayEntryDistanceU => headSwayEntryDistanceU;

    /// <returns>how much the gaze is held steady against sway; 0~1</returns>
    public float HeadGazeStabilization => headGazeStabilization;

    /// <returns>neck length that follows the head; u</returns>
    public float NeckFollowLengthU => neckFollowLengthU;

    /// <returns>most the head leans; deg</returns>
    public float MaximumHeadLeanDeg => maximumHeadLeanDeg;

    /// <returns>face clearance while rising; u</returns>
    public float RisingFaceClearanceU => risingFaceClearanceU;

    /// <returns>neck thickness at its waist, as a share of the body;
    /// 0.5~1</returns>
    public float NeckWaistScale => neckWaistScale;

    /// <returns>time of the release curve; s</returns>
    public float ReleaseCurveDurationS => releaseCurveDurationS;

    /// <returns>length over which head contact blends; u</returns>
    public float HeadContactBlendLengthU => headContactBlendLengthU;

    /// <returns>neck length that is rounded; u</returns>
    public float RoundedNeckLengthU => roundedNeckLengthU;

    /// <returns>smoothing passes over the rounded neck</returns>
    public int RoundedNeckPasses => roundedNeckPasses;

    /// <returns>largest offset the rounding may add; u</returns>
    public float RoundedNeckMaxOffsetU => roundedNeckMaxOffsetU;

    // Inspector Fields  #######################################################
    [Header("Trigger")]
    [SerializeField]
    [Tooltip("3D distance to the monkey below which the cobra wakes; u")]
    private float triggerDistanceU = 12f;

    [Header("Watch and Fall")]
    [SerializeField]
    [Tooltip("head-up hiss duration after spotting the monkey, before "
        + "attacking; s")]
    private float warningDurationS = 2.5f;

    [SerializeField, Min(0f)]
    [Tooltip("extra distance past the trigger before the cobra stands down; u")]
    private float alertExitPaddingU = 3f;

    [Header("Watch Hiss")]
    [SerializeField]
    [Tooltip("hiss played while the cobra watches the monkey")]
    private AudioClip warningHiss;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("volume of the watch hiss; 0~1")]
    private float warningHissVolume = .3f;

    [SerializeField, Min(.1f)]
    [Tooltip("time between watch hisses; s")]
    private float warningHissIntervalS = 2.4f;

    [SerializeField, Min(.1f)]
    [Tooltip("watch hiss is at full volume inside this distance; u")]
    private float warningHissMinDistanceU = 3f;

    [Header("Watch Head Pose")]
    [SerializeField, Range(0f, 70f)]
    [Tooltip("how far the head tilts down while it watches; deg")]
    private float waitingLookDownDeg = 35f;

    [SerializeField, Min(0f)]
    [Tooltip("how far the head lifts while it waits; u")]
    private float waitingHeadLiftU = .7f;

    [SerializeField, Min(.1f)]
    [Tooltip("height of the face while standing; u")]
    private float standingFaceHeightU = 2.2f;

    [SerializeField, Min(.1f)]
    [Tooltip("neck length while standing; u")]
    private float standingNeckLengthU = 1.8f;

    [SerializeField, Min(0f)]
    [Tooltip("how far the chest lifts while standing; u")]
    private float standingChestLiftU = .4f;

    [SerializeField, Min(1f)]
    [Tooltip("body twist along its length while standing; deg/u")]
    private float bodyTwistDegPerU = 480f;

    [SerializeField, Range(0f, 89f)]
    [Tooltip("steepest belly angle the supporting coil may take; deg")]
    private float maximumSupportBellyAngleDeg = 75f;

    [SerializeField, Min(.1f)]
    [Tooltip("height of the standing base; u")]
    private float standingBaseHeightU = 1f;

    [SerializeField, Min(.1f)]
    [Tooltip("outer radius of the standing coil; u")]
    private float standingCoilOuterRadiusU = .8f;

    [SerializeField, Min(.05f)]
    [Tooltip("inner radius of the standing coil; u")]
    private float standingCoilInnerRadiusU = .35f;

    [SerializeField, Min(.5f)]
    [Tooltip("turns of the standing coil")]
    private float standingCoilTurns = 1.1f;

    [Header("Branch Perch")]
    [SerializeField, Min(.1f)]
    [Tooltip("length of branch the coil spans; u")]
    private float branchCoilSpanU = 1.8f;

    [SerializeField, Min(.5f)]
    [Tooltip("turns the body wraps round the branch")]
    private float branchCoilTurns = 3f;

    [SerializeField, Min(.1f)]
    [Tooltip("sitting coil radius along the branch; u")]
    private float sittingCoilRadiusAlongU = 1.05f;

    [SerializeField, Min(.1f)]
    [Tooltip("sitting coil radius across the branch; u")]
    private float sittingCoilRadiusAcrossU = .6f;

    [SerializeField, Range(.1f, .9f)]
    [Tooltip("inner coil size as a share of the outer; 0.1~0.9")]
    private float sittingCoilInnerScale = .35f;

    [SerializeField, Min(.1f)]
    [Tooltip("height each coil layer rises; u")]
    private float sittingCoilLayerRiseU = .36f;

    [SerializeField, Min(0f)]
    [Tooltip("gap between the body skin & the branch; u")]
    private float branchSkinClearanceU = .01f;

    [SerializeField, Min(0f)]
    [Tooltip("how far the head sits forward along the branch; u")]
    private float branchHeadForwardU = .25f;

    [SerializeField, Min(0f)]
    [Tooltip("how far the hood neck sits back from the head; u")]
    private float hoodNeckSetbackU = .24f;

    [SerializeField, Min(.1f)]
    [Tooltip("neck length kept clear of the branch near the hood; u")]
    private float hoodNeckClearanceLengthU = .65f;

    [SerializeField, Min(0f)]
    [Tooltip("clearance the body keeps from the branch as it leaves; u")]
    private float branchEscapeClearanceU = .12f;

    [SerializeField, Min(0f)]
    [Tooltip("how far the head sits to the branch side; u")]
    private float branchHeadSideU = 1.1f;

    [SerializeField, Min(.1f)]
    [Tooltip("how fast the perched head turns to aim; higher is quicker")]
    private float perchedAimSharpness = 7f;

    [SerializeField, Range(.6f, 1f)]
    [Tooltip("thinnest the neck may get, as a share of body thickness; 0.6~1")]
    private float minimumNeckThicknessShare = .85f;

    [SerializeField, Min(0f)]
    [Tooltip("sway of the free neck; u")]
    private float freeNeckSwayU = .12f;

    [SerializeField, Min(.1f)]
    [Tooltip("how far the body drops to settle on the bark; u")]
    private float bodyBarkSettleDropU = 1.2f;

    [SerializeField, Min(.001f)]
    [Tooltip("soft edge where the body meets the bark; u")]
    private float bodyBarkContactFeatherU = .05f;

    [SerializeField, Min(.1f)]
    [Tooltip("time the cobra takes to uncoil from its stand; s")]
    private float standReleaseDurationS = .7f;

    [SerializeField, Min(0f)]
    [Tooltip("how far the head reaches out as it uncoils; u")]
    private float standReleaseReachU = .15f;

    [SerializeField, Min(.1f)]
    [Tooltip("how fast the head eases into a pose; higher is quicker")]
    private float headPoseSharpness = 4f;

    [SerializeField, Min(.1f)]
    [Tooltip("length of the neck collar; u")]
    private float neckCollarLengthU = .75f;

    [SerializeField]
    [Tooltip("attack travel distance b4 the cobra switches itself off; u")]
    private float maxFallDistanceU = 30f;

    [SerializeField, Min(0.1f)]
    [Tooltip("longest an attack lasts before the cobra switches off; s")]
    private float maximumFallDurationS = 2.5f;

    [SerializeField, Min(0.01f)]
    [Tooltip("time the attack takes to reach full speed; s")]
    private float attackSpeedRampS = 0.45f;

    [SerializeField, Min(0.1f)]
    [Tooltip("distance over which the sideways slide brakes; u")]
    private float attackSideBrakeDistanceU = 0.8f;

    [SerializeField, Min(0.1f)]
    [Tooltip("trunk radius the cobra swims along; u")]
    private float treeSurfaceRadiusU = 5f;

    [SerializeField, Min(0f)]
    [Tooltip("gap kept between the body & the trunk; u")]
    private float treeSurfaceClearanceU = 0.02f;

    [SerializeField, Range(0f, 0.12f)]
    [Tooltip("how deep the belly presses into the bark; u")]
    private float bellyPressDepthU = 0.06f;

    [Header("Attack Animation")]
    [SerializeField, Min(.05f)]
    [Tooltip("time of the wind-up before a strike; s")]
    private float strikeWindupDurationS = .42f;

    [SerializeField, Min(0f)]
    [Tooltip("how far the head pulls back in the wind-up; u")]
    private float strikePullbackU = .36f;

    [SerializeField, Min(0f)]
    [Tooltip("how much the body compresses in the wind-up; u")]
    private float strikeCompressionU = .08f;

    [SerializeField, Min(1f)]
    [Tooltip("how fast the strike pose is reached; higher is quicker")]
    private float strikePoseSharpness = 22f;

    [SerializeField, Min(.02f)]
    [Tooltip("time the launch takes to build speed; s")]
    private float strikeLaunchRampS = .09f;

    [SerializeField, Min(.05f)]
    [Tooltip("time of the speed burst of a strike; s")]
    private float strikeBurstDurationS = .38f;

    [SerializeField, Min(1f)]
    [Tooltip("speed of the strike burst, as a multiple of the fall speed")]
    private float strikeSpeedMultiplier = 1.6f;

    [SerializeField, Range(.2f, 1f)]
    [Tooltip("share of the head motion the body follows in a strike; 0.2~1")]
    private float strikeHeadFollowScale = .5f;

    [SerializeField, Min(.01f)]
    [Tooltip("smoothing time of the body trailing the head; s")]
    private float headTrailSmoothTimeS = .16f;

    [SerializeField, Min(.2f)]
    [Tooltip("how far the head drops as it turns; u")]
    private float headTurnDropU = 1.25f;

    [Header("Live head tracking")]
    [SerializeField, Min(1f)]
    [Tooltip("distance within which the head tracks the monkey; u")]
    private float headTrackingRangeU = 10f;

    [SerializeField, Min(0f)]
    [Tooltip("extra distance before head tracking ends; u")]
    private float headTrackingExitPaddingU = .75f;

    [SerializeField, Min(30f)]
    [Tooltip("head turn speed while looking; deg/s")]
    private float headLookTurnSpeedDegPerS = 300f;

    [SerializeField, Range(0f, 35f)]
    [Tooltip("head pitch on a flat surface; deg")]
    private float flatHeadPitchDeg = 16f;

    [SerializeField, Range(0f, 70f)]
    [Tooltip("head pitch when aiming upward; deg")]
    private float upwardHeadPitchDeg = 55f;

    [SerializeField, Range(0f, 45f)]
    [Tooltip("head turn allowed along the surface; deg")]
    private float surfaceHeadTurnDeg = 25f;

    [SerializeField, Min(0f)]
    [Tooltip("gap kept between the head & the bark; u")]
    private float headBarkGapU = .015f;

    [SerializeField, Min(.1f)]
    [Tooltip("radius of the head hit contact; u")]
    private float headContactRadiusU = .42f;

    [SerializeField, Range(8, 40)]
    [Tooltip("hit sections along the body")]
    private int bodyContactSections = 24;

    [SerializeField, Min(.1f)]
    [Tooltip("distance inside which the aim is held steady; u")]
    private float steadyAimDistanceU = 2f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("aim sway kept when close; 0~1")]
    private float closeAimSwayScale = .1f;

    [SerializeField, Min(.1f)]
    [Tooltip("how fast the head steers; higher is quicker")]
    private float headSteeringSharpness = 18f;

    [SerializeField, Range(.2f, 1f)]
    [Tooltip("slowest approach, as a share of the fall speed; 0.2~1")]
    private float minimumApproachFallScale = .45f;

    [SerializeField, Min(1f)]
    [Tooltip("range over which the approach slows; u")]
    private float approachDropRangeU = 3f;

    [SerializeField, Min(.1f)]
    [Tooltip("how strongly the head aims at its target")]
    private float headAimGain = 6f;

    [SerializeField, Min(0f)]
    [Tooltip("damping of the head aim")]
    private float headAimDamping = 1f;

    [SerializeField, Range(45f, 180f)]
    [Tooltip("turn allowed along the tracking surface; deg")]
    private float trackingSurfaceTurnDeg = 160f;

    [SerializeField, Min(.01f)]
    [Tooltip("margin above the target height where the fall stops; u")]
    private float targetHeightStopMarginU = .15f;

    [SerializeField, Min(.1f)]
    [Tooltip("how strongly the fall eases into the target height")]
    private float verticalArrivalGain = 3f;

    [SerializeField, Min(0f)]
    [Tooltip("furthest the head may stretch in a strike; u")]
    private float maximumStrikeReachU = .65f;

    [SerializeField, Min(.1f)]
    [Tooltip("distance at which a strike is reached for; u")]
    private float strikeReachDistanceU = 2f;

    [SerializeField, Min(.05f)]
    [Tooltip("distance over which the support blends out of the coil; u")]
    private float uncoilSupportBlendU = .4f;

    [Header("Eye gaze anchors")]
    [SerializeField]
    [Tooltip("whether the eye anchors below are filled in")]
    private bool eyeAnchorsReady;

    [SerializeField]
    [Tooltip("left eye position on the head surface, local; u")]
    private Vector3 leftEyeSurfaceLocal;

    [SerializeField]
    [Tooltip("right eye position on the head surface, local; u")]
    private Vector3 rightEyeSurfaceLocal;

    [SerializeField]
    [Tooltip("left eye surface normal, local")]
    private Vector3 leftEyeNormalLocal = Vector3.forward;

    [SerializeField]
    [Tooltip("right eye surface normal, local")]
    private Vector3 rightEyeNormalLocal = Vector3.forward;

    [SerializeField, Min(.01f)]
    [Tooltip("radius of an eye white; u")]
    private float eyeWhiteRadiusU = .13f;

    [SerializeField, Min(.01f)]
    [Tooltip("radius of a pupil; u")]
    private float eyePupilRadiusU = .075f;

    [Header("Body")]
    [SerializeField]
    [Tooltip("gap b/t body segments; u")]
    private float segmentSpacingU = 0.35f;

    [SerializeField]
    [Tooltip("hit radius of each segment; u")]
    private float hitRadiusU = 0.5f;

    [Header("Cartoon Appearance")]
    [SerializeField]
    [Tooltip("look of the cartoon head & body")]
    private CartoonCobraVisual.Settings cartoonAppearance = new();

    [Header("Falling Snake Visual")]
    [SerializeField, Min(0.1f)]
    [Tooltip("scale of the snake against its reference size")]
    private float referenceSizeMultiplier = 1.5f;

    [SerializeField]
    [Tooltip("face direction of the reference head, local")]
    private Vector3 referenceFaceDirection =
        new Vector3(-0.435f, -0.265f, -0.861f);

    [SerializeField, Min(0f)]
    [Tooltip("speed of the body wave; Hz")]
    private float bodyWaveSpeedHz = 1f;

    [SerializeField, Min(0.1f)]
    [Tooltip("wave cycles along the body")]
    private float bodyWaveCycles = 0.8f;

    [SerializeField, Min(0.1f)]
    [Tooltip("how fast the swim path turns; higher is quicker")]
    private float swimTurnSharpness = 8f;

    [SerializeField, Min(0.1f)]
    [Tooltip("length of the smooth body; u")]
    private float smoothBodyLengthU = 15.2f;

    [SerializeField, Min(0.01f)]
    [Tooltip("radius of the smooth body; u")]
    private float smoothBodyRadiusU = 0.24f;

    [SerializeField, Range(0f, 55f)]
    [Tooltip("most the body bends while swimming; deg")]
    private float bodyBendDeg = 38f;

    [Header("Head-led Surface Swim")]
    [SerializeField, Min(0f)]
    [Tooltip("side to side head sway; u")]
    private float headSwayAmplitudeU = 0.26f;

    [SerializeField, Min(0.5f)]
    [Tooltip("length of one swim wave; u")]
    private float swimWavelengthU = 3.6f;

    [SerializeField, Min(0.01f)]
    [Tooltip("distance over which the sway fades in; u")]
    private float headSwayEntryDistanceU = 1.8f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("how much the gaze is held steady against sway; 0~1")]
    private float headGazeStabilization = 0.55f;

    [SerializeField, Min(0.1f)]
    [Tooltip("neck length that follows the head; u")]
    private float neckFollowLengthU = 1.25f;

    [SerializeField, Range(0f, 60f)]
    [Tooltip("most the head leans; deg")]
    private float maximumHeadLeanDeg = 50f;

    [SerializeField, Min(0f)]
    [Tooltip("face clearance while rising; u")]
    private float risingFaceClearanceU = .28f;

    [SerializeField, Range(.5f, 1f)]
    [Tooltip("neck thickness at its waist, as a share of the body; 0.5~1")]
    private float neckWaistScale = .74f;

    [SerializeField, Min(.2f)]
    [Tooltip("time of the release curve; s")]
    private float releaseCurveDurationS = 1.15f;

    [SerializeField, Min(.1f)]
    [Tooltip("length over which head contact blends; u")]
    private float headContactBlendLengthU = .45f;

    [SerializeField, Min(.2f)]
    [Tooltip("neck length that is rounded; u")]
    private float roundedNeckLengthU = 1.8f;

    [SerializeField, Range(1, 32)]
    [Tooltip("smoothing passes over the rounded neck")]
    private int roundedNeckPasses = 20;

    [SerializeField, Min(.01f)]
    [Tooltip("largest offset the rounding may add; u")]
    private float roundedNeckMaxOffsetU = .12f;

    // Editor Validation  ######################################################
    private void OnValidate() {
        triggerDistanceU = Mathf.Max(0f, triggerDistanceU);
        warningDurationS = Mathf.Max(0f, warningDurationS);
        maxFallDistanceU = Mathf.Max(0f, maxFallDistanceU);
        segmentSpacingU = Mathf.Max(0f, segmentSpacingU);
        hitRadiusU = Mathf.Max(0.01f, hitRadiusU);
        referenceSizeMultiplier = Mathf.Max(0.1f, referenceSizeMultiplier);
        bodyWaveSpeedHz = Mathf.Max(0f, bodyWaveSpeedHz);
        bodyWaveCycles = Mathf.Max(0.1f, bodyWaveCycles);
    }
}
