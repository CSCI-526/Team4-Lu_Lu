using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Countdown (ie Frozen 7s) and a shrinking bar for the great cobra's coconut
/// freeze, blinking in the last seconds. A passive view: its object starts
/// inactive in the prefab and <see cref="CoconutManager"/> turns it on only
/// while the cobra is frozen in a run, so it needs no show or hide logic of
/// its own. Put it on a prefab nested straight under the HUD canvas.
/// </summary>
public class FreezeTimerWidget: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("countdown text")]
    private TMP_Text timerLabel;

    [SerializeField]
    [Tooltip(
        "bar under the countdown; its x scale shrinks w/ the time left, so "
            + "keep its pivot on its left edge"
    )]
    private Graphic timerBar;

    [Header("Look")]
    [SerializeField]
    [Tooltip("countdown text, {0} is the whole seconds left")]
    private string timerFormat = "Frozen {0}s";

    [SerializeField]
    [Tooltip("countdown & bar color")]
    private Color frozenColor = new Color(0.55f, 0.85f, 1f, 1f);

    [SerializeField]
    [Tooltip("blink color near the end")]
    private Color endingColor = new Color(1f, 0.6f, 0.15f, 1f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("countdown & bar blink this long b4 the cobra moves again; s")]
    private float endingWarnS = 3f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("blinks per s near the end")]
    private float endingBlinkHz = 4f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (timerLabel == null) {
            Debug.LogWarning(
                "FreezeTimerWidget:\tmust assign Inspector Field: timerLabel",
                this
            );
        }
        if (timerBar == null) {
            Debug.LogWarning(
                "FreezeTimerWidget:\tmust assign Inspector Field: timerBar",
                this
            );
        }
    }

    private void Update() {
        CoconutManager manager = CoconutManager.I;
        CobraMotor cobra = manager != null ? manager.Cobra : null;
        if (cobra == null || !cobra.IsFrozen) {
            return;
        }

        float remainingS = cobra.FreezeRemainingS;
        float fraction = cobra.FreezeDurationS > 0f
            ? Mathf.Clamp01(remainingS / cobra.FreezeDurationS)
            : 0f;

        Color color = frozenColor;
        if (remainingS <= endingWarnS) {
            // 0~1 pulse, frozen with the game clock
            float pulse = 0.5f + 0.5f
                * Mathf.Sin(Time.time * endingBlinkHz * 2f * Mathf.PI);
            color = Color.Lerp(frozenColor, endingColor, pulse);
        }

        if (timerLabel != null) {
            timerLabel.text =
                string.Format(timerFormat, Mathf.CeilToInt(remainingS));
            timerLabel.color = color;
        }
        if (timerBar != null) {
            timerBar.rectTransform.localScale = new Vector3(fraction, 1f, 1f);
            timerBar.color = color;
        }
    }
}
