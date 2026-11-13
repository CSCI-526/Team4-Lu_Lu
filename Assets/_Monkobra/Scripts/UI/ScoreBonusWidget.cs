using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// shows a short "+N" beside the score for every reward ScoreManager grants
// popups come from a pool of ScoreBonusItem prefabs, so many can show at once
public class ScoreBonusWidget: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("popup prefab: a plus text and a number text")]
    private ScoreBonusItem itemPrefab;

    [SerializeField]
    [Min(0)]
    [Tooltip("popups made at start, so a burst needs no Instantiate")]
    private int prewarmCount = 6;

    [SerializeField]
    [Min(1)]
    [Tooltip("most popups showing at once; the oldest is recycled past it")]
    private int maxActive = 12;

    [SerializeField]
    [Tooltip("where a popup starts, from this object's anchor; UI units")]
    private Vector2 startPosition = new Vector2(0f, -20f);

    [SerializeField]
    [Tooltip("min UI units b/t 2 popups; an older popup is pushed up to keep it")]
    private float stackSpacing = 44f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (itemPrefab == null) {
            Debug.LogError(
                "ScoreBonusWidget:\tmust assign Inspector Field: itemPrefab",
                this);
            enabled = false;
            return;
        }
        pool = new ObjectPool<ScoreBonusItem>(
            CreateItem,
            OnGetItem,
            OnReleaseItem,
            OnDestroyItem,
            false,
            Mathf.Max(prewarmCount, 1),
            Mathf.Max(maxActive, 1)
        );
        // build the first few now, so the first grabs do not hitch
        List<ScoreBonusItem> warm = new List<ScoreBonusItem>();
        for (int i = 0; i < prewarmCount; i++) {
            warm.Add(pool.Get());
        }
        foreach (ScoreBonusItem item in warm) {
            pool.Release(item);
        }
    }

    private void OnEnable() {
        scoreManager = ScoreManager.I;
        if (scoreManager == null) {
            return;
        }
        scoreManager.RewardAdded += OnRewardAdded;
    }

    private void OnDisable() {
        if (scoreManager != null) {
            scoreManager.RewardAdded -= OnRewardAdded;
            scoreManager = null;
        }
        ReleaseAll();
    }

    // scaled time: a pause freezes every popup mid fade
    private void Update() {
        // a lost run clears what is left
        if (scoreManager != null && scoreManager.IsRunEnded) {
            ReleaseAll();
            return;
        }
        for (int i = active.Count - 1; i >= 0; i--) {
            ScoreBonusItem item = active[i];
            item.Tick(Time.deltaTime);
            if (item.IsFinished) {
                active.RemoveAt(i);
                pool.Release(item);
            }
        }
    }

    // private members  ########################################################
    private readonly List<ScoreBonusItem> active = new List<ScoreBonusItem>();
    private ObjectPool<ScoreBonusItem> pool;
    private ScoreManager scoreManager;

    // private methods  ########################################################
    private void OnRewardAdded(RewardKind kind, long points) {
        if (points <= 0 || pool == null) {
            return;
        }
        // past the cap the oldest popup makes way, a reward is never skipped
        if (active.Count >= maxActive) {
            ScoreBonusItem oldest = active[0];
            active.RemoveAt(0);
            pool.Release(oldest);
        }
        MakeRoomForNewest();
        ScoreBonusItem item = pool.Get();
        item.Begin(kind, points, startPosition);
        active.Add(item);
    }

    // push older popups up so none sits closer than stackSpacing to the next
    private void MakeRoomForNewest() {
        float floor = stackSpacing;
        for (int i = active.Count - 1; i >= 0; i--) {
            ScoreBonusItem item = active[i];
            float rise = item.Rise;
            if (rise < floor) {
                item.Push(floor - rise);
                rise = floor;
            }
            floor = rise + stackSpacing;
        }
    }

    private void ReleaseAll() {
        if (pool == null) {
            return;
        }
        for (int i = active.Count - 1; i >= 0; i--) {
            // a scene teardown may have destroyed the item already
            if (active[i] != null) {
                pool.Release(active[i]);
            }
        }
        active.Clear();
    }

    private ScoreBonusItem CreateItem() {
        ScoreBonusItem item = Instantiate(itemPrefab, transform);
        item.name = "ScoreBonusItem";
        return item;
    }

    private void OnGetItem(ScoreBonusItem item) {
        item.gameObject.SetActive(true);
    }

    private void OnReleaseItem(ScoreBonusItem item) {
        item.gameObject.SetActive(false);
    }

    private void OnDestroyItem(ScoreBonusItem item) {
        Destroy(item.gameObject);
    }
}
