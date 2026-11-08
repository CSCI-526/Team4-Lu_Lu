using System;
using TMPro;
using UnityEngine;

/// <summary>
/// One popup variant: a text on a plate, shown over the screen, then
/// dismissed. It holds its own text, look, timing, and flags, all edited in
/// the Inspector of its prefab; <see cref="PopUpManager"/> is its only owner
/// and calls <see cref="Show"/> and <see cref="Hide"/>.
/// <para>
/// Save the object inactive: <see cref="Show"/> activates it, and it
/// deactivates itself once the fade-out ended.
/// </para>
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MessagePopUp: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>key callers use to show this popup</returns>
    public PopUpId Id => id;

    /// <returns>higher interrupts a showing popup, equal or lower waits
    /// </returns>
    public int Priority => priority;

    /// <returns>if it shows only while the run is live</returns>
    public bool IsRunOnly => isRunOnly;

    /// <returns>if it shows at most once per scene load</returns>
    public bool IsOncePerScene => isOncePerScene;

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
    /// Shows the popup: formats the text with the args, then fades in.
    /// </summary>
    /// <param name="args">values for the <c>{0}</c> slots of the text</param>
    /// <param name="resumeS">show time left of an interrupted popup, negative
    /// for a fresh show</param>
    public void Show(object[] args, float resumeS = -1f) {
        gameObject.SetActive(true);

        label.text = FormatText(args);
        remainingS = resumeS >= 0f
            ? resumeS
            : (showS > 0f ? showS : float.PositiveInfinity);
        elapsedS = 0f;
        isHiding = false;
        isShowing = true;
        ApplyAlpha();

        if (Debug.isDebugBuild) {
            Debug.Log("PopUp:\tshow " + id, this);
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

        if (Debug.isDebugBuild) {
            Debug.Log("PopUp:\tstop " + id, this);
        }
    }

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("key callers use to show this popup")]
    private PopUpId id;

    [SerializeField]
    [Tooltip("message text; edit its color & font on the object")]
    private TMP_Text label;

    [Header("Text")]
    [SerializeField]
    [TextArea]
    [Tooltip("shown text, {0} is the value the caller passes")]
    private string message = "";

    [Header("Timing")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("stays this long, fade-out included; 0 stays until hidden; s")]
    private float showS = 2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("fade-in time; s")]
    private float fadeInS = 0.15f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("fade-out time; s")]
    private float fadeOutS = 0.5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("alpha pulses per s while shown, 0 for none")]
    private float pulseHz;

    [Header("Rules")]
    [SerializeField]
    [Tooltip("higher interrupts a showing popup, equal or lower waits")]
    private int priority;

    [SerializeField]
    [Tooltip("shows only while the run is live, drops on a loss")]
    private bool isRunOnly = true;

    [SerializeField]
    [Tooltip("shows at most once per scene load")]
    private bool isOncePerScene;

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
    }

    private void OnEnable() {
        // a fresh start, Show sets the real values right after
        isShowing = false;
        isHiding = false;
        elapsedS = 0f;
        if (canvasGroup != null) {
            canvasGroup.alpha = 0f;
        }
    }

    private void Update() {
        if (!isShowing) {
            return;
        }

        float dt = Time.deltaTime;
        if (isHiding) {
            hideElapsedS += dt;
            if (fadeOutS <= 0f || hideElapsedS >= fadeOutS) {
                Finish();
                return;
            }
            canvasGroup.alpha =
                fadeStartAlpha * (1f - hideElapsedS / fadeOutS);
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
    private float remainingS;
    private float elapsedS;
    private float hideElapsedS;
    private float fadeStartAlpha;
    private bool isShowing;
    private bool isHiding;

    // cached references  ------------------------------------------------------
    private CanvasGroup canvasGroup;

    // private methods  ########################################################
    private string FormatText(object[] args) {
        if (args == null || args.Length == 0) {
            return message;
        }

        try {
            return string.Format(message, args);
        } catch (FormatException) {
            Debug.LogWarning(
                "PopUp:\tbad format in message of " + id + ": " + message,
                this);
            return message;
        }
    }

    // alpha = fade-in x fade-out of a timed show x pulse
    private void ApplyAlpha() {
        float alpha = fadeInS > 0f ? Mathf.Clamp01(elapsedS / fadeInS) : 1f;
        if (!float.IsInfinity(remainingS) && fadeOutS > 0f) {
            alpha = Mathf.Min(alpha, Mathf.Clamp01(remainingS / fadeOutS));
        }
        if (pulseHz > 0f) {
            float wave = 0.5f + 0.5f * Mathf.Sin(
                elapsedS * pulseHz * 2f * Mathf.PI);
            alpha *= 1f - PULSE_DEPTH * wave;
        }
        canvasGroup.alpha = alpha;
    }

    private void Finish() {
        isShowing = false;
        isHiding = false;
        gameObject.SetActive(false);

        if (Debug.isDebugBuild) {
            Debug.Log("PopUp:\thidden " + id, this);
        }
        Hidden?.Invoke(this);
    }
}
