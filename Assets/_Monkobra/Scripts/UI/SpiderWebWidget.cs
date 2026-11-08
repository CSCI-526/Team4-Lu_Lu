using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space UI for the spider web trap, driven by the monkey's
/// <see cref="SpiderWebStruggle"/> events. The first trap since the scene
/// loaded asks for the web hint popup and shows the progress bar, later traps
/// show the bar only, and breaking free hides both. The hint ranks above the
/// low-stamina hint, which the <see cref="PopUpManager"/> resumes afterward;
/// a loss drops it there too.
/// <para>
/// Put it on an always-active Canvas child: it only toggles the bar object,
/// so its event subscriptions live on while that is hidden.
/// </para>
/// </summary>
public class SpiderWebWidget: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("monkey's struggle in this scene, its events drive this UI")]
    private SpiderWebStruggle struggle;

    [SerializeField]
    [Tooltip("progress bar root, background plus fill, shown while trapped")]
    private GameObject progressBar;

    [SerializeField]
    [Tooltip("fill image, set to Filled, Horizontal from the left at runtime")]
    private Image progressFill;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (struggle == null) {
            Debug.LogWarning(
                "SpiderWebWidget:\tmust assign Inspector Field: struggle",
                this
            );
        }
        if (progressBar == null) {
            Debug.LogWarning(
                "SpiderWebWidget:\tmust assign Inspector Field: progressBar",
                this
            );
        }
        if (progressFill == null) {
            Debug.LogWarning(
                "SpiderWebWidget:\tmust assign Inspector Field: progressFill",
                this
            );
        }

        ConfigureFill();
        SetVisible(false);
    }

    private void OnEnable() {
        if (struggle != null) {
            struggle.Trapped += OnTrapped;
            struggle.ProgressChanged += OnProgressChanged;
            struggle.Escaped += OnEscaped;
        }
    }

    private void OnDisable() {
        if (struggle != null) {
            struggle.Trapped -= OnTrapped;
            struggle.ProgressChanged -= OnProgressChanged;
            struggle.Escaped -= OnEscaped;
        }
    }

    // the cobra can end the game on a held monkey, no Escaped follows, and
    // the bar would show through the half-transparent end panel
    private void Update() {
        if (
            isVisible
            && GameController.I != null
            && GameController.I.State != GameState.Playing
        ) {
            SetVisible(false);
        }
    }

    // Event Handlers  #########################################################
    private void OnTrapped(bool isFirstTrap) {
        SetFill(0f);
        SetVisible(true);
        if (isFirstTrap) {
            ShowHint();
        }
    }

    private void OnProgressChanged(float progress) {
        SetFill(progress);
    }

    private void OnEscaped() {
        SetVisible(false);
        if (PopUpManager.I != null) {
            PopUpManager.I.Hide(PopUpId.SpiderWebHint);
        }
    }

    // private members  ########################################################
    private bool isVisible; // bar shown right now

    // private methods  ########################################################
    // a Filled Image without a sprite draws a plain full quad and ignores
    // fillAmount, so fall back to a sprite made from Unity's own white texture
    private void ConfigureFill() {
        if (progressFill == null) {
            return;
        }

        if (progressFill.sprite == null) {
            Texture2D white = Texture2D.whiteTexture;
            progressFill.sprite = Sprite.Create(
                white,
                new Rect(0f, 0f, white.width, white.height),
                new Vector2(0.5f, 0.5f)
            );
        }
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0f;
    }

    private void SetFill(float progress) {
        if (progressFill != null) {
            progressFill.fillAmount = Mathf.Clamp01(progress);
        }
    }

    private void ShowHint() {
        if (PopUpManager.I == null) {
            Debug.LogWarning(
                "SpiderWebWidget:\tfail to get singleton: PopUpManager", this);
            return;
        }
        PopUpManager.I.Show(PopUpId.SpiderWebHint);
    }

    private void SetVisible(bool showBar) {
        isVisible = showBar;
        if (progressBar != null) {
            progressBar.SetActive(showBar);
        }
    }
}
