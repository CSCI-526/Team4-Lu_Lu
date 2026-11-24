using UnityEngine;


/// <summary>
/// The Cobra King fight's sound effects: a gong & hiss as it begins, a
/// hiss as each warned attack strikes, a clang when
/// the gold armor takes a hit, a watermelon bursting as a thrown fruit hits
/// the crown, the beaten king shattering, and a short victory call a moment
/// later.
/// <para>
/// Next to <see cref="CobraKingFightController"/> on the
/// <c>CobraKingFight</c> prefab. It only listens to the controller's
/// events; nothing calls it and it changes nothing. Plain 2D sound through
/// a few sources taken in turn; they follow
/// <see cref="AudioListener.pause"/>, never the time scale, so the
/// finisher's slow motion & the reward panel never bend or stop them.
/// </para>
/// <para>
/// The clips are made by <c>tools/sfx/make_sfx.py</c>.
/// </para>
/// </summary>
[RequireComponent(typeof(CobraKingFightController))]
public class CobraKingSfx: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Clips")]
    [SerializeField]
    [Tooltip("the fight begins: a gong & a long hiss")]
    private AudioClip entrance;

    [SerializeField]
    [Tooltip("as each bite, tail sweep & minion wave strikes, after its "
        + "warning")]
    private AudioClip attackWarn;

    [SerializeField]
    [Tooltip("the gold armor takes a fatal hit")]
    private AudioClip armorBlock;

    [SerializeField]
    [Tooltip("a thrown fruit smashing on the crown")]
    private AudioClip crownHit;

    [SerializeField]
    [Tooltip("the king breaking into cubes")]
    private AudioClip shatter;

    [SerializeField]
    [Tooltip("call after the king is beaten, victoryDelayS later")]
    private AudioClip victory;

    [Header("Tuning")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("every effect's loudness, beside the music's 0.5")]
    private float volume = 0.55f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("time from the shatter to the victory call; s")]
    private float victoryDelayS = 1f;

    [SerializeField]
    [Range(0f, 0.2f)]
    [Tooltip("random pitch change of each play, so repeats never sound alike")]
    private float pitchJitter = 0.04f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (entrance == null || attackWarn == null || armorBlock == null
            || crownHit == null || shatter == null || victory == null) {
            Debug.LogWarning(
                "CobraKingSfx:\tmust assign Inspector Fields: entrance, "
                    + "attackWarn, armorBlock, crownHit, shatter, victory",
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
        fight = GetComponent<CobraKingFightController>();
    }

    private void OnEnable() {
        if (fight == null) {
            return;
        }
        fight.FightStarted += OnFightStarted;
        fight.AttackStruck += OnAttackStruck;
        fight.ArmorAbsorbed += OnArmorAbsorbed;
        fight.JewelHit += OnJewelHit;
        fight.KingShattered += OnKingShattered;
    }

    private void OnDisable() {
        if (fight == null) {
            return;
        }
        fight.FightStarted -= OnFightStarted;
        fight.AttackStruck -= OnAttackStruck;
        fight.ArmorAbsorbed -= OnArmorAbsorbed;
        fight.JewelHit -= OnJewelHit;
        fight.KingShattered -= OnKingShattered;
    }

    // private members  ########################################################
    // sounds at once; the oldest is cut off by one more
    private const int VOICES = 5;

    private AudioSource[] sources;
    private int nextVoice;
    private CobraKingFightController fight;

    private void Play(AudioClip clip, float delayS = 0f) {
        if (sources == null || clip == null) {
            return;
        }
        AudioSource voice = sources[nextVoice];
        nextVoice = (nextVoice + 1) % VOICES;
        voice.clip = clip;
        voice.volume = volume;
        voice.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        // audio time, so a frozen or slowed game still keeps the delay
        voice.PlayDelayed(delayS);
    }

    // Event Handlers  #########################################################
    private void OnFightStarted() {
        Play(entrance);
    }

    private void OnAttackStruck() {
        Play(attackWarn);
    }

    private void OnArmorAbsorbed() {
        Play(armorBlock);
    }

    private void OnJewelHit() {
        Play(crownHit);
    }

    private void OnKingShattered() {
        Play(shatter);
        Play(victory, victoryDelayS);
    }
}
