using UnityEngine;

// hides the score and height HUD while the boss fight runs, shows it again
// once the king is beaten and the climb goes on
// on the ScoreHeight root, which stays active: it only toggles its children
public class ScoreHeightBossHider: MonoBehaviour {
    // MonoBehaviour Lifecycle  ################################################
    // Start, not OnEnable: the boss controller's singleton is set in Awake
    private void Start() {
        boss = BossFightController.I;
        if (boss == null) {
            return;
        }
        boss.FightStarted += OnFightStarted;
        boss.FightEnded += OnFightEnded;
    }

    private void OnDestroy() {
        if (boss != null) {
            boss.FightStarted -= OnFightStarted;
            boss.FightEnded -= OnFightEnded;
        }
    }

    // private members  ########################################################
    private BossFightController boss;

    // private methods  ########################################################
    private void OnFightStarted() {
        SetVisible(false);
    }

    private void OnFightEnded() {
        SetVisible(true);
    }

    // the children switch, so the root and this script keep listening
    private void SetVisible(bool isVisible) {
        foreach (Transform child in transform) {
            child.gameObject.SetActive(isVisible);
        }
    }
}
