using UnityEngine;

[DisallowMultipleComponent]
//同一个对象不能重复挂两个 ScorePickup，避免配置重复
public class ScorePickup: MonoBehaviour {
    // banana or coconut; picks the points & the counter in ScoreManager
    public PickupKind Kind => kind;

    public bool TryCollect() {
        if (!isActiveAndEnabled || isCollected || Time.timeScale <= 0f
            || (GameController.I != null
                && !GameController.I.IsRunLive)) {
            return false;
        }//组件没启用、已经领过、游戏暂停或结束，都会拒绝领取
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
