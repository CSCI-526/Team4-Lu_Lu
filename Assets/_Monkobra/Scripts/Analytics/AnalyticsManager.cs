using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Writes playtest analytics to a markdown file per session. A session is one
/// game launch: the file opens with an h1 timestamp, and every game over
/// appends one run entry. Scene restarts keep the same session, and the
/// <see cref="restartSessionKey"/> forces a fresh file mid-launch.
/// <para>
/// Attach to a root GameObject of the first scene. It is kept across scene
/// reloads, and the copy each reload brings in destroys itself. Files land in
/// <c>Application.persistentDataPath/Analytics/</c>.
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
        get; private set;
    } = string.Empty;

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

        string stamp = now.ToString(
            "yyyy-MM-dd_HH_mm_ss",
            CultureInfo.InvariantCulture
        );
        string dir = Path.Combine(Application.persistentDataPath, FOLDER_NAME);

        try {
            Directory.CreateDirectory(dir);
        } catch (Exception e) {
            SessionFilePath = string.Empty;
            Debug.LogWarning(
                "Analytics:\tfail to open session file: " + e,
                this
            );
            return;
        }

        SessionFilePath = Path.Combine(dir, "0" + stamp + ".md");
        if (!SaveSession()) {
            SessionFilePath = string.Empty;
            return;
        }

        if (Debug.isDebugBuild) {
            Debug.Log("Analytics:\tsession opened " + SessionFilePath);
        }
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("forces a new session file")]
    private Key restartSessionKey = Key.F9;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("seconds b/t stamina samples while a run is playing")]
    private float staminaSampleIntervalS = 2f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        if (I != null && I != this) {
            Debug.LogWarning("Analytics:\tduplicate instance destroyed", this);
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        // static event, webs spawn at runtime so none can be looked up here
        SpiderWebTrap.Encountered += OnWebEncountered;
        StartNewSession();
        ObserveScene();
    }

    private void Update() {
        if (
            currentRun != null
            && observedGame != null
            && observedGame.State == GameState.Playing
            && Time.time >= nextStaminaSampleTime
        ) {
            AddStaminaSample();
            nextStaminaSampleTime = Time.time + staminaSampleIntervalS;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) {
            return;
        }

        if (keyboard[restartSessionKey].wasPressedThisFrame) {
            Debug.LogWarning(
                "Analytics:\trestart session key pressed, closing session "
                    + SessionFilePath,
                this
            );
            StartNewSession();
        }
    }

    private void OnDestroy() {
        if (I != this) {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SpiderWebTrap.Encountered -= OnWebEncountered;
        Observe(null, null, null);
        I = null;
    }

    // Event Handlers  #########################################################
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        // each reload builds fresh instances of every observed subject
        ObserveScene();
    }

    private void OnGameStateChanged(GameState state) {
        if (state == GameState.Playing) {
            currentRun = new RunData {
                StartTime = Time.time,
                StartClock = DateTime.Now
            };
            AddStaminaSample();
            nextStaminaSampleTime = Time.time + staminaSampleIntervalS;
        } else if (state == GameState.Lost) {
            FinishRun();
        }
    }

    private void OnBananaCollected() {
        if (currentRun == null) {
            return;
        }

        currentRun.Bananas.Add(CreateStamp());
        AddStaminaSample(true);
    }

    private void OnWebEncountered(SpiderWebTrap _) {
        if (currentRun != null) {
            currentRun.Webs.Add(CreateStamp());
        }
    }

    private void OnBranchHit(Collider _) {
        if (currentRun != null) {
            currentRun.Branches.Add(CreateStamp());
        }
    }

    // constants  ##############################################################
    private const string FOLDER_NAME = "Analytics";

    // private members  ########################################################
    private SessionData session = new SessionData();
    // run being played, null b/t a game over and the next start
    private RunData currentRun;
    private float nextStaminaSampleTime;

    // cached references  ------------------------------------------------------
    private GameController observedGame;
    private ScoreManager observedScore;
    private HitBranchDetection observedBranch;

    // private methods  ########################################################
    private void ObserveScene() {
        Observe(
            GameController.I,
            ScoreManager.I,
            FindFirstObjectByType<HitBranchDetection>()
        );
    }

    private void Observe(
        GameController game,
        ScoreManager score,
        HitBranchDetection branch
    ) {
        if (observedGame != null) {
            observedGame.StateChanged -= OnGameStateChanged;
        }
        if (observedScore != null) {
            observedScore.BananaCollected -= OnBananaCollected;
        }
        if (observedBranch != null) {
            observedBranch.BranchHit -= OnBranchHit;
        }

        observedGame = game;
        observedScore = score;
        observedBranch = branch;

        if (observedGame != null) {
            observedGame.StateChanged += OnGameStateChanged;
        }
        if (observedScore != null) {
            observedScore.BananaCollected += OnBananaCollected;
        }
        if (observedBranch != null) {
            observedBranch.BranchHit += OnBranchHit;
        }
    }

    // snapshot of the moment and the height climbed above the run start
    private static EventStamp CreateStamp() {
        RampedDifficultyService ramp = RampedDifficultyService.I;
        return new EventStamp(
            DateTime.Now,
            ramp != null ? ramp.CurrentPlayerClimbedYDistance : 0f
        );
    }

    // records the stamina bar now, skipped w/o one in the scene
    private void AddStaminaSample(bool isBanana = false) {
        StaminaBarController bar = StaminaBarController.I;
        if (bar == null || currentRun == null) {
            return;
        }

        currentRun.StaminaSamples.Add(new StaminaSample(
            Time.time - currentRun.StartTime,
            bar.CurrentStamina,
            isBanana
        ));
    }

    // whether the whole session was written to its file
    private bool SaveSession() {
        return AnalyticsMarkdownWriter.WriteFile(
            SessionFilePath,
            AnalyticsMarkdownWriter.BuildSession(session)
        );
    }

    // closes the run as lost, adds it to the session & saves
    private void FinishRun() {
        if (string.IsNullOrEmpty(SessionFilePath) || currentRun == null) {
            return;
        }

        RunData run = currentRun;
        currentRun = null;

        // where & why the run ended, as opposed to the highest point reached
        run.End = CreateStamp();
        GameController game = GameController.I;
        run.Cause = game != null ? game.LastDeathCause : DeathCause.Unknown;
        run.Status = RunStatus.Lost;
        run.DurationS = Time.time - run.StartTime;
        run.FinalHeight = RampedDifficultyService.I != null
            ? Mathf.Max(
                0,
                Mathf.FloorToInt(
                    RampedDifficultyService.I.HighestPlayerClimbedYDistance
                )
            )
            : 0;
        ScoreManager scoreMgr = ScoreManager.I;
        run.TotalScore = scoreMgr != null ? scoreMgr.TotalScore : 0;
        run.HeightScore = scoreMgr != null ? scoreMgr.DistanceScore : 0;
        run.RewardScore = scoreMgr != null ? scoreMgr.RewardScore : 0;

        // the end sample needs the run to stay current for a moment
        currentRun = run;
        AddStaminaSample();
        currentRun = null;

        session.Runs.Add(run);
        if (SaveSession() && Debug.isDebugBuild) {
            Debug.Log("Analytics:\trun " + RunCount + " appended");
        }
    }
}
