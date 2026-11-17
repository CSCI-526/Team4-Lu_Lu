using System;
using System.Globalization;
using TMPro;
using UnityEngine;

// one score bonus popup: a "+" text and a number text side by side
// both texts take the colour of the reward kind; fonts come from the prefab
// the widget owning the pool calls Begin, then Tick every frame until
// IsFinished, then takes the item back
// life: a bang (pops over-size, settles), a hold, then it floats up and fades
[RequireComponent(typeof(CanvasGroup))]
public class ScoreBonusItem: MonoBehaviour {
    // Public Members  #########################################################
    // if the popup has played all of its fade and rise
    public bool IsFinished => age >= TotalSeconds;

    // how far the popup has risen from its start, pushes included; UI units
    public float Rise => ExitRise() + push;

    // Public Methods  #########################################################
    // starts a popup at start, writes the points after the plus sign and
    // tints both texts by kind
    public void Begin(RewardKind kind, long points, Vector2 start) {
        if (valueText == null || plusText == null) {
            return;
        }
        EnsureRefs();
        valueText.text = points.ToString("N0", CultureInfo.InvariantCulture);
        Color color = ColorOf(kind);
        plusText.color = color;
        valueText.color = color;
        startPosition = start;
        age = 0f;
        push = 0f;
        Apply();
    }

    // an older popup is shoved up to make room for a newer one
    public void Push(float amount) {
        push += amount;
    }

    // scaled time: the widget passes Time.deltaTime, so a pause freezes it
    public void Tick(float deltaTime) {
        age += deltaTime;
        Apply();
    }

    // Inspector Fields  #######################################################
    [Serializable]
    private struct KindColor {
        public RewardKind kind;
        public Color color;
    }

    [SerializeField]
    [Tooltip("the plus sign text")]
    private TMP_Text plusText;

    [SerializeField]
    [Tooltip("the number text")]
    private TMP_Text valueText;

    [Header("Bang")]
    [SerializeField]
    [Min(0.01f)]
    [Tooltip("seconds the popup takes to pop and settle")]
    private float bangSeconds = 0.25f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("scale the popup starts the bang at; 1 = full size")]
    private float bangStartScale = 0.3f;

    [SerializeField]
    [Min(1f)]
    [Tooltip("scale the bang overshoots to before settling at 1")]
    private float bangPeakScale = 1.5f;

    [Header("Hold")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("seconds the popup stays still at full size")]
    private float holdSeconds = 0.45f;

    [Header("Exit")]
    [SerializeField]
    [Min(0.01f)]
    [Tooltip("seconds the popup floats up and fades out")]
    private float exitSeconds = 0.4f;

    [SerializeField]
    [Tooltip("UI units the popup rises while it exits")]
    private float exitRiseDistance = 70f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("scale the popup shrinks to by the end of the exit")]
    private float exitEndScale = 0.85f;

    [SerializeField]
    [Tooltip("colour per reward kind; a kind with no entry shows white")]
    private KindColor[] colors;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (plusText == null) {
            Debug.LogError(
                "ScoreBonusItem:\tmust assign Inspector Field: plusText",
                this);
        }
        if (valueText == null) {
            Debug.LogError(
                "ScoreBonusItem:\tmust assign Inspector Field: valueText",
                this);
        }
        EnsureRefs();
    }

    // private members  ########################################################
    private RectTransform rect;
    private CanvasGroup group;
    private Vector2 startPosition;
    private float age;
    // extra rise from being pushed up by newer popups
    private float push;

    // the bang takes this share of its time to reach the peak
    private const float BANG_PEAK_SHARE = 0.6f;
    // alpha is full this share of the way through the bang
    private const float BANG_ALPHA_SHARE = 0.25f;

    private float TotalSeconds => bangSeconds + holdSeconds + exitSeconds;

    // private methods  ########################################################
    private void EnsureRefs() {
        if (rect == null) {
            rect = (RectTransform)transform;
        }
        if (group == null) {
            group = GetComponent<CanvasGroup>();
        }
    }

    // 0 before the exit, then 0 to 1 over exitSeconds
    private float ExitProgress() {
        float exitAge = age - bangSeconds - holdSeconds;
        return Mathf.Clamp01(exitAge / exitSeconds);
    }

    private float ExitRise() {
        return Mathf.SmoothStep(0f, 1f, ExitProgress()) * exitRiseDistance;
    }

    // pops from the start scale to the peak, then settles at 1
    private float BangScale() {
        float u = Mathf.Clamp01(age / bangSeconds);
        if (u < BANG_PEAK_SHARE) {
            float t = u / BANG_PEAK_SHARE;
            float eased = 1f - (1f - t) * (1f - t);
            return Mathf.Lerp(bangStartScale, bangPeakScale, eased);
        }
        float settle = (u - BANG_PEAK_SHARE) / (1f - BANG_PEAK_SHARE);
        return Mathf.Lerp(
            bangPeakScale, 1f, Mathf.SmoothStep(0f, 1f, settle));
    }

    private void Apply() {
        rect.anchoredPosition = startPosition + Vector2.up * Rise;

        float scale;
        float alpha;
        if (age < bangSeconds) {
            scale = BangScale();
            alpha = Mathf.Clamp01(age / (bangSeconds * BANG_ALPHA_SHARE));
        } else if (age < bangSeconds + holdSeconds) {
            scale = 1f;
            alpha = 1f;
        } else {
            float exit = ExitProgress();
            scale = Mathf.Lerp(1f, exitEndScale, exit);
            alpha = 1f - exit;
        }
        rect.localScale = new Vector3(scale, scale, 1f);
        group.alpha = Mathf.Clamp01(alpha);
    }

    private Color ColorOf(RewardKind kind) {
        if (colors != null) {
            for (int i = 0; i < colors.Length; i++) {
                if (colors[i].kind == kind) {
                    return colors[i].color;
                }
            }
        }
        return Color.white;
    }
}
