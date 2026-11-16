using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Every Inspector-tuned value of a falling cobra, grouped as the
/// Inspector shows them. Read through the properties; the roles
/// never write a tuned value.
/// </summary>
[System.Serializable]
public sealed class FallingCobraTuning {
    // Public Members  #########################################################
    public float TriggerDistanceU => triggerDistanceU;
    public float WarningDurationS => warningDurationS;
    public float AlertExitPaddingU => alertExitPaddingU;
    public AudioClip WarningHiss => warningHiss;
    public float WarningHissVolume => warningHissVolume;
    public float WarningHissIntervalS => warningHissIntervalS;
    public float WarningHissMinDistanceU => warningHissMinDistanceU;
    public float WaitingLookDownDegrees => waitingLookDownDegrees;
    public float WaitingHeadLiftU => waitingHeadLiftU;
    public float StandingFaceHeightU => standingFaceHeightU;
    public float StandingNeckLengthU => standingNeckLengthU;
    public float StandingChestLiftU => standingChestLiftU;
    public float BodyTwistDegreesPerU => bodyTwistDegreesPerU;
    public float MaximumSupportBellyAngle => maximumSupportBellyAngle;
    public float StandingBaseHeightU => standingBaseHeightU;
    public float StandingCoilOuterRadiusU => standingCoilOuterRadiusU;
    public float StandingCoilInnerRadiusU => standingCoilInnerRadiusU;
    public float StandingCoilTurns => standingCoilTurns;
    public float BranchCoilSpanU => branchCoilSpanU;
    public float BranchCoilTurns => branchCoilTurns;
    public float SittingCoilRadiusAlongU => sittingCoilRadiusAlongU;
    public float SittingCoilRadiusAcrossU => sittingCoilRadiusAcrossU;
    public float SittingCoilInnerScale => sittingCoilInnerScale;
    public float SittingCoilLayerRiseU => sittingCoilLayerRiseU;
    public float BranchSkinClearanceU => branchSkinClearanceU;
    public float BranchHeadForwardU => branchHeadForwardU;
    public float HoodNeckSetbackU => hoodNeckSetbackU;
    public float HoodNeckClearanceLengthU => hoodNeckClearanceLengthU;
    public float BranchEscapeClearanceU => branchEscapeClearanceU;
    public float BranchHeadSideU => branchHeadSideU;
    public float PerchedAimSharpness => perchedAimSharpness;
    public float MinimumNeckThicknessShare => minimumNeckThicknessShare;
    public float FreeNeckSwayU => freeNeckSwayU;
    public float BodyBarkSettleDropU => bodyBarkSettleDropU;
    public float BodyBarkContactFeatherU => bodyBarkContactFeatherU;
    public float StandReleaseDurationS => standReleaseDurationS;
    public float StandReleaseReachU => standReleaseReachU;
    public float HeadPoseSharpness => headPoseSharpness;
    public float NeckCollarLengthU => neckCollarLengthU;
    public float MaxFallDistanceU => maxFallDistanceU;
    public float MaximumFallDurationS => maximumFallDurationS;
    public float AttackSpeedRampS => attackSpeedRampS;
    public float AttackSideBrakeDistanceU => attackSideBrakeDistanceU;
    public float TreeSurfaceRadiusU => treeSurfaceRadiusU;
    public float TreeSurfaceClearanceU => treeSurfaceClearanceU;
    public float BellyPressDepthU => bellyPressDepthU;
    public float StrikeWindupDurationS => strikeWindupDurationS;
    public float StrikePullbackU => strikePullbackU;
    public float StrikeCompressionU => strikeCompressionU;
    public float StrikePoseSharpness => strikePoseSharpness;
    public float StrikeLaunchRampS => strikeLaunchRampS;
    public float StrikeBurstDurationS => strikeBurstDurationS;
    public float StrikeSpeedMultiplier => strikeSpeedMultiplier;
    public float StrikeHeadFollowScale => strikeHeadFollowScale;
    public float HeadTrailSmoothTimeS => headTrailSmoothTimeS;
    public float HeadTurnDropU => headTurnDropU;
    public float HeadTrackingRangeU => headTrackingRangeU;
    public float HeadTrackingExitPaddingU => headTrackingExitPaddingU;
    public float HeadLookTurnSpeedDegrees => headLookTurnSpeedDegrees;
    public float FlatHeadPitchDegrees => flatHeadPitchDegrees;
    public float UpwardHeadPitchDegrees => upwardHeadPitchDegrees;
    public float SurfaceHeadTurnDegrees => surfaceHeadTurnDegrees;
    public float HeadBarkGapU => headBarkGapU;
    public float HeadContactRadiusU => headContactRadiusU;
    public int BodyContactSections => bodyContactSections;
    public float SteadyAimDistanceU => steadyAimDistanceU;
    public float CloseAimSwayScale => closeAimSwayScale;
    public float HeadSteeringSharpness => headSteeringSharpness;
    public float MinimumApproachFallScale => minimumApproachFallScale;
    public float ApproachDropRangeU => approachDropRangeU;
    public float HeadAimGain => headAimGain;
    public float HeadAimDamping => headAimDamping;
    public float TrackingSurfaceTurnDegrees => trackingSurfaceTurnDegrees;
    public float TargetHeightStopMarginU => targetHeightStopMarginU;
    public float VerticalArrivalGain => verticalArrivalGain;
    public float MaximumStrikeReachU => maximumStrikeReachU;
    public float StrikeReachDistanceU => strikeReachDistanceU;
    public float UncoilSupportBlendU => uncoilSupportBlendU;
    public bool EyeAnchorsReady => eyeAnchorsReady;
    public Vector3 LeftEyeSurfaceLocal => leftEyeSurfaceLocal;
    public Vector3 RightEyeSurfaceLocal => rightEyeSurfaceLocal;
    public Vector3 LeftEyeNormalLocal => leftEyeNormalLocal;
    public Vector3 RightEyeNormalLocal => rightEyeNormalLocal;
    public float EyeWhiteRadiusU => eyeWhiteRadiusU;
    public float EyePupilRadiusU => eyePupilRadiusU;
    public float SegmentSpacingU => segmentSpacingU;
    public float HitRadiusU => hitRadiusU;
    public CartoonCobraVisual.Settings CartoonAppearance => cartoonAppearance;
    public float ReferenceSizeMultiplier => referenceSizeMultiplier;
    public Vector3 ReferenceFaceDirection => referenceFaceDirection;
    public float BodyWaveSpeedHz => bodyWaveSpeedHz;
    public float BodyWaveCycles => bodyWaveCycles;
    public float SwimTurnSharpness => swimTurnSharpness;
    public float SmoothBodyLengthU => smoothBodyLengthU;
    public float SmoothBodyRadiusU => smoothBodyRadiusU;
    public float BodyBendDegrees => bodyBendDegrees;
    public float HeadSwayAmplitudeU => headSwayAmplitudeU;
    public float SwimWavelengthU => swimWavelengthU;
    public float HeadSwayEntryDistanceU => headSwayEntryDistanceU;
    public float HeadGazeStabilization => headGazeStabilization;
    public float NeckFollowLengthU => neckFollowLengthU;
    public float MaximumHeadLeanDegrees => maximumHeadLeanDegrees;
    public float RisingFaceClearanceU => risingFaceClearanceU;
    public float NeckWaistScale => neckWaistScale;
    public float ReleaseCurveDurationS => releaseCurveDurationS;
    public float HeadContactBlendLengthU => headContactBlendLengthU;
    public float RoundedNeckLengthU => roundedNeckLengthU;
    public int RoundedNeckPasses => roundedNeckPasses;
    public float RoundedNeckMaxOffsetU => roundedNeckMaxOffsetU;

    // Public Methods  #########################################################
    /// <summary>Clamps the values the Inspector can push out of
    /// range.</summary>
    public void Validate() {
        triggerDistanceU = Mathf.Max(0f, triggerDistanceU);
        warningDurationS = Mathf.Max(0f, warningDurationS);
        maxFallDistanceU = Mathf.Max(0f, maxFallDistanceU);
        segmentSpacingU = Mathf.Max(0f, segmentSpacingU);
        hitRadiusU = Mathf.Max(0.01f, hitRadiusU);
        referenceSizeMultiplier = Mathf.Max(0.1f, referenceSizeMultiplier);
        bodyWaveSpeedHz = Mathf.Max(0f, bodyWaveSpeedHz);
        bodyWaveCycles = Mathf.Max(0.1f, bodyWaveCycles);
    }

    // Inspector Fields  #######################################################
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
    private float alertExitPaddingU = 3f;
    [Header("Watch Hiss")]
    [SerializeField]
    private AudioClip warningHiss;
    [SerializeField, Range(0f, 1f)]
    private float warningHissVolume = .3f;
    [SerializeField, Min(.1f)]
    private float warningHissIntervalS = 2.4f;
    [SerializeField, Min(.1f)]
    private float warningHissMinDistanceU = 3f;
    [Header("Watch Head Pose")]
    [SerializeField, Range(0f, 70f)]
    private float waitingLookDownDegrees = 35f;
    [SerializeField, Min(0f)]
    private float waitingHeadLiftU = .7f;
    [SerializeField, Min(.1f)]
    private float standingFaceHeightU = 2.2f;
    [SerializeField, Min(.1f)]
    private float standingNeckLengthU = 1.8f;
    [SerializeField, Min(0f)]
    private float standingChestLiftU = .4f;
    [SerializeField, Min(1f)]
    private float bodyTwistDegreesPerU = 480f;
    [SerializeField, Range(0f, 89f)]
    private float maximumSupportBellyAngle = 75f;
    [SerializeField, Min(.1f)]
    private float standingBaseHeightU = 1f;
    [SerializeField, Min(.1f)]
    private float standingCoilOuterRadiusU = .8f;
    [SerializeField, Min(.05f)]
    private float standingCoilInnerRadiusU = .35f;
    [SerializeField, Min(.5f)]
    private float standingCoilTurns = 1.1f;
    [Header("Branch Perch")]
    [SerializeField, Min(.1f)]
    private float branchCoilSpanU = 1.8f;
    [SerializeField, Min(.5f)]
    private float branchCoilTurns = 3f;
    [SerializeField, Min(.1f)]
    private float sittingCoilRadiusAlongU = 1.05f;
    [SerializeField, Min(.1f)]
    private float sittingCoilRadiusAcrossU = .6f;
    [SerializeField, Range(.1f, .9f)]
    private float sittingCoilInnerScale = .35f;
    [SerializeField, Min(.1f)]
    private float sittingCoilLayerRiseU = .36f;
    [SerializeField, Min(0f)]
    private float branchSkinClearanceU = .01f;
    [SerializeField, Min(0f)]
    private float branchHeadForwardU = .25f;
    [SerializeField, Min(0f)]
    private float hoodNeckSetbackU = .24f;
    [SerializeField, Min(.1f)]
    private float hoodNeckClearanceLengthU = .65f;
    [SerializeField, Min(0f)]
    private float branchEscapeClearanceU = .12f;
    [SerializeField, Min(0f)]
    private float branchHeadSideU = 1.1f;
    [SerializeField, Min(.1f)]
    private float perchedAimSharpness = 7f;
    [SerializeField, Range(.6f, 1f)]
    private float minimumNeckThicknessShare = .85f;
    [SerializeField, Min(0f)]
    private float freeNeckSwayU = .12f;
    [SerializeField, Min(.1f)]
    private float bodyBarkSettleDropU = 1.2f;
    [SerializeField, Min(.001f)]
    private float bodyBarkContactFeatherU = .05f;
    [SerializeField, Min(.1f)]
    private float standReleaseDurationS = .7f;
    [SerializeField, Min(0f)]
    private float standReleaseReachU = .15f;
    [SerializeField, Min(.1f)]
    private float headPoseSharpness = 4f;
    [SerializeField, Min(.1f)]
    private float neckCollarLengthU = .75f;
    [SerializeField]
    [Tooltip("attack travel distance b4 the cobra switches itself off; u")]
    private float maxFallDistanceU = 30f;
    [SerializeField, Min(0.1f)]
    private float maximumFallDurationS = 2.5f;
    [SerializeField, Min(0.01f)]
    private float attackSpeedRampS = 0.45f;
    [SerializeField, Min(0.1f)]
    private float attackSideBrakeDistanceU = 0.8f;
    [SerializeField, Min(0.1f)]
    private float treeSurfaceRadiusU = 5f;
    [SerializeField, Min(0f)]
    private float treeSurfaceClearanceU = 0.02f;
    [SerializeField, Range(0f, 0.12f)]
    private float bellyPressDepthU = 0.06f;
    [Header("Attack Animation")]
    [SerializeField, Min(.05f)]
    private float strikeWindupDurationS = .42f;
    [SerializeField, Min(0f)]
    private float strikePullbackU = .36f;
    [SerializeField, Min(0f)]
    private float strikeCompressionU = .08f;
    [SerializeField, Min(1f)]
    private float strikePoseSharpness = 22f;
    [SerializeField, Min(.02f)]
    private float strikeLaunchRampS = .09f;
    [SerializeField, Min(.05f)]
    private float strikeBurstDurationS = .38f;
    [SerializeField, Min(1f)]
    private float strikeSpeedMultiplier = 1.6f;
    [SerializeField, Range(.2f, 1f)]
    private float strikeHeadFollowScale = .5f;
    [SerializeField, Min(.01f)]
    private float headTrailSmoothTimeS = .16f;
    [SerializeField, Min(.2f)]
    private float headTurnDropU = 1.25f;
    [Header("Live head tracking")]
    [SerializeField, Min(1f)]
    private float headTrackingRangeU = 10f;
    [SerializeField, Min(0f)]
    private float headTrackingExitPaddingU = .75f;
    [SerializeField, Min(30f)]
    private float headLookTurnSpeedDegrees = 300f;
    [SerializeField, Range(0f, 35f)]
    private float flatHeadPitchDegrees = 16f;
    [SerializeField, Range(0f, 70f)]
    private float upwardHeadPitchDegrees = 55f;
    [SerializeField, Range(0f, 45f)]
    private float surfaceHeadTurnDegrees = 25f;
    [SerializeField, Min(0f)]
    private float headBarkGapU = .015f;
    [SerializeField, Min(.1f)]
    private float headContactRadiusU = .42f;
    [SerializeField, Range(8, 40)]
    private int bodyContactSections = 24;
    [SerializeField, Min(.1f)]
    private float steadyAimDistanceU = 2f;
    [SerializeField, Range(0f, 1f)]
    private float closeAimSwayScale = .1f;
    [SerializeField, Min(.1f)]
    private float headSteeringSharpness = 18f;
    [SerializeField, Range(.2f, 1f)]
    private float minimumApproachFallScale = .45f;
    [SerializeField, Min(1f)]
    private float approachDropRangeU = 3f;
    [SerializeField, Min(.1f)]
    private float headAimGain = 6f;
    [SerializeField, Min(0f)]
    private float headAimDamping = 1f;
    [SerializeField, Range(45f, 180f)]
    private float trackingSurfaceTurnDegrees = 160f;
    [SerializeField, Min(.01f)]
    private float targetHeightStopMarginU = .15f;
    [SerializeField, Min(.1f)]
    private float verticalArrivalGain = 3f;
    [SerializeField, Min(0f)]
    private float maximumStrikeReachU = .65f;
    [SerializeField, Min(.1f)]
    private float strikeReachDistanceU = 2f;
    [SerializeField, Min(.05f)]
    private float uncoilSupportBlendU = .4f;
    [Header("Eye gaze anchors")]
    [SerializeField]
    private bool eyeAnchorsReady;
    [SerializeField]
    private Vector3 leftEyeSurfaceLocal;
    [SerializeField]
    private Vector3 rightEyeSurfaceLocal;
    [SerializeField]
    private Vector3 leftEyeNormalLocal = Vector3.forward;
    [SerializeField]
    private Vector3 rightEyeNormalLocal = Vector3.forward;
    [SerializeField, Min(.01f)]
    private float eyeWhiteRadiusU = .13f;
    [SerializeField, Min(.01f)]
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
    private CartoonCobraVisual.Settings cartoonAppearance = new();
    [Header("Falling Snake Visual")]
    [SerializeField, Min(0.1f)]
    private float referenceSizeMultiplier = 1.5f;
    [SerializeField]
    private Vector3 referenceFaceDirection =
        new Vector3(-0.435f, -0.265f, -0.861f);
    [SerializeField, Min(0f)]
    private float bodyWaveSpeedHz = 1f;
    [SerializeField, Min(0.1f)]
    private float bodyWaveCycles = 0.8f;
    [SerializeField, Min(0.1f)]
    private float swimTurnSharpness = 8f;
    [SerializeField, Min(0.1f)]
    private float smoothBodyLengthU = 15.2f;
    [SerializeField, Min(0.01f)]
    private float smoothBodyRadiusU = 0.24f;
    [SerializeField, Range(0f, 55f)]
    private float bodyBendDegrees = 38f;
    [Header("Head-led Surface Swim")]
    [SerializeField, Min(0f)]
    private float headSwayAmplitudeU = 0.26f;
    [SerializeField, Min(0.5f)]
    private float swimWavelengthU = 3.6f;
    [SerializeField, Min(0.01f)]
    private float headSwayEntryDistanceU = 1.8f;
    [SerializeField, Range(0f, 1f)]
    private float headGazeStabilization = 0.55f;
    [SerializeField, Min(0.1f)]
    private float neckFollowLengthU = 1.25f;
    [SerializeField, Range(0f, 60f)]
    private float maximumHeadLeanDegrees = 50f;
    [SerializeField, Min(0f)]
    private float risingFaceClearanceU = .28f;
    [SerializeField, Range(.5f, 1f)]
    private float neckWaistScale = .74f;
    [SerializeField, Min(.2f)]
    private float releaseCurveDurationS = 1.15f;
    [SerializeField, Min(.1f)]
    private float headContactBlendLengthU = .45f;
    [SerializeField, Min(.2f)]
    private float roundedNeckLengthU = 1.8f;
    [SerializeField, Range(1, 32)]
    private int roundedNeckPasses = 20;
    [SerializeField, Min(.01f)]
    private float roundedNeckMaxOffsetU = .12f;
}
