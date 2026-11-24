using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The popup entity: a text on a plate, shown over the screen, then
/// dismissed. It owns no text or timing of its own: <see cref="Show"/> takes
/// a <see cref="PopUpDefinition"/> and dresses itself with it, so one entity
/// serves every popup of its <see cref="PopUpKind"/>. <see
/// cref="PopUpManager"/>
/// is its only owner and calls <see cref="Show"/>, <see cref="Hide"/> and
/// <see cref="Stop"/>.
/// <para>
/// Save the object inactive: <see cref="Show"/> activates it, and it
/// deactivates itself once the fade-out ended.
/// </para>
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MessagePopUp: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>the popup it shows now or showed last; null before the first
    /// show</returns>
    public PopUpDefinition Definition => definition;

    /// <returns>if it is on screen now, fading included</returns>
    public bool IsShowing => isShowing;

    /// <returns>show time left; s, infinity when it stays until hidden
    /// </returns>
    public float RemainingS => remainingS;

    /// <summary>raised once the fade-out ended and the object went inactive
    /// </summary>
    public event Action<MessagePopUp> Hidden;

    // Public Methods  #########################################################
    /// <summary>
    /// Shows a popup: dresses the entity with its definition, formats the
    /// text with the args, then fades in.
    /// </summary>
    /// <param name="shown">the popup to show</param>
    /// <param name="args">values for the <c>{0}</c> slots of the text</param>
    /// <param name="resumeS">show time left of an interrupted popup, negative
    /// for a fresh show</param>
    public void Show(
        PopUpDefinition shown, object[] args, float resumeS = -1f
    ) {
        gameObject.SetActive(true);

        definition = shown;
        Dress(shown);
        shownArgs = args;
        label.text = FormatText(args);
        remainingS = resumeS >= 0f
            ? resumeS
            : (shown.ShowS > 0f ? shown.ShowS : float.PositiveInfinity);
        elapsedS = 0f;
        isHiding = false;
        isShowing = true;
        ApplyAlpha();

        if (Debug.isDebugBuild) {
            Debug.Log("PopUp:\tshow " + shown.Id, this);
        }
    }

    /// <summary>Fades out, then deactivates and raises
    /// <see cref="Hidden"/>.</summary>
    public void Hide() {
        if (!isShowing || isHiding) {
            return;
        }

        isHiding = true;
        hideElapsedS = 0f;
        fadeStartAlpha = canvasGroup.alpha;
    }

    /// <summary>Drops the popup at once, silent: no fade, no
    /// <see cref="Hidden"/>. For an interrupt or a loss.</summary>
    public void Stop() {
        isShowing = false;
        isHiding = false;
        gameObject.SetActive(false);

        if (Debug.isDebugBuild && definition != null) {
            Debug.Log("PopUp:\tstop " + definition.Id, this);
        }
    }

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("message text; its font stays on the object")]
    private TMP_Text label;

    [SerializeField]
    [Tooltip("fruit picture of a Fruit popup; none on a plain one")]
    private Image iconImage;

    [SerializeField]
    [Tooltip("animated hint icon; none when the popup has no hint")]
    private HintIconWidget hintSlot;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (label == null) {
            Debug.LogWarning(
                "PopUp:\tmust assign Inspector Field: label", this);
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) {
            Debug.LogError("PopUp:\tfail to get Component: CanvasGroup", this);
        }
        rect = (RectTransform)transform;
    }

    private void OnEnable() {
        ControlStyleManager.StyleChanged += OnStyleChanged;

        // a fresh start, Show sets the real values right after
        isShowing = false;
        isHiding = false;
        elapsedS = 0f;
        if (canvasGroup != null) {
            canvasGroup.alpha = 0f;
        }
    }

    private void OnDisable() {
        ControlStyleManager.StyleChanged -= OnStyleChanged;
    }

    private void Update() {
        if (!isShowing) {
            return;
        }

        float dt = Time.deltaTime;
        if (isHiding) {
            hideElapsedS += dt;
            if (definition.FadeOutS <= 0f
                || hideElapsedS >= definition.FadeOutS) {
                Finish();
                return;
            }
            canvasGroup.alpha = fadeStartAlpha
                * (1f - hideElapsedS / definition.FadeOutS);
            return;
        }

        elapsedS += dt;
        remainingS -= dt;
        if (remainingS <= 0f) {
            Finish();
            return;
        }
        ApplyAlpha();
    }

    // constants  ##############################################################
    private const float PULSE_DEPTH = 0.35f;

    // private members  ########################################################
    private PopUpDefinition definition;
    private float remainingS;
    private float elapsedS;
    private float hideElapsedS;
    private float fadeStartAlpha;
    private bool isShowing;
    private bool isHiding;
    private object[] shownArgs; // args of the current show, for a re-format

    // cached references  ------------------------------------------------------
    private CanvasGroup canvasGroup;
    private RectTransform rect;

    // private methods  ########################################################
    // the entity takes the look of the popup about to show
    private void Dress(PopUpDefinition shown) {
        label.fontSize = shown.FontSize;
        label.color = shown.TextColor;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, shown.AnchorY);
        rect.anchoredPosition = new Vector2(0f, shown.OffsetY);
        if (iconImage != null) {
            iconImage.sprite = shown.Icon;
        }
        if (hintSlot != null) {
            hintSlot.SetIcons(
                shown.KeyboardHintController, shown.TouchHintController
            );
        }
    }

    // a held popup (a fruit prompt) re-words itself when the style flips
    private void OnStyleChanged(ControlStyle _) {
        if (isShowing && label != null) {
            label.text = FormatText(shownArgs);
        }
    }

    // the touch wording when touch is in use and the popup has one
    private string FormatText(object[] args) {
        string text = ControlStyleManager.IsTouch
            && !string.IsNullOrEmpty(definition.TouchMessage)
            ? definition.TouchMessage
            : definition.Message;
        if (args == null || args.Length == 0) {
            return text;
        }

        try {
            return string.Format(text, args);
        } catch (FormatException) {
            Debug.LogWarning(
                "PopUp:\tbad format in message of " + definition.Id + ": "
                    + text,
                this);
            return text;
        }
    }

    // alpha = fade-in x fade-out of a timed show x pulse
    private void ApplyAlpha() {
        float alpha = definition.FadeInS > 0f
            ? Mathf.Clamp01(elapsedS / definition.FadeInS)
            : 1f;
        if (!float.IsInfinity(remainingS) && definition.FadeOutS > 0f) {
            alpha = Mathf.Min(
                alpha, Mathf.Clamp01(remainingS / definition.FadeOutS));
        }
        if (definition.PulseHz > 0f) {
            float wave = 0.5f + 0.5f * Mathf.Sin(
                elapsedS * definition.PulseHz * 2f * Mathf.PI);
            alpha *= 1f - PULSE_DEPTH * wave;
        }
        canvasGroup.alpha = alpha;
    }

    private void Finish() {
        isShowing = false;
        isHiding = false;
        gameObject.SetActive(false);

        if (Debug.isDebugBuild) {
            Debug.Log("PopUp:\thidden " + definition.Id, this);
        }
        Hidden?.Invoke(this);
    }
}
