using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The pause and lose screens in the UI style (docs/ui-style-doc.md): a dark
/// Background wash, a logo-style title, square gold icon buttons with their
/// words beside them and their key on a keycap, the lose screen's run card
/// and leaderboard as plates of one height with the key hint row under both,
/// and the leaderboard rows in the palette. The boss fight's win panel is
/// built in code by <see cref="BossFightHud"/>, which takes its colors from
/// <see cref="UIStyle"/> itself. Like the rest of
/// <see cref="UIStyleBuilder"/>, it restyles in place and can run again.
/// </summary>
public static partial class UIStyleBuilder {
    // private methods  ########################################################
    [MenuItem("Monkobra/UI Style/5. Style Pause and Lose Screens")]
    private static void StyleRunScreens() {
        if (!AreFontsReady()) {
            return;
        }
        StylePauseScreen();
        StyleLoseScreen();
        StyleLeaderboard();
        AssetDatabase.SaveAssets();
        Debug.Log("UIStyleBuilder:\tstyled the pause and lose screens");
    }

    // pause  ------------------------------------------------------------------
    private static void StylePauseScreen() {
        EditPrefab(PAUSE_SCREEN, root => {
            Transform panel = root.transform.Find("Panel");
            if (panel == null) {
                Debug.LogError("UIStyleBuilder:\tno Panel in " + PAUSE_SCREEN);
                return;
            }
            Image wash = panel.GetComponent<Image>();
            if (wash != null) {
                wash.color = WithAlpha(UIStyle.Background, SCREEN_WASH);
            }
            TMP_Text title = panel.Find("Title")?.GetComponent<TMP_Text>();
            if (title != null) {
                StyleText(title, ExtraBold, TITLE_SIZE, UIStyle.Cream, TitleMaterial);
            }
            StyleIconColumn(panel.Find("Options"), PAUSE_OPTIONS_X, new[] {
                ("ResumeEscButton", "Icon_Resume", "Resume"),
                ("RestartRButton", "Icon_Restart", "Restart"),
                ("ExittoTitleButton", "Icon_Exit", "Exit"),
            }, BUTTON_SIZE);
            ShowKeyboardGroups(root);
        });
    }

    // A column of square gold buttons, each with its words to the right of
    // the face and its key, if it has one, on a keycap to the left. The
    // column sits so that the widest row is centered on centerX.
    private static void StyleIconColumn(
        Transform options,
        float centerX,
        (string button, string glyph, string words)[] rows,
        float labelSize
    ) {
        if (options == null) {
            Debug.LogError("UIStyleBuilder:\tno Options to style");
            return;
        }
        Vector2 face = FaceCenter();
        float faceLeft = face.x - ICON_FACE / 2f;
        float faceRight = face.x + ICON_FACE / 2f;
        float widestLabel = 0f;
        bool hasKey = false;

        foreach ((string buttonName, string glyph, string words) in rows) {
            Button button = options.Find(buttonName)?.GetComponent<Button>();
            if (button == null) {
                Debug.LogError("UIStyleBuilder:\tno " + buttonName + " in Options");
                continue;
            }
            StyleIconButton(button, glyph);
            ((RectTransform)button.transform).pivot = new Vector2(0.5f, 0.5f);

            // the words: a lost label (the lose screen's) is made again
            RectTransform labelRect = FindOrCreate("Label", button.transform);
            TMP_Text label = GetOrAdd<TextMeshProUGUI>(labelRect.gameObject);
            label.text = words;
            StyleText(label, ExtraBold, labelSize, UIStyle.Cream);
            label.alignment = TextAlignmentOptions.Left;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            float width = Mathf.Ceil(label.GetPreferredValues(words).x) + 4f;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(width, ICON_FACE);
            labelRect.anchoredPosition = new Vector2(faceRight + LABEL_GAP, face.y);
            widestLabel = Mathf.Max(widestLabel, width);

            RectTransform key = (RectTransform)button.transform.Find("KeyIcon");
            if (key != null) {
                MakeKeycap(key.GetComponent<Image>());
                key.anchorMin = key.anchorMax = new Vector2(0.5f, 0.5f);
                key.pivot = new Vector2(0.5f, 0.5f);
                key.sizeDelta = new Vector2(KEY_HINT_SIZE, KEY_HINT_SIZE);
                key.anchoredPosition = new Vector2(
                    faceLeft - HINT_GAP - KEY_HINT_SIZE / 2f, face.y
                );
                hasKey = true;
            }
        }

        // faces 20 apart: the sprites' margins already add 22
        VerticalLayoutGroup column = options.GetComponent<VerticalLayoutGroup>();
        float buttonHeight = ICON_FACE + MARGIN_TOP + MARGIN_BOTTOM;
        if (column != null) {
            column.spacing = BUTTON_GAP - (MARGIN_TOP + MARGIN_BOTTOM);
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childControlWidth = column.childControlHeight = false;
            column.childForceExpandWidth = column.childForceExpandHeight = false;
        }
        float spacing = column != null ? column.spacing : 0f;
        RectTransform rect = (RectTransform)options;
        rect.sizeDelta = new Vector2(
            ICON_FACE + MARGIN_LEFT + MARGIN_RIGHT,
            rows.Length * buttonHeight + (rows.Length - 1) * spacing
        );
        float left = hasKey ? faceLeft - HINT_GAP - KEY_HINT_SIZE : faceLeft;
        float right = faceRight + LABEL_GAP + widestLabel;
        rect.anchoredPosition = new Vector2(
            centerX - (left + right) / 2f, rect.anchoredPosition.y
        );
    }

    // lose  -------------------------------------------------------------------
    // the run card (left) and the leaderboard (right) are plates of one
    // height, centered; the key hint row sits under both, so it never
    // crowds the card at 4:3
    private static void StyleLoseScreen() {
        EditPrefab(LOSE_SCREEN, root => {
            Image wash = root.GetComponent<Image>();
            if (wash != null) {
                wash.color = WithAlpha(UIStyle.Background, LOSE_WASH);
            }
            Transform screen = root.transform;
            float top = PANEL_HEIGHT / 2f;

            RectTransform card = (RectTransform)screen.Find("RunCard");
            if (card != null) {
                MakePlate(card.GetComponent<Image>(), CARD_ALPHA);
                card.sizeDelta = new Vector2(card.sizeDelta.x, PANEL_HEIGHT);
                card.anchoredPosition = new Vector2(RUN_CARD_X, 0f);
            }

            CardText(screen, "RunLabel", SemiBold, SMALL_SIZE,
                WithAlpha(UIStyle.Cream, 0.7f), top - 34f);
            CardText(screen, "ResultText", ExtraBold, RESULT_SIZE,
                UIStyle.Cream, top - 90f, TitleMaterial);
            CardText(screen, "FinalScoreLabel", SemiBold, HUD_LABEL_SIZE,
                WithAlpha(UIStyle.Cream, 0.85f), top - 146f);
            CardText(screen, "FinalScore", ExtraBold, FINAL_SCORE_SIZE,
                UIStyle.Gold, top - 188f, HudNumberMaterial());
            CardText(screen, "FinalHeight", SemiBold, FINAL_HEIGHT_SIZE,
                UIStyle.Cream, top - 304f);

            // the fruit counts, one row across the card
            foreach (string row in new[] { "BananaRow", "PineappleRow", "CoconutRow" }) {
                RectTransform rowRect = (RectTransform)screen.Find(row);
                if (rowRect == null) {
                    continue;
                }
                rowRect.anchoredPosition = new Vector2(
                    rowRect.anchoredPosition.x, top - 250f
                );
                TMP_Text count = rowRect.Find("Count")?.GetComponent<TMP_Text>();
                if (count != null) {
                    StyleText(count, SemiBold, COUNT_ROW_SIZE, UIStyle.Cream);
                }
            }

            Transform options = screen.Find("Options");
            if (options != null) {
                ((RectTransform)options).anchoredPosition = new Vector2(
                    ((RectTransform)options).anchoredPosition.x, top - 425f
                );
            }
            StyleIconColumn(options, RUN_CARD_X, new[] {
                ("RestartRButton", "Icon_Restart", "Restart"),
                ("ExittoTitleButton", "Icon_Exit", "Exit"),
            }, LOSE_LABEL_SIZE);

            // under both panels, at its true size (no more 0.75 scale)
            RectTransform hint = (RectTransform)screen.Find("KeyHint");
            if (hint != null) {
                hint.localScale = Vector3.one;
                hint.anchorMin = hint.anchorMax = new Vector2(0.5f, 0.5f);
                hint.pivot = new Vector2(0.5f, 0.5f);
                hint.sizeDelta = new Vector2(HINT_ROW_WIDTH, HINT_ROW_HEIGHT);
                hint.anchoredPosition = new Vector2(
                    0f, -top - HINT_BELOW_PANELS - HINT_ROW_HEIGHT / 2f
                );
            }

            foreach (LeaderboardWidget board in root.GetComponentsInChildren<
                LeaderboardWidget
            >(true)) {
                RectTransform boardRect = (RectTransform)board.transform;
                boardRect.anchoredPosition = new Vector2(
                    boardRect.anchoredPosition.x, 0f
                );
            }

            // the dev-only download button: a small gold button in the corner
            Button download = screen.Find("DownloadReportButton")?.GetComponent<Button>();
            if (download != null) {
                StyleMenuButton(download);
                RectTransform rect = (RectTransform)download.transform;
                rect.sizeDelta = new Vector2(
                    DOWNLOAD_FACE.x + MARGIN_LEFT + MARGIN_RIGHT,
                    DOWNLOAD_FACE.y + MARGIN_TOP + MARGIN_BOTTOM
                );
                rect.anchorMin = rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(
                    -(SAFE_MARGIN - MARGIN_RIGHT) - rect.sizeDelta.x / 2f,
                    -(SAFE_MARGIN - MARGIN_TOP) - rect.sizeDelta.y / 2f
                );
                TMP_Text words = download.GetComponentInChildren<TMP_Text>(true);
                if (words != null) {
                    words.fontSize = DOWNLOAD_SIZE;
                }
            }
            ShowKeyboardGroups(root);
        });
    }

    // a line of the run card: centered on the card, one line, shrunk to fit
    private static void CardText(
        Transform screen, string textName, TMP_FontAsset font, float size,
        Color color, float y, Material material = null
    ) {
        TMP_Text text = screen.Find(textName)?.GetComponent<TMP_Text>();
        if (text == null) {
            Debug.LogError("UIStyleBuilder:\tno " + textName + " on the lose screen");
            return;
        }
        StyleText(text, font, size, color, material);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Round(size * 0.6f);
        text.fontSizeMax = size;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(CARD_TEXT_WIDTH, Mathf.Ceil(size * 1.3f));
        rect.anchoredPosition = new Vector2(RUN_CARD_X, y);
    }

    // the leaderboard: a plate, the palette, Baloo 2  -------------------------
    private static void StyleLeaderboard() {
        EditPrefab(LEADERBOARD, root => {
            Image plate = root.GetComponent<Image>();
            if (plate != null) {
                MakePlate(plate, CARD_ALPHA);
            }
            // the plate's gold rim takes over the gold line along the top
            root.transform.Find("AccentRule")?.gameObject.SetActive(false);

            TMP_Text title = root.transform.Find("Title")?.GetComponent<TMP_Text>();
            if (title != null) {
                StyleText(title, ExtraBold, PANEL_TITLE_SIZE, UIStyle.Cream);
            }
            foreach (string header in new[] { "RankHeader", "ClimberHeader", "ScoreHeader" }) {
                TMP_Text text = root.transform.Find(header)?.GetComponent<TMP_Text>();
                if (text != null) {
                    StyleText(text, SemiBold, LEADERBOARD_HEADER_SIZE,
                        WithAlpha(UIStyle.Cream, 0.6f));
                }
            }
            foreach (string rule in new[] { "HeaderRule", "FooterRule" }) {
                Image line = root.transform.Find(rule)?.GetComponent<Image>();
                if (line != null) {
                    line.color = UIStyle.Divider;
                }
            }
            TMP_Text rank = root.transform.Find("PlayerRank")?.GetComponent<TMP_Text>();
            if (rank != null) {
                StyleText(rank, ExtraBold, LEADERBOARD_RANK_SIZE, UIStyle.Gold);
            }
        });

        // the rows: the widget paints them per entry from these colors; the
        // player's row takes Bold, which the weight table draws in ExtraBold
        EditPrefab(LEADERBOARD_ROW, root => {
            foreach (TMP_Text cell in root.GetComponentsInChildren<TMP_Text>(true)) {
                Restyle(cell, SemiBold);
                cell.fontSize = LEADERBOARD_ROW_SIZE;
            }
            LeaderboardRowWidget widget = root.GetComponent<LeaderboardRowWidget>();
            if (widget != null) {
                SerializedObject so = new SerializedObject(widget);
                so.FindProperty("normalTextColor").colorValue = UIStyle.Cream;
                so.FindProperty("playerTextColor").colorValue = UIStyle.Gold;
                so.FindProperty("normalBackground").colorValue =
                    WithAlpha(UIStyle.Cream, 0.03f);
                so.FindProperty("alternateBackground").colorValue =
                    WithAlpha(UIStyle.Cream, 0.07f);
                so.FindProperty("playerBackground").colorValue =
                    WithAlpha(UIStyle.Gold, 0.2f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        });
    }

    // constants  ##############################################################
    private const float SCREEN_WASH = 0.8f; // over the paused game
    private const float LOSE_WASH = 0.9f;
    private const float CARD_ALPHA = 0.96f;
    private const float LABEL_GAP = 14f; // face to its words
    private const float PAUSE_OPTIONS_X = 0f;
    private const float RUN_CARD_X = -350f;
    private const float PANEL_HEIGHT = 520f; // run card & leaderboard
    private const float CARD_TEXT_WIDTH = 320f;
    private const float RESULT_SIZE = 52f;
    private const float FINAL_SCORE_SIZE = 52f;
    private const float FINAL_HEIGHT_SIZE = 24f;
    private const float COUNT_ROW_SIZE = 28f;
    private const float LOSE_LABEL_SIZE = 30f;
    private const float HINT_ROW_WIDTH = 900f;
    private const float HINT_ROW_HEIGHT = 60f;
    private const float HINT_BELOW_PANELS = 14f;
    private const float PANEL_TITLE_SIZE = 40f;
    private const float LEADERBOARD_HEADER_SIZE = 16f;
    private const float LEADERBOARD_ROW_SIZE = 22f;
    private const float LEADERBOARD_RANK_SIZE = 24f;
    private const float DOWNLOAD_SIZE = 22f;
    private static readonly Vector2 DOWNLOAD_FACE = new Vector2(240f, 44f);

    private const string PAUSE_SCREEN =
        "Assets/_Monkobra/Prefabs/PauseScreen/PauseScreen.prefab";
    private const string LOSE_SCREEN =
        "Assets/_Monkobra/Prefabs/LoseScreen/LoseScreen.prefab";
    private const string LEADERBOARD = PREFABS + "LoseScreen/Leaderboard.prefab";
    private const string LEADERBOARD_ROW = PREFABS + "LoseScreen/LeaderboardRow.prefab";
}
