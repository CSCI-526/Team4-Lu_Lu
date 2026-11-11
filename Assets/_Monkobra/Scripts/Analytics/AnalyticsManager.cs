using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The data collection half of analytics: observes the game, stamps every
/// event, samples height and stamina, and opens and closes the runs of a
/// session. What it collects is <see cref="SessionData"/>; how it looks is
/// <see cref="AnalyticsMarkdownWriter"/>, where it is saved is
/// <see cref="AnalyticsSessionFile"/>.
/// <para>
/// A session is one game launch, and every run adds one entry. Scene restarts
/// keep the same session, and the <see cref="HackAction.RestartSession"/> key
/// forces a fresh file mid-launch.
/// </para>
/// <para>
/// Attach to a root GameObject of the first scene. It is kept across scene
/// reloads, and the copy each reload brings in destroys itself.
/// </para>
/// </summary>
public class AnalyticsManager: MonoBehaviour {
    // Public Members  #########################################################
    public static AnalyticsManager I {
        get; private set;
    }

    /// <summary>
    /// full path of the current session file, empty until one opens
    /// </summary>
    public string SessionFilePath {
        get {
            return file.FilePath;
        }
    }

    /// <summary>
    /// runs recorded in the current session
    /// </summary>
    public int RunCount {
        get {
            return session.Runs.Count;
        }
    }

    // Public Methods  #########################################################
    /// <summary>
    /// closes the current session and opens a new file, run numbering restarts
    /// </summary>
    public void StartNewSession() {
        // a run still open belongs to the session being closed
        CloseRun(RunStatus.Interrupted);
        DateTime now = DateTime.Now;
        session = new SessionData {
            StartClock = now,
            Title = now.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            ),
            AppVersion = Application.version
        };
        currentRun = null;

        if (!file.Open(now)) {
            return;
        }
        if (!SaveSession()) {
            file.Close();
            return;
        }

        if (Debug.isDebugBuild) {
            Debug.Log("Analytics:\tsession opened " + SessionFilePath);
        }
    }

    /// <summary>
    /// saves a mock session file beside the real ones, the live session is
    /// untouched
    /// </summary>
    public void SaveMockSession() {
        string path = MockAnalyticsGenerator.SaveMockSession(
            AnalyticsSessionFile.FolderPath,
            DateTime.Now,
            config.DeathCauseWindowS
        );
        if (string.IsNullOrEmpty(path)) {
            Debug.LogWarning("Analytics:\tfail to save mock session", this);
        } else if (Debug.isDebugBuild) {
            Debug.Log("Analytics:\tmock session saved " + path, this);
        }
    }

    /// <summary>
    /// hands the session report to the browser as a download; elsewhere it
    /// saves the session file instead. A run still open is rendered as it
    /// stands and keeps recording.
    /// </summary>
    public void DownloadReport() {
        if (file.TryDownload(session)) {
            return;
        }

        if (SaveSession()) {
            if (Debug.isDebugBuild) {
                Debug.Log("Analytics:\treport saved " + SessionFilePath, this);
            }
        } else {
            Debug.LogWarning("Analytics:\tfail to save report", this);
        }
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("shared analytics tuning")]
    private AnalyticsConfig config;

    [SerializeField]
    [Tooltip("dev/hack keys, they only work in a development build")]
    private HackShortcutConfig hackShortcuts;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "Analytics:\tmust assign Inspector Field: config, "
                + "using defaults", this);
        }
        if (hackShortcuts == null) {
            Debug.LogWarning(
                "Analytics:\tmust assign Inspector Field: hackShortcuts",
                this);
        }

        if (I != null && I != this) {
            // a scene may keep its own copy, the first instance wins; the
            // warning is expected and harmless
            Debug.LogWarning("Analytics:\tduplicate instance destroyed", this);
            Destroy(gameObject);
            return;
        }

        I = this;
        // one session spans the whole launch, across every scene load
        DontDestroyOnLoad(gameObject);
        if (config == null) {
            config = ScriptableObject.CreateInstance<AnalyticsConfig>();
        }
        hackKeys = new AnalyticsHackKeys(
            hackShortcuts,
            OnRestartSessionKey,
            DownloadReport,
            SaveMockSession
        );
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneNames.Loading += OnSceneLoading;
        // static event, webs spawn at runtime so none can be looked up here
        SpiderWebTrap.Encountered += OnWebEncountered;
        StartNewSession();
    }

    // the first scene is observed once every Awake ran, as this object is no
    // longer on the GameController, so the singletons are not set in Awake
    private void Start() {
        if (I != this) {
            return;
        }

        ObserveScene(SceneManager.GetActiveScene());
        // a run already live when observing began, eg a dev scene
        if (observedGame != null && observedGame.IsRunLive
            && currentRun == null) {
            OpenRun(observedGame.IsInStartingZone);
        }
    }

    private void Update() {
        if (
            currentRun != null
            && observedGame != null
            && observedGame.IsRunLive
        ) {
            if (Time.time >= nextSampleTime) {
                AddSample();
                nextSampleTime = Time.time + config.SampleIntervalS;
            }
            if (
                config.AutosaveIntervalS > 0f
                && Time.time >= nextAutosaveTime
            ) {
                SaveSession();
            }
        }

        // a duplicate instance destroys itself in Awake and has no keys
        if (hackKeys != null) {
            hackKeys.Poll();
        }
    }

    // covers Play mode stop in the Editor
    private void OnApplicationQuit() {
        CloseRun(RunStatus.Interrupted);
    }

    private void OnApplicationPause(bool isPaused) {
        if (isPaused) {
            CloseRun(RunStatus.Interrupted);
        }
    }

    private void OnDestroy() {
        if (I != this) {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneNames.Loading -= OnSceneLoading;
        SpiderWebTrap.Encountered -= OnWebEncountered;
        Observe(null, null, null, null, null, null);
        I = null;
    }

    // Event Handlers  #########################################################
    private void OnRestartSessionKey() {
        Debug.LogWarning(
            "Analytics:\trestart session key pressed, closing session "
                + SessionFilePath,
            this
        );
        StartNewSession();
    }

    // a run cut by a load ends here, while its scene still holds the score
    private void OnSceneLoading() {
        CloseRun(RunStatus.Interrupted);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        // a load that skipped SceneNames.Load still closes the open run
        CloseRun(RunStatus.Interrupted);
        // each load builds fresh instances of every observed subject; a menu
        // scene has none, so nothing is observed there
        ObserveScene(scene);
    }

    private void OnGameStateChanged(GameState state) {
        if (state == GameState.StartingZone) {
            // the run goes live here: the zone's fruit, webs & height count
            OpenRun(true);
        } else if (state == GameState.Playing) {
            if (currentRun == null) {
                // no starting zone, e.g. a dev scene that starts playing
                OpenRun(false);
            } else if (currentRun.StartedInZone && !currentRun.HasZoneExit) {
                currentRun.ZoneExit = CreateStamp();
                currentRun.HasZoneExit = true;
                SaveSession();
            }
        } else if (state == GameState.Lost) {
            CloseRun(RunStatus.Lost);
        } else if (state == GameState.Won) {
            CloseRun(RunStatus.Won);
        }
    }

    // one line per source: the metric channel does the stamping & saving
    private void OnBananaCollected() {
        RecordEvent(AnalyticsMetric.Banana);
    }

    private void OnCoconutCollected() {
        RecordEvent(AnalyticsMetric.Coconut);
    }

    private void OnCoconutUsed() {
        RecordEvent(AnalyticsMetric.CoconutUsed);
    }

    private void OnWebEncountered(SpiderWebTrap _) {
        RecordEvent(AnalyticsMetric.Web);
    }

    private void OnBranchHit(Collider _) {
        RecordEvent(AnalyticsMetric.Branch);
    }

    // a leap counts at its start, not its end
    private void OnLeapChanged(bool isLeaping) {
        if (isLeaping) {
            RecordEvent(AnalyticsMetric.PineappleLeap);
        }
    }

    private void OnRoughBarkTouched() {
        RecordEvent(AnalyticsMetric.RoughBark);
    }

    // freezes from a coconut & from the gold armor both arrive here
    private void OnCobraFrozen(float _) {
        RecordEvent(AnalyticsMetric.CobraFreeze);
    }

    // a pause counts, a resume does not
    private void OnPauseChanged(bool isPaused) {
        if (isPaused) {
            RecordEvent(AnalyticsMetric.Pause);
        }
    }

    private void OnBossFightStarted() {
        RecordEvent(AnalyticsMetric.BossFight);
    }

    private void OnBossArmorAbsorbed() {
        RecordEvent(AnalyticsMetric.BossArmorSaved);
    }

    private void OnBossRevived() {
        RecordEvent(AnalyticsMetric.BossRevive);
    }

    // private members  ########################################################
    private readonly AnalyticsSessionFile file = new AnalyticsSessionFile();
    private AnalyticsHackKeys hackKeys;
    private SessionData session = new SessionData();
    // run being played, null b/t a game over and the next start
    private RunData currentRun;
    private float nextSampleTime;
    private float nextAutosaveTime;

    // cached references  ------------------------------------------------------
    private GameController observedGame;
    private ScoreManager observedScore;
    private HitBranchSensor observedBranch;
    private CoconutManager observedCoconuts;
    private MonkeyController observedMonkey;
    private BossFightController observedBoss;

    // private methods  ########################################################
    // observes the subjects of <scene> only: right after a load the old
    // scene's objects still exist
    private void ObserveScene(Scene scene) {
        Observe(
            InScene(GameController.I, scene),
            InScene(ScoreManager.I, scene),
            InScene(FindFirstObjectByType<HitBranchSensor>(), scene),
            InScene(CoconutManager.I, scene),
            InScene(FindFirstObjectByType<MonkeyController>(), scene),
            InScene(BossFightController.I, scene)
        );
    }

    private static T InScene<T>(T subject, Scene scene) where T: Component {
        return subject != null && subject.gameObject.scene == scene
            ? subject
            : null;
    }

    private void Observe(
        GameController game,
        ScoreManager score,
        HitBranchSensor branch,
        CoconutManager coconuts,
        MonkeyController monkey,
        BossFightController boss
    ) {
        if (observedGame != null) {
            observedGame.StateChanged -= OnGameStateChanged;
            observedGame.LeapChanged -= OnLeapChanged;
            observedGame.PauseChanged -= OnPauseChanged;
        }
        if (observedMonkey != null) {
            observedMonkey.RoughBarkTouched -= OnRoughBarkTouched;
        }
        if (observedBoss != null) {
            observedBoss.FightStarted -= OnBossFightStarted;
            observedBoss.ArmorAbsorbed -= OnBossArmorAbsorbed;
            observedBoss.Revived -= OnBossRevived;
        }
        if (observedScore != null) {
            observedScore.BananaCollected -= OnBananaCollected;
        }
        if (observedBranch != null) {
            observedBranch.BranchHit -= OnBranchHit;
        }
        if (observedCoconuts != null) {
            observedCoconuts.Collected -= OnCoconutCollected;
            observedCoconuts.Used -= OnCoconutUsed;
            observedCoconuts.Frozen -= OnCobraFrozen;
        }

        observedGame = game;
        observedScore = score;
        observedBranch = branch;
        observedCoconuts = coconuts;
        observedMonkey = monkey;
        observedBoss = boss;

        if (observedGame != null) {
            observedGame.StateChanged += OnGameStateChanged;
            observedGame.LeapChanged += OnLeapChanged;
            observedGame.PauseChanged += OnPauseChanged;
        }
        if (observedMonkey != null) {
            observedMonkey.RoughBarkTouched += OnRoughBarkTouched;
        }
        if (observedBoss != null) {
            observedBoss.FightStarted += OnBossFightStarted;
            observedBoss.ArmorAbsorbed += OnBossArmorAbsorbed;
            observedBoss.Revived += OnBossRevived;
        }
        if (observedScore != null) {
            observedScore.BananaCollected += OnBananaCollected;
        }
        if (observedBranch != null) {
            observedBranch.BranchHit += OnBranchHit;
        }
        if (observedCoconuts != null) {
            observedCoconuts.Collected += OnCoconutCollected;
            observedCoconuts.Used += OnCoconutUsed;
            observedCoconuts.Frozen += OnCobraFrozen;
        }
    }

    // the one way an event enters a run: stamps it, ignored b/t runs, saves
    private void RecordEvent(AnalyticsMetric metric) {
        if (currentRun == null) {
            return;
        }

        currentRun.GetEvents(metric).Add(CreateStamp());
        SaveSession();
    }

    // opens a run, an old one never closed belongs to a reloaded scene
    private void OpenRun(bool isInZone) {
        CloseRun(RunStatus.Interrupted);
        currentRun = new RunData {
            StartTime = Time.time,
            StartClock = DateTime.Now,
            StartedInZone = isInZone,
            Mode = observedGame != null ? observedGame.Mode : RunMode.Full
        };
        AddSample();
        // the bar is gone from a menu scene, the chart then keeps its default
        StaminaBarController bar = StaminaBarController.I;
        if (bar != null) {
            currentRun.MaxStamina = bar.MaxStamina;
        }
        nextSampleTime = Time.time + config.SampleIntervalS;
        session.Runs.Add(currentRun);
        SaveSession();
    }

    // snapshot of the moment and the height climbed above the run start
    private static EventStamp CreateStamp() {
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        return new EventStamp(
            DateTime.Now,
            ramp != null ? ramp.CurrentPlayerClimbedYDistance : 0f
        );
    }

    // records height & the stamina bar now, stamina reads 0 w/o a bar in the
    // scene
    private void AddSample() {
        if (currentRun == null) {
            return;
        }

        StaminaBarController bar = StaminaBarController.I;
        currentRun.Samples.Add(new RunSample(
            Time.time - currentRun.StartTime,
            CreateStamp().Height,
            bar != null ? bar.CurrentStamina : 0f
        ));
    }

    // whether the whole session was written to its file; any save, event or
    // timed, restarts the timed save countdown
    private bool SaveSession() {
        nextAutosaveTime = Time.time + config.AutosaveIntervalS;
        return file.Save(session);
    }

    // closes the open run w/ status & saves, the run joined the session at its
    // start; death cause is inferred only for a lost run
    private void CloseRun(RunStatus status) {
        if (!file.IsOpen || currentRun == null) {
            return;
        }

        RunData run = currentRun;
        currentRun = null;

        // where & why the run ended, as opposed to the highest point reached
        run.End = CreateStamp();
        if (status == RunStatus.Lost) {
            run.Cause = DeathCauseResolver.Resolve(
                run,
                run.End.Clock,
                config.DeathCauseWindowS
            );
        }
        run.Status = status;
        run.DurationS = Time.time - run.StartTime;
        run.HighestHeight = RampedDifficultyManager.I != null
            ? Mathf.Max(
                0f,
                RampedDifficultyManager.I.HighestPlayerClimbedYDistance
            )
            : 0f;
        ScoreManager scoreMgr = ScoreManager.I;
        run.TotalScore = scoreMgr != null ? scoreMgr.TotalScore : 0;
        run.HeightScore = scoreMgr != null ? scoreMgr.DistanceScore : 0;
        run.RewardScore = scoreMgr != null ? scoreMgr.RewardScore : 0;
        run.CoconutRewardScore =
            scoreMgr != null ? scoreMgr.CoconutRewardScore : 0;

        // the end sample needs the run to stay current for a moment
        currentRun = run;
        AddSample();
        currentRun = null;

        if (SaveSession() && Debug.isDebugBuild) {
            Debug.Log("Analytics:\trun " + RunCount + " " + status);
        }
    }
}
