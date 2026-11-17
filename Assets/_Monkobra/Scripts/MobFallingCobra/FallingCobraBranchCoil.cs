using System.Collections.Generic;
using UnityEngine;
using static FallingCobraRig;
using static FallingCobraMath;

/// <summary>Frames the supporting branch and places the coil, perch, and head
/// against it.</summary>
public sealed class FallingCobraBranchCoil {
    // Private Members  ########################################################
    private readonly FallingCobraRig rig;
    private readonly FallingCobraTuning tuning;

    public FallingCobraBranchCoil(FallingCobraRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    public void RefreshBranchFrame() {
        if (rig.supportBranch == null) {
            return;
        }
        Transform branch = rig.supportBranch.transform;
        rig.branchCenter = branch.TransformPoint(rig.supportBranch.center);
        Vector3 outwards =
            new Vector3(rig.branchCenter.x - rig.axisXZ.x, 0f,
                rig.branchCenter.z - rig.axisXZ.y).normalized;
        rig.branchAxis = branch.right;
        if (Vector3.Dot(rig.branchAxis, outwards) < 0f) {
            rig.branchAxis = -rig.branchAxis;
        }
        rig.branchUp = branch.up;
        rig.branchSide = Vector3.Cross(rig.branchUp, rig.branchAxis).normalized;
        Vector3 scale = branch.lossyScale, size = rig.supportBranch.size;
        rig.branchHalfLength = Mathf.Abs(scale.x * size.x) * .5f;
        rig.branchHalfHeight = Mathf.Abs(scale.y * size.y) * .5f;
        rig.branchHalfWidth = Mathf.Abs(scale.z * size.z) * .5f;
    }

    // The snake coils around itself above the wood; the branch never passes
    // through a loop.
    public Vector3 BranchCoilPoint(float behind, float tubeRadius) {
        float along =
            Mathf.Clamp01(behind
                / Mathf.Max(.1f,
                    tuning.SmoothBodyLengthU - tuning.StandingNeckLengthU));
        float phase =
            along * tuning.BranchCoilTurns * Mathf.PI * 2f
                + rig.perchedCoilPhaseDegrees * Mathf.Deg2Rad;
        float baseStart =
            Mathf.Max(.01f,
                (tuning.BranchCoilTurns - 1f)
                    / Mathf.Max(1f, tuning.BranchCoilTurns));
        float radiusScale = along <= baseStart
            ? Mathf.Lerp(.55f, 1f,
                Mathf.Sin(Mathf.Clamp01(along / baseStart) * Mathf.PI * .5f))
            : Mathf.Lerp(1f, tuning.SittingCoilInnerScale,
                Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(baseStart, 1f, along)));
        float baseProgress =
            (tuning.StandingNeckLengthU
                + (tuning.SmoothBodyLengthU - tuning.StandingNeckLengthU)
                    * baseStart) / tuning.SmoothBodyLengthU;
        float baseRadius =
            tuning.SmoothBodyRadiusU
                * (1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(.55f, 1f, baseProgress)));
        // The last turn lies flat on the branch; its taper curls inward
        // underneath the upper body.
        float stackHeight = along < baseStart
            ? baseRadius + tuning.SittingCoilLayerRiseU
                * tuning.BranchCoilTurns * (baseStart - along) : tubeRadius;
        float height =
            rig.branchHalfHeight + Mathf.Max(tubeRadius, stackHeight)
                + tuning.BranchSkinClearanceU;
        return rig.branchCenter + rig.branchAxis
            * (rig.branchAnchorAlong + tuning.SittingCoilRadiusAlongU
                * radiusScale * Mathf.Cos(phase))
            + rig.branchSide
                * (tuning.SittingCoilRadiusAcrossU * radiusScale
                    * Mathf.Sin(phase)) + rig.branchUp * height;
    }

    public Vector3 BranchSurfacePoint(Vector3 point) {
        Vector3 delta = point - rig.branchCenter;
        float a = Vector3.Dot(delta, rig.branchAxis),
            u = Vector3.Dot(delta, rig.branchUp),
                s = Vector3.Dot(delta, rig.branchSide);
        Vector3 closest =
            rig.branchCenter + rig.branchAxis
                * Mathf.Clamp(a, -rig.branchHalfLength, rig.branchHalfLength)
            + rig.branchUp
                * Mathf.Clamp(u, -rig.branchHalfHeight, rig.branchHalfHeight)
            + rig.branchSide
                * Mathf.Clamp(s, -rig.branchHalfWidth, rig.branchHalfWidth);
        if ((closest - point).sqrMagnitude > .000001f) {
            return closest;
        }
        float da = rig.branchHalfLength - Mathf.Abs(a),
            du = rig.branchHalfHeight - Mathf.Abs(u),
                ds = rig.branchHalfWidth - Mathf.Abs(s);
        if (da < du && da < ds) {
            return closest + rig.branchAxis * ((a >= 0f ? 1f : -1f) * da);
        }
        if (du < ds) {
            return closest + rig.branchUp * ((u >= 0f ? 1f : -1f) * du);
        }
        return closest + rig.branchSide * ((s >= 0f ? 1f : -1f) * ds);
    }

    public Vector3 KeepClearOfBranch(Vector3 point, float radius) {
        if (rig.supportBranch == null) {
            return point;
        }
        Vector3 closest = BranchSurfacePoint(point);
        Vector3 delta = point - rig.branchCenter;
        bool inside =
            Mathf.Abs(Vector3.Dot(delta, rig.branchAxis)) < rig.branchHalfLength
            && Mathf.Abs(Vector3.Dot(delta,
                rig.branchUp)) < rig.branchHalfHeight
                    && Mathf.Abs(Vector3.Dot(delta,
                        rig.branchSide)) < rig.branchHalfWidth;
        Vector3 away = inside ? closest - point : point - closest;
        float distance = away.magnitude,
            clearance = radius + tuning.BranchSkinClearanceU;
        if (!inside && distance >= clearance) {
            return point;
        }
        return closest + (distance > .00001f ? away / distance : rig.branchUp)
            * clearance;
    }

    public Vector3 VentralSupportDirection(Vector3 point, Vector3 treeInward,
        float tubeRadius) {
        if (rig.supportBranch == null) {
            return treeInward;
        }
        Vector3 delta =
            point - rig.branchCenter - rig.branchAxis * rig.branchAnchorAlong;
        float height = Vector3.Dot(delta, rig.branchUp) - rig.branchHalfHeight;
        float alongEdge =
            Mathf.Abs(Vector3.Dot(delta, rig.branchAxis))
                - tuning.SittingCoilRadiusAlongU - tubeRadius;
        float sideEdge =
            Mathf.Abs(Vector3.Dot(delta, rig.branchSide))
                - tuning.SittingCoilRadiusAcrossU - tubeRadius;
        float pileSupport =
            Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(-tubeRadius, .05f, height))
            * (1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(0f, .25f, Mathf.Max(alongEdge, sideEdge))));
        Vector3 supportedBelly =
            Vector3.Slerp(treeInward, -rig.branchUp, pileSupport).normalized;
        Vector3 towardBranch = BranchSurfacePoint(point) - point;
        float distance = towardBranch.magnitude;
        if (distance < .00001f) {
            return supportedBelly;
        }
        float contact =
            1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(tubeRadius + .03f, tubeRadius + .25f,
                    distance));
        return Vector3.Slerp(supportedBelly, towardBranch / distance,
            contact).normalized;
    }

    public void UpdatePerchedAim(float deltaTime) {
        if (rig.player == null || rig.supportBranch == null) {
            return;
        }
        Vector3 toward =
            rig.Tracking.MonkeyAimPosition() - rig.branchCenter
                - rig.branchAxis * rig.branchAnchorAlong;
        float side = Vector3.Dot(toward, rig.branchSide),
            along = Vector3.Dot(toward, rig.branchAxis);
        float gain = 1f - Mathf.Exp(-tuning.PerchedAimSharpness * deltaTime);
        rig.perchedAimSide =
            Mathf.Lerp(rig.perchedAimSide,
                Mathf.Clamp(side / Mathf.Max(.5f, tuning.BranchHeadSideU), -1f,
                    1f), gain);
        float phase =
            Mathf.Atan2(side / Mathf.Max(.1f, tuning.SittingCoilRadiusAcrossU),
                along / Mathf.Max(.1f, tuning.SittingCoilRadiusAlongU))
                    * Mathf.Rad2Deg;
        rig.perchedCoilPhaseDegrees =
            Mathf.LerpAngle(rig.perchedCoilPhaseDegrees, phase, gain);
        float headClearance = rig.referenceHeadVisual != null
            ?Vector3.Dot(rig.referenceHeadVisual.position
                + rig.referenceHeadVisual.rotation
                    * rig.originalHeadBounds.center - rig.branchCenter,
                        rig.branchUp):0f;
        if (Mathf.Abs(rig.perchedAimSide)>.2f
            && headClearance>rig.branchHalfHeight
                + rig.originalHeadBounds.extents.magnitude + .05f)
            rig.branchHeadSideSign = Mathf.Sign(rig.perchedAimSide);
    }

    public Vector3 SupportedPaintBelly(Vector3 outward, float branchWeight) {
        // The raised neck faces the trunk; a seated coil rests its belly
        // downward on the wood.
        // Do not project onto the spine tangent: steep bends can invert that
        // projection.
        return Vector3.Slerp(-outward, -rig.branchUp,
            Mathf.Clamp01(branchWeight)).normalized;
    }

    public Vector3 KeepHeadClearOfBranch(Vector3 position,
        Quaternion rotation) {
        if (rig.supportBranch == null) {
            return position;
        }
        Vector3 center = position + rotation * rig.originalHeadBounds.center;
        float radius = rig.originalHeadBounds.extents.magnitude + .035f;
        Vector3 delta = center - rig.branchCenter;
        float alongGap =
            Mathf.Max(0f,
                Mathf.Abs(Vector3.Dot(delta, rig.branchAxis))
                    - rig.branchHalfLength);
        float protectedTop = rig.branchHalfHeight,
            protectedSide = rig.branchHalfWidth;
        if (rig.state == FallingCobraState.Falling && rig.swimTrailReady
            && rig.RemainingCoilWeight(BodyRings - 1)>.01f) {
            // Exit outside the supported pile, rather than diving through the
            // remaining body.
            protectedTop +=
                tuning.SmoothBodyRadiusU * 2f + tuning.SittingCoilLayerRiseU
                    * tuning.BranchCoilTurns;
            protectedSide =
                Mathf.Max(protectedSide,
                    tuning.SittingCoilRadiusAcrossU + tuning.SmoothBodyRadiusU);
        }
        float protectedCenter = (protectedTop - rig.branchHalfHeight) * .5f;
        float protectedHalfHeight = (protectedTop + rig.branchHalfHeight) * .5f;
        float upGap =
            Mathf.Max(0f,
                Mathf.Abs(Vector3.Dot(delta, rig.branchUp) - protectedCenter)
                    - protectedHalfHeight);
        float remaining = radius * radius - alongGap * alongGap - upGap * upGap;
        if (remaining <= 0f) {
            return position;
        }
        float requiredSide = protectedSide + Mathf.Sqrt(remaining);
        float penetration =
            requiredSide - Vector3.Dot(delta, rig.branchSide)
                * rig.branchHeadSideSign;
        const float feather = .1f;
        float shift = penetration >= feather ? penetration
            : penetration <= -feather ? 0f
            : (penetration + feather) * (penetration + feather)
                / (4f * feather);
        return position + rig.branchSide * (rig.branchHeadSideSign * shift);
    }

    public Vector3 RestingBranchFace(Quaternion rotation) {
        Vector3 woodTop =
            rig.branchCenter + rig.branchAxis * rig.branchAnchorAlong
                + rig.branchUp * rig.branchHalfHeight;
        float bottom = 0f;
        for (int corner = 0; corner < 8; corner++) {
            Vector3 p =
                rig.originalHeadBounds.center
                    + Vector3.Scale(rig.originalHeadBounds.extents,
                new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0
                    ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
            bottom = Mathf.Min(bottom,
                Vector3.Dot(rotation * (p - rig.originalHeadFaceCenter),
                    rig.branchUp));
        }
        Vector3 outwards =
            new Vector3(rig.branchCenter.x - rig.axisXZ.x, 0f,
                rig.branchCenter.z - rig.axisXZ.y).normalized;
        return woodTop + rig.branchUp * (-bottom + .025f) + outwards
            * tuning.BranchHeadForwardU + rig.branchSide
                * (tuning.BranchHeadSideU * rig.perchedAimSide);
    }

    public Vector3 StandingTailPoint(float behind, Vector3 outward,
        Vector3 side) {
        float length =
            Mathf.Max(.1f,
                tuning.SmoothBodyLengthU - tuning.StandingNeckLengthU);
        float along = Mathf.Clamp01(behind / length);
        float radius =
            Mathf.Lerp(tuning.StandingCoilOuterRadiusU,
                tuning.StandingCoilInnerRadiusU, along);
        float theta = along * tuning.StandingCoilTurns * Mathf.PI * 2f;
        float horizontal = radius * Mathf.Sin(theta);
        float height =
            tuning.StandingBaseHeightU + radius * Mathf.Cos(theta)
                - tuning.StandingCoilOuterRadiusU;
        float skinRadius =
            tuning.TreeSurfaceRadiusU + tuning.TreeSurfaceClearanceU;
        float angle = horizontal / skinRadius;
        Vector3 radial = outward * Mathf.Cos(angle) + side * Mathf.Sin(angle);
        return new Vector3(rig.axisXZ.x + radial.x * skinRadius,
            rig.body.position.y + height, rig.axisXZ.y + radial.z * skinRadius);
    }

}
