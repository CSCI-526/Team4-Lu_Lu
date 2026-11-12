using System;
using UnityEngine;
//记录高度分和奖励分，在分数变化时通知 UI，并在游戏结束后停止计分
// run before UI scripts, so ScoreManager.I is set when they enable
[DefaultExecutionOrder(-100)]
public class ScoreManager: MonoBehaviour {
    // Public Members  #########################################################
    public static ScoreManager I { get; private set; }
    public long TotalScore => DistanceScore + RewardScore;
    public long DistanceScore { get; private set; }
    public long RewardScore { get; private set; }
    public int BananaCollectedCount { get; private set; }
    public int CoconutCollectedCount { get; private set; }
    // pineapple grabs, counted when each leap starts
    public int PineappleCollectedCount { get; private set; }
    // part of RewardScore that came from one kind of reward
    public long RewardScoreOf(RewardKind kind) {
        return rewardScoreByKind[(int)kind];
    }
    public double HighestClimbedMeters => highestClimbedUnits / unitsPerMeter;
    public bool IsRunEnded => isRunEnded;
    public event Action<long> ScoreChanged;
    // raised once per banana pickup that scored
    public event Action BananaCollected;
    // raised once per coconut pickup that scored
    public event Action CoconutCollected;
    // raised once per scored pickup with the points that pickup just added
    public event Action<RewardKind, long> RewardAdded;

    //必须满足：组件启用、已经开局、尚未结束、没有暂停，才允许计分
    public bool CanScore => isActiveAndEnabled && hasStarted && !isRunEnded && Time.timeScale > 0f && (GameController.I == null || GameController.I.IsRunLive);

    // Public Methods  #########################################################
    //添加水果等奖励分
    public bool TryAddReward(PickupKind kind = PickupKind.Banana) {
        if (!CanScore) {
            return false;
        }

        long oldScore = TotalScore;
        if (kind == PickupKind.Coconut) {
            // a coconut is worth a multiple of the banana points
            long coconutPoints = Math.Min((long)playerBalanceConfig.CoconutRewardPoints, long.MaxValue - TotalScore);
            RewardScore += coconutPoints;
            rewardScoreByKind[(int)RewardKind.Coconut] += coconutPoints;
            CoconutCollectedCount++;
            NotifyIfChanged(oldScore);
            RewardAdded?.Invoke(RewardKind.Coconut, coconutPoints);
            CoconutCollected?.Invoke();
            return true;
        }

        long bananaPoints = Math.Min((long)playerBalanceConfig.ScoreRewardPoints, long.MaxValue - TotalScore);
        RewardScore += bananaPoints;
        rewardScoreByKind[(int)RewardKind.Banana] += bananaPoints;
        // every other reward is a banana pickup
        BananaCollectedCount++;
        NotifyIfChanged(oldScore);
        RewardAdded?.Invoke(RewardKind.Banana, bananaPoints);
        BananaCollected?.Invoke();
        return true;
    }

    // awards points for a good deed that is not a fruit pickup, once per call
    public bool AddReward(RewardKind kind, int points) {
        if (!CanScore || points <= 0) {
            return false;
        }

        long oldScore = TotalScore;
        long added = Math.Min((long)points, long.MaxValue - TotalScore);
        RewardScore += added;
        rewardScoreByKind[(int)kind] += added;
        NotifyIfChanged(oldScore);
        RewardAdded?.Invoke(kind, added);
        return true;
    }

    public void EndRun() {
        if (!hasStarted || isRunEnded) {
            return;
        }
        isRunEnded = true;
        SampleHeight();
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("player balance tuning; scoring rates & reward points")]
    private PlayerBalanceConfig playerBalanceConfig;

    [SerializeField]
    [Tooltip("monkey; height source")]
    private Transform player;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (playerBalanceConfig == null) {
            Debug.LogError(
                "ScoreManager:\tmust assign Inspector Field: playerBalanceConfig",
                this);
        }
        if (player == null) {
            Debug.LogError(
                "ScoreManager:\tmust assign Inspector Field: player", this);
        }
        if (I != null && I != this) {
            enabled = false;
            Destroy(this);
            return;
        }
        if (playerBalanceConfig == null || player == null) {
            enabled = false;
            return;
        }
        I = this;
    }

    //读取计分设置，记录出生高度，把分数和最高爬升距离清零
    private void Start() {
        pointsPerMeter = playerBalanceConfig.PointsPerMeter;
        unitsPerMeter = playerBalanceConfig.UnitsPerMeter;
        highestClimbedUnits = 0d;
        DistanceScore = 0;
        RewardScore = 0;
        BananaCollectedCount = 0;
        CoconutCollectedCount = 0;
        PineappleCollectedCount = 0;
        Array.Clear(rewardScoreByKind, 0, rewardScoreByKind.Length);
        leapController = FindFirstObjectByType<PineappleLeapController>();
        if (leapController != null) {
            leapController.Begun += OnPineappleLeapBegun;
        }
        coconutManager = FindFirstObjectByType<CoconutManager>();
        if (coconutManager != null) {
            coconutManager.Frozen += OnCobraFrozen;
        }
        markerSystem = FindFirstObjectByType<HeightMarkerSystem>();
        if (markerSystem != null) {
            markerSystem.MarkerReached += OnMarkerReached;
        }
        webStruggle = FindFirstObjectByType<SpiderWebStruggle>();
        if (webStruggle != null) {
            webStruggle.Escaped += OnWebEscaped;
        }
        isRunEnded = GameController.I != null
            && GameController.I.State == GameState.Lost;
        hasStarted = true;
        ScoreChanged?.Invoke(TotalScore);
    }

    private void FixedUpdate() {
        if (CanScore) {
            SampleHeight();
        }
    }

    private void LateUpdate() {
        if (CanScore) {
            SampleHeight();
        }
    }

    private void OnDestroy() {
        if (leapController != null) {
            leapController.Begun -= OnPineappleLeapBegun;
        }
        if (coconutManager != null) {
            coconutManager.Frozen -= OnCobraFrozen;
        }
        if (markerSystem != null) {
            markerSystem.MarkerReached -= OnMarkerReached;
        }
        if (webStruggle != null) {
            webStruggle.Escaped -= OnWebEscaped;
        }
        if (I == this) {
            I = null;
        }
    }

    // private members  ########################################################
    private readonly long[] rewardScoreByKind =
        new long[Enum.GetValues(typeof(RewardKind)).Length];
    private bool hasStarted;
    private bool isRunEnded;
    private double highestClimbedUnits;
    private double unitsPerMeter = 1d;
    private int pointsPerMeter;
    private PineappleLeapController leapController;
    private CoconutManager coconutManager;
    private HeightMarkerSystem markerSystem;
    private SpiderWebStruggle webStruggle;

    // private methods  ########################################################
    // each pineapple grab starts 1 leap
    private void OnPineappleLeapBegun() {
        if (hasStarted && !isRunEnded) {
            PineappleCollectedCount++;
            AddReward(
                RewardKind.Pineapple, playerBalanceConfig.PineappleRewardPoints);
        }
    }

    // a coconut froze the great cobra
    private void OnCobraFrozen(float _) {
        AddReward(
            RewardKind.CobraFreeze, playerBalanceConfig.CobraFreezeRewardPoints);
    }

    // the marker system already raises this once per new, higher marker
    private void OnMarkerReached(int _) {
        AddReward(
            RewardKind.HeightMarker,
            playerBalanceConfig.HeightMarkerRewardPoints);
    }

    private void OnWebEscaped() {
        AddReward(
            RewardKind.WebEscape, playerBalanceConfig.WebEscapeRewardPoints);
    }

    private void SampleHeight() {
        if (RampedDifficultyManager.I == null) {
            return;
        }
        double climbedUnits =
            RampedDifficultyManager.I.CurrentPlayerClimbedYDistance;
        //如果数值无效，或者没超过之前的最高高度，就直接退出。不能刷分
        if (double.IsNaN(climbedUnits) || double.IsInfinity(climbedUnits)
            || climbedUnits <= highestClimbedUnits) {
            return;
        }

        long oldScore = TotalScore;
        highestClimbedUnits = climbedUnits;
        double points = Math.Floor(HighestClimbedMeters) * pointsPerMeter;
        //高度分 = 最高爬升米数向下取整 × 每米分值
        long available = long.MaxValue - RewardScore;
        DistanceScore = points >= available ? available : (long)points;
        NotifyIfChanged(oldScore);
    }

    private void NotifyIfChanged(long oldScore) {
        if (TotalScore != oldScore) {
            ScoreChanged?.Invoke(TotalScore);
        }//通知 UI
    }
}
