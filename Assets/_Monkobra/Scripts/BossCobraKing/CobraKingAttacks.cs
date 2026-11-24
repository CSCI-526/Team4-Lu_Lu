using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static CobraKingRig;

/// <summary>The fight loop and the king's attacks: bite, sweep, push, and the
/// warnings before each.</summary>
public sealed class CobraKingAttacks {
    // Private Members  ########################################################
    private readonly CobraKingRig rig;
    private readonly CobraKingTuning tuning;

    public CobraKingAttacks(CobraKingRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    // Fight Loop  -------------------------------------------------------------
    // rest (jewel lit) → a round of attacks (jewel dark) → rest ...
    public void UpdateFightLoop() {
        switch (rig.loop) {
            case CobraKingLoop.Intro:
                // waits for the first hit, which starts the first rest
                break;
            case CobraKingLoop.Rest:
                rig.loopTimeS -= Time.deltaTime;
                // in the finale a round waits for a running wave to near its
                // end, so the two only overlap a little
                bool isWaveClear = !rig.isMinionStreaming
                    || WaveDangerLeftS() <= tuning.FinaleOverlapS;
                if (rig.loopTimeS <= 0f && isWaveClear) {
                    StartRound();
                }
                break;
            case CobraKingLoop.Attack:
                UpdateAttack();
                break;
        }
    }

    public void StartRest() {
        rig.loop = CobraKingLoop.Rest;
        rig.loopTimeS = (rig.isEasy
            ? (rig.Third >= 2 ? tuning.EasyRestFinalS : tuning.EasyRestS)
            : (rig.Third >= 2 ? tuning.RestFinalS : tuning.RestS))
            * rig.RushTimeScale;
        rig.body.SetWeakSpotLit(true);
    }

    // two bites a round, & each lost third of health adds an attack
    public void StartRound() {
        rig.loop = CobraKingLoop.Attack;
        rig.attackQueue.Clear();
        // in normal mode's finale the minions stream on their own, so a
        // round is a single bite or sweep, taking turns: never more than two
        // attacks at once
        if (rig.IsMinionFinale) {
            rig.attackQueue.Enqueue(rig.isFinaleSweepNext
                ? CobraKingAttack.Sweep : CobraKingAttack.Bite);
            rig.isFinaleSweepNext = !rig.isFinaleSweepNext;
            NextAttack();
            return;
        }
        if (rig.Third >= 2) {
            rig.attackQueue.Enqueue(CobraKingAttack.Minions);
        }
        rig.attackQueue.Enqueue(CobraKingAttack.Bite);
        // the tail joins a little b4 the first third is lost
        if (rig.LostShare >= tuning.SweepFromLostShare) {
            rig.attackQueue.Enqueue(CobraKingAttack.Sweep);
        }
        rig.attackQueue.Enqueue(CobraKingAttack.Bite);
        NextAttack();
    }

    public void NextAttack() {
        rig.Feedback.ClearAttackMarkers();
        if (rig.attackQueue.Count == 0) {
            StartRest();
            return;
        }
        rig.attack = rig.attackQueue.Dequeue();
        rig.attackTimeS = 0f;
        rig.lockAngle = rig.AngleOf(rig.monkey.position);
        rig.lockY = rig.monkey.position.y;
        switch (rig.attack) {
            case CobraKingAttack.Bite:
                StartBite();
                break;
            case CobraKingAttack.Sweep:
                StartSweep();
                break;
            case CobraKingAttack.Minions:
                rig.Minions.StartMinions();
                break;
        }
    }

    public void UpdateAttack() {
        rig.attackTimeS += Time.deltaTime;
        switch (rig.attack) {
            case CobraKingAttack.Bite:
                UpdateBite();
                break;
            case CobraKingAttack.Sweep:
                UpdateSweep();
                break;
            case CobraKingAttack.Minions:
                rig.Minions.UpdateMinions();
                break;
        }
    }

    // Bite: a red disc marks the spot, the head dives at it
    public void StartBite() {
        rig.biteSpot =
            rig.PointOnTrunk(rig.lockAngle, rig.lockY, rig.MonkeyRadius());
        rig.Arena.AddTrunkZone(rig.lockAngle, rig.lockY,
            tuning.BiteRadiusU * 2f, "BITE!", true);
        rig.biteStarted = false;
    }

    // the strike starts once the head is free, its wind-up filling the rest
    // of the warning; the attack ends once the head is back at rest
    public void UpdateBite() {
        rig.Feedback.UpdateMarkers(
            rig.attackTimeS
                / Mathf.Max(0.01f, tuning.BiteWarnS * rig.WarnScale), false,
                    false
        );
        if (!rig.biteStarted) {
            if (rig.body.IsStriking) {
                return;
            }
            rig.biteStarted = true;
            float windUp = Mathf.Max(
                0.3f,
                tuning.BiteWarnS * rig.WarnScale - rig.attackTimeS
            );
            Vector3 spot = rig.biteSpot;
            rig.body.StartStrike(spot, windUp, () => {
                // its sound as the head lands, not at the warning
                rig.onAttackStruck?.Invoke();
                if ((rig.monkey.position
                    - spot).magnitude < tuning.BiteRadiusU) {
                    LethalHit();
                }
            });
            return;
        }
        if (!rig.body.IsStriking) {
            rig.biteStarted = false;
            NextAttack();
        }
    }

    // Sweep: a red band fills round the trunk at the height, then the tail
    // circles it
    public void StartSweep() {
        GameObject band = rig.Feedback.CreatePrimitive(
            PrimitiveType.Cylinder, "SweepBand",
            new Vector3(
                (tuning.TrunkRadiusU + 0.08f) * 2f,
                0.01f,
                (tuning.TrunkRadiusU + 0.08f) * 2f
            ),
            tuning.WarningColor
        );
        band.transform.position = new Vector3(
            rig.trunkCenter.x, rig.lockY, rig.trunkCenter.z
        );
        int warning = rig.hud.AddWarning(SweepSignPoint(), "TAIL!");
        rig.markers.Add(new CobraKingMarker {
            fill = band.transform,
            warning = warning,
            isBand = true
        });
        rig.tailHit = false;
    }

    // the band's sign follows the monkey round the trunk, so it stays in view
    public Vector3 SweepSignPoint() {
        return rig.PointOnTrunk(
            rig.AngleOf(rig.monkey.position), rig.lockY + tuning.SweepBandHalfU,
                rig.MonkeyRadius()
        );
    }

    public void UpdateSweep() {
        float warn = tuning.SweepWarnS * rig.WarnScale;
        if (rig.attackTimeS < warn) {
            rig.Feedback.UpdateMarkers(rig.attackTimeS / warn, true, false);
            return;
        }
        rig.Feedback.ClearAttackMarkers();
        if (rig.tail == null) {
            // its sound as the tail comes round, not at the warning
            rig.onAttackStruck?.Invoke();
            rig.tail = new Transform[tuning.TailBeadCount];
            for (int i = 0; i < tuning.TailBeadCount; i++) {
                rig.tail[i] = rig.Feedback.CreatePrimitive(
                    PrimitiveType.Sphere, "Tail" + i,
                    Vector3.one * Mathf.Lerp(1.6f, 0.8f,
                        i / Mathf.Max(1f, tuning.TailBeadCount - 1f)),
                    tuning.TailColor
                ).transform;
            }
        }

        // starts across the trunk & loops round once
        float t = (rig.attackTimeS - warn) / tuning.SweepTimeS;
        float headAngle = rig.lockAngle + 180f + t * 360f;
        for (int i = 0; i < rig.tail.Length; i++) {
            rig.tail[i].position = rig.PointOnTrunk(
                headAngle - i * 9f, rig.lockY, tuning.TrunkRadiusU + 1.1f
            );
        }

        float gap = Mathf.Abs(Mathf.DeltaAngle(
            headAngle, rig.AngleOf(rig.monkey.position)
        ));
        if (!rig.tailHit && gap < 14f
            && Mathf.Abs(rig.monkey.position.y
                - rig.lockY) < tuning.SweepBandHalfU) {
            rig.tailHit = true;
            LethalHit();
        }

        if (t >= 1f) {
            foreach (Transform bead in rig.tail) {
                UnityEngine.Object.Destroy(bead.gameObject);
            }
            rig.tail = null;
            NextAttack();
        }
    }

    // time left till a running bite or sweep has struck; s
    public float AttackDangerLeftS() {
        if (rig.loop != CobraKingLoop.Attack) {
            return 0f;
        }
        switch (rig.attack) {
            case CobraKingAttack.Bite:
                // the warning, then a short dive
                return Mathf.Max(0f,
                    tuning.BiteWarnS * rig.WarnScale - rig.attackTimeS)
                    + 0.3f;
            case CobraKingAttack.Sweep:
                return Mathf.Max(
                    0f, tuning.SweepWarnS * rig.WarnScale + tuning.SweepTimeS
                        - rig.attackTimeS
                );
            default:
                return 0f;
        }
    }

    // time left till the running wave's last minion has fallen past the
    // monkey's height; s
    public float WaveDangerLeftS() {
        if (!rig.isWaveRunning) {
            return 0f;
        }
        float lastDropS = tuning.MinionWarnS * rig.WarnScale
            + Mathf.Max(0, tuning.MinionCount - 2) * tuning.MinionStaggerS;
        float fallS = Mathf.Max(0f, rig.coilBottomY - rig.minionLockY)
            / Mathf.Max(0.1f, tuning.MinionFallSpeedU);
        return Mathf.Max(0f, lastDropS + fallS + 0.3f - rig.waveTimeS);
    }

    // a bite, a sweep or a minion: fatal, unless the gold shield takes it
    public void LethalHit() {
        // nor on the way back to the start, while it cannot dodge
        if (rig.phase != CobraKingPhase.Fighting || rig.isReturningToStart) {
            return;
        }
        if (rig.hasShield) {
            rig.hasShield = false;
            rig.hud.PopText("Saved!", rig.monkey.position, tuning.GoldenColor);
            KnockDown(rig.body.HeadCollider);
            return;
        }
        rig.Ending.StartFinisher(false);
    }

    public static void SetUntouchable(bool isUntouchable) {
        if (GameController.I != null) {
            GameController.I.SetLeaping(isUntouchable);
        }
    }

    // the usual stun & drop w/ the heavier shake, never fatal
    public static void KnockDown(Collider source) {
        HitBranchSensor sensor = HitBranchSensor.I;
        if (sensor != null && source != null) {
            sensor.ReportHit(
                source,
                HitBranchSensor.HitKind.FallingCobraController
            );
        }
    }

    // the climb is capped below the king, & a monkey at the cap is butted
    // down; too low, the king taunts
    public void UpdateWarnings() {
        float ceilingY = rig.body.LowestY - tuning.CeilingGapU;
        if (rig.monkeyController != null) {
            rig.monkeyController.SetHeightCeiling(ceilingY);
        }
        float y = rig.monkey.position.y;
        if (y < rig.entryY - tuning.RetreatLimitU) {
            rig.Feedback.Say(tuning.RetreatLine);
        } else if (y > ceilingY - tuning.PushRangeU) {
            StartPush();
        }
    }

    // a striking head reaches far below the rest pose, so its touch is no
    // sign of a monkey too close; only the rest pose's range counts then
    public void OnKingTouched() {
        if (rig.phase == CobraKingPhase.Fighting && !rig.body.IsStriking) {
            StartPush();
        }
    }

    // any time the monkey gets too close the king shoves it off & says so:
    // a head butt when its head is free, else at once
    public void StartPush() {
        if (Time.time < rig.nextPushTime) {
            return;
        }
        rig.nextPushTime = Time.time + tuning.PushCooldownS;
        rig.Feedback.SayNow(tuning.ApproachLine, CobraKingLinePriority.Urgent);
        if (rig.body.IsStriking) {
            KnockDown(rig.body.HeadCollider);
            return;
        }
        rig.body.StartStrike(rig.monkey.position, tuning.PushWindUpS, () => {
            if (rig.monkey.position.y > rig.body.LowestY - tuning.PushRangeU
                - 1.5f) {
                KnockDown(rig.body.HeadCollider);
            }
        });
    }

    public void UpdateChaseWarning() {
        if (rig.hasWarnedChase || Time.time < rig.chaseWarnTime) {
            return;
        }
        rig.hasWarnedChase = true;
        rig.hud.ShowBannerFor(
            "The cobra is coming!", "Climb!",
            new Color(0.95f, 0.25f, 0.2f), 2.5f
        );
    }

}
