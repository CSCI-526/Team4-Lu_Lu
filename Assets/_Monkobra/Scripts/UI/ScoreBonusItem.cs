using System;
using System.Globalization;
using TMPro;
using UnityEngine;

// one score bonus popup: a "+" text and a number text side by side
// both texts take the colour of the reward kind; fonts come from the prefab
// the widget owning the pool calls Begin, then Tick every frame until
// IsFinished, then takes the item back
[RequireComponent(typeof(CanvasGroup))]
public class ScoreBonusItem: MonoBehaviour {
    // Public Members  #########################################################
    // if the popup has played all of its fade and rise
    public bool IsFinished => age >= TotalSeconds;

    // how far the popup has risen from its start, pushes included; UI units
    public float Rise => RiseAt();

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

    [SerializeField]
    [Tooltip("seconds to fade in")]
    private float fadeInSeconds = 0.12f;

    [SerializeField]
    [Tooltip("seconds fully visible")]
    private float holdSeconds = 0.5f;

    [SerializeField]
    [Tooltip("seconds to fade out")]
    private float fadeOutSeconds = 0.35f;

    [SerializeField]
    [Tooltip("UI units the popup rises over its whole life")]
    private float riseDistance = 60f;

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

    private float TotalSeconds =>
        fadeInSeconds + holdSeconds + fadeOutSeconds;

    // private methods  ########################################################
    private void EnsureRefs() {
        if (rect == null) {
            rect = (RectTransform)transform;
        }
        if (group == null) {
            group = GetComponent<CanvasGroup>();
        }
    }

    private float RiseAt() {
        float u = Mathf.Clamp01(age / Mathf.Max(TotalSeconds, 0.0001f));
        // ease out: fast start, settles near the top
        float eased = 1f - (1f - u) * (1f - u);
        return eased * riseDistance + push;
    }

    private void Apply() {
        rect.anchoredPosition = startPosition + Vector2.up * RiseAt();

        float alpha;
        if (age < fadeInSeconds) {
            alpha = age / Mathf.Max(fadeInSeconds, 0.0001f);
        } else if (age < fadeInSeconds + holdSeconds) {
            alpha = 1f;
        } else {
            float fadeOutAge = age - fadeInSeconds - holdSeconds;
            alpha = 1f - fadeOutAge / Mathf.Max(fadeOutSeconds, 0.0001f);
        }
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
