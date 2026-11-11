using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows one <see cref="InputHint"/> on the <see cref="Image"/> beside it,
/// following <see cref="ControlStyleManager"/> live: a clip when the set has
/// one for the style, else a still icon, else nothing, and then the optional
/// fallback object (the old text) shows in its place.
/// </summary>
[RequireComponent(typeof(Image))]
public class InputIconImage: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>
    /// Points the image at another hint, for an owner that builds or reuses
    /// it at runtime, and applies it at once.
    /// </summary>
    public void SetHint(InputHint newHint) {
        hint = newHint;
        Apply(ControlStyleManager.Current);
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("icon table")]
    private InputIconSet iconSet;

    [SerializeField]
    [Tooltip("hint this image shows")]
    private InputHint hint;

    [SerializeField]
    [Tooltip("object shown only while the set has no art for the hint, eg "
        + "the old text; may be empty")]
    private GameObject fallback;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (iconSet == null) {
            Debug.LogError(
                "InputIconImage:\tmust assign Inspector Field: iconSet", this);
            enabled = false;
            return;
        }
        image = GetComponent<Image>();
        player = GetComponent<SpriteClipPlayer>();
        if (player == null) {
            player = gameObject.AddComponent<SpriteClipPlayer>();
        }
    }

    private void OnEnable() {
        ControlStyleManager.StyleChanged += Apply;
        Apply(ControlStyleManager.Current);
    }

    private void OnDisable() {
        ControlStyleManager.StyleChanged -= Apply;
    }

    // Private Methods  ########################################################
    private void Apply(ControlStyle style) {
        Sprite[] clip = iconSet.GetClip(hint, style);
        Sprite still = iconSet.GetIcon(hint, style);

        if (clip != null) {
            player.Play(clip, iconSet.GetClipFps(hint));
        } else {
            player.Stop();
            image.sprite = still;
        }

        bool hasArt = clip != null || still != null;
        image.enabled = hasArt;
        if (fallback != null) {
            fallback.SetActive(!hasArt);
        }
    }

    // Private Members  ########################################################
    // cached references  ------------------------------------------------------
    private Image image;
    private SpriteClipPlayer player;
}
