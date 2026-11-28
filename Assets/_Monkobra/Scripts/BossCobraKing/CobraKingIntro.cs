using UnityEngine;

/// <summary>
/// The Cobra King's part of the intro cut scene: it rears and strikes
/// harmlessly round the monkey, a feint to one side then the other. Plugged
/// into <see cref="BossIntroCutScene.Playing"/>; each strike is the king's
/// own wind-up & dive, but aimed past the monkey and w/ no impact, so no
/// heart is lost (a touch is ignored while the head strikes, see
/// <see cref="CobraKingAttacks.OnKingTouched"/>).
/// </summary>
public sealed class CobraKingIntro {
    // private members  ########################################################
    private readonly CobraKingRig rig;
    private readonly CobraKingConfig config;
    private int feintsDone;

    public CobraKingIntro(CobraKingRig rig) {
        this.rig = rig;
        config = rig.config;
    }

    // Public Methods  #########################################################
    /// <summary>Starts counting the feints of a new intro.</summary>
    public void Begin() {
        feintsDone = 0;
    }

    /// <summary>
    /// The scene's frame hook: starts the next feint once it is due and the
    /// head is free.
    /// </summary>
    /// <param name="elapsedS">seconds since the cut scene began</param>
    public void Update(float elapsedS) {
        if (feintsDone >= config.IntroFeintCount || rig.monkey == null
            || rig.body.IsStriking) {
            return;
        }
        float dueS = config.IntroFeintStartS
            + feintsDone * config.IntroFeintGapS;
        if (elapsedS < dueS) {
            return;
        }
        rig.body.StartStrike(
            FeintTarget(feintsDone), config.IntroFeintWindUpS, null
        );
        feintsDone++;
    }

    // private methods  ########################################################
    // a point beside the monkey, left then right round the trunk, never on it
    private Vector3 FeintTarget(int index) {
        Vector3 outward = rig.monkey.position - rig.trunkCenter;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 0.001f
            ? outward.normalized : Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, outward);
        float sign = index % 2 == 0 ? -1f : 1f;
        return rig.monkey.position + side * (sign * config.IntroFeintMissU)
            + Vector3.up * config.IntroFeintLiftU;
    }
}
