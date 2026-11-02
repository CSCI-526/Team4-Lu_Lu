using TMPro;
using UnityEngine;

/// <summary>
/// Short center prompts of the coconut item: the double-tap teaching on the
/// first coconut of the scene (skipped once one was used this session), the
/// confirmation that the cobra froze, the refusal while it is already frozen,
/// and the warning that it moves again. They come from
/// <see cref="CoconutManager"/>'s events. The label is built at runtime on
/// the root canvas, like the cobra warning of <see cref="ProgressBarWidget"/>,
/// but higher up so the two never overlap.
/// <para>
/// Keep this object active from scene load: it must hear the thaw, when the
/// freeze timer HUD is already off.
/// </para>
/// </summary>
public class CoconutMessageWidget: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Texts")]
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

    [Header("Colors")]
    [SerializeField]
    [Tooltip("first coconut, frozen & refused message color")]
    private Color frozenColor = new Color(0.55f, 0.85f, 1f, 1f);

    [SerializeField]
    [Tooltip("thaw message color")]
    private Color endingColor = new Color(1f, 0.6f, 0.15f, 1f);

    [Header("Look")]
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
        messageLabel = CreateMessageLabel();
    }

    private void Start() {
        manager = CoconutManager.I;
        if (manager == null) {
            Debug.LogWarning(
                "CoconutMessageWidget:\tfail to get singleton: "
                    + "CoconutManager, no coconut prompts",
                this
            );
            enabled = false;
            return;
        }
        manager.Collected += OnCollected;
        manager.Frozen += OnFrozen;
        manager.FreezeRefused += OnFreezeRefused;
        manager.Thawed += OnThawed;
    }

    private void OnDestroy() {
        if (manager != null) {
            manager.Collected -= OnCollected;
            manager.Frozen -= OnFrozen;
            manager.FreezeRefused -= OnFreezeRefused;
            manager.Thawed -= OnThawed;
        }
        // the label sits on the root canvas, not under this object
        if (messageLabel != null) {
            Destroy(messageLabel.gameObject);
        }
    }

    private void Update() {
        UpdateMessage(CoconutManager.IsRunLive);
    }

    // Event Handlers  #########################################################
    private void OnCollected() {
        if (hasTaughtThisScene) {
            return;
        }
        hasTaughtThisScene = true;

        // a player who used one already this session knows how
        if (!CoconutManager.HasUsedThisSession) {
            ShowMessage(firstCoconutText, frozenColor, firstCoconutShowS);
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

    // private members  ########################################################
    private CoconutManager manager;
    private bool hasTaughtThisScene; // the first coconut was announced
    private TextMeshProUGUI messageLabel;
    private Color messageColor;
    private float messageDurationS; // show time of the running message; s
    private float messageHideTime; // Time.time the message is gone

    // private methods  ########################################################
    private static Color WithAlpha(Color color, float alpha) {
        color.a = alpha;
        return color;
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
    private void UpdateMessage(bool isRunLive) {
        if (messageLabel == null) {
            return;
        }

        float remainingS = messageHideTime - Time.time;
        bool show = isRunLive && remainingS > 0f;
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
                "CoconutMessageWidget:\tno parent Canvas, center messages "
                    + "are off",
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
