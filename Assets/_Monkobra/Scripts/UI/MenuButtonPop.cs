using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// the feel of a logo-style button, next to a Button whose Sprite Swap uses the
/// MenuButton or IconButton sprites: the selected button grows, a pressed one
/// sinks its content onto the shadow (as the Pressed sprite moves its face),
/// and a locked one shows its lock beside a dimmed label. Unscaled time, so it
/// works on a paused screen; see docs/ui-style-doc.md
/// </summary>
[RequireComponent(typeof(Selectable))]
public class MenuButtonPop: MonoBehaviour, ISelectHandler, IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("what sinks while pressed: the label, or the glyph of an icon "
        + "button")]
    private RectTransform content;

    [SerializeField]
    [Tooltip("the label, recolored when locked and kept beside the lock; "
        + "empty on an icon button")]
    private TMP_Text label;

    [SerializeField]
    [Tooltip("shown only while the button is not interactable, left of the "
        + "label")]
    private RectTransform lockIcon;

    [SerializeField]
    [Min(1f)]
    [Tooltip("scale of the selected button")]
    private float selectedScale = 1.08f;

    [SerializeField]
    [Min(0.01f)]
    [Tooltip("how fast the scale follows the selection, per unscaled second")]
    private float scaleSpeed = 18f;

    [SerializeField]
    [Tooltip("how far the content sinks while pressed, the sprite's shadow "
        + "offset")]
    private Vector2 pressedOffset = new Vector2(5f, -6f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("gap between the lock and the label")]
    private float lockGap = 10f;

    [SerializeField]
    [Tooltip("label color while interactable")]
    private Color labelColor = new Color32(0x35, 0x20, 0x16, 0xFF);

    [SerializeField]
    [Tooltip("label color while locked")]
    private Color lockedLabelColor = new Color32(0x4A, 0x3A, 0x2C, 0xFF);

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        selectable = GetComponent<Selectable>();
        if (content != null) {
            contentHome = content.anchoredPosition;
        }
        if (label != null) {
            labelHome = label.rectTransform.anchoredPosition;
        }
    }

    private void OnDisable() {
        isSelected = false;
        isPressed = false;
        transform.localScale = Vector3.one;
        Place(false);
    }

    private void Update() {
        bool isInteractable = selectable.IsInteractable();
        float target = isInteractable && isSelected ? selectedScale : 1f;
        float k = 1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime);
        float scale = Mathf.Lerp(transform.localScale.x, target, k);
        transform.localScale = new Vector3(scale, scale, 1f);

        bool isLocked = !isInteractable;
        bool isSunk = isInteractable && isPressed;
        if (isLocked != wasLocked || isSunk != wasSunk || !isPlaced) {
            wasLocked = isLocked;
            wasSunk = isSunk;
            isPlaced = true;
            Place(isSunk);
        }
    }

    // Event Handlers  #########################################################
    public void OnSelect(BaseEventData eventData) {
        isSelected = true;
    }

    public void OnDeselect(BaseEventData eventData) {
        isSelected = false;
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) {
            isPressed = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData) {
        isPressed = false;
    }

    // private methods  ########################################################
    // the content sinks when pressed; when locked the lock shows and the pair
    // (lock + label) stays centered on the face
    private void Place(bool isSunk) {
        Vector2 sink = isSunk ? pressedOffset : Vector2.zero;
        if (content != null && content != (label != null
            ? label.rectTransform : null)) {
            content.anchoredPosition = contentHome + sink;
        }
        if (label == null) {
            if (lockIcon != null) {
                lockIcon.gameObject.SetActive(wasLocked);
            }
            return;
        }

        label.color = wasLocked ? lockedLabelColor : labelColor;
        Vector2 shift = Vector2.zero;
        if (lockIcon != null) {
            lockIcon.gameObject.SetActive(wasLocked);
            if (wasLocked) {
                float iconWidth = lockIcon.rect.width;
                float textWidth = label.GetPreferredValues(label.text).x;
                shift.x = (iconWidth + lockGap) / 2f;
                lockIcon.anchoredPosition = new Vector2(
                    labelHome.x + shift.x - textWidth / 2f - lockGap
                        - iconWidth / 2f,
                    lockIcon.anchoredPosition.y
                );
            }
        }
        label.rectTransform.anchoredPosition = labelHome + shift + sink;
    }

    // Private Members  ########################################################
    private Selectable selectable;
    private Vector2 contentHome;
    private Vector2 labelHome;
    private bool isSelected;
    private bool isPressed;
    private bool wasLocked;
    private bool wasSunk;
    private bool isPlaced;
}
