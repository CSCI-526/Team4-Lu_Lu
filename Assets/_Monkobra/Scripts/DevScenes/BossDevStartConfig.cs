using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Numbers and first-time state for the boss dev scenes: where the other king
/// sits in the Cobra King scene, and which tutorials count as already seen,
/// so a scene plays like the player's first meeting with that boss. The boss
/// under test needs no height here: its location is the monkey's start, so
/// its fight begins on the first live frame.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Objects/BossDevStartConfig")]
public class BossDevStartConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>in the Cobra King scene, how far above the Cobra King zone
    /// top the Spider King zone starts; u</returns>
    public float OtherKingGapU => otherKingGapU;

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
    [Tooltip("Cobra King scene only: the Spider King zone starts this far "
        + "above the Cobra King zone end, out of the way; u")]
    private float otherKingGapU = 50f;

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
