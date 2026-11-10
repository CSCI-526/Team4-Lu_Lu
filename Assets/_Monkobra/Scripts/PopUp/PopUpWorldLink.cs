using UnityEngine;

/// <summary>
/// Draws a line from the bottom center of its popup to a point in the 3D
/// world, so a popup reads as attached to the object it talks about. Sits on
/// the popup variant root; the owner of the popup hands over the world target
/// with <see cref="SetTarget"/>, and the line hides while there is none or it
/// is behind the camera.
/// </summary>
// Follow this frame's camera pose after CinemachineBrain (order 100).
[DefaultExecutionOrder(200)]
public class PopUpWorldLink: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>Points the line at a world object; null hides it.</summary>
    public void SetTarget(Transform target) {
        worldTarget = target;
    }

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("the line object: a child Image of the popup, pivot at its base")]
    private RectTransform line;

    [Header("Look")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("line thickness; UI units")]
    private float lineWidth = 3f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (line == null) {
            Debug.LogWarning(
                "PopUp:\tmust assign Inspector Field: line", this);
        }
        popUpRect = (RectTransform)transform;
    }

    private void OnEnable() {
        Canvas canvas = GetComponentInParent<Canvas>();
        rootCanvas = canvas != null ? canvas.rootCanvas : null;
    }

    private void LateUpdate() {
        if (line == null) {
            return;
        }
        line.gameObject.SetActive(TryPlaceLine());
    }

    // private members  ########################################################
    private Transform worldTarget;
    private RectTransform popUpRect;
    private Canvas rootCanvas;

    // private methods  ########################################################
    // if the target is on screen and the line was placed this frame
    private bool TryPlaceLine() {
        Camera worldCamera = Camera.main;
        if (
            worldTarget == null
            || !worldTarget.gameObject.activeInHierarchy
            || worldCamera == null
            || rootCanvas == null
        ) {
            return false;
        }

        Vector3 screen = worldCamera.WorldToScreenPoint(worldTarget.position);
        Camera canvasCamera =
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;
        if (screen.z <= 0f || !RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                popUpRect, screen, canvasCamera, out Vector2 end
            )
        ) {
            return false;
        }

        // both points are local to the popup, the line's parent
        Rect rect = popUpRect.rect;
        Vector2 start = new Vector2(rect.center.x, rect.yMin);
        Vector2 span = end - start;
        float angle = Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg - 90f;

        line.localPosition = new Vector3(start.x, start.y, 0f);
        line.localRotation = Quaternion.Euler(0f, 0f, angle);
        line.sizeDelta = new Vector2(lineWidth, span.magnitude);
        return true;
    }
}
