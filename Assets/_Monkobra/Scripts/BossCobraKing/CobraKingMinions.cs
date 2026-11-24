using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static CobraKingRig;

/// <summary>The minion waves and streams of the finale.</summary>
public sealed class CobraKingMinions {
    // private members  ########################################################
    private readonly CobraKingRig rig;
    private readonly CobraKingTuning tuning;

    public CobraKingMinions(CobraKingRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    // Minions: waves of small snakes fall one after another across the
    // monkey's side of the trunk, each wave leaving one gap to dodge into
    public void StartMinions() {
        // the king calls its minions once a game, the first time they come
        if (!rig.hasCalledMinions) {
            rig.hasCalledMinions = true;
            rig.Feedback.SayNow(tuning.MinionCallLine,
                CobraKingLinePriority.Key);
        }
        rig.waveIndex = 0;
        StartWave();
    }

    public void StartWave() {
        rig.Feedback.ClearStripMarkers();
        rig.minions.Clear();
        rig.waveTimeS = 0f;
        rig.isWaveRunning = true;
        // its own lock, so a bite or sweep running alongside keeps theirs
        rig.minionLockAngle = rig.AngleOf(rig.monkey.position);
        rig.minionLockY = rig.monkey.position.y;

        // a gap never at either end, & the sweep runs either way
        int gap = Random.Range(1, Mathf.Max(2, tuning.MinionCount - 1));
        float direction = Random.value < 0.5f ? -1f : 1f;
        float warn = tuning.MinionWarnS * rig.WarnScale;
        int signAt = tuning.MinionCount / 2 == gap ? gap + 1
            : tuning.MinionCount / 2;
        int order = 0;
        for (int i = 0; i < tuning.MinionCount; i++) {
            float angle = rig.minionLockAngle
                + direction * (i - (tuning.MinionCount - 1) * 0.5f)
                    * tuning.MinionSpreadDeg;
            bool isGap = i == gap;
            if (!isGap) {
                // the same filling danger zone as the first version; one
                // LOOK UP! sign for the whole wave, on a middle zone
                // the strip is the minion's whole fall: from the coil it
                // drops out of, to where it is gone
                rig.Arena.AddTrunkStrip(
                    angle,
                    rig.coilBottomY,
                    rig.MinionEndY,
                    tuning.MinionHitRadiusU * 1.4f,
                    i == signAt ? "LOOK UP!" : null
                );
            }
            rig.minions.Add(new CobraKingMinion {
                angle = angle,
                root = null,
                isDone = isGap,
                dropDelayS = warn + order * tuning.MinionStaggerS
            });
            if (!isGap) {
                order++;
            }
        }
    }

    public void UpdateMinionStream() {
        if (!rig.isMinionStreaming) {
            if (!rig.IsMinionFinale || rig.loop == CobraKingLoop.Intro) {
                return;
            }
            rig.isMinionStreaming = true;
            if (!rig.hasCalledMinions) {
                rig.hasCalledMinions = true;
                rig.Feedback.SayNow(tuning.MinionCallLine,
                    CobraKingLinePriority.Key);
            }
            StartWave();
            return;
        }
        if (rig.isWaveRunning) {
            UpdateMinions();
        } else if (
            Time.time >= rig.nextWaveTime
            && rig.Attacks.AttackDangerLeftS() <= tuning.FinaleOverlapS
        ) {
            // likewise a wave waits for a bite or sweep to near its end
            StartWave();
        }
    }

    public void UpdateMinions() {
        float lastWaveTimeS = rig.waveTimeS;
        rig.waveTimeS += Time.deltaTime;
        float warn = tuning.MinionWarnS * rig.WarnScale;
        // its sound as the first minion drops, not at the warning
        if (lastWaveTimeS < warn && rig.waveTimeS >= warn) {
            rig.onAttackStruck?.Invoke();
        }
        if (rig.waveTimeS < warn) {
            rig.Feedback.UpdateMarkers(rig.waveTimeS / warn, false, true);
        }
        bool isAnyLeft = false;
        for (int i = 0; i < rig.minions.Count; i++) {
            CobraKingMinion minion = rig.minions[i];
            if (minion.isDone) {
                continue;
            }
            isAnyLeft = true;
            if (rig.waveTimeS < minion.dropDelayS) {
                continue;
            }
            if (minion.root == null) {
                minion.root = CreateMinion();
                minion.root.position = rig.PointOnTrunk(
                    minion.angle, rig.coilBottomY, rig.MonkeyRadius()
                );
            }
            minion.root.position += Vector3.down
                * tuning.MinionFallSpeedU * Time.deltaTime;
            bool hasHit = (minion.root.position - rig.monkey.position).magnitude
                < tuning.MinionHitRadiusU;
            if (hasHit) {
                // a minion is a snake too: its touch is fatal, unless the
                // gold shield takes it
                rig.Attacks.LethalHit();
            }
            if (hasHit || minion.root.position.y < rig.MinionEndY) {
                UnityEngine.Object.Destroy(minion.root.gameObject);
                minion.root = null;
                minion.isDone = true;
            }
            rig.minions[i] = minion;
        }
        if (isAnyLeft) {
            return;
        }
        rig.isWaveRunning = false;
        rig.Feedback.ClearStripMarkers();
        // the finale's stream starts its next wave after a short gap
        if (rig.isMinionStreaming) {
            rig.nextWaveTime = Time.time + tuning.MinionStreamGapS;
            return;
        }
        rig.waveIndex++;
        if (rig.waveIndex < rig.MinionWaves) {
            StartWave();
        } else {
            rig.Attacks.NextAttack();
        }
    }

    // a small snake: three green beads in a column
    public Transform CreateMinion() {
        Transform root = new GameObject("Minion").transform;
        root.SetParent(rig.host, false);
        for (int i = 0; i < 3; i++) {
            Transform bead = rig.Feedback.CreatePrimitive(
                PrimitiveType.Sphere, "Bead" + i,
                Vector3.one * (0.7f - i * 0.12f), tuning.MinionColor
            ).transform;
            bead.SetParent(root, false);
            bead.localPosition = Vector3.up * i * 0.55f;
        }
        return root;
    }

}
