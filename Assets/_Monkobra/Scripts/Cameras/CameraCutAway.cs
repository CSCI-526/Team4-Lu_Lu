using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// How a cut away from the monkey looks (the cobra alert): the game's UI
/// goes and black cinema bars grow slowly in from the top & bottom; as the
/// camera cuts back the bars slowly draw back out, and once they are gone
/// the UI is back at once, so the player sees at once this is a look
/// elsewhere, not the climb, and that the controls wait while it lasts
/// (<see cref="CameraRigManager"/> locks them).
/// <para>
/// Next to <see cref="CameraRigManager"/> on the <c>Cameras</c> prefab's
/// <c>CameraRig</c>; it only listens to <see cref="CameraRigManager.ShotChanged"/>.
/// The UI is hidden by switching off every screen space canvas but its own,
/// so their scripts & timers run on; any pause or the run's end brings the
/// UI straight back, so the pause menu & the lose screen always show. The
/// bars build their own overlay canvas at start, switched off whenever no
/// bar shows, and slide in real time. Their height & slide time are in
/// <see cref="CameraConfig"/>.
/// </para>
/// </summary>
[RequireComponent(typeof(CameraRigManager))]
public class CameraCutAway: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>if the camera has cut away from the monkey, or its bars are
    /// still drawing back: the UI is hidden till this ends</returns>
    public bool IsCutAway => isCutAway || shown > 0f;

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("bar height & slide time")]
    private CameraConfig config;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "CameraCutAway:\tmust assign Inspector Field: config", this);
            enabled = false;
            return;
        }

        rig = GetComponent<CameraRigManager>();
        Build();
    }

    private void OnEnable() {
        if (rig != null) {
            rig.ShotChanged += OnShotChanged;
        }
    }

    // the game controller wakes in its own prefab, read after every Awake
    private void Start() {
        game = GameController.I;
        if (game != null) {
            game.PauseChanged += OnPauseChanged;
            game.StateChanged += OnStateChanged;
        }
    }

    private void OnDisable() {
        if (rig != null) {
            rig.ShotChanged -= OnShotChanged;
        }
        isCutAway = false;
        ShowGameUI();
        shown = 0f;
        target = 0f;
        Apply();
    }

    private void OnDestroy() {
        if (game != null) {
            game.PauseChanged -= OnPauseChanged;
            game.StateChanged -= OnStateChanged;
        }
    }

    private void Update() {
        if (Mathf.Approximately(shown, target)) {
            return;
        }
        shown = Mathf.MoveTowards(
            shown, target, Time.unscaledDeltaTime / config.CutAwayBarSlideS);
        Apply();
        // the bars drawn all the way back: the UI returns
        if (shown <= 0f) {
            Refresh();
        }
    }

    // Private Members  ########################################################
    // under the game's UI canvas, should it ever show w/ the bars
    private const int SORT_ORDER = -10;
    private const float REFERENCE_HEIGHT = 720f;

    private CameraRigManager rig;
    private GameController game;
    private Canvas canvas;
    private RectTransform top;
    private RectTransform bottom;
    private float shown; // 0 hidden, 1 fully in
    private float target;
    private bool isCutAway;
    // the canvases this switched off, so it switches back only those
    private readonly List<Canvas> hiddenCanvases = new List<Canvas>();

    private void Build() {
        GameObject holder = new GameObject("CutAwayBars");
        holder.transform.SetParent(transform, false);
        canvas = holder.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SORT_ORDER;
        CanvasScaler scaler = holder.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, REFERENCE_HEIGHT);
        scaler.matchWidthOrHeight = 1f;

        top = Bar("Top", holder.transform);
        bottom = Bar("Bottom", holder.transform);
        Apply();
    }

    // a plain black cinema bar
    private static RectTransform Bar(string barName, Transform parent) {
        return Plain(barName, parent, Color.black);
    }

    private static RectTransform Plain(string partName, Transform parent, Color color) {
        GameObject part = new GameObject(partName, typeof(RectTransform), typeof(Image));
        part.transform.SetParent(parent, false);
        Image image = part.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = (RectTransform)part.transform;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return rect;
    }

    // each bar grows in from its screen edge, eased
    private void Apply() {
        if (canvas == null) {
            return;
        }
        canvas.enabled = shown > 0f;
        float share = config.CutAwayBarShare * Mathf.SmoothStep(0f, 1f, shown);
        top.anchorMin = new Vector2(0f, 1f - share);
        top.anchorMax = Vector2.one;
        bottom.anchorMin = Vector2.zero;
        bottom.anchorMax = new Vector2(1f, share);
    }

    // the UI goes only while the run plays on, never over a pause or its end
    private bool IsUIHidable =>
        game == null
        || (!game.IsPaused
            && (game.State == GameState.Playing || game.State == GameState.StartingZone));

    private void HideGameUI() {
        if (hiddenCanvases.Count > 0) {
            return;
        }
        foreach (Canvas other in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) {
            if (other != canvas && other.enabled && other.isRootCanvas
                && other.renderMode != RenderMode.WorldSpace) {
                other.enabled = false;
                hiddenCanvases.Add(other);
            }
        }
    }

    private void ShowGameUI() {
        foreach (Canvas other in hiddenCanvases) {
            if (other != null) {
                other.enabled = true;
            }
        }
        hiddenCanvases.Clear();
    }

    private void Refresh() {
        if (IsCutAway && IsUIHidable) {
            HideGameUI();
        } else {
            ShowGameUI();
        }
    }

    // Event Handlers  #########################################################
    private void OnShotChanged(CameraRigManager.ShotKind kind, bool isLive) {
        if (kind != CameraRigManager.ShotKind.CobraAlert) {
            return;
        }
        isCutAway = isLive;
        target = isLive ? 1f : 0f;
        Refresh();
    }

    private void OnPauseChanged(bool isPaused) {
        Refresh();
    }

    private void OnStateChanged(GameState state) {
        Refresh();
    }
}
