using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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
        get; private set;
    }

    // Public Methods  #########################################################
    /// <summary>
    /// closes the current session and opens a new file, run numbering restarts
    /// </summary>
    public void StartNewSession() {
        RunCount = 0;
        runEntries.Clear();
        sessionDeathCauses.Clear();
        sessionDeathHeights.Clear();

        DateTime now = DateTime.Now;
        string stamp = now.ToString(
            "yyyy-MM-dd_HH_mm_ss",
            CultureInfo.InvariantCulture
        );
        string dir = Path.Combine(Application.persistentDataPath, FOLDER_NAME);

        try {
            Directory.CreateDirectory(dir);
            SessionFilePath = Path.Combine(dir, "0" + stamp + ".md");
            string title = now.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            );
            sessionHeader = "# MonKobra Analytics: Session: " + title + "\n\n"
                + "MonKobra v" + Application.version + "\n\n";
            File.WriteAllText(
                SessionFilePath,
                sessionHeader,
                new UTF8Encoding(false)
            );
        } catch (Exception e) {
            SessionFilePath = string.Empty;
            Debug.LogWarning(
                "Analytics:\tfail to open session file: " + e,
                this
            );
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
            observedGame != null
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
            runStartTime = Time.time;
            runStartClock = DateTime.Now;
            bananaClocks.Clear();
            webClocks.Clear();
            branchClocks.Clear();
            staminaSamples.Clear();
            AddStaminaSample();
            nextStaminaSampleTime = Time.time + staminaSampleIntervalS;
        } else if (state == GameState.Lost) {
            AppendRun();
        }
    }

    private void OnBananaCollected() {
        bananaClocks.Add(CreateStamp());
        AddStaminaSample(true);
    }

    private void OnWebEncountered(SpiderWebTrap _) {
        webClocks.Add(CreateStamp());
    }

    private void OnBranchHit(Collider _) {
        branchClocks.Add(CreateStamp());
    }

    // constants  ##############################################################
    private const string FOLDER_NAME = "Analytics";
    private const string CLOCK_FORMAT = "HH:mm:ss";
    // width of one death height histogram bin; world u
    private const int HEIGHT_BIN_U = 10;

    // nested types  ###########################################################
    private readonly struct EventStamp {
        public readonly DateTime Clock;
        // height above the run start y; world u
        public readonly float Height;

        public EventStamp(DateTime clock, float height) {
            Clock = clock;
            Height = height;
        }
    }

    private readonly struct StaminaSample {
        // seconds since the run started
        public readonly float Elapsed;
        public readonly float Stamina;
        // whether the sample was taken on a banana pickup
        public readonly bool IsBanana;

        public StaminaSample(float elapsed, float stamina, bool isBanana) {
            Elapsed = elapsed;
            Stamina = stamina;
            IsBanana = isBanana;
        }
    }

    // one point on the run's timeline, ordered by seconds since run start
    private readonly struct TimelineEntry {
        public readonly double Elapsed;
        public readonly string Label;
        // height above the run start y; world u
        public readonly float Height;
        public readonly bool HasHeight;

        public TimelineEntry(
            double elapsed,
            string label,
            float height,
            bool hasHeight
        ) {
            Elapsed = elapsed;
            Label = label;
            Height = height;
            HasHeight = hasHeight;
        }
    }

    // private members  ########################################################
    private float runStartTime;
    private DateTime runStartClock;
    private readonly List<EventStamp> bananaClocks = new List<EventStamp>();
    private readonly List<EventStamp> webClocks = new List<EventStamp>();
    private readonly List<EventStamp> branchClocks = new List<EventStamp>();
    private readonly List<StaminaSample> staminaSamples =
        new List<StaminaSample>();
    private float nextStaminaSampleTime;

    // the session file is rewritten on each run so its summary stays on top
    private string sessionHeader = string.Empty;
    private readonly List<string> runEntries = new List<string>();
    private readonly List<DeathCause> sessionDeathCauses =
        new List<DeathCause>();
    private readonly List<float> sessionDeathHeights = new List<float>();

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

    private static string FormatClock(DateTime clock) {
        return clock.ToString(CLOCK_FORMAT, CultureInfo.InvariantCulture);
    }

    // "unit * count = total" when every banana paid the same, else total only
    private static string FormatBananaScore(long total, int count) {
        if (count > 0 && total % count == 0) {
            return (total / count) + " * " + count + " = " + total;
        }

        return total.ToString(CultureInfo.InvariantCulture);
    }

    // snapshot of the moment and the height climbed above the run start
    private static EventStamp CreateStamp() {
        RampedDifficultyService ramp = RampedDifficultyService.I;
        return new EventStamp(
            DateTime.Now,
            ramp != null ? ramp.CurrentPlayerClimbedYDistance : 0f
        );
    }

    // each stamp as time elapsed since the run started, then its height
    // iff hasHeight
    private static void AppendClockList(
        StringBuilder sb,
        List<EventStamp> stamps,
        DateTime start,
        bool hasHeight
    ) {
        for (int i = 0; i < stamps.Count; i++) {
            TimeSpan elapsed = stamps[i].Clock - start;
            sb.Append(i + 1).Append(". +");
            sb.Append(elapsed.ToString(
                @"hh\:mm\:ss",
                CultureInfo.InvariantCulture
            ));
            if (hasHeight) {
                sb.Append("    y=");
                sb.Append(stamps[i].Height.ToString(
                    "+0.0;-0.0;+0.0",
                    CultureInfo.InvariantCulture
                ));
            }
            sb.Append('\n');
        }
        if (stamps.Count > 0) {
            sb.Append('\n');
        }
    }

    // records the stamina bar now, skipped w/o one in the scene
    private void AddStaminaSample(bool isBanana = false) {
        StaminaBarController bar = StaminaBarController.I;
        if (bar == null) {
            return;
        }

        staminaSamples.Add(new StaminaSample(
            Time.time - runStartTime,
            bar.CurrentStamina,
            isBanana
        ));
    }

    // line of sampled stamina over the run w/ a bar at each banana pickup,
    // skipped w/o 2 samples
    private void AppendStaminaCurve(StringBuilder sb) {
        if (staminaSamples.Count < 2) {
            return;
        }

        StaminaBarController bar = StaminaBarController.I;
        int maxStamina = Mathf.Max(
            1,
            Mathf.CeilToInt(bar != null ? bar.MaxStamina : 0f)
        );
        var times = new List<string>();
        var values = new List<string>();
        var markers = new List<string>();
        for (int i = 0; i < staminaSamples.Count; i++) {
            times.Add(FormatSeconds(staminaSamples[i].Elapsed));
            values.Add(staminaSamples[i].Stamina.ToString(
                "F1",
                CultureInfo.InvariantCulture
            ));
            markers.Add(
                staminaSamples[i].IsBanana
                    ? maxStamina.ToString(CultureInfo.InvariantCulture)
                    : "0"
            );
        }

        sb.Append("#### Stamina Curve\n\nBars mark banana pickups.\n\n");
        sb.Append("```mermaid\nxychart-beta\n");
        sb.Append("    title \"Stamina Curve\"\n");
        sb.Append("    x-axis \"Elapsed (s)\" [")
            .Append(string.Join(", ", times)).Append("]\n");
        sb.Append("    y-axis \"Stamina\" 0 --> ").Append(maxStamina);
        sb.Append('\n');
        sb.Append("    bar [").Append(string.Join(", ", markers));
        sb.Append("]\n");
        sb.Append("    line [").Append(string.Join(", ", values));
        sb.Append("]\n```\n\n");
    }

    // pie of what ended each run of the session
    private void AppendDeathCauses(StringBuilder sb) {
        sb.Append("#### Death Causes\n\n```mermaid\npie showData\n");
        sb.Append("    title Death Causes\n");
        foreach (DeathCause cause in Enum.GetValues(typeof(DeathCause))) {
            int count = sessionDeathCauses.FindAll(c => c == cause).Count;
            if (count > 0) {
                sb.Append("    \"").Append(cause).Append("\" : ");
                sb.Append(count).Append('\n');
            }
        }
        sb.Append("```\n\n");
    }

    // bars of deaths per height band across the session; heights below 0
    // count in the first band
    private void AppendDeathHeightHistogram(StringBuilder sb) {
        var bins = new List<int>();
        for (int i = 0; i < sessionDeathHeights.Count; i++) {
            int bin = Mathf.Max(
                0,
                Mathf.FloorToInt(sessionDeathHeights[i] / HEIGHT_BIN_U)
            );
            while (bins.Count <= bin) {
                bins.Add(0);
            }
            bins[bin]++;
        }

        var labels = new List<string>();
        int maxCount = 1;
        for (int i = 0; i < bins.Count; i++) {
            labels.Add(
                "\"" + (i * HEIGHT_BIN_U) + "-"
                    + (((i + 1) * HEIGHT_BIN_U) - 1) + "\""
            );
            maxCount = Mathf.Max(maxCount, bins[i]);
        }

        sb.Append("#### Death Height Histogram\n\n```mermaid\nxychart-beta\n");
        sb.Append("    title \"Death Height Histogram\"\n");
        sb.Append("    x-axis \"Height (u)\" [")
            .Append(string.Join(", ", labels)).Append("]\n");
        sb.Append("    y-axis \"Deaths\" 0 --> ").Append(maxCount);
        sb.Append('\n');
        sb.Append("    bar [").Append(string.Join(", ", bins));
        sb.Append("]\n```\n\n");
    }

    // rewrites the whole file: header, session summary, then every run
    private bool WriteSessionFile() {
        var sb = new StringBuilder(sessionHeader);
        if (runEntries.Count > 0) {
            sb.Append("## Session Summary\n\n");
            AppendDeathCauses(sb);
            AppendDeathHeightHistogram(sb);
        }
        for (int i = 0; i < runEntries.Count; i++) {
            sb.Append(runEntries[i]);
        }

        try {
            File.WriteAllText(
                SessionFilePath,
                sb.ToString(),
                new UTF8Encoding(false)
            );
        } catch (Exception e) {
            Debug.LogWarning("Analytics:\tfail to write session: " + e, this);
            return false;
        }

        return true;
    }

    private static string FormatSeconds(double seconds) {
        return seconds.ToString("F1", CultureInfo.InvariantCulture);
    }

    private static string FormatHeight(float height) {
        return height.ToString("+0.0;-0.0;+0.0", CultureInfo.InvariantCulture);
    }

    // every event of the run, plus start & game over, sorted by time
    private List<TimelineEntry> BuildTimeline(EventStamp end) {
        var entries = new List<TimelineEntry> {
            new TimelineEntry(0.0, "Start", 0f, true)
        };
        AddTimelineEntries(entries, bananaClocks, "Banana", true);
        AddTimelineEntries(entries, webClocks, "Web", false);
        AddTimelineEntries(entries, branchClocks, "Branch", true);
        entries.Add(new TimelineEntry(
            (end.Clock - runStartClock).TotalSeconds,
            "Game Over",
            end.Height,
            true
        ));
        entries.Sort((a, b) => a.Elapsed.CompareTo(b.Elapsed));
        return entries;
    }

    private void AddTimelineEntries(
        List<TimelineEntry> entries,
        List<EventStamp> stamps,
        string name,
        bool hasHeight
    ) {
        for (int i = 0; i < stamps.Count; i++) {
            entries.Add(new TimelineEntry(
                (stamps[i].Clock - runStartClock).TotalSeconds,
                name + " " + (i + 1),
                stamps[i].Height,
                hasHeight
            ));
        }
    }

    // pie of height score against banana score, skipped w/o any score
    private static void AppendScoreSplit(
        StringBuilder sb,
        long heightScore,
        long rewardScore
    ) {
        if (heightScore <= 0 && rewardScore <= 0) {
            return;
        }

        sb.Append("#### Score Split\n\n```mermaid\npie showData\n");
        sb.Append("    title Score Split\n");
        if (heightScore > 0) {
            sb.Append("    \"Height\" : ").Append(heightScore).Append('\n');
        }
        if (rewardScore > 0) {
            sb.Append("    \"Banana\" : ").Append(rewardScore).Append('\n');
        }
        sb.Append("```\n\n");
    }

    // line of height at each event that carries one, x axis is categorical so
    // spacing is per event, not per second
    private static void AppendHeightOverTime(
        StringBuilder sb,
        List<TimelineEntry> entries
    ) {
        var times = new List<string>();
        var heights = new List<string>();
        float minHeight = 0f;
        float maxHeight = 1f;
        for (int i = 0; i < entries.Count; i++) {
            if (!entries[i].HasHeight) {
                continue;
            }

            times.Add(FormatSeconds(entries[i].Elapsed));
            heights.Add(entries[i].Height.ToString(
                "F1",
                CultureInfo.InvariantCulture
            ));
            minHeight = Mathf.Min(minHeight, entries[i].Height);
            maxHeight = Mathf.Max(maxHeight, entries[i].Height);
        }

        sb.Append("#### Height Over Time\n\n```mermaid\nxychart-beta\n");
        sb.Append("    title \"Height Over Time\"\n");
        sb.Append("    x-axis \"Elapsed (s)\" [")
            .Append(string.Join(", ", times)).Append("]\n");
        sb.Append("    y-axis \"Height (u)\" ")
            .Append(Mathf.FloorToInt(minHeight)).Append(" --> ")
            .Append(Mathf.CeilToInt(maxHeight)).Append('\n');
        sb.Append("    line [").Append(string.Join(", ", heights));
        sb.Append("]\n```\n\n");
    }

    private static void AppendRunTimeline(
        StringBuilder sb,
        List<TimelineEntry> entries
    ) {
        sb.Append("#### Run Timeline\n\n```mermaid\ntimeline\n");
        sb.Append("    title Run Timeline\n");
        for (int i = 0; i < entries.Count; i++) {
            sb.Append("    +").Append(FormatSeconds(entries[i].Elapsed));
            sb.Append(" s : ").Append(entries[i].Label);
            if (entries[i].HasHeight) {
                sb.Append(", y ").Append(FormatHeight(entries[i].Height));
            }
            sb.Append('\n');
        }
        sb.Append("```\n\n");
    }

    private void AppendRun() {
        if (string.IsNullOrEmpty(SessionFilePath)) {
            return;
        }

        RunCount++;
        float duration = Time.time - runStartTime;
        int height = RampedDifficultyService.I != null
            ? Mathf.Max(
                0,
                Mathf.FloorToInt(
                    RampedDifficultyService.I.HighestPlayerClimbedYDistance
                )
            )
            : 0;
        ScoreManager scoreMgr = ScoreManager.I;
        long score = scoreMgr != null ? scoreMgr.TotalScore : 0;
        long heightScore = scoreMgr != null ? scoreMgr.DistanceScore : 0;
        long rewardScore = scoreMgr != null ? scoreMgr.RewardScore : 0;

        // where & why the run ended, as opposed to the highest point reached
        EventStamp death = CreateStamp();
        GameController game = GameController.I;
        DeathCause cause = game != null
            ? game.LastDeathCause
            : DeathCause.Unknown;
        AddStaminaSample();

        var sb = new StringBuilder();
        sb.Append("## Run ").Append(RunCount).Append("\n\n");
        sb.Append("- Final Height: ").Append(height).Append('\n');
        sb.Append("- Death Height: ").Append(FormatHeight(death.Height));
        sb.Append('\n');
        sb.Append("- Death Cause: ").Append(cause).Append("\n\n");

        sb.Append("#### Run Duration: ");
        sb.Append(duration.ToString("F1", CultureInfo.InvariantCulture));
        sb.Append(" s\n\n");
        sb.Append("- Time Start: ").Append(FormatClock(runStartClock));
        sb.Append('\n');
        sb.Append("- Time Stop: ").Append(FormatClock(DateTime.Now));
        sb.Append("\n\n");

        sb.Append("#### Final Score: ").Append(score).Append("\n\n");
        sb.Append("- Score from Height: ").Append(heightScore);
        sb.Append('\n');
        sb.Append("- Score from Banana: ");
        sb.Append(FormatBananaScore(rewardScore, bananaClocks.Count));
        sb.Append("\n\n");

        sb.Append("#### Banana Count: ").Append(bananaClocks.Count);
        sb.Append("\n\n");
        AppendClockList(sb, bananaClocks, runStartClock, true);

        sb.Append("#### Web Encounter: ").Append(webClocks.Count);
        sb.Append("\n\n");
        AppendClockList(sb, webClocks, runStartClock, false);

        sb.Append("#### Branch Hit: ").Append(branchClocks.Count);
        sb.Append("\n\n");
        AppendClockList(sb, branchClocks, runStartClock, true);

        List<TimelineEntry> timeline = BuildTimeline(death);
        AppendScoreSplit(sb, heightScore, rewardScore);
        AppendHeightOverTime(sb, timeline);
        AppendStaminaCurve(sb);
        AppendRunTimeline(sb, timeline);

        runEntries.Add(sb.ToString());
        sessionDeathCauses.Add(cause);
        sessionDeathHeights.Add(death.Height);
        if (!WriteSessionFile()) {
            return;
        }

        if (Debug.isDebugBuild) {
            Debug.Log("Analytics:\trun " + RunCount + " appended");
        }
    }
}
