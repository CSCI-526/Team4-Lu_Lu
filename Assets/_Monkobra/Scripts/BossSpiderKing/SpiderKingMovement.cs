using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>Moves the spider: idle, stalk, prowl, rush, cocoon, the stages, and
/// the tail.</summary>
public sealed class SpiderKingMovement {
    // private members  ########################################################
    private readonly SpiderKingRig rig;
    private readonly SpiderKingConfig config;

    public SpiderKingMovement(SpiderKingRig rig) {
        this.rig = rig;
        config = rig.config;
    }

    // Public Methods  #########################################################
    public void ResetSpiderState() {
        rig.mode = SpiderKingMode.Idle;
        rig.modeTimeS = 0f;
        rig.attack = SpiderKingAttack.None;
        rig.attackTimeS = 0f;
        rig.nextAttackTime = Time.time + 2f;
        rig.stunEndTime = 0f;
        rig.saveGraceEndTime = 0f;
        rig.websBeforeSlash = 0;
        rig.isDashing = false;
        rig.Attacks.ClearDashLane();
        rig.volleyLeft = 0;
        rig.lastStage = rig.Stage;
        rig.Attacks.ClearSlash();
        rig.nextDashTime = Time.time + 1f;
        rig.parrySlowEndTime = 0f;
        rig.tailTimeS = -1f;
        rig.isTeachFrozen = false;
        rig.frozenShot = null;
        rig.frozenCrescent = null;
        rig.Shots.ClearShots();
        rig.Melons.ClearThrows();
        if (rig.stuckWeb != null) {
            UnityEngine.Object.Destroy(rig.stuckWeb);
            rig.stuckWeb = null;
        }
        rig.body.SetEyeGlow(0f);
        rig.body.SetSwipe(0f);
        rig.body.SetEating(false);
    }

    // while it calls out it starts no new attack, only follows the monkey;
    // what it had begun is played out
    public void HoldForCallOut() {
        rig.callOutEndTime = Time.unscaledTime + BossFightHud.CALL_OUT_S;
    }

    // a point on an arc round the trunk, never through it
    public Vector3 ArcPoint(Vector3 from, Vector3 to, float t, float bulgeU) {
        float angleFrom = rig.AngleOf(from);
        float angle =
            angleFrom + Mathf.DeltaAngle(angleFrom, rig.AngleOf(to)) * t;
        float y = Mathf.Lerp(from.y, to.y, t);
        float radius =
            Mathf.Lerp(rig.HorizontalDistance(from), rig.HorizontalDistance(to),
            t) + Mathf.Sin(t * Mathf.PI) * bulgeU;
        return rig.PointOnTrunk(angle, y,
            Mathf.Max(radius, config.TrunkRadiusU + 0.3f));
    }

    // Spider  -----------------------------------------------------------------
    public void SetMode(SpiderKingMode next) {
        rig.mode = next;
        rig.modeTimeS = 0f;
    }

    public void UpdateSpider() {
        rig.modeTimeS += Time.deltaTime;
        float walk = 0f;
        bool isTrapped = rig.struggle != null && rig.struggle.IsTrapped;
        bool isStunned = Time.time < rig.stunEndTime;
        rig.Parry.UpdateStuckWeb(isStunned);

        // a trapped monkey is crawled to & wrapped up
        bool isDashCommitted = rig.isDashing
            || rig.attack == SpiderKingAttack.Dash;
        if (isTrapped && !isStunned && rig.teach == SpiderKingTeachStep.Done
            && rig.mode != SpiderKingMode.Cocooning && !isDashCommitted
                && !rig.isSlashing) {
            rig.Attacks.CancelAttack();
            SetMode(SpiderKingMode.Cocooning);
            rig.hud.Say("Hold still...");
        }

        float lastAngle = rig.spiderAngle;
        float lastY = rig.spiderY;
        if (rig.isSlashing) {
            walk = rig.Attacks.UpdateSlash();
        } else if (rig.isDashing) {
            walk = rig.Attacks.UpdateDash();
        } else if (rig.mode == SpiderKingMode.Cocooning) {
            walk = UpdateCocoon(isTrapped, isStunned);
        } else {
            walk = UpdateIdle(isStunned);
        }

        rig.Arena.LayWebs(rig.SurfaceDistance(lastAngle, lastY, rig.spiderAngle,
            rig.spiderY));

        // touching it is harmless; only its attacks hurt

        float glow = rig.mode == SpiderKingMode.Cocooning || rig.isDashing
            || rig.isSlashing
            ? 1f
            : rig.Attacks.AttackGlow();
        rig.body.SetEyeGlow(isStunned ? 0f : glow);
        if (rig.isSlashing && rig.slashChargeLeftS > 0f && !isStunned) {
            rig.body.SetEyeGlow(0.7f + 0.3f * Mathf.Sin(Time.time * 30f),
                config.WarnColor);
        }
        if (Time.unscaledTime < rig.biteEndTime) {
            float t = 1f - (rig.biteEndTime - Time.unscaledTime) / BITE_S;
            rig.body.SetSwipe(Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f);
            rig.body.SetEyeGlow(1f);
        }
        rig.body.Place(rig.spiderAngle, rig.spiderY, walk);
    }

    public float UpdateIdle(bool isStunned) {
        if (isStunned) {
            // stuck, it still fires out the volley it began
            if (rig.teach == SpiderKingTeachStep.Done
                && !rig.isReturningToStart) {
                rig.Attacks.UpdateVolley();
            }
            return 0f;
        }
        if (rig.attack != SpiderKingAttack.None) {
            rig.Attacks.UpdateAttack();
            return 0f;
        }
        if (rig.teach != SpiderKingTeachStep.Done) {
            return rig.Teaching.UpdateTeach();
        }
        if (rig.isReturningToStart) {
            return 0f;
        }
        rig.Attacks.UpdateVolley();
        bool isBusy = rig.HasLiveShots || rig.IsCallingOut
            || (rig.struggle != null && rig.struggle.IsTrapped);
        // its charge must be seen: out of view it rushes at the monkey
        bool isDue = Time.time >= rig.nextAttackTime && !isBusy;
        bool isRushing = isDue && !IsInView();
        float walk = isRushing ? Rush() : Stalk();
        // caught up: it always dashes at once, to be dodged (not while it
        // calls out)
        if (!isBusy && rig.Attacks.IsCloseEnoughToDash()) {
            rig.Attacks.BeginAttack(SpiderKingAttack.Dash, false);
            return 0f;
        }
        if (isDue && !isRushing) {
            rig.Attacks.StartAttack();
        }
        return walk;
    }

    public float Rush() {
        Vector3 at = rig.monkey.position;
        bool hasArrived = MoveToward(rig.AngleOf(at), at.y,
            config.RushSpeedU * rig.LoopSpeedScale, config.StalkDistanceU);
        return hasArrived ? 0f : 1f;
    }

    // if the spider, its charge, is on screen & on the trunk's near side
    public bool IsInView() {
        Camera view = Camera.main;
        if (view == null) {
            return true;
        }
        Vector3 head = rig.body.HeadPosition;
        Vector3 port = view.WorldToViewportPoint(head);
        bool isOnScreen = port.z > 0f
            && port.x > VIEW_EDGE && port.x < 1f - VIEW_EDGE
            && port.y > VIEW_EDGE && port.y < 1f - VIEW_EDGE;
        Vector3 toCamera = view.transform.position - rig.trunkCenter;
        toCamera.y = 0f;
        float radians = rig.spiderAngle * Mathf.Deg2Rad;
        Vector3 outward =
            new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        bool isNearSide = toCamera.sqrMagnitude < 0.001f
            || Vector3.Dot(outward, toCamera.normalized) > 0.45f;
        return isOnScreen && isNearSide;
    }

    // crawls after the monkey round the arena
    public float Stalk() {
        Vector3 at = rig.monkey.position;
        bool hasArrived =
            MoveToward(rig.AngleOf(at), at.y,
                config.StalkSpeedU * rig.ChaseScale,
            config.StalkDistanceU);
        if (!hasArrived) {
            return 0.6f;
        }
        Prowl(at);
        return 0.5f;
    }

    // never standing still b/t attacks: it circles the monkey at its
    // distance, now one way, now the other
    public void Prowl(Vector3 at) {
        if (Time.time >= rig.nextProwlFlipTime) {
            rig.prowlSign = -rig.prowlSign;
            rig.nextProwlFlipTime = Time.time + Random.Range(1.2f, 2.6f);
        }
        float arcU = Mathf.DeltaAngle(rig.spiderAngle, rig.AngleOf(at))
            * Mathf.Deg2Rad * config.TrunkRadiusU;
        float dy = at.y - rig.spiderY;
        float distance = Mathf.Sqrt(arcU * arcU + dy * dy);
        if (distance < 0.01f) {
            return;
        }
        // across the line to the monkey, & a pull back onto its distance
        float toU = arcU / distance;
        float toV = dy / distance;
        float speedU = config.StalkSpeedU * rig.ChaseScale * PROWL_SHARE;
        float pull =
            Mathf.Clamp(distance - config.StalkDistanceU, -1f, 1f) * 2f;
        float stepU =
            (-toV * rig.prowlSign * speedU + toU * pull) * Time.deltaTime;
        float stepV =
            (toU * rig.prowlSign * speedU + toV * pull) * Time.deltaTime;
        float nextY = rig.spiderY + stepV;
        // turned back at the arena's floor & top
        if (nextY < rig.ArenaBottomY
            || nextY > rig.entryY + config.ArenaHeightU) {
            rig.prowlSign = -rig.prowlSign;
            return;
        }
        rig.spiderAngle += stepU / config.TrunkRadiusU * Mathf.Rad2Deg;
        rig.spiderY = nextY;
    }

    public float UpdateCocoon(bool isTrapped, bool isStunned) {
        if (!isTrapped) {
            rig.hasBeenTrapped = true;
            SetMode(SpiderKingMode.Idle);
            rig.nextAttackTime = Time.time + RestNow() * 0.6f;
            return 0f;
        }
        if (isStunned) {
            return 0f;
        }
        Vector3 at = rig.monkey.position;
        float speedU = rig.hasBeenTrapped
            ? config.CocoonSpeedU
            : config.CocoonSpeedU * config.FirstCocoonSpeedScale;
        bool hasArrived =
            MoveToward(rig.AngleOf(at), at.y, speedU, config.CocoonReachU);
        if (hasArrived) {
            rig.Damage.LethalHit("Wrapped!");
            return 0f;
        }
        return 1f;
    }

    // moves along the bark, the shortest way round; true once within reach
    public bool MoveToward(
        float targetAngle,
        float targetY,
        float speedU,
        float reachU
    ) {
        targetY =
            Mathf.Clamp(targetY, rig.ArenaBottomY,
                rig.entryY + config.ArenaHeightU);
        float arcU = Mathf.DeltaAngle(rig.spiderAngle, targetAngle)
            * Mathf.Deg2Rad * config.TrunkRadiusU;
        float dy = targetY - rig.spiderY;
        float distance = Mathf.Sqrt(arcU * arcU + dy * dy);
        if (distance <= reachU) {
            return true;
        }
        float step = Mathf.Min(speedU * Time.deltaTime, distance - reachU);
        rig.spiderAngle +=
            arcU / distance * step / config.TrunkRadiusU * Mathf.Rad2Deg;
        rig.spiderY += dy / distance * step;
        return false;
    }

    // Attacks  ----------------------------------------------------------------
    // the weaker, the shorter the rests
    public float RestNow() {
        return config.RestS
            * Mathf.Lerp(config.MinRestShare, 1f,
                rig.health / (float)rig.maxHealth)
            * (rig.isEasy ? config.EasyTimeScale : 1f) * rig.LoopTimeScale;
    }

    public static int PerStage(Vector3Int values, int stage) =>
        Mathf.Max(1, stage == 0 ? values.x : stage == 1 ? values.y : values.z);

    public float DistanceToMonkey() {
        Vector3 at = rig.monkey.position;
        return rig.SurfaceDistance(rig.spiderAngle, rig.spiderY,
            rig.AngleOf(at), at.y);
    }

    // the tail glows gold through its swing, white at a clash
    public void UpdateTailTint() {
        if (rig.tail == null) {
            return;
        }
        bool isFlash = Time.unscaledTime < rig.tailFlashEndTime;
        bool isLit = isFlash || rig.Parry.IsParryArmed();
        if (!isLit) {
            if (rig.isTailTinted) {
                rig.isTailTinted = false;
                foreach (Renderer part in
                    rig.tail.GetComponentsInChildren<Renderer>(true)) {
                    part.SetPropertyBlock(null);
                }
            }
            return;
        }
        rig.isTailTinted = true;
        Color color = isFlash ? Color.white : config.GoldenColor;
        foreach (Renderer part in
            rig.tail.GetComponentsInChildren<Renderer>(true)) {
            rig.Feedback.SetColor(part, color);
        }
    }

    // the swing of the monkey's own tail: a quick swish side to side w/ a
    // flick up, back to its rest pose after
    public void StartTail() {
        if (rig.tail != null && rig.tailTimeS < 0f) {
            rig.tailRest = rig.tail.localRotation;
        }
        rig.hasSwingHit = false;
        rig.tailTimeS = 0f;
        SetTailColliders(false);
    }

    // the swinging tail's colliders are off, so the swing never shoves the
    // body or the grab; back on at rest
    public void SetTailColliders(bool isOn) {
        if (rig.tail == null) {
            return;
        }
        foreach (Collider part in
            rig.tail.GetComponentsInChildren<Collider>(true)) {
            part.enabled = isOn;
        }
    }

    // draws back, swishes hard across w/ a flick up, then swings back
    public void UpdateTail() {
        if (rig.tailTimeS < 0f) {
            return;
        }
        rig.tailTimeS += Time.unscaledDeltaTime;
        float total =
            config.TailWindUpS + config.TailActiveS + config.TailRecoverS;
        if (rig.tailTimeS >= total) {
            rig.tailTimeS = -1f;
            if (rig.tail != null) {
                rig.tail.localRotation = rig.tailRest;
            }
            SetTailColliders(true);
            return;
        }
        if (rig.tail == null) {
            return;
        }
        float swish;
        float flick;
        if (rig.tailTimeS < config.TailWindUpS) {
            float t = rig.tailTimeS / Mathf.Max(0.001f, config.TailWindUpS);
            swish = -45f * t;
            flick = -15f * t;
        } else if (rig.tailTimeS < config.TailWindUpS + config.TailActiveS) {
            float t = (rig.tailTimeS - config.TailWindUpS) / config.TailActiveS;
            float ease = 1f - (1f - t) * (1f - t);
            swish = Mathf.Lerp(-45f, 95f, ease);
            flick = -15f - 45f * Mathf.Sin(t * Mathf.PI);
        } else {
            float t = (rig.tailTimeS - config.TailWindUpS - config.TailActiveS)
                / Mathf.Max(0.001f, config.TailRecoverS);
            swish = Mathf.Lerp(95f, 0f, t * t * (3f - 2f * t));
            flick = Mathf.Lerp(-15f, 0f, t);
        }
        rig.tail.localRotation = rig.tailRest
            * Quaternion.AngleAxis(flick, Vector3.right)
            * Quaternion.AngleAxis(swish, Vector3.up);
    }

}
