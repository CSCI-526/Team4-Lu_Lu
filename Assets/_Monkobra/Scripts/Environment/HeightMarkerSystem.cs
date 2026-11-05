using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Height markers beside the trunk: a whole number every <c>intervalU</c> of
/// climbed height, in the same units and from the same baseline as the HUD
/// Height (<see cref="RampedDifficultyManager.GetClimbedYDistance"/>), so a
/// height tuned in <see cref="GameBalanceConfig"/>, e.g. the coconut start
/// height, can be read off the trunk. Put it on an object on the trunk axis,
/// active from scene load, never under the pool's mock tree.
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
/// it. Labels carry no collider, so no physics query or spawner sees them.
/// </para>
/// <para>
/// The first time the monkey climbs past a marker, the marker brightens and
/// grows for a moment, as progress feedback; climbing past it again after a
/// drop does not repeat it.
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
    [Tooltip("climbed height b/t two markers, in HUD Height units; u")]
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
    [Tooltip("gap the line of sight to a label keeps from the trunk; u")]
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

    [Header("Look")]
    [SerializeField]
    [Min(0.1f)]
    [Tooltip("TextMeshPro font size, about 10 per u of text height")]
    private float fontSize = 12f;

    [SerializeField]
    [Tooltip("number fill color, light against the dark outline")]
    private Color faceColor = new Color(0.96f, 0.93f, 0.85f, 1f);

    [SerializeField]
    [Tooltip(
        "outline color, keeps the number readable on the sky, the ground "
            + "and the bark alike"
    )]
    private Color outlineColor = new Color(0.1f, 0.09f, 0.08f, 0.9f);

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("outline thickness, a share of the font's SDF range; 0 = none")]
    private float outlineWidth = 0.25f;

    [Header("Pass Pulse")]
    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "a marker the monkey climbs past for the first time brightens & "
            + "grows for this long, as progress feedback; 0 turns it off; s"
    )]
    private float pulseDurationS = 0.5f;

    [SerializeField]
    [Min(1f)]
    [Tooltip("size at the pulse peak, as a multiple of the plain size")]
    private float pulseScale = 1.4f;

    [SerializeField]
    [Tooltip("number fill color at the pulse peak")]
    private Color pulseColor = Color.white;

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
            foreach (TextMeshPro label in labels) {
                ApplyStyle(label);
            }
        }

        // the markers in the window around the monkey; marker 0 is the start
        // itself and never shows
        int interval = IntervalU;
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
            if (marker > highest) {
                Hide(label);
                continue;
            }

            int heightU = marker * interval;
            if (shownHeightsU[i] != heightU) {
                shownHeightsU[i] = heightU;
                // the same number format as the HUD Height
                label.text = heightU.ToString("N0");
            }

            label.transform.SetPositionAndRotation(
                new Vector3(
                    anchor.x,
                    difficulty.PlayerGameStartYPosition + heightU,
                    anchor.z
                ),
                view.rotation
            );

            // the pass pulse, or the plain look once it has faded
            float pulse = PulseWeight(heightU);
            label.color = Color.Lerp(faceColor, pulseColor, pulse);
            label.transform.localScale =
                Vector3.one * Mathf.Lerp(1f, pulseScale, pulse);

            if (!label.gameObject.activeSelf) {
                label.gameObject.SetActive(true);
            }
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
        outlineWidth = Mathf.Clamp01(outlineWidth);
        pulseDurationS = Mathf.Max(0f, pulseDurationS);
        pulseScale = Mathf.Max(1f, pulseScale);
        // a look tweak in Play mode reaches the live labels next frame
        isStyleDirty = true;
    }

    // constants  ##############################################################
    private const string LABEL_NAME = "HeightMarkerLabel";
    // label rect; the text overflows it, only its pivot end matters; u
    private const float LABEL_WIDTH_U = 6f;
    private const float LABEL_HEIGHT_U = 2f;
    // a camera closer than this to the clearance has no safe label spot; u
    private const float MIN_SIGHT_DEPTH_U = 0.01f;

    // private members  ########################################################
    private readonly List<TextMeshPro> labels = new();
    // climbed height each label shows, int.MinValue while it shows none; u
    private readonly List<int> shownHeightsU = new();
    private bool isStyleDirty; // if a look field changed since last applied

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
            labels.Add(CreateLabel());
            shownHeightsU.Add(int.MinValue);
        }
    }

    private TextMeshPro CreateLabel() {
        GameObject labelObject = new GameObject(
            LABEL_NAME,
            typeof(RectTransform)
        );
        labelObject.transform.SetParent(transform, false);

        // a bare TextMeshPro: MeshRenderer & MeshFilter, no collider
        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.sizeDelta =
            new Vector2(LABEL_WIDTH_U, LABEL_HEIGHT_U);

        MeshRenderer meshRenderer = labelObject.GetComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        ApplyStyle(label);
        labelObject.SetActive(false);
        return label;
    }

    // font, size, colors, and which end sits on the anchor
    private void ApplyStyle(TextMeshPro label) {
        // a font swap resets the material, so only on a real change
        if (font != null && label.font != font) {
            label.font = font;
        }
        label.fontSize = fontSize;
        label.color = faceColor;
        // set after the font, on the label's own material instance
        label.outlineWidth = outlineWidth;
        label.outlineColor = outlineColor;

        // the end nearest the trunk sits on the anchor, the number grows
        // away from the trunk
        label.alignment = isOnRightSide
            ? TextAlignmentOptions.MidlineLeft
            : TextAlignmentOptions.MidlineRight;
        label.rectTransform.pivot = new Vector2(isOnRightSide ? 0f : 1f, 0.5f);
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
