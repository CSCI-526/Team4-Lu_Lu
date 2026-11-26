using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// the intro comic between the title and the level selection: the panels of
/// <see cref="IntroComicConfig"/>, built under the stage at start, each easing
/// its camera from one view to another while its cued layers move on their
/// own. Space, E, Enter, a click or a tap (UI/StartPress) first lets a panel
/// catch up to its end, so no pop or reveal is missed, then turns the page;
/// after the last page, and on Esc or the Skip button, the level selection
/// opens. It plays once a launch: it marks <see cref="TutorialItem.IntroComic"/>
/// seen, so later title presses go straight to the menu. Runs on unscaled time.
/// </summary>
public class IntroComicScreen: MonoBehaviour {
    // Public Members  #########################################################
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("the panels, written by Monkobra/Build Intro Comic")]
    private IntroComicConfig config;

    [Header("Input")]
    [SerializeField]
    [Tooltip("Input Actions asset holding the UI map")]
    private InputActionAsset inputActions;

    [SerializeField]
    [Tooltip("path of the page-turn action: Space, E, Enter, a click or a "
        + "touch")]
    private string nextActionPath = "UI/StartPress";

    [SerializeField]
    [Tooltip("path of the skip action: Esc, P")]
    private string skipActionPath = "UI/Cancel";

    [SerializeField]
    [Min(0.05f)]
    [Tooltip("seconds a panel takes to catch up to its end after a press")]
    private float catchUpS = 0.5f;

    [Header("Parts")]
    [SerializeField]
    [Tooltip("the 1280x720 frame the panels are built in; it clips them")]
    private RectTransform stage;

    [SerializeField]
    [Tooltip("the caption plate, sized to its words")]
    private RectTransform captionPlate;

    [SerializeField]
    [Tooltip("the caption words, on the plate")]
    private TMP_Text captionText;

    [SerializeField]
    [Tooltip("fades the caption between pages")]
    private CanvasGroup captionGroup;

    [SerializeField]
    [Min(0f)]
    [Tooltip("plate height above and below its words, canvas units")]
    private float captionPaddingY = 14f;

    [SerializeField]
    [Tooltip("the next-page hint, shown a while into each panel")]
    private CanvasGroup nextHint;

    [SerializeField]
    [Tooltip("skips to the level selection")]
    private Button skipButton;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null || inputActions == null || stage == null
            || captionText == null) {
            Debug.LogError(
                "IntroComicScreen:\tmust assign Inspector Fields: Config, "
                    + "Input Actions, Stage, Caption Text",
                this
            );
            enabled = false;
            return;
        }

        nextAction = inputActions.FindAction(nextActionPath);
        if (nextAction == null) {
            Debug.LogError(
                "IntroComicScreen:\tfail to find action: " + nextActionPath,
                this
            );
            enabled = false;
            return;
        }
        skipAction = inputActions.FindAction(skipActionPath);
        if (skipAction == null) {
            Debug.LogWarning(
                "IntroComicScreen:\tfail to find action: " + skipActionPath,
                this
            );
        }

        if (TutorialSkipManager.I != null) {
            TutorialSkipManager.I.MarkSeen(TutorialItem.IntroComic);
        }
        BuildPanels();
    }

    // the two actions are shared with the title and the level selection,
    // whose OnEnable and OnDisable can run on either side of this screen's
    // during a scene load; so this screen turns them on each frame it needs
    // them and never off, or it could switch off the next screen's Esc
    private void OnEnable() {
        EnsureActionsOn();
        enabledTime = Time.unscaledTime;
    }

    private void Start() {
        if (skipButton != null) {
            // never selected, so Space turns the page instead of pressing it
            skipButton.navigation = new Navigation {
                mode = Navigation.Mode.None
            };
            skipButton.onClick.AddListener(Finish);
        }

        if (panels.Count == 0) {
            Finish();
            return;
        }
        Show(0, true);
    }

    private void Update() {
        if (current < 0) {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        PanelState panel = panels[current];
        panelTime += dt * rushSpeed;
        if (panelTime >= panel.SettleS) {
            rushSpeed = 1f;
        }
        Animate(panel, panelTime);
        UpdateFade(dt);
        UpdateHint();

        if (isLeaving || Time.unscaledTime - enabledTime < config.InputDelayS) {
            return;
        }
        EnsureActionsOn();
        if (skipAction != null && skipAction.WasPressedThisFrame()) {
            Finish();
            return;
        }
        if (nextAction.WasPressedThisFrame() && !IsPressOnSkipButton()) {
            Advance();
        }
    }

    private void OnDestroy() {
        if (skipButton != null) {
            skipButton.onClick.RemoveListener(Finish);
        }
    }

    // private members  ########################################################
    private sealed class LayerState {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Home;
        public IntroComicConfig.Cue Cue;
    }

    private sealed class PanelState {
        public IntroComicConfig.Panel Data;
        public RectTransform Rect;
        public CanvasGroup Group;
        public readonly List<LayerState> Layers = new List<LayerState>();
        public float SettleS;
    }

    private const int UI_LAYER = 5;
    private static readonly Vector2 PANEL_SIZE = new Vector2(1280f, 720f);
    private const float POP_FROM = 0.6f; // a popping layer's first scale
    private const float STEPS_PER_S = 2f; // a walk's bobs
    private const float FALL_SWAYS = 2f; // a fall's sways, left and back
    private const float FALL_TILT = 25f; // degrees at the end of a sway
    private const float DRIFT_BOBS_PER_S = 0.8f;
    private const float GLOW_PERIOD_S = 1.5f; // a revealed layer's slow pulse
    private const float HINT_FADE_S = 0.3f;

    private readonly List<PanelState> panels = new List<PanelState>();
    private InputAction nextAction;
    private InputAction skipAction;
    private float enabledTime;
    private int current = -1;
    private PanelState previous;
    private float panelTime;
    private float rushSpeed = 1f;
    private bool isFading;
    private float fadeTime;
    private bool isCaptionSwapped;
    private bool isLeaving;

    // private methods  ########################################################
    // a hidden panel per config entry, its layers placed and posed
    private void BuildPanels() {
        RectTransform holder = NewRect("Panels", stage);
        holder.SetAsFirstSibling();
        holder.anchorMin = Vector2.zero;
        holder.anchorMax = Vector2.one;
        holder.offsetMin = holder.offsetMax = Vector2.zero;

        for (int i = 0; i < config.Panels.Count; i++) {
            IntroComicConfig.Panel data = config.Panels[i];
            PanelState panel = new PanelState {
                Data = data,
                Rect = NewRect("Panel_" + (i + 1), holder),
                SettleS = SettleTime(data),
            };
            panel.Rect.sizeDelta = PANEL_SIZE;
            panel.Group = panel.Rect.gameObject.AddComponent<CanvasGroup>();
            panel.Group.blocksRaycasts = false;
            panel.Group.interactable = false;

            foreach (IntroComicConfig.Layer layer in data.layers) {
                RectTransform rect = NewRect(layer.name, panel.Rect);
                Image image = rect.gameObject.AddComponent<Image>();
                image.sprite = layer.sprite;
                image.raycastTarget = false;
                rect.sizeDelta = layer.size;

                // a walker scales from its feet, so it stays on its path
                bool isWalker = layer.cue != null
                    && layer.cue.kind == IntroComicConfig.CueKind.Walk;
                rect.pivot = isWalker ? new Vector2(0.5f, 0f)
                    : new Vector2(0.5f, 0.5f);
                Vector2 home = layer.center
                    - (isWalker ? new Vector2(0f, layer.size.y / 2f)
                        : Vector2.zero);
                rect.anchoredPosition = home;
                panel.Layers.Add(new LayerState {
                    Rect = rect, Image = image, Home = home, Cue = layer.cue,
                });
            }
            panel.Rect.gameObject.SetActive(false);
            panels.Add(panel);
        }
    }

    // a press first lets the panel catch up to its end, then turns the page
    private void Advance() {
        PanelState panel = panels[current];
        if (panelTime < panel.SettleS) {
            rushSpeed = Mathf.Max(1f, (panel.SettleS - panelTime) / catchUpS);
            return;
        }
        if (current + 1 >= panels.Count) {
            Finish();
            return;
        }
        Show(current + 1, false);
    }

    // the new panel fades in over the old one, which stays opaque below it
    private void Show(int index, bool isInstant) {
        if (previous != null) {
            previous.Rect.gameObject.SetActive(false); // a fade cut short
        }
        previous = current >= 0 ? panels[current] : null;
        current = index;
        panelTime = 0f;
        rushSpeed = 1f;

        PanelState panel = panels[index];
        panel.Rect.gameObject.SetActive(true);
        panel.Rect.SetAsLastSibling();
        Animate(panel, 0f);

        isFading = !isInstant;
        fadeTime = 0f;
        isCaptionSwapped = isInstant;
        panel.Group.alpha = isInstant ? 1f : 0f;
        if (isInstant) {
            SetCaption(panel.Data.caption);
            SetAlpha(captionGroup, 1f);
        }
        SetAlpha(nextHint, 0f);
    }

    private void UpdateFade(float dt) {
        if (!isFading) {
            return;
        }
        fadeTime += dt;
        float p = config.FadeS > 0f ? Mathf.Clamp01(fadeTime / config.FadeS)
            : 1f;
        panels[current].Group.alpha = p;

        // the caption fades out, changes, and fades back in
        if (p < 0.5f) {
            SetAlpha(captionGroup, 1f - 2f * p);
        } else {
            if (!isCaptionSwapped) {
                SetCaption(panels[current].Data.caption);
                isCaptionSwapped = true;
            }
            SetAlpha(captionGroup, 2f * p - 1f);
        }

        if (p >= 1f) {
            isFading = false;
            if (previous != null) {
                previous.Rect.gameObject.SetActive(false);
                previous = null;
            }
        }
    }

    private void UpdateHint() {
        float shown = isFading ? 0f
            : Mathf.Clamp01((panelTime - config.HintDelayS) / HINT_FADE_S);
        SetAlpha(nextHint, shown);
    }

    // the plate grows to fit the caption's lines
    private void SetCaption(string words) {
        captionText.text = words;
        if (captionPlate == null) {
            return;
        }
        float width = captionText.rectTransform.rect.width;
        float height = captionText.GetPreferredValues(words, width, 0f).y;
        captionPlate.sizeDelta = new Vector2(
            captionPlate.sizeDelta.x,
            Mathf.Ceil(height) + 2f * captionPaddingY
        );
    }

    private void EnsureActionsOn() {
        if (nextAction != null && !nextAction.enabled) {
            nextAction.Enable();
        }
        if (skipAction != null && !skipAction.enabled) {
            skipAction.Enable();
        }
    }

    private void Finish() {
        if (isLeaving) {
            return;
        }
        isLeaving = true;
        SceneNames.Load(SceneNames.LEVEL_SELECT);
    }

    // a click or tap on Skip is the button's, not a page turn
    private bool IsPressOnSkipButton() {
        Pointer pointer = Pointer.current;
        if (skipButton == null || pointer == null
            || !pointer.press.wasPressedThisFrame) {
            return false;
        }
        return RectTransformUtility.RectangleContainsScreenPoint(
            (RectTransform)skipButton.transform,
            pointer.position.ReadValue(),
            null // the canvas is Screen Space Overlay
        );
    }

    // the camera: the panel scaled and moved so the view's center sits in
    // the middle of the stage, never past the panel's edge
    private static void Animate(PanelState panel, float t) {
        IntroComicConfig.Panel data = panel.Data;
        float k = EaseInOut(Mathf.Clamp01(t / data.motionS));
        float zoom =
            Mathf.Max(1f, Mathf.Lerp(data.start.zoom, data.end.zoom, k));
        Vector2 center = Vector2.Lerp(data.start.center, data.end.center, k);
        Vector2 reach = PANEL_SIZE / 2f * (1f - 1f / zoom);
        center.x = Mathf.Clamp(center.x, -reach.x, reach.x);
        center.y = Mathf.Clamp(center.y, -reach.y, reach.y);
        panel.Rect.localScale = new Vector3(zoom, zoom, 1f);
        panel.Rect.anchoredPosition = -center * zoom;

        foreach (LayerState layer in panel.Layers) {
            if (layer.Cue != null
                && layer.Cue.kind != IntroComicConfig.CueKind.None) {
                Play(layer, t);
            }
        }
    }

    private static void Play(LayerState layer, float t) {
        IntroComicConfig.Cue cue = layer.Cue;
        float local = t - cue.delayS;
        float period = Mathf.Max(0.01f, cue.periodS);
        float u = Mathf.Clamp01(local / period);
        float wave = Mathf.Max(0f, local) * 2f * Mathf.PI / period;
        Vector2 offset = Vector2.zero;
        float scale = 1f;
        float alpha = 1f;
        float tilt = 0f;

        switch (cue.kind) {
            case IntroComicConfig.CueKind.Fall:
                float sway = Mathf.Sin(u * FALL_SWAYS * 2f * Mathf.PI);
                offset = Vector2.Lerp(cue.start, cue.end, u)
                    + new Vector2(sway * cue.amount, 0f);
                tilt = sway * FALL_TILT;
                alpha = 1f - Mathf.InverseLerp(0.8f, 1f, u);
                break;
            case IntroComicConfig.CueKind.Pulse:
                scale = 1f + cue.amount * Mathf.Abs(Mathf.Sin(wave / 2f));
                break;
            case IntroComicConfig.CueKind.Pop:
                alpha = local < 0f ? 0f : Mathf.Clamp01(u * 2f);
                scale = Mathf.LerpUnclamped(POP_FROM, 1f, EaseOutBack(u));
                break;
            case IntroComicConfig.CueKind.Walk:
                offset = Vector2.Lerp(cue.start, cue.end, u);
                offset.y += cue.amount * Mathf.Abs(
                    Mathf.Sin(u * period * STEPS_PER_S * Mathf.PI));
                scale = Mathf.Lerp(cue.startScale, cue.endScale, u);
                alpha = 1f - Mathf.InverseLerp(0.85f, 1f, u);
                break;
            case IntroComicConfig.CueKind.Drift:
                offset = Vector2.Lerp(cue.start, cue.end, u);
                offset.y += cue.amount
                    * Mathf.Sin(t * DRIFT_BOBS_PER_S * 2f * Mathf.PI);
                break;
            case IntroComicConfig.CueKind.Blink:
                alpha = local < 0f
                    || Mathf.FloorToInt(local / period) % 2 == 0 ? 1f : 0f;
                break;
            case IntroComicConfig.CueKind.Breathe:
                scale = 1f + cue.amount * (0.5f - 0.5f * Mathf.Cos(wave));
                break;
            case IntroComicConfig.CueKind.Twinkle:
                alpha =
                    Mathf.Lerp(cue.amount, 1f, 0.5f + 0.5f * Mathf.Cos(wave));
                break;
            case IntroComicConfig.CueKind.Reveal:
                // on, off, then on for good with a slow pulse
                float blinks = local / period;
                alpha = local < 0f ? 0f
                    : blinks < 1f ? 1f
                    : blinks < 2f ? 0f
                    : 0.8f + 0.2f * Mathf.Cos(
                        (local - 2f * period) * 2f * Mathf.PI / GLOW_PERIOD_S);
                break;
        }

        layer.Rect.anchoredPosition = layer.Home + offset;
        layer.Rect.localScale = new Vector3(scale, scale, 1f);
        layer.Rect.localEulerAngles = new Vector3(0f, 0f, tilt);
        Color color = layer.Image.color;
        color.a = alpha;
        layer.Image.color = color;
    }

    // when everything that happens once has happened: the camera at rest, every
    // fall, pop, walk and reveal done; the loops go on
    private static float SettleTime(IntroComicConfig.Panel data) {
        float settle = data.motionS;
        foreach (IntroComicConfig.Layer layer in data.layers) {
            IntroComicConfig.Cue cue = layer.cue;
            if (cue == null) {
                continue;
            }
            switch (cue.kind) {
                case IntroComicConfig.CueKind.Fall:
                case IntroComicConfig.CueKind.Pop:
                case IntroComicConfig.CueKind.Walk:
                    settle = Mathf.Max(settle, cue.delayS + cue.periodS);
                    break;
                case IntroComicConfig.CueKind.Reveal:
                    settle = Mathf.Max(settle, cue.delayS + 2f * cue.periodS);
                    break;
            }
        }
        return settle;
    }

    private static RectTransform NewRect(string name, Transform parent) {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = UI_LAYER;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static void SetAlpha(CanvasGroup group, float alpha) {
        if (group != null) {
            group.alpha = alpha;
        }
    }

    private static float EaseInOut(float k) {
        return 0.5f - 0.5f * Mathf.Cos(k * Mathf.PI);
    }

    private static float EaseOutBack(float u) {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float v = u - 1f;
        return 1f + c3 * v * v * v + c1 * v * v;
    }
}
