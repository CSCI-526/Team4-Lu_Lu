using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>The melons the monkey throws at the spider.</summary>
public sealed class SpiderKingMelons {
    // private members  ########################################################
    private readonly SpiderKingRig rig;
    private readonly SpiderKingTuning tuning;

    public SpiderKingMelons(SpiderKingRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    // Throws  -----------------------------------------------------------------
    // a copy of the grabbed fruit arcs round the trunk to the spider,
    // spinning; on arrival it hits
    public void LaunchMelon(GameObject source, Vector3 from) {
        GameObject copy = UnityEngine.Object.Instantiate(source);
        copy.transform.localScale = source.transform.lossyScale;
        SpiderKingFeedback.MakeDecor(copy);
        copy.SetActive(true);
        copy.transform.position = from;
        rig.throws.Add(new SpiderKingThrow {
            transform = copy.transform,
            from = from,
        });
    }

    public void UpdateThrows() {
        for (int i = rig.throws.Count - 1; i >= 0; i--) {
            SpiderKingThrow thrown = rig.throws[i];
            if (thrown.transform == null) {
                rig.throws.RemoveAt(i);
                continue;
            }
            thrown.ageS += Time.deltaTime;
            float t = Mathf.Clamp01(thrown.ageS / tuning.MelonFlyS);
            thrown.transform.position =
                rig.Movement.ArcPoint(thrown.from, rig.body.HeadPosition,
                t, 2.5f);
            thrown.transform.Rotate(Vector3.right, 720f * Time.deltaTime,
                Space.World);
            if (t < 1f) {
                continue;
            }
            UnityEngine.Object.Destroy(thrown.transform.gameObject);
            rig.throws.RemoveAt(i);
            OnMelonHit();
        }
    }

    public void ClearThrows() {
        foreach (SpiderKingThrow thrown in rig.throws) {
            if (thrown.transform != null) {
                UnityEngine.Object.Destroy(thrown.transform.gameObject);
            }
        }
        rig.throws.Clear();
    }

    public void OnMelonHit() {
        rig.Damage.Hurt(tuning.MelonDamage);
    }

}
