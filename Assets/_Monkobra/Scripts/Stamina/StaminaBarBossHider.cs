using UnityEngine;

// hides the stamina bar while a boss fight runs, where the monkey's hearts
// show instead, and shows it again once the fight ends
// on the StaminaBar root, which stays active: it only toggles its children
public class StaminaBarBossHider: MonoBehaviour {
    // MonoBehaviour Lifecycle  ################################################
    private void OnEnable() {
        MonkeyHearts.Changed += OnHeartsChanged;
    }

    private void OnDisable() {
        MonkeyHearts.Changed -= OnHeartsChanged;
    }

    // private methods  ########################################################
    private void OnHeartsChanged() {
        SetVisible(!MonkeyHearts.IsActive);
    }

    // the children switch, so the root, its slider & this script keep going
    private void SetVisible(bool isVisible) {
        foreach (Transform child in transform) {
            child.gameObject.SetActive(isVisible);
        }
    }
}
