using UnityEngine;
using static CobraMotorRig;

/// <summary>The close chase hiss and its alert cue.</summary>
public sealed class CobraMotorHiss {
    // Private Members  ########################################################
    private readonly CobraMotorRig rig;
    private readonly CobraMotorTuning tuning;

    public CobraMotorHiss(CobraMotorRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    public void CreateProximityHiss() {
        if (tuning.ProximityHiss == null) {
            return;
        }
        rig.hissBodyShapes = new CapsuleCollider[rig.bodySegments.Length];
        for (int i = 0; i<rig.bodySegments.Length; i++) {
            if (rig.bodySegments[i] != null)
                rig.bodySegments[i].TryGetComponent(out rig.hissBodyShapes[i]);
        }
        GameObject voice = new GameObject("GreatCobraHiss");
        voice.transform.SetParent(rig.host, false);
        rig.proximityHissSource = voice.AddComponent<AudioSource>();
        rig.proximityHissSource.clip = tuning.ProximityHiss;
        rig.proximityHissSource.playOnAwake = false;
        rig.proximityHissSource.loop = false;
        rig.proximityHissSource.volume = tuning.HissVolume;
        rig.proximityHissSource.pitch = tuning.HissPitch;
        rig.proximityHissSource.spatialBlend = 1f;
        rig.proximityHissSource.dopplerLevel = 0f;
        rig.proximityHissSource.rolloffMode = AudioRolloffMode.Linear;
        rig.proximityHissSource.minDistance = tuning.HissMinDistanceU;
        rig.proximityHissSource.maxDistance =
            Mathf.Max(tuning.HissMinDistanceU, tuning.HissMaxDistanceU);
        GameObject alert = new GameObject("GreatCobraChaseAlertHiss");
        alert.transform.SetParent(rig.host, false);
        rig.chaseAlertHissSource = alert.AddComponent<AudioSource>();
        rig.chaseAlertHissSource.clip = tuning.ProximityHiss;
        rig.chaseAlertHissSource.playOnAwake = false;
        rig.chaseAlertHissSource.loop = false;
        rig.chaseAlertHissSource.volume = tuning.ChaseAlertHissVolume;
        rig.chaseAlertHissSource.pitch = tuning.HissPitch;
        // The introduction shot must be audible while the monkey is still far
        // above the cobra.
        rig.chaseAlertHissSource.spatialBlend = 0f;
    }

    public void UpdateProximityHiss(Vector3 headPosition) {
        if (rig.proximityHissSource == null) {
            return;
        }
        if (rig.chaseAlertHissSource != null
            && (rig.isFrozen || rig.isGuarding
                || (GameController.I != null
                    && !GameController.I.IsRunLive)))
            rig.chaseAlertHissSource.Stop();
        bool alertPlaying = rig.chaseAlertHissSource != null
            && rig.chaseAlertHissSource.isPlaying;
        Vector3 nearest = headPosition;
        float distanceSquared = rig.player != null
            ?(rig.player.position - nearest).sqrMagnitude
                :float.PositiveInfinity;
        if (rig.player != null && rig.hissBodyShapes != null) {
            foreach (var shape in rig.hissBodyShapes) {
                if (shape == null || !shape.enabled
                    || !shape.gameObject.activeInHierarchy) {
                    continue;
                }
                Vector3 point = shape.ClosestPoint(rig.player.position);
                float candidate =
                    (point - rig.player.position).sqrMagnitude;
                if (candidate < distanceSquared) {
                    distanceSquared = candidate;
                    nearest = point;
                }
            }
        }
        rig.proximityHissSource.transform.position = nearest;
        bool near = rig.player != null && !rig.isFrozen && !rig.isGuarding
            && Time.timeScale>0f
            && (GameController.I == null || GameController.I.IsRunLive)
            && distanceSquared <= Mathf.Pow(tuning.HissStartDistanceU
                +(rig.wasInHissRange?tuning.HissExitPaddingU:0f), 2f);
        if (!near) {
            StopProximityHiss();
            return;
        }
        if (!rig.wasInHissRange && !alertPlaying) {
            rig.nextProximityHissTime = Time.time;
        }
        rig.wasInHissRange = true;
        if (!alertPlaying && Time.time >= rig.nextProximityHissTime
            && !rig.proximityHissSource.isPlaying) {
            rig.proximityHissSource.Play();
            rig.nextProximityHissTime =
                Time.time
                    +Mathf.Max(tuning.HissIntervalS,
                        tuning.ProximityHiss.length
                            / Mathf.Max(.1f, tuning.HissPitch));
        }
    }

    public void StopProximityHiss() {
        rig.wasInHissRange = false;
        if (rig.proximityHissSource != null
            && rig.proximityHissSource.isPlaying) {
            rig.proximityHissSource.Stop();
        }
    }

}
