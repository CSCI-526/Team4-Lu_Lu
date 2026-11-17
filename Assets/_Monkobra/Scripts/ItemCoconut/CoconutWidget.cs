using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD of the coconut item on the top-right of the in-game HUD, where a
/// grabbed coconut's sprite flies to (<see cref="FruitFlyTarget"/>): a coconut
/// icon w/ the count (ie x2), kept and dimmed at x0. Until the first coconut
/// use of the session, a short how-to sits under it while coconuts are in
/// hand, and flashes whenever the cobra surges.
/// <para>
/// A passive view. Its object starts inactive in the prefab and
/// <see cref="CoconutManager"/> turns it on at the first coconut of the
/// scene, so nothing shows b4 that; the freeze timer and the center prompts
/// are <see cref="FreezeTimerWidget"/> and <see cref="CoconutMessageWidget"/>.
/// The icon pops when a coconut's flight lands, or at once w/o the effect,
/// and on every use.
/// </para>
/// </summary>
public class CoconutWidget: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("coconut icon; dimmed at x0, pops on a landing or a use")]
    private Graphic icon;

    [SerializeField]
    [Tooltip("count label, ie x2")]
    private TMP_Text countLabel;

    [SerializeField]
    [Tooltip(
        "how-to under the count, shown until the first coconut use of the "
            + "session; starts inactive in the prefab, this widget switches it"
    )]
    private TMP_Text useHintLabel;

    [Header("Count")]
    [SerializeField]
    [Tooltip("count text, {0} is the count")]
    private string countFormat = "x{0}";

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("icon & count opacity at x0")]
    private float emptyAlpha = 0.35f;

    [SerializeField]
    [Min(1f)]
    [Tooltip("icon scale at the start of a pop")]
    private float popScale = 1.3f;

    [SerializeField]
    [Min(0.01f)]
    [Tooltip("how long a pop shrinks back; s")]
    private float popS = 0.25f;

    [Header("Use Hint")]
    [SerializeField]
    [Tooltip("how-to text under the count while a keyboard is in use")]
    private string useHintText = "Double-tap Space";

    [SerializeField]
    [Tooltip("how-to text under the count while touch is in use")]
    private string useHintTouchText = "Double-tap anywhere";

    [SerializeField]
    [Tooltip("how-to flash color while the cobra surges")]
    private Color useHintFlashColor = new Color(1f, 0.6f, 0.15f, 1f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("how-to flashes this long on each cobra surge; s")]
    private float useHintFlashS = 2f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("flashes per s")]
    private float useHintFlashHz = 4f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (icon == null) {
            Debug.LogWarning(
                "CoconutWidget:\tmust assign Inspector Field: icon",
                this
            );
        }
        if (countLabel == null) {
            Debug.LogWarning(
                "CoconutWidget:\tmust assign Inspector Field: countLabel",
                this
            );
        }
        if (useHintLabel == null) {
            Debug.LogWarning(
                "CoconutWidget:\tmust assign Inspector Field: useHintLabel",
                this
            );
        }

        if (icon != null) {
            iconBaseColor = icon.color;
        }
        if (countLabel != null) {
            countBaseColor = countLabel.color;
        }
        if (useHintLabel != null) {
            useHintBaseColor = useHintLabel.color;
        }
        ApplyUseHintText(ControlStyleManager.Current);
    }

    private void OnEnable() {
        TryBind();
        ControlStyleManager.StyleChanged += ApplyUseHintText;
        ApplyUseHintText(ControlStyleManager.Current);
    }

    private void OnDisable() {
        Unbind();
        ControlStyleManager.StyleChanged -= ApplyUseHintText;
    }

    // the how-to names the keys of the control style in use
    private void ApplyUseHintText(ControlStyle style) {
        if (useHintLabel != null) {
            useHintLabel.text = style == ControlStyle.Touch
                ? useHintTouchText
                : useHintText;
        }
    }

    private void Update() {
        // the manager may wake after this widget when the prefab ships active
        TryBind();

        UpdatePop();
        UpdateUseHint(CoconutManager.IsRunLive);
    }

    // Event Handlers  #########################################################
    private void OnCountChanged(int count) {
        ShowCount(count);

        // a pickup pops when its flown sprite lands, see OnFlightArrived
        bool isPickup = count > shownCount;
        shownCount = count;
        if (!isPickup || flyEffect == null) {
            Pop();
        }
    }

    private void OnFlightArrived(FruitFlyTarget target) {
        if (target == FruitFlyTarget.TopRightCounter) {
            Pop();
        }
    }

    // the moment a coconut helps most, so the how-to calls for attention
    private void OnCatchUpStarted(int stacks) {
        useHintFlashEndTime = Time.time + useHintFlashS;
    }

    // private members  ########################################################
    private CoconutManager manager;
    private FruitFlyEffect flyEffect;
    private int shownCount; // count on the label
    private Color iconBaseColor = Color.white;
    private Color countBaseColor = Color.white;
    private Color useHintBaseColor = Color.white;
    private float useHintFlashEndTime; // Time.time the how-to stops flashing
    private float popEndTime; // Time.time the icon pop is over

    // private methods  ########################################################
    private static Color WithAlpha(Color color, float alpha) {
        color.a = alpha;
        return color;
    }

    // subscribes once the manager exists; the HUD turns on from it, so this
    // is normally done in OnEnable
    private void TryBind() {
        if (manager != null || CoconutManager.I == null) {
            return;
        }

        manager = CoconutManager.I;
        manager.CountChanged += OnCountChanged;
        manager.CatchUpStarted += OnCatchUpStarted;

        flyEffect = FruitFlyEffect.I;
        if (flyEffect != null) {
            flyEffect.Arrived += OnFlightArrived;
        }

        // the count as it stands, w/o a pop
        shownCount = manager.Count;
        ShowCount(shownCount);
    }

    private void Unbind() {
        if (manager != null) {
            manager.CountChanged -= OnCountChanged;
            manager.CatchUpStarted -= OnCatchUpStarted;
            manager = null;
        }
        if (flyEffect != null) {
            flyEffect.Arrived -= OnFlightArrived;
            flyEffect = null;
        }
    }

    private void ShowCount(int count) {
        if (countLabel != null) {
            countLabel.text = string.Format(countFormat, count);
        }
        float alpha = count > 0 ? 1f : emptyAlpha;
        if (icon != null) {
            icon.color = WithAlpha(iconBaseColor, iconBaseColor.a * alpha);
        }
        if (countLabel != null) {
            countLabel.color =
                WithAlpha(countBaseColor, countBaseColor.a * alpha);
        }
    }

    private void Pop() {
        popEndTime = Time.time + popS;
    }

    // icon shrinks back from the pop scale after a landing or a use
    private void UpdatePop() {
        if (icon == null) {
            return;
        }
        float remaining = Mathf.Clamp01((popEndTime - Time.time) / popS);
        icon.rectTransform.localScale =
            Vector3.one * Mathf.Lerp(1f, popScale, remaining);
    }

    // how-to while coconuts are in hand and none was used yet this session,
    // flashing after a cobra surge
    private void UpdateUseHint(bool isRunLive) {
        if (useHintLabel == null) {
            return;
        }

        bool show = isRunLive
            && !CoconutManager.HasUsedThisSession
            && manager != null
            && manager.Count > 0
            && (manager.Cobra == null || !manager.Cobra.IsFrozen);
        if (useHintLabel.gameObject.activeSelf != show) {
            useHintLabel.gameObject.SetActive(show);
        }
        if (!show) {
            return;
        }

        Color color = useHintBaseColor;
        if (Time.time < useHintFlashEndTime) {
            // 0~1 pulse, frozen with the game clock
            float pulse = 0.5f + 0.5f
                * Mathf.Sin(Time.time * useHintFlashHz * 2f * Mathf.PI);
            color = Color.Lerp(useHintBaseColor, useHintFlashColor, pulse);
        }
        useHintLabel.color = color;
    }
}
