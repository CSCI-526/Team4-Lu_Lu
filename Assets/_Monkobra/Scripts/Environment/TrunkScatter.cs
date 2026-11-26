using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One spot <see cref="TrunkScatter"/> picked on the trunk: where it sits and
/// how to retry it when something blocks it.
/// </summary>
public readonly struct ScatterItem {
    public ScatterItem(int index, float y, float yawDeg, uint retrySeed) {
        Index = index;
        Y = y;
        YawDeg = yawDeg;
        RetrySeed = retrySeed;
    }

    /// <summary>position in its channel's stream, from 0 at the stack
    /// bottom</summary>
    public readonly int Index;

    /// <summary>world y, fixed for the run; u</summary>
    public readonly float Y;

    /// <summary>first-choice angle around the trunk, 0~360; deg</summary>
    public readonly float YawDeg;

    /// <summary>per-item hash seed, for placers that need more random
    /// numbers b/t the same item and run</summary>
    public readonly uint RetrySeed;

    /// <param name="attempt">0 is the first choice, then golden angle
    /// steps</param>
    /// <returns>yaw of the given attempt, 0~360; deg</returns>
    public float GetRetryYawDeg(int attempt) {
        return Mathf.Repeat(
            YawDeg + attempt * ScatterStream.GOLDEN_ANGLE_DEG,
            360f
        );
    }
}

/// <summary>
/// Pure math of one channel: evenly spaced items along the trunk, their
/// count per stretch following a density curve. Needs no scene, so the report
/// tool runs it as is.
/// <para>
/// Cumulative placement: <c>C(y)</c> is the expected item count from the
/// stack bottom up to <c>y</c>. Item <c>i</c> sits where <c>C</c> reaches
/// <c>i + u + j_i</c>, with <c>u</c> a per-run offset and <c>j_i</c> a hashed
/// jitter. Height and yaw depend only on (seed, channel, i) and the density,
/// never on segment order or boundaries.
/// </para>
/// </summary>
public class ScatterStream {
    // Public Members  #########################################################
    /// <summary>yaw advance per item, so neighbours spiral round the
    /// trunk; deg</summary>
    public const float GOLDEN_ANGLE_DEG = 137.5f;

    /// <param name="seed">run seed</param>
    /// <param name="baseY">world y of the stack bottom, where C is 0</param>
    /// <param name="jitterFraction">spacing jitter, 0~0.95</param>
    /// <param name="yawJitterDeg">yaw wobble either way; deg</param>
    /// <param name="stepU">integration step; u</param>
    public ScatterStream(
        int seed,
        ScatterChannel channel,
        float baseY,
        float jitterFraction,
        float yawJitterDeg,
        float stepU
    ) {
        this.seed = seed;
        this.channel = channel;
        this.baseY = baseY;
        this.jitterFraction = Mathf.Clamp(jitterFraction, 0f, 0.95f);
        this.yawJitterDeg = Mathf.Max(0f, yawJitterDeg);
        this.stepU = Mathf.Max(0.05f, stepU);

        offset = Hash01(-1, SALT_OFFSET);
        yawPhaseDeg = Hash01(-1, SALT_PHASE) * 360f;
        cumulative.Add(0f);
    }

    /// <summary>
    /// Appends every item whose height lies in [bottomY, topY) to
    /// <paramref name="results"/>, lowest first. Neighbouring ranges share
    /// no item and leave no gap.
    /// </summary>
    /// <param name="densityAtY">expected items per
    /// <see cref="TrunkScatter.DENSITY_SPAN_U"/> at a world y; must stay the
    /// same for the run, the integral is memoized</param>
    public void GetItems(
        float bottomY,
        float topY,
        Func<float, float> densityAtY,
        List<ScatterItem> results
    ) {
        if (topY <= bottomY || topY <= baseY) {
            return;
        }
        bottomY = Mathf.Max(bottomY, baseY);

        float cumBottom = GetCumulative(bottomY, densityAtY);
        float cumTop = GetCumulative(topY, densityAtY);
        if (cumTop <= cumBottom) {
            return;
        }

        float halfJitter = jitterFraction * 0.5f;
        int firstIndex = Mathf.Max(
            0,
            Mathf.CeilToInt(cumBottom - offset - halfJitter)
        );
        int lastIndex = Mathf.FloorToInt(cumTop - offset + halfJitter);
        for (int i = firstIndex; i <= lastIndex; i++) {
            float target = i + offset + (Hash01(i, SALT_JITTER) - 0.5f)
                * jitterFraction;
            if (target < cumBottom || target >= cumTop) {
                continue;
            }
            results.Add(
                new ScatterItem(
                    i,
                    GetInverse(target),
                    GetYawDeg(i),
                    Hash(i, SALT_RETRY)
                )
            );
        }
    }

    // private members  ########################################################
    private const uint SALT_OFFSET = 1u;
    private const uint SALT_JITTER = 2u;
    private const uint SALT_PHASE = 3u;
    private const uint SALT_YAW = 4u;
    private const uint SALT_RETRY = 5u;

    private readonly int seed;
    private readonly ScatterChannel channel;
    private readonly float baseY;
    private readonly float jitterFraction;
    private readonly float yawJitterDeg;
    private readonly float stepU;
    private readonly float offset; // per-run, per-channel, 0~1
    private readonly float yawPhaseDeg;

    // C at baseY + k x stepU, grown upward as asked
    private readonly List<float> cumulative = new();

    // private methods  ########################################################
    private void ExtendTo(float topY, Func<float, float> densityAtY) {
        while (baseY + (cumulative.Count - 1) * stepU < topY) {
            int k = cumulative.Count - 1;
            float midY = baseY + (k + 0.5f) * stepU;
            float perU = Mathf.Max(0f, densityAtY(midY))
                / TrunkScatter.DENSITY_SPAN_U;
            cumulative.Add(cumulative[k] + perU * stepU);
        }
    }

    // expected item count from baseY up to y
    private float GetCumulative(float y, Func<float, float> densityAtY) {
        ExtendTo(y + stepU, densityAtY);
        float position = (y - baseY) / stepU;
        int k = Mathf.Min((int)position, cumulative.Count - 2);
        float fraction = position - k;
        return Mathf.Lerp(cumulative[k], cumulative[k + 1], fraction);
    }

    // lowest world y where C reaches target, inside the table already grown
    private float GetInverse(float target) {
        if (target <= 0f) {
            return baseY;
        }
        // first table entry at or above target; its predecessor is below
        int low = 1;
        int high = cumulative.Count - 1;
        while (low < high) {
            int mid = (low + high) >> 1;
            if (cumulative[mid] >= target) {
                high = mid;
            } else {
                low = mid + 1;
            }
        }
        float below = cumulative[low - 1];
        float span = cumulative[low] - below;
        float fraction = span > 0f
            ? Mathf.Clamp01((target - below) / span)
            : 0f;
        return baseY + (low - 1 + fraction) * stepU;
    }

    private float GetYawDeg(int index) {
        float wobble = (Hash01(index, SALT_YAW) * 2f - 1f) * yawJitterDeg;
        return Mathf.Repeat(
            yawPhaseDeg + index * GOLDEN_ANGLE_DEG + wobble,
            360f
        );
    }

    private static uint Mix(uint x) {
        unchecked {
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            x ^= x >> 16;
        }
        return x;
    }

    private uint Hash(int index, uint salt) {
        unchecked {
            uint h = Mix((uint)seed + 0x9e3779b9u * ((uint)channel + 1u));
            h = Mix(h ^ ((uint)index * 0x85ebca6bu));
            return Mix(h ^ (salt * 0xc2b2ae35u));
        }
    }

    // 0~1, 24 random bits
    private float Hash01(int index, uint salt) {
        return (Hash(index, salt) >> 8) * (1f / 16777216f);
    }
}

/// <summary>
/// Singleton on the tree pool object: the run's scatter seed and one
/// <see cref="ScatterStream"/> per <see cref="ScatterChannel"/>. Placers ask
/// it which spots of a channel fall in a height range, as a segment is
/// placed or restarted, and grow or skip them.
/// </summary>
// before the pool's Awake builds the stack, and every segment's Start
[DefaultExecutionOrder(-50)]
public class TrunkScatter: MonoBehaviour {
    // Public Members  #########################################################
    public static TrunkScatter I { get; private set; }

    /// <summary>expected-count unit of every density curve: items per this
    /// much trunk; u</summary>
    public const float DENSITY_SPAN_U = 10f;

    /// <returns>this run's seed</returns>
    public int Seed => seed;

    /// <returns>yaw retries of a blocked item before it is skipped</returns>
    public int MaxRetries => config != null ? config.MaxRetries : 0;

    /// <summary>
    /// Appends the channel's items with height in [bottomY, topY) to
    /// <paramref name="results"/>, lowest first.
    /// </summary>
    /// <param name="densityAtY">expected items per
    /// <see cref="DENSITY_SPAN_U"/> at a world y, eg a curve read at the
    /// ramped difficulty of that y</param>
    public void GetItems(
        ScatterChannel channel,
        float bottomY,
        float topY,
        Func<float, float> densityAtY,
        List<ScatterItem> results
    ) {
        if (!isReady) {
            return;
        }
        GetStream(channel).GetItems(bottomY, topY, densityAtY, results);
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("system tuning: jitter, yaw jitter, integration step, retries")]
    private TrunkScatterConfig config;

    [Header("Seed")]
    [SerializeField]
    [Tooltip("replay one tree every run, for tests")]
    private bool useFixedSeed;

    [SerializeField]
    [Tooltip("seed used when useFixedSeed is ticked")]
    private int fixedSeed = 12345;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "TrunkScatter:\tmust assign Inspector Field: config",
                this
            );
            enabled = false;
            return;
        }
        pool = GetComponent<DynamicTreeSegmentPool>();
        if (pool == null) {
            Debug.LogWarning(
                "TrunkScatter:\tneeds a DynamicTreeSegmentPool on the same "
                    + "object",
                this
            );
            enabled = false;
            return;
        }

        if (I != null && I != this) {
            Debug.LogWarning("TrunkScatter:\tduplicate, replacing", this);
        }
        I = this;
        seed = useFixedSeed ? fixedSeed : UnityEngine.Random.Range(
            int.MinValue,
            int.MaxValue
        );
        isReady = true;
        if (Debug.isDebugBuild) {
            Debug.Log(
                $"TrunkScatter:\tseed {seed}"
                    + (useFixedSeed ? " (fixed)" : "")
            );
        }
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
    }

    // private members  ########################################################
    private DynamicTreeSegmentPool pool;
    private bool isReady; // if Awake passed the Inspector guard
    private int seed;
    private ScatterStream[] streams;

    // private methods  ########################################################
    // built on first use, after the pool laid the stack out
    private ScatterStream GetStream(ScatterChannel channel) {
        if (streams == null) {
            streams = new ScatterStream[
                Enum.GetValues(typeof(ScatterChannel)).Length
            ];
        }
        int slot = (int)channel;
        streams[slot] ??= new ScatterStream(
            seed,
            channel,
            pool.StackBottomY,
            config.JitterFraction,
            config.YawJitterDeg,
            config.IntegrationStepU
        );
        return streams[slot];
    }
}
