using UnityEngine;

/// <summary>
/// Shows the animated icon of a hint, one prefab per control style, and
/// follows <see cref="ControlStyleManager"/> live, so a hint teaches the
/// Preferred input of the style in use (Space press for a keyboard, a finger
/// tap for touch). A style w/o a prefab shows nothing; w/o any prefab the
/// slot switches itself off, so a layout around it closes up.
/// <para>
/// Each icon is spawned once, under a holder that scales it to
/// <see cref="fitHeight"/>, never on the icon root: its Animator owns the
/// root's scale. Put it on a UI child, or build one in code w/
/// <see cref="Create"/>.
/// </para>
/// </summary>
public class HintIconSlot: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>
    /// Builds a slot at runtime under a parent, for a hint made in code. The
    /// object is made inactive first so <see cref="Awake"/> sees the prefabs.
    /// </summary>
    /// <returns>the rect to anchor, or null w/o a parent</returns>
    public static RectTransform Create(
        RectTransform parent,
        GameObject keyboardPrefab,
        GameObject touchPrefab,
        float fitHeight
    ) {
        if (parent == null) {
            return null;
        }

        var go = new GameObject("HintIcon", typeof(RectTransform));
        go.layer = 5; // UI
        go.SetActive(false);
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);

        HintIconSlot slot = go.AddComponent<HintIconSlot>();
        slot.keyboardPrefab = keyboardPrefab;
        slot.touchPrefab = touchPrefab;
        slot.fitHeight = fitHeight;
        go.SetActive(true);
        return rect;
    }

    // Inspector Fields  #######################################################
    [Header("Icons")]
    [SerializeField]
    [Tooltip("animated icon while a keyboard is in use, ie a Space press")]
    private GameObject keyboardPrefab;

    [SerializeField]
    [Tooltip("animated icon while touch is in use, ie a finger tap")]
    private GameObject touchPrefab;

    [Header("Fit")]
    [SerializeField]
    [Min(1f)]
    [Tooltip("icons shrink to this height, never grow; canvas units")]
    private float fitHeight = 64f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        if (keyboardPrefab == null && touchPrefab == null) {
            gameObject.SetActive(false);
            return;
        }

        keyboardHolder = Spawn("Keyboard", keyboardPrefab);
        touchHolder = Spawn("Touch", touchPrefab);
    }

    private void OnEnable() {
        ControlStyleManager.StyleChanged += Apply;
        Apply(ControlStyleManager.Current);
    }

    private void OnDisable() {
        ControlStyleManager.StyleChanged -= Apply;
    }

    // private members  ########################################################
    private GameObject keyboardHolder;
    private GameObject touchHolder;

    // private methods  ########################################################
    // a holder per style, so the style flip is a SetActive and the icon's
    // Animator restarts from its first key each time it shows
    private GameObject Spawn(string holderName, GameObject prefab) {
        if (prefab == null) {
            return null;
        }

        var holder = new GameObject(holderName, typeof(RectTransform));
        holder.layer = 5; // UI
        var holderRect = (RectTransform)holder.transform;
        holderRect.SetParent(transform, false);
        holderRect.anchorMin = holderRect.anchorMax = new Vector2(0.5f, 0.5f);
        holderRect.sizeDelta = Vector2.zero;

        GameObject icon = Instantiate(prefab, holderRect, false);
        var iconRect = icon.transform as RectTransform;
        if (iconRect != null && iconRect.sizeDelta.y > fitHeight) {
            holderRect.localScale =
                Vector3.one * (fitHeight / iconRect.sizeDelta.y);
        }
        return holder;
    }

    private void Apply(ControlStyle style) {
        bool isTouch = style == ControlStyle.Touch;
        if (keyboardHolder != null) {
            keyboardHolder.SetActive(!isTouch);
        }
        if (touchHolder != null) {
            touchHolder.SetActive(isTouch);
        }
    }
}
