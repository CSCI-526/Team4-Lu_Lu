using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The in-game HUD in the UI style (docs/ui-style-doc.md): the Space icons as
/// keycaps, the top-right cluster (pause button, its key hint, the coconut
/// counter) laid out from the corner so it never overlaps, the climb
/// progress bar in the loading page's art, the energy bar, the score, the
/// popups, the tutorial, and the scene overrides that pinned the old score
/// font. Like the rest of <see cref="UIStyleBuilder"/>, it restyles in place
/// and can run again.
/// </summary>
public static partial class UIStyleBuilder {
    // private methods  ########################################################
    [MenuItem("Monkobra/UI Style/4. Style HUD")]
    private static void StyleHud() {
        if (!AreFontsReady()
            || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
            return;
        }
        StyleInputIcons();
        StyleCoconutAndFreeze();
        StyleTopRight();
        StyleProgressBar();
        StyleStaminaBar();
        StyleScore();
        StylePopUps();
        StyleTutorial();
        StyleSmallHud();
        AssetDatabase.SaveAssets();
        RevertSceneFontOverrides();
        Debug.Log("UIStyleBuilder:\tstyled the HUD");
    }

    // input icons  ------------------------------------------------------------
    // the Space entity becomes a keycap; the press clips swap the root
    // Image's sprite, so each key controller's curve moves to the child that
    // now draws the key
    private static void StyleInputIcons() {
        EditPrefab(KEY_ICON, root => {
            Image image = root.GetComponent<Image>();
            if (image != null) {
                MakeKeycap(image);
            }
        });
        foreach (string icon in SPACE_ICONS) {
            RuntimeAnimatorController controller =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    INPUT_ANIMATIONS + icon + ".controller");
            if (controller == null) {
                continue;
            }
            foreach (AnimationClip clip in controller.animationClips) {
                MoveSpriteCurves(clip, "Outline");
            }
        }
        // a finger is no key: only the cream of the outlines
        EditPrefab(TOUCH_ICON, root => {
            Image image = root.GetComponent<Image>();
            if (image != null) {
                image.color = UIStyle.Cream;
            }
        });
    }

    private static void MoveSpriteCurves(AnimationClip clip, string childPath) {
        foreach (EditorCurveBinding binding in
            AnimationUtility.GetObjectReferenceCurveBindings(clip)) {
            if (binding.path != "" || binding.propertyName != "m_Sprite") {
                continue;
            }
            ObjectReferenceKeyframe[] keys =
                AnimationUtility.GetObjectReferenceCurve(clip, binding);
            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            EditorCurveBinding moved = binding;
            moved.path = childPath;
            AnimationUtility.SetObjectReferenceCurve(clip, moved, keys);
            EditorUtility.SetDirty(clip);
        }
    }

    // top-right cluster  ------------------------------------------------------
    private static void StyleCoconutAndFreeze() {
        EditPrefab(COCONUT_HUD, root => {
            TMP_Text count =
                root.transform.Find("Count")?.GetComponent<TMP_Text>();
            if (count != null) {
                StyleText(count, SemiBold, COUNT_SIZE, UIStyle.Cream);
            }
            TMP_Text hint =
                root.transform.Find("UseHint")?.GetComponent<TMP_Text>();
            if (hint != null) {
                StyleText(hint, SemiBold, SMALL_SIZE, UIStyle.Cream);
            }
        });
        // the countdown's colors come from FreezeTimerWidget
        EditPrefab(FREEZE_TIMER, root => {
            TMP_Text timer =
                root.transform.Find("Timer")?.GetComponent<TMP_Text>();
            if (timer != null) {
                Restyle(timer, SemiBold);
            }
        });
    }

    // from the top-right corner, right to left: the pause button inside the
    // safe margin, its "Esc to Pause" hint, then the coconut counter, each a
    // gap from the next and sized from its own rect, so no aspect ratio can
    // make them meet
    private static void StyleTopRight() {
        float hintLeft = 0f;
        // the button's own parts: its Esc hint and label, left of the face
        EditPrefab(PAUSE_BUTTON, pause => {
            StyleIconButton(pause.GetComponent<Button>(), "Icon_Pause");
            RectTransform button = (RectTransform)pause.transform;
            button.anchorMin = button.anchorMax = Vector2.one;
            button.pivot = new Vector2(0.5f, 0.5f);
            Vector2 size = button.sizeDelta;
            float faceLeft = -size.x / 2f + MARGIN_LEFT;
            float faceY = FaceCenter().y;

            RectTransform label =
                (RectTransform)pause.transform.Find("PauseKeyLabel");
            float labelLeft = faceLeft - HINT_GAP;
            if (label != null) {
                TMP_Text text = label.GetComponent<TMP_Text>();
                StyleText(text, SemiBold, HUD_LABEL_SIZE, UIStyle.Cream);
                text.alignment = TextAlignmentOptions.Right;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                float width =
                    Mathf.Ceil(text.GetPreferredValues(text.text).x) + 2f;
                label.anchorMin = label.anchorMax = new Vector2(0.5f, 0.5f);
                label.pivot = new Vector2(1f, 0.5f);
                label.sizeDelta = new Vector2(width, KEY_HINT_SIZE);
                label.anchoredPosition =
                    new Vector2(faceLeft - HINT_GAP, faceY);
                labelLeft = faceLeft - HINT_GAP - width;
            }
            hintLeft = labelLeft;
            RectTransform key =
                (RectTransform)pause.transform.Find("PauseKeyIcon");
            if (key != null) {
                MakeKeycap(key.GetComponent<Image>());
                key.anchorMin = key.anchorMax = new Vector2(0.5f, 0.5f);
                key.pivot = new Vector2(0.5f, 0.5f);
                key.sizeDelta = new Vector2(KEY_HINT_SIZE, KEY_HINT_SIZE);
                key.anchoredPosition = new Vector2(
                    labelLeft - KEY_LABEL_GAP - KEY_HINT_SIZE / 2f, faceY
                );
                hintLeft = labelLeft - KEY_LABEL_GAP - KEY_HINT_SIZE;
            }
        });

        // the cluster's place in the HUD, on the scene's instances: the
        // button hugs the corner and the counter sits left of its hint
        EditGameplayScenes(() => {
            RectTransform button = FindSceneRect("PauseButton");
            RectTransform coconut = FindSceneRect("CoconutHUD");
            if (button == null) {
                Debug.LogError("UIStyleBuilder:	no PauseButton in the scene");
                return false;
            }
            button.anchorMin = button.anchorMax = Vector2.one;
            button.pivot = new Vector2(0.5f, 0.5f);
            Vector2 size = button.sizeDelta;
            button.anchoredPosition = new Vector2(
                -(SAFE_MARGIN - MARGIN_RIGHT) - size.x / 2f,
                -(SAFE_MARGIN - MARGIN_TOP) - size.y / 2f
            );
            float faceY = FaceCenter().y;

            // the counter's number & how-to hang right of its icon, so its
            // right end is the icon's right plus that overhang
            if (coconut != null) {
                RectTransform count = (RectTransform)coconut.Find("Count");
                float overhang = count != null
                    ? count.anchoredPosition.x + count.sizeDelta.x
                    : 0f;
                float right = button.anchoredPosition.x + hintLeft
                    - CLUSTER_GAP - overhang;
                coconut.anchorMin = coconut.anchorMax = Vector2.one;
                coconut.pivot = new Vector2(0.5f, 0.5f);
                coconut.anchoredPosition = new Vector2(
                    right - coconut.sizeDelta.x / 2f,
                    button.anchoredPosition.y + faceY
                );
            }
            return true;
        });
    }

    // climb progress bar  -----------------------------------------------------
    // The bar is a Scrollbar turned 90 degrees and stretched along its length;
    // the faces and the readout turn back and undo the stretch, so they stand
    // upright at true proportions. The faces become the loading page's heads,
    // the cobra's neck its row of green segments.
    private static void StyleProgressBar() {
        EditPrefab(PROGRESS_BAR, root => {
            root.transform.localScale = new Vector3(BAR_STRETCH, 1f, 1f);
            Image track = root.GetComponent<Image>();
            if (track != null) {
                track.color = WithAlpha(UIStyle.Panel, 0.85f);
                AddOutline(track.gameObject, UIStyle.Brown, 1.5f);
            }
            Vector3 upright = new Vector3(1f, 1f / BAR_STRETCH, 1f);

            Transform monkey =
                root.transform.Find("Sliding Area/Handle/MonkeyIcon");
            if (monkey != null) {
                monkey.localScale = upright;
                DestroyChildren(monkey);
                Picture(monkey.gameObject, "MonkeyHead_Open", MONKEY_ICON);
            }

            Image head = null;
            RectTransform body = null;
            Transform marker = root.transform.Find("Sliding Area/CobraMarker");
            if (marker != null) {
                marker.localScale = upright;
                DestroyChildren(marker);
                Image dot = marker.GetComponent<Image>();
                if (dot != null) {
                    Object.DestroyImmediate(dot); // the old 1 px marker dot
                }
                ((RectTransform)marker).sizeDelta = COBRA_ICON;

                // segments from under the head down, the head drawn over them;
                // the widget stretches them to the bar's bottom end
                body = NewChild("Body", marker);
                body.anchorMin = body.anchorMax = new Vector2(0.5f, 0.5f);
                body.pivot = new Vector2(0.5f, 1f);
                body.sizeDelta = new Vector2(BEAD_SIZE, 0f);
                body.anchoredPosition = Vector2.zero;
                Image beads = body.gameObject.AddComponent<Image>();
                beads.sprite = Load<Sprite>(TITLE_SPRITES + "CobraSegment.png");
                beads.type = Image.Type.Tiled;
                beads.pixelsPerUnitMultiplier = beads.sprite != null
                    ? beads.sprite.rect.width / BEAD_SIZE
                    : 1f;
                beads.raycastTarget = false;

                RectTransform headRect = NewChild("Head", marker);
                headRect.anchorMin = headRect.anchorMax =
                    new Vector2(0.5f, 0.5f);
                headRect.sizeDelta = COBRA_ICON;
                head = Picture(headRect.gameObject, "CobraHead", COBRA_ICON);
            }

            Transform readout = root.transform.Find("DistanceReadout");
            if (readout != null) {
                readout.localScale = upright;
                Image plate =
                    readout.Find("DistanceLabel")?.GetComponent<Image>();
                if (plate != null) {
                    plate.color = UIStyle.Brown;
                    AddOutline(plate.gameObject, UIStyle.Gold, 1.5f);
                }
                TMP_Text distance = readout.Find("DistanceLabel/DistanceText")
                    ?.GetComponent<TMP_Text>();
                if (distance != null) {
                    StyleText(distance, SemiBold, DISTANCE_SIZE, UIStyle.Cream);
                }
                Transform bracket = readout.Find("DistanceBracket");
                if (bracket != null) {
                    foreach (Image tick in
                        bracket.GetComponentsInChildren<Image>(true)) {
                        tick.color = WithAlpha(UIStyle.Cream, 0.85f);
                    }
                }
            }

            // the widget no longer paints circles; the head flashes on a surge,
            // the body fills the bar below it, and the cobra stays in the bar
            ProgressBarWidget widget = root.GetComponent<ProgressBarWidget>();
            if (widget != null) {
                SerializedObject so = new SerializedObject(widget);
                so.FindProperty("circleImages").arraySize = 0;
                so.FindProperty("markerFlash").objectReferenceValue = head;
                so.FindProperty("cobraBody").objectReferenceValue = body;
                so.FindProperty("isMarkerSideStepped").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        });
    }

    private static Image Picture(GameObject go, string sprite, Vector2 size) {
        Image image = GetOrAdd<Image>(go);
        image.sprite = Load<Sprite>(TITLE_SPRITES + sprite + ".png");
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        ((RectTransform)go.transform).sizeDelta = size;
        return image;
    }

    // energy, score  ----------------------------------------------------------
    private static void StyleStaminaBar() {
        EditPrefab(STAMINA_BAR, root => {
            Image track =
                root.transform.Find("Background")?.GetComponent<Image>();
            if (track != null) {
                track.color = WithAlpha(UIStyle.Panel, 0.85f);
                AddOutline(track.gameObject, UIStyle.Brown, 2f);
            }
            Image fill =
                root.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
            if (fill != null) {
                fill.color = UIStyle.Gold;
            }
            TMP_Text label =
                root.transform.Find("EnergyLabel")?.GetComponent<TMP_Text>();
            if (label != null) {
                StyleText(label, SemiBold, HUD_LABEL_SIZE, UIStyle.Cream);
            }
        });
    }

    // The score sits on a plate in the top-left safe margin, level with the
    // pause button: a "Score" caption at the left, the number right-aligned,
    // so it still grows leftward away from the bonus feed on its right. The
    // number is gold with a thin outline and a soft shadow (the HUD number
    // material), lighter than the logo's Title look.
    private static void StyleScore() {
        Material number = HudNumberMaterial();
        EditPrefab(SCORE_HUD, root => {
            TMP_Text score = root.GetComponent<TMP_Text>();
            if (score != null) {
                Restyle(score, ExtraBold, number);
                score.color = UIStyle.Gold;
            }
        });
        EditPrefab(SCORE_HEIGHT, root => {
            RectTransform score =
                (RectTransform)root.transform.Find("ScoreHUD");
            if (score == null) {
                Debug.LogError(
                    "UIStyleBuilder:\tno ScoreHUD in " + SCORE_HEIGHT
                );
                return;
            }
            RectTransform plate = FindOrCreate("ScorePlate", root.transform);
            plate.SetAsFirstSibling(); // drawn under the number
            PlaceTopLeft(plate);
            MakePlate(GetOrAdd<Image>(plate.gameObject), SCORE_PLATE_ALPHA);

            RectTransform caption = FindOrCreate("Caption", plate);
            caption.anchorMin = Vector2.zero;
            caption.anchorMax = Vector2.one;
            caption.offsetMin = new Vector2(PLATE_PADDING, 0f);
            caption.offsetMax = new Vector2(-PLATE_PADDING, 0f);
            TMP_Text words = GetOrAdd<TextMeshProUGUI>(caption.gameObject);
            words.text = "Score";
            StyleText(words, SemiBold, HUD_LABEL_SIZE,
                WithAlpha(UIStyle.Cream, 0.85f));
            words.horizontalAlignment = HorizontalAlignmentOptions.Left;
            words.verticalAlignment = VerticalAlignmentOptions.Capline;
            words.textWrappingMode = TextWrappingModes.NoWrap;
            words.raycastTarget = false;
            float captionWidth =
                Mathf.Ceil(words.GetPreferredValues("Score").x);

            PlaceTopLeft(score);
            TMP_Text text = score.GetComponent<TMP_Text>();
            Restyle(text, ExtraBold, number);
            text.color = UIStyle.Gold;
            text.enableAutoSizing = true;
            text.fontSizeMin = SCORE_MIN_SIZE;
            text.fontSizeMax = SCORE_SIZE;
            text.fontSize = SCORE_SIZE;
            text.horizontalAlignment = HorizontalAlignmentOptions.Right;
            text.verticalAlignment = VerticalAlignmentOptions.Capline;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.margin = new Vector4(
                PLATE_PADDING + captionWidth + CAPTION_GAP, 0f, PLATE_PADDING,
                    0f
            );
        });
        // the bonus colors come per reward kind from ScoreBonusItem
        EditPrefab(SCORE_BONUS_ITEM, root => {
            foreach (TMP_Text part in
                root.GetComponentsInChildren<TMP_Text>(true)) {
                Restyle(part, ExtraBold, number);
            }
        });
    }

    // the plate's box: inside the safe margin, as tall as the pause button's
    // face and at its height
    private static void PlaceTopLeft(RectTransform rect) {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(SAFE_MARGIN, -SAFE_MARGIN);
        rect.sizeDelta = SCORE_PLATE;
    }

    // The plate: a rounded Panel fill, see-through, and a gold rim drawn by
    // its own child. Not an Outline effect: that repeats the whole fill
    // under it in gold, which shows through a see-through plate.
    private static void MakePlate(Image plate, float alpha) {
        foreach (Shadow effect in plate.GetComponents<Shadow>()) {
            Object.DestroyImmediate(effect); // Outline is a Shadow too
        }
        plate.sprite = Load<Sprite>(SPRITES + "Plate_Fill.png");
        plate.type = Image.Type.Sliced;
        plate.pixelsPerUnitMultiplier = 1f;
        plate.color = WithAlpha(UIStyle.Panel, alpha);
        plate.raycastTarget = false;

        RectTransform rimRect = FindOrCreate("Rim", plate.transform);
        rimRect.SetAsFirstSibling();
        rimRect.anchorMin = Vector2.zero;
        rimRect.anchorMax = Vector2.one;
        rimRect.offsetMin = rimRect.offsetMax = Vector2.zero;
        Image rim = GetOrAdd<Image>(rimRect.gameObject);
        rim.sprite = Load<Sprite>(SPRITES + "Plate_Rim.png");
        rim.type = Image.Type.Sliced;
        rim.pixelsPerUnitMultiplier = 1f;
        rim.color = UIStyle.Gold;
        rim.raycastTarget = false;
    }

    // ExtraBold, a thin dark brown outline, a soft dark shadow; made once
    private static Material HudNumberMaterial() {
        Material found = Load<Material>(HUD_NUMBER_MATERIAL);
        if (found != null) {
            return found;
        }
        Material made = new Material(ExtraBold.material) {
            name =
                System.IO.Path.GetFileNameWithoutExtension(HUD_NUMBER_MATERIAL),
            shader = Shader.Find("TextMeshPro/Distance Field"),
        };
        made.SetColor(ShaderUtilities.ID_OutlineColor, UIStyle.Brown);
        made.SetFloat(ShaderUtilities.ID_OutlineWidth, HUD_OUTLINE);
        made.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        made.SetColor(ShaderUtilities.ID_UnderlayColor,
            WithAlpha(UIStyle.Background, 0.6f));
        made.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, HUD_SHADOW.x);
        made.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, HUD_SHADOW.y);
        made.SetFloat(ShaderUtilities.ID_UnderlayDilate, HUD_OUTLINE);
        made.SetFloat(ShaderUtilities.ID_UnderlaySoftness, HUD_SHADOW_SOFTNESS);
        AssetDatabase.CreateAsset(made, HUD_NUMBER_MATERIAL);
        return made;
    }

    // popups, tutorial  -------------------------------------------------------
    // a see-through plate with a gold rim, as light as the old black one, so
    // the monkey's arm stays in sight behind a grab hint
    private static void StylePopUps() {
        foreach (string popUp in POP_UP_BASES) {
            EditPrefab(popUp, root => {
                Image plate =
                    root.transform.Find("Background")?.GetComponent<Image>();
                if (plate != null) {
                    MakePlate(plate, POP_UP_ALPHA);
                }
                foreach (string part in new[] { "Text", "Tail" }) {
                    TMP_Text text =
                        root.transform.Find(part)?.GetComponent<TMP_Text>();
                    if (text != null) {
                        Restyle(text, SemiBold);
                        text.color = UIStyle.Cream;
                    }
                }
                Image line = root.transform.Find("Line")?.GetComponent<Image>();
                if (line != null) {
                    line.color = WithAlpha(UIStyle.Cream, 0.85f);
                }
            });
        }

        // each popup sets its own text color in the catalog: a warning red, a
        // frozen blue stay; a plain white becomes cream
        PopUpCatalog catalog =
            AssetDatabase.LoadAssetAtPath<PopUpCatalog>(POP_UP_CATALOG);
        if (catalog != null) {
            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("definitions");
            for (int i = 0; i < list.arraySize; i++) {
                SerializedProperty color = list.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("textColor");
                Color c = color.colorValue;
                if (c.r > 0.99f && c.g > 0.99f && c.b > 0.99f) {
                    color.colorValue = WithAlpha(UIStyle.Cream, c.a);
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }
    }

    // a dark wash instead of a white one, the words in Baloo 2 (the legacy
    // Text labels become TMP), the keys as keycaps
    private static void StyleTutorial() {
        EditPrefab(TUTORIAL, root => {
            Image wash = root.GetComponent<Image>();
            if (wash != null) {
                wash.color = WithAlpha(UIStyle.Background, 0.6f);
            }
            foreach (Text legacy in root.GetComponentsInChildren<Text>(true)) {
                bool isCaption = legacy.name.Contains("Caption");
                ToTmp(
                    legacy,
                    isCaption ? ExtraBold : SemiBold,
                    isCaption ? CAPTION_SIZE : TUTORIAL_HINT_SIZE,
                    isCaption ? UIStyle.Cream : WithAlpha(UIStyle.Cream, 0.85f)
                );
            }
            TMP_Text press =
                root.transform.Find("Press/Label")?.GetComponent<TMP_Text>();
            if (press != null) {
                StyleText(press, SemiBold, PRESS_SIZE, UIStyle.Cream);
            }
            Transform keys = root.transform.Find("KeyboardControls");
            if (keys != null) {
                foreach (Image icon in
                    keys.GetComponentsInChildren<Image>(true)) {
                    if (icon.name.StartsWith("Icon") && icon.sprite != null
                        && icon.sprite.name.EndsWith("_outline")) {
                        MakeKeycap(icon);
                    }
                }
            }
        });
    }

    private static void ToTmp(Text legacy, TMP_FontAsset font, float size,
        Color color) {
        GameObject go = legacy.gameObject;
        string words = legacy.text;
        TextAnchor anchor = legacy.alignment;
        bool wraps = legacy.horizontalOverflow == HorizontalWrapMode.Wrap;
        Object.DestroyImmediate(legacy);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = words;
        StyleText(text, font, size, color);
        text.alignment = Align(anchor);
        text.textWrappingMode = wraps
            ? TextWrappingModes.Normal
            : TextWrappingModes.NoWrap;
        text.raycastTarget = false;
    }

    private static TextAlignmentOptions Align(TextAnchor anchor) {
        switch (anchor) {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }

    // web escape bar, hack key list, trunk height labels  ---------------------
    private static void StyleSmallHud() {
        EditPrefab(SPIDER_WEB_PROMPT, root => {
            Image track =
                root.transform.Find("ProgressBar")?.GetComponent<Image>();
            if (track != null) {
                track.color = WithAlpha(UIStyle.Panel, 0.9f);
                AddOutline(track.gameObject, UIStyle.Brown, 1.5f);
            }
            Image fill =
                root.transform.Find("ProgressBar/Fill")?.GetComponent<Image>();
            if (fill != null) {
                fill.color = UIStyle.Gold;
            }
        });
        EditPrefab(HACK_HELP, root => {
            TMP_Text list = root.GetComponent<TMP_Text>();
            if (list != null) {
                StyleText(list, SemiBold, HACK_LIST_SIZE,
                    WithAlpha(UIStyle.Cream, 0.85f));
            }
        });
        EditPrefab(HEIGHT_MARKERS, root => {
            HeightMarkerSystem markers =
                root.GetComponent<HeightMarkerSystem>();
            if (markers != null) {
                SerializedObject so = new SerializedObject(markers);
                so.FindProperty("font").objectReferenceValue = SemiBold;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        });
    }

    // scenes  -----------------------------------------------------------------
    // the gameplay scenes held their own font on HUD texts (the
    // old score font); dropping those overrides lets the prefabs' Baloo 2
    // through
    private static void RevertSceneFontOverrides() {
        string open = SceneManager.GetActiveScene().path;
        foreach (string scenePath in GAMEPLAY_SCENES) {
            Scene scene =
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int reverted = 0;
            foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None
            )) {
                if (!PrefabUtility.IsPartOfPrefabInstance(text)) {
                    continue;
                }
                SerializedObject so = new SerializedObject(text);
                foreach (string property in FONT_PROPERTIES) {
                    SerializedProperty sp = so.FindProperty(property);
                    if (sp != null && sp.prefabOverride) {
                        PrefabUtility.RevertPropertyOverride(
                            sp, InteractionMode.AutomatedAction
                        );
                        reverted++;
                    }
                }
            }
            if (reverted > 0) {
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log(
                "UIStyleBuilder:\t" + scene.name + ": reverted " + reverted
                    + " font overrides"
            );
        }
        if (!string.IsNullOrEmpty(open)) {
            EditorSceneManager.OpenScene(open, OpenSceneMode.Single);
        }
    }

    // helpers  ----------------------------------------------------------------
    private static void EditPrefab(string path,
        System.Action<GameObject> edit) {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // run an edit on each gameplay scene's objects, save the scene when the
    // edit says it changed something, then return to the scene left open
    private static void EditGameplayScenes(System.Func<bool> edit) {
        string open = SceneManager.GetActiveScene().path;
        foreach (string scenePath in GAMEPLAY_SCENES) {
            Scene scene =
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (edit()) {
                EditorSceneManager.SaveScene(scene);
            }
        }
        if (!string.IsNullOrEmpty(open)) {
            EditorSceneManager.OpenScene(open, OpenSceneMode.Single);
        }
    }

    private static RectTransform FindSceneRect(string objectName) {
        foreach (RectTransform rect in Object.FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None
        )) {
            if (rect.name == objectName) {
                return rect;
            }
        }
        return null;
    }

    // font & material only, size & color kept
    private static void Restyle(
        TMP_Text text, TMP_FontAsset font, Material material = null
    ) {
        text.font = font;
        text.fontSharedMaterial = material != null ? material : font.material;
        text.fontStyle = FontStyles.Normal;
    }

    private static void DestroyChildren(Transform parent) {
        for (int i = parent.childCount - 1; i >= 0; i--) {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static Color WithAlpha(Color color, float alpha) {
        color.a = alpha;
        return color;
    }

    // constants  ##############################################################
    private const float SAFE_MARGIN = 24f; // from the screen edge
    private const float HINT_GAP = 10f; // button face to its key hint
    private const float KEY_LABEL_GAP = 6f; // keycap to its words
    private const float CLUSTER_GAP = 24f; // key hint to the coconut counter
    private const float KEY_HINT_SIZE = 40f;
    private const float HUD_LABEL_SIZE = 22f;
    private const float COUNT_SIZE = 30f;
    private const float SMALL_SIZE = 18f;
    private const float DISTANCE_SIZE = 17f;
    private const float CAPTION_SIZE = 30f;
    private const float TUTORIAL_HINT_SIZE = 20f;
    private const float PRESS_SIZE = 28f;
    private const float HACK_LIST_SIZE = 14f;

    // plates: the popups as see-through as their old black plate
    private const float POP_UP_ALPHA = 0.55f;
    private const float SCORE_PLATE_ALPHA = 0.55f;
    private const float PLATE_PADDING = 16f;
    private const float CAPTION_GAP = 12f; // "Score" to the number
    private static readonly Vector2 SCORE_PLATE = new Vector2(220f, 64f);
    private const float SCORE_SIZE = 40f;
    private const float SCORE_MIN_SIZE = 24f;

    // the HUD number look, in the material's normalized units
    private const float HUD_OUTLINE = 0.12f;
    private static readonly Vector2 HUD_SHADOW = new Vector2(0.2f, -0.45f);
    private const float HUD_SHADOW_SOFTNESS = 0.35f;
    private const string HUD_NUMBER_MATERIAL =
        FONTS + "Baloo2-ExtraBold SDF - HudNumber.mat";

    // the progress bar: the stretch the HUD gives it, the heads, the body
    private const float BAR_STRETCH = 1.8f;
    private static readonly Vector2 MONKEY_ICON = new Vector2(40f, 38f);
    private static readonly Vector2 COBRA_ICON = new Vector2(34f, 33f);
    private const float BEAD_SIZE = 14f;

    private const string TITLE_SPRITES =
        "Assets/_Monkobra/Sprites/TitleScreen/";
    private const string INPUT_ICONS = "Assets/_Monkobra/Prefabs/InputIcons/";
    private const string KEY_ICON = INPUT_ICONS + "InputIcon_Key.prefab";
    private const string TOUCH_ICON = INPUT_ICONS + "InputIcon_Touch.prefab";
    private const string INPUT_ANIMATIONS =
        "Assets/_Monkobra/Animations/InputIcons/";
    private static readonly string[] SPACE_ICONS = {
        "SpacePress", "SpacePress_Double", "SpacePress_Hold",
        "SpacePress_Rapid",
    };
    private const string PREFABS = "Assets/_Monkobra/Prefabs/";
    private const string UI_PREFABS = PREFABS + "UI/";
    private const string PAUSE_BUTTON =
        PREFABS + "PauseScreen/PauseButton.prefab";
    private const string COCONUT_HUD =
        PREFABS + "ItemCoconut/CoconutHUD.prefab";
    private const string FREEZE_TIMER =
        PREFABS + "ItemCoconut/FreezeTimerHUD.prefab";
    private const string PROGRESS_BAR = UI_PREFABS + "ProgressBar.prefab";
    private const string STAMINA_BAR = PREFABS + "Stamina/StaminaBar.prefab";
    private const string SCORE_HUD = PREFABS + "Scoring/ScoreHUD.prefab";
    private const string SCORE_BONUS_ITEM =
        PREFABS + "Scoring/ScoreBonusItem.prefab";
    private const string SCORE_HEIGHT = PREFABS + "Scoring/ScoreHeight.prefab";
    private const string SPIDER_WEB_PROMPT =
        PREFABS + "MobSpiderWeb/SpiderWebPrompt.prefab";
    private const string HACK_HELP = UI_PREFABS + "HackShortcutHelp.prefab";
    private const string HEIGHT_MARKERS =
        "Assets/_Monkobra/Prefabs/Environment/HeightMarkerSystem.prefab";
    private const string TUTORIAL =
        "Assets/_Monkobra/Prefabs/TutorialScreen/TutorialScreen.prefab";
    private const string POP_UP_CATALOG =
        "Assets/_Monkobra/Settings/PopUp/PopUpCatalog.asset";
    private static readonly string[] POP_UP_BASES = {
        "Assets/_Monkobra/Prefabs/PopUp/_PopUpBase.prefab",
        "Assets/_Monkobra/Prefabs/PopUp/PopUp_Fruit.prefab",
    };
    private static readonly string[] GAMEPLAY_SCENES = {
        "Assets/_Monkobra/Scenes/GameplayScene.unity",
    };
    private static readonly string[] FONT_PROPERTIES = {
        "m_fontAsset", "m_sharedMaterial", "m_hasFontAssetChanged",
    };
}
