using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime keycap and instruction shared by the fruit tutorial and boss HUD.
/// Follows a world point on a screen-space Canvas, without a prefab or sprite.
/// </summary>
// Follow this frame's camera pose after CinemachineBrain (order 100).
[DefaultExecutionOrder(200)]
public class KeyPromptUI: MonoBehaviour {
    // Public Members  #########################################################
    public static Vector2 DefaultOffset => new Vector2(0f, 80f);

    // Public Methods  #########################################################
    /// <summary>Builds the same keycap used by the boss, initially hidden.
    /// </summary>
    public static KeyPromptUI Create(
        RectTransform parent,
        string objectName = "Prompt"
    ) {
        if (parent == null) {
            return null;
        }

        RectTransform rect = CreateRect(objectName, parent);
        KeyPromptUI prompt = rect.gameObject.AddComponent<KeyPromptUI>();
        prompt.Build(rect, parent);
        rect.gameObject.SetActive(false);
        return prompt;
    }

    /// <summary>Changes the key label and instruction without rebuilding.
    /// </summary>
    public void SetText(string key, string instruction) {
        keyLabel.text = key;
        howLabel.text = instruction;
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
    private void LateUpdate() {
        Place();
    }

    // cached references  ------------------------------------------------------
    private RectTransform promptRect;
    private RectTransform parentRect;
    private Canvas canvas;
    private CanvasGroup visibility;
    private TextMeshProUGUI keyLabel;
    private TextMeshProUGUI howLabel;
    private Vector3 promptWorldPosition;
    private Vector2 promptOffset;

    // private methods  ########################################################
    private void Build(RectTransform rect, RectTransform parent) {
        promptRect = rect;
        parentRect = parent;
        canvas = parent.GetComponentInParent<Canvas>();
        if (canvas != null) {
            canvas = canvas.rootCanvas;
        }
        visibility = gameObject.AddComponent<CanvasGroup>();
        visibility.blocksRaycasts = false;
        visibility.interactable = false;
        promptRect.sizeDelta = new Vector2(220f, 90f);

        RectTransform keycap = CreateRect("Keycap", promptRect);
        keycap.anchorMin = keycap.anchorMax = new Vector2(0.5f, 1f);
        keycap.pivot = new Vector2(0.5f, 1f);
        keycap.sizeDelta = new Vector2(150f, 44f);
        AddImage(keycap, Color.white);

        RectTransform keyFace = CreateRect("Face", keycap);
        Stretch(keyFace);
        keyFace.offsetMin = new Vector2(3f, 3f);
        keyFace.offsetMax = new Vector2(-3f, -3f);
        AddImage(keyFace, new Color(0.15f, 0.15f, 0.15f, 0.9f));

        keyLabel = CreateLabel("Key", keyFace, "space", 24f);
        Stretch(keyLabel.rectTransform);
        howLabel = CreateLabel(
            "How", promptRect, "hold, then release", 22f
        );
        RectTransform howRect = howLabel.rectTransform;
        howRect.anchorMin = howRect.anchorMax = new Vector2(0.5f, 0f);
        howRect.pivot = new Vector2(0.5f, 0f);
        howRect.sizeDelta = new Vector2(260f, 36f);
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

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddImage(RectTransform rect, Color color) {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
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
