using System.Collections.Generic;
using UnityEngine;
using static FallingCobraRig;
using static FallingCobraMath;

/// <summary>The path the head swims down the bark, and the sampling of it that
/// places every ring behind the head.</summary>
public sealed class FallingCobraSwimTrail {
    // Private Members  ########################################################
    private readonly FallingCobraRig rig;
    private readonly FallingCobraTuning tuning;


    public FallingCobraSwimTrail(FallingCobraRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    public void AppendSwimTrail(Vector3 position) {
        if (rig.swimTrail.Count == 0) {
            rig.swimTrail.Add(new SwimTrailPoint(position, 0f));
            return;
        }
        SwimTrailPoint previous = rig.swimTrail[rig.swimTrail.Count - 1];
        float distance = Vector3.Distance(previous.position, position);
        if (distance>0.00001f) {
            rig.swimTrail.Add(new SwimTrailPoint(position,
                previous.distance + distance));
        }
    }

    public void BeginSwimTrail() {
        rig.swimTrail.Clear();
        rig.launchBodyOffsets = new float[BodyRings];
        for (int ring = BodyRings - 1; ring >= 0; ring--) {
            Vector3 position = rig.spinePoseReady ? rig.spineCenters[ring]
                : rig.body.position + rig.host.up
                    * (tuning.SmoothBodyLengthU * ring / (BodyRings - 1));
            Vector3 outward =
                new Vector3(position.x - rig.axisXZ.x, 0f,
                    position.z - rig.axisXZ.y).normalized;
            AppendSwimTrail(rig.OutsideTree(position - outward
                * rig.GroundSpineOffset(ring)));
            rig.launchBodyOffsets[ring] =
                rig.swimTrail[rig.swimTrail.Count - 1].distance;
        }
        rig.launchTrailDistance =
            rig.swimTrail[rig.swimTrail.Count - 1].distance;
        rig.launchBodyArc = rig.launchTrailDistance;
        for (int ring = 0; ring<BodyRings; ring++) {
            rig.launchBodyOffsets[ring] =
                rig.launchTrailDistance - rig.launchBodyOffsets[ring];
        }
        rig.launchNeckArc = 0f;
        int raisedEnd =
            Mathf.Clamp(Mathf.RoundToInt(tuning.StandingNeckLengthU
                / tuning.SmoothBodyLengthU * (BodyRings - 1)), 1,
                    BodyRings - 1);
        for (int ring = 1; ring <= raisedEnd; ring++) {
            rig.launchNeckArc +=
                Vector3.Distance(rig.spineCenters[ring - 1],
                    rig.spineCenters[ring]);
        }
        rig.previousTrailRoot = rig.swimTrail[rig.swimTrail.Count - 1].position;
        rig.displayedTrailHead = rig.previousTrailRoot;
        rig.displayedTrailVelocity = Vector3.zero;
        rig.releaseHeadHeight = rig.spineCenters[0].y;
        rig.releaseTrailOffset =
            rig.previousTrailRoot - rig.GetTreeSurfacePoint(rig.body.position);
        rig.releaseStandingBlend = rig.waitingHeadBlend;
        rig.previousTrailTime = Time.time;
        rig.trailHeading = rig.Tracking.GetAttackHeading(rig.body.position);
        rig.swimPhase = rig.swimPhaseOffset;
        rig.swimTravelDistance = 0f;
        rig.swimTrailReady = true;
    }

    public Vector3 GetAttackTrailRoot() {
        if (rig.pursuitPaused && rig.swimTrailReady) {
            return rig.previousTrailRoot;
        }
        float age = Mathf.Max(0f, Time.time - rig.attackStartedTime);
        float t = Mathf.Clamp01(age / tuning.ReleaseCurveDurationS);
        float ease = t * t * t * (t * (t * 6f - 15f) + 10f);
        Vector3 point =
            rig.GetTreeSurfacePoint(rig.Tracking.GetPredictedAttackPosition())
                + rig.releaseTrailOffset * (1f - ease);
        point -= rig.launchWindupOffset
            * Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(age / tuning.StrikeLaunchRampS)) * (1f - ease);
        if (rig.descentStopped) {
            point.y = Mathf.Max(point.y, rig.stoppedTrailHeight);
        }
        return rig.OutsideTree(point);
    }

    public void AdvanceSwimTrail() {
        if (!rig.swimTrailReady) {
            BeginSwimTrail();
        }
        if (rig.pursuitPaused) {
            rig.previousTrailTime = Time.time;
            rig.displayedTrailVelocity = Vector3.zero;
            return;
        }
        float elapsed = Time.time - rig.previousTrailTime;
        if (elapsed <= 0.000001f) {
            return;
        }
        Vector3 currentRoot = GetAttackTrailRoot(),
            lastRoot = rig.previousTrailRoot;
        int samples = Mathf.Clamp(Mathf.CeilToInt(elapsed * 120f), 1, 64);
        float sampleTime = elapsed / samples;
        for (int sample = 1; sample <= samples; sample++) {
            float fraction = sample / (float)samples;
            Vector3 nominal =
                Vector3.Lerp(rig.previousTrailRoot, currentRoot, fraction);
            float advance =
                Vector3.Distance(rig.GetTreeSurfacePoint(lastRoot),
                    rig.GetTreeSurfacePoint(nominal));
            rig.swimTravelDistance += advance;
            rig.swimPhase +=
                advance / Mathf.Max(.5f, tuning.SwimWavelengthU) * Mathf.PI
                    * 2f;
            Vector3 outward =
                new Vector3(nominal.x - rig.axisXZ.x, 0f,
                    nominal.z - rig.axisXZ.y).normalized;
            Vector3 desired = rig.Tracking.GetAttackHeading(nominal);
            Vector3 previous =
                Vector3.ProjectOnPlane(rig.trailHeading, outward).normalized;
            if (previous.sqrMagnitude<0.000001f) {
                previous = desired;
            }
            float turn = Vector3.SignedAngle(previous, desired, outward);
            rig.trailHeading =
                Quaternion.AngleAxis(turn
                    * (1f - Mathf.Exp(-tuning.SwimTurnSharpness * sampleTime)),
                        outward) * previous;
            Vector3 lateral =
                Vector3.Cross(rig.trailHeading, outward).normalized;
            float enterDistance =
                Mathf.Max(tuning.SmoothBodyRadiusU * 4f,
                    tuning.HeadSwayEntryDistanceU);
            float enter =
                Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(rig.swimTravelDistance / enterDistance));
            Vector3 head =
                nominal + lateral
                    * (tuning.HeadSwayAmplitudeU * Mathf.Sin(rig.swimPhase)
                        * enter * rig.Tracking.SteadyHeadAim(nominal));
            head = Vector3.SmoothDamp(rig.displayedTrailHead, head,
                ref rig.displayedTrailVelocity,
                    Mathf.Lerp(tuning.HeadTrailSmoothTimeS
                        * tuning.StrikeHeadFollowScale,
                            tuning.HeadTrailSmoothTimeS,
                                Mathf.SmoothStep(0f, 1f,
                                    Mathf.Clamp01((Time.time
                                        - rig.attackStartedTime)
                                            / tuning.StrikeBurstDurationS))),
                                                Mathf.Infinity, sampleTime);
            // Store the head's branch-clearing route so the entire body follows
            // that route.
            for (int clearancePass = 0; clearancePass<4; clearancePass++) {
                Vector3 headOutward =
                    new Vector3(head.x - rig.axisXZ.x, 0f,
                        head.z - rig.axisXZ.y).normalized;
                Vector3 headCenter =
                    head + headOutward * rig.GroundSpineOffset(0);
                Quaternion headRotation =
                    Quaternion.Slerp(rig.standingHeadRotation,
                        rig.Tracking.DirectedAttackRotation(headCenter),
                            rig.HeadDeparture(headCenter));
                headCenter =
                    rig.Tracking.FitHeadToBark(headCenter, headRotation);
                head = rig.OutsideTree(headCenter - headOutward
                    * rig.GroundSpineOffset(0));
                headCenter = head + headOutward * rig.GroundSpineOffset(0);
                head = rig.OutsideTree(head
                    + rig.BranchCoil.KeepHeadClearOfBranch(headCenter,
                        headRotation) - headCenter);
            }
            rig.displayedTrailHead = head;
            // Follow the displayed head poses; substep corner projections must
            // not add invisible detours.
            if (sample == samples) {
                AppendSwimTrail(head);
            }
            lastRoot = nominal;
        }
        rig.previousTrailRoot = currentRoot;
        rig.previousTrailTime = Time.time;
        float newest = rig.swimTrail[rig.swimTrail.Count - 1].distance;
        while (rig.swimTrail.Count>4
            && newest
                - rig.swimTrail[1].distance>Mathf.Max(tuning.SmoothBodyLengthU,
                    rig.launchBodyArc) + 1f) {
            rig.swimTrail.RemoveAt(0);
        }
    }

    public void SettleRemainingCoil() {
        if (rig.supportBranch == null) {
            return;
        }
        float gap = float.PositiveInfinity;
        for (int ring = 1; ring<BodyRings; ring++) {
            if (rig.RemainingCoilWeight(ring)<.99f) {
                continue;
            }
            gap = Mathf.Min(gap,
                Vector3.Dot(rig.spineCenters[ring] - rig.branchCenter,
                    rig.branchUp) - rig.branchHalfHeight - rig.spineRadii[ring]
                        - tuning.BranchSkinClearanceU);
        }
        if (float.IsInfinity(gap) || gap <= 0f) {
            return;
        }
        for (int ring = 1; ring<BodyRings; ring++) {
            rig.spineCenters[ring] -=
                rig.branchUp * (gap * rig.RemainingCoilWeight(ring));
        }
    }

    public Vector3 SampleSwimTrail(float behind, ref int cursor) {
        float distance =
            rig.swimTrail[rig.swimTrail.Count - 1].distance - behind;
        if (distance <= rig.swimTrail[0].distance) {
            Vector3 tail =
                (rig.swimTrail[0].position
                    - rig.swimTrail[1].position).normalized;
            return rig.OutsideTree(rig.swimTrail[0].position + tail
                * (rig.swimTrail[0].distance - distance));
        }
        while (cursor>1 && rig.swimTrail[cursor - 1].distance>distance) {
            cursor--;
        }
        int lower = cursor - 1, upper = cursor;
        SwimTrailPoint a = rig.swimTrail[lower], b = rig.swimTrail[upper];
        SwimTrailPoint before = rig.swimTrail[Mathf.Max(0, lower - 1)],
            after = rig.swimTrail[Mathf.Min(rig.swimTrail.Count - 1,
                upper + 1)];
        float span = Mathf.Max(.00001f, b.distance - a.distance);
        float t = Mathf.Clamp01((distance - a.distance) / span), t2 = t * t,
            t3 = t2 * t;
        Vector3 firstTangent =
            (b.position - before.position)
                * (span / Mathf.Max(.00001f, b.distance - before.distance));
        Vector3 lastTangent =
            (after.position - a.position)
                * (span / Mathf.Max(.00001f, after.distance - a.distance));
        Vector3 point =
            (2f * t3 - 3f * t2 + 1f) * a.position + (t3 - 2f * t2 + t)
                * firstTangent
            +(-2f * t3 + 3f * t2) * b.position + (t3 - t2) * lastTangent;
        return rig.OutsideTree(point);
    }
}
