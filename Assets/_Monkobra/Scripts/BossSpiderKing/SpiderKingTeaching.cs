using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>The first-fight lesson that teaches the parry.</summary>
public sealed class SpiderKingTeaching {
    // private members  ########################################################
    private readonly SpiderKingRig rig;
    private readonly SpiderKingConfig config;

    public SpiderKingTeaching(SpiderKingRig rig) {
        this.rig = rig;
        config = rig.config;
    }

    // Public Methods  #########################################################
    // Teaching  ---------------------------------------------------------------
    // the one lesson, the parry: slowed waves till one is batted back; no
    // other attack meanwhile & a lesson never kills (only the guard below)
    public float UpdateTeach() {
        rig.teachTimeS += Time.deltaTime;
        if (rig.teach == SpiderKingTeachStep.Choose) {
            rig.Arena.UpdateChoiceTags();
            return 0f;
        }
        bool isTrapped = rig.struggle != null && rig.struggle.IsTrapped;
        if (isTrapped) {
            rig.nextAttackTime = Time.time + config.TeachGapS;
            return 0f;
        }
        bool isFree = !rig.HasLiveShots;
        switch (rig.teach) {
            case SpiderKingTeachStep.Parry:
                if (Time.time >= rig.nextAttackTime && isFree) {
                    rig.Attacks.BeginAttack(SpiderKingAttack.Web, true);
                }
                return 0f;
            case SpiderKingTeachStep.Slash:
                bool isSlashFree = !rig.isSlashing
                    && !rig.crescents.Exists(c => !c.isReflected);
                if (Time.time >= rig.nextAttackTime && isFree && isSlashFree) {
                    Teach("Bounce it back too!", "", config.WarnColor, 0f);
                    rig.Attacks.StartSlash(true);
                }
                return 0f;
            case SpiderKingTeachStep.Final:
            case SpiderKingTeachStep.Ready:
                float waitS = rig.teach == SpiderKingTeachStep.Ready
                    ? config.ReadyS : config.FinalS;
                if (rig.teachTimeS >= waitS) {
                    rig.teach = SpiderKingTeachStep.Done;
                    rig.hud.HideBanner();
                    rig.nextAttackTime = Time.time + 1.5f;
                }
                return 0f;
        }
        return 0f;
    }

    public void AdvanceTeach(SpiderKingTeachStep next) {
        rig.teach = next;
        rig.teachTimeS = 0f;
        rig.nextAttackTime = Time.time + config.TeachGapS;
        rig.hud.SetPrompt(false, Vector3.zero);
        ShowStepBanner();
    }

    public void ShowStepBanner() {
        switch (rig.teach) {
            case SpiderKingTeachStep.Parry:
                Teach(config.BossName, "", config.WarnColor, 2f);
                break;
        }
    }

    public void Teach(string title, string line, Color color, float seconds) {
        rig.hud.ShowBannerFor(title, line, color, seconds);
    }

}
