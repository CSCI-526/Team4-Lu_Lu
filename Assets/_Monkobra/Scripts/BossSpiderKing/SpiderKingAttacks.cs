using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>The spider attacks: the volley, the dash, and the slash with their
/// aims and effects.</summary>
public sealed class SpiderKingAttacks {
    // Private Members  ########################################################
    private readonly SpiderKingRig rig;
    private readonly SpiderKingTuning tuning;

    public SpiderKingAttacks(SpiderKingRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    public float VolleyGap() {
        bool isSlow = (rig.volleyGapIndex % 2 == 0) == rig.isVolleySlowFirst;
        rig.volleyGapIndex++;
        // never shorter than a parry & a new double tap take
        return Mathf.Max(MIN_VOLLEY_GAP_S,
            (isSlow ? tuning.VolleySlowGapS : tuning.VolleyFastGapS)
                * rig.TimeScaleNow);
    }

    // the next bullet of a volley, its orb flashing at the head
    public void UpdateVolley() {
        if (rig.volleyCharge != null && !rig.shots.Contains(rig.volleyCharge)) {
            rig.volleyCharge = null;
        }
        if (rig.volleyLeft <= 0) {
            return;
        }
        // the gap is its new charge: the orb swells at the head again
        if (rig.volleyCharge == null) {
            rig.volleyCharge =
                rig.Shots.NewShot(SpiderKingShotKind.Web, rig.monkey.position);
            rig.volleyChargeFromTime = Time.time;
        }
        float chargeS =
            Mathf.Max(0.05f, rig.nextVolleyTime - rig.volleyChargeFromTime);
        float progress = Mathf.Clamp01((Time.time - rig.volleyChargeFromTime)
            / chargeS);
        rig.volleyCharge.target = rig.monkey.position;
        rig.volleyCharge.fill = progress;
        rig.body.SetCharge(progress);
        UpdateChargeSparks();
        if (Time.time < rig.nextVolleyTime) {
            return;
        }
        rig.volleyLeft--;
        rig.nextVolleyTime = Time.time + VolleyGap();
        SpiderKingShot bullet = rig.volleyCharge;
        rig.volleyCharge = null;
        rig.body.SetCharge(0f);
        bullet.isCharging = false;
        bullet.from = rig.body.HeadPosition;
        bullet.length = rig.Shots.PathLength(bullet);
        if (rig.volleyLeft == 0) {
            rig.nextAttackTime = Mathf.Max(Time.time + rig.Movement.RestNow(),
                rig.stunEndTime + 0.5f);
        }
    }

    public bool IsCloseEnoughToDash() {
        return Time.time >= rig.nextDashTime
            && rig.Movement.DistanceToMonkey() <= tuning.DashStartU;
    }

    // from afar: a web shot or a slash
    public void StartAttack() {
        if (rig.struggle != null && rig.struggle.IsTrapped) {
            return;
        }
        float distance = rig.Movement.DistanceToMonkey();
        bool canSlash = distance <= tuning.SlashRangeU
            && distance >= tuning.SlashMinRangeU;
        // far off a slash is likelier, it is what reaches
        float chance = distance > tuning.SlashMinRangeU * 2f
            ? Mathf.Max(tuning.SlashChance, 0.75f)
            : tuning.SlashChance;
        // a stage of volleys opens w/ a couple of them b4 any slash
        if (canSlash && rig.websBeforeSlash <= 0 && Random.value < chance) {
            StartSlash();
            return;
        }
        BeginAttack(SpiderKingAttack.Web, false);
    }

    // the wind up: the eyes glow; a web shot charges its orb, a dash rears
    // the legs up & shows its lane
    public void BeginAttack(SpiderKingAttack kind, bool isTeaching) {
        rig.attack = kind;
        rig.attackTimeS = 0f;
        float slow = isTeaching ? tuning.TeachSlowScale : 1f;
        rig.aimPoint = rig.monkey.position;
        switch (kind) {
            case SpiderKingAttack.Web:
                rig.attackWindUpS =
                    tuning.WebShotWindUpS * slow * rig.TimeScaleNow;
                rig.chargingShot =
                    rig.Shots.NewShot(SpiderKingShotKind.Web, rig.aimPoint);
                break;
            default:
                rig.attackWindUpS =
                    tuning.DashWindUpS * slow * rig.TimeScaleNow;
                rig.warningId = rig.hud.AddWarning(rig.aimPoint, "DASH!");
                break;
        }
        if (isTeaching) {
            rig.Teaching.ShowStepBanner();
        }
    }

    public float AttackGlow() {
        return rig.attack == SpiderKingAttack.None
            ? 0f
            : Mathf.Clamp01(rig.attackTimeS / rig.attackWindUpS);
    }

    public void UpdateAttack() {
        rig.attackTimeS += Time.deltaTime;
        float progress = Mathf.Clamp01(rig.attackTimeS / rig.attackWindUpS);
        rig.aimPoint = rig.monkey.position;
        if (rig.chargingShot != null) {
            rig.chargingShot.target = rig.aimPoint;
            rig.chargingShot.fill = progress;
        }
        if (rig.warningId >= 0) {
            rig.hud.UpdateWarning(rig.warningId, rig.monkey.position, progress);
        }
        if (rig.attack == SpiderKingAttack.Dash) {
            rig.body.SetSwipe(progress);
            bool isDashLocked =
                rig.attackTimeS >= rig.attackWindUpS - tuning.DashLockS;
            if (!isDashLocked) {
                AimDash();
            }
            DrawDashLane(isDashLocked);
        }
        if (rig.attack == SpiderKingAttack.Web) {
            rig.body.SetCharge(progress);
            UpdateChargeSparks();
        }
        if (rig.attackTimeS < rig.attackWindUpS) {
            return;
        }

        SpiderKingAttack fired = rig.attack;
        SpiderKingShot shot = rig.chargingShot;
        rig.chargingShot = null;
        CancelAttack(fired == SpiderKingAttack.Dash);
        if (fired != SpiderKingAttack.Dash) {
            rig.nextAttackTime = Time.time
                + (rig.teach == SpiderKingTeachStep.Done
                    ? rig.Movement.RestNow() : tuning.TeachGapS);
        }
        switch (fired) {
            case SpiderKingAttack.Web:
                if (shot != null) {
                    shot.isCharging = false;
                    shot.from = rig.body.HeadPosition;
                    shot.length = rig.Shots.PathLength(shot);
                }
                // later a volley: the rest follow, slow then fast or fast
                // then slow, each to bat back
                if (rig.teach == SpiderKingTeachStep.Done) {
                    rig.websBeforeSlash = Mathf.Max(0, rig.websBeforeSlash - 1);
                    rig.volleyLeft =
                        SpiderKingMovement.PerStage(tuning.WebShotsPerStage,
                            rig.Stage) - 1;
                    rig.volleyGapIndex = 0;
                    rig.isVolleySlowFirst = Random.value < 0.5f;
                    rig.nextVolleyTime = Time.time + VolleyGap();
                }
                break;
            case SpiderKingAttack.Dash:
                StartDash();
                break;
        }
    }

    public void CancelAttack(bool isKeepingLane = false) {
        rig.attack = SpiderKingAttack.None;
        rig.attackTimeS = 0f;
        if (rig.warningId >= 0) {
            rig.hud.RemoveWarning(rig.warningId);
            rig.warningId = -1;
        }
        rig.body.SetSwipe(0f);
        rig.body.SetCharge(0f);
        if (rig.chargingShot != null) {
            rig.Shots.RemoveShot(rig.chargingShot);
            rig.chargingShot = null;
        }
        if (!isKeepingLane) {
            ClearDashLane();
        }
    }

    // Dash  -------------------------------------------------------------------
    // a fast charge along the bark to where the monkey was, a bit past it;
    // touching the monkey on the way knocks it down
    // the dash runs to where the lane was locked
    public void StartDash() {
        rig.isDashing = true;
        rig.hasDashHit = false;
        rig.dashTimeS = 0f;
        rig.dashTravelU = 0f;
        rig.body.SetSwipe(1f);
    }

    // the dash's end: where the monkey is, a bit past it; the start, way &
    // length are kept, so the dash runs exactly along the lane drawn
    public void AimDash() {
        Vector3 at = rig.monkey.position;
        float arcU =
            Mathf.DeltaAngle(rig.spiderAngle, rig.AngleOf(at)) * Mathf.Deg2Rad
            * tuning.TrunkRadiusU;
        float dy = at.y - rig.spiderY;
        float distance = Mathf.Max(0.01f, Mathf.Sqrt(arcU * arcU + dy * dy));
        rig.dashStartAngle = rig.spiderAngle;
        rig.dashStartY = rig.spiderY;
        rig.dashWayU = arcU / distance;
        rig.dashWayV = dy / distance;
        rig.dashLengthU = distance + tuning.DashOvershootU;
        // cut where it would leave the arena, so the lane is the whole run
        if (rig.dashWayV > 0.001f) {
            rig.dashLengthU = Mathf.Min(rig.dashLengthU,
                (rig.entryY + tuning.ArenaHeightU - rig.spiderY)
                    / rig.dashWayV);
        } else if (rig.dashWayV < -0.001f) {
            rig.dashLengthU = Mathf.Min(rig.dashLengthU,
                (rig.ArenaBottomY - rig.spiderY) / rig.dashWayV);
        }
        rig.dashLengthU = Mathf.Max(0.5f, rig.dashLengthU);
        rig.dashAngle = rig.spiderAngle
            + rig.dashWayU * rig.dashLengthU / tuning.TrunkRadiusU
                * Mathf.Rad2Deg;
        rig.dashY = rig.spiderY + rig.dashWayV * rig.dashLengthU;
    }

    // a red lane on the bark from the spider to the dash's end, as wide as
    // the dash catches; dim while it aims, bright & flashing once locked
    // the lane: exactly where the dash catches, its run (dashLengthU) plus
    // the catch reach (dashHitU) past its end, dashHitU to either side
    public void DrawDashLane(bool isLocked) {
        if (rig.dashLane == null) {
            rig.dashLane = new Transform[PATH_PARTS * 2];
            for (int i = 0; i < rig.dashLane.Length; i++) {
                rig.dashLane[i] =
                    rig.Feedback.CreatePrimitive(PrimitiveType.Cylinder,
                        "DashLane",
                    Vector3.one * 0.1f, tuning.WarnColor).transform;
            }
        }
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 30f);
        Color color = isLocked
            ? Color.Lerp(tuning.WarnColor, Color.white, pulse * 0.5f)
            : tuning.WarnColor * 0.6f;
        float thickness = isLocked ? 0.22f : 0.12f;
        float reach = rig.dashLengthU + tuning.DashHitU;
        // across the way, on the bark
        float acrossU = -rig.dashWayV * tuning.DashHitU;
        float acrossV = rig.dashWayU * tuning.DashHitU;
        for (int edge = 0; edge < 2; edge++) {
            float sign = edge == 0 ? -1f : 1f;
            for (int i = 0; i < PATH_PARTS; i++) {
                float d0 = reach * i / PATH_PARTS;
                float d1 = reach * (i + 1) / PATH_PARTS;
                Vector3 from = BarkPoint(rig.dashStartAngle, rig.dashStartY,
                    rig.dashWayU * d0 + acrossU * sign,
                        rig.dashWayV * d0 + acrossV * sign);
                Vector3 to = BarkPoint(rig.dashStartAngle, rig.dashStartY,
                    rig.dashWayU * d1 + acrossU * sign,
                        rig.dashWayV * d1 + acrossV * sign);
                SetDashLanePart(edge * PATH_PARTS + i, from, to, thickness,
                    color);
            }
        }
    }

    public void SetDashLanePart(int index, Vector3 from, Vector3 to,
        float thickness, Color color) {
        SpiderKingFeedback.SetLine(rig.dashLane[index], from, to, thickness);
        rig.Feedback.SetColor(rig.dashLane[index].GetComponent<Renderer>(),
            color);
    }

    public void ClearDashLane() {
        if (rig.dashLane == null) {
            return;
        }
        foreach (Transform part in rig.dashLane) {
            if (part != null) {
                UnityEngine.Object.Destroy(part.gameObject);
            }
        }
        rig.dashLane = null;
    }

    public float UpdateDash() {
        rig.dashTimeS += Time.deltaTime;
        // straight along the locked lane, never turning; the arena's top or
        // bottom ends it early
        rig.dashTravelU = Mathf.Min(rig.dashLengthU,
            rig.dashTravelU + tuning.DashSpeedU * rig.SpeedScale
                * Time.deltaTime);
        float nextY = rig.dashStartY + rig.dashWayV * rig.dashTravelU;
        bool isOutOfArena = nextY < rig.ArenaBottomY - 0.01f
            || nextY > rig.entryY + tuning.ArenaHeightU + 0.01f;
        if (!isOutOfArena) {
            rig.spiderAngle = rig.dashStartAngle
                + rig.dashWayU * rig.dashTravelU / tuning.TrunkRadiusU
                    * Mathf.Rad2Deg;
            rig.spiderY = nextY;
        }
        bool hasArrived = rig.dashTravelU >= rig.dashLengthU || isOutOfArena;
        if (!rig.hasDashHit && rig.Movement.DistanceToMonkey() < tuning.DashHitU
            && Time.time >= rig.saveGraceEndTime) {
            rig.hasDashHit = true;
            rig.Damage.LethalHit("Caught!", false, true);
        }
        if (!hasArrived && rig.dashTimeS < 1.5f) {
            return 1f;
        }
        rig.isDashing = false;
        rig.body.SetSwipe(0f);
        ClearDashLane();
        rig.nextDashTime =
            Time.time
                + Mathf.Lerp(tuning.DashCooldownS, tuning.LateDashCooldownS,
            rig.Rage);
        if (rig.teach == SpiderKingTeachStep.Done) {
            rig.nextAttackTime =
                Mathf.Max(rig.nextAttackTime, Time.time + 0.8f);
        }
        return 0f;
    }

    // Slash  ------------------------------------------------------------------
    // from afar, after a charge it is seen at, its front legs slash three
    // times, each first a red aim line on the bark, then a silk blade that
    // flies fast along the bark at where the monkey was; the gaps b/t
    // slashes go slow then fast or fast then slow. Fatal, unless dodged or
    // batted back w/ the tail as it turns gold, which hurts the spider
    public void StartSlash(bool isTeaching = false) {
        rig.isSlashing = true;
        rig.slashLeft = isTeaching ? 1
            : SpiderKingMovement.PerStage(tuning.SlashesPerStage, rig.Stage);
        rig.slashSlow = isTeaching ? tuning.TeachSlowScale : 1f;
        rig.slashChargeLeftS = tuning.SlashChargeS * rig.slashSlow;
        rig.slashGapIndex = 0;
        rig.isSlashSlowFirst = Random.value < 0.5f;
        rig.nextSlashTime = Time.time;
        rig.isSlashAiming = false;
    }

    public float UpdateSlash() {
        // the charge, seen like the web shot's
        if (rig.slashChargeLeftS > 0f) {
            rig.slashChargeLeftS -= Time.deltaTime;
            float charge =
                1f - rig.slashChargeLeftS
                    / (tuning.SlashChargeS * rig.slashSlow);
            rig.body.SetCharge(charge);
            rig.body.SetSwipe(charge);
            UpdateSlashChargeFx(charge);
            if (rig.slashChargeLeftS <= 0f) {
                rig.body.SetCharge(0f);
                HideSlashSigil();
            }
            return 0f;
        }
        if (!rig.isSlashAiming) {
            if (Time.time < rig.nextSlashTime) {
                return 0f;
            }
            rig.slashTimeS = 0f;
            rig.isSlashAiming = true;
        }
        rig.slashTimeS += Time.deltaTime;
        // the aim follows the monkey till the slash
        rig.slashAimDeg = AimDegToMonkey();
        float windUpS = tuning.SlashWindUpS * rig.slashSlow * rig.TimeScaleNow;
        float progress = Mathf.Clamp01(rig.slashTimeS / windUpS);
        rig.body.SetSwipe(progress);
        DrawSlashAim(progress);
        if (rig.slashTimeS < windUpS) {
            return 0f;
        }

        rig.isSlashAiming = false;
        rig.body.SetSwipe(0f);
        ShowSlashAim(false);
        rig.Shots.LaunchCrescent(rig.slashAimDeg);
        rig.slashLeft--;
        if (rig.slashLeft <= 0) {
            rig.isSlashing = false;
            rig.nextAttackTime = Time.time
                + (rig.teach == SpiderKingTeachStep.Done
                    ? rig.Movement.RestNow() : tuning.TeachGapS);
            return 0f;
        }
        bool isSlow = (rig.slashGapIndex % 2 == 0) == rig.isSlashSlowFirst;
        rig.slashGapIndex++;
        rig.nextSlashTime = Time.time
            + (isSlow ? tuning.SlashSlowGapS : tuning.SlashFastGapS)
                * rig.TimeScaleNow;
        return 0f;
    }

    // the direction from the spider to the monkey on the bark; deg, 0 round
    // the trunk, 90 up
    public float AimDegToMonkey() {
        Vector3 at = rig.monkey.position;
        float arcU =
            Mathf.DeltaAngle(rig.spiderAngle, rig.AngleOf(at)) * Mathf.Deg2Rad
            * tuning.TrunkRadiusU;
        return Mathf.Atan2(at.y - rig.spiderY, arcU) * Mathf.Rad2Deg;
    }

    // a point on the bark, u round the trunk & v up from a base spot
    public Vector3 BarkPoint(float baseAngle, float baseY, float u, float v) {
        return rig.PointOnTrunk(baseAngle + u / tuning.TrunkRadiusU
            * Mathf.Rad2Deg,
            baseY + v, tuning.TrunkRadiusU + 0.12f);
    }

    // the aim: a thin red line along the bark out to the slash's reach,
    // brighter as the slash nears
    public void DrawSlashAim(float progress) {
        if (rig.slashAim == null) {
            rig.slashAim = new Transform[PATH_PARTS];
            for (int i = 0; i < rig.slashAim.Length; i++) {
                rig.slashAim[i] =
                    rig.Feedback.CreatePrimitive(PrimitiveType.Cylinder,
                        "SlashAim",
                    Vector3.one * 0.1f, tuning.WarnColor).transform;
            }
        }
        ShowSlashAim(true);
        float radians = rig.slashAimDeg * Mathf.Deg2Rad;
        float du = Mathf.Cos(radians);
        float dv = Mathf.Sin(radians);
        Color color =
            Color.Lerp(tuning.WarnColor * 0.5f, tuning.WarnColor, progress);
        for (int i = 0; i < PATH_PARTS; i++) {
            // dashed: every other part
            bool isShown = i % 2 == 0;
            rig.slashAim[i].gameObject.SetActive(isShown);
            float d0 = tuning.SlashRangeU * i / PATH_PARTS;
            float d1 = tuning.SlashRangeU * (i + 1) / PATH_PARTS;
            SpiderKingFeedback.SetLine(rig.slashAim[i],
                BarkPoint(rig.spiderAngle, rig.spiderY, du * d0, dv * d0),
                BarkPoint(rig.spiderAngle, rig.spiderY, du * d1, dv * d1),
                Mathf.Lerp(0.06f, 0.14f, progress));
            rig.Feedback.SetColor(rig.slashAim[i].GetComponent<Renderer>(),
                color);
        }
    }

    public void ShowSlashAim(bool isShown) {
        if (rig.slashAim == null) {
            return;
        }
        foreach (Transform part in rig.slashAim) {
            if (part != null) {
                part.gameObject.SetActive(isShown);
            }
        }
    }

    // the slashes still to come stop; crescents in flight fly on
    public void StopSlashing() {
        HideSlashSigil();
        rig.isSlashing = false;
        rig.isSlashAiming = false;
        rig.slashChargeLeftS = 0f;
        rig.body.SetCharge(0f);
        ShowSlashAim(false);
        rig.body.SetSwipe(0f);
    }

    // all of it gone, crescents too
    public void ClearSlash() {
        StopSlashing();
        if (rig.slashAim != null) {
            foreach (Transform part in rig.slashAim) {
                if (part != null) {
                    UnityEngine.Object.Destroy(part.gameObject);
                }
            }
        }
        rig.slashAim = null;
        while (rig.crescents.Count > 0) {
            rig.Shots.RemoveCrescent(rig.crescents[0]);
        }
    }

    // white sparks & rings drawn in to the head while it charges
    // the web shot's charge: calm & white, silk drawn in to the orb
    public void UpdateChargeSparks() {
        rig.chargeRingS -= Time.deltaTime;
        if (rig.chargeRingS <= 0f) {
            rig.chargeRingS = 0.16f;
            rig.Parry.AddRing(rig.body.HeadPosition, tuning.SilkColor, 0f,
                true);
        }
        rig.chargeSparkS -= Time.deltaTime;
        if (rig.chargeSparkS > 0f) {
            return;
        }
        rig.chargeSparkS = 0.03f;
        Vector3 head = rig.body.HeadPosition;
        Vector3 start = head + Random.onUnitSphere * 2.2f;
        Transform spark =
            rig.Feedback.CreatePrimitive(PrimitiveType.Cube, "Spark",
            Vector3.one * 0.15f, Color.white).transform;
        spark.position = start;
        rig.sparks.Add(new SpiderKingSpark {
            transform = spark,
            velocity = (head - start) / 0.25f,
            lifeS = 0.25f,
            isFloating = true,
        });
    }

    // the slash's charge: angry & red, nothing like the web shot's: a red
    // blade sign over its head grows & flickers, red rings burst out & red
    // streaks spit off it, its eyes blaze red
    public void UpdateSlashChargeFx(float charge) {
        Vector3 head = rig.body.HeadPosition;
        rig.chargeRingS -= Time.deltaTime;
        if (rig.chargeRingS <= 0f) {
            rig.chargeRingS = 0.2f;
            rig.Parry.AddRing(head, tuning.WarnColor, 0f);
        }
        rig.chargeSparkS -= Time.deltaTime;
        if (rig.chargeSparkS <= 0f) {
            rig.chargeSparkS = 0.06f;
            rig.Parry.BurstStreaks(head, tuning.WarnColor, 2, 3f, 7f);
        }
        if (rig.slashSigil == null) {
            rig.slashSigil = new Transform[SIGIL_PARTS];
            for (int i = 0; i < SIGIL_PARTS; i++) {
                rig.slashSigil[i] =
                    rig.Feedback.CreatePrimitive(PrimitiveType.Cylinder,
                    "SlashSign", Vector3.one, tuning.WarnColor).transform;
            }
        }
        // a crescent facing the camera, opening up over its head
        Quaternion facing = rig.Parry.ViewRotation();
        Vector3 right = facing * Vector3.right;
        Vector3 up = facing * Vector3.up;
        float flicker = 0.75f + 0.25f * Mathf.Sin(Time.time * 45f);
        float radius = Mathf.Lerp(0.6f, 1.8f, charge);
        Vector3 center = head + up * (0.6f + 0.5f * charge);
        float spread = Mathf.Lerp(40f, 150f, charge) * Mathf.Deg2Rad;
        Color color = Color.Lerp(tuning.WarnColor, Color.white,
            0.25f * (1f - flicker));
        for (int i = 0; i < SIGIL_PARTS; i++) {
            float a0 = -spread * 0.5f + spread * i / SIGIL_PARTS;
            float a1 = -spread * 0.5f + spread * (i + 1) / SIGIL_PARTS;
            Vector3 p0 = center + (right * Mathf.Sin(a0) + up * Mathf.Cos(a0))
                * radius;
            Vector3 p1 = center + (right * Mathf.Sin(a1) + up * Mathf.Cos(a1))
                * radius;
            // thick in the middle, sharp at the tips
            float mid = 1f - Mathf.Abs((i + 0.5f) / SIGIL_PARTS * 2f - 1f);
            rig.slashSigil[i].gameObject.SetActive(true);
            SpiderKingFeedback.SetLine(rig.slashSigil[i], p0, p1,
                (0.05f + 0.2f * mid) * (0.5f + 0.5f * charge) * flicker);
            rig.Feedback.SetColor(rig.slashSigil[i].GetComponent<Renderer>(),
                color);
        }
    }

    public void HideSlashSigil() {
        if (rig.slashSigil == null) {
            return;
        }
        foreach (Transform part in rig.slashSigil) {
            if (part != null) {
                UnityEngine.Object.Destroy(part.gameObject);
            }
        }
        rig.slashSigil = null;
    }

}
