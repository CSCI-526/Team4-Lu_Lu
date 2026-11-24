using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>The crescents and shots in flight, their waves, paths, and
/// hits.</summary>
public sealed class SpiderKingShots {
    // private members  ########################################################
    private readonly SpiderKingRig rig;
    private readonly SpiderKingConfig config;

    public SpiderKingShots(SpiderKingRig rig) {
        this.rig = rig;
        config = rig.config;
    }

    // Public Methods  #########################################################
    // a crescent bulging forward, as wide as slashWidthU, flat on the bark
    public void LaunchCrescent(float directionDeg) {
        SpiderKingCrescent crescent = new SpiderKingCrescent {
            originAngle = rig.spiderAngle,
            originY = rig.spiderY,
            directionDeg = directionDeg,
            distanceU = 0.8f,
            parts = new Transform[CRESCENT_STRANDS * CRESCENT_PARTS
                + CRESCENT_SPOKES],
        }
        ;
        for (int i = 0; i < crescent.parts.Length; i++) {
            crescent.parts[i] =
                rig.Feedback.CreatePrimitive(PrimitiveType.Cylinder,
                "SilkBlade", Vector3.one * 0.1f, config.SilkColor).transform;
        }
        rig.crescents.Add(crescent);
        rig.Parry.Burst(rig.body.HeadPosition, config.SilkColor, 6);
    }

    public void UpdateCrescents() {
        for (int i = rig.crescents.Count - 1; i >= 0; i--) {
            SpiderKingCrescent crescent = rig.crescents[i];
            float speedU = config.SlashSpeedU * rig.SpeedScale;
            if (crescent.isReflected) {
                // the clash: it slows into the tail, sparks, holds, then
                // speeds off back to where it was slashed from, the spider
                crescent.clashS += Time.unscaledDeltaTime;
                float inShare = SpiderKingParry.ClashIn(crescent.clashS);
                if (inShare < 1f) {
                    crescent.distanceU = Mathf.Lerp(crescent.parryDistanceU,
                        crescent.contactDistanceU, inShare * inShare);
                } else {
                    if (!crescent.hasClashed) {
                        crescent.hasClashed = true;
                        rig.Parry.Clash(rig.Parry.CrescentContact(crescent));
                    }
                    float outShare = SpiderKingParry.ClashOut(crescent.clashS);
                    float kick = 1f - (1f - outShare) * (1f - outShare);
                    crescent.distanceU = Mathf.Lerp(crescent.contactDistanceU,
                        0.8f, kick);
                    if (outShare >= 1f) {
                        RemoveCrescent(crescent);
                        rig.Parry.Burst(rig.body.HeadPosition,
                            config.GoldenColor, 14);
                        rig.Damage.Hurt(config.SlashParryDamage);
                        if (rig.phase != SpiderKingPhase.Fighting) {
                            break;
                        }
                        continue;
                    }
                }
                rig.Parry.ClashTrail(crescent.clashS,
                    CrescentPoint(crescent, 0f),
                    ref crescent.nextFxS);
                DrawCrescent(crescent, true);
                continue;
            }
            float lastDistanceU = crescent.distanceU;
            crescent.distanceU += speedU * Time.deltaTime;
            if (crescent.distanceU > config.SlashRangeU + 4f) {
                RemoveCrescent(crescent);
                continue;
            }
            // to the monkey's near edge, the side the slash comes from
            float ahead = CrescentAhead(crescent, out float across)
                - MONKEY_EDGE_U;
            bool isOnWay =
                Mathf.Abs(across) <= rig.SlashWidthNowU * 0.5f + 0.6f;
            bool isGold = isOnWay && ahead >= -0.5f
                && ahead <= rig.WaveGoldNowU;
            DrawCrescent(crescent, isGold);
            if (isOnWay && ahead >= -0.5f && ahead <= rig.ParryReachNowU
                && rig.Parry.IsParryArmed()) {
                rig.Parry.OnCrescentParried(crescent);
                continue;
            }
            if (isGold && !rig.hasTaughtSlash && !rig.isTeachFrozen) {
                rig.Parry.FreeForLesson();
                rig.isTeachFrozen = true;
                rig.frozenCrescent = crescent;
                rig.lastTapTime = -10f;
                rig.Teaching.Teach("Now!", "", UIStyle.Gold, 0f);
                rig.hud.ShowDoubleTapHint();
            }
            if (IsHitByCrescent(crescent, lastDistanceU)
                && Time.time >= rig.saveGraceEndTime) {
                RemoveCrescent(crescent);
                // a loss or the armor clears the list, so stop here
                rig.Damage.LethalHit("Hit!");
                break;
            }
        }
    }

    // how far the monkey is ahead of the crescent's middle along its way,
    // & how far across it
    public float CrescentAhead(SpiderKingCrescent crescent, out float across) {
        Vector3 at = rig.monkey.position;
        float mu = Mathf.DeltaAngle(crescent.originAngle, rig.AngleOf(at))
            * Mathf.Deg2Rad * config.TrunkRadiusU;
        float mv = at.y - crescent.originY;
        float radians = crescent.directionDeg * Mathf.Deg2Rad;
        float along = mu * Mathf.Cos(radians) + mv * Mathf.Sin(radians);
        across = -mu * Mathf.Sin(radians) + mv * Mathf.Cos(radians);
        return along - (crescent.distanceU + CRESCENT_BULGE_U);
    }

    // a point of the crescent: lateral from -1 to 1 across it, back that
    // far behind its front
    public Vector3 CrescentPoint(SpiderKingCrescent crescent, float lateral,
        float back = 0f) {
        float radians = crescent.directionDeg * Mathf.Deg2Rad;
        float du = Mathf.Cos(radians);
        float dv = Mathf.Sin(radians);
        float side = lateral * rig.SlashWidthNowU * 0.5f;
        float ahead = crescent.distanceU - back
            + CRESCENT_BULGE_U * (1f - lateral * lateral);
        return rig.Attacks.BarkPoint(crescent.originAngle, crescent.originY,
            du * ahead - dv * side, dv * ahead + du * side);
    }

    // a silk blade: three silk strands one behind the other, tied by short
    // spokes like a torn piece of web; thick & flickering red-white so it
    // reads from afar, gold & flashing when it can be batted back, gold
    // flying back; only its look, its width & hit are as before
    public void DrawCrescent(SpiderKingCrescent crescent, bool isGold) {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 30f);
        float flicker = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 14f);
        Color color = crescent.isReflected
            ? config.GoldenColor
            : isGold ? Color.Lerp(config.GoldenColor, Color.white, pulse)
                : Color.Lerp(Color.white, config.WarnColor,
                    CRESCENT_WARN_TINT * flicker);
        int index = 0;
        for (int strand = 0; strand < CRESCENT_STRANDS; strand++) {
            float back = strand * 0.3f;
            float width = 1f - strand * 0.15f;
            for (int i = 0; i < CRESCENT_PARTS; i++) {
                float l0 = (-1f + 2f * i / CRESCENT_PARTS) * width;
                float l1 = (-1f + 2f * (i + 1) / CRESCENT_PARTS) * width;
                float middle = 1f - Mathf.Abs((l0 + l1) * 0.5f);
                SetCrescentPart(crescent, index++,
                    CrescentPoint(crescent, l0, back),
                    CrescentPoint(crescent, l1, back),
                    Mathf.Lerp(CRESCENT_TIP_THICK_U, strand == 0
                        ? CRESCENT_FRONT_THICK_U : CRESCENT_BACK_THICK_U,
                        middle),
                    color);
            }
        }
        for (int i = 0; i < CRESCENT_SPOKES; i++) {
            float lateral = -0.8f + 1.6f * i / (CRESCENT_SPOKES - 1f);
            SetCrescentPart(crescent, index++,
                CrescentPoint(crescent, lateral, 0f),
                CrescentPoint(crescent, lateral * 0.7f,
                    (CRESCENT_STRANDS - 1) * 0.3f),
                CRESCENT_SPOKE_THICK_U, color);
        }
    }

    // a blade's look: its strands' thickness at the tips, in the front
    // strand's middle & the back ones', its spokes'; u
    private const float CRESCENT_TIP_THICK_U = 0.12f;
    private const float CRESCENT_FRONT_THICK_U = 0.42f;
    private const float CRESCENT_BACK_THICK_U = 0.24f;
    private const float CRESCENT_SPOKE_THICK_U = 0.1f;
    // share of red its white flickers to
    private const float CRESCENT_WARN_TINT = 0.45f;

    public void SetCrescentPart(SpiderKingCrescent crescent, int index,
        Vector3 from,
        Vector3 to, float thickness, Color color) {
        SpiderKingFeedback.SetLine(crescent.parts[index], from, to, thickness);
        rig.Feedback.SetColor(crescent.parts[index].GetComponent<Renderer>(),
            color);
    }

    // the monkey where the crescent is now, along it & across it
    // the monkey swept over by the crescent's front this step, from where it
    // was to where it is, so a fast one never skips past
    public bool IsHitByCrescent(SpiderKingCrescent crescent,
        float lastDistanceU) {
        Vector3 at = rig.monkey.position;
        float mu = Mathf.DeltaAngle(crescent.originAngle, rig.AngleOf(at))
            * Mathf.Deg2Rad * config.TrunkRadiusU;
        float mv = at.y - crescent.originY;
        float radians = crescent.directionDeg * Mathf.Deg2Rad;
        float along = mu * Mathf.Cos(radians) + mv * Mathf.Sin(radians);
        float across = -mu * Mathf.Sin(radians) + mv * Mathf.Cos(radians);
        float halfWidth = rig.SlashWidthNowU * 0.5f;
        if (Mathf.Abs(across) > halfWidth) {
            return false;
        }
        float lateral = across / halfWidth;
        float bulge = CRESCENT_BULGE_U * (1f - lateral * lateral);
        float back = lastDistanceU + bulge - 0.8f;
        float front = crescent.distanceU + bulge + 0.8f;
        return along >= back && along <= front;
    }

    public void RemoveCrescent(SpiderKingCrescent crescent) {
        foreach (Transform part in crescent.parts) {
            if (part != null) {
                UnityEngine.Object.Destroy(part.gameObject);
            }
        }
        rig.crescents.Remove(crescent);
    }

    // Shots  ------------------------------------------------------------------
    public SpiderKingShot NewShot(SpiderKingShotKind kind, Vector3 target) {
        SpiderKingShot shot = new SpiderKingShot {
            kind = kind,
            target = target,
            from = rig.body.HeadPosition,
            isCharging = true,
        }
        ;
        // the ball is the charge orb at the head
        shot.ball =
            rig.Feedback.CreatePrimitive(PrimitiveType.Sphere, kind.ToString(),
            Vector3.one * 0.4f, config.SilkColor).transform;
        shot.wave = BuildWave();
        shot.wave.gameObject.SetActive(false);
        shot.length = PathLength(shot);
        rig.shots.Add(shot);
        return shot;
    }

    // a silk bullet: one long glowing capsule, flying along its length
    public Transform BuildWave() {
        Transform bullet = new GameObject("SilkBullet").transform;
        bullet.SetParent(rig.host, false);
        Transform core =
            rig.Feedback.CreatePrimitive(PrimitiveType.Capsule, "Bullet",
            Vector3.one, Color.white).transform;
        core.SetParent(bullet, false);
        core.localRotation = Quaternion.Euler(90f, 0f, 0f);
        core.localScale = new Vector3(0.45f, 0.75f, 0.45f);
        return bullet;
    }

    public void ColorWave(Transform wave, Color color) {
        foreach (Renderer part in
            wave.GetComponentsInChildren<Renderer>(true)) {
            rig.Feedback.SetColor(part, color);
        }
    }

    public void RemoveShot(SpiderKingShot shot) {
        if (shot.ball != null) {
            UnityEngine.Object.Destroy(shot.ball.gameObject);
        }
        if (shot.wave != null) {
            UnityEngine.Object.Destroy(shot.wave.gameObject);
        }
        rig.shots.Remove(shot);
    }

    public void ClearShots() {
        while (rig.shots.Count > 0) {
            RemoveShot(rig.shots[0]);
        }
        rig.chargingShot = null;
    }

    // bullets home in on the monkey; a batted back one flies back from
    // where it was hit
    public Vector3 PathPoint(SpiderKingShot shot, float t) {
        Vector3 to = shot.isReflected ? shot.target : rig.monkey.position;
        return rig.Movement.ArcPoint(shot.from, to, t, 1.5f);
    }

    public float PathLength(SpiderKingShot shot) {
        float length = 0f;
        Vector3 last = PathPoint(shot, 0f);
        for (int i = 1; i <= PATH_PARTS; i++) {
            Vector3 next = PathPoint(shot, i / (float)PATH_PARTS);
            length += Vector3.Distance(last, next);
            last = next;
        }
        return Mathf.Max(0.1f, length);
    }

    public void UpdateShots() {
        for (int i = rig.shots.Count - 1; i >= 0; i--) {
            SpiderKingShot shot = rig.shots[i];
            if (shot.ball == null) {
                rig.shots.RemoveAt(i);
                continue;
            }
            if (shot.isCharging) {
                shot.from = rig.body.HeadPosition;
                shot.length = PathLength(shot);
                // the orb swells & throbs at the head
                float throb = 1f + 0.15f * Mathf.Sin(Time.time * 40f);
                shot.ball.position = rig.body.HeadPosition;
                shot.ball.localScale = Vector3.one
                    * (0.2f + 0.9f * shot.fill) * throb;
                continue;
            }
            if (shot.isReflected) {
                UpdateClashShot(shot);
                continue;
            }
            shot.length = PathLength(shot);
            float speedU = config.WebShotSpeedU * rig.SpeedScale;
            shot.t += speedU * Time.deltaTime / shot.length;
            shot.t = Mathf.Clamp01(shot.t);
            Vector3 at = PathPoint(shot, shot.t);
            shot.lastPosition = at;
            shot.ball.gameObject.SetActive(false);
            // to the monkey's near edge, the side the bullet comes from
            float toMonkey = Vector3.Distance(at, rig.monkey.position)
                - MONKEY_EDGE_U;
            bool isGold = toMonkey <= rig.WaveGoldNowU;
            DrawWave(shot, at, isGold);
            if (toMonkey <= rig.ParryReachNowU && rig.Parry.IsParryArmed()) {
                rig.Parry.OnParry(shot, at);
                continue;
            }
            if (isGold && !rig.hasTaughtWeb && !rig.isTeachFrozen) {
                rig.Parry.FreezeForParry(shot);
            }
            if (shot.t >= 1f) {
                RemoveShot(shot);
                Vector3 landing = rig.monkey.position;
                rig.Arena.PlaceWeb(rig.AngleOf(landing), landing.y);
            }
        }
    }

    // the batted back bullet: slows into the tail, sparks, holds, then
    // speeds off back to the spider, sticking & hurting it
    public void UpdateClashShot(SpiderKingShot shot) {
        shot.clashS += Time.unscaledDeltaTime;
        Vector3 contact = rig.Parry.EdgeToward(shot.clashFrom);
        Vector3 at;
        float inShare = SpiderKingParry.ClashIn(shot.clashS);
        if (inShare < 1f) {
            at = Vector3.Lerp(shot.clashFrom, contact, inShare * inShare);
        } else {
            if (!shot.hasClashed) {
                shot.hasClashed = true;
                rig.Parry.Clash(contact);
            }
            float outShare = SpiderKingParry.ClashOut(shot.clashS);
            float kick = 1f - (1f - outShare) * (1f - outShare);
            at = outShare <= 0f
                ? contact + Random.insideUnitSphere * 0.12f
                : rig.Movement.ArcPoint(contact, rig.body.HeadPosition, kick,
                    1.5f);
            if (outShare >= 1f) {
                RemoveShot(shot);
                rig.Parry.Burst(rig.body.HeadPosition, config.GoldenColor, 14);
                rig.Parry.WebTheSpider();
                rig.Damage.Hurt(config.WebParryDamage);
                return;
            }
        }
        rig.Parry.ClashTrail(shot.clashS, at, ref shot.nextFxS);
        Vector3 heading = at - shot.lastPosition;
        shot.lastPosition = at;
        shot.wave.gameObject.SetActive(true);
        if (heading.sqrMagnitude > 0.000001f) {
            shot.wave.SetPositionAndRotation(at,
                Quaternion.LookRotation(heading.normalized, Vector3.up));
        } else {
            shot.wave.position = at;
        }
        shot.wave.localScale = Vector3.one * 1.2f;
        ColorWave(shot.wave, config.GoldenColor);
    }

    // the bullet flies along its length; white, gold & flashing once close
    // enough to parry
    public void DrawWave(SpiderKingShot shot, Vector3 at, bool isGold) {
        shot.wave.gameObject.SetActive(true);
        float step = shot.isReflected ? -0.05f : 0.05f;
        Vector3 ahead = PathPoint(shot, Mathf.Clamp01(shot.t + step));
        Vector3 heading = ahead - at;
        if (heading.sqrMagnitude < 0.0001f) {
            heading = rig.monkey.position - at;
        }
        if (heading.sqrMagnitude > 0.0001f) {
            shot.wave.SetPositionAndRotation(at,
                Quaternion.LookRotation(heading.normalized, Vector3.up));
        } else {
            shot.wave.position = at;
        }
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 30f);
        Color color = shot.isReflected
            ? config.GoldenColor
            : isGold ? Color.Lerp(config.GoldenColor, Color.white, pulse)
                : Color.white;
        shot.wave.localScale = Vector3.one * (isGold ? 1.25f : 1f);
        ColorWave(shot.wave, color);
    }

}
