using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD of the coconut item on the top-right anchor of the in-game HUD, where
/// a grabbed coconut's sprite flies to (<see cref="FruitFlyTarget"/>), above
/// the progress bar: a coconut icon w/ the count (ie x2), hidden until the
/// first coconut of the scene, then kept and dimmed at x0. While the cobra is
/// frozen, a countdown (ie Frozen 7s) and a shrinking bar sit under it, and
/// blink in the last seconds. Until the first coconut use of the session, a
/// short how-to sits in the same spot instead, and flashes whenever the
/// cobra surges while coconuts are in hand. Short center messages teach the
/// double-tap on the first coconut, confirm a use, refuse one while already
/// frozen, and warn when the cobra moves again.
/// <para>
/// Reads <see cref="CoconutInventory.I"/>, its cobra and
/// <see cref="FruitFlyEffect.I"/> in Start; put it on an always active Canvas
/// child. The icon pops when a coconut's flight lands, or at once w/o the
/// effect, and on every use. The center message label is built at
/// runtime on the root canvas, like the cobra warning of
/// <see cref="ProgressBarWidget"/>, but higher up so the two never overlap.
/// </para>
/// </summary>
public class CoconutWidget: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("icon, count & timer group; hidden until the first coconut")]
    private GameObject panel;

    [SerializeField]
    [Tooltip("coconut icon; dimmed at x0, pops on a landing or a use")]
    private Graphic icon;

    [SerializeField]
    [Tooltip("count label, ie x2")]
    private TMP_Text countLabel;

    [SerializeField]
    [Tooltip("freeze countdown under the icon, shown while frozen")]
    private TMP_Text timerLabel;

    [SerializeField]
    [Tooltip(
        "bar under the countdown, shown while frozen; its x scale shrinks w/ "
            + "the time left, so keep its pivot on its left edge"
    )]
    private Graphic timerBar;

    [SerializeField]
    [Tooltip(
        "how-to under the count, shown until the first coconut use of the "
            + "session; may share the timer's spot, the two never show at once"
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

    [Header("Freeze Timer")]
    [SerializeField]
    [Tooltip("countdown text, {0} is the whole seconds left")]
    private string timerFormat = "Frozen {0}s";

    [SerializeField]
    [Tooltip("countdown, bar & frozen message color")]
    private Color frozenColor = new Color(0.55f, 0.85f, 1f, 1f);

    [SerializeField]
    [Tooltip("blink color near the end, also the thaw message color")]
    private Color endingColor = new Color(1f, 0.6f, 0.15f, 1f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("countdown & bar blink this long b4 the cobra moves again; s")]
    private float endingWarnS = 3f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("blinks per s near the end")]
    private float endingBlinkHz = 4f;

    [Header("Use Hint")]
    [SerializeField]
    [Tooltip("how-to text under the count")]
    private string useHintText = "Double-tap Space";

    [SerializeField]
    [Tooltip("how-to flash color while the cobra surges")]
    private Color useHintFlashColor = new Color(1f, 0.6f, 0.15f, 1f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("how-to flashes this long on each cobra surge; s")]
    private float useHintFlashS = 2f;

    [Header("Center Messages")]
    [SerializeField]
    [Tooltip("shown on the first coconut of the scene")]
    private string firstCoconutText =
        "Coconut! Double-tap Space to freeze the cobra";

    [SerializeField]
    [Tooltip("shown when a coconut freezes the cobra, {0} is the seconds")]
    private string frozenFormat = "Cobra frozen for {0}s!";

    [SerializeField]
    [Tooltip("shown on a double-tap while the cobra is already frozen")]
    private string refusedText = "Cobra is already frozen";

    [SerializeField]
    [Tooltip("shown when the freeze runs out")]
    private string thawedText = "The cobra is moving again!";

    [SerializeField]
    [Min(0.01f)]
    [Tooltip("text height as share of screen height")]
    private float messageFontScreenShare = 0.05f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("a message stays this long, last part fading; s")]
    private float messageShowS = 2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("the first coconut message stays this long; s")]
    private float firstCoconutShowS = 3.5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("fade-out part of the show time; s")]
    private float messageFadeS = 0.5f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "message height on screen, 0 bottom ~ 1 top; above the cobra "
            + "warning at 0.65"
    )]
    private float messageScreenY = 0.76f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (panel == null) {
            Debug.LogWarning(
                "CoconutWidget:\tmust assign Inspector Field: panel",
                this
            );
        }
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
        if (timerLabel == null) {
            Debug.LogWarning(
                "CoconutWidget:\tmust assign Inspector Field: timerLabel",
                this
            );
        }
        if (timerBar == null) {
            Debug.LogWarning(
                "CoconutWidget:\tmust assign Inspector Field: timerBar",
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
            useHintLabel.text = useHintText;
            useHintLabel.gameObject.SetActive(false);
        }
        if (panel != null) {
            panel.SetActive(false);
        }
        SetTimerVisible(false);
        messageLabel = CreateMessageLabel();
    }

    private void Start() {
        inventory = CoconutInventory.I;
        if (inventory == null) {
            Debug.LogWarning(
                "CoconutWidget:\tfail to get singleton: CoconutInventory, "
                    + "HUD stays hidden",
                this
            );
            enabled = false;
            return;
        }
        inventory.CountChanged += OnCountChanged;
        inventory.FreezeRefused += OnFreezeRefused;
        inventory.Used += OnUsed;

        cobra = inventory.Cobra;
        if (cobra != null) {
            cobra.Frozen += OnFrozen;
            cobra.Thawed += OnThawed;
            cobra.CatchUpStarted += OnCatchUpStarted;
        }

        flyEffect = FruitFlyEffect.I;
        if (flyEffect != null) {
            flyEffect.Arrived += OnFlightArrived;
        }

        // a count from b4 this Start, ie a hack key, still shows
        if (inventory.Count > 0) {
            OnCountChanged(inventory.Count);
        }
    }

    private void OnDestroy() {
        if (inventory != null) {
            inventory.CountChanged -= OnCountChanged;
            inventory.FreezeRefused -= OnFreezeRefused;
            inventory.Used -= OnUsed;
        }
        if (cobra != null) {
            cobra.Frozen -= OnFrozen;
            cobra.Thawed -= OnThawed;
            cobra.CatchUpStarted -= OnCatchUpStarted;
        }
        if (flyEffect != null) {
            flyEffect.Arrived -= OnFlightArrived;
        }
    }

    private void Update() {
        bool isPlaying =
            GameController.I == null
            || GameController.I.State == GameState.Playing;

        UpdatePop();
        UpdateTimer(isPlaying);
        UpdateUseHint(isPlaying);
        UpdateMessage(isPlaying);
    }

    // a session is one app run: restarts reload the scene, statics stay;
    // reset on Play too, for an Editor w/o domain reload
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetSession() {
        hasUsedThisSession = false;
    }

    // Event Handlers  #########################################################
    private void OnCountChanged(int count) {
        if (!hasShownPanel && count > 0) {
            hasShownPanel = true;
            if (panel != null) {
                panel.SetActive(true);
            }
            // a player who used one already this session knows how
            if (!hasUsedThisSession) {
                ShowMessage(firstCoconutText, frozenColor, firstCoconutShowS);
            }
        }

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

    private void OnFrozen(float durationS) {
        ShowMessage(
            string.Format(frozenFormat, Mathf.CeilToInt(durationS)),
            frozenColor,
            messageShowS
        );
    }

    private void OnFreezeRefused() {
        ShowMessage(refusedText, frozenColor, messageShowS);
    }

    private void OnThawed() {
        ShowMessage(thawedText, endingColor, messageShowS);
    }

    private void OnUsed() {
        hasUsedThisSession = true;
    }

    // the moment a coconut helps most, so the how-to calls for attention
    private void OnCatchUpStarted(int stacks) {
        useHintFlashEndTime = Time.time + useHintFlashS;
    }

    // private members  ########################################################
    private static bool hasUsedThisSession; // a coconut was used since launch

    private CoconutInventory inventory;
    private CobraMotor cobra;
    private FruitFlyEffect flyEffect;
    private bool hasShownPanel;
    private int shownCount; // count on the label
    private Color iconBaseColor = Color.white;
    private Color countBaseColor = Color.white;
    private Color useHintBaseColor = Color.white;
    private float useHintFlashEndTime; // Time.time the how-to stops flashing
    private float popEndTime; // Time.time the icon pop is over
    private TextMeshProUGUI messageLabel;
    private Color messageColor;
    private float messageDurationS; // show time of the running message; s
    private float messageHideTime; // Time.time the message is gone

    // private methods  ########################################################
    private static Color WithAlpha(Color color, float alpha) {
        color.a = alpha;
        return color;
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

    // countdown & bar while frozen, blinking in the last seconds
    private void UpdateTimer(bool isPlaying) {
        bool isFrozen = isPlaying && cobra != null && cobra.IsFrozen;
        SetTimerVisible(isFrozen);
        if (!isFrozen) {
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

    // how-to while coconuts are in hand and none was used yet this session,
    // flashing after a cobra surge
    private void UpdateUseHint(bool isPlaying) {
        if (useHintLabel == null) {
            return;
        }

        bool show = isPlaying
            && !hasUsedThisSession
            && inventory != null
            && inventory.Count > 0
            && (cobra == null || !cobra.IsFrozen);
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
                * Mathf.Sin(Time.time * endingBlinkHz * 2f * Mathf.PI);
            color = Color.Lerp(useHintBaseColor, useHintFlashColor, pulse);
        }
        useHintLabel.color = color;
    }

    private void SetTimerVisible(bool isVisible) {
        if (timerLabel != null
            && timerLabel.gameObject.activeSelf != isVisible) {
            timerLabel.gameObject.SetActive(isVisible);
        }
        if (timerBar != null
            && timerBar.gameObject.activeSelf != isVisible) {
            timerBar.gameObject.SetActive(isVisible);
        }
    }

    // shows the center message w/ the given text, same look for every one
    private void ShowMessage(string text, Color color, float showS) {
        messageDurationS = showS;
        messageHideTime = Time.time + showS;
        messageColor = color;
        if (messageLabel == null) {
            return;
        }

        messageLabel.text = text;

        // re-sized on every show, in case the window size changed
        Canvas canvas = messageLabel.canvas;
        float scaleFactor = canvas != null && canvas.scaleFactor > 0f
            ? canvas.scaleFactor
            : 1f;
        float fontSize = Screen.height * messageFontScreenShare / scaleFactor;
        messageLabel.fontSize = fontSize;
        messageLabel.rectTransform.sizeDelta =
            new Vector2(0f, fontSize * 1.6f);
    }

    // the message fades out at its end, and shows only during a run
    private void UpdateMessage(bool isPlaying) {
        if (messageLabel == null) {
            return;
        }

        float remainingS = messageHideTime - Time.time;
        bool show = isPlaying && remainingS > 0f;
        if (messageLabel.gameObject.activeSelf != show) {
            messageLabel.gameObject.SetActive(show);
        }
        if (!show) {
            return;
        }

        float fadeS = Mathf.Min(messageFadeS, messageDurationS);
        messageLabel.color = WithAlpha(
            messageColor,
            fadeS > 0f ? Mathf.Clamp01(remainingS / fadeS) : 1f
        );
    }

    // a full-width label built at runtime on the root canvas
    private TextMeshProUGUI CreateMessageLabel() {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) {
            Debug.LogWarning(
                "CoconutWidget:\tno parent Canvas, center messages are off",
                this
            );
            return null;
        }

        GameObject labelObject =
            new GameObject("CoconutMessage", typeof(RectTransform));
        labelObject.transform.SetParent(canvas.rootCanvas.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, messageScreenY);
        labelRect.anchorMax = new Vector2(1f, messageScreenY);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0f, 100f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        labelObject.SetActive(false);
        return label;
    }
}
