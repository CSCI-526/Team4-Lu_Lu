using UnityEngine;

/// <summary>
/// The Spider King's part of the intro cut scene: it drops in from above on
/// a silk line, legs scrambling, lands on its start spot, then rears up and
/// screeches, its eyes glowing, like the web intro event's entry. Plugged
/// into <see cref="BossIntroCutScene.Playing"/>; <see cref="End"/> puts the
/// body back to rest and clears the silk, however the scene ended.
/// </summary>
public sealed class SpiderKingIntro {
    // private members  ########################################################
    // the silk reaches this far above the drop's top, off the screen; u
    private const float SILK_OVERSHOOT_U = 6f;

    private readonly SpiderKingRig rig;
    private readonly SpiderKingConfig config;
    private GameObject silk;
    private bool isBegun; // the body was moved by this intro

    public SpiderKingIntro(SpiderKingRig rig) {
        this.rig = rig;
        config = rig.config;
    }

    // Public Methods  #########################################################
    /// <summary>
    /// The scene's frame hook: poses the spider for the seconds into the
    /// scene, drop, landing, then the rearing screech.
    /// </summary>
    /// <param name="elapsedS">seconds since the cut scene began</param>
    public void Update(float elapsedS) {
        if (!rig.body.IsBuilt) {
            return;
        }
        isBegun = true;
        float topY = rig.startY + config.IntroDropU;
        float dropS = Mathf.Max(0.01f, config.IntroDropS);
        if (elapsedS < dropS) {
            // eased out: it slows as it nears its spot
            float t = elapsedS / dropS;
            float eased = 1f - (1f - t) * (1f - t);
            rig.body.Place(
                rig.startAngle, Mathf.Lerp(topY, rig.startY, eased), 1f
            );
            DrawSilk(topY + SILK_OVERSHOOT_U);
            return;
        }

        DestroySilk();
        float sinceLandS = elapsedS - dropS;
        float rear = Mathf.SmoothStep(
            0f, 1f,
            (sinceLandS - config.IntroLandS)
                / Mathf.Max(0.01f, config.IntroScreechS)
        );
        rig.body.SetCharge(rear);
        rig.body.SetSwipe(rear);
        rig.body.SetEyeGlow(rear);
        rig.body.Place(rig.startAngle, rig.startY, 0f);
    }

    /// <summary>
    /// Ends the intro: the silk goes and the body rests on its start spot,
    /// calm, w/ dark eyes. Does nothing to a body the intro never moved, eg
    /// a skipped scene.
    /// </summary>
    public void End() {
        DestroySilk();
        if (!isBegun) {
            return;
        }
        isBegun = false;
        rig.body.SetCharge(0f);
        rig.body.SetSwipe(0f);
        rig.body.SetEyeGlow(0f);
        rig.body.Place(rig.startAngle, rig.startY, 0f);
    }

    // private methods  ########################################################
    // a thin vertical line from the spider up to <paramref name="anchorY"/>
    private void DrawSilk(float anchorY) {
        Vector3 bottom = rig.body.Position;
        float lengthU = Mathf.Max(0.01f, anchorY - bottom.y);
        if (silk == null) {
            silk = rig.Feedback.CreatePrimitive(
                PrimitiveType.Cylinder, "IntroSilk", Vector3.one,
                config.SilkColor
            );
        }
        // a cylinder is 2 u tall
        silk.transform.localScale = new Vector3(
            config.IntroSilkWidthU, lengthU * 0.5f, config.IntroSilkWidthU
        );
        silk.transform.position =
            new Vector3(bottom.x, bottom.y + lengthU * 0.5f, bottom.z);
        silk.transform.rotation = Quaternion.identity;
    }

    private void DestroySilk() {
        if (silk != null) {
            Object.Destroy(silk);
            silk = null;
        }
    }
}
