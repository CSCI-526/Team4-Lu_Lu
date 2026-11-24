using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>Hits, throw-offs, touch warnings, and the armor spent.</summary>
public sealed class SpiderKingDamage {
    // private members  ########################################################
    private readonly SpiderKingRig rig;
    private readonly SpiderKingTuning tuning;

    public SpiderKingDamage(SpiderKingRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    public static void GiveArmor() {
        CobraKingFightController cobraKing = CobraKingFightController.I;
        if (cobraKing != null) {
            cobraKing.GiveArmor();
        }
    }

    public static bool HasArmor() {
        CobraKingFightController cobraKing = CobraKingFightController.I;
        return cobraKing != null && cobraKing.HasArmor;
    }

    // a melon or a batted back slash
    public void Hurt(int damage) {
        if (rig.phase != SpiderKingPhase.Fighting || damage <= 0) {
            return;
        }
        rig.health = Mathf.Max(0, rig.health - damage);
        rig.onSpiderHit?.Invoke();
        rig.hud.SetHealth(rig.health, rig.maxHealth);
        rig.hud.PopDamage("-" + damage, rig.body.HeadPosition);
        rig.Parry.Burst(rig.body.HeadPosition, new Color(0.9f, 0.2f, 0.25f),
            10);
        if (rig.health <= 0) {
            rig.Ending.StartFinisher(true);
            return;
        }
        int stage = rig.Stage;
        if (stage > rig.lastStage) {
            // a new stage: a roar of red rings, its eyes blazing
            rig.lastStage = stage;
            // a stage of volleys opens w/ at least two of them, no slash
            // till then
            int volley =
                SpiderKingMovement.PerStage(tuning.WebShotsPerStage, stage);
            if (volley >= 2) {
                rig.websBeforeSlash = VOLLEYS_FIRST;
            }
            rig.Parry.AddRing(rig.body.HeadPosition, tuning.WarnColor, 0f);
            rig.Parry.AddRing(rig.body.HeadPosition, tuning.WarnColor, 0.1f);
            rig.Parry.AddRing(rig.body.HeadPosition, tuning.WarnColor, 0.2f);
            // its volleys grow: called out once, from its head up to under
            // the health bar
            if (volley >= 2) {
                rig.hud.CallOut(volley >= 3 ? "Triple fire ready!"
                    : "Rapid fire ready!", rig.body.HeadPosition,
                        tuning.WarnColor);
                rig.Movement.HoldForCallOut();
            } else {
                rig.hud.Say(stage == 1 ? "You'll pay for that!" : "ENOUGH!");
            }
            rig.Parry.BurstStreaks(rig.body.HeadPosition, tuning.WarnColor, 18,
                4f, 10f);
            return;
        }
        // no word on a plain hit; only low on health it begs
        if (rig.health <= rig.maxHealth / 3) {
            rig.hud.Say("No! Stop it!");
        }
    }

    public void LethalHit(string word, bool isTouch = false,
        bool isDash = false) {
        if (rig.phase != SpiderKingPhase.Fighting || rig.isReturningToStart
            || Time.time < rig.saveGraceEndTime) {
            return;
        }
        // till the first parry is learnt nothing of the spider's hurts
        if (rig.teach == SpiderKingTeachStep.Parry) {
            return;
        }
        // a touch shows it must not be touched; saved, w/ its words
        if (isTouch) {
            ShowTouchWarning(HasArmor());
        }
        // the armor saves as in the Cobra King's arena: "Saved!" & the usual
        // knock down; the spider goes on
        if (TrySpendArmor()) {
            if (rig.mode == SpiderKingMode.Cocooning) {
                rig.Movement.SetMode(SpiderKingMode.Idle);
                rig.nextAttackTime = Time.time + rig.Movement.RestNow();
            }
            if (isDash) {
                ThrowOffDash();
            } else if (isTouch) {
                ThrowOffSpider();
            } else {
                SpiderKingFeedback.KnockDown(rig.body.Contact);
            }
            return;
        }
        rig.Ending.StartFinisher(false);
    }

    // it must never be touched: it bites, red rings & sparks burst off the
    // monkey & "Don't touch it!" shows
    public void ShowTouchWarning(bool isSaved) {
        rig.biteEndTime = Time.unscaledTime + BITE_S;
        Vector3 at = rig.monkey.position;
        rig.Parry.AddRing(at, tuning.WarnColor, 0f);
        rig.Parry.AddRing(at, tuning.WarnColor, 0.08f);
        rig.Parry.BurstStreaks(at, tuning.WarnColor, 16, 5f, 12f);
        if (isSaved) {
            rig.hud.PopText("Don't touch it!", rig.body.HeadPosition,
                tuning.WarnColor);
        }
    }

    // saved from a touch: the monkey is thrown well off the spider, out of
    // any web, untouchable on the way
    public void ThrowOffSpider() {
        if (rig.monkeyController == null) {
            return;
        }
        if (rig.struggle != null) {
            rig.struggle.ForceRelease();
        }
        Vector3 at = rig.monkey.position;
        float awayDeg = Mathf.DeltaAngle(rig.spiderAngle, rig.AngleOf(at));
        float sign = awayDeg >= 0f ? 1f : -1f;
        float throwDeg = TOUCH_THROW_U / tuning.TrunkRadiusU * Mathf.Rad2Deg;
        float y = at.y + Mathf.Sign(at.y - rig.spiderY) * 2f;
        rig.returnTarget = rig.PointOnTrunk(rig.AngleOf(at) + sign * throwDeg,
            Mathf.Clamp(y, rig.entryY, rig.entryY + tuning.ArenaHeightU),
            rig.HorizontalDistance(at));
        SpiderKingFeedback.SetUntouchable(true);
        rig.monkeyController.ClearStun();
        rig.monkeyController.IsHeld = true;
        rig.monkeyController.PullTo(rig.returnTarget,
            tuning.ReviveReturnSpeedU);
        rig.isReturningToStart = true;
    }

    // saved from a dash: thrown out sideways off its lane, the side it was
    // caught on (the other if the arena's top or floor is in the way), so the
    // spider running on along it never touches it again
    public void ThrowOffDash() {
        if (rig.monkeyController == null) {
            return;
        }
        if (rig.struggle != null) {
            rig.struggle.ForceRelease();
        }
        Vector3 at = rig.monkey.position;
        float mu = Mathf.DeltaAngle(rig.dashStartAngle, rig.AngleOf(at))
            * Mathf.Deg2Rad * tuning.TrunkRadiusU;
        float mv = at.y - rig.dashStartY;
        float side = rig.dashWayU * mv - rig.dashWayV * mu >= 0f ? 1f : -1f;
        float lowY = rig.entryY;
        float highY = rig.entryY + tuning.ArenaHeightU - 0.5f;
        float sideY = at.y + side * rig.dashWayU * DASH_THROW_U;
        // too little room that way: the other side
        if (sideY < lowY || sideY > highY) {
            float otherY = at.y - side * rig.dashWayU * DASH_THROW_U;
            if (otherY >= lowY && otherY <= highY) {
                side = -side;
            }
        }
        float throwU = -side * rig.dashWayV * DASH_THROW_U;
        float throwV = side * rig.dashWayU * DASH_THROW_U;
        rig.returnTarget = rig.PointOnTrunk(
            rig.AngleOf(at) + throwU / tuning.TrunkRadiusU * Mathf.Rad2Deg,
            Mathf.Clamp(at.y + throwV, lowY, highY),
            rig.HorizontalDistance(at));
        SpiderKingFeedback.SetUntouchable(true);
        rig.monkeyController.ClearStun();
        rig.monkeyController.IsHeld = true;
        rig.monkeyController.PullTo(rig.returnTarget,
            tuning.ReviveReturnSpeedU);
        rig.isReturningToStart = true;
    }

    // the Cobra King's leftover armor, if worn; frees the monkey too
    public bool TrySpendArmor() {
        CobraKingFightController cobraKing = CobraKingFightController.I;
        if (cobraKing == null || !cobraKing.TrySpendArmor()) {
            return false;
        }
        rig.saveGraceEndTime = Time.time + tuning.SaveGraceS;
        // the golden melon waits a short while after the armor goes, so it
        // never pops up at once beside the monkey, eg back at the start;
        // after that, unpicked, its full interval
        rig.nextGoldenTime = Time.time;
        rig.goldenSinceArmor = 0;
        if (rig.struggle != null) {
            rig.struggle.ForceRelease();
        }
        return true;
    }

}
