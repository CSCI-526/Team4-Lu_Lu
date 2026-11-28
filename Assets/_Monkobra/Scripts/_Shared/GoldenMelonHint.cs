using UnityEngine;

/// <summary>
/// The first golden watermelon of a session gets a small plain popup with a
/// line to the melon. Both kings' arenas call <see cref="Show"/> as a golden
/// watermelon appears; the popup shows only while the monkey is within
/// <see cref="PlayerBalanceConfig.GoldenMelonPopUpRadiusU"/> of the melon, and
/// hides when it is out of range, hides itself, or is grabbed. The grab marks
/// <see cref="TutorialItem.GoldenWatermelonGrab"/> seen, so none shows again
/// that session (the dev skip is honored).
/// </summary>
[DisallowMultipleComponent]
public class GoldenMelonHint: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>Starts watching <paramref name="melon"/>: its popup shows once
    /// the monkey is close, unless a golden watermelon was grabbed already this
    /// session.</summary>
    /// <param name="melon">the golden watermelon the line points at</param>
    public static void Show(Transform melon) {
        TutorialSkipManager skip = TutorialSkipManager.I;
        if (melon == null || PopUpManager.I == null
            || (skip != null && skip.ShouldSkip(ITEM))) {
            return;
        }
        GoldenMelonHint hint = GetHint();
        hint.HidePopUp();
        hint.target = melon;
    }

    /// <summary>A golden watermelon was grabbed: it counts as taught & its
    /// popup hides.</summary>
    public static void Grabbed() {
        if (TutorialSkipManager.I != null) {
            TutorialSkipManager.I.MarkSeen(ITEM);
        }
        Hide();
    }

    /// <summary>Hides the popup & its line, & stops watching the
    /// melon.</summary>
    public static void Hide() {
        if (instance != null) {
            instance.HideNow();
        }
    }

    // MonoBehaviour Lifecycle  ################################################
    private void Update() {
        if (target == null) {
            return;
        }
        // the melon hid, or was grabbed or destroyed: the popup goes with it
        if (!target.gameObject.activeInHierarchy) {
            HideNow();
            return;
        }
        UpdateRange();
        if (isPopUpUp && link != null) {
            link.SetTarget(target);
        }
    }

    private void OnDestroy() {
        if (instance == this) {
            instance = null;
        }
    }

    // private members  ########################################################
    private const PopUpId POPUP = PopUpId.GoldenWatermelonFirst;
    private const TutorialItem ITEM = TutorialItem.GoldenWatermelonGrab;
    // the popup hides only this much farther than it shows, so it never
    // flickers at the edge of its range
    private const float HIDE_RANGE_SCALE = 1.1f;

    private static GoldenMelonHint instance;
    private PlayerBalanceConfig config;
    private PopUpWorldLink link;
    private Transform target;
    private bool isPopUpUp;
    private bool hasWarnedNoConfig;

    // private methods  ########################################################
    private static GoldenMelonHint GetHint() {
        if (instance == null) {
            instance = new GameObject("GoldenMelonHint")
                .AddComponent<GoldenMelonHint>();
        }
        return instance;
    }

    // the popup is up only while the monkey is near the melon
    private void UpdateRange() {
        MonkeyController monkey = MonkeyController.I;
        float radiusU = GetRadiusU();
        if (monkey == null || radiusU <= 0f) {
            HidePopUp();
            return;
        }
        float distanceU = Vector3.Distance(monkey.transform.position,
            target.position);
        if (isPopUpUp) {
            if (distanceU > radiusU * HIDE_RANGE_SCALE) {
                HidePopUp();
            }
        } else if (distanceU <= radiusU) {
            ShowPopUp();
        }
    }

    // the config is one loaded asset the monkey & the fruit prompt hold
    private float GetRadiusU() {
        if (config == null) {
            PlayerBalanceConfig[] loaded =
                Resources.FindObjectsOfTypeAll<PlayerBalanceConfig>();
            config = loaded.Length > 0 ? loaded[0] : null;
        }
        if (config == null && !hasWarnedNoConfig) {
            hasWarnedNoConfig = true;
            Debug.LogWarning(
                "GoldenMelonHint:\tno PlayerBalanceConfig loaded", this);
        }
        return config != null ? config.GoldenMelonPopUpRadiusU : 0f;
    }

    private void ShowPopUp() {
        if (PopUpManager.I == null) {
            return;
        }
        PopUpManager.I.Show(POPUP);
        MessagePopUp popUp = PopUpManager.I.GetPopUp(POPUP);
        link = popUp != null ? popUp.GetComponent<PopUpWorldLink>() : null;
        isPopUpUp = true;
    }

    private void HidePopUp() {
        if (!isPopUpUp) {
            return;
        }
        isPopUpUp = false;
        if (link != null) {
            link.SetTarget(null);
        }
        if (PopUpManager.I != null) {
            PopUpManager.I.Hide(POPUP);
        }
    }

    private void HideNow() {
        HidePopUp();
        target = null;
    }
}
