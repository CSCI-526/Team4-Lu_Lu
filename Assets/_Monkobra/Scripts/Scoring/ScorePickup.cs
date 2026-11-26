using UnityEngine;

// one per object, so a pickup is never configured twice
[DisallowMultipleComponent]
public class ScorePickup: MonoBehaviour {
    // banana or coconut; picks the points & the counter in ScoreManager
    public PickupKind Kind => kind;

    public bool TryCollect() {
        if (!isActiveAndEnabled || isCollected || Time.timeScale <= 0f
            || (RunController.I != null
                && !RunController.I.IsRunLive)) {
            return false;
        }

        // refuse while disabled, already collected, paused, or the run is over
        ScoreManager manager = ScoreManager.I;
        if (manager != null && !manager.CanScore) {
            return false;
        }

        isCollected = true;
        if (manager != null) {
            manager.TryAddReward(kind);
        } else {
            Debug.LogWarning(
                "ScorePickup:\tno ScoreManager; collected without points",
                this);
        }
        gameObject.SetActive(false);
        return true;
    }

    [SerializeField]
    [Tooltip("banana keeps the old behavior, coconut scores & counts apart")]
    private PickupKind kind = PickupKind.Banana;

    private bool isCollected;

    private void OnEnable() {
        isCollected = false;
    }
}
