using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Height markers beside the trunk: a whole number every <c>intervalU</c> of
/// climbed height (<see cref="RampedDifficultyManager.GetClimbedYDistance"/>),
/// shown to the player as meters, so a height tuned in
/// <see cref="PlayerBalanceConfig"/>, e.g. the coconut start height, can be
/// read off the trunk. Put it on an object on the trunk axis, active from
/// scene load, never under the pool's mock tree.
/// <para>
/// Markers are keyed to height, not to tree segments: a small pool of
/// TextMeshPro labels, built here, shows only the markers from
/// <c>showBelowU</c> under to <c>showAboveU</c> over the monkey, marker k
/// always on label k mod pool size. A recycled segment thus can neither leave
/// a marker behind nor show one twice, and the segments are never touched.
/// </para>
/// <para>
/// Each frame, once the camera has moved, the labels are placed beside the
/// trunk's silhouette on one side as seen from the camera, just far enough
/// out that the trunk never hides them, and turned to face the screen. The
/// monkey climbs on the trunk's camera-facing front, so a label never covers
/// it. Each number sits on a dark rounded plate, the look of the progress
/// bar's distance readout, so it reads on the sky, the ground and the bark
/// alike; the plate is a sprite built here, no imported art. Neither carries
/// a collider, so no physics query or spawner sees them.
/// </para>
/// <para>
/// The first time the monkey climbs past a marker, the marker grows and its
/// plate brightens for a moment, as progress feedback; climbing past it again
/// after a drop does not repeat it.
/// </para>
/// </summary>
// after the CinemachineBrain's LateUpdate (order 100, set in its .meta), so
// the labels follow this frame's camera pose
[DefaultExecutionOrder(200)]
public class HeightMarkerSystem: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("label font, LiberationSans SDF like the HUD")]
    private TMP_FontAsset font;

    [Header("Spacing")]
    [SerializeField]
    [Min(1)]
    [Tooltip("climbed height b/t two markers; u")]
    private int intervalU = 50;

    [SerializeField]
    [Min(0f)]
    [Tooltip("markers show down to this far below the monkey; u")]
    private float showBelowU = 20f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "markers show up to this far above the monkey; keep it under the "
            + "DTSPool lookAheadU, so no marker floats over the trunk top; u"
    )]
    private float showAboveU = 18f;

    [Header("Placement")]
    [SerializeField]
    [Min(0.01f)]
    [Tooltip(
        "trunk radius, measured from this object's x & z, the trunk axis; u"
    )]
    private float trunkRadiusU = 5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("gap the line of sight to a plate keeps from the trunk; u")]
    private float sightClearanceU = 0.3f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "labels sit this far behind the trunk axis, as seen from the camera, "
            + "so branches & fruit at their height draw over them; u"
    )]
    private float behindAxisU = 0.5f;

    [SerializeField]
    [Tooltip(
        "labels on the trunk's right side as seen from the camera, else on "
            + "its left"
    )]
    private bool isOnRightSide = true;

    [Header("Text")]
    [SerializeField]
    [Tooltip(
        "unit after the number, space included; the player reads climbed u "
            + "as meters, as on the progress bar"
    )]
    private string unitSuffix = " m";

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("TextMeshPro font size, about 10 per u of text height")]
    private float fontSize = 12f;

    [SerializeField]
    [Tooltip("number color, bold on the plate like the progress bar readout")]
    private Color faceColor = Color.white;

    [Header("Plate")]
    [SerializeField]
    [Tooltip("plate color, the progress bar distance readout's by default")]
    private Color plateColor = new Color(0.09f, 0.125f, 0.098f, 0.87f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("plate margin left & right of the number; u")]
    private float platePaddingXU = 0.35f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("plate margin above & below the number; u")]
    private float platePaddingYU = 0.25f;

    [SerializeField]
    [Min(0.01f)]
    [Tooltip(
        "plate corner radius; about half the plate height makes round ends; u"
    )]
    private float plateCornerU = 0.6f;

    [Header("Pass Pulse")]
    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "a marker the monkey climbs past for the first time grows & its "
            + "plate brightens for this long, as progress feedback; 0 turns "
            + "it off; s"
    )]
    private float pulseDurationS = 0.5f;

    [SerializeField]
    [Min(1f)]
    [Tooltip("size at the pulse peak, as a multiple of the plain size")]
    private float pulseScale = 1.4f;

    [SerializeField]
    [Tooltip("plate color at the pulse peak")]
    private Color pulsePlateColor = new Color(0.3f, 0.42f, 0.33f, 0.95f);

    [Header("Boss Marker")]
    [SerializeField]
    [Tooltip("boss marker's plate, beating toward the beat color")]
    private Color bossPlateColor = new Color(0.6f, 0.05f, 0.05f, 0.92f);

    [SerializeField]
    [Tooltip("boss marker's plate at the top of each beat")]
    private Color bossPlateBeatColor = new Color(0.95f, 0.2f, 0.15f, 0.95f);

    [SerializeField]
    [Min(1f)]
    [Tooltip("boss marker's size, times a plain marker's")]
    private float bossScale = 1.3f;

    [Header("Testing")]
    [SerializeField]
    [Tooltip(
        "replace intervalU w/ testIntervalU for quick checks. Untick it b4 "
            + "committing"
    )]
    private bool useTestOverride = false;

    [SerializeField]
    [Min(1)]
    [Tooltip("climbed height b/t two markers while testing; u")]
    private int testIntervalU = 5;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (font == null) {
            Debug.LogWarning(
                "HeightMarkerSystem:\tmust assign Inspector Field: font, "
                    + "labels fall back to the TMP default font",
                this
            );
        }

        if (useTestOverride) {
            Debug.LogWarning(
                "HeightMarkerSystem:\ttest override on, a marker every "
                    + testIntervalU + " u",
                this
            );
        }
    }

    private void Start() {
        if (Debug.isDebugBuild) {
            Debug.Log(
                "HeightMarkerSystem:\tready, a marker every " + IntervalU
                    + " u"
            );
        }
    }

    private void LateUpdate() {
        RampedDifficultyManager difficulty = RampedDifficultyManager.I;
        if (difficulty == null || !TryGetView(out Transform view)) {
            HideAll();
            return;
        }

        if (isStyleDirty) {
            isStyleDirty = false;
            for (int i = 0; i < labels.Count; i++) {
                ApplyStyle(i);
                // rewrite the text & refit the plate, the unit or the sizes
                // may have changed
                shownHeightsU[i] = int.MinValue;
            }
        }

        // the markers in the window around the monkey; marker 0 is the start
        // itself and never shows
        int interval = IntervalU;
        // the boss fight's height gets its own look, no extra words
        BossFightController bossFight = BossFightController.I;
        int bossHeightU = bossFight != null && bossFight.isActiveAndEnabled
            && bossFight.TriggerHeightU > 0f
            ? Mathf.RoundToInt(bossFight.TriggerHeightU)
            : int.MinValue;
        // once the fight has begun its marker is gone, the real king is there
        bool isBossMarkerDone = bossFight != null && bossFight.IsInArena;
        bool isFigureShown = false;
        UpdatePassPulse(difficulty, interval);
        float climbedU = difficulty.CurrentPlayerClimbedYDistance;
        int lowest = Mathf.Max(
            1,
            Mathf.CeilToInt((climbedU - showBelowU) / interval)
        );
        int highest = Mathf.FloorToInt((climbedU + showAboveU) / interval);
        if (highest < lowest) {
            HideAll();
            return;
        }
        EnsureLabels(highest - lowest + 1);

        if (!TryGetAnchor(view.position, out Vector3 anchor)) {
            HideAll();
            return;
        }

        // marker k always on label k mod count, so consecutive markers never
        // share a label
        int count = labels.Count;
        for (int i = 0; i < count; i++) {
            TextMeshPro label = labels[i];
            int marker = lowest + Mod(i - lowest, count);
            int heightU = marker * interval;
            bool isBoss = heightU == bossHeightU;
            if (marker > highest || (isBoss && isBossMarkerDone)) {
                Hide(label);
                continue;
            }

            // active first: the text mesh, which the plate is fitted to, only
            // builds on an active label
            if (!label.gameObject.activeSelf) {
                label.gameObject.SetActive(true);
            }

            if (shownHeightsU[i] != heightU) {
                shownHeightsU[i] = heightU;
                // thousands separated, e.g. 1,000 m
                label.text = heightU.ToString("N0") + unitSuffix;
                FitPlate(i);
            }

            label.transform.SetPositionAndRotation(
                new Vector3(
                    anchor.x,
                    difficulty.PlayerGameStartYPosition + heightU,
                    anchor.z
                ),
                view.rotation
            );

            // the pass pulse, or the plain look once it has faded; the plate
            // is a child, so it grows w/ the text
            float pulse = PulseWeight(heightU);
            plates[i].color = Color.Lerp(plateColor, pulsePlateColor, pulse);
            label.transform.localScale =
                Vector3.one * Mathf.Lerp(1f, pulseScale, pulse);
            // the boss fight's height: a bigger, red, beating plate
            if (isBoss) {
                float beat = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
                plates[i].color = Color.Lerp(
                    bossPlateColor, bossPlateBeatColor, beat
                );
                label.transform.localScale *= bossScale * (1f + 0.06f * beat);
                isFigureShown = PlaceBossFigure(bossFight, label, plates[i]);
            }
        }
        if (!isFigureShown && bossFigure != null) {
            bossFigure.gameObject.SetActive(false);
        }
    }

    // a small look-alike of the Cobra King, built by the boss fight, lies on
    // top of the boss marker's plate; labels are pooled, so it follows
    // whichever label shows that marker
    private bool PlaceBossFigure(
        BossFightController bossFight, TextMeshPro label, SpriteRenderer plate
    ) {
        if (bossFigure == null) {
            bossFigure = bossFight.BuildMarkerFigure();
            if (bossFigure == null) {
                return false;
            }
        }
        if (bossFigure.parent != label.transform) {
            bossFigure.SetParent(label.transform, false);
        }
        bossFigure.localPosition = plate.transform.localPosition
            + Vector3.up * plate.size.y * 0.5f;
        if (!bossFigure.gameObject.activeSelf) {
            bossFigure.gameObject.SetActive(true);
        }
        return true;
    }

    private void OnDestroy() {
        if (plateSprite != null) {
            Destroy(plateSprite);
        }
        if (plateTexture != null) {
            Destroy(plateTexture);
        }
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        intervalU = Mathf.Max(1, intervalU);
        testIntervalU = Mathf.Max(1, testIntervalU);
        showBelowU = Mathf.Max(0f, showBelowU);
        showAboveU = Mathf.Max(0f, showAboveU);
        trunkRadiusU = Mathf.Max(0.01f, trunkRadiusU);
        sightClearanceU = Mathf.Max(0f, sightClearanceU);
        behindAxisU = Mathf.Max(0f, behindAxisU);
        fontSize = Mathf.Max(0.1f, fontSize);
        platePaddingXU = Mathf.Max(0f, platePaddingXU);
        platePaddingYU = Mathf.Max(0f, platePaddingYU);
        plateCornerU = Mathf.Max(0.01f, plateCornerU);
        pulseDurationS = Mathf.Max(0f, pulseDurationS);
        pulseScale = Mathf.Max(1f, pulseScale);
        // a look tweak in Play mode reaches the live labels next frame
        isStyleDirty = true;
    }

    // constants  ##############################################################
    private const string LABEL_NAME = "HeightMarkerLabel";
    private const string PLATE_NAME = "Plate";
    // label rect; the text overflows it, only its pivot end matters; u
    private const float LABEL_WIDTH_U = 6f;
    private const float LABEL_HEIGHT_U = 2f;
    // a camera closer than this to the clearance has no safe label spot; u
    private const float MIN_SIGHT_DEPTH_U = 0.01f;
    // the text draws over its plate whatever their distances to the camera
    private const int PLATE_SORTING_ORDER = 0;
    private const int TEXT_SORTING_ORDER = 1;
    // the plate sits this far behind the text, away from the camera; u
    private const float PLATE_DEPTH_U = 0.02f;
    // side of the generated circle texture the plate is sliced from; px
    private const int PLATE_TEXTURE_SIZE = 64;

    // private members  ########################################################
    private readonly List<TextMeshPro> labels = new();
    private readonly List<SpriteRenderer> plates = new(); // one per label
    private Transform bossFigure; // the king look-alike on the boss marker
    // climbed height each label shows, int.MinValue while it shows none; u
    private readonly List<int> shownHeightsU = new();
    private bool isStyleDirty; // if a look field changed since last applied

    // plate sprite, shared by every plate  ------------------------------------
    private Texture2D plateTexture;
    private Sprite plateSprite;
    private float plateSpriteCornerU; // corner radius plateSprite was built for

    // pass pulse  -------------------------------------------------------------
    private int reachedMarker; // highest marker climbed past so far
    private int pulseIntervalU; // interval reachedMarker counts in; u
    private int pulseHeightU = int.MinValue; // marker pulsing, if any; u
    private float pulseEndTime; // Time.time the pulse is over

    // cached references  ------------------------------------------------------
    private Camera viewCamera;

    // private methods  ########################################################
    private int IntervalU => useTestOverride ? testIntervalU : intervalU;

    private static int Mod(int value, int divisor) {
        return ((value % divisor) + divisor) % divisor;
    }

    private bool TryGetView(out Transform view) {
        if (viewCamera == null) {
            viewCamera = Camera.main;
        }
        view = viewCamera != null ? viewCamera.transform : null;
        return view != null;
    }

    /// <summary>
    /// The world spot, y aside, for every label's trunk-side end: beside the
    /// silhouette on the chosen side, as seen from
    /// <paramref name="viewPosition"/>, pushed <c>behindAxisU</c> past the
    /// axis and out just far enough that the line of sight keeps
    /// <c>sightClearanceU</c> from the trunk.
    /// </summary>
    /// <returns>false iff the camera is too close to the trunk for any spot
    /// to be clear</returns>
    private bool TryGetAnchor(Vector3 viewPosition, out Vector3 anchor) {
        anchor = Vector3.zero;

        // the trunk is an upright cylinder, so only the horizontal plane
        // decides what it hides
        Vector3 axis = transform.position;
        Vector3 toView = viewPosition - axis;
        toView.y = 0f;
        float viewDistanceU = toView.magnitude;
        float clearRadiusU = trunkRadiusU + sightClearanceU;
        if (viewDistanceU <= clearRadiusU + MIN_SIGHT_DEPTH_U) {
            return false;
        }
        toView /= viewDistanceU;

        // right of the axis as seen from the camera, or left
        Vector3 side = isOnRightSide
            ? Vector3.Cross(toView, Vector3.up)
            : Vector3.Cross(Vector3.up, toView);

        // smallest side offset whose sight line, from a camera viewDistanceU
        // in front of the axis to a spot behindAxisU behind it, passes
        // clearRadiusU from the axis
        float sightDepthU = viewDistanceU + behindAxisU;
        float sideOffsetU = clearRadiusU * sightDepthU / Mathf.Sqrt(
            viewDistanceU * viewDistanceU - clearRadiusU * clearRadiusU
        );

        anchor = axis + side * sideOffsetU - toView * behindAxisU;
        return true;
    }

    // starts a pulse on the marker the monkey has just climbed past for the
    // first time; climbing back past it after a drop does not pulse again,
    // like the height score. Past several at once, e.g. in a leap, only the
    // top one pulses
    private void UpdatePassPulse(
        RampedDifficultyManager difficulty,
        int interval
    ) {
        int reached = Mathf.FloorToInt(
            difficulty.HighestPlayerClimbedYDistance / interval
        );

        // a new interval renumbers the markers, so count anew w/o a pulse;
        // this also sets the count on the first frame
        if (interval != pulseIntervalU) {
            pulseIntervalU = interval;
            reachedMarker = reached;
            return;
        }

        if (reached <= reachedMarker) {
            return;
        }
        reachedMarker = reached;
        pulseHeightU = reached * interval;
        pulseEndTime = Time.time + pulseDurationS;
    }

    /// <returns>0~1, how far into its pulse the marker at
    /// <paramref name="heightU"/> is: 0 at both ends and w/o a pulse, 1 at
    /// the peak</returns>
    private float PulseWeight(int heightU) {
        if (heightU != pulseHeightU || pulseDurationS <= 0f) {
            return 0f;
        }
        // game time, so a pause holds the pulse
        float leftS = pulseEndTime - Time.time;
        if (leftS <= 0f) {
            return 0f;
        }
        return Mathf.Sin(Mathf.PI * (1f - leftS / pulseDurationS));
    }

    // grows the pool to at least count labels; it never shrinks, spare
    // labels just stay hidden
    private void EnsureLabels(int count) {
        while (labels.Count < count) {
            AddLabel();
        }
    }

    // a hidden label w/ its plate; neither has a collider
    private void AddLabel() {
        GameObject labelObject = new GameObject(
            LABEL_NAME,
            typeof(RectTransform)
        );
        labelObject.transform.SetParent(transform, false);

        // a bare TextMeshPro: MeshRenderer & MeshFilter only
        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.fontStyle = FontStyles.Bold;
        label.sortingOrder = TEXT_SORTING_ORDER;
        label.rectTransform.sizeDelta =
            new Vector2(LABEL_WIDTH_U, LABEL_HEIGHT_U);

        MeshRenderer textRenderer = labelObject.GetComponent<MeshRenderer>();
        textRenderer.shadowCastingMode = ShadowCastingMode.Off;
        textRenderer.receiveShadows = false;

        // a child, so it moves, turns and pulses w/ its text
        GameObject plateObject = new GameObject(PLATE_NAME);
        plateObject.transform.SetParent(labelObject.transform, false);
        SpriteRenderer plate = plateObject.AddComponent<SpriteRenderer>();
        plate.drawMode = SpriteDrawMode.Sliced;
        plate.sortingOrder = PLATE_SORTING_ORDER;
        plate.shadowCastingMode = ShadowCastingMode.Off;
        plate.receiveShadows = false;

        labels.Add(label);
        plates.Add(plate);
        shownHeightsU.Add(int.MinValue);
        ApplyStyle(labels.Count - 1);
        labelObject.SetActive(false);
    }

    // font, size, colors, plate sprite, and which end sits on the anchor
    private void ApplyStyle(int index) {
        TextMeshPro label = labels[index];
        // a font swap resets the material, so only on a real change
        if (font != null && label.font != font) {
            label.font = font;
        }
        label.fontSize = fontSize;
        label.color = faceColor;

        // the trunk-side end sits on the anchor, the number grows away from
        // the trunk, and the margin there leaves room for the plate's padding
        label.alignment = isOnRightSide
            ? TextAlignmentOptions.MidlineLeft
            : TextAlignmentOptions.MidlineRight;
        label.rectTransform.pivot = new Vector2(isOnRightSide ? 0f : 1f, 0.5f);
        label.margin = isOnRightSide
            ? new Vector4(platePaddingXU, 0f, 0f, 0f)
            : new Vector4(0f, 0f, platePaddingXU, 0f);

        SpriteRenderer plate = plates[index];
        plate.sprite = GetPlateSprite();
        plate.color = plateColor;
    }

    // sizes the plate around the text's glyphs plus padding, never below a
    // circle, so its corners never overlap
    private void FitPlate(int index) {
        TextMeshPro label = labels[index];
        label.ForceMeshUpdate();
        Bounds glyphs = label.textBounds; // label local, scale aside

        float minSizeU = 2f * plateCornerU;
        SpriteRenderer plate = plates[index];
        plate.size = new Vector2(
            Mathf.Max(minSizeU, glyphs.size.x + 2f * platePaddingXU),
            Mathf.Max(minSizeU, glyphs.size.y + 2f * platePaddingYU)
        );
        plate.transform.localPosition = new Vector3(
            glyphs.center.x,
            glyphs.center.y,
            PLATE_DEPTH_U
        );
    }

    // one rounded-rect sprite for every plate, rebuilt only when the corner
    // radius changes; drawn sliced, its corners keep plateCornerU at any
    // plate size
    private Sprite GetPlateSprite() {
        if (plateTexture == null) {
            plateTexture = CreateCircleTexture();
        }
        if (
            plateSprite != null
            && Mathf.Approximately(plateSpriteCornerU, plateCornerU)
        ) {
            return plateSprite;
        }

        if (plateSprite != null) {
            Destroy(plateSprite);
        }
        // a corner is the circle's quarter, border px wide
        float borderPx = PLATE_TEXTURE_SIZE * 0.5f - 1f;
        plateSprite = Sprite.Create(
            plateTexture,
            new Rect(0f, 0f, PLATE_TEXTURE_SIZE, PLATE_TEXTURE_SIZE),
            new Vector2(0.5f, 0.5f),
            borderPx / plateCornerU,
            0,
            SpriteMeshType.FullRect,
            new Vector4(borderPx, borderPx, borderPx, borderPx)
        );
        plateSprite.name = "Height Marker Plate";
        plateSprite.hideFlags = HideFlags.DontSave;
        plateSpriteCornerU = plateCornerU;
        return plateSprite;
    }

    // an antialiased white disc, tinted by each plate's color; generated, so
    // no imported art is needed
    private static Texture2D CreateCircleTexture() {
        Color32[] pixels = new Color32[PLATE_TEXTURE_SIZE * PLATE_TEXTURE_SIZE];
        float center = (PLATE_TEXTURE_SIZE - 1) * 0.5f;
        float radius = PLATE_TEXTURE_SIZE * 0.5f - 1f;
        for (int y = 0; y < PLATE_TEXTURE_SIZE; y++) {
            for (int x = 0; x < PLATE_TEXTURE_SIZE; x++) {
                float dx = x - center;
                float dy = y - center;
                float alpha = Mathf.Clamp01(
                    radius - Mathf.Sqrt(dx * dx + dy * dy)
                );
                pixels[y * PLATE_TEXTURE_SIZE + x] = new Color32(
                    255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f)
                );
            }
        }

        Texture2D texture = new Texture2D(
            PLATE_TEXTURE_SIZE, PLATE_TEXTURE_SIZE,
            TextureFormat.RGBA32, false
        );
        texture.name = "Height Marker Plate";
        texture.hideFlags = HideFlags.DontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void HideAll() {
        foreach (TextMeshPro label in labels) {
            Hide(label);
        }
    }

    private static void Hide(TextMeshPro label) {
        if (label.gameObject.activeSelf) {
            label.gameObject.SetActive(false);
        }
    }
}
