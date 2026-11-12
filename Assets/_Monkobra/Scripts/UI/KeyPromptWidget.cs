using TMPro;
using UnityEngine;

/// <summary>
/// Runtime animated input icon and instruction of the boss HUD. Follows a
/// world point on a screen-space Canvas; the icon is a <see cref="HintIconSlot"/>
/// that shows the keyboard or the touch prefab of the style in use.
/// </summary>
// Follow this frame's camera pose after CinemachineBrain (order 100).
[DefaultExecutionOrder(200)]
public class KeyPromptWidget: MonoBehaviour {
    // Public Members  #########################################################
    public static Vector2 DefaultOffset => new Vector2(0f, 80f);

    // Public Methods  #########################################################
    /// <summary>Builds the prompt, initially hidden. The icon prefabs are the
    /// animated hold-and-release icons of each control style.</summary>
    public static KeyPromptWidget Create(
        RectTransform parent,
        GameObject keyboardIcon,
        GameObject touchIcon,
        string objectName = "Prompt"
    ) {
        if (parent == null) {
            return null;
        }

        RectTransform rect = CreateRect(objectName, parent);
        KeyPromptWidget prompt =
            rect.gameObject.AddComponent<KeyPromptWidget>();
        prompt.Build(rect, parent, keyboardIcon, touchIcon);
        rect.gameObject.SetActive(false);
        return prompt;
    }

    /// <summary>Changes the keyboard instruction without rebuilding.
    /// </summary>
    public void SetInstruction(string instruction) {
        howText = instruction;
        ApplyStyleText(ControlStyleManager.Current);
    }

    /// <summary>Shows at a world point, or hides; offset is in UI units.
    /// </summary>
    public void SetPrompt(
        bool isShown,
        Vector3 worldPosition,
        Vector2 offset
    ) {
        promptWorldPosition = worldPosition;
        promptOffset = offset;
        if (gameObject.activeSelf != isShown) {
            gameObject.SetActive(isShown);
        }
        if (isShown) {
            Place();
        }
    }

    public void SetPrompt(bool isShown, Vector3 worldPosition) {
        SetPrompt(isShown, worldPosition, DefaultOffset);
    }

    // MonoBehaviour Lifecycle  ################################################
    private void OnEnable() {
        ControlStyleManager.StyleChanged += ApplyStyleText;
        ApplyStyleText(ControlStyleManager.Current);
    }

    private void OnDisable() {
        ControlStyleManager.StyleChanged -= ApplyStyleText;
    }

    private void LateUpdate() {
        Place();
    }

    // cached references  ------------------------------------------------------
    private RectTransform promptRect;
    private RectTransform parentRect;
    private Canvas canvas;
    private CanvasGroup visibility;
    private TextMeshProUGUI howLabel;
    private Vector3 promptWorldPosition;
    private Vector2 promptOffset;

    // the wording per control style; SetInstruction changes the keyboard one
    private string howText = "hold, then release";
    private const string TOUCH_HOW_TEXT = "touch and hold, then release";

    // the icon is the key, so the text names the gesture only
    private const float ICON_SIZE = 64f;

    // private methods  ########################################################
    private void Build(
        RectTransform rect,
        RectTransform parent,
        GameObject keyboardIcon,
        GameObject touchIcon
    ) {
        promptRect = rect;
        parentRect = parent;
        canvas = parent.GetComponentInParent<Canvas>();
        if (canvas != null) {
            canvas = canvas.rootCanvas;
        }
        visibility = gameObject.AddComponent<CanvasGroup>();
        visibility.blocksRaycasts = false;
        visibility.interactable = false;
        promptRect.sizeDelta = new Vector2(260f, ICON_SIZE + 46f);

        RectTransform icon = HintIconSlot.Create(
            promptRect, keyboardIcon, touchIcon, ICON_SIZE
        );
        icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 1f);
        icon.pivot = new Vector2(0.5f, 1f);
        icon.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);
        // the holders center on the slot, which sits ICON_SIZE/2 below the top
        icon.anchoredPosition = Vector2.zero;
        howLabel = CreateLabel(
            "How", promptRect, howText, 22f
        );
        RectTransform howRect = howLabel.rectTransform;
        howRect.anchorMin = howRect.anchorMax = new Vector2(0.5f, 0f);
        howRect.pivot = new Vector2(0.5f, 0f);
        howRect.sizeDelta = new Vector2(260f, 36f);
        ApplyStyleText(ControlStyleManager.Current);
    }

    // the instruction names the gesture of the control style in use
    private void ApplyStyleText(ControlStyle style) {
        if (howLabel == null) {
            return;
        }
        bool isTouch = style == ControlStyle.Touch;
        howLabel.text = isTouch ? TOUCH_HOW_TEXT : howText;
    }

    private void Place() {
        if (promptRect == null || parentRect == null || visibility == null) {
            return;
        }
        Camera worldCamera = Camera.main;
        if (worldCamera == null || canvas == null) {
            visibility.alpha = 0f;
            return;
        }

        Vector3 screen = worldCamera.WorldToScreenPoint(promptWorldPosition);
        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
        if (screen.z <= 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, screen, canvasCamera, out Vector2 local
        )) {
            visibility.alpha = 0f;
            return;
        }

        visibility.alpha = 1f;
        // ScreenPointToLocalPoint returns coordinates relative to the parent's
        // pivot, so localPosition also works when that pivot is not centered.
        promptRect.localPosition = new Vector3(
            local.x + promptOffset.x, local.y + promptOffset.y, 0f
        );
    }

    private static RectTransform CreateRect(string rectName, Transform parent) {
        var go = new GameObject(rectName, typeof(RectTransform));
        go.layer = 5; // UI
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static TextMeshProUGUI CreateLabel(
        string labelName,
        Transform parent,
        string text,
        float size
    ) {
        RectTransform rect = CreateRect(labelName, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }
}
