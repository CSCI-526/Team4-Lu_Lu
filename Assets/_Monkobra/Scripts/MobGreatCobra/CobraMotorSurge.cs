using UnityEngine;
using static CobraMotorRig;

/// <summary>Counts the monkey's mistakes and surges the cobra for
/// each.</summary>
public sealed class CobraMotorSurge {
    // private members  ########################################################
    private readonly CobraMotorRig rig;
    private readonly CobraMotorTuning tuning;
    private readonly System.Action<int> onCatchUpStarted;

    public CobraMotorSurge(CobraMotorRig rig,
        System.Action<int> onCatchUpStarted) {
        this.rig = rig;
        tuning = rig.tuning;
        this.onCatchUpStarted = onCatchUpStarted;
    }

    // Public Methods  #########################################################
    // a branch or falling cobra hit, or a web, costs the monkey time; the
    // cobra surges to make the mistake felt
    public void ListenForMistakes(GameObject playerObject) {
        rig.playerSensor =
            playerObject.GetComponentInChildren<HitBranchSensor>(true);
        if (rig.playerSensor != null) {
            rig.playerSensor.BranchHit += OnMonkeyHit;
        }
        rig.playerStruggle = playerObject.GetComponent<SpiderWebStruggle>();
        if (rig.playerStruggle != null) {
            rig.playerStruggle.Trapped += OnMonkeyTrapped;
        }
    }

    // one stun is one mistake: hits the monkey ignores while stunned, e.g.
    // a 2nd branch on the way down, add nothing
    public void OnMonkeyHit(Collider hazard) {
        if (rig.isGuarding) {
            return;
        }
        if (tuning.PlayerBalanceConfig != null
            && Time.time < rig.nextCountedHitTime) {
            return;
        }
        if (tuning.PlayerBalanceConfig != null) {
            rig.nextCountedHitTime =
                Time.time + tuning.PlayerBalanceConfig.HitStunDurationS;
        }
        StartSurge();
    }

    // raised on every trap; its flag only says if it is the first trap of
    // the scene, so it is ignored here
    public void OnMonkeyTrapped(bool isFirstTrap) {
        if (rig.isGuarding) {
            return;
        }
        StartSurge();
    }

    // each mistake adds a stack and surge time, both capped by the config;
    // a frozen cobra ignores mistakes
    public void StartSurge() {
        if (
            tuning.CobraBalanceConfig == null
            || tuning.CobraBalanceConfig.CobraSurgePerMistakeS <= 0f
            || rig.isFrozen
            // it holds still in the starting zone, so no surge & no cue
            || IsInStartingZone
        ) {
            return;
        }
        float remainingS = Mathf.Max(0f, rig.surgeEndTime - Time.time);
        if (rig.surgeStacks == 0) {
            remainingS = 0f;
        }
        rig.surgeEndTime =
            Time.time
            + Mathf.Min(
                remainingS + tuning.CobraBalanceConfig.CobraSurgePerMistakeS,
                tuning.CobraBalanceConfig.CobraMaxSurgeS
            );
        rig.surgeStacks++;
        onCatchUpStarted?.Invoke(rig.surgeStacks);
    }

    /// <summary>Stops listening to the monkey's mistakes.</summary>
    public void StopListening() {
        if (rig.playerSensor != null) {
            rig.playerSensor.BranchHit -= OnMonkeyHit;
        }
        if (rig.playerStruggle != null) {
            rig.playerStruggle.Trapped -= OnMonkeyTrapped;
        }
    }

}
