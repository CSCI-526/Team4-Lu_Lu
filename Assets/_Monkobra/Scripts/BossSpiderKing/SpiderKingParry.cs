using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>The parry, the clash effects, the parry camera, and the time
/// scale.</summary>
public sealed class SpiderKingParry {
    // private members  ########################################################
    private readonly SpiderKingRig rig;
    private readonly SpiderKingTuning tuning;

    public SpiderKingParry(SpiderKingRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    // Parry  ------------------------------------------------------------------
    // a double tap of Space swings the monkey's own tail: for a moment a gold
    // web wave close by is batted back, & the spider is stuck in it
    public void UpdateParryInput(GameController game) {
        rig.Movement.UpdateTail();
        bool isCaught = rig.struggle != null && rig.struggle.IsTrapped;
        // in a lesson's stop the double tap is always the parry
        if (isCaught && !rig.isTeachFrozen) {
            rig.lastTapTime = -10f;
            return;
        }
        if (game.IsPaused
            || !SpiderKingFeedback.WasPressed(tuning.ConfirmAction)) {
            return;
        }
        // only w/ something to bat on its way (or charging): a double tap
        // for a grab otherwise never swings the tail
        bool isComing = rig.shots.Exists(s => !s.isReflected)
            || rig.crescents.Exists(c => !c.isReflected) || rig.isSlashing;
        if (!isComing) {
            rig.lastTapTime = -10f;
            return;
        }
        float now = Time.unscaledTime;
        // a swing can't start till the last one is over, unless that one
        // hit: a parry readies the tail at once, so a chain of slashes can
        // all be batted back, but a whiff waits its whole follow through
        bool isSwinging = rig.tailTimeS >= 0f && !rig.hasSwingHit;
        if (now - rig.lastTapTime <= tuning.DoubleTapS && !isSwinging) {
            rig.lastTapTime = -10f;
            rig.Movement.StartTail();
            if (rig.isTeachFrozen) {
                ParryFrozen();
            }
        } else {
            rig.lastTapTime = now;
        }
    }

    // the lesson's frozen bullet or slash is batted back at once
    public void ParryFrozen() {
        rig.isTeachFrozen = false;
        rig.hud.SetPrompt(false, Vector3.zero);
        if (rig.frozenShot != null && rig.frozenShot.wave != null) {
            SpiderKingShot shot = rig.frozenShot;
            rig.frozenShot = null;
            OnParry(shot, shot.wave.position);
        } else if (rig.frozenCrescent != null) {
            SpiderKingCrescent crescent = rig.frozenCrescent;
            rig.frozenCrescent = null;
            OnCrescentParried(crescent);
        }
    }

    // a parry plays in slow motion w/ the camera close in on the monkey,
    // no flash: "Nice!" & gold sparks
    public void ParryEffect(Vector3 at) {
        // more on its way (a volley's next bullet, the next slash): only a
        // short beat, the camera barely in, so the next one is seen in time
        bool isMore = IsMoreComing();
        rig.parrySlowEndTime = Time.unscaledTime
            + (isMore ? QUICK_SLOW_S : tuning.ParrySlowS);
        rig.shotMaxWeight = isMore ? QUICK_SHOT_WEIGHT : 1f;
        // a shot already on goes on w/o easing in again
        float now = Time.unscaledTime;
        bool isShotOn = now < rig.shotEndTime;
        rig.hasShotPose = isShotOn && rig.hasShotPose;
        rig.shotStartTime = isShotOn
            ? Mathf.Min(rig.shotStartTime, now - SHOT_IN_S)
            : now;
        rig.shotEndTime = rig.parrySlowEndTime + SHOT_OUT_S;
        if (!isShotOn) {
            rig.shotWeightCap = rig.shotMaxWeight;
        }
        rig.shotFrom = at;
        rig.shotSide = isShotOn ? rig.shotSide
            : (Random.value < 0.5f ? -1f : 1f);
        rig.shotClashTime = -10f;
    }

    // anything still to bat or dodge: a bullet or slash coming, a volley or
    // a row of slashes not done
    public bool IsMoreComing() => rig.shots.Exists(s => !s.isReflected)
        || rig.crescents.Exists(c => !c.isReflected) || rig.isSlashing
        || rig.volleyLeft > 0 || (rig.chargingShot != null);

    // a full parry shot is cut short the moment another attack comes, so it
    // never hides it
    public void CutShotIfMoreComes() {
        float now = Time.unscaledTime;
        if (now >= rig.shotEndTime || rig.shotMaxWeight < 1f
            || !IsMoreComing()) {
            return;
        }
        rig.shotMaxWeight = QUICK_SHOT_WEIGHT;
        rig.parrySlowEndTime = Mathf.Min(rig.parrySlowEndTime, now + 0.1f);
        rig.shotEndTime = Mathf.Min(rig.shotEndTime, now + SHOT_OUT_S * 0.6f);
    }

    // The Parry Shot  ---------------------------------------------------------
    // a camera move laid over the follow camera just b4 it draws & taken off
    // after, so it never fights Cinemachine & runs in real time while time
    // crawls or stands: it swoops in close to the monkey, swinging round it
    // & tilting, slams in & shakes at the clash, pans after the thing kicked
    // back at the spider, then eases back out
    public void OnBeginCamera(ScriptableRenderContext context, Camera view) {
        if (view == null || view != Camera.main || rig.monkey == null) {
            return;
        }
        float now = Time.unscaledTime;
        if (now >= rig.shotEndTime) {
            return;
        }
        Transform eye = view.transform;
        float t = now - rig.shotStartTime;
        float total = rig.shotEndTime - rig.shotStartTime;
        // eased down, not jumped, when cut short for a coming attack
        rig.shotWeightCap =
            Mathf.MoveTowards(rig.shotWeightCap, rig.shotMaxWeight,
            Time.unscaledDeltaTime * 4f);
        float weight = Mathf.Min(Ease(t / SHOT_IN_S),
            Ease((rig.shotEndTime - now) / SHOT_OUT_S)) * rig.shotWeightCap;
        if (weight <= 0f) {
            return;
        }
        rig.isShotApplied = true;
        rig.restEyePosition = eye.position;
        rig.restEyeRotation = eye.rotation;
        rig.restFov = view.fieldOfView;

        Vector3 focus = rig.monkey.position;
        Vector3 back = eye.position - focus;
        if (back.sqrMagnitude < 0.01f) {
            back = Vector3.back;
        }
        back.Normalize();
        // a slow swing round the monkey all through the slow motion
        float progress = Mathf.Clamp01(t / Mathf.Max(0.01f, total));
        float orbit =
            rig.shotSide * tuning.ParryOrbitDeg * (0.35f + 0.65f * progress);
        Vector3 way = Quaternion.AngleAxis(orbit, Vector3.up) * back;
        way = (way + Vector3.up * 0.15f).normalized;
        float distance = tuning.ParryCloseUpU;
        float roll = rig.shotSide * tuning.ParryRollDeg;
        float fovScale = 1f;
        // the clash: a slam in, a roll kick, a shake
        float sinceClash = now - rig.shotClashTime;
        Vector3 shake = Vector3.zero;
        if (sinceClash >= 0f && sinceClash < SHOT_SLAM_S) {
            float hit = 1f - sinceClash / SHOT_SLAM_S;
            hit *= hit;
            distance -= 1.1f * hit;
            roll -= rig.shotSide * 6f * hit;
            fovScale -= 0.12f * hit;
            shake = Random.insideUnitSphere * 0.18f * hit;
        }
        // looking a bit toward where the thing came from, then after it as
        // it is kicked back at the spider
        Vector3 look = Vector3.Lerp(focus, rig.shotFrom, 0.2f);
        if (sinceClash >= 0f && rig.body != null) {
            float pan = Ease((sinceClash - CLASH_HOLD_S) / CLASH_OUT_S);
            look = Vector3.Lerp(look,
                Vector3.Lerp(focus, rig.body.HeadPosition, 0.45f), pan);
            distance += 0.9f * pan;
        }
        Vector3 shotPosition = focus + way * distance + shake;
        Quaternion shotRotation = Quaternion.LookRotation(
            look - shotPosition, Vector3.up)
            * Quaternion.AngleAxis(roll, Vector3.forward);
        eye.SetPositionAndRotation(
            Vector3.Lerp(rig.restEyePosition, shotPosition, weight),
            Quaternion.Slerp(rig.restEyeRotation, shotRotation, weight));
        view.fieldOfView =
            Mathf.Lerp(rig.restFov, rig.restFov * fovScale, weight);
        rig.shotViewRotation = eye.rotation;
        rig.hasShotPose = true;
    }

    // which way the camera looks as drawn: the parry shot's while it is on
    public Quaternion ViewRotation() {
        if (Time.unscaledTime < rig.shotEndTime && rig.hasShotPose) {
            return rig.shotViewRotation;
        }
        Camera view = Camera.main;
        return view != null ? view.transform.rotation : Quaternion.identity;
    }

    // the follow camera's own pose back after the draw
    public void OnEndCamera(ScriptableRenderContext context, Camera view) {
        if (!rig.isShotApplied || view == null || view != Camera.main) {
            return;
        }
        rig.isShotApplied = false;
        view.transform.SetPositionAndRotation(rig.restEyePosition,
            rig.restEyeRotation);
        view.fieldOfView = rig.restFov;
    }

    public static float Ease(float share) {
        share = Mathf.Clamp01(share);
        return share * share * (3f - 2f * share);
    }

    // a batted back slash flies back at the spider & hurts it
    public void OnCrescentParried(SpiderKingCrescent crescent) {
        rig.hasSwingHit = true;
        crescent.isReflected = true;
        crescent.clashS = 0f;
        crescent.hasClashed = false;
        crescent.parryDistanceU = crescent.distanceU;
        // its middle meets the monkey's near edge
        float ahead =
            rig.Shots.CrescentAhead(crescent, out float _) - MONKEY_EDGE_U;
        crescent.contactDistanceU = crescent.distanceU + Mathf.Max(0f, ahead);
        ParryEffect(rig.Shots.CrescentPoint(crescent, 0f));
        if (!rig.hasTaughtSlash) {
            rig.hasTaughtSlash = true;
            rig.hud.HideBanner();
        }
    }

    // where a batted thing is met: the monkey's edge on the side it comes
    // from
    public Vector3 EdgeToward(Vector3 from) {
        Vector3 way = from - rig.monkey.position;
        return way.sqrMagnitude > 0.0001f
            ? rig.monkey.position + way.normalized * MONKEY_EDGE_U
            : rig.monkey.position;
    }

    // a slash's: the monkey's edge facing back along the slash's way
    public Vector3 CrescentContact(SpiderKingCrescent crescent) {
        float radians = crescent.directionDeg * Mathf.Deg2Rad;
        Vector3 back =
            rig.Attacks.BarkPoint(rig.AngleOf(rig.monkey.position),
                rig.monkey.position.y,
            -Mathf.Cos(radians) * MONKEY_EDGE_U,
            -Mathf.Sin(radians) * MONKEY_EDGE_U);
        back = rig.monkey.position
            + (back - rig.PointOnTrunk(rig.AngleOf(rig.monkey.position),
            rig.monkey.position.y, tuning.TrunkRadiusU + 0.12f));
        return back;
    }

    // the middle of the monkey's body, a hair out toward the camera side
    public Vector3 TailContact() {
        Vector3 outward = rig.monkey.position - rig.trunkCenter;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 0.001f ? outward.normalized
            : Vector3.forward;
        return rig.monkey.position + outward * 0.25f;
    }

    // the clash: sparks flying off the tail where the thing meets it, &
    // "Nice!"
    public void Clash(Vector3 at) {
        rig.clashStopEndTime = Time.unscaledTime + CLASH_STOP_S;
        rig.shotClashTime = Time.unscaledTime;
        rig.shakeEndTime = Time.unscaledTime + SHAKE_S;
        rig.hud.PopText("Nice!", at, UIStyle.Gold);
        AddCross(at);
        BurstStreaks(at, new Color(1f, 0.55f, 0.1f), 26, 6f, 15f);
        BurstStreaks(at, new Color(1f, 0.9f, 0.4f), 16, 4f, 10f);
        AddRing(at, tuning.GoldenColor, 0f);
        AddRing(at, new Color(1f, 0.55f, 0.1f), 0.07f);
        AddPop(at);
        rig.tailFlashEndTime = Time.unscaledTime + 0.15f;
    }

    // two long white strokes crossing at the clash, a hard flash of it
    public void AddCross(Vector3 at) {
        Vector3 right = ViewRotation() * Vector3.right;
        Vector3 up = ViewRotation() * Vector3.up;
        for (int i = 0; i < 2; i++) {
            Vector3 way = (right + (i == 0 ? up : -up)).normalized;
            Transform stroke =
                rig.Feedback.CreatePrimitive(PrimitiveType.Cylinder, "Cross",
                Vector3.one, Color.white).transform;
            SpiderKingFeedback.SetLine(stroke, at - way * 0.9f, at + way * 0.9f,
                0.06f);
            rig.pops.Add(new SpiderKingSpark {
                transform = stroke,
                lifeS = 0.12f,
                isStroke = true,
                size = 0.06f,
            });
        }
    }

    // sparks that streak: long thin bits along their flight
    public void BurstStreaks(Vector3 at, Color color, int count,
        float minSpeedU, float maxSpeedU) {
        for (int i = 0; i < count; i++) {
            Transform spark =
                rig.Feedback.CreatePrimitive(PrimitiveType.Cube, "Streak",
                Vector3.one * 0.1f, color).transform;
            spark.position = at;
            rig.sparks.Add(new SpiderKingSpark {
                transform = spark,
                velocity = Random.onUnitSphere
                    * Random.Range(minSpeedU, maxSpeedU),
                lifeS = Random.Range(0.25f, 0.5f),
                isStreak = true,
            });
        }
    }

    // a still bead that fades, eg the trail of a batted back thing
    public void AddTrailBead(Vector3 at) {
        Transform bead =
            rig.Feedback.CreatePrimitive(PrimitiveType.Sphere, "TrailBead",
            Vector3.one * 0.3f, tuning.GoldenColor).transform;
        bead.position = at;
        rig.sparks.Add(new SpiderKingSpark {
            transform = bead,
            velocity = Vector3.zero,
            lifeS = 0.25f,
            isFloating = true,
            size = 0.35f,
        });
    }

    // a shock ring facing the camera, opening fast & thinning
    public void AddRing(Vector3 at, Color color, float delayS,
        bool isInward = false) {
        SpiderKingRing ring = new SpiderKingRing {
            center = at,
            color = color,
            ageS = -delayS,
            isInward = isInward,
            parts = new Transform[RING_PARTS],
        }
        ;
        for (int i = 0; i < RING_PARTS; i++) {
            ring.parts[i] =
                rig.Feedback.CreatePrimitive(PrimitiveType.Cylinder,
                    "ShockRing",
                Vector3.one * 0.1f, color).transform;
            ring.parts[i].gameObject.SetActive(false);
        }
        rig.rings.Add(ring);
    }

    // a white orb that swells & fades at the clash
    public void AddPop(Vector3 at) {
        Transform orb =
            rig.Feedback.CreatePrimitive(PrimitiveType.Sphere, "ClashPop",
            Vector3.one * 0.2f, Color.white).transform;
        orb.position = at;
        rig.pops.Add(new SpiderKingSpark {
            transform = orb,
            lifeS = 0.18f,
            size = 0.7f,
        });
    }

    public void UpdateClashFx() {
        Quaternion facing = ViewRotation();
        for (int i = rig.rings.Count - 1; i >= 0; i--) {
            SpiderKingRing ring = rig.rings[i];
            ring.ageS += Time.unscaledDeltaTime;
            float t = ring.ageS / RING_S;
            if (t >= 1f) {
                foreach (Transform part in ring.parts) {
                    if (part != null) {
                        UnityEngine.Object.Destroy(part.gameObject);
                    }
                }
                rig.rings.RemoveAt(i);
                continue;
            }
            if (t < 0f) {
                continue;
            }
            float ease = 1f - (1f - t) * (1f - t);
            float radius = ring.isInward
                ? Mathf.Lerp(3f, 0.3f, ease)
                : Mathf.Lerp(0.2f, 1.6f, ease);
            float thickness = ring.isInward
                ? Mathf.Lerp(0.03f, 0.14f, t)
                : Mathf.Lerp(0.09f, 0.01f, t);
            // a drawn in ring follows the head it closes on
            if (ring.isInward && rig.body.IsBuilt) {
                ring.center = rig.body.HeadPosition;
            }
            Vector3 right = facing * Vector3.right;
            Vector3 up = facing * Vector3.up;
            for (int k = 0; k < RING_PARTS; k++) {
                float a0 = k * Mathf.PI * 2f / RING_PARTS;
                float a1 = (k + 1) * Mathf.PI * 2f / RING_PARTS;
                Vector3 p0 = ring.center + (right * Mathf.Cos(a0)
                    + up * Mathf.Sin(a0)) * radius;
                Vector3 p1 = ring.center + (right * Mathf.Cos(a1)
                    + up * Mathf.Sin(a1)) * radius;
                ring.parts[k].gameObject.SetActive(true);
                SpiderKingFeedback.SetLine(ring.parts[k], p0, p1, thickness);
            }
        }
        for (int i = rig.pops.Count - 1; i >= 0; i--) {
            SpiderKingSpark pop = rig.pops[i];
            pop.ageS += Time.unscaledDeltaTime;
            float t = pop.ageS / pop.lifeS;
            if (pop.transform == null || t >= 1f) {
                if (pop.transform != null) {
                    UnityEngine.Object.Destroy(pop.transform.gameObject);
                }
                rig.pops.RemoveAt(i);
                continue;
            }
            if (pop.isStroke) {
                // a stroke thins away along its length
                Vector3 scaleNow = pop.transform.localScale;
                scaleNow.x = scaleNow.z = pop.size * (1f - t);
                pop.transform.localScale = scaleNow;
                rig.pops[i] = pop;
                continue;
            }
            // up fast, then gone
            float scale = t < 0.4f
                ? Mathf.Lerp(0.2f, pop.size, t / 0.4f)
                : Mathf.Lerp(pop.size, 0f, (t - 0.4f) / 0.6f);
            pop.transform.localScale = Vector3.one * scale;
            rig.pops[i] = pop;
        }
        rig.Movement.UpdateTailTint();
    }

    // the grinding sparks while a batted thing is held on the tail, & the
    // gold trail as it flies back
    public void ClashTrail(float clashS, Vector3 at, ref float nextFxS) {
        bool isHeld = clashS >= CLASH_IN_S
            && clashS < CLASH_IN_S + CLASH_HOLD_S;
        bool isOut = clashS >= CLASH_IN_S + CLASH_HOLD_S;
        if ((!isHeld && !isOut) || clashS < nextFxS) {
            return;
        }
        nextFxS = clashS + (isHeld ? 0.03f : 0.025f);
        if (isHeld) {
            BurstStreaks(at, new Color(1f, 0.6f, 0.15f), 3, 2f, 6f);
        } else {
            AddTrailBead(at);
        }
    }

    // a clash's share 0~1 of its slowing in, its hold & its speeding off,
    // all in real time
    public static float ClashIn(float clashS) =>
        Mathf.Clamp01(clashS / CLASH_IN_S);

    public static float ClashOut(float clashS) =>
        Mathf.Clamp01((clashS - CLASH_IN_S - CLASH_HOLD_S) / CLASH_OUT_S);

    // the swing bats things back from the double tap to the end of its
    // active part: its wind up is only the look
    public bool IsParryArmed() => rig.tailTimeS >= 0f
        && rig.tailTimeS < tuning.TailWindUpS + tuning.TailActiveS;

    // a lesson's stop never finds the monkey stuck in a web, where Space
    // would only struggle & the lesson could never be passed
    public void FreeForLesson() {
        if (rig.struggle != null && rig.struggle.IsTrapped) {
            rig.struggle.ForceRelease();
        }
    }

    // the lesson: time stops as the wave turns gold, till the double tap
    public void FreezeForParry(SpiderKingShot shot) {
        FreeForLesson();
        rig.isTeachFrozen = true;
        rig.frozenShot = shot;
        rig.lastTapTime = -10f;
        // the animated double tap icon teaches the gesture, no words
        rig.Teaching.Teach("Now!", "", UIStyle.Gold, 0f);
        rig.hud.ShowDoubleTapHint();
    }

    // a hit stop, a gold flash & sparks; the ball turns gold & flies back
    public void OnParry(SpiderKingShot shot, Vector3 at) {
        rig.hasSwingHit = true;
        shot.isReflected = true;
        shot.clashS = 0f;
        shot.clashFrom = at;
        shot.hasClashed = false;
        ParryEffect(at);
        if (!rig.hasTaughtWeb) {
            rig.hasTaughtWeb = true;
            rig.hud.HideBanner();
        }
        // the lesson passed: the fight is on once it breaks out
        if (rig.teach == SpiderKingTeachStep.Parry) {
            rig.teach = SpiderKingTeachStep.Done;
            rig.nextAttackTime = Time.time + tuning.ParryStunS + 1f;
        }
    }

    // the volley it began is still fired out
    public void WebTheSpider() {
        // already stuck: more batted back bullets never add to it
        if (Time.time < rig.stunEndTime) {
            Burst(rig.body.Position, tuning.SilkColor, 6);
            return;
        }
        rig.stunEndTime = Time.time + tuning.ParryStunS;
        rig.Attacks.CancelAttack();
        rig.isDashing = false;
        rig.Attacks.StopSlashing();
        if (rig.mode == SpiderKingMode.Cocooning) {
            rig.Movement.SetMode(SpiderKingMode.Idle);
        }
        rig.nextAttackTime =
            Mathf.Max(rig.nextAttackTime, rig.stunEndTime + 0.5f);
        rig.hud.PopText("STUCK!", rig.body.HeadPosition, UIStyle.Gold);
        rig.hud.Say("Ugh! My own web!");
        Burst(rig.body.Position, tuning.SilkColor, 14);
        if (rig.stuckWeb == null) {
            rig.stuckWeb =
                UnityEngine.Object.Instantiate(tuning.WebPrefab,
                    rig.body.Position, Quaternion.identity,
                rig.host).gameObject;
            SpiderKingFeedback.MakeDecor(rig.stuckWeb);
            rig.stuckWeb.transform.localScale *= 1.5f;
        }
    }

    // the web wrapped round the stuck spider, gone once it breaks out
    public void UpdateStuckWeb(bool isStunned) {
        if (rig.stuckWeb == null) {
            return;
        }
        if (!isStunned) {
            UnityEngine.Object.Destroy(rig.stuckWeb);
            rig.stuckWeb = null;
            return;
        }
        rig.stuckWeb.transform.SetPositionAndRotation(
            rig.PointOnTrunk(rig.spiderAngle, rig.spiderY,
                tuning.TrunkRadiusU + 1.7f),
            Quaternion.Euler(0f, -rig.spiderAngle, 0f)
        );
    }

    // the slow motion of a parry, & the stops of the lessons
    public void UpdateTimeScale(GameController game) {
        if (game.IsPaused || rig.phase != SpiderKingPhase.Fighting) {
            return;
        }
        float scale = rig.isTeachFrozen
            || Time.unscaledTime < rig.clashStopEndTime
            ? 0f
            : Time.unscaledTime < rig.parrySlowEndTime
                ? tuning.ParrySlowTimeScale : 1f;
        if (scale < 1f || rig.isTimeBent) {
            Time.timeScale = scale;
            rig.isTimeBent = scale < 1f;
        }
    }

    // Sparks  -----------------------------------------------------------------
    public void Burst(Vector3 at, Color color, int count) {
        for (int i = 0; i < count; i++) {
            Transform spark =
                rig.Feedback.CreatePrimitive(PrimitiveType.Cube, "Spark",
                Vector3.one * 0.18f, color).transform;
            spark.position = at;
            rig.sparks.Add(new SpiderKingSpark {
                transform = spark,
                velocity = Random.onUnitSphere * Random.Range(3f, 7f),
                lifeS = Random.Range(0.4f, 0.8f),
            });
        }
    }

    public void UpdateSparks() {
        for (int i = rig.sparks.Count - 1; i >= 0; i--) {
            SpiderKingSpark spark = rig.sparks[i];
            spark.ageS += Time.unscaledDeltaTime;
            if (spark.transform == null || spark.ageS >= spark.lifeS) {
                if (spark.transform != null) {
                    UnityEngine.Object.Destroy(spark.transform.gameObject);
                }
                rig.sparks.RemoveAt(i);
                continue;
            }
            if (!spark.isFloating) {
                spark.velocity += Vector3.down * 9f * Time.unscaledDeltaTime;
            }
            spark.transform.position += spark.velocity * Time.unscaledDeltaTime;
            float left = 1f - spark.ageS / spark.lifeS;
            if (spark.isStreak) {
                if (spark.velocity.sqrMagnitude > 0.0001f) {
                    spark.transform.rotation =
                        Quaternion.LookRotation(spark.velocity.normalized);
                }
                spark.transform.localScale = new Vector3(0.07f, 0.07f,
                    0.1f + 0.06f * spark.velocity.magnitude) * left;
            } else {
                float size = spark.size > 0f ? spark.size : 0.18f;
                spark.transform.localScale = Vector3.one * size * left;
            }
            rig.sparks[i] = spark;
        }
    }

}
