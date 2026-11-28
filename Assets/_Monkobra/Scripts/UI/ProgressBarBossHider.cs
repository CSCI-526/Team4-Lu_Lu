using UnityEngine;

// hides the right-hand distance indicator while a boss fight runs, where the
// arena has no climb to measure, and shows it again once the fight ends
// on the ProgressBar root, which stays active so its widget keeps going; a
// CanvasGroup hides the root's own track along w/ every child
[RequireComponent(typeof(CanvasGroup))]
public class ProgressBarBossHider: MonoBehaviour {
    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        group = GetComponent<CanvasGroup>();
    }

    private void OnEnable() {
        MonkeyHearts.Changed += OnHeartsChanged;
        OnHeartsChanged();
    }

    private void OnDisable() {
        MonkeyHearts.Changed -= OnHeartsChanged;
    }

    // private members  ########################################################
    private CanvasGroup group;

    // private methods  ########################################################
    private void OnHeartsChanged() {
        group.alpha = MonkeyHearts.IsActive ? 0f : 1f;
    }
}
