using UnityEngine;


/// <summary>
/// The Spider King fight's sound effects, as the Cobra King's: a gong &
/// hiss as it begins, a hiss as each attack strikes (a web bullet fired, a
/// slash flying, a dash going; never at its charge), a watermelon bursting
/// as the spider is hurt, the beaten spider shattering, and a short victory
/// call a moment later. The gold armor's clang is the Cobra King's, as the
/// armor is his.
/// <para>
/// Next to <see cref="SpiderKingFightController"/> on the
/// <c>SpiderKingFight</c> prefab. It only listens to the controller's
/// events; nothing calls it and it changes nothing. Plain 2D sound through
/// a few sources taken in turn; they follow
/// <see cref="AudioListener.pause"/>, never the time scale, so the slow
/// motions & the reward panel never bend or stop them.
/// </para>
/// <para>
/// The clips are the Cobra King's, made by <c>tools/sfx/make_sfx.py</c>.
/// </para>
/// </summary>
[RequireComponent(typeof(SpiderKingFightController))]
public class SpiderKingSfx: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Clips")]
    [SerializeField]
    [Tooltip("the fight begins: a gong & a long hiss")]
    private AudioClip entrance;

    [SerializeField]
    [Tooltip("as each web bullet, slash & dash strikes, after its charge")]
    private AudioClip attack;

    [SerializeField]
    [Tooltip("the spider hurt by a melon or a parried slash")]
    private AudioClip spiderHit;

    [SerializeField]
    private AudioClip shatter;

    [SerializeField]
    private AudioClip victory;

    [Header("Tuning")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("every effect's loudness, beside the music's 0.5")]
    private float volume = 0.55f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("the attack hiss's loudness share, as a volley fires often")]
    private float attackVolumeShare = 0.7f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("time from the shatter to the victory call; s")]
    private float victoryDelayS = 1f;

    [SerializeField]
    [Range(0f, 0.2f)]
    [Tooltip("random pitch change of each play, so repeats never sound alike")]
    private float pitchJitter = 0.06f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (entrance == null || attack == null || spiderHit == null
            || shatter == null || victory == null) {
            Debug.LogWarning(
                "SpiderKingSfx:\tmust assign Inspector Fields: entrance, "
                    + "attack, spiderHit, shatter, victory",
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
        fight = GetComponent<SpiderKingFightController>();
    }

    private void OnEnable() {
        if (fight == null) {
            return;
        }
        fight.FightStarted += OnFightStarted;
        fight.AttackStruck += OnAttackStruck;
        fight.SpiderHit += OnSpiderHit;
        fight.Defeated += OnDefeated;
    }

    private void OnDisable() {
        if (fight == null) {
            return;
        }
        fight.FightStarted -= OnFightStarted;
        fight.AttackStruck -= OnAttackStruck;
        fight.SpiderHit -= OnSpiderHit;
        fight.Defeated -= OnDefeated;
    }

    // Private Members  ########################################################
    // sounds at once; the oldest is cut off by one more
    private const int VOICES = 5;

    private AudioSource[] sources;
    private int nextVoice;
    private SpiderKingFightController fight;

    private void Play(AudioClip clip, float share = 1f, float delayS = 0f) {
        if (sources == null || clip == null) {
            return;
        }
        AudioSource voice = sources[nextVoice];
        nextVoice = (nextVoice + 1) % VOICES;
        voice.clip = clip;
        voice.volume = volume * share;
        voice.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        // audio time, so a frozen or slowed game still keeps the delay
        voice.PlayDelayed(delayS);
    }

    // Event Handlers  #########################################################
    private void OnFightStarted() {
        Play(entrance);
    }

    private void OnAttackStruck() {
        Play(attack, attackVolumeShare);
    }

    private void OnSpiderHit() {
        Play(spiderHit);
    }

    private void OnDefeated() {
        Play(shatter);
        Play(victory, 1f, victoryDelayS);
    }
}
