using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static CobraKingRig;

/// <summary>The shield ring, its dots, and the golden banana that renews the
/// armor.</summary>
public sealed class CobraKingArmor {
    // private members  ########################################################
    private readonly CobraKingRig rig;
    private readonly CobraKingConfig config;
    private Renderer[] crownRenderers; // parts of the worn crown, to tint

    public CobraKingArmor(CobraKingRig rig) {
        this.rig = rig;
        config = rig.config;
    }

    // Public Methods  #########################################################
    public void MakeGolden(GameObject banana) {
        // its own outline replaces the reach-target one
        Material mask = null;
        foreach (FruitOutlineListener listener in
            banana.GetComponentsInChildren<FruitOutlineListener>(true)) {
            if (mask == null && listener.Style != null) {
                mask = listener.Style.GetMaskMaterial();
            }
            UnityEngine.Object.Destroy(listener);
        }
        Material outline = null;
        if (config.GoldenOutlineMaterial != null) {
            outline = new Material(config.GoldenOutlineMaterial);
            outline.SetColor(OUTLINE_COLOR, config.GoldenOutlineColor);
            outline.SetFloat(
                OUTLINE_WIDTH,
                config.GoldenOutlineMaterial.GetFloat(OUTLINE_WIDTH)
                    * config.GoldenOutlineWidthScale
            );
        }
        foreach (Renderer part in
            banana.GetComponentsInChildren<Renderer>(true)) {
            Material baseMaterial = part.sharedMaterial;
            if (outline != null) {
                part.sharedMaterials = mask != null
                    ? new[] { baseMaterial, mask, outline }
                    : new[] { baseMaterial, outline };
            }
            rig.Feedback.SetColor(part, config.GoldenColor);
        }
    }

    // now & then the golden banana grows in sight, beside the monkey, for a
    // short while; only once the first banana has hit the king
    public void UpdateGolden() {
        if (rig.goldenIndex < 0 || rig.phase != CobraKingPhase.Fighting
            || !rig.hasHit) {
            return;
        }
        CobraKingArenaFruit golden = rig.fruits[rig.goldenIndex];
        if (golden.branch.activeSelf) {
            if (Time.time >= rig.goldenHideTime) {
                HideGolden();
                rig.nextGoldenTime = Time.time + config.GoldenIntervalS;
            }
            return;
        }
        if (Time.time >= rig.nextGoldenTime && !rig.hasShield) {
            ShowGolden();
        }
    }

    public void ShowGolden() {
        CobraKingArenaFruit golden = rig.fruits[rig.goldenIndex];
        float side = Random.value < 0.5f ? -1f : 1f;
        float angle =
            rig.AngleOf(rig.monkey.position) + side * config.GoldenSideDeg;
        float y = Mathf.Clamp(
            rig.monkey.position.y + config.GoldenAboveMonkeyU,
            rig.entryY + config.FruitHeightRangeU.x,
            rig.body.LowestY - config.CeilingGapU - 0.5f
        );
        rig.Arena.PlaceBranch(golden.branch.transform, angle, y);
        golden.branch.SetActive(true);
        golden.root.SetActive(true);
        rig.goldenHideTime = Time.time + config.GoldenShowS;
        // the first one of a session gets the popup w/ a line to it
        GoldenMelonHint.Show(golden.root.transform);
    }

    public void HideGolden() {
        CobraKingArena.ForgetFruits(rig.fruits[rig.goldenIndex].branch);
        rig.fruits[rig.goldenIndex].branch.SetActive(false);
    }

    // dotted line from the first banana to the crown, until the 1st hit
    public void BuildDots() {
        rig.dots = new Transform[config.DotCount];
        for (int i = 0; i < config.DotCount; i++) {
            rig.dots[i] = rig.Feedback.CreatePrimitive(
                PrimitiveType.Sphere, "HintDot" + i,
                Vector3.one * config.DotRadiusU * 2f, Color.white
            ).transform;
        }
    }

    // the golden crown worn while shielded: the king's crown model, alone on
    // the monkey's head
    public void BuildShieldRing() {
        Transform crown = CobraKingBody.BuildCrown(
            rig.host, rig.body.PrimitiveMaterial, config.GoldenColor
        );
        crown.name = "ArmorCrown";
        crown.localScale = Vector3.one * config.ArmorCrownScale;
        crownRenderers = crown.GetComponentsInChildren<Renderer>();
        rig.armor = new[] { crown };
        rig.armorBaseScales = new[] { crown.localScale };
        crown.gameObject.SetActive(false);
    }

    // Golden Banana  ----------------------------------------------------------
    public void TakeGoldenBanana() {
        StaminaBarController stamina = StaminaBarController.I;
        if (stamina != null) {
            stamina.AddStamina(stamina.MaxStamina);
        }
        rig.hasShield = true;
        rig.hud.PopText("Shield!", rig.monkey.position, config.GoldenColor);
    }

    // the hud badge follows the armor worn, shown once a won armor has
    // landed; one sync here covers every way it is gained or lost
    private void SyncArmorBadge() {
        bool isWorn = rig.hasShield
            && (rig.armorRewardTimeS < 0f || rig.hasArmorLanded);
        if (isWorn == rig.isArmorBadgeOn) {
            return;
        }
        rig.isArmorBadgeOn = isWorn;
        rig.hud.ShowArmorBadge(isWorn);
    }

    // the armor follows the monkey; its back faces out from the trunk
    public void UpdateShieldRing() {
        SyncArmorBadge();
        if (rig.armor == null) {
            return;
        }
        Vector3 outward = rig.monkey.position - rig.trunkCenter;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 0.001f ? outward.normalized
            : Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, outward);
        float shine = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
        Color gold = Color.Lerp(config.GoldenColor, Color.white, shine * 0.35f);
        float rewardT = UpdateArmorReward();

        // the crown sits on the monkey's head
        Vector3 headPosition =
            rig.monkey.position + Vector3.up * config.ArmorHelmetU;
        for (int i = 0; i < rig.armor.Length; i++) {
            rig.armor[i].gameObject.SetActive(rig.hasShield);
            if (!rig.hasShield) {
                continue;
            }
            Vector3 position = headPosition;
            float scale = 1f;
            Color color = gold;
            if (rewardT >= 0f) {
                // swirl: each piece spirals in from its own side, growing
                // smaller as it nears, then all pulse bright once it lands
                float fly = Mathf.Clamp01(rewardT / ARMOR_LAND_SHARE);
                float ease = 1f - Mathf.Pow(1f - fly, 3f);
                float radius = Mathf.Lerp(config.ArmorRewardRadiusU, 0f, ease);
                float spin = (i * 90f + fly * 540f) * Mathf.Deg2Rad;
                position += (outward * Mathf.Cos(spin) + side * Mathf.Sin(spin))
                    * radius + Vector3.up * radius * 0.7f;
                if (rewardT < ARMOR_LAND_SHARE) {
                    scale = Mathf.Lerp(1.8f, 1f, ease);
                } else {
                    float pulse = Mathf.Sin(
                        (rewardT - ARMOR_LAND_SHARE) / (1f - ARMOR_LAND_SHARE)
                            * Mathf.PI
                    );
                    scale = 1f + 0.35f * pulse;
                    color = Color.Lerp(gold, Color.white, pulse * 0.8f);
                }
            }
            rig.armor[i].position = position;
            rig.armor[i].localScale = rig.armorBaseScales[i] * scale;
            rig.armor[i].rotation = Quaternion.LookRotation(
                outward, Vector3.up
            );
            foreach (Renderer part in crownRenderers) {
                rig.Feedback.SetColor(part, color);
            }
        }
    }

    // the win reward's swirl, 0~1 while it plays, else -1; on landing a
    // gold burst & flash, & the armor badge that stays while it is kept
    public float UpdateArmorReward() {
        if (rig.armorRewardTimeS < 0f) {
            return -1f;
        }
        rig.armorRewardTimeS += Time.deltaTime;
        float t = Mathf.Clamp01(rig.armorRewardTimeS / config.ArmorRewardS);
        if (!rig.hasArmorLanded && t >= ARMOR_LAND_SHARE) {
            rig.hasArmorLanded = true;
            rig.Throws.BurstAt(rig.monkey.position + Vector3.up * 0.4f,
                config.GoldenColor);
        }
        if (t >= 1f) {
            rig.armorRewardTimeS = -1f;
        }
        return t;
    }

}
