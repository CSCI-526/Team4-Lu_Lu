using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Green screen-edge warning while the great cobra is close below the
/// monkey, in 2 stages by the height gap: stage 1 tints the bottom half of the
/// screen, stage 2 the full ring. It beats, and each stage beats at its own
/// rate, a step up per stage and never a ramp. Never shows in the starting
/// zone, nor while the cobra is frozen or guarding, nor during a leap, nor
/// out of a live run.
/// <para>
/// Shares the screen edge with the black stamina pulse of
/// <see cref="CameraCoolDownFeedback"/> through
/// <see cref="ScreenVignetteArbiter"/>: a beat starts only with the slot, and
/// a refused beat waits, so the two vignettes alternate. All values are in
/// <see cref="CameraConfig"/>. The URP Vignette is black and round only, so
/// this is a UI overlay with its 2 gradient sprites made at startup; put it
/// on a prefab nested under the HUD canvas, stretched over the screen.
/// </para>
/// </summary>
[RequireComponent(typeof(Image))]
public class CobraProximityVignette: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("tuning for both stages, the beat rates and the look")]
    private CameraConfig config;

    [SerializeField]
    [Tooltip("full-screen overlay drawn by this widget, default: own Image")]
    private Image overlay;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "CobraProximityVignette:\tmust assign Inspector Field: config",
                this
            );
            enabled = false;
            return;
        }

        if (overlay == null) {
            overlay = GetComponent<Image>();
        }
        overlay.raycastTarget = false;
        overlay.type = Image.Type.Simple;
        overlay.preserveAspect = false;
        MakeSprites();
        SetAlpha(0f);
    }

    private void OnDisable() {
        EndBeat();
        stage = 0;
        if (overlay != null && config != null) {
            SetAlpha(0f);
        }
    }

    private void OnDestroy() {
        if (bottomSprite != null) {
            Destroy(bottomSprite);
        }
        if (ringSprite != null) {
            Destroy(ringSprite);
        }
        if (bottomTexture != null) {
            Destroy(bottomTexture);
        }
        if (ringTexture != null) {
            Destroy(ringTexture);
        }
    }

    private void Update() {
        float gapU;
        if (!TryReadGap(out gapU)) {
            stage = 0;
            EndBeat();
            SetAlpha(0f);
            return;
        }

        stage = ResolveStage(gapU);
        if (stage == 0) {
            EndBeat();
            SetAlpha(0f);
            return;
        }

        overlay.sprite = stage >= 2 ? ringSprite : bottomSprite;
        float periodS = 1f / (stage >= 2
            ? config.CobraStage2PulseHz
            : config.CobraStage1PulseHz);

        if (!isBeating) {
            SetAlpha(0f);
            if (Time.unscaledTime >= nextBeatS) {
                TryBeginBeat(periodS);
            }
            return;
        }

        // a nearer stage speeds the next beat up at once
        nextBeatS = Mathf.Min(nextBeatS, beatStartS + periodS);
        beatElapsedS = Time.unscaledTime - beatStartS;
        float easeInS = config.CobraVignetteEaseInS;
        float easeOutS = config.CobraVignetteEaseOutS;
        float totalS = easeInS + easeOutS;

        // a beat never outlasts its period, so the beats stay apart
        float squeeze = totalS > periodS ? periodS / totalS : 1f;
        easeInS *= squeeze;
        easeOutS *= squeeze;

        if (beatElapsedS >= easeInS + easeOutS) {
            EndBeat();
            SetAlpha(0f);
            return;
        }
        SetAlpha(config.CobraVignettePeakAlpha
            * CalcWeight(beatElapsedS, easeInS, easeOutS));
    }

    // constants  ##############################################################
    private const string PLAYER_TAG = "Player";
    private const int SPRITE_SIZE = 256;

    // inner radius of the dark edge, 0 centre, 1 mid-edge of the screen
    private const float RING_INNER = 0.55f;
    private const float RING_OUTER = 1f;

    // share of the screen height, from the bottom, kept solid & then faded
    private const float BOTTOM_SOLID = 0.3f;
    private const float BOTTOM_FADE_END = 0.6f;

    // private members  ########################################################
    // 0 none, 1 bottom half, 2 full ring
    private int stage;
    private bool isBeating;
    private float beatStartS;
    private float beatElapsedS;
    private float nextBeatS;

    // cached references  ------------------------------------------------------
    private Transform player;
    private Texture2D bottomTexture;
    private Texture2D ringTexture;
    private Sprite bottomSprite;
    private Sprite ringSprite;

    // private methods  ########################################################
    // reads the height gap below the monkey while the warning may show; the
    // cobra's top is its raised head, as on the progress bar
    private bool TryReadGap(out float gapU) {
        gapU = 0f;

        GameController game = GameController.I;
        if (
            game == null
            || game.State != GameState.Playing
            || game.IsLeaping
            || Time.timeScale <= 0f
        ) {
            return false;
        }

        CoconutManager coconuts = CoconutManager.I;
        CobraMotor cobra = coconuts != null ? coconuts.Cobra : null;
        if (
            cobra == null
            || cobra.IsFrozen
            || cobra.IsGuarding
            || !cobra.TryGetBodyBounds(out Bounds bodyBounds)
        ) {
            return false;
        }

        if (player == null) {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag(PLAYER_TAG);
            if (playerObject == null) {
                return false;
            }
            player = playerObject.transform;
        }

        gapU = Mathf.Max(0f, player.position.y - bodyBounds.max.y);
        return true;
    }

    // a stage is entered at its distance and kept a little longer on the way
    // out, so a gap hovering on a boundary does not flicker
    private int ResolveStage(float gapU) {
        float stage1U = config.CobraStage1DistanceU;
        float stage2U = config.CobraStage2DistanceU;
        float holdU = config.CobraStageHysteresisU;

        int next = stage;
        if (gapU <= stage2U) {
            next = 2;
        } else if (gapU <= stage1U && next < 1) {
            next = 1;
        }

        if (next == 2 && gapU > stage2U + holdU) {
            next = 1;
        }
        if (next == 1 && gapU > stage1U + holdU) {
            next = 0;
        }
        return next;
    }

    // a beat starts only with the shared slot; a refusal is retried next
    // frame, which also tells the arbiter this vignette waits
    private void TryBeginBeat(float periodS) {
        ScreenVignetteArbiter arbiter = ScreenVignetteArbiter.I;
        if (arbiter != null && !arbiter.TryAcquire(VignetteOwner.Cobra)) {
            return;
        }

        isBeating = true;
        beatStartS = Time.unscaledTime;
        beatElapsedS = 0f;
        nextBeatS = beatStartS + periodS;
    }

    private void EndBeat() {
        if (!isBeating) {
            return;
        }
        isBeating = false;

        if (ScreenVignetteArbiter.I != null) {
            ScreenVignetteArbiter.I.Release(VignetteOwner.Cobra);
        }
    }

    // 0 at rest, 1 at the peak; smoothstep eases both slopes
    private static float CalcWeight(
        float timeS,
        float easeInS,
        float easeOutS
    ) {
        float t = timeS < easeInS
            ? (easeInS > 0f ? timeS / easeInS : 1f)
            : 1f - (easeOutS > 0f ? (timeS - easeInS) / easeOutS : 1f);
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }

    private void SetAlpha(float alpha) {
        Color color = config.CobraVignetteColor;
        color.a = alpha;
        overlay.color = color;
        overlay.enabled = alpha > 0.001f;
    }

    // 2 white gradient sprites, tinted by the Image: a ring dark at the edge,
    // and the same ring kept only along the bottom of the screen
    private void MakeSprites() {
        bottomTexture = MakeTexture(true);
        ringTexture = MakeTexture(false);
        bottomSprite = MakeSprite(bottomTexture);
        ringSprite = MakeSprite(ringTexture);
    }

    private static Texture2D MakeTexture(bool isBottomOnly) {
        Color32[] pixels = new Color32[SPRITE_SIZE * SPRITE_SIZE];
        float last = SPRITE_SIZE - 1;

        for (int y = 0; y < SPRITE_SIZE; y++) {
            float v = y / last;
            float dy = (v - 0.5f) * 2f;
            for (int x = 0; x < SPRITE_SIZE; x++) {
                float dx = (x / last - 0.5f) * 2f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(RING_INNER, RING_OUTER, radius)
                );
                if (isBottomOnly) {
                    alpha *= 1f - Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(BOTTOM_SOLID, BOTTOM_FADE_END, v)
                    );
                }
                pixels[y * SPRITE_SIZE + x] = new Color32(
                    255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f)
                );
            }
        }

        Texture2D texture = new Texture2D(
            SPRITE_SIZE, SPRITE_SIZE, TextureFormat.RGBA32, false
        );
        texture.name = isBottomOnly
            ? "Monkobra Cobra Vignette Bottom"
            : "Monkobra Cobra Vignette Ring";
        texture.hideFlags = HideFlags.DontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Sprite MakeSprite(Texture2D texture) {
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, SPRITE_SIZE, SPRITE_SIZE),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
