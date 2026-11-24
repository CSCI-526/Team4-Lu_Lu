using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static CobraKingRig;

/// <summary>Height markers, spoken lines, camera focus, and the primitive and
/// color helpers.</summary>
public sealed class CobraKingFeedback {
    // private members  ########################################################
    private readonly CobraKingRig rig;
    private readonly CobraKingTuning tuning;

    public CobraKingFeedback(CobraKingRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    // the camera aims up between the monkey & the king, so both stay in view
    public void UpdateCameraFocus() {
        if (rig.cameraFraming == null) {
            return;
        }
        bool isFighting = rig.phase == CobraKingPhase.Fighting
            || rig.phase == CobraKingPhase.Finishing;
        float focus = isFighting
            ? Mathf.Clamp(
                (rig.body.HeadPosition.y - rig.monkey.position.y)
                    * tuning.CameraFocusShare,
                0f,
                tuning.MaxCameraFocusU
            )
            : 0f;
        // pulled back through the fight & the crown's flight
        rig.cameraFraming.SetBossFraming(
            focus,
            rig.phase != CobraKingPhase.Waiting
                && rig.phase != CobraKingPhase.Done
                ? tuning.CameraDistanceScale
                : 1f
        );
    }

    // fills the attack's zones (or the minion strips) to progress 0~1; the
    // band grows from its height
    public void UpdateMarkers(float progress, bool isSweep, bool isStrips) {
        progress = Mathf.Clamp01(progress);
        foreach (CobraKingMarker marker in rig.markers) {
            if (marker.fill == null || marker.isStrip != isStrips) {
                continue;
            }
            if (marker.isStrip) {
                // the full strip glows red, beating faster & brighter as
                // the minions near
                float beat = 0.5f + 0.5f * Mathf.Sin(
                    Time.time * Mathf.Lerp(6f, 22f, progress)
                );
                float glow = Mathf.Lerp(0.55f, 1f, Mathf.Max(beat, progress));
                SetColor(
                    marker.fill.GetComponent<Renderer>(),
                    Color.Lerp(tuning.WarningDimColor, tuning.WarningColor,
                        glow)
                );
                marker.fill.localScale = new Vector3(
                    marker.size * Mathf.Lerp(0.8f, 0.95f, progress),
                    marker.length,
                    0.06f
                );
                if (marker.warning >= 0) {
                    rig.hud.UpdateWarning(
                        marker.warning,
                        rig.PointOnTrunk(
                            marker.angle,
                            rig.minionLockY + 2.5f,
                            tuning.TrunkRadiusU
                        ),
                        progress
                    );
                }
                continue;
            }
            if (marker.isBand) {
                marker.fill.localScale = new Vector3(
                    (tuning.TrunkRadiusU + 0.08f) * 2f,
                    Mathf.Max(0.01f, progress * tuning.SweepBandHalfU),
                    (tuning.TrunkRadiusU + 0.08f) * 2f
                );
            } else {
                float size = marker.size * Mathf.Max(0.02f, progress);
                marker.fill.localScale = new Vector3(size, 0.03f, size);
            }
            if (marker.warning >= 0) {
                Vector3 signAt = isSweep && marker.isBand
                    ? rig.Attacks.SweepSignPoint()
                    : marker.rim != null
                        ? marker.rim.position
                        : marker.fill.position;
                rig.hud.UpdateWarning(marker.warning, signAt, progress);
            }
        }
    }

    // every marker & sign, eg for the finisher or a restart
    public void ClearMarkers() {
        foreach (CobraKingMarker marker in rig.markers) {
            DestroyMarker(marker);
        }
        rig.markers.Clear();
        rig.hud.ClearWarnings();
    }

    // a bite's or sweep's markers, leaving the minion strips
    public void ClearAttackMarkers() {
        ClearMarkersOfKind(false);
    }

    // the minion strips, leaving a bite's or sweep's markers
    public void ClearStripMarkers() {
        ClearMarkersOfKind(true);
    }

    public void ClearMarkersOfKind(bool isStrip) {
        for (int i = rig.markers.Count - 1; i >= 0; i--) {
            if (rig.markers[i].isStrip != isStrip) {
                continue;
            }
            DestroyMarker(rig.markers[i]);
            rig.markers.RemoveAt(i);
        }
    }

    public void DestroyMarker(CobraKingMarker marker) {
        if (marker.rim != null) {
            UnityEngine.Object.Destroy(marker.rim.gameObject);
        }
        if (marker.fill != null) {
            UnityEngine.Object.Destroy(marker.fill.gameObject);
        }
        if (marker.warning >= 0) {
            rig.hud.RemoveWarning(marker.warning);
        }
    }

    public void ClearAttackObjects() {
        if (rig.tail != null) {
            foreach (Transform bead in rig.tail) {
                UnityEngine.Object.Destroy(bead.gameObject);
            }
            rig.tail = null;
        }
        foreach (CobraKingMinion minion in rig.minions) {
            if (minion.root != null) {
                UnityEngine.Object.Destroy(minion.root.gameObject);
            }
        }
        rig.minions.Clear();
        foreach (CobraKingProjectile thrown in rig.projectiles) {
            if (thrown.transform != null) {
                UnityEngine.Object.Destroy(thrown.transform.gameObject);
            }
        }
        rig.projectiles.Clear();
    }

    public void Say(string line) {
        if (line == rig.lastLine && Time.time < rig.nextSameLineTime) {
            return;
        }
        SayNow(line, CobraKingLinePriority.Normal);
    }

    // a key line holds the bubble for a while, so no other line cuts it off
    // "Stay away from me!" comes first, then the minion call, then the rest;
    // a held line blocks lower ones, & a minion call cut short by the push
    // line is said again right after it
    public void SayNow(string line, CobraKingLinePriority priority) {
        bool isHeld = Time.time < rig.keyLineEndTime;
        if (isHeld && priority < rig.heldPriority) {
            if (priority == CobraKingLinePriority.Key) {
                rig.pendingKeyLine = line;
            }
            return;
        }
        if (isHeld && rig.heldPriority == CobraKingLinePriority.Key
            && priority == CobraKingLinePriority.Urgent) {
            rig.pendingKeyLine = rig.lastLine;
        }
        rig.lastLine = line;
        rig.nextSameLineTime = Time.time + tuning.SpeechCooldownS;
        rig.heldPriority = priority;
        rig.keyLineEndTime = priority > CobraKingLinePriority.Normal
            ? Time.time + tuning.KeyLineHoldS
            : 0f;
        rig.hud.Say(line);
    }

    public void UpdatePendingLine() {
        if (rig.pendingKeyLine == null || Time.time < rig.keyLineEndTime) {
            return;
        }
        string line = rig.pendingKeyLine;
        rig.pendingKeyLine = null;
        SayNow(line, CobraKingLinePriority.Key);
    }

    // a decor primitive in the king's lit material, no collider
    public GameObject CreatePrimitive(
        PrimitiveType type,
        string objectName,
        Vector3 scale,
        Color color
    ) {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = objectName;
        part.transform.SetParent(rig.host, true);
        part.transform.localScale = scale;
        // off at once & removed at the frame's end: an immediate destroy is
        // refused when a part is made in a physics callback, eg a save's
        // burst on the great cobra's bite
        Collider partCollider = part.GetComponent<Collider>();
        partCollider.enabled = false;
        UnityEngine.Object.Destroy(partCollider);
        Renderer partRenderer = part.GetComponent<Renderer>();
        if (rig.body.PrimitiveMaterial != null) {
            partRenderer.sharedMaterial = rig.body.PrimitiveMaterial;
        }
        SetColor(partRenderer, color);
        return part;
    }

    public void SetColor(Renderer target, Color color) {
        if (target == null) {
            return;
        }
        target.GetPropertyBlock(rig.colorBlock);
        rig.colorBlock.SetColor(BASE_COLOR, color);
        rig.colorBlock.SetColor(COLOR, color);
        target.SetPropertyBlock(rig.colorBlock);
    }

}
