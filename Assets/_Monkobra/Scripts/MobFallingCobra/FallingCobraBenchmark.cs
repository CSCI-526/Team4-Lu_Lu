#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Dev-only benchmark of the falling cobras, active only when the player or
/// Editor starts with <c>-fallingCobraBenchmark &lt;outputFolder&gt;</c>.
/// Loads <c>GameplayScene</c>, holds the monkey still beside the cobras, keeps
/// <see cref="COBRA_TARGET"/> cobras live around it, times
/// <see cref="RECORD_FRAMES"/> frames with <see cref="ProfilerRecorder"/>,
/// then writes <c>results.json</c> and a screenshot of a perched, a warning
/// and a falling snake to the folder, and quits.
/// <para>
/// Frames that spawned a cobra are left out of the statistics, so the numbers
/// measure the cobras, not the benchmark.
/// </para>
/// </summary>
[DefaultExecutionOrder(-32000)]
public sealed class FallingCobraBenchmark: MonoBehaviour {
    // Public Members  #########################################################
    /// <summary>the command line argument that switches the benchmark on,
    /// followed by the output folder</summary>
    public const string ARGUMENT = "-fallingCobraBenchmark";

    [Serializable]
    public sealed class NameCount {
        public string name;
        public int count;
    }

    [Serializable]
    public sealed class MetricStats {
        public string name;
        public bool isAvailable;
        public float meanMs;
        public float p50Ms;
        public float p95Ms;
        public float maxMs;
        public float meanCallsPerFrame;
    }

    [Serializable]
    public sealed class Report {
        public string error;
        public string unityVersion;
        public string timestamp;
        public int cobraTarget;
        public int warmUpFrames;
        public int recordedFrames;
        public int excludedFrames;
        public int spawnedTotal;
        public float meanLive;
        public float meanDormant;
        public float meanWarning;
        public float meanFalling;
        public int hitCount;
        public List<NameCount> hitSources = new();
        public List<MetricStats> metrics = new();
        public List<string> screenshots = new();
        public List<string> missingScreenshots = new();
    }

    // MonoBehaviour Lifecycle  ################################################
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart() {
        string[] args = Environment.GetCommandLineArgs();
        int at = Array.IndexOf(args, ARGUMENT);
        if (at < 0 || FindFirstObjectByType<FallingCobraBenchmark>() != null) {
            return;
        }
        GameObject host = new GameObject("FallingCobraBenchmark");
        FallingCobraBenchmark benchmark =
            host.AddComponent<FallingCobraBenchmark>();
        benchmark.outputDir = at + 1 < args.Length
            && !args[at + 1].StartsWith("-") ? args[at + 1]
            : Path.Combine(Application.persistentDataPath,
                "FallingCobraBenchmark");
    }

    private void Awake() {
        DontDestroyOnLoad(gameObject);
        // uncapped and unfocused, so the frame time is the CPU cost
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        Application.logMessageReceived += OnLog;
        mainThread =
            ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread",
                1);
        foreach (string name in COBRA_MARKERS) {
            recorders.Add(new Metric(name, ProfilerCategory.Scripts));
        }
        foreach (string name in PHYSICS_MARKERS) {
            recorders.Add(new Metric(name, ProfilerCategory.Physics));
        }
        report.cobraTarget = COBRA_TARGET;
        report.warmUpFrames = WARM_UP_FRAMES;
        report.unityVersion = Application.unityVersion;
        report.timestamp = DateTime.Now.ToString("s");
        if (Debug.isDebugBuild) {
            Debug.Log("FallingCobraBenchmark:\tstart, output " + outputDir);
        }
    }

    private void OnDestroy() {
        Application.logMessageReceived -= OnLog;
        mainThread.Dispose();
        foreach (Metric metric in recorders) {
            metric.frameRecorder.Dispose();
            metric.callRecorder.Dispose();
        }
        if (shotTexture != null) {
            shotTexture.Release();
        }
    }

    private void Update() {
        if (phase == Phase.Done) {
            return;
        }
        if (Time.realtimeSinceStartup > TIMEOUT_S) {
            Finish("timed out in phase " + phase);
            return;
        }
        switch (phase) {
            case Phase.Loading:
                UpdateLoading();
                break;
            case Phase.Starting:
                UpdateStarting();
                break;
            case Phase.Settling:
                phaseFrames++;
                if (phaseFrames >= SETTLE_FRAMES) {
                    EnterSeeding();
                }
                break;
            case Phase.WarmUp:
                TickScene();
                phaseFrames++;
                if (phaseFrames >= WARM_UP_FRAMES) {
                    ResetSamples();
                    phase = Phase.Recording;
                    phaseFrames = 0;
                }
                break;
            case Phase.Recording:
                TickScene();
                SampleFrame();
                if (sampleCount >= RECORD_FRAMES) {
                    phase = Phase.Shots;
                    StartCoroutine(ShotsRoutine());
                }
                break;
        }
    }

    // constants  ##############################################################
    private const int COBRA_TARGET = 12;
    private const int WARM_UP_FRAMES = 120;
    private const int RECORD_FRAMES = 300;
    private const int SETTLE_FRAMES = 60;
    private const int TOP_UP_INTERVAL_FRAMES = 15;
    private const int SEED = 20260525;
    private const int SPAWN_ATTEMPTS_PER_TOP_UP = 60;
    private const int SHOT_TIMEOUT_FRAMES = 1200;
    private const int SHOT_WIDTH = 1280;
    private const int SHOT_HEIGHT = 720;
    private const int RECORDER_CAPACITY = 60000;
    private const float TIMEOUT_S = 240f;
    private const float NEAR_SEGMENT_GAP_U = 25f;
    private const float LIVE_RADIUS_U = 30f;
    private const float PULL_BELOW_U = 3f;
    private const float PULL_SPEED_U = 40f;
    private const float SHOT_DISTANCE_U = 10f;
    private const float SHOT_FOV_DEG = 40f;
    private const string HIT_LOG_PREFIX =
        "FallingCobraController:\thit from monkey collider ";
    private static readonly string[] COBRA_MARKERS = {
        "FallingCobra.Update", "FallingCobra.FixedUpdate",
        "FallingCobra.LateUpdate", "FallingCobra.Spine",
        "FallingCobra.UpdateBlends", "FallingCobra.BuildBodyPath",
        "FallingCobra.ChooseHeadPose", "FallingCobra.FitNeckCurve",
        "FallingCobra.PlaceNeckSpine", "FallingCobra.BuildTubeMesh",
        "FallingCobra.PublishMesh", "FallingCobra.AdvanceSwimTrail",
        "FallingCobra.FairSpineCorners", "FallingCobra.RoundNeckSurface",
    };
    private static readonly string[] PHYSICS_MARKERS = {
        "Physics.Simulate", "FixedUpdate.PhysicsFixedUpdate",
        "Physics.Processing", "Physics.FetchResults",
    };

    // private members  ########################################################
    private enum Phase { Loading, Starting, Settling, WarmUp, Recording,
        Shots, Done }

    private sealed class Metric {
        public readonly string name;
        // the last finished frame's summed time; ns
        public readonly ProfilerRecorder frameRecorder;
        // every call, only counted
        public readonly ProfilerRecorder callRecorder;
        public readonly List<float> perFrameMs = new();
        public int callsAtStart;

        public Metric(string name, ProfilerCategory category) {
            this.name = name;
            frameRecorder = ProfilerRecorder.StartNew(category, name, 1,
                ProfilerRecorderOptions.StartImmediately
                | ProfilerRecorderOptions.SumAllSamplesInFrame
                | ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
            callRecorder = ProfilerRecorder.StartNew(category, name,
                RECORDER_CAPACITY,
                ProfilerRecorderOptions.StartImmediately
                | ProfilerRecorderOptions.CollectOnlyOnCurrentThread
                | ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
        }
    }

    private string outputDir;
    private Phase phase = Phase.Loading;
    private int phaseFrames;
    private bool isLoadRequested;
    private int loadFrame;
    private int sampleCount;
    private int skipSamples;
    private int excludedFrames;
    private int recordedFrameTotal;
    private int checkCount;
    private int liveSum;
    private int dormantSum;
    private int warningSum;
    private int fallingSum;
    private int spawnedTotal;
    private ProfilerRecorder mainThread;
    private readonly List<Metric> recorders = new();
    private readonly List<float> mainThreadMs = new();
    private readonly Report report = new();
    private readonly Dictionary<string, int> hitSources = new();
    private readonly List<TreeSegmentController> segments = new();
    private FallingCobraSpawner spawner;
    private Transform monkey;
    private RenderTexture shotTexture;
    private Camera shotCamera;

    // private methods  ########################################################
    private void UpdateLoading() {
        if (!isLoadRequested) {
            isLoadRequested = true;
            loadFrame = Time.frameCount;
            // the same seed every run, so the scene rolls the same way
            UnityEngine.Random.InitState(SEED);
            SceneNames.Load(SceneNames.GAMEPLAY, RunMode.Full);
            return;
        }
        if (Time.frameCount > loadFrame + 1
            && SceneManager.GetActiveScene().name == SceneNames.GAMEPLAY
            && GameController.I != null && MonkeyController.I != null) {
            phase = Phase.Starting;
            phaseFrames = 0;
        }
    }

    private void UpdateStarting() {
        GameController game = GameController.I;
        if (game.State == GameState.Tutorial) {
            // HACK: the tutorial waits for a key press, no public way past it
            MethodInfo setState =
                typeof(GameController).GetMethod("SetState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            setState.Invoke(game, new object[] { GameState.StartingZone });
            return;
        }
        if (!game.IsRunLive) {
            return;
        }
        MonkeyController.I.IsHeld = true;
        monkey = MonkeyController.I.transform;
        phase = Phase.Settling;
        phaseFrames = 0;
    }

    private void EnterSeeding() {
        spawner = FindFirstObjectByType<FallingCobraSpawner>();
        if (spawner == null) {
            Finish("no FallingCobraSpawner in the scene");
            return;
        }
        segments.AddRange(
            FindObjectsByType<TreeSegmentController>(FindObjectsSortMode.None));
        float monkeyY = monkey.position.y;
        segments.Sort((a, b) => SegmentGapU(a, monkeyY)
            .CompareTo(SegmentGapU(b, monkeyY)));
        if (segments.Count == 0) {
            Finish("no TreeSegmentController in the scene");
            return;
        }
        // only the segments within reach of the monkey carry cobras
        segments.RemoveAll(segment => SegmentGapU(segment, monkeyY)
            > NEAR_SEGMENT_GAP_U);
        if (segments.Count == 0) {
            Finish("no tree segment near the monkey");
            return;
        }
        UnityEngine.Random.InitState(SEED);
        TopUp();
        // the held monkey is slid in beside the cobras, so they wake & strike
        FallingCobraController first =
            FindNearestInState(FallingCobraState.Dormant);
        if (first != null) {
            MonkeyController.I.PullTo(first.transform.position
                + Vector3.down * PULL_BELOW_U, PULL_SPEED_U);
        }
        phase = Phase.WarmUp;
        phaseFrames = 0;
    }

    // vertical gap from the monkey to the segment's tallest collider; u
    private static float SegmentGapU(TreeSegmentController segment,
        float monkeyY) {
        Bounds best = default;
        foreach (Collider box in segment.GetComponentsInChildren<Collider>()) {
            if (box.bounds.size.y > best.size.y) {
                best = box.bounds;
            }
        }
        return Mathf.Max(0f, Mathf.Abs(monkeyY - best.center.y)
            - best.extents.y);
    }

    // keeps the live count and tops it up every few frames
    private void TickScene() {
        if (Time.frameCount % TOP_UP_INTERVAL_FRAMES == 0) {
            TopUp();
        }
    }

    private void TopUp() {
        FallingCobraController[] cobras = FindObjectsByType<
            FallingCobraController>(FindObjectsSortMode.None);
        int live = 0, dormant = 0, warning = 0, falling = 0;
        foreach (FallingCobraController cobra in cobras) {
            if ((cobra.transform.position - monkey.position).magnitude
                > LIVE_RADIUS_U) {
                continue;
            }
            live++;
            switch (cobra.CurrentState) {
                case FallingCobraState.Dormant: dormant++; break;
                case FallingCobraState.Warning: warning++; break;
                case FallingCobraState.Falling: falling++; break;
            }
        }
        checkCount++;
        liveSum += live;
        dormantSum += dormant;
        warningSum += warning;
        fallingSum += falling;
        int missing = COBRA_TARGET - live;
        for (int attempt = 0; attempt < SPAWN_ATTEMPTS_PER_TOP_UP
            && missing > 0; attempt++) {
            if (spawner.TrySpawnCobra(segments[attempt % segments.Count])) {
                missing--;
                spawnedTotal++;
                skipSamples = 2;
            }
        }
    }

    // reads the last finished frame's markers; a spawn frame is left out
    private void SampleFrame() {
        recordedFrameTotal++;
        if (mainThread.Count == 0) {
            return;
        }
        if (skipSamples > 0) {
            skipSamples--;
            excludedFrames++;
            return;
        }
        mainThreadMs.Add(mainThread.LastValue / 1e6f);
        foreach (Metric metric in recorders) {
            bool isRead = metric.frameRecorder.Valid
                && metric.frameRecorder.Count > 0;
            metric.perFrameMs.Add(isRead
                ? metric.frameRecorder.LastValue / 1e6f : 0f);
        }
        sampleCount++;
    }

    private void ResetSamples() {
        mainThreadMs.Clear();
        sampleCount = 0;
        excludedFrames = 0;
        recordedFrameTotal = 0;
        foreach (Metric metric in recorders) {
            metric.perFrameMs.Clear();
            metric.callsAtStart = metric.callRecorder.Valid
                ? metric.callRecorder.Count : 0;
        }
    }

    // one screenshot per snake state, from a camera beside the snake
    private IEnumerator ShotsRoutine() {
        FallingCobraState[] wanted = {
            FallingCobraState.Dormant, FallingCobraState.Warning,
            FallingCobraState.Falling,
        };
        HashSet<FallingCobraState> shot = new();
        for (int frame = 0; frame < SHOT_TIMEOUT_FRAMES
            && shot.Count < wanted.Length; frame++) {
            yield return new WaitForEndOfFrame();
            TickScene();
            foreach (FallingCobraState state in wanted) {
                if (shot.Contains(state)) {
                    continue;
                }
                FallingCobraController cobra = FindNearestInState(state);
                if (cobra != null) {
                    CaptureShot(cobra, state);
                    shot.Add(state);
                }
            }
        }
        foreach (FallingCobraState state in wanted) {
            if (!shot.Contains(state)) {
                report.missingScreenshots.Add(state.ToString());
            }
        }
        Finish(null);
    }

    private FallingCobraController FindNearestInState(FallingCobraState state) {
        FallingCobraController best = null;
        float bestDistance = float.MaxValue;
        foreach (FallingCobraController cobra in
            FindObjectsByType<FallingCobraController>(
                FindObjectsSortMode.None)) {
            if (cobra.CurrentState != state) {
                continue;
            }
            float distance =
                (cobra.transform.position - monkey.position).magnitude;
            if (distance < bestDistance) {
                best = cobra;
                bestDistance = distance;
            }
        }
        return best;
    }

    private void CaptureShot(FallingCobraController cobra,
        FallingCobraState state) {
        if (shotCamera == null) {
            CreateShotCamera();
        }
        Transform segment = cobra.transform.parent;
        Vector3 anchor = segment != null ? segment.position : Vector3.zero;
        Vector3 outward = cobra.transform.position - anchor;
        outward.y = 0f;
        outward = outward.sqrMagnitude > .0001f ? outward.normalized
            : Vector3.forward;
        // the whole snake in frame, from the head up to its far end
        Vector3 focus = cobra.transform.position + Vector3.up
            * (cobra.LengthU * .5f);
        float distanceU = Mathf.Max(SHOT_DISTANCE_U, cobra.LengthU * 1.5f);
        shotCamera.transform.SetPositionAndRotation(
            focus + outward * distanceU,
            Quaternion.LookRotation(-outward));
        shotCamera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = shotTexture;
        Texture2D image = new Texture2D(SHOT_WIDTH, SHOT_HEIGHT,
            TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, SHOT_WIDTH, SHOT_HEIGHT), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        Directory.CreateDirectory(outputDir);
        string path = Path.Combine(outputDir,
            state.ToString().ToLowerInvariant() + ".png");
        File.WriteAllBytes(path, image.EncodeToPNG());
        Destroy(image);
        report.screenshots.Add(path);
        if (Debug.isDebugBuild) {
            Debug.Log("FallingCobraBenchmark:\tscreenshot " + path);
        }
    }

    private void CreateShotCamera() {
        GameObject host = new GameObject("BenchmarkCamera");
        host.transform.SetParent(transform, false);
        shotCamera = host.AddComponent<Camera>();
        if (Camera.main != null) {
            shotCamera.CopyFrom(Camera.main);
        }
        shotCamera.enabled = false;
        shotCamera.fieldOfView = SHOT_FOV_DEG;
        shotCamera.nearClipPlane = .1f;
        shotCamera.farClipPlane = 200f;
        shotTexture = new RenderTexture(SHOT_WIDTH, SHOT_HEIGHT, 24);
        shotCamera.targetTexture = shotTexture;
        UniversalAdditionalCameraData extra =
            host.AddComponent<UniversalAdditionalCameraData>();
        extra.renderPostProcessing = true;
    }

    private void OnLog(string condition, string stackTrace, LogType type) {
        if (!condition.StartsWith(HIT_LOG_PREFIX)) {
            return;
        }
        string source = condition.Substring(HIT_LOG_PREFIX.Length);
        hitSources.TryGetValue(source, out int count);
        hitSources[source] = count + 1;
        report.hitCount++;
    }

    private static MetricStats Summarize(string name, bool isAvailable,
        List<float> msPerFrame, float meanCallsPerFrame) {
        MetricStats stats = new MetricStats { name = name,
            isAvailable = isAvailable, meanCallsPerFrame = meanCallsPerFrame };
        if (msPerFrame.Count == 0) {
            return stats;
        }
        List<float> sorted = new(msPerFrame);
        sorted.Sort();
        float total = 0f;
        foreach (float ms in msPerFrame) {
            total += ms;
        }
        stats.meanMs = total / msPerFrame.Count;
        stats.p50Ms = sorted[sorted.Count / 2];
        stats.p95Ms = sorted[Mathf.Min(sorted.Count - 1,
            Mathf.CeilToInt(sorted.Count * .95f) - 1)];
        stats.maxMs = sorted[sorted.Count - 1];
        return stats;
    }

    private void Finish(string error) {
        phase = Phase.Done;
        report.error = error;
        report.recordedFrames = sampleCount;
        report.excludedFrames = excludedFrames;
        report.spawnedTotal = spawnedTotal;
        if (checkCount > 0) {
            report.meanLive = liveSum / (float)checkCount;
            report.meanDormant = dormantSum / (float)checkCount;
            report.meanWarning = warningSum / (float)checkCount;
            report.meanFalling = fallingSum / (float)checkCount;
        }
        foreach (KeyValuePair<string, int> entry in hitSources) {
            report.hitSources.Add(new NameCount { name = entry.Key,
                count = entry.Value });
        }
        report.metrics.Add(Summarize("MainThread", mainThread.Valid,
            mainThreadMs, 0f));
        foreach (Metric metric in recorders) {
            int calls = metric.callRecorder.Valid
                ? metric.callRecorder.Count - metric.callsAtStart : 0;
            float perFrame = recordedFrameTotal > 0
                ? calls / (float)recordedFrameTotal : 0f;
            report.metrics.Add(Summarize(metric.name,
                metric.frameRecorder.Valid, metric.perFrameMs, perFrame));
        }
        Directory.CreateDirectory(outputDir);
        string path = Path.Combine(outputDir, "results.json");
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        if (Debug.isDebugBuild) {
            Debug.Log("FallingCobraBenchmark:\twrote " + path
                + (error == null ? "" : ", error " + error));
        }
        Application.Quit(error == null ? 0 : 1);
    }
}
#endif
