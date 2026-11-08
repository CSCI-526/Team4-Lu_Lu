using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Scrollbar))]
public class ProgressBarWidget: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Cobra Marker")]
    [SerializeField]
    [Tooltip("cobra head; its height sets the marker")]
    private Transform cobraHead;

    [SerializeField]
    [Tooltip("marker moved along the bar")]
    private RectTransform cobraMarker;

    [Header("Window")]
    [SerializeField]
    [Tooltip("world-y span of the full bar, player pinned at top; u")]
    private float windowSizeY = 20f;

    [Header("Distance Readout")]
    [SerializeField]
    [Tooltip("upright UI space shared by the distance label and bracket")]
    private RectTransform distanceRoot;

    [SerializeField]
    private TMP_Text distanceLabel;

    [SerializeField]
    [Tooltip("background frame containing the distance text")]
    private RectTransform distanceLabelFrame;

    [SerializeField]
    [Tooltip("bracket spanning the two marker centers")]
    private RectTransform distanceBracket;

    [SerializeField, Min(0f)]
    [Tooltip("UI-space clearance; slide the snake left when faces get close")]
    private float markerClearance = 48f;

    [Header("Procedural UI Shapes")]
    [SerializeField]
    [Tooltip("circle and ellipse Images; share one circle generated at startup")]
    private Image[] circleImages;

    [Header("Catch-Up Cue")]
    [SerializeField]
    [Tooltip("appended w/ stack count from 2nd stacked mistake on")]
    private string catchUpStackFormat = " x{0}";

    [SerializeField]
    [Tooltip("marker flash color")]
    private Color catchUpColor = new Color(1f, 0.2f, 0.15f, 1f);

    [SerializeField]
    [Tooltip("marker flashes per s while surging")]
    private float catchUpPulseHz = 3f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (cobraHead == null) {
            Debug.LogWarning(
                "ProgressBarWidget:\tmust assign Inspector Field: cobraHead",
                this);
        }
        if (cobraMarker == null) {
            Debug.LogWarning(
                "ProgressBarWidget:\tmust assign Inspector Field: cobraMarker",
                this);
        }

        progressBar = GetComponent<Scrollbar>();

        if (progressBar == null) {
            Debug.LogError(
                "ProgressBarWidget:\tfail to get Component: Scrollbar",
                this
            );

            enabled = false;
            return;
        }

        if (windowSizeY <= 0f) {
            Debug.LogWarning(
                "ProgressBarWidget:\twindowSizeY must be positive",
                this
            );

            windowSizeY = 1f;
        }

        if (distanceRoot == null || distanceLabel == null
            || distanceLabelFrame == null
            || distanceBracket == null) {
            Debug.LogWarning(
                "ProgressBarWidget:\tassign the distance UI "
                    + "from the matching ProgressBar prefab",
                this
            );
        }

        if (distanceLabel != null) {
            distanceLabelRect = distanceLabelFrame != null
                ? distanceLabelFrame
                : distanceLabel.rectTransform;
            distanceLabelOffset = distanceLabelRect.anchoredPosition.x;
            distanceLabel.margin = Vector4.zero;
            distanceLabel.text = "-- m";
        }
        if (distanceBracket != null) {
            distanceBracketOffset = distanceBracket.anchoredPosition.x;
        }

        CreateCircleGraphics();

        if (cobraHead != null) {
            cobra = cobraHead.GetComponentInParent<CobraMotor>();
        }

        if (cobraMarker != null) {
            markerGraphic = cobraMarker.GetComponent<Graphic>();
            if (markerGraphic != null) {
                markerBaseColor = markerGraphic.color;
            }
        }

        if (cobra != null) {
            cobra.CatchUpStarted += OnCatchUpStarted;
            cobra.ChaseStarted += OnChaseStarted;
        }
    }

    private void OnDestroy() {
        if (cobra != null) {
            cobra.CatchUpStarted -= OnCatchUpStarted;
            cobra.ChaseStarted -= OnChaseStarted;
        }
        if (circleSprite != null) {
            Destroy(circleSprite);
        }
        if (circleTexture != null) {
            Destroy(circleTexture);
        }
    }

    private void LateUpdate() {
        if (!TryFindPlayer()) {
            return;
        }

        // player stays at the far end of the bar
        progressBar.SetValueWithoutNotify(1f);

        UpdateCobraProgress();
        UpdateCatchUpCue();
    }

    // Event Handlers  #########################################################
    // the warning text lives on the popup prefabs; the stack count goes in as
    // its {0} suffix, empty for a first surge
    private void OnCatchUpStarted(int stacks) {
        if (PopUpManager.I == null) {
            return;
        }
        PopUpManager.I.Show(
            PopUpId.CobraSpeedUp,
            stacks > 1 ? string.Format(catchUpStackFormat, stacks) : ""
        );
    }

    private void OnChaseStarted() {
        if (PopUpManager.I == null) {
            return;
        }
        PopUpManager.I.Show(PopUpId.CobraChase);
    }

    // constants  ##############################################################
    private const string PLAYER_TAG = "Player";
    private const int CIRCLE_TEXTURE_SIZE = 128;

    // private members  ########################################################
    private Scrollbar progressBar;
    private Transform player;

    private bool hasWarnedNoPlayer;

    private CobraMotor cobra;
    private Graphic markerGraphic;
    private Color markerBaseColor;

    private RectTransform distanceLabelRect;
    private float distanceLabelOffset;
    private float distanceBracketOffset;
    private int shownDecimeters = int.MinValue;

    private Texture2D circleTexture;
    private Sprite circleSprite;

    // private methods  ########################################################
    // Every face circle/ellipse uses this one antialiased white sprite,
    // tinted by its Image. No built-in sprite IDs or imported artwork needed.
    private void CreateCircleGraphics() {
        if (circleImages == null || circleImages.Length == 0) {
            Debug.LogWarning(
                "ProgressBarWidget:\tassign circleImages from the matching prefab",
                this
            );
            return;
        }

        Color32[] pixels =
            new Color32[CIRCLE_TEXTURE_SIZE * CIRCLE_TEXTURE_SIZE];
        float center = (CIRCLE_TEXTURE_SIZE - 1) * 0.5f;
        float radius = CIRCLE_TEXTURE_SIZE * 0.5f - 1f;
        for (int y = 0; y < CIRCLE_TEXTURE_SIZE; y++) {
            for (int x = 0; x < CIRCLE_TEXTURE_SIZE; x++) {
                float dx = x - center;
                float dy = y - center;
                float alpha = Mathf.Clamp01(
                    radius - Mathf.Sqrt(dx * dx + dy * dy)
                );
                pixels[y * CIRCLE_TEXTURE_SIZE + x] = new Color32(
                    255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f)
                );
            }
        }

        circleTexture = new Texture2D(
            CIRCLE_TEXTURE_SIZE, CIRCLE_TEXTURE_SIZE,
            TextureFormat.RGBA32, false
        );
        circleTexture.name = "Monkobra UI Circle";
        circleTexture.hideFlags = HideFlags.DontSave;
        circleTexture.wrapMode = TextureWrapMode.Clamp;
        circleTexture.filterMode = FilterMode.Bilinear;
        circleTexture.SetPixels32(pixels);
        circleTexture.Apply(false, false);

        circleSprite = Sprite.Create(
            circleTexture,
            new Rect(0f, 0f, CIRCLE_TEXTURE_SIZE, CIRCLE_TEXTURE_SIZE),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect
        );
        circleSprite.name = "Monkobra UI Circle";
        circleSprite.hideFlags = HideFlags.DontSave;
        foreach (Image image in circleImages) {
            if (image == null) {
                continue;
            }
            image.overrideSprite = null;
            image.sprite = circleSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }
    }

    private bool TryFindPlayer() {
        if (player != null) {
            return true;
        }

        GameObject playerObject =
            GameObject.FindGameObjectWithTag(PLAYER_TAG);

        if (playerObject == null) {
            if (!hasWarnedNoPlayer) {
                Debug.LogWarning(
                    "ProgressBarWidget:\tfail to find GameObject tagged: Player",
                    this
                );

                hasWarnedNoPlayer = true;
            }

            return false;
        }

        player = playerObject.transform;
        return true;
    }

    private void UpdateCobraProgress() {
        if (cobraHead == null || cobraMarker == null) {
            return;
        }

        // gap below player, clamped so a far cobra sticks at bar bottom
        float gap = Mathf.Max(0f, player.position.y - cobraHead.position.y);
        float cobraProgress = 1f - Mathf.Clamp01(gap / windowSizeY);

        Vector2 anchor = new Vector2(0.5f, 0.5f);

        switch (progressBar.direction) {
            case Scrollbar.Direction.LeftToRight:
                anchor.x = cobraProgress;
                break;

            case Scrollbar.Direction.RightToLeft:
                anchor.x = 1f - cobraProgress;
                break;

            case Scrollbar.Direction.BottomToTop:
                anchor.y = cobraProgress;
                break;

            case Scrollbar.Direction.TopToBottom:
                anchor.y = 1f - cobraProgress;
                break;
        }

        cobraMarker.anchorMin = anchor;
        cobraMarker.anchorMax = anchor;
        cobraMarker.anchoredPosition = Vector2.zero;

        UpdateDistanceReadout(gap);
    }

    // The bar clamps the cobra marker at its bottom, but the number always
    // shows the full world-height gap, in world units shown as meters, the
    // same as the height markers on the trunk.
    private void UpdateDistanceReadout(float gap) {
        if (distanceRoot == null || distanceLabelRect == null
            || distanceBracket == null || progressBar.handleRect == null) {
            return;
        }

        int decimeters = Mathf.RoundToInt(gap * 10f);
        if (decimeters != shownDecimeters) {
            shownDecimeters = decimeters;
            distanceLabel.text = (decimeters * 0.1f).ToString(
                "0.0", CultureInfo.InvariantCulture
            ) + " m";
        }

        // Convert the actual marker centers into the upright readout's
        // coordinates. The original bar is rotated 90 degrees and stretched.
        RectTransform handle = progressBar.handleRect;
        Vector3 monkeyPoint = distanceRoot.InverseTransformPoint(
            handle.TransformPoint(handle.rect.center)
        );
        Vector3 cobraPoint = distanceRoot.InverseTransformPoint(
            cobraMarker.TransformPoint(cobraMarker.rect.center)
        );
        float markerGap = Mathf.Abs(monkeyPoint.y - cobraPoint.y);
        float sideStep = Mathf.Max(0f, markerClearance - markerGap);
        cobraMarker.position -= distanceRoot.TransformVector(
            new Vector3(sideStep, 0f, 0f)
        );
        cobraPoint.x -= sideStep;

        float middleY = (monkeyPoint.y + cobraPoint.y) * 0.5f;

        float leftmost = Mathf.Min(monkeyPoint.x, cobraPoint.x);
        distanceLabelRect.anchoredPosition = new Vector2(
            leftmost + distanceLabelOffset, middleY
        );
        distanceBracket.anchoredPosition = new Vector2(
            leftmost + distanceBracketOffset, middleY
        );
        distanceBracket.gameObject.SetActive(markerGap >= markerClearance);
        distanceBracket.sizeDelta = new Vector2(
            distanceBracket.sizeDelta.x,
            markerGap
        );
    }

    // the marker flashes while the cobra catches up, only during a run
    private void UpdateCatchUpCue() {
        bool isPlaying =
            GameController.I == null
            || GameController.I.State == GameState.Playing;
        bool isCatchingUp = isPlaying && cobra != null && cobra.IsCatchingUp;

        // 0~1 pulse, frozen with the game clock
        float pulse =
            0.5f + 0.5f * Mathf.Sin(Time.time * catchUpPulseHz * 2f * Mathf.PI);

        if (markerGraphic != null) {
            markerGraphic.color = isCatchingUp
                ? Color.Lerp(markerBaseColor, catchUpColor, pulse)
                : markerBaseColor;
        }
    }
}
