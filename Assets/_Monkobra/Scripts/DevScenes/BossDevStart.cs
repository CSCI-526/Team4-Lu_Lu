using UnityEngine;

/// <summary>which boss a dev scene is built around</summary>
public enum Boss {
    CobraKing,
    SpiderKing
}

/// <summary>
/// Makes a dev scene play like the player's first meeting with one boss:
/// marks the tutorials a player would have met on the climb as seen, sets the
/// Loop the fight plays, and for the Spider King counts the Cobra King as
/// beaten, and ends the starting zone at once. The scene's own balance asset
/// puts the monkey's start a short approach below the boss's trigger, so its
/// fight opens after a short climb; nothing here moves the monkey.
/// Sits only in the dev scenes built by
/// <c>Monkobra > Build Boss Dev Scenes</c>, never in <c>GameplayScene</c>.
/// <para>
/// The seen set lives in <see cref="TutorialSkipManager"/> and is empty at
/// every launch, so the boss's own items show each time Play starts, and
/// no saved data is touched.
/// </para>
/// </summary>
public class BossDevStart: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("boss this scene is built around")]
    private Boss boss;

    [SerializeField]
    [Tooltip("the tutorials pre-seen per boss")]
    private BossDevStartConfig config;

    [SerializeField]
    [Min(0)]
    [Tooltip("Loop Override: Play opens this boss's fight at this Loop; 0 is "
        + "EASY, 1 Normal, 2 and up grown by the Loop Growth, w/ its hearts "
        + "and no lessons; the revive card keeps Normal Play's rule")]
    private int loopOverride = 1;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogError(
                "BossDevStart:\tmust assign Inspector Field: config",
                this
            );
            enabled = false;
            return;
        }

        // a boss scene is always the full run, whatever the last load chose
        RunModeSelection.Selected = RunMode.Full;
        MarkSeenItems();
    }

    // the Loop is set before the first live frame: the fight begins on it,
    // and a Loop set after its start would be reset by it
    private void Start() {
        ApplyLoopOverride();
    }

    private void Update() {
        // wait for the run to go live, the fight controller has to exist
        if (RunController.I == null || !RunController.I.IsRunLive) {
            return;
        }

        // the skip first: the Cobra King trigger fires only in Playing
        if (boss == Boss.SpiderKing) {
            CountCobraKingBeaten();
        }
        EndStartingZone();
        enabled = false;
    }

    // private methods  ########################################################
    // tells the skip manager the player already met these on the climb
    private void MarkSeenItems() {
        TutorialSkipManager skip = TutorialSkipManager.I;
        if (skip == null) {
            Debug.LogWarning(
                "BossDevStart:\tno TutorialSkipManager, tutorials not "
                    + "pre-seen",
                this
            );
            return;
        }

        var items = boss == Boss.CobraKing
            ? config.CobraKingSeenItems
            : config.SpiderKingSeenItems;
        foreach (TutorialItem item in items) {
            skip.MarkSeen(item);
        }
        // no lessons past Loop 1
        if (loopOverride >= 2) {
            skip.MarkSeen(TutorialItem.CobraKingLesson);
            skip.MarkSeen(TutorialItem.SpiderKingLesson);
            skip.MarkSeen(TutorialItem.ArmorWords);
        }
        if (Debug.isDebugBuild) {
            Debug.Log(
                $"BossDevStart:\t{boss} scene, {items.Count} items pre-seen",
                this
            );
        }
    }

    // the boss under test plays the Loop set on this component
    private void ApplyLoopOverride() {
        if (boss == Boss.CobraKing && CobraKingFightController.I != null) {
            CobraKingFightController.I.SetLoopOverride(loopOverride);
        } else if (boss == Boss.SpiderKing
            && SpiderKingFightController.I != null) {
            SpiderKingFightController.I.SetLoopOverride(loopOverride);
        }
        if (Debug.isDebugBuild) {
            Debug.Log($"BossDevStart:\t{boss} plays Loop {loopOverride}",
                this);
        }
    }

    // the dev scene has no starting zone: the run is a normal climb at once
    private void EndStartingZone() {
        RunController.I.EndStartingZone();
        if (Debug.isDebugBuild) {
            Debug.Log("BossDevStart:\tstarting zone ended", this);
        }
    }

    // a fight not begun yet is skipped as if beaten, the armor worn, so the
    // climb ends at the Spider King
    private void CountCobraKingBeaten() {
        CobraKingFightController cobraKing = CobraKingFightController.I;
        if (cobraKing == null) {
            Debug.LogWarning(
                "BossDevStart:\tno CobraKingFightController, Cobra King not "
                    + "skipped",
                this
            );
            return;
        }

        cobraKing.SkipForTest();
        if (Debug.isDebugBuild) {
            Debug.Log("BossDevStart:\tCobra King counted as beaten", this);
        }
    }
}
