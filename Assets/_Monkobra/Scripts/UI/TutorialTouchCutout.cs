using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

/// <summary>
/// The touch tutorial's blue action area: 4 blue strips cover the whole
/// screen except a hole over the real <see cref="OnScreenStick"/>, so the live
/// joystick shows through and the blue area reads as the part of the screen
/// that triggers actions. While shown it also hides the tutorial's white
/// overlay, which would wash the joystick out.
/// <para>
/// Sits on a full-stretch object that <see cref="ControlStyleSwitch"/> shows
/// in Touch style only. The stick lives in another prefab, so it is found at
/// runtime, never linked in the Inspector, and the hole is laid out every
/// frame so a resize or a moved stick stays right.
/// </para>
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TutorialTouchCutout: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("tutorial's white overlay; hidden while the blue area shows")]
    private Image overlay;

    [SerializeField]
    [Tooltip("strip above the hole; stretches the full width")]
    private RectTransform topStrip;

    [SerializeField]
    [Tooltip("strip below the hole; stretches the full width")]
    private RectTransform bottomStrip;

    [SerializeField]
    [Tooltip("strip left of the hole; spans the hole's height")]
    private RectTransform leftStrip;

    [SerializeField]
    [Tooltip("strip right of the hole; spans the hole's height")]
    private RectTransform rightStrip;

    [SerializeField]
    [Tooltip("caption kept just above the hole; may be empty")]
    private RectTransform moveCaption;

    [SerializeField]
    [Tooltip("color of the blue area, alpha included")]
    private Color areaColor = new Color(0.15f, 0.45f, 1f, 0.35f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("hole size beyond the stick; px")]
    private float marginPx = 12f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("gap from the hole to the caption above it; px")]
    private float captionGapPx = 30f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (
            topStrip == null
            || bottomStrip == null
            || leftStrip == null
            || rightStrip == null
        ) {
            Debug.LogError(
                "TutorialTouchCutout:\tmust assign all 4 strips", this);
            enabled = false;
            return;
        }

        area = (RectTransform)transform;
        Tint(topStrip);
        Tint(bottomStrip);
        Tint(leftStrip);
        Tint(rightStrip);
    }

    private void OnEnable() {
        if (overlay != null) {
            overlay.enabled = false;
        }
        stick = null;
    }

    private void OnDisable() {
        if (overlay != null) {
            overlay.enabled = true;
        }
    }

    private void LateUpdate() {
        if (stick == null) {
            stick = FindFirstObjectByType<OnScreenStick>(
                FindObjectsInactive.Include);
            if (stick == null) {
                return;
            }
        }
        Layout();
    }

    // Private Methods  ########################################################
    private void Tint(RectTransform strip) {
        Image image = strip.GetComponent<Image>();
        if (image != null) {
            image.color = areaColor;
        }
    }

    // sizes the 4 strips round the stick's rect, in this area's own space
    private void Layout() {
        stick.GetComponent<RectTransform>().GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < corners.Length; i++) {
            Vector3 local = area.InverseTransformPoint(corners[i]);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        Rect bounds = area.rect;
        float holeMinX = Mathf.Clamp(min.x - marginPx, bounds.xMin, bounds.xMax)
            - bounds.xMin;
        float holeMaxX = Mathf.Clamp(max.x + marginPx, bounds.xMin, bounds.xMax)
            - bounds.xMin;
        float holeMinY = Mathf.Clamp(min.y - marginPx, bounds.yMin, bounds.yMax)
            - bounds.yMin;
        float holeMaxY = Mathf.Clamp(max.y + marginPx, bounds.yMin, bounds.yMax)
            - bounds.yMin;

        Place(bottomStrip, 0f, 0f, bounds.width, holeMinY);
        Place(topStrip, 0f, holeMaxY, bounds.width, bounds.height - holeMaxY);
        Place(leftStrip, 0f, holeMinY, holeMinX, holeMaxY - holeMinY);
        Place(
            rightStrip,
            holeMaxX,
            holeMinY,
            bounds.width - holeMaxX,
            holeMaxY - holeMinY
        );

        if (moveCaption != null) {
            PlaceCaption((min.x + max.x) * 0.5f, max.y + marginPx);
        }
    }

    // anchors a strip to the area's bottom-left corner, then sizes it
    private static void Place(
        RectTransform strip,
        float x,
        float y,
        float width,
        float height
    ) {
        strip.anchorMin = Vector2.zero;
        strip.anchorMax = Vector2.zero;
        strip.pivot = Vector2.zero;
        strip.anchoredPosition = new Vector2(x, y);
        strip.sizeDelta = new Vector2(
            Mathf.Max(0f, width), Mathf.Max(0f, height));
    }

    // centers the caption over the hole, a gap above its top edge
    private void PlaceCaption(float localX, float localTopY) {
        Vector3 world = area.TransformPoint(new Vector3(localX, localTopY, 0f));
        world.y += captionGapPx * area.lossyScale.y;
        moveCaption.position = world;
    }

    // Private Members  ########################################################
    private readonly Vector3[] corners = new Vector3[4];

    // cached references  ------------------------------------------------------
    private RectTransform area;
    private OnScreenStick stick;
}
