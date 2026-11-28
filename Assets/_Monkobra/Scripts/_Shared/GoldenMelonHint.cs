using UnityEngine;

/// <summary>
/// The first golden watermelon of a session gets the pineapple's kind of
/// popup: the fruit icon, short text, the hold hint, and a line to the melon.
/// Both kings' arenas call <see cref="Show"/> as a golden watermelon appears;
/// the popup is held while the melon is up & hides when it hides or is
/// grabbed. The grab marks <see cref="TutorialItem.GoldenWatermelonGrab"/>
/// seen, so none shows again that session (the dev skip is honored).
/// </summary>
[DisallowMultipleComponent]
public class GoldenMelonHint: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>Shows the popup & links it to <paramref name="melon"/>, unless
    /// a golden watermelon was grabbed already this session.</summary>
    /// <param name="melon">the golden watermelon the line points at</param>
    public static void Show(Transform melon) {
        TutorialSkipManager skip = TutorialSkipManager.I;
        if (melon == null || PopUpManager.I == null
            || (skip != null && skip.ShouldSkip(ITEM))) {
            return;
        }
        GoldenMelonHint hint = GetHint();
        PopUpManager.I.Show(POPUP);
        MessagePopUp popUp = PopUpManager.I.GetPopUp(POPUP);
        hint.link = popUp != null
            ? popUp.GetComponent<PopUpWorldLink>() : null;
        hint.target = melon;
        hint.isShown = true;
    }

    /// <summary>A golden watermelon was grabbed: it counts as taught & its
    /// popup hides.</summary>
    public static void Grabbed() {
        if (TutorialSkipManager.I != null) {
            TutorialSkipManager.I.MarkSeen(ITEM);
        }
        Hide();
    }

    /// <summary>Hides the popup & its line.</summary>
    public static void Hide() {
        if (instance != null) {
            instance.HideNow();
        }
    }

    // MonoBehaviour Lifecycle  ################################################
    private void Update() {
        if (!isShown) {
            return;
        }
        // the melon hid, or was grabbed or destroyed: the popup goes with it
        if (target == null || !target.gameObject.activeInHierarchy) {
            HideNow();
            return;
        }
        if (link != null) {
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

    private static GoldenMelonHint instance;
    private PopUpWorldLink link;
    private Transform target;
    private bool isShown;

    // private methods  ########################################################
    private static GoldenMelonHint GetHint() {
        if (instance == null) {
            instance = new GameObject("GoldenMelonHint")
                .AddComponent<GoldenMelonHint>();
        }
        return instance;
    }

    private void HideNow() {
        if (!isShown) {
            return;
        }
        isShown = false;
        if (link != null) {
            link.SetTarget(null);
        }
        if (PopUpManager.I != null) {
            PopUpManager.I.Hide(POPUP);
        }
    }
}
