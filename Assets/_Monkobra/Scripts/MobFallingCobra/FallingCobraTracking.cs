using System.Collections.Generic;
using UnityEngine;
using static FallingCobraRig;
using static FallingCobraMath;

/// <summary>Follows the monkey: aim point, tracked range, head steering, and
/// the fall velocity.</summary>
public sealed class FallingCobraTracking {
    // Private Members  ########################################################
    private readonly FallingCobraRig rig;
    private readonly FallingCobraTuning tuning;

    public FallingCobraTracking(FallingCobraRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    public Vector3 GetAttackHeading(Vector3 position) {
        Vector3 outward =
            new Vector3(position.x - rig.axisXZ.x, 0f,
                position.z - rig.axisXZ.y).normalized;
        Vector2 velocity = CommittedAttackVelocity(position);
        Vector3 heading =
            Vector3.Cross(Vector3.up, outward) * velocity.x + Vector3.up
                * velocity.y;
        return heading.sqrMagnitude>0.000001f?heading.normalized:-rig.host.up;
    }

    public Vector3 GetPredictedAttackPosition() {
        Vector3 position = rig.body.position;
        float lag =
            Mathf.Clamp(Time.time - Time.fixedTime, 0f, Time.fixedDeltaTime);
        Vector2 velocity = CommittedAttackVelocity(position);
        position.y += velocity.y * lag;
        if (rig.ringRadiusU>MIN_RING_RADIUS_U) {
            float angle =
                Mathf.Atan2(position.x - rig.axisXZ.x,
                    position.z - rig.axisXZ.y) + velocity.x * lag
                        / rig.ringRadiusU;
            position.x = rig.axisXZ.x + Mathf.Sin(angle) * rig.ringRadiusU;
            position.z = rig.axisXZ.y + Mathf.Cos(angle) * rig.ringRadiusU;
        }
        return position;
    }

    public Vector2 SurfaceDeltaToPlayer(Vector3 position) {
        if (rig.player == null || rig.ringRadiusU <= MIN_RING_RADIUS_U) {
            return new Vector2(0f, -1f);
        }
        Vector2 fromAxis = new Vector2(position.x, position.z) - rig.axisXZ;
        Vector2 toPlayer =
            new Vector2(rig.player.position.x, rig.player.position.z)
                - rig.axisXZ;
        if (toPlayer.sqrMagnitude < MIN_RING_RADIUS_U * MIN_RING_RADIUS_U) {
            return new Vector2(0f, -1f);
        }
        float currentAngle =
            Mathf.Atan2(fromAxis.x, fromAxis.y) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(toPlayer.x, toPlayer.y) * Mathf.Rad2Deg;
        return new Vector2(Mathf.DeltaAngle(currentAngle, targetAngle)
            * Mathf.Deg2Rad * rig.ringRadiusU,
            rig.player.position.y - position.y);
    }

    public Quaternion DirectedAttackRotation(Vector3 position) {
        Vector3 outward =
            new Vector3(position.x - rig.axisXZ.x, 0f,
                position.z - rig.axisXZ.y).normalized;
        Vector3 heading =
            Vector3.ProjectOnPlane(rig.displayedTrailVelocity,
                outward).normalized;
        if (heading.sqrMagnitude < .00001f) {
            heading = GetAttackHeading(position);
        }
        Vector3 toMonkey = rig.committedTargetPosition - position;
        if (rig.monkeyInHeadRange) {
            Vector3 tangent =
                Vector3.ProjectOnPlane(toMonkey, outward).normalized;
            if (tangent.sqrMagnitude > .00001f) {
                float turn =
                    Mathf.Clamp(Vector3.SignedAngle(heading, tangent, outward),
                        -tuning.TrackingSurfaceTurnDegrees,
                            tuning.TrackingSurfaceTurnDegrees);
                heading = Quaternion.AngleAxis(turn, outward) * heading;
            }
        }
        Quaternion dorsal =
            Quaternion.LookRotation(
                Vector3.ProjectOnPlane(rig.SourceEyeForward(), Vector3.up)
                    .normalized,
                Vector3.up);
        Quaternion flat =
            Quaternion.LookRotation(outward, -heading)
                * Quaternion.Inverse(dorsal);
        if (!rig.monkeyInHeadRange || toMonkey.sqrMagnitude < .00001f) {
            return flat;
        }
        // The face, rather than just its pupils, follows the live target. Keep
        // roll supported by the bark.
        Vector3 gaze = toMonkey.normalized;
        Vector3 faceUp = Vector3.ProjectOnPlane(outward, gaze).normalized;
        if (faceUp.sqrMagnitude < .00001f) {
            faceUp = Vector3.ProjectOnPlane(Vector3.up, gaze).normalized;
        }
        Quaternion aimed =
            Quaternion.LookRotation(gaze, faceUp)
                * Quaternion.Inverse(rig.SourceEyeBasis());
        return Quaternion.RotateTowards(flat, aimed,
            tuning.UpwardHeadPitchDegrees);
    }

    public Vector3 FitHeadToBark(Vector3 position, Quaternion rotation) {
        if (rig.state != FallingCobraState.Falling) {
            return position;
        }
        float blend = rig.HeadDeparture(position);
        Vector3 outward =
            new Vector3(position.x - rig.axisXZ.x, 0f,
                position.z - rig.axisXZ.y).normalized;
        Vector3 direction = Quaternion.Inverse(rotation) * outward;
        if (!rig.supportCached
            || Vector3.Dot(direction, rig.supportDirection)<.99995f) {
            rig.supportMinimum = float.PositiveInfinity;
            foreach (Vector3 vertex in rig.originalHeadVertices) {
                rig.supportMinimum =
                    Mathf.Min(rig.supportMinimum,
                        Vector3.Dot(vertex, direction));
            }
            rig.supportDirection = direction;
            rig.supportCached = true;
        }
        float desired =
            tuning.TreeSurfaceRadiusU + tuning.HeadBarkGapU
                - rig.supportMinimum;
        if (rig.monkeyInHeadRange) {
            Vector3 faceOffset =
                rotation
                    * (tuning.EyeAnchorsReady
                        ? (tuning.LeftEyeSurfaceLocal
                            + tuning.RightEyeSurfaceLocal) * .5f
                                - rig.SourceEyeUp()
                                    * .06f : rig.originalHeadFaceCenter);
            Vector3 face = position + faceOffset;
            Vector3 delta = rig.committedTargetPosition - face;
            float surfaceGap = Vector3.ProjectOnPlane(delta, outward).magnitude;
            float reachBlend =
                1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(surfaceGap / tuning.StrikeReachDistanceU));
            float targetRadius =
                Vector3.Dot(rig.committedTargetPosition
                    - new Vector3(rig.axisXZ.x, position.y, rig.axisXZ.y)
                        - faceOffset, outward);
            desired +=
                Mathf.Clamp(targetRadius - desired, 0f,
                    tuning.MaximumStrikeReachU) * reachBlend;
        }
        float radius =
            new Vector2(position.x - rig.axisXZ.x,
                position.z - rig.axisXZ.y).magnitude;
        return position + outward * ((desired - radius) * blend);
    }

    public Vector3 MonkeyAimPosition() {
        if (rig.player == null) {
            return rig.committedTargetPosition;
        }
        if (rig.cachedAimOwner != rig.player) {
            rig.cachedAimOwner = rig.player;
            rig.playerBodyCollider = null;
            float best = 0f;
            foreach (Collider candidate in
                rig.player.GetComponentsInChildren<Collider>()) {
                if (candidate.isTrigger
                    || candidate.GetComponentInParent<ArmController>()
                        != null) {
                    continue;
                }
                Vector3 size = candidate.bounds.size;
                float volume = size.x * size.y * size.z;
                if (volume>best) {
                    best = volume;
                    rig.playerBodyCollider = candidate;
                }
            }
        }
        return rig.playerBodyCollider != null && rig.playerBodyCollider.enabled
            ?rig.playerBodyCollider.bounds.center:rig.player.position;
    }

    public void UpdateTrackedMonkey() {
        if (rig.player == null) {
            rig.monkeyInHeadRange = false;
            rig.pursuitPaused = rig.state == FallingCobraState.Falling;
            return;
        }
        Vector3 aim = MonkeyAimPosition();
        float range =
            Mathf.Max(tuning.HeadTrackingRangeU, tuning.TriggerDistanceU);
        if (IsMonkeyReaching()) {
            range = Mathf.Max(range, rig.reachWakeDistanceU);
        }
        rig.monkeyInHeadRange =
            Vector3.Distance(rig.ThreatPosition, aim) <= range
                + (rig.monkeyInHeadRange?tuning.HeadTrackingExitPaddingU:0f);
        rig.pursuitPaused = rig.state == FallingCobraState.Falling
            && !rig.monkeyInHeadRange;
        if (rig.monkeyInHeadRange) {
            rig.committedTargetPosition = aim;
            rig.hasCommittedTarget = true;
            Vector2 radial = new Vector2(aim.x, aim.z) - rig.axisXZ;
            rig.committedTargetAngle =
                Mathf.Atan2(radial.x, radial.y) * Mathf.Rad2Deg;
        }
        if (rig.state == FallingCobraState.Falling) {
            Vector3 head = rig.ThreatPosition;
            float rootAngle =
                Mathf.Atan2(rig.body.position.x - rig.axisXZ.x,
                    rig.body.position.z - rig.axisXZ.y) * Mathf.Rad2Deg;
            float headAngle =
                Mathf.Atan2(head.x - rig.axisXZ.x, head.z - rig.axisXZ.y)
                    * Mathf.Rad2Deg;
            if (!rig.headFeedbackReady) {
                rig.previousFeedbackAngle = headAngle;
                rig.headAngularSpeed = 0f;
                rig.headFeedbackReady = true;
            }
            float measured =
                Mathf.DeltaAngle(rig.previousFeedbackAngle, headAngle)
                    * Mathf.Deg2Rad * rig.ringRadiusU / Time.fixedDeltaTime;
            rig.previousFeedbackAngle = headAngle;
            measured =
                Mathf.Clamp(measured, -rig.horizontalSpeedU * 2f,
                    rig.horizontalSpeedU * 2f);
            rig.headAngularSpeed =
                Mathf.Lerp(rig.headAngularSpeed, measured,
                    1f - Mathf.Exp(-12f * Time.fixedDeltaTime));
            float aimError =
                Mathf.DeltaAngle(headAngle, rig.committedTargetAngle)
                    * Mathf.Deg2Rad * rig.ringRadiusU;
            rig.headAimCommand =
                Mathf.Clamp(tuning.HeadAimGain * aimError
                    - tuning.HeadAimDamping * rig.headAngularSpeed,
                        -rig.horizontalSpeedU, rig.horizontalSpeedU);
            float bias = Mathf.DeltaAngle(rootAngle, headAngle);
            rig.headSteeringBias =
                Mathf.LerpAngle(rig.headSteeringBias, bias,
                    1f - Mathf.Exp(-tuning.HeadSteeringSharpness
                        * Time.fixedDeltaTime));
            float drop = head.y - rig.committedTargetPosition.y;
            float desiredFall = 1f;
            if (rig.monkeyInHeadRange) {
                if (!rig.descentStopped
                    && drop <= tuning.TargetHeightStopMarginU) {
                    rig.descentStopped = true;
                    rig.stoppedTrailHeight = rig.displayedTrailHead.y;
                    rig.displayedTrailVelocity.y =
                        Mathf.Max(0f, rig.displayedTrailVelocity.y);
                }
                // Brake using the visible face height; the root pivot can be
                // well below the raised head.
                desiredFall = rig.descentStopped ? 0f
                    : Mathf.Clamp01((drop - tuning.TargetHeightStopMarginU)
                        * tuning.VerticalArrivalGain
                            / Mathf.Max(.01f, rig.fallSpeedU));
            }
            if (rig.monkeyInHeadRange && drop>-.25f
                && drop<tuning.ApproachDropRangeU) {
                float angularError =
                    Mathf.Abs(Mathf.DeltaAngle(headAngle,
                        rig.committedTargetAngle)) * Mathf.Deg2Rad
                            * rig.ringRadiusU;
                float offAim =
                    Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(tuning.HeadContactRadiusU * .6f,
                            tuning.HeadContactRadiusU * 1.6f, angularError));
                float near =
                    1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(1f, tuning.ApproachDropRangeU,
                            Mathf.Max(0f, drop)));
                desiredFall *=
                    Mathf.Lerp(1f, tuning.MinimumApproachFallScale,
                        offAim * near);
            }
            rig.approachFallScale = rig.descentStopped ? 0f
                : Mathf.Min(desiredFall,
                    Mathf.Lerp(rig.approachFallScale, desiredFall,
                        1f - Mathf.Exp(-10f * Time.fixedDeltaTime)));
        }
    }

    public float SteadyHeadAim(Vector3 position) {
        if (!rig.monkeyInHeadRange) {
            return 1f;
        }
        return Mathf.Lerp(tuning.CloseAimSwayScale, 1f,
            Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(Vector3.Distance(position,
                    rig.committedTargetPosition) / tuning.SteadyAimDistanceU)));
    }

    public bool IsMonkeyReaching() {
        if (rig.playerArms == null) {
            return false;
        }
        foreach (ArmController arm in rig.playerArms) {
            if (arm != null && arm.IsReaching) {
                return true;
            }
        }
        return false;
    }

    public Vector2 CommittedAttackVelocity(Vector3 position) {
        if (rig.pursuitPaused) {
            return Vector2.zero;
        }
        float sideways = 0f;
        if (rig.hasCommittedTarget && rig.ringRadiusU > MIN_RING_RADIUS_U) {
            Vector2 radial = new Vector2(position.x, position.z) - rig.axisXZ;
            float angle = Mathf.Atan2(radial.x, radial.y) * Mathf.Rad2Deg;
            float arc =
                Mathf.DeltaAngle(angle,
                    rig.committedTargetAngle - rig.headSteeringBias)
                        * Mathf.Deg2Rad * rig.ringRadiusU;
            float brake =
                Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(Mathf.Abs(arc)
                        / tuning.AttackSideBrakeDistanceU));
            float desired = Mathf.Sign(arc) * rig.horizontalSpeedU * brake;
            sideways =
                Mathf.Clamp(desired, -Mathf.Abs(arc) / Time.fixedDeltaTime,
                    Mathf.Abs(arc) / Time.fixedDeltaTime);
        }
        if (rig.monkeyInHeadRange) {
            sideways = rig.headAimCommand;
        }
        float age = Mathf.Max(0f, Time.time - rig.attackStartedTime);
        float release =
            Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(age / tuning.AttackSpeedRampS));
        float burst =
            Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(age / tuning.StrikeLaunchRampS))
            *Mathf.Lerp(tuning.StrikeSpeedMultiplier,1f,
                Mathf.SmoothStep(0f,1f,
                    Mathf.Clamp01(age/tuning.StrikeBurstDurationS)));
        release = Mathf.Max(release, burst);
        return new Vector2(sideways, -rig.fallSpeedU * rig.approachFallScale)
            * release;
    }

}
