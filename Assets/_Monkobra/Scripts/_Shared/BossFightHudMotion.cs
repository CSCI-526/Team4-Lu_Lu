using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BossFightHudRig;

/// <summary>Moves and fades the HUD each frame: the call out, warnings, flash,
/// damage pops, speech, and panel animations.</summary>
public sealed class BossFightHudMotion {
    // private members  ########################################################
    private readonly BossFightHudRig rig;
    private readonly BossFightHudTuning tuning;

    public BossFightHudMotion(BossFightHudRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    // at the head a beat, gliding up to under the health bar, held, faded
    public void UpdateCallOut() {
        if (rig.callOutRect == null) {
            return;
        }
        rig.callOutAgeS += Time.unscaledDeltaTime;
        PlaceOnScreen(rig.callOutRect, rig.callOutWorld, tuning.SpeechOffset);
        Vector2 from = rig.callOutRect.anchoredPosition;
        Vector2 half = rig.canvasRect != null ? rig.canvasRect.rect.size * 0.5f
            : Vector2.zero;
        Vector2 docked = new Vector2(0f,
            half.y - 96f - rig.callOutRect.rect.height * 0.5f);
        float glide = Mathf.Clamp01((rig.callOutAgeS - CALL_AT_HEAD_S)
            / CALL_GLIDE_S);
        glide = glide * glide * (3f - 2f * glide);
        rig.callOutRect.anchoredPosition = Vector2.Lerp(from, docked, glide);
        // a pop as it shows
        float pop = Mathf.Clamp01(rig.callOutAgeS / 0.15f);
        rig.callOutRect.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, pop);
    }

    // signs beat faster & grow as the hit nears
    public void UpdateWarnings() {
        foreach (BossFightHudWarning warning in rig.warnings) {
            if (warning.sign == null) {
                continue;
            }
            Place(warning.sign, warning.worldPosition, tuning.WarningOffset);
            float beatHz = Mathf.Lerp(2f, 9f, warning.progress);
            float beat =
                0.5f + 0.5f * Mathf.Sin(Time.time * beatHz * 2f * Mathf.PI);
            float scale = Mathf.Lerp(0.9f, 1.3f, warning.progress)
                * (1f + 0.12f * beat);
            warning.sign.localScale = Vector3.one * scale;
        }
    }

    public void UpdateFlash() {
        if (rig.flashImage == null) {
            return;
        }
        if (rig.flashLeftS > 0f) {
            rig.flashLeftS -= Time.unscaledDeltaTime;
        }
        float alpha = tuning.FlashS > 0f
            ? Mathf.Clamp01(rig.flashLeftS / tuning.FlashS) : 0f;
        rig.flashImage.color = new Color(
            rig.flashColor.r, rig.flashColor.g, rig.flashColor.b, alpha
        );
        rig.flashImage.enabled = alpha > 0f;
    }

    public void UpdatePops() {
        for (int i = rig.pops.Count - 1; i >= 0; i--) {
            BossFightHudPop pop = rig.pops[i];
            pop.ageS += Time.deltaTime;
            float t = pop.ageS / POP_S;
            if (t >= 1f || pop.label == null) {
                if (pop.label != null) {
                    UnityEngine.Object.Destroy(pop.label.gameObject);
                }
                rig.pops.RemoveAt(i);
                continue;
            }
            Place(
                pop.label.rectTransform,
                pop.worldPosition,
                new Vector2(0f, 40f + t * 80f)
            );
            pop.label.alpha = 1f - t * t;
            rig.pops[i] = pop;
        }
    }

    // keeps a UI rect over a world point, hidden behind the camera
    public void Place(RectTransform target, Vector3 world, Vector2 offset) {
        Camera cam = Camera.main;
        if (cam == null || rig.canvasRect == null) {
            return;
        }
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z < 0f) {
            target.anchoredPosition = new Vector2(0f, -10000f);
            return;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rig.canvasRect, screen, rig.canvasCamera, out Vector2 local
        );
        target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = local + offset;
    }

    // like Place, but kept inside the screen: a head off screen or behind
    // the camera still shows its line, pinned to the nearest edge
    // over the king's head while it is well in view; otherwise, eg the
    // camera turned away or the head off screen, docked under the health bar
    // with the king's name, so a line is never missed
    public void PlaceSpeech() {
        Camera cam = Camera.main;
        Vector3 view = cam != null
            ? cam.WorldToViewportPoint(rig.speechWorldPosition)
            : Vector3.back;
        bool isHeadInView = view.z > 0f
            && view.x > 0.1f && view.x < 0.9f
            && view.y > 0.12f && view.y < 0.75f;
        if (isHeadInView) {
            PlaceOnScreen(rig.speechRect, rig.speechWorldPosition,
                tuning.SpeechOffset);
            return;
        }
        rig.speechRect.anchorMin = rig.speechRect.anchorMax =
            new Vector2(0.5f, 1f);
        rig.speechRect.pivot = new Vector2(0.5f, 1f);
        rig.speechRect.anchoredPosition = new Vector2(0f, -96f);
    }

    public void PlaceOnScreen(
        RectTransform target,
        Vector3 world,
        Vector2 offset
    ) {
        Camera cam = Camera.main;
        if (cam == null || rig.canvasRect == null) {
            return;
        }
        Vector2 half = rig.canvasRect.rect.size * 0.5f;
        Vector2 box = target.rect.size * 0.5f + new Vector2(16f, 16f);
        Vector2 local;
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z < 0f) {
            // behind the camera: the top of the screen, where the king is
            local = new Vector2(0f, half.y);
        } else {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rig.canvasRect, screen, rig.canvasCamera, out local
            );
            local += offset;
        }
        // stay clear of the name & health bar at the top
        float top = half.y - box.y - 90f;
        local.x = Mathf.Clamp(local.x, -half.x + box.x, half.x - box.x);
        local.y = Mathf.Clamp(local.y, -half.y + box.y, top);
        target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = local;
    }

    // the ad's title throbs and its shapes bob & spin; time stands still
    // under it, so this runs on unscaled time
    public void UpdateAd() {
        if (rig.adPanel == null || !rig.adPanel.activeSelf) {
            return;
        }
        float t = Time.unscaledTime;
        rig.adTitle.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 6f));
        for (int i = 0; i < rig.adShapes.Count; i++) {
            rig.adShapes[i].localRotation = Quaternion.Euler(
                0f, 0f, t * (40f + i * 12f) * (i % 2 == 0 ? 1f : -1f)
            );
            rig.adShapes[i].localScale = Vector3.one
                * (1f + 0.15f * Mathf.Sin(t * 3f + i));
        }
    }

    // the Continue button beats gently; time stands still, so unscaled
    public void UpdateRewardPanel() {
        if (rig.rewardPanel == null || !rig.rewardPanel.activeSelf) {
            return;
        }
        rig.rewardAgeS += Time.unscaledDeltaTime;
        float beat = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
        rig.rewardButton.localScale = Vector3.one * (1f + 0.06f * beat);
    }

    // the badge pops in big & settles; unscaled, so a pause never stalls it
    public void UpdateArmorBadge() {
        if (rig.armorBadge == null || rig.armorBadgePopS <= 0f) {
            return;
        }
        rig.armorBadgePopS =
            Mathf.Max(0f, rig.armorBadgePopS - Time.unscaledDeltaTime);
        float left = rig.armorBadgePopS / ARMOR_BADGE_POP_S;
        rig.armorBadge.localScale = Vector3.one * (1f + 0.8f * left * left);
    }

}
