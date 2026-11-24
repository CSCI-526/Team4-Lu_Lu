using UnityEngine;
using static CobraMotorRig;

/// <summary>The climb speed of the chase, the head raise near the monkey, and
/// the exit boost.</summary>
public sealed class CobraMotorChase {
    // private members  ########################################################
    private readonly CobraMotorRig rig;
    private readonly CobraMotorConfig config;
    private readonly System.Action onChaseStarted;

    public CobraMotorChase(CobraMotorRig rig, System.Action onChaseStarted) {
        this.rig = rig;
        config = rig.config;
        this.onChaseStarted = onChaseStarted;
    }

    // Public Methods  #########################################################
    public float ChaseHeadRaiseBlend() {
        if (rig.isGuarding) {
            return 1f;
        }
        if (rig.player == null || rig.pathCenter == null) {
            return 0f;
        }
        float distance = Vector3.Distance(rig.player.position,
            rig.GetPathPosition(rig.headAngle, rig.headHeight));
        return Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(config.HeadRaiseStartDistanceU,
            Mathf.Min(config.HeadRaiseFullDistanceU,
                config.HeadRaiseStartDistanceU - .01f),
                distance));
    }

    /// <returns>vertical gap from the coil top up to the monkey, never
    /// below 0; U</returns>
    public float CalcDistanceBelowPlayer() {
        float coilTopHeight = rig.headHeight - config.NeckLiftU;
        return Mathf.Max(
            0f,
            rig.player.position.y - rig.pathCenter.position.y - coilTopHeight
        );
    }

    /// <returns>climb speed outside a surge: the monkey's top climb speed
    /// times the config's base speed curve at the monkey's ramped difficulty
    /// (0~3), so the chase tightens as the climb goes on, times the distance
    /// curve, so a far-behind cobra runs faster; else the inspector's climb
    /// speed; u/s</returns>
    public float BaseClimbSpeed() {
        if (
            rig.cobraBalanceConfig == null
            || rig.monkeyClimbConfig == null
            || rig.player == null
            || RampedDifficultyManager.I == null
        ) {
            return config.ClimbSpeedUPerS;
        }
        float difficulty =
            RampedDifficultyManager.I.GetRampedValueFromYPosition(
                rig.player.position.y
            );
        float distanceMultiplier =
            rig.cobraBalanceConfig.CobraDistanceSpeedCurve.Evaluate(
                CalcDistanceBelowPlayer()
            );
        float baseMultiplier = Mathf.Clamp(
            rig.cobraBalanceConfig.CobraBaseSpeedCurve.Evaluate(difficulty),
            0f,
            2f
        );
        return Mathf.Max(
            0f,
            rig.monkeyClimbConfig.TopClimbUpSpeedU
                * baseMultiplier
                * distanceMultiplier
        );
    }

    // the starting zone ending (to Playing) starts the one-off exit boost
    public void StartExitBoostOnZoneExit() {
        if (GameController.I == null) {
            return;
        }

        GameState state = GameController.I.State;
        bool didExitZone = rig.lastGameState == GameState.StartingZone
            && state == GameState.Playing;
        rig.lastGameState = state;
        if (!didExitZone) {
            return;
        }

        if (rig.cobraBalanceConfig != null) {
            rig.exitBoostEndTime =
                Time.time + rig.cobraBalanceConfig.CobraExitBoostDurationS;
        }
        onChaseStarted?.Invoke();
    }

}
