using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flies a semi-transparent sprite from a grabbed fruit's spot on screen to a
/// HUD target or the monkey, then reports the arrival, so a pickup visibly
/// feeds what it restores.
/// <para>
/// Sits on a full-stretch child of the HUD canvas. Sprites are pooled
/// <see cref="Image"/>s. Targets resolve every frame, so a moving camera or a
/// punching bar never strands a sprite. Scaled time drives the flight, a
/// pause freezes it; a lost run drops every flight unreported.
/// </para>
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FruitFlyEffect: MonoBehaviour {
    // Public Members  #########################################################
    public static FruitFlyEffect I {
        get; private set;
    }

    /// <summary>raised with the target each time a flight lands</summary>
    public event Action<FruitFlyTarget> Arrived;

    // Public Methods  #########################################################
    /// <summary>
    /// launches a sprite from <paramref name="worldPos"/> to
    /// <paramref name="target"/>; with no camera, no config, or the point
    /// behind the camera, it lands at once
    /// </summary>
    /// <param name="worldPos">launch point, eg the fruit's bounds centre
    /// </param>
    /// <param name="target">where the sprite flies to</param>
    /// <param name="sprite">sprite to fly, null for the default</param>
    /// <param name="onArrived">called once on landing, never if the run ends
    /// first</param>
    public void Play(
        Vector3 worldPos,
        FruitFlyTarget target,
        Sprite sprite,
        Action onArrived = null
    ) {
        Camera worldCamera = Camera.main;
        Vector3 startScreen = worldCamera != null
            ? worldCamera.WorldToScreenPoint(worldPos)
            : Vector3.zero;
        if (config == null || worldCamera == null || startScreen.z < 0f) {
            Land(target, onArrived);
            return;
        }

        Image image = TakeImage();
        Sprite shownSprite = sprite != null ? sprite : defaultSprite;
        image.sprite = shownSprite;
        image.enabled = shownSprite != null || showsBlankSprite;
        image.color = new Color(1f, 1f, 1f, config.Alpha);
        image.rectTransform.sizeDelta = Vector2.one * config.SizePx;

        Flight flight = new Flight {
            Image = image,
            StartScreen = startScreen,
            Target = target,
            OnArrived = onArrived,
        };
        flights.Add(flight);
        PlaceFlight(flight, 0f);
    }

    // Inspector Fields  #######################################################
    [Header("Tuning")]
    [SerializeField]
    [Tooltip("flight duration, look, and easing")]
    private FruitFlyConfig config;

    [Header("Targets")]
    [SerializeField]
    [Tooltip("bottom-left stamina bar, the banana's target")]
    private RectTransform energyBarTarget;

    [SerializeField]
    [Tooltip("icon of the top-right coconut HUD, the coconut's target")]
    private RectTransform topRightCounterTarget;

    [SerializeField]
    [Tooltip(
        "monkey, the pineapple's target; empty finds the MonkeyController "
            + "at Start"
    )]
    private Transform monkeyTarget;

    [Header("Look")]
    [SerializeField]
    [Tooltip("sprite flown when a fruit names none, the banana")]
    private Sprite defaultSprite;

    [SerializeField]
    [Tooltip("whether to fly a plain square when no sprite is set")]
    private bool showsBlankSprite = true;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "FruitFlyEffect:\tmust assign Inspector Field: config, "
                    + "flights land at once",
                this
            );
        }
        if (energyBarTarget == null) {
            Debug.LogWarning(
                "FruitFlyEffect:\tmust assign Inspector Field: "
                    + "energyBarTarget",
                this
            );
        }
        if (topRightCounterTarget == null) {
            Debug.LogWarning(
                "FruitFlyEffect:\tmust assign Inspector Field: "
                    + "topRightCounterTarget",
                this
            );
        }
        if (defaultSprite == null && Debug.isDebugBuild) {
            Debug.Log(
                "FruitFlyEffect:\tno defaultSprite, a fruit naming none flies "
                    + (showsBlankSprite ? "a blank square" : "hidden"),
                this
            );
        }

        // drop this duplicate component only, the GameObject may hold more
        if (I != null && I != this) {
            Debug.LogWarning(
                "FruitFlyEffect:\tduplicate instance, removing",
                this
            );
            Destroy(this);
            return;
        }

        I = this;
        rect = (RectTransform)transform;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) {
            Debug.LogError("FruitFlyEffect:\tfail to get parent: Canvas", this);
        } else {
            rootCanvas = canvas.rootCanvas;
        }

        if (Debug.isDebugBuild) {
            Debug.Log("FruitFlyEffect:\tready", this);
        }
    }

    private void OnEnable() {
        if (GameController.I != null) {
            GameController.I.StateChanged += OnStateChanged;
        }
    }

    // GameController wakes in its own Awake, which may run after OnEnable
    private void Start() {
        if (GameController.I != null) {
            GameController.I.StateChanged -= OnStateChanged;
            GameController.I.StateChanged += OnStateChanged;
        }

        // the monkey lives in another prefab, look it up over a scene link
        if (monkeyTarget == null) {
            MonkeyController monkey = FindAnyObjectByType<MonkeyController>();
            if (monkey != null) {
                monkeyTarget = monkey.transform;
            } else {
                Debug.LogWarning(
                    "FruitFlyEffect:\tfail to find MonkeyController, monkey "
                        + "flights land in place",
                    this
                );
            }
        }
    }

    private void OnDisable() {
        if (GameController.I != null) {
            GameController.I.StateChanged -= OnStateChanged;
        }
        ClearFlights();
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
    }

    private void Update() {
        if (config == null) {
            return;
        }

        // back to front, a landing removes its own entry
        for (int i = flights.Count - 1; i >= 0; i--) {
            Flight flight = flights[i];
            flight.ElapsedS += Time.deltaTime;
            float t = config.DurationS > 0f
                ? Mathf.Clamp01(flight.ElapsedS / config.DurationS)
                : 1f;
            PlaceFlight(flight, t);

            if (t >= 1f) {
                flights.RemoveAt(i);
                ReturnImage(flight.Image);
                Land(flight.Target, flight.OnArrived);
            }
        }
    }

    // Event Handlers  #########################################################
    // a flight cut by the run's end never restores into the next run
    private void OnStateChanged(GameState next) {
        if (next == GameState.Lost) {
            ClearFlights();
        }
    }

    // Private Types  ##########################################################
    private class Flight {
        public Image Image;
        public Vector2 StartScreen;
        public FruitFlyTarget Target;
        public Action OnArrived;
        public float ElapsedS;
    }

    // private members  ########################################################
    private readonly List<Flight> flights = new List<Flight>();
    private readonly Stack<Image> freeImages = new Stack<Image>();

    // cached references  ------------------------------------------------------
    private RectTransform rect;
    private Canvas rootCanvas;

    // overlay canvases take no camera for screen conversions
    private Camera CanvasCamera =>
        rootCanvas == null
        || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

    // private methods  ########################################################
    private void Land(FruitFlyTarget target, Action onArrived) {
        onArrived?.Invoke();
        Arrived?.Invoke(target);
    }

    // start and target meet in screen space, then map into this rect
    private void PlaceFlight(Flight flight, float t) {
        Vector2 targetScreen;
        if (!TryGetTargetScreen(flight.Target, out targetScreen)) {
            targetScreen = flight.StartScreen;
        }

        float eased = config.Ease != null ? config.Ease.Evaluate(t) : t;
        Vector2 screen = Vector2.LerpUnclamped(
            flight.StartScreen,
            targetScreen,
            eased
        );

        Vector2 local;
        if (
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                screen,
                CanvasCamera,
                out local
            )
        ) {
            flight.Image.rectTransform.localPosition = local;
        }
        flight.Image.rectTransform.localScale =
            Vector3.one * Mathf.Lerp(1f, config.EndScale, t);
    }

    // re-read every frame: the bar punches and the camera follows the monkey
    private bool TryGetTargetScreen(
        FruitFlyTarget target,
        out Vector2 screen
    ) {
        switch (target) {
            case FruitFlyTarget.EnergyBar:
                return TryGetRectScreen(energyBarTarget, out screen);
            case FruitFlyTarget.TopRightCounter:
                return TryGetRectScreen(topRightCounterTarget, out screen);
            case FruitFlyTarget.Monkey:
                Camera worldCamera = Camera.main;
                if (monkeyTarget != null && worldCamera != null) {
                    screen = worldCamera.WorldToScreenPoint(
                        monkeyTarget.position
                    );
                    return true;
                }
                break;
        }
        screen = Vector2.zero;
        return false;
    }

    private bool TryGetRectScreen(RectTransform target, out Vector2 screen) {
        if (target == null) {
            screen = Vector2.zero;
            return false;
        }
        Vector3 centre = target.TransformPoint(target.rect.center);
        screen = RectTransformUtility.WorldToScreenPoint(CanvasCamera, centre);
        return true;
    }

    private Image TakeImage() {
        Image image = freeImages.Count > 0 ? freeImages.Pop() : CreateImage();
        // drawn above earlier flights
        image.transform.SetAsLastSibling();
        return image;
    }

    private Image CreateImage() {
        GameObject go = new GameObject(
            "FruitFlySprite",
            typeof(RectTransform),
            typeof(Image)
        );
        RectTransform imageRect = (RectTransform)go.transform;
        imageRect.SetParent(rect, false);
        imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    // hide by the component, not the GameObject: OnDisable may run while the
    // hierarchy is deactivating, which forbids SetActive
    private void ReturnImage(Image image) {
        image.enabled = false;
        freeImages.Push(image);
    }

    // drop every flight unreported
    private void ClearFlights() {
        foreach (Flight flight in flights) {
            if (flight.Image != null) {
                ReturnImage(flight.Image);
            }
        }
        flights.Clear();
    }
}
