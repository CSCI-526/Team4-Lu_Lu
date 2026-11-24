using UnityEngine;
using static CobraMotorRig;

/// <summary>The boss arena guard: the held height and the peeking
/// head.</summary>
public sealed class CobraMotorGuard {
    // private members  ########################################################
    private readonly CobraMotorRig rig;
    private readonly CobraMotorConfig config;

    public CobraMotorGuard(CobraMotorRig rig) {
        this.rig = rig;
        config = rig.config;
    }

    // Public Methods  #########################################################
    // guarding: hold still below the arena, the head poking up now & then
    public void UpdateGuard() {
        float peek = 0f;
        float t = Time.time - rig.nextPeekTime;
        float upS = config.PeekTimesS.x;
        float holdS = config.PeekTimesS.y;
        float downS = config.PeekTimesS.z;
        if (t >= 0f) {
            if (t < upS) {
                // springs up, overshooting a little before it settles
                peek = EaseOutBack(t / upS);
            } else if (t < upS + holdS) {
                // sways gently while it looks around
                peek = 1f + 0.05f * Mathf.Sin((t - upS) * 9f);
            } else if (t < upS + holdS + downS) {
                float d = (t - upS - holdS) / downS;
                peek = 1f - d * d * (3f - 2f * d);
            } else {
                rig.nextPeekTime = Time.time + UnityEngine.Random.Range(
                    config.PeekIntervalRangeS.x, config.PeekIntervalRangeS.y
                );
            }
        }
        rig.headHeight = rig.guardHeadHeight;
        rig.guardPeek = peek;
    }

    // a peek lifts the first stretch of neck off the coil in a smooth S:
    // it bows out from the trunk, then rises, the head on top; the bend
    // fades in along the neck, so it joins the coil without a kink
    public void ApplyGuardPeek(float arcStep) {
        if (rig.guardPeek <= 0f || rig.segmentPositions.Length < 2) {
            return;
        }
        int anchor = Mathf.Clamp(
            Mathf.CeilToInt(config.PeekNeckU / Mathf.Max(0.01f, arcStep)),
            1,
            rig.segmentPositions.Length - 1
        );
        Vector3 root = rig.segmentPositions[anchor];
        Vector3 outward = root - rig.pathCenter.position;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 0.0001f
            ? outward.normalized
            : Vector3.forward;
        float sway = Mathf.Sin(Time.time * 2.3f) * 0.25f * rig.guardPeek;
        Vector3 side = Vector3.Cross(Vector3.up, outward);

        for (int i = 0; i < anchor; i++) {
            float s = (anchor - i) / (float)anchor;
            // 0 at the coil, 1 head
            Vector3 raised = root
                + Vector3.up * config.PeekRiseU * rig.guardPeek
                    * Mathf.Pow(s, 1.15f)
                + outward * config.PeekCurlU * rig.guardPeek
                    * Mathf.Sin(s * Mathf.PI)
                + side * sway * s * s;
            float blend =
                Mathf.Clamp01(rig.guardPeek) * (s * s * (3f - 2f * s));
            rig.segmentPositions[i] = Vector3.Lerp(
                rig.segmentPositions[i], raised, Mathf.Clamp01(blend * 1.6f)
            );
        }
    }

    public static float EaseOutBack(float x) {
        const float c1 = 1.4f;
        const float c3 = c1 + 1f;
        float m = x - 1f;
        return 1f + c3 * m * m * m + c1 * m * m;
    }

}
