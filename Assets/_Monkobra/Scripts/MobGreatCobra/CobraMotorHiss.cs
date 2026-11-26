using UnityEngine;
using static CobraMotorRig;

/// <summary>The close chase hiss and its alert cue.</summary>
public sealed class CobraMotorHiss {
    // private members  ########################################################
    private readonly CobraMotorRig rig;
    private readonly CobraMotorConfig config;

    public CobraMotorHiss(CobraMotorRig rig) {
        this.rig = rig;
        config = rig.config;
    }

    // Public Methods  #########################################################
    public void CreateProximityHiss() {
        if (config.ProximityHiss == null) {
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
        rig.proximityHissSource.clip = config.ProximityHiss;
        rig.proximityHissSource.playOnAwake = false;
        rig.proximityHissSource.loop = false;
        rig.proximityHissSource.volume = config.HissVolume;
        rig.proximityHissSource.pitch = config.HissPitch;
        rig.proximityHissSource.spatialBlend = 1f;
        rig.proximityHissSource.dopplerLevel = 0f;
        rig.proximityHissSource.rolloffMode = AudioRolloffMode.Linear;
        rig.proximityHissSource.minDistance = config.HissMinDistanceU;
        rig.proximityHissSource.maxDistance =
            Mathf.Max(config.HissMinDistanceU, config.HissMaxDistanceU);
        GameObject alert = new GameObject("GreatCobraChaseAlertHiss");
        alert.transform.SetParent(rig.host, false);
        rig.chaseAlertHissSource = alert.AddComponent<AudioSource>();
        rig.chaseAlertHissSource.clip = config.ProximityHiss;
        rig.chaseAlertHissSource.playOnAwake = false;
        rig.chaseAlertHissSource.loop = false;
        rig.chaseAlertHissSource.volume = config.ChaseAlertHissVolume;
        rig.chaseAlertHissSource.pitch = config.HissPitch;
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
                || (RunController.I != null
                    && !RunController.I.IsRunLive)))
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
            && (RunController.I == null || RunController.I.IsRunLive)
            && distanceSquared <= Mathf.Pow(config.HissStartDistanceU
                +(rig.wasInHissRange?config.HissExitPaddingU:0f), 2f);
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
                    +Mathf.Max(config.HissIntervalS,
                        config.ProximityHiss.length
                            / Mathf.Max(.1f, config.HissPitch));
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
