using UnityEngine;

/// <summary>
/// The Spider King fight's tutorial popups, each w/ the animated key icon of
/// the control style in use (the popup entity's <see cref="HintIconWidget"/>):
/// the tail parry on the first incoming shot, the melon throw once the
/// lessons are done, the dash dodge on the first dash, & the web escape on
/// the first trap. Each is a <see cref="TutorialItem"/> asked of the skip
/// manager, so it shows once a session & the dev skip box hides them all.
/// <para>
/// Polled each frame by the fight controller, which tells it whether the
/// fight is on: a tip shows on its first moment & hides as that moment ends
/// or its action is done.
/// </para>
/// </summary>
public sealed class SpiderKingTips {
    // private members  ########################################################
    private enum Tip {
        Parry,
        Melon,
        Dash,
        WebEscape,
    }

    private static readonly PopUpId[] IDS = {
        PopUpId.SpiderTipParry, PopUpId.SpiderTipMelon,
        PopUpId.SpiderTipDash, PopUpId.SpiderTipWebEscape,
    };

    private static readonly TutorialItem[] ITEMS = {
        TutorialItem.SpiderTipParry, TutorialItem.SpiderTipMelon,
        TutorialItem.SpiderTipDash, TutorialItem.SpiderTipWebEscape,
    };

    private readonly SpiderKingRig rig;
    private readonly bool[] isShowing = new bool[IDS.Length];
    private bool hasGrabbedMelon; // the melon throw is learnt

    public SpiderKingTips(SpiderKingRig rig) {
        this.rig = rig;
    }

    // Public Methods  #########################################################
    /// <summary>The player grabbed a melon, so its tip is done.</summary>
    public void NotifyMelonGrabbed() {
        hasGrabbedMelon = true;
    }

    /// <summary>Shows & hides the tips for this frame.</summary>
    /// <param name="isFighting">whether the fight is on, else every tip
    /// hides</param>
    public void Update(bool isFighting) {
        bool isShotComing = rig.shots.Exists(s => !s.isReflected)
            || rig.crescents.Exists(c => !c.isReflected) || rig.isSlashing;
        bool isTrapped = rig.struggle != null && rig.struggle.IsTrapped;
        bool hasSwungTail = rig.tailTimeS >= 0f;

        Sync(Tip.Parry, isFighting && isShotComing && !hasSwungTail);
        Sync(Tip.Melon, isFighting && !hasGrabbedMelon
            && rig.teach == SpiderKingTeachStep.Done);
        Sync(Tip.Dash, isFighting && rig.attack == SpiderKingAttack.Dash);
        Sync(Tip.WebEscape, isFighting && isTrapped);
    }

    // private methods  ########################################################
    // a tip shows when wanted & not skipped, & hides when no more wanted
    private void Sync(Tip tip, bool isWanted) {
        PopUpManager popUps = PopUpManager.I;
        if (popUps == null) {
            return;
        }
        int index = (int)tip;
        if (isShowing[index]) {
            if (!isWanted) {
                popUps.Hide(IDS[index]);
                isShowing[index] = false;
            }
            return;
        }
        if (!isWanted) {
            return;
        }
        TutorialSkipManager skip = TutorialSkipManager.I;
        if (skip != null && skip.ShouldSkip(ITEMS[index])) {
            return;
        }
        if (skip != null) {
            skip.MarkSeen(ITEMS[index]);
        }
        popUps.Show(IDS[index]);
        isShowing[index] = true;
    }
}
