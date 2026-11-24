using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The intro comic's UI look, for <c>Monkobra/Build Intro Comic</c>
/// (<see cref="IntroComicBuilder"/>): its caption on the plate, made opaque so
/// the words read the same on the sunny panels and in the dark jungle, and a
/// small Skip <c>MenuButton</c>.
/// </summary>
public static partial class UIStyleBuilder {
    // Public Members  #########################################################
    // the olive the see-through Panel plate shows over the comic's dry ground,
    // opaque so every panel's caption matches
    internal static readonly Color IntroCaptionPlate =
        new Color32(0x35, 0x3D, 0x23, 0xFF);

    // the caption plate on an Image, the words a child inset from its sides
    internal static TMP_Text StyleIntroCaption(Image plate, float insetX) {
        MakePlate(plate, 1f);
        plate.color = IntroCaptionPlate;

        RectTransform wordsRect = FindOrCreate("Text", plate.transform);
        wordsRect.anchorMin = Vector2.zero;
        wordsRect.anchorMax = Vector2.one;
        wordsRect.offsetMin = new Vector2(insetX, 0f);
        wordsRect.offsetMax = new Vector2(-insetX, 0f);
        TMP_Text words = GetOrAdd<TextMeshProUGUI>(wordsRect.gameObject);
        StyleText(words, SemiBold, BODY_SIZE, UIStyle.Cream);
        words.alignment = TextAlignmentOptions.Center;
        words.textWrappingMode = TextWrappingModes.Normal;
        words.lineSpacing = INTRO_CAPTION_LINE_SPACING;
        words.raycastTarget = false;
        return words;
    }

    // a small MenuButton with a label; a hover never selects it, so Space,
    // which turns the comic's page, can never press it
    internal static Button MakeIntroSkipButton(
        Transform parent, Vector2 size, string label
    ) {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(
            Load<GameObject>(MENU_BUTTON_PREFAB), parent
        );
        go.name = "Skip";
        Button button = go.GetComponent<Button>();
        StyleMenuButton(button);
        ((RectTransform)go.transform).sizeDelta = size;

        TMP_Text words = go.GetComponentInChildren<TMP_Text>(true);
        if (words != null) {
            StyleText(words, ExtraBold, INTRO_SKIP_SIZE, UIStyle.Brown);
            words.text = label;
        }
        MenuButtonHover hover = go.GetComponent<MenuButtonHover>();
        if (hover != null) {
            hover.enabled = false;
        }
        return button;
    }

    // constants  ##############################################################
    // as the leaderboard's small SAVE button
    private const float INTRO_SKIP_SIZE = 22f;
    // Baloo 2's tall 1.6 line, tightened to the previews' 38 units at size 30
    private const float INTRO_CAPTION_LINE_SPACING = -30f;
}
