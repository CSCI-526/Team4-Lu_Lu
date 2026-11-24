using UnityEngine;


/// <summary>
/// The monkey's sound effects: grabbing a banana, a coconut or a pineapple,
/// a missed grab, the pineapple leap's rush, throwing a coconut,
/// the cobra freezing & thawing, each tug against a web, reaching a
/// height marker, running low on stamina, a hit by a branch or a hazard,
/// and the cobra's bite that ends the run; now & then, after any fruit, the
/// monkey's happy call.
/// <para>
/// On the <c>_Monkey</c> prefab root. It only listens: to the
/// <see cref="ArmController"/> grab events, the monkey's
/// <see cref="PineappleLeapController"/> & <see cref="SpiderWebStruggle"/>,
/// <see cref="CoconutManager"/>, <see cref="HeightMarkerSystem"/>,
/// <see cref="StaminaBarController"/>, <see cref="HitBranchSensor.Hit"/> and
/// <see cref="GameController.StateChanged"/>; nothing calls it and it
/// changes nothing. Plain 2D sound through a few sources taken in turn, so
/// each play keeps its own pitch; they follow
/// <see cref="AudioListener.pause"/>, so the pause screen silences them.
/// </para>
/// <para>
/// The clips are made by <c>tools/sfx/make_sfx.py</c>.
/// </para>
/// </summary>
public class MonkeySfx: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Grabs")]
    [SerializeField]
    private AudioClip bananaGrab;

    [SerializeField]
    private AudioClip coconutGrab;

    [SerializeField]
    private AudioClip pineappleGrab;

    [SerializeField]
    private AudioClip grabMiss;

    [Header("Pineapple Leap")]
    [SerializeField]
    [Tooltip("wind rising as the monkey is pulled up")]
    private AudioClip leapRush;

    [Header("Coconut")]
    [SerializeField]
    private AudioClip coconutThrow;

    [SerializeField]
    [Tooltip("the coconut hitting the cobra, which freezes")]
    private AudioClip cobraFreeze;

    [SerializeField]
    private AudioClip cobraThaw;

    [Header("Web")]
    [SerializeField]
    [Tooltip("each escape press while a web holds the monkey, a few takes "
        + "cycled "
        + "through; nothing as it is caught or breaks free")]
    private AudioClip[] webStruggles;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("how much higher a tug sounds as escape nears, 0.3 = 30% at the "
        + "end")]
    private float struggleRisePitch = 0.3f;

    [Header("Happy Calls")]
    [SerializeField]
    [Tooltip("the monkey's happy calls, one picked at random now & then after "
        + "a fruit, any kind, boss fights' too")]
    private AudioClip[] happyCalls;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("chance a fruit brings a happy call")]
    private float happyChance = 0.35f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("the call comes this long after the grab's own sound; s")]
    private float happyDelayS = 0.12f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("the least time between 2 happy calls, so a fruit streak never "
        + "chatters; s")]
    private float happyCooldownS = 4f;

    [Header("Progress & Danger")]
    [SerializeField]
    private AudioClip markerReached;

    [SerializeField]
    [Tooltip("once each time stamina runs low & the player tries to move")]
    private AudioClip staminaLow;

    [SerializeField]
    [Tooltip("a branch or a hazard striking the monkey")]
    private AudioClip hit;

    [SerializeField]
    [Tooltip("the cobra's bite, the run lost")]
    private AudioClip lose;

    [Header("Tuning")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("every effect's loudness, beside the music's 0.5")]
    private float volume = 0.55f;

    [SerializeField]
    [Range(0f, 0.2f)]
    [Tooltip("random pitch change of each play, so repeats never sound alike")]
    private float pitchJitter = 0.05f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (bananaGrab == null || coconutGrab == null || pineappleGrab == null
            || grabMiss == null || leapRush == null
            || coconutThrow == null || cobraFreeze == null || cobraThaw == null
            || webStruggles == null || webStruggles.Length == 0
            || happyCalls == null
                || happyCalls.Length == 0
                || markerReached == null
            || staminaLow == null || hit == null || lose == null) {
            Debug.LogWarning(
                "MonkeySfx:\tmust assign Inspector Fields: bananaGrab, "
                    + "coconutGrab, pineappleGrab, grabMiss, leapRush, "
                    + "coconutThrow, cobraFreeze, cobraThaw, webStruggles, "
                    + "happyCalls, markerReached, staminaLow, hit, lose",
                this
            );
            enabled = false;
            return;
        }

        sources = new AudioSource[VOICES];
        for (int i = 0; i < VOICES; i++) {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
            // plain stereo, never placed in the world
            sources[i].spatialBlend = 0f;
        }
        leap = GetComponentInChildren<PineappleLeapController>(true);
        web = GetComponentInChildren<SpiderWebStruggle>(true);
    }

    private void OnEnable() {
        ArmController.FruitGrabbed += OnFruitGrabbed;
        ArmController.GrabMissed += OnGrabMissed;
        if (leap != null) {
            leap.Begun += OnLeapBegun;
        }
        if (web != null) {
            web.EscapePressed += OnEscapePressed;
        }
    }

    // the singletons are set in their Awake, all done by now
    private void Start() {
        sensor = HitBranchSensor.I;
        if (sensor != null) {
            sensor.Hit += OnHit;
        }
        game = GameController.I;
        if (game != null) {
            game.StateChanged += OnStateChanged;
        }
        coconuts = CoconutManager.I;
        if (coconuts != null) {
            coconuts.Thrown += OnCoconutThrown;
            coconuts.Frozen += OnCobraFrozen;
            coconuts.Thawed += OnCobraThawed;
        }
        markers = FindFirstObjectByType<HeightMarkerSystem>();
        if (markers != null) {
            markers.MarkerReached += OnMarkerReached;
        }
        stamina = StaminaBarController.I;
        if (stamina != null) {
            stamina.LowStaminaMoveAttempted += OnLowStaminaMoveAttempted;
        }
    }

    // the low stamina sound plays again only once stamina has recovered
    private void Update() {
        if (hasWarnedLow && stamina != null && !stamina.IsLowStamina) {
            hasWarnedLow = false;
        }
    }

    private void OnDisable() {
        ArmController.FruitGrabbed -= OnFruitGrabbed;
        ArmController.GrabMissed -= OnGrabMissed;
        if (leap != null) {
            leap.Begun -= OnLeapBegun;
        }
        if (web != null) {
            web.EscapePressed -= OnEscapePressed;
        }
    }

    private void OnDestroy() {
        if (sensor != null) {
            sensor.Hit -= OnHit;
        }
        if (game != null) {
            game.StateChanged -= OnStateChanged;
        }
        if (coconuts != null) {
            coconuts.Thrown -= OnCoconutThrown;
            coconuts.Frozen -= OnCobraFrozen;
            coconuts.Thawed -= OnCobraThawed;
        }
        if (markers != null) {
            markers.MarkerReached -= OnMarkerReached;
        }
        if (stamina != null) {
            stamina.LowStaminaMoveAttempted -= OnLowStaminaMoveAttempted;
        }
    }

    // private members  ########################################################
    // sounds at once; the oldest is cut off by one more
    private const int VOICES = 6;

    private AudioSource[] sources;
    private int nextVoice;
    private PineappleLeapController leap;
    private SpiderWebStruggle web;
    private HitBranchSensor sensor;
    private GameController game;
    private CoconutManager coconuts;
    private HeightMarkerSystem markers;
    private StaminaBarController stamina;
    private bool hasWarnedLow;
    private int nextStruggle;
    private int lastHappy = -1;
    private float lastHappyTime = float.NegativeInfinity;

    private void Play(
        AudioClip clip, float share = 1f, float pitch = 1f, float delayS = 0f
    ) {
        if (sources == null || clip == null) {
            return;
        }
        AudioSource voice = sources[nextVoice];
        nextVoice = (nextVoice + 1) % VOICES;
        voice.clip = clip;
        voice.volume = volume * share;
        voice.pitch = pitch * (1f + Random.Range(-pitchJitter, pitchJitter));
        voice.PlayDelayed(delayS);
    }

    // now & then a happy call, never the same one twice running
    private void MaybeCheer() {
        if (
            Time.time - lastHappyTime < happyCooldownS
                || Random.value >= happyChance
        ) {
            return;
        }
        int pick = Random.Range(0, happyCalls.Length);
        if (pick == lastHappy && happyCalls.Length > 1) {
            pick = (
                pick + 1 + Random.Range(0, happyCalls.Length - 1)
            ) % happyCalls.Length;
        }
        lastHappy = pick;
        lastHappyTime = Time.time;
        Play(happyCalls[pick], 0.8f, 1f, happyDelayS);
    }

    // Event Handlers  #########################################################
    private void OnFruitGrabbed(Collider fruit) {
        if (fruit == null) {
            return;
        }
        if (fruit.GetComponentInParent<LeapOnGrab>(true) != null) {
            Play(pineappleGrab);
        } else {
            ScorePickup pickup = fruit.GetComponentInParent<ScorePickup>(true);
            bool isCoconut = pickup != null
                && pickup.Kind == PickupKind.Coconut;
            Play(isCoconut ? coconutGrab : bananaGrab);
        }
        MaybeCheer();
    }

    private void OnGrabMissed() {
        Play(grabMiss, 0.8f);
    }

    private void OnLeapBegun() {
        Play(leapRush);
    }

    private void OnCoconutThrown(Transform coconut) {
        Play(coconutThrow);
    }

    private void OnCobraFrozen(float durationS) {
        Play(cobraFreeze);
    }

    private void OnCobraThawed() {
        Play(cobraThaw);
    }

    // a tug sounds higher the nearer the monkey is to breaking free
    private void OnEscapePressed(float progress) {
        Play(
            webStruggles[nextStruggle], 0.8f, 1f + struggleRisePitch * progress
        );
        nextStruggle = (nextStruggle + 1) % webStruggles.Length;
    }

    private void OnMarkerReached(int heightU) {
        Play(markerReached, 0.8f);
    }

    private void OnLowStaminaMoveAttempted() {
        if (hasWarnedLow) {
            return;
        }
        hasWarnedLow = true;
        Play(staminaLow, 0.8f);
    }

    private void OnHit(Collider hazard, HitBranchSensor.HitKind kind) {
        Play(hit);
    }

    private void OnStateChanged(GameState state) {
        if (state == GameState.Lost) {
            Play(lose);
        }
    }
}
