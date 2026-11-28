using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space UI for the spider web trap, driven by the monkey's
/// <see cref="SpiderWebStruggle"/> events. A trap asks for the web hint
/// popup until the first escape of the session, and always shows the progress
/// bar; breaking free hides both and marks the hint seen. The hint ranks
/// above the low-stamina hint, which the <see cref="PopUpManager"/> resumes
/// afterward; a loss drops it there too.
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
    [Tooltip(
        "arc root, square, its Image is the band background and its child "
            + "the fill; shown while trapped"
    )]
    private GameObject progressBar;

    [SerializeField]
    [Tooltip("fill image, set to a 120 deg ring band at runtime")]
    private Image progressFill;

    [Header("Arc")]
    [SerializeField]
    [Range(0f, 360f)]
    [Tooltip(
        "direction the band arches toward, its middle; deg counterclockwise "
            + "from screen right, 90 is the top"
    )]
    private float arcCenterDeg = 90f;

    [SerializeField]
    [Range(0.05f, 0.5f)]
    [Tooltip("band thickness as a share of its radius; 0~1")]
    private float bandThickness = 0.18f;

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

    private void OnDestroy() {
        if (ringSprite != null) {
            Destroy(ringSprite.texture);
            Destroy(ringSprite);
        }
    }

    // the cobra can end the game on a held monkey, no Escaped follows, and
    // the bar would show through the half-transparent end panel
    private void Update() {
        if (
            isVisible
            && RunController.I != null
            && RunController.I.State != GameState.Playing
        ) {
            SetVisible(false);
        }
    }

    // Event Handlers  #########################################################
    // the hint returns on each trap until one escape marks it seen
    private void OnTrapped(bool _) {
        SetFill(0f);
        SetVisible(true);
        ShowHint();
    }

    private void OnProgressChanged(float progress) {
        SetFill(progress);
    }

    private void OnEscaped() {
        SetVisible(false);
        if (TutorialSkipManager.I != null) {
            TutorialSkipManager.I.MarkSeen(TutorialItem.WebEscapeHint);
        }
        if (PopUpManager.I != null) {
            PopUpManager.I.Hide(PopUpId.SpiderWebHint);
        }
    }

    // private members  ########################################################
    private bool isVisible; // bar shown right now
    private Sprite ringSprite; // runtime ring both arc images draw

    private const float ARC_SPAN_DEG = 120f; // sweep of the whole band
    private const float ARC_FRACTION = ARC_SPAN_DEG / 360f; // of the ring
    private const int RING_TEXTURE_PX = 256;

    // private methods  ########################################################
    // the band is a ring sprite cut by a radial fill to 1/3 of the circle, so
    // the background shows 1/3 and the fill 1/3 of the progress; a Filled
    // Image without a sprite ignores fillAmount, so the ring is made here
    private void ConfigureFill() {
        if (progressFill == null) {
            return;
        }

        ringSprite = BuildRingSprite(bandThickness);
        ConfigureArcImage(progressFill);
        progressFill.fillAmount = 0f;

        if (progressBar == null) {
            return;
        }
        Image back = progressBar.GetComponent<Image>();
        if (back != null) {
            ConfigureArcImage(back);
            back.fillAmount = ARC_FRACTION;
        }
        // the fill starts at the top and runs clockwise, so turn the whole arc
        // until its start sits half a span before arcCenterDeg
        progressBar.transform.localRotation = Quaternion.Euler(
            0f,
            0f,
            arcCenterDeg + ARC_SPAN_DEG * 0.5f - 90f
        );
    }

    private void ConfigureArcImage(Image arc) {
        arc.sprite = ringSprite;
        arc.type = Image.Type.Filled;
        arc.fillMethod = Image.FillMethod.Radial360;
        arc.fillOrigin = (int)Image.Origin360.Top;
        arc.fillClockwise = true;
    }

    private void SetFill(float progress) {
        if (progressFill != null) {
            progressFill.fillAmount = Mathf.Clamp01(progress) * ARC_FRACTION;
        }
    }

    // a white ring, 1 px soft at both edges, tinted by the Image color
    private static Sprite BuildRingSprite(float thickness) {
        int size = RING_TEXTURE_PX;
        float center = (size - 1) * 0.5f;
        float outerR = size * 0.5f;
        float innerR = outerR * (1f - thickness);

        Color32[] pixels = new Color32[size * size];
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                float dist = Mathf.Sqrt(
                    (j - center) * (j - center) + (i - center) * (i - center)
                );
                float alpha =
                    Mathf.Clamp01(outerR - dist) * Mathf.Clamp01(dist - innerR);
                pixels[i * size + j] = new Color32(
                    255,
                    255,
                    255,
                    (byte)Mathf.RoundToInt(alpha * 255f)
                );
            }
        }

        Texture2D texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        ) {
            name = "SpiderWebRing",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    private void ShowHint() {
        if (TutorialSkipManager.I != null
            && TutorialSkipManager.I.ShouldSkip(TutorialItem.WebEscapeHint)) {
            return;
        }
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
