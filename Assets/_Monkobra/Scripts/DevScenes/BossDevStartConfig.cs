using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Numbers and first-time state for the boss dev scenes: how far down each
/// king's section of the tree is moved, so the monkey meets the boss soon
/// after its normal start, and which tutorials count as already seen, so a
/// scene plays like the player's first meeting with that boss.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Objects/BossDevStartConfig")]
public class BossDevStartConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>climbed height the Cobra King's zone starts at in the dev
    /// scenes; its trigger and zone end keep their distance from it; u
    /// </returns>
    public float CobraZoneStartU => cobraZoneStartU;

    /// <returns>climbed height the Spider King's zone starts at in the dev
    /// scenes; its trigger, jump height and zone end keep their distance
    /// from it; u</returns>
    public float SpiderZoneStartU => spiderZoneStartU;

    /// <returns>tutorials marked seen at the Cobra King scene start, ie
    /// what a player has met on the climb</returns>
    public IReadOnlyList<TutorialItem> CobraKingSeenItems =>
        cobraKingSeenItems;

    /// <returns>tutorials marked seen at the Spider King scene start, ie
    /// the climb and the beaten Cobra King</returns>
    public IReadOnlyList<TutorialItem> SpiderKingSeenItems =>
        spiderKingSeenItems;

    // Inspector Fields  #######################################################
    [Header("Boss Heights")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("where the Cobra King zone starts, climbed height; keep it "
        + "above the starting zone exit; u")]
    private float cobraZoneStartU = 40f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("where the Spider King zone starts, climbed height; keep its "
        + "jump height (25 below) above the Cobra King zone end; u")]
    private float spiderZoneStartU = 140f;

    [Header("Cobra King Scene")]
    [SerializeField]
    [Tooltip("seen at start; leave out ArmorWords and CobraKingLesson so "
        + "the scene shows them")]
    private List<TutorialItem> cobraKingSeenItems = new() {
        TutorialItem.IntroComic,
        TutorialItem.RunStartTutorial,
        TutorialItem.BananaGrab,
        TutorialItem.PineappleGrab,
        TutorialItem.CoconutGrab,
        TutorialItem.CoconutDoubleTap,
        TutorialItem.WebEscapeHint,
        TutorialItem.LowStaminaHint,
        TutorialItem.CobraChaseStart,
        TutorialItem.SpiderWebIntro,
        TutorialItem.SpiderKingLesson,
    };

    [Header("Spider King Scene")]
    [SerializeField]
    [Tooltip("seen at start; leave out SpiderKingLesson so the scene shows "
        + "it; SpiderWebIntro seen turns the web intro off")]
    private List<TutorialItem> spiderKingSeenItems = new() {
        TutorialItem.IntroComic,
        TutorialItem.RunStartTutorial,
        TutorialItem.BananaGrab,
        TutorialItem.PineappleGrab,
        TutorialItem.CoconutGrab,
        TutorialItem.CoconutDoubleTap,
        TutorialItem.WebEscapeHint,
        TutorialItem.LowStaminaHint,
        TutorialItem.CobraChaseStart,
        TutorialItem.SpiderWebIntro,
        TutorialItem.CobraKingLesson,
        TutorialItem.ArmorWords,
    };
}
