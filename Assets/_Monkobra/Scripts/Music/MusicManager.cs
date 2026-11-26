using UnityEngine;


/// <summary>
/// Plays the background music for the whole launch: the climb loop from the
/// first scene on, crossfading to the boss loop while a boss fight is on and
/// back once it is over.
/// <para>
/// A component of the <see cref="GameController"/> prefab, so it lives across
/// scene loads and a restart never cuts the music. It only watches the
/// fights' public state, nothing calls it.
/// </para>
/// <para>
/// The boss loop starts from its top each fight, while the climb loop is
/// paused under it and picks up where it left off. Both follow
/// <see cref="AudioListener.pause"/>, so the pause screen silences them like
/// every other sound.
/// </para>
/// </summary>
[DefaultExecutionOrder(-990)]
[RequireComponent(typeof(GameController))]
public class MusicManager: MonoBehaviour {
    // Public Members  #########################################################
    public static MusicManager I {
        get; private set;
    }

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("seamless loop played all the time outside a boss fight")]
    private AudioClip climbLoop;

    [SerializeField]
    [Tooltip("seamless loop played while a boss fight is on")]
    private AudioClip bossLoop;

    [Header("Tuning")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("music loudness, under the sound effects")]
    private float volume = 0.5f;

    [SerializeField]
    [Min(0.05f)]
    [Tooltip("time to fade from one loop to the other; s")]
    private float crossfadeS = 1.2f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // GameController destroys a duplicate before this runs; kept as a guard
        if (I != null && I != this) {
            return;
        }

        I = this;

        // Inspector Assignment Guard  -----------------------------------------
        if (climbLoop == null || bossLoop == null) {
            Debug.LogWarning(
                "MusicManager:\tmust assign Inspector Fields: climbLoop, "
                    + "bossLoop",
                this
            );
            enabled = false;
            return;
        }

        climbSource = MakeSource("ClimbLoop", climbLoop);
        bossSource = MakeSource("BossLoop", bossLoop);
        climbSource.volume = volume;
        climbSource.Play();
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
    }

    private void Update() {
        bool isBoss = IsBossFightOn();
        if (isBoss && !isBossPlaying) {
            bossSource.time = 0f;
            bossSource.Play();
            isBossPlaying = true;
        }

        // unscaled: a fight freezes time for its panels, the fade goes on
        bossMix = Mathf.MoveTowards(
            bossMix,
            isBoss ? 1f : 0f,
            Time.unscaledDeltaTime / crossfadeS
        );

        // equal power, so the loudness holds steady through the crossfade
        climbSource.volume = volume * Mathf.Cos(bossMix * Mathf.PI * 0.5f);
        bossSource.volume = volume * Mathf.Sin(bossMix * Mathf.PI * 0.5f);

        // a silent loop is put to rest: the climb loop waits where it was,
        // the boss loop stops, to start from its top next fight
        if (bossMix >= 1f && !isClimbPaused) {
            climbSource.Pause();
            isClimbPaused = true;
        } else if (bossMix < 1f && isClimbPaused) {
            climbSource.UnPause();
            isClimbPaused = false;
        }
        if (bossMix <= 0f && isBossPlaying) {
            bossSource.Stop();
            isBossPlaying = false;
        }
    }

    // private members  ########################################################
    private AudioSource climbSource;
    private AudioSource bossSource;

    // 0 all climb loop, 1 all boss loop
    private float bossMix;

    private bool isBossPlaying;
    private bool isClimbPaused;

    private AudioSource MakeSource(string sourceName, AudioClip clip) {
        GameObject holder = new GameObject(sourceName);
        holder.transform.SetParent(transform, false);
        AudioSource source = holder.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        // plain stereo, never placed in the world
        source.spatialBlend = 0f;
        source.volume = 0f;
        // music is the last voice to be dropped when too many sounds play
        source.priority = 0;
        return source;
    }

    // a fight is on from its start till its reward is taken, the Cobra King's
    // or the Spider King's; a finished run plays the climb loop again
    private static bool IsBossFightOn() {
        RunController game = RunController.I;
        if (game != null
            && (game.State == GameState.Lost || game.State == GameState.Won)) {
            return false;
        }

        CobraKingFightController cobraKing = CobraKingFightController.I;
        if (cobraKing != null && cobraKing.IsInArena && !cobraKing.IsDone) {
            return true;
        }
        SpiderKingFightController spiderKing = SpiderKingFightController.I;
        return spiderKing != null && spiderKing.IsInArena;
    }
}
