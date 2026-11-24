using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds nickname controls with the shared font and button
/// styles.</summary>
public static partial class UIStyleBuilder {
    // Public Methods  #########################################################
    [MenuItem("Monkobra/UI Style/6. Build Leaderboard Name Input")]
    public static void BuildLeaderboardNames() {
        EditPrefab(LEADERBOARD, BuildLeaderboardNamesIn);
        AssetDatabase.SaveAssets();
        Debug.Log("UIStyleBuilder:\tleaderboard name input ready");
    }

    internal static void BuildLeaderboardNamesIn(GameObject root) {
        LeaderboardWidget board = root.GetComponent<LeaderboardWidget>();
        RectTransform group = FindOrCreate("NameEditor", root.transform);
        CenterNameRect(group, 0f, 0f, 620f, 520f);

        RectTransform inputRect = FindOrCreate("NameInput", group);
        CenterNameRect(inputRect, -85f, 180f, 390f, 42f);
        Image inputImage = GetOrAdd<Image>(inputRect.gameObject);
        MakePlate(inputImage, 1f);
        inputImage.raycastTarget = true;
        RectTransform viewport = FindOrCreate("Viewport", inputRect);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12f, 3f);
        viewport.offsetMax = new Vector2(-12f, -3f);
        GetOrAdd<RectMask2D>(viewport.gameObject);
        TMP_Text words = NameText("Text", viewport, 22f, UIStyle.Cream);
        TMP_Text placeholder = NameText("Placeholder", viewport, 18f,
            WithAlpha(UIStyle.Cream, 0.65f));
        placeholder.text = "Your name (1-16 letters/numbers)";
        TMP_InputField input = GetOrAdd<TMP_InputField>(inputRect.gameObject);
        input.textViewport = viewport;
        input.textComponent = words;
        input.placeholder = placeholder;
        input.targetGraphic = inputImage;
        input.characterLimit = 16;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.richText = false;
        input.customCaretColor = true;
        input.caretColor = UIStyle.Gold;
        input.selectionColor = WithAlpha(UIStyle.Gold, 0.35f);
        input.navigation = new Navigation { mode = Navigation.Mode.None };

        Transform existing = group.Find("SaveName");
        GameObject save = existing != null ? existing.gameObject
            : (GameObject)PrefabUtility.InstantiatePrefab(
                Load<GameObject>(MENU_BUTTON_PREFAB), group
            );
        save.name = "SaveName";
        Button button = save.GetComponent<Button>();
        StyleMenuButton(button);
        CenterNameRect((RectTransform)save.transform, 202f, 180f, 156f, 64f);
        TMP_Text buttonLabel = save.GetComponentInChildren<TMP_Text>(true);
        StyleText(buttonLabel, ExtraBold, 22f, UIStyle.Brown);
        buttonLabel.text = "SAVE";

        RectTransform hintRect = FindOrCreate("NameHint", group);
        CenterNameRect(hintRect, 0f, -181f, 560f, 24f);
        TMP_Text hint = GetOrAdd<TextMeshProUGUI>(hintRect.gameObject);
        StyleText(hint, SemiBold, 18f, UIStyle.Cream);
        hint.alignment = TextAlignmentOptions.Center;
        hint.textWrappingMode = TextWrappingModes.NoWrap;
        hint.raycastTarget = false;
        hint.text = "Names may repeat. Each player keeps their own score.";

        // Keep ten rows and the existing footer inside the same 620x520 plate.
        SerializedObject serializedBoard = new SerializedObject(board);
        SerializedProperty rows = serializedBoard.FindProperty("rows");
        for (int i = 0; i < rows.arraySize; i++) {
            LeaderboardRowWidget row = rows.GetArrayElementAtIndex(i)
                .objectReferenceValue as LeaderboardRowWidget;
            RectTransform rect = row.transform as RectTransform;
            rect.anchoredPosition = new Vector2(0f, 95f - i * 27f);
        }
        SetNameY(root, "HeaderRule", 146f);
        SetNameY(root, "RankHeader", 125f);
        SetNameY(root, "ClimberHeader", 125f);
        SetNameY(root, "ScoreHeader", 125f);

        LeaderboardNameEditor editor =
            GetOrAdd<LeaderboardNameEditor>(group.gameObject);
        SerializedObject serialized = new SerializedObject(editor);
        serialized.FindProperty("leaderboard").objectReferenceValue = board;
        serialized.FindProperty("nameInput").objectReferenceValue = input;
        serialized.FindProperty("saveButton").objectReferenceValue = button;
        serialized.FindProperty("hintLabel").objectReferenceValue = hint;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Editor Lifecycle  #######################################################
    [InitializeOnLoadMethod]
    private static void QueueNameInputBuild() {
        EditorApplication.delayCall += BuildNameInputIfMissing;
    }

    private static void BuildNameInputIfMissing() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            return;
        }
        GameObject prefab = Load<GameObject>(LEADERBOARD);
        if (prefab != null && prefab.GetComponentInChildren<
            LeaderboardNameEditor>(true) == null) {
            BuildLeaderboardNames();
        }
    }

    // private methods  ########################################################
    private static TMP_Text NameText(
        string name, RectTransform parent, float size, Color color
    ) {
        RectTransform rect = FindOrCreate(name, parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        TMP_Text text = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
        StyleText(text, SemiBold, size, color);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.richText = false;
        text.raycastTarget = false;
        return text;
    }

    private static void CenterNameRect(
        RectTransform rect, float x, float y, float width, float height
    ) {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetNameY(GameObject root, string name, float y) {
        RectTransform rect = root.transform.Find(name) as RectTransform;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
    }
}
