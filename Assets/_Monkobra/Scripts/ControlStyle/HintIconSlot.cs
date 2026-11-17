using UnityEngine;

/// <summary>
/// Shows the animated icon of a hint, one Animator controller per control
/// style, and follows <see cref="ControlStyleManager"/> live, so a hint
/// teaches the Preferred input of the style in use (Space press for a
/// keyboard, a finger tap for touch). The icon entity itself comes from the
/// <see cref="HintIconLibrary"/>; a hint only picks the controller, ie press,
/// double, hold, or rapid. A style w/o a controller shows nothing; w/o any
/// controller the slot switches itself off, so a layout around it closes up.
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
    /// object is made inactive first so <see cref="Awake"/> sees the fields.
    /// </summary>
    /// <returns>the rect to anchor, or null w/o a parent</returns>
    public static RectTransform Create(
        RectTransform parent,
        HintIconLibrary library,
        RuntimeAnimatorController keyboardController,
        RuntimeAnimatorController touchController,
        float fitHeight
    ) {
        if (parent == null) {
            return null;
        }

        GameObject go = new GameObject("HintIcon", typeof(RectTransform));
        go.layer = 5; // UI
        go.SetActive(false);
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);

        HintIconSlot slot = go.AddComponent<HintIconSlot>();
        slot.library = library;
        slot.keyboardController = keyboardController;
        slot.touchController = touchController;
        slot.fitHeight = fitHeight;
        go.SetActive(true);
        return rect;
    }

    /// <summary>
    /// Swaps the controllers at runtime, so one slot serves several hints. A
    /// popup entity calls it before each show. Both null switches the slot
    /// off, so a layout around it closes up.
    /// </summary>
    public void SetIcons(
        RuntimeAnimatorController keyboard, RuntimeAnimatorController touch
    ) {
        keyboardController = keyboard;
        touchController = touch;
        if (!isAwake) {
            return; // Awake builds from the fields
        }

        Clear();
        bool hasIcon = keyboard != null || touch != null;
        if (hasIcon) {
            Build();
        }
        gameObject.SetActive(hasIcon);
        if (hasIcon) {
            Apply(ControlStyleManager.Current);
        }
    }

    // Inspector Fields  #######################################################
    [Header("Icons")]
    [SerializeField]
    [Tooltip("the keycap and finger entities every hint shows")]
    private HintIconLibrary library;

    [SerializeField]
    [Tooltip("animates the keycap while a keyboard is in use, ie a Space "
        + "press; blank shows none")]
    private RuntimeAnimatorController keyboardController;

    [SerializeField]
    [Tooltip("animates the finger while touch is in use, ie a tap; blank "
        + "shows none")]
    private RuntimeAnimatorController touchController;

    [Header("Fit")]
    [SerializeField]
    [Min(1f)]
    [Tooltip("icons shrink to this height, never grow; canvas units")]
    private float fitHeight = 64f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("enlarges the fitted icon, 1 keeps it as fitted")]
    private float iconScale = 1f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        isAwake = true;
        if (library == null) {
            Debug.LogWarning(
                "HintIconSlot:\tmust assign Inspector Field: library", this);
        }
        if (keyboardController == null && touchController == null) {
            gameObject.SetActive(false);
            return;
        }

        Build();
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
    private bool isAwake;

    // private methods  ########################################################
    private void Build() {
        if (library == null) {
            return;
        }
        keyboardHolder = Spawn(
            "Keyboard", library.KeyboardEntity, keyboardController);
        touchHolder = Spawn("Touch", library.TouchEntity, touchController);
    }

    // off at once, so the layout does not count a holder awaiting its end of
    // frame
    private void Clear() {
        foreach (GameObject holder in new[] { keyboardHolder, touchHolder }) {
            if (holder != null) {
                holder.SetActive(false);
                Destroy(holder);
            }
        }
        keyboardHolder = touchHolder = null;
    }

    // a holder per style, so the style flip is a SetActive and the icon's
    // Animator restarts from its first key each time it shows
    private GameObject Spawn(
        string holderName,
        GameObject entity,
        RuntimeAnimatorController controller
    ) {
        if (entity == null || controller == null) {
            return null;
        }

        GameObject holder = new GameObject(holderName, typeof(RectTransform));
        holder.layer = 5; // UI
        RectTransform holderRect = (RectTransform)holder.transform;
        holderRect.SetParent(transform, false);
        holderRect.anchorMin = holderRect.anchorMax = new Vector2(0.5f, 0.5f);
        holderRect.sizeDelta = Vector2.zero;

        GameObject icon = Instantiate(entity, holderRect, false);
        // bound while off, so the Animator starts on its own controller
        icon.SetActive(false);
        Animator animator = icon.GetComponent<Animator>();
        if (animator != null) {
            animator.runtimeAnimatorController = controller;
        }
        icon.SetActive(true);

        RectTransform iconRect = icon.transform as RectTransform;
        float fit = 1f;
        if (iconRect != null && iconRect.sizeDelta.y > fitHeight) {
            fit = fitHeight / iconRect.sizeDelta.y;
        }
        holderRect.localScale = Vector3.one * fit * iconScale;
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
