using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tints the great cobra light blue while a coconut freezes it, so the frozen
/// state reads at a glance, and blinks back toward its own color in the last
/// seconds b4 it moves again. Listens to <see cref="CobraMotor.Frozen"/> and
/// <see cref="CobraMotor.Thawed"/>.
/// <para>
/// Put it next to <see cref="CobraMotor"/>. It tints every renderer below it
/// that has no color override of its own, so the black eyes stay black; the
/// override is dropped on thaw, so the body keeps its material's color.
/// </para>
/// </summary>
[RequireComponent(typeof(CobraMotor))]
public class CobraFreezeTint: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("color mixed into the body while frozen")]
    private Color frozenColor = new Color(0.6f, 0.85f, 1f, 1f);

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("share of the frozen color mixed into each part's own color")]
    private float tintStrength = 0.75f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("the tint blinks this long b4 the cobra moves again; s")]
    private float endingWarnS = 3f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("blinks per s near the end")]
    private float endingBlinkHz = 4f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        cobra = GetComponent<CobraMotor>();
    }

    // after CobraMotor's Awake, so its cloned segments & eyes exist
    private void Start() {
        if (cobra == null) {
            enabled = false;
            return;
        }

        foreach (Renderer part in GetComponentsInChildren<Renderer>(true)) {
            Material material = part.sharedMaterial;
            if (part.HasPropertyBlock() || material == null) {
                continue;
            }
            if (material.HasProperty(BASE_COLOR_ID)) {
                parts.Add(new TintedPart(part, BASE_COLOR_ID,
                    material.GetColor(BASE_COLOR_ID)));
            } else if (material.HasProperty(COLOR_ID)) {
                parts.Add(new TintedPart(part, COLOR_ID,
                    material.GetColor(COLOR_ID)));
            }
        }

        cobra.Frozen += OnFrozen;
        cobra.Thawed += OnThawed;
    }

    private void OnDestroy() {
        if (cobra != null) {
            cobra.Frozen -= OnFrozen;
            cobra.Thawed -= OnThawed;
        }
    }

    private void Update() {
        if (!isTinted) {
            return;
        }

        float strength = tintStrength;
        if (cobra.FreezeRemainingS <= endingWarnS) {
            // 0~1 pulse, frozen with the game clock
            float pulse = 0.5f + 0.5f
                * Mathf.Sin(Time.time * endingBlinkHz * 2f * Mathf.PI);
            strength *= pulse;
        }
        ApplyTint(strength);
    }

    // Event Handlers  #########################################################
    private void OnFrozen(float durationS) {
        isTinted = true;
        ApplyTint(tintStrength);
    }

    private void OnThawed() {
        isTinted = false;
        foreach (TintedPart part in parts) {
            if (part.Renderer != null) {
                part.Renderer.SetPropertyBlock(null);
            }
        }
    }

    // constants  ##############################################################
    private static readonly int BASE_COLOR_ID =
        Shader.PropertyToID("_BaseColor");

    private static readonly int COLOR_ID = Shader.PropertyToID("_Color");

    // private members  ########################################################
    private readonly struct TintedPart {
        public readonly Renderer Renderer;
        public readonly int ColorId;
        public readonly Color BaseColor;

        public TintedPart(Renderer renderer, int colorId, Color baseColor) {
            Renderer = renderer;
            ColorId = colorId;
            BaseColor = baseColor;
        }
    }

    private CobraMotor cobra;
    private bool isTinted;
    private readonly List<TintedPart> parts = new();
    private MaterialPropertyBlock block;

    // private methods  ########################################################
    private void ApplyTint(float strength) {
        block ??= new MaterialPropertyBlock();
        foreach (TintedPart part in parts) {
            if (part.Renderer == null) {
                continue;
            }
            block.Clear();
            block.SetColor(
                part.ColorId,
                Color.Lerp(part.BaseColor, frozenColor, strength)
            );
            part.Renderer.SetPropertyBlock(block);
        }
    }
}
