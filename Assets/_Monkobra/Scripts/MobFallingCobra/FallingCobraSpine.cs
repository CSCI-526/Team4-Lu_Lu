using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using static FallingCobraRig;
using static FallingCobraMath;

/// <summary>Builds the spine and the tube mesh of a falling cobra each frame,
/// in named steps that hand their results forward through one reused frame
/// state.</summary>
public sealed class FallingCobraSpine {
    // private members  ########################################################
    private readonly FallingCobraRig rig;
    private readonly FallingCobraConfig config;
    private sealed class FrameState {
        public bool following;
        public float poseDelta;
        public float phase;
        public Matrix4x4 toWorld;
        public Matrix4x4 toLocal;
        public Vector3 rootOut;
        public Vector3 rootSide;
        public Quaternion headRotation;
        public Vector3 headCenter;
        public float risingClearance;
        public float departure;
        public float raisedLength;
        public float curveLength;
        public int endRing;
        public bool continuousFreeNeck;
        public float curveRingSpan;
        public float freeSpanDistance;
        public Vector3 continuousAnchor;
        public Vector3 continuousAnchorDirection;
        public Vector3 curveEnd;
        public Vector3 curveEndDirection;
        public Vector3 headBelly;
    }
    private readonly FrameState ctx = new();

    // cosine of the 2 degree bend below which a ring is left straight
    private const float BEND_FLOOR_COS = .99939083f;

    // profiler markers, one per pipeline step  --------------------------------
    private static readonly ProfilerMarker SPINE_MARKER =
        new("FallingCobra.Spine");
    private static readonly ProfilerMarker BLENDS_MARKER =
        new("FallingCobra.UpdateBlends");
    private static readonly ProfilerMarker BODY_PATH_MARKER =
        new("FallingCobra.BuildBodyPath");
    private static readonly ProfilerMarker HEAD_POSE_MARKER =
        new("FallingCobra.ChooseHeadPose");
    private static readonly ProfilerMarker NECK_CURVE_MARKER =
        new("FallingCobra.FitNeckCurve");
    private static readonly ProfilerMarker NECK_SPINE_MARKER =
        new("FallingCobra.PlaceNeckSpine");
    private static readonly ProfilerMarker TUBE_MESH_MARKER =
        new("FallingCobra.BuildTubeMesh");
    private static readonly ProfilerMarker PUBLISH_MARKER =
        new("FallingCobra.PublishMesh");
    private static readonly ProfilerMarker FAIR_MARKER =
        new("FallingCobra.FairSpineCorners");
    private static readonly ProfilerMarker ROUND_NECK_MARKER =
        new("FallingCobra.RoundNeckSurface");


    public FallingCobraSpine(FallingCobraRig rig) {
        this.rig = rig;
        config = rig.config;
    }

    // Public Methods  #########################################################
    /// <summary>Poses the whole snake for this frame.</summary>
    public void Update() {
        using var _ = SPINE_MARKER.Auto();
        UpdateBlends();
        BuildBodyPath();
        ChooseHeadPose();
        FitNeckCurve();
        PlaceNeckSpine();
        BuildTubeMesh();
        PublishMesh();
    }

    // Pipeline Steps  ########################################################
    private void UpdateBlends() {
        using var _ = BLENDS_MARKER.Auto();
        ctx.following = rig.state == FallingCobraState.Falling;
        ctx.poseDelta = Mathf.Max(0f, Time.time - rig.lastHeadPoseTime);
        rig.lastHeadPoseTime = Time.time;
        float gain = 1f - Mathf.Exp(-config.HeadPoseSharpness * ctx.poseDelta);
        rig.waitingHeadBlend =
            Mathf.Lerp(rig.waitingHeadBlend,
                rig.state == FallingCobraState.Warning ? 1f : 0f, gain);
        float windup = rig.state == FallingCobraState.Warning
            && rig.monkeyInHeadRange
            ?Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(rig.fallStartTime
                    - config.StrikeWindupDurationS, rig.fallStartTime,
                        Time.time)):0f;
        rig.strikeWindupBlend =
            Mathf.Lerp(rig.strikeWindupBlend, windup,
                1f - Mathf.Exp(-config.StrikePoseSharpness * ctx.poseDelta));
        if (!ctx.following && rig.monkeyInHeadRange) {
            rig.BranchCoil.UpdatePerchedAim(ctx.poseDelta);
        }
        if (ctx.following) {
            rig.Trail.AdvanceSwimTrail();
        }
    }
    private void BuildBodyPath() {
        using var _ = BODY_PATH_MARKER.Auto();
        ctx.phase =
            Time.time * config.BodyWaveSpeedHz * .3f * Mathf.PI * 2f
                + rig.swimPhaseOffset;
        ctx.toWorld = rig.referenceVisual.localToWorldMatrix;
        ctx.toLocal = rig.referenceVisual.worldToLocalMatrix;
        ctx.rootOut =
            new Vector3(rig.body.position.x - rig.axisXZ.x, 0f,
                rig.body.position.z - rig.axisXZ.y).normalized;
        ctx.rootSide = Vector3.Cross(Vector3.up, ctx.rootOut);

        int cursor = ctx.following ? rig.swimTrail.Count - 1 : 0;
        Vector3 idle = Vector3.zero;
        for (int ring = 0; ring < BodyRings; ring++) {
            float along = ring / (float)(BodyRings - 1);
            float taper =
                1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(.55f, 1f, along));
            float neckAlong = along * config.SmoothBodyLengthU;
            float neckWaist =
                Mathf.Lerp(config.NeckWaistScale, 1f,
                    Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(.35f, 1.25f, neckAlong)));
            rig.spineRadii[ring] =
                Mathf.Max(.001f, config.SmoothBodyRadiusU * taper * neckWaist);
            Vector3 contact;
            if (ctx.following) {
                contact =
                    rig.Trail.SampleSwimTrail(rig.BodyTrailDistance(ring),
                        ref cursor);
            }
            else {
                float angle =
                    Mathf.Sin(ctx.phase - along * config.BodyWaveCycles
                        * Mathf.PI
                        * 2f) * config.BodyBendDeg * .3f * Mathf.Deg2Rad
                    * Mathf.SmoothStep(0f, 1f, along / .3f);
                if (ring > 0) {
                    idle += new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f)
                        * (config.SmoothBodyLengthU / (BodyRings - 1));
                }
                contact =
                    rig.GetTreeSurfacePoint(ctx.toWorld.MultiplyPoint3x4(idle));
            }
            Vector3 outward =
                new Vector3(contact.x - rig.axisXZ.x, 0f,
                    contact.z - rig.axisXZ.y).normalized;
            rig.spineCenters[ring] =
                contact + outward * rig.GroundSpineOffset(ring);
        }

    }
    private void ChooseHeadPose() {
        using var _ = HEAD_POSE_MARKER.Auto();
        Vector3 crawlHead = rig.spineCenters[0];
        Vector3 headOut =
            new Vector3(crawlHead.x - rig.axisXZ.x, 0f,
                crawlHead.z - rig.axisXZ.y).normalized;
        Vector3 crawlTail = rig.host.up;
        if (ctx.following) {
            int sample = rig.swimTrail.Count - 1;
            Vector3 contact = rig.Trail.SampleSwimTrail(0f, ref sample);
            sample = rig.swimTrail.Count - 1;
            Vector3 actual =
                Vector3.ProjectOnPlane(rig.Trail.SampleSwimTrail(.06f,
                    ref sample)
                    - contact, headOut).normalized;
            Vector3 nominal =
                Vector3.ProjectOnPlane(-rig.trailHeading, headOut).normalized;
            crawlTail =
                Vector3.Slerp(actual, nominal,
                    config.HeadGazeStabilization).normalized;
        }
        if (crawlTail.sqrMagnitude < .00001f) {
            crawlTail = rig.host.up;
        }
        Quaternion crawlRotation = Quaternion.LookRotation(-headOut, crawlTail);
        if (ctx.following) {
            crawlRotation = rig.Tracking.DirectedAttackRotation(crawlHead);
        }
        if (rig.state != FallingCobraState.Falling || rig.monkeyInHeadRange) {
            Vector3 watchPoint = ctx.following?rig.ThreatPosition
                :rig.supportBranch != null
                    ?rig.BranchCoil.BranchCoilPoint(0f,
                        config.SmoothBodyRadiusU)
                        + Vector3.up
                            * (config.StandingFaceHeightU
                                - config.StandingBaseHeightU)
                +ctx.rootOut * config.BranchHeadForwardU + rig.branchSide
                    * (config.BranchHeadSideU
                        * rig.perchedAimSide):rig.ThreatPosition;
            if (!ctx.following) {
                watchPoint += rig.strikeWindupOffset;
            }
            Vector3 watchDelta = rig.Tracking.MonkeyAimPosition() - watchPoint;
            Vector3 direction = rig.monkeyInHeadRange
                && watchDelta.sqrMagnitude > .00001f
                ? watchDelta.normalized
                    : (ctx.rootOut + Vector3.down
                        * Mathf.Tan(config.WaitingLookDownDeg
                            * Mathf.Deg2Rad)).normalized;
            Vector3 faceUp =
                Vector3.ProjectOnPlane(Vector3.up, direction).normalized;
            if (faceUp.sqrMagnitude < .00001f) {
                faceUp = ctx.rootOut;
            }
            Quaternion face = Quaternion.LookRotation(direction, faceUp);
            rig.standingHeadRotation =
                face * Quaternion.Inverse(rig.SourceEyeBasis());
        }
        Quaternion restRotation =
            Quaternion.LookRotation(ctx.rootOut * .5f + Vector3.down
                * .8660254f,
                Vector3.up) * Quaternion.Inverse(rig.SourceEyeBasis());
        float liftBlend =
            1f - (1f - rig.waitingHeadBlend) * (1f - rig.waitingHeadBlend);
        float turnBlend =
            Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(.15f, .85f, rig.waitingHeadBlend));
        ctx.risingClearance =
            4f * rig.waitingHeadBlend * (1f - rig.waitingHeadBlend)
                * config.RisingFaceClearanceU;
        ctx.departure = ctx.following?rig.HeadDeparture(crawlHead):0f;
        ctx.headRotation = ctx.following
            ? Quaternion.Slerp(rig.standingHeadRotation, crawlRotation,
                ctx.departure)
            : Quaternion.Slerp(restRotation, rig.standingHeadRotation,
                turnBlend);
        Vector3 desiredFace =
            new Vector3(rig.axisXZ.x,
                rig.body.position.y + config.StandingFaceHeightU, rig.axisXZ.y)
            + ctx.rootOut
                * (config.TreeSurfaceRadiusU + config.TreeSurfaceClearanceU
                    + config.WaitingHeadLiftU);
        if (rig.supportBranch != null) desiredFace =
            rig.BranchCoil.BranchCoilPoint(0f, config.SmoothBodyRadiusU)
            + Vector3.up
                * (config.StandingFaceHeightU - config.StandingBaseHeightU)
                    + ctx.rootOut * config.BranchHeadForwardU + rig.branchSide
                        * (config.BranchHeadSideU * rig.perchedAimSide);
        if (!ctx.following) {
            Vector3 toward = rig.Tracking.MonkeyAimPosition() - desiredFace;
            if (toward.sqrMagnitude<.00001f) {
                toward = ctx.rootOut;
            }
            rig.strikeWindupOffset =
                (-toward.normalized * config.StrikePullbackU - Vector3.up
                    * config.StrikeCompressionU) * rig.strikeWindupBlend;
            desiredFace += rig.strikeWindupOffset;
        }
        Vector3 restFace = rig.supportBranch != null
            ? rig.BranchCoil.RestingBranchFace(restRotation)
                : crawlHead + restRotation * rig.originalHeadFaceCenter;
        Vector3 restHead = restFace - restRotation * rig.originalHeadFaceCenter;
        Vector3 standHead =
            desiredFace - rig.standingHeadRotation * rig.originalHeadFaceCenter;
        Vector3 raisedFace =
            Vector3.Lerp(restFace, desiredFace, liftBlend) + ctx.rootOut
                * ctx.risingClearance;
        ctx.headCenter = ctx.following ? crawlHead
            : raisedFace - ctx.headRotation * rig.originalHeadFaceCenter;

        if (rig.headRotationReady) {
            ctx.headRotation =
                Quaternion.RotateTowards(rig.shownHeadRotation,
                    ctx.headRotation,
                    config.HeadLookTurnSpeedDegPerS * ctx.poseDelta);
        }
        rig.shownHeadRotation = ctx.headRotation;
        rig.headRotationReady = true;
        ctx.headCenter =
            rig.Tracking.FitHeadToBark(ctx.headCenter, ctx.headRotation);
        ctx.headCenter =
            rig.BranchCoil.KeepHeadClearOfBranch(ctx.headCenter,
                ctx.headRotation);

    }
    private void FitNeckCurve() {
        using var _ = NECK_CURVE_MARKER.Auto();
        ctx.raisedLength =
            Mathf.Min(config.StandingNeckLengthU,
                config.SmoothBodyLengthU * .7f);
        ctx.curveLength = ctx.following
            ? Mathf.Lerp(ctx.raisedLength, config.NeckFollowLengthU,
                ctx.departure)
                + .8f * Mathf.Sin(Mathf.PI * ctx.departure) : ctx.raisedLength;
        ctx.endRing =
            Mathf.Clamp(Mathf.RoundToInt(ctx.curveLength
                / config.SmoothBodyLengthU
                * (BodyRings - 1)), 1, BodyRings - 2);
        ctx.continuousFreeNeck = false;
        ctx.curveRingSpan = ctx.endRing;
        ctx.freeSpanDistance = 0f;
        ctx.continuousAnchor = Vector3.zero;
        ctx.continuousAnchorDirection = Vector3.zero;
        if (ctx.following) {
            rig.Trail.SettleRemainingCoil();
            int anchor = 1;
            while (anchor<BodyRings - 2
                && rig.RemainingCoilWeight(anchor)<.98f) {
                anchor++;
            }
            if (anchor<BodyRings - 2 && anchor >= 4) {
                int lower = anchor - 1;
                float transition = .5f - Mathf.Sin(Mathf.Asin(.96f) / 3f);
                float boundary =
                    rig.launchTrailDistance - rig.launchNeckArc
                        + Mathf.Lerp(-config.UncoilSupportBlendU,
                            config.UncoilSupportBlendU, transition);
                ctx.freeSpanDistance =
                    rig.swimTrail[rig.swimTrail.Count - 1].distance - boundary;
                float lowDistance = rig.BodyTrailDistance(lower),
                    highDistance = rig.BodyTrailDistance(anchor);
                float fraction =
                    Mathf.InverseLerp(lowDistance, highDistance,
                        ctx.freeSpanDistance);
                float span = Mathf.Max(.0001f, highDistance - lowDistance);
                ctx.curveRingSpan = lower + fraction;
                ctx.endRing = anchor;
                ctx.continuousFreeNeck = true;
                Vector3 first =
                    (rig.spineCenters[anchor] - rig.spineCenters[lower - 1])
                        * (span / Mathf.Max(.0001f,
                            highDistance - rig.BodyTrailDistance(lower - 1)));
                Vector3 last =
                    (rig.spineCenters[anchor + 1] - rig.spineCenters[lower])
                        * (span / Mathf.Max(.0001f,
                            rig.BodyTrailDistance(anchor + 1) - lowDistance));
                ctx.continuousAnchor =
                    SpineCurve(rig.spineCenters[lower],
                        rig.spineCenters[anchor], first, last, fraction);
                ctx.continuousAnchorDirection =
                    SpineCurveDirection(rig.spineCenters[lower],
                        rig.spineCenters[anchor], first, last, fraction);
            }
        }
        ctx.curveLength = ctx.continuousFreeNeck?ctx.freeSpanDistance
            :ctx.curveRingSpan * config.SmoothBodyLengthU / (BodyRings - 1);
        ctx.curveEnd = ctx.continuousFreeNeck?ctx.continuousAnchor
            :rig.spineCenters[ctx.endRing];
        ctx.curveEndDirection = ctx.continuousFreeNeck
            ?ctx.continuousAnchorDirection
            :(rig.spineCenters[ctx.endRing + 1]
                - rig.spineCenters[ctx.endRing - 1]).normalized;
        if (!ctx.following) {
            Vector3 standBaseContact =
                rig.BranchCoil.StandingTailPoint(0f, ctx.rootOut, ctx.rootSide);
            Vector3 standBase = rig.supportBranch != null
                ? rig.BranchCoil.BranchCoilPoint(0f,
                    rig.spineRadii[ctx.endRing])
                    : standBaseContact + ctx.rootOut
                        * rig.GroundSpineOffset(ctx.endRing);
            ctx.curveEnd = standBase;
            ctx.curveEndDirection = rig.supportBranch != null
                ? (rig.BranchCoil.BranchCoilPoint(.02f,
                    rig.spineRadii[ctx.endRing])
                    - standBase).normalized
                : (rig.BranchCoil.StandingTailPoint(.02f, ctx.rootOut,
                    ctx.rootSide)
                    - standBaseContact).normalized;
            for (int ring = ctx.endRing; ring < BodyRings; ring++) {
                float behind =
                    (ring - ctx.endRing) * config.SmoothBodyLengthU
                        / (BodyRings - 1);
                Vector3 contact =
                    rig.BranchCoil.StandingTailPoint(behind, ctx.rootOut,
                        ctx.rootSide);
                Vector3 outward =
                    new Vector3(contact.x - rig.axisXZ.x, 0f,
                        contact.z - rig.axisXZ.y).normalized;
                Vector3 stand = rig.supportBranch != null
                    ? rig.BranchCoil.BranchCoilPoint(behind,
                        rig.spineRadii[ring])
                        : contact + outward * rig.GroundSpineOffset(ring);
                rig.spineCenters[ring] = stand;
            }
        }
    }
    private void PlaceNeckSpine() {
        using var _ = NECK_SPINE_MARKER.Auto();
        float handle = ctx.continuousFreeNeck
            ?Vector3.Distance(ctx.headCenter, ctx.curveEnd) * .9f
            :Mathf.Max(Vector3.Distance(ctx.headCenter, ctx.curveEnd) * .75f,
                ctx.curveLength * .65f);
        Vector3 startTangent = ctx.headRotation * Vector3.up * handle;
        Vector3 endTangent = ctx.curveEndDirection * handle;
        for (int ring = 0; ring < ctx.endRing; ring++) {
            float t = ctx.continuousFreeNeck
                ?Mathf.Clamp01(rig.BodyTrailDistance(ring)
                    / Mathf.Max(.001f, ctx.freeSpanDistance)):Mathf.Clamp01(ring
                        / ctx.curveRingSpan);
            rig.spineCenters[ring] =
                SpineCurve(ctx.headCenter, ctx.curveEnd, startTangent,
                    endTangent, t);
            if (ctx.continuousFreeNeck && ring>0) {
                Vector3 point = rig.spineCenters[ring],
                    radial = new Vector3(point.x - rig.axisXZ.x, 0f,
                        point.z - rig.axisXZ.y);
                float rawRadius = radial.magnitude;
                Vector3 outwards = rawRadius>.0001f?radial / rawRadius
                    :ctx.rootOut;
                float barkRadius =
                    config.TreeSurfaceRadiusU + config.TreeSurfaceClearanceU
                        + rig.spineRadii[ring] * .9f - config.BellyPressDepthU;
                float belowWood =
                    Vector3.Dot(rig.branchCenter + rig.branchUp
                        * rig.branchHalfHeight - ctx.headCenter, rig.branchUp);
                float contact =
                    SmoothVisual01(t / .2f)
                        * (1f - SmoothVisual01(Mathf.InverseLerp(.6f, 1f, t)))
                            * SmoothVisual01(belowWood
                                / config.BodyBarkSettleDropU);
                float radius = Mathf.Lerp(rawRadius, barkRadius, contact);
                float gap = radius - barkRadius;
                float softRadius =
                    barkRadius
                        + (gap + Mathf.Sqrt(gap * gap
                            + config.BodyBarkContactFeatherU
                                * config.BodyBarkContactFeatherU)) * .5f;
                radius = Mathf.Lerp(radius, softRadius,
                    SmoothVisual01(t / .05f));
                Vector3 side = Vector3.Cross(Vector3.up, outwards);
                point = new Vector3(rig.axisXZ.x, point.y, rig.axisXZ.y)
                    + outwards * radius;
                point += side
                    * (config.FreeNeckSwayU
                        * Mathf.Sin(ctx.phase + t * Mathf.PI * 2f)
                            * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 4f));
                rig.spineCenters[ring] = point;
            }
            if (!ctx.following) {
                float arch = Mathf.Sin(Mathf.PI * t);
                rig.spineCenters[ring] +=
                    ctx.rootOut
                        * (config.StandingChestLiftU * rig.waitingHeadBlend
                        * arch * arch);
                float nearHood =
                    Mathf.Sin(Mathf.PI
                        * Mathf.Clamp01(ring * config.SmoothBodyLengthU
                            / (BodyRings - 1)
                                / config.HoodNeckClearanceLengthU));
                rig.spineCenters[ring] -=
                    ctx.rootOut
                        * (config.HoodNeckSetbackU * rig.waitingHeadBlend
                        * nearHood * nearHood);
                Vector3 faceForward =
                    ctx.headRotation * rig.originalHeadFaceBasis
                        * Vector3.forward;
                rig.spineCenters[ring] -=
                    faceForward * (ctx.risingClearance * nearHood * nearHood);
            }
        }
        for (int ring = 1; ring < BodyRings; ring++) {
            float behind = ring * config.SmoothBodyLengthU / (BodyRings - 1);
            float radius =
                Mathf.Lerp(rig.headNeckRadiusU, rig.spineRadii[ring],
                    Mathf.SmoothStep(0f, 1f, behind / .4f));
            rig.spineCenters[ring] =
                rig.BranchCoil.KeepClearOfBranch(rig.spineCenters[ring],
                    radius);
        }
        if (ctx.following) {
            rig.Trail.SettleRemainingCoil();
            if (!ctx.continuousFreeNeck) {
                RelaxOriginalRaisedNeck();
            }
        }
        FairSpineCorners(ctx.following, ctx.endRing);
        rig.spineCenters[0] = ctx.headCenter;
        rig.referenceHeadVisual.SetPositionAndRotation(ctx.headCenter,
            ctx.headRotation);

    }
    private void BuildTubeMesh() {
        using var _ = TUBE_MESH_MARKER.Auto();
        float collarLoftU = config.NeckCollarLengthU;
        Vector3 headOutward =
            new Vector3(ctx.headCenter.x - rig.axisXZ.x, 0f,
                ctx.headCenter.z - rig.axisXZ.y).normalized;
        ctx.headBelly = rig.BranchCoil.SupportedPaintBelly(headOutward, 0f);
        Quaternion frame = ctx.headRotation;
        float surface =
            config.TreeSurfaceRadiusU + config.TreeSurfaceClearanceU
                - config.BellyPressDepthU;
        for (int ring = 0; ring < BodyRings; ring++) {
            float behind = ring * config.SmoothBodyLengthU / (BodyRings - 1);
            Vector3 tangent = ring == 0 ? ctx.headRotation * Vector3.up
                : (rig.spineCenters[Mathf.Min(BodyRings - 1, ring + 1)]
                    - rig.spineCenters[ring - 1]).normalized;
            if (tangent.sqrMagnitude < .00001f) {
                tangent = frame * Vector3.up;
            }
            rig.spineTangents[ring] = tangent;
            Vector3 outward =
                new Vector3(rig.spineCenters[ring].x - rig.axisXZ.x, 0f,
                    rig.spineCenters[ring].z - rig.axisXZ.y).normalized;
            float centreRadius =
                new Vector2(rig.spineCenters[ring].x - rig.axisXZ.x,
                    rig.spineCenters[ring].z - rig.axisXZ.y).magnitude;
            float lift = centreRadius - (surface + rig.spineRadii[ring] * .9f);
            rig.spineContact[ring] =
                1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(.02f, .15f, lift));
            if (ring > 0) {
                Vector3 transported =
                    Vector3.ProjectOnPlane(frame * Vector3.forward,
                        tangent).normalized;
                if (transported.sqrMagnitude < .00001f) {
                    transported =
                        Vector3.ProjectOnPlane(-outward, tangent).normalized;
                }
                frame = Quaternion.LookRotation(transported, tangent);

            }
            float paintBranchWeight = ctx.following
                ?rig.RemainingCoilWeight(ring)
                :Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(ctx.raisedLength - .25f,
                        ctx.raisedLength + .25f,
                        behind));
            Vector3 paintBelly = ring == 0?ctx.headBelly
                :rig.BranchCoil.SupportedPaintBelly(outward, paintBranchWeight);
            Vector3 right = frame * Vector3.right,
                belly = frame * Vector3.forward;
            float loft =
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(behind / collarLoftU));
            for (int side = 0; side <= BodySides; side++) {
                float angle = (side % BodySides) * Mathf.PI * 2f / BodySides,
                    cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
                Vector3 localCircle =
                    new Vector3(cosine * rig.spineRadii[ring], 0f,
                        sine * rig.spineRadii[ring]);
                Vector3 section =
                    Vector3.Lerp(rig.neckRim[side], localCircle, loft);
                Vector3 point =
                    rig.spineCenters[ring] + frame * section - tangent
                        * (NeckOverlapU * (1f - loft));
                Vector3 normal = right * cosine + belly * sine;
                normal = Vector3.Slerp(frame * rig.neckSourceNormals[side],
                    normal, loft).normalized;
                Vector3 radial =
                    new Vector3(point.x - rig.axisXZ.x, 0f,
                        point.z - rig.axisXZ.y);
                if (ring > 0 && radial.magnitude < surface) {
                    radial.Normalize();
                    point.x = rig.axisXZ.x + radial.x * surface;
                    point.z = rig.axisXZ.y + radial.z * surface;
                    normal = -radial;
                }
                int index = ring * BodyStride + side;
                rig.referenceWaveVertices[index] =
                    ctx.toLocal.MultiplyPoint3x4(point);
                rig.referenceWaveNormals[index] =
                    ctx.toLocal.MultiplyVector(normal).normalized;
                rig.paintedBellyDirections[index] =
                    ctx.toLocal.MultiplyVector(paintBelly).normalized;
            }
        }
        RoundNeckSurface(ctx.endRing);
    }
    private void PublishMesh() {
        using var _ = PUBLISH_MARKER.Auto();
        int tip = BodyRings * BodyStride, neckCap = tip + 1;
        rig.referenceWaveVertices[tip] =
            ctx.toLocal.MultiplyPoint3x4(rig.spineCenters[BodyRings - 1]
                + rig.spineTangents[BodyRings - 1] * .005f);
        rig.referenceWaveNormals[tip] =
            ctx.toLocal.MultiplyVector(rig.spineTangents[BodyRings - 1]);
        rig.referenceWaveVertices[neckCap] =
            ctx.toLocal.MultiplyPoint3x4(ctx.headCenter - ctx.headRotation
                * Vector3.up
                * NeckOverlapU);
        rig.referenceWaveNormals[neckCap] =
            ctx.toLocal.MultiplyVector(-(ctx.headRotation * Vector3.up));
        rig.paintedBellyDirections[neckCap] =
            ctx.toLocal.MultiplyVector(ctx.headBelly).normalized;
        rig.paintedBellyDirections[tip] =
            rig.paintedBellyDirections[(BodyRings - 1) * BodyStride];
        SmoothTubeNormals();
        rig.animatedReferenceMesh.vertices = rig.referenceWaveVertices;
        rig.animatedReferenceMesh.normals = rig.referenceWaveNormals;
        rig.animatedReferenceMesh.SetUVs(2, rig.paintedBellyDirections);
        rig.animatedReferenceMesh.RecalculateBounds();
        Vector3 sourceBelly =
            -Vector3.ProjectOnPlane(rig.SourceEyeForward(),
                Vector3.up).normalized;
        float palettePhase =
            .25f - Mathf.Atan2(sourceBelly.z, sourceBelly.x) / (Mathf.PI * 2f);
        rig.referenceRenderer.GetPropertyBlock(rig.surfaceBlock);
        rig.surfaceBlock.SetVector("_TreeAxis",
            new Vector4(rig.axisXZ.x, 0f, rig.axisXZ.y, 0f));
        rig.surfaceBlock.SetFloat("_HeadBellyPhase", palettePhase);
        rig.surfaceBlock.SetVector("_HeadBellyDirection", ctx.headBelly);
        rig.surfaceBlock.SetFloat("_CollarLength", config.NeckCollarLengthU);
        rig.surfaceBlock.SetFloat("_HeadWatch", ctx.following
            ? 1f - ctx.departure
            : 1f);
        rig.surfaceBlock.SetFloat("_BodyLength", config.SmoothBodyLengthU);
        rig.referenceRenderer.SetPropertyBlock(rig.surfaceBlock);
        rig.referenceHeadRenderer.GetPropertyBlock(rig.surfaceBlock);
        rig.surfaceBlock.SetVector("_TreeAxis",
            new Vector4(rig.axisXZ.x, 0f, rig.axisXZ.y, 0f));
        rig.surfaceBlock.SetFloat("_HeadBellyPhase", palettePhase);
        rig.surfaceBlock.SetVector("_HeadBellyDirection", ctx.headBelly);
        rig.surfaceBlock.SetFloat("_CollarLength", config.NeckCollarLengthU);
        rig.surfaceBlock.SetFloat("_HeadWatch", ctx.following
            ? rig.waitingHeadBlend : 1f);
        rig.surfaceBlock.SetFloat("_HeadWatch", 1f);
        rig.referenceHeadRenderer.SetPropertyBlock(rig.surfaceBlock);
        rig.spinePoseReady = true;
        rig.Eyes.UpdateTrackingEyes();
        rig.Eyes.UpdateVisibleContacts();
    }

    // Neck And Tube Helpers  ##################################################
    private void RoundNeckSurface(int neckEnd) {
        using var _ = ROUND_NECK_MARKER.Auto();
        if (rig.neckSurfaceBuffer == null
            || rig.neckSurfaceBuffer.Length
                != rig.referenceWaveVertices.Length) {
            rig.neckSurfaceBuffer =
                new Vector3[rig.referenceWaveVertices.Length];
            rig.neckSurfaceOriginal =
                new Vector3[rig.referenceWaveVertices.Length];
        }
        System.Array.Copy(rig.referenceWaveVertices, rig.neckSurfaceOriginal,
            rig.referenceWaveVertices.Length);
        float step = config.SmoothBodyLengthU / (BodyRings - 1);
        int end =
            Mathf.Clamp(Mathf.CeilToInt(config.RoundedNeckLengthU / step), 3,
                Mathf.Min(BodyRings - 2, neckEnd + 4));
        float roundLength = end * step;
        for (int pass = 0; pass<config.RoundedNeckPasses; pass++) {
            System.Array.Copy(rig.referenceWaveVertices, rig.neckSurfaceBuffer,
                rig.referenceWaveVertices.Length);
            for (int ring = 1; ring<end; ring++) {
                float behind = ring * step;
                float weight =
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(behind / .16f))
                    *(1f-Mathf.SmoothStep(0f,1f,
                        Mathf.InverseLerp(roundLength*.65f,roundLength,
                            behind)));
                for (int side = 0; side <= BodySides; side++) {
                    int i = ring * BodyStride + side;
                    Vector3 average =
                        (rig.referenceWaveVertices[i - BodyStride]
                            + rig.referenceWaveVertices[i + BodyStride]) * .5f;
                    Vector3 rounded =
                        Vector3.Lerp(rig.referenceWaveVertices[i], average,
                            .5f * weight);
                    rig.neckSurfaceBuffer[i] =
                        rig.neckSurfaceOriginal[i]
                            + Vector3.ClampMagnitude(rounded
                                - rig.neckSurfaceOriginal[i],
                                    config.RoundedNeckMaxOffsetU);
                }
            }
            Vector3[] swap = rig.referenceWaveVertices;
            rig.referenceWaveVertices = rig.neckSurfaceBuffer;
            rig.neckSurfaceBuffer = swap;
        }
        if (rig.roundedRingCenters == null) {
            rig.roundedRingCenters = new Vector3[BodyRings];
            rig.originalRingCenters = new Vector3[BodyRings];
        }
        for (int ring = 0; ring <= end; ring++) {
            Vector3 center = Vector3.zero, original = Vector3.zero;
            for (int side = 0; side<BodySides; side++) {
                center += rig.referenceWaveVertices[ring * BodyStride + side];
                original += rig.neckSurfaceOriginal[ring * BodyStride + side];
            }
            rig.roundedRingCenters[ring] = center / BodySides;
            rig.originalRingCenters[ring] = original / BodySides;
        }
        Vector3 headAxis =
            rig.referenceVisual.InverseTransformDirection(
                rig.spineTangents[0]).normalized;
        Vector3 previousRight =
            rig.referenceVisual.InverseTransformDirection(
                rig.referenceHeadVisual.right).normalized;
        for (int ring = 1; ring<end; ring++) {
            float behind = ring * step;
            Vector3 axis =
                (rig.roundedRingCenters[ring + 1]
                    - rig.roundedRingCenters[ring - 1]).normalized;
            axis = Vector3.Slerp(headAxis, axis,
                Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(behind
                        / config.NeckCollarLengthU))).normalized;
            float thicknessFloor =
                config.SmoothBodyRadiusU * config.MinimumNeckThicknessShare
                    * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(behind / .35f));
            Vector3 transported =
                Vector3.ProjectOnPlane(previousRight, axis).normalized;
            if (transported.sqrMagnitude<.000001f) {
                transported =
                    Vector3.ProjectOnPlane(Vector3.right, axis).normalized;
            }
            Vector3 targetRight =
                Vector3.ProjectOnPlane(rig.referenceWaveVertices[ring
                    * BodyStride] - rig.roundedRingCenters[ring],
                        axis).normalized;
            if (targetRight.sqrMagnitude<.000001f) {
                targetRight = transported;
            }
            float roll = Vector3.SignedAngle(transported, targetRight, axis);
            Vector3 right =
                Quaternion.AngleAxis(Mathf.Clamp(roll,
                    -config.BodyTwistDegPerU * step,
                        config.BodyTwistDegPerU * step), axis)
                            * transported;
            Vector3 forward = Vector3.Cross(right, axis).normalized;
            previousRight = right;
            for (int side = 0; side <= BodySides; side++) {
                int i = ring * BodyStride + side;
                float angle = (side%BodySides) * Mathf.PI * 2f / BodySides;
                Vector3 radial =
                    right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                float radius =
                    Mathf.Max(thicknessFloor,
                        Vector3.Distance(rig.neckSurfaceOriginal[i],
                            rig.originalRingCenters[ring]));
                rig.referenceWaveVertices[i] =
                    rig.roundedRingCenters[ring] + radial.normalized * radius;
            }
        }

    }
    private void SmoothTubeNormals() {
        if (rig.roundedRingCenters == null) {
            rig.roundedRingCenters = new Vector3[BodyRings];
        }
        for (int ring = 0; ring<BodyRings; ring++) {
            Vector3 center = Vector3.zero;
            for (int side = 0; side<BodySides; side++) {
                center += rig.referenceWaveVertices[ring * BodyStride + side];
            }
            rig.roundedRingCenters[ring] = center / BodySides;
        }
        float step = config.SmoothBodyLengthU / (BodyRings - 1);
        for (int ring = 1; ring<BodyRings; ring++)for (int side =
            0; side <= BodySides; side++) {
            int i = ring * BodyStride + side, s = side%BodySides;
            Vector3 along =
                rig.referenceWaveVertices[Mathf.Min(BodyRings - 1, ring + 1)
                    * BodyStride + side]
                -rig.referenceWaveVertices[(ring - 1) * BodyStride + side];
            Vector3 across =
                rig.referenceWaveVertices[ring * BodyStride + (s + 1)%BodySides]
                -rig.referenceWaveVertices[ring * BodyStride
                    + (s + BodySides - 1)%BodySides];
            Vector3 normal = Vector3.Cross(along, across);
            if (normal.sqrMagnitude<.00000001f) {
                continue;
            }
            normal.Normalize();
            if (Vector3.Dot(normal,
                rig.referenceWaveVertices[i]
                    - rig.roundedRingCenters[ring])<0f) {
                normal = -normal;
            }
            rig.referenceWaveNormals[i] =
                Vector3.Slerp(rig.referenceWaveNormals[i], normal,
                    SmoothVisual01(ring * step / .3f)).normalized;
        }
    }
    private void RelaxOriginalRaisedNeck() {
        if (rig.relaxedNeck == null || rig.relaxedNeck.Length != BodyRings) {
            rig.relaxedNeck = new Vector3[BodyRings];
        }
        System.Array.Copy(rig.spineCenters, rig.relaxedNeck, BodyRings);
        float step = config.SmoothBodyLengthU / (BodyRings - 1),
            newest = rig.swimTrail[rig.swimTrail.Count - 1].distance;
        int reach = Mathf.Max(1, Mathf.RoundToInt(.4f / step));
        float phase =
            Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((Time.time - rig.attackStartedTime) / .25f));
        for (int ring = 3; ring<BodyRings - 2; ring++) {
            float behind = ring * step, pathDistance = newest - behind;
            float start = rig.launchTrailDistance - rig.launchNeckArc;
            float window =
                Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(start - .25f, start + .25f, pathDistance))
                *(1f-Mathf.SmoothStep(0f,1f,
                    Mathf.InverseLerp(rig.launchTrailDistance+.25f,
                        rig.launchTrailDistance+.75f,pathDistance)))
                *Mathf.SmoothStep(0f,1f,Mathf.Clamp01(behind/.5f));
            if (window <= 0f || rig.RemainingCoilWeight(ring)>.01f) {
                continue;
            }
            Vector3 point =
                (rig.spineCenters[Mathf.Max(0, ring - 2 * reach)]
                    + rig.spineCenters[Mathf.Max(0, ring - reach)] * 4f
                        + rig.spineCenters[ring] * 6f
                +rig.spineCenters[Mathf.Min(BodyRings - 1, ring + reach)] * 4f
                    + rig.spineCenters[Mathf.Min(BodyRings - 1,
                        ring + 2 * reach)]) / 16f;
            rig.relaxedNeck[ring] =
                rig.BranchCoil.KeepClearOfBranch(
                    Vector3.Lerp(rig.spineCenters[ring], point, phase * window),
                    rig.spineRadii[ring] + .012f);
        }
        Vector3[] swap = rig.spineCenters;
        rig.spineCenters = rig.relaxedNeck;
        rig.relaxedNeck = swap;
    }
    private void FairSpineCorners(bool following, int neckEnd) {
        using var _ = FAIR_MARKER.Auto();
        if (rig.fairSpine == null || rig.fairSpine.Length != BodyRings) {
            rig.fairSpine = new Vector3[BodyRings];
        }
        int end = following?BodyRings - 2:Mathf.Min(BodyRings - 2, neckEnd + 2);
        float toleranceSqr =
            config.SpineFairToleranceU * config.SpineFairToleranceU;
        for (int pass = 0; pass < config.SpineFairMaxPasses; pass++) {
            System.Array.Copy(rig.spineCenters, rig.fairSpine, BodyRings);
            float movedSqr = 0f;
            for (int ring = 3; ring<end; ring++) {
                float freeWeight = following?1f - rig.RemainingCoilWeight(ring)
                    :1f;
                if (freeWeight<.00001f) {
                    continue;
                }
                Vector3 before =
                    rig.spineCenters[ring] - rig.spineCenters[ring - 1],
                        after = rig.spineCenters[ring + 1]
                            - rig.spineCenters[ring];
                // a bend under the floor angle adds no strength, so the dot
                // product spares the angle call on every straight ring
                float lengthsSqr = before.sqrMagnitude * after.sqrMagnitude;
                if (lengthsSqr < .000000000001f
                    || Vector3.Dot(before, after) / Mathf.Sqrt(lengthsSqr)
                        >= BEND_FLOOR_COS) {
                    continue;
                }
                float bend = Vector3.Angle(before, after);
                float strength =
                    SmoothVisual01(Mathf.InverseLerp(2f, 8f, bend))
                        * freeWeight;
                if (strength<.00001f) {
                    continue;
                }
                Vector3 point =
                    (rig.spineCenters[ring - 1] + rig.spineCenters[ring] * 2f
                        + rig.spineCenters[ring + 1]) * .25f;
                rig.fairSpine[ring] =
                    rig.BranchCoil.KeepClearOfBranch(
                        Vector3.Lerp(rig.spineCenters[ring], point, strength),
                        rig.spineRadii[ring] + .012f);
                movedSqr =
                    Mathf.Max(movedSqr,
                        (rig.fairSpine[ring] - rig.spineCenters[ring])
                            .sqrMagnitude);
            }
            Vector3[] swap = rig.spineCenters;
            rig.spineCenters = rig.fairSpine;
            rig.fairSpine = swap;
            if (movedSqr <= toleranceSqr) {
                break;
            }
        }
    }
}
