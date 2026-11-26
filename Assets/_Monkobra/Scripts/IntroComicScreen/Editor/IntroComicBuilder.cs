using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// <c>Monkobra/Build/Intro Comic</c> brings the comic drawn by
/// <c>tools/comic/make_panels.py</c> into the game, through Unity, never by
/// hand-editing YAML: it copies each layer's 2x PNG into
/// <c>Sprites/IntroComicScreen</c> as a crunched sprite, writes the panels of
/// <c>panels.json</c> into the <see cref="IntroComicConfig"/> asset, builds the
/// <c>IntroComicScreen</c> prefab (the stage, the caption plate with its
/// next-page hint, the Skip button), and the first time makes
/// <c>IntroComicScene</c>, the menu shell around the prefab, right after the
/// title in the build. Run it again after re-exporting the panels; it keeps
/// the config's timings and the scene.
/// </summary>
public static class IntroComicBuilder {
    // private methods  ########################################################
    [MenuItem("Monkobra/Build/Intro Comic", false, 31)]
    private static void Build() {
        string source = Path.Combine(ProjectRoot, SOURCE);
        string jsonPath = Path.Combine(source, "panels.json");
        if (!File.Exists(jsonPath)) {
            Debug.LogError(
                "IntroComicBuilder:\tno " + SOURCE + "/panels.json, run "
                    + "python tools/comic/make_panels.py first"
            );
            return;
        }
        if (
            UIStyleBuilder.SemiBold == null || UIStyleBuilder.ExtraBold == null
        ) {
            Debug.LogError(
                "IntroComicBuilder:\trun Monkobra/Build/UI Fonts first"
            );
            return;
        }

        PanelsFile file = JsonUtility.FromJson<PanelsFile>(
            File.ReadAllText(jsonPath)
        );
        if (file == null || file.panels == null || file.panels.Length == 0) {
            Debug.LogError("IntroComicBuilder:\tno panels in " + jsonPath);
            return;
        }

        if (!ImportSprites(source, file)) {
            return;
        }
        IntroComicConfig config = WriteConfig(file);
        GameObject prefab = BuildPrefab(config);
        if (prefab == null) {
            Debug.LogError("IntroComicBuilder:\tfail to save " + PREFAB);
            return;
        }
        BuildScene(prefab);
        Debug.Log(
            "IntroComicBuilder:\tbuilt " + file.panels.Length + " panels into "
                + CONFIG + ", " + PREFAB + " and " + SCENE
        );
    }

    // every layer as a sprite, its sides kept even by make_panels.py so the
    // crunched block compression takes it; sprites of an older export go
    private static bool ImportSprites(string source, PanelsFile file) {
        Directory.CreateDirectory(Path.Combine(ProjectRoot, SPRITES));
        HashSet<string> wanted = new HashSet<string>();
        foreach (PanelEntry panel in file.panels) {
            foreach (LayerEntry layer in panel.layers) {
                string from = Path.Combine(source, layer.file);
                if (!File.Exists(from)) {
                    Debug.LogError("IntroComicBuilder:\tmissing " + from);
                    return false;
                }
                string asset = SpritePath(panel.index, layer.name);
                File.Copy(from, Path.Combine(ProjectRoot, asset), true);
                wanted.Add(asset);
            }
        }
        AssetDatabase.Refresh();

        foreach (string guid in AssetDatabase.FindAssets(
            "t:Texture2D", new[] { SPRITES }
        )) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!wanted.Contains(path)) {
                AssetDatabase.DeleteAsset(path);
            }
        }

        AssetDatabase.StartAssetEditing();
        try {
            foreach (string path in wanted) {
                TextureImporter importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) {
                    continue;
                }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = MAX_TEXTURE_SIZE;
                importer.textureCompression =
                    TextureImporterCompression.Compressed;
                importer.crunchedCompression = true;
                importer.compressionQuality = CRUNCH_QUALITY;
                importer.SaveAndReimport();
            }
        } finally {
            AssetDatabase.StopAssetEditing();
        }
        return true;
    }

    // panels.json's page units (top left, y down) into canvas units from the
    // panel's middle, y up
    private static IntroComicConfig WriteConfig(PanelsFile file) {
        IntroComicConfig config =
            AssetDatabase.LoadAssetAtPath<IntroComicConfig>(CONFIG);
        if (config == null) {
            Directory.CreateDirectory(
                Path.Combine(ProjectRoot, Path.GetDirectoryName(CONFIG))
            );
            config = ScriptableObject.CreateInstance<IntroComicConfig>();
            AssetDatabase.CreateAsset(config, CONFIG);
        }

        List<IntroComicConfig.Panel> panels =
            new List<IntroComicConfig.Panel>();
        foreach (PanelEntry entry in file.panels) {
            List<IntroComicConfig.Layer> layers =
                new List<IntroComicConfig.Layer>();
            foreach (LayerEntry layer in entry.layers) {
                float x = layer.rect[0];
                float y = layer.rect[1];
                float w = layer.rect[2];
                float h = layer.rect[3];
                layers.Add(new IntroComicConfig.Layer {
                    name = layer.name,
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                        SpritePath(entry.index, layer.name)
                    ),
                    center = FromPage(x + w / 2f, y + h / 2f),
                    size = new Vector2(w, h),
                    cue = ToCue(layer.cue, entry.index, layer.name),
                });
            }
            panels.Add(new IntroComicConfig.Panel {
                caption = entry.caption,
                motionS = Mathf.Max(0.1f, entry.motion.seconds),
                start = ToView(entry.motion.start),
                end = ToView(entry.motion.end),
                layers = layers.ToArray(),
            });
        }

        config.SetPanels(panels.ToArray());
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        return config;
    }

    private static GameObject BuildPrefab(IntroComicConfig config) {
        Directory.CreateDirectory(
            Path.Combine(ProjectRoot, Path.GetDirectoryName(PREFAB))
        );
        GameObject root = new GameObject(
            "IntroComicScreen", typeof(RectTransform)
        );
        try {
            root.layer = UI_LAYER;
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = PAGE;
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            // the color around the 16:9 frame on another screen shape
            RectTransform backdrop = NewRect("Backdrop", root.transform);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            Image backdropImage = backdrop.gameObject.AddComponent<Image>();
            backdropImage.color = UIStyle.Background;
            backdropImage.raycastTarget = false;

            // the panels are built under it at start, below the caption
            RectTransform stage = NewRect("Stage", root.transform);
            stage.sizeDelta = PAGE;
            stage.gameObject.AddComponent<RectMask2D>();

            RectTransform plate = NewRect("Caption", stage);
            plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0f);
            plate.pivot = new Vector2(0.5f, 0f);
            plate.anchoredPosition = new Vector2(0f, CAPTION_BOTTOM);
            plate.sizeDelta = new Vector2(CAPTION_WIDTH, CAPTION_HEIGHT);
            TMP_Text words = UIStyleBuilder.StyleIntroCaption(
                plate.gameObject.AddComponent<Image>(), CAPTION_INSET_X
            );
            words.text = config.Panels.Count > 0
                ? config.Panels[0].caption : "";
            CanvasGroup captionGroup =
                plate.gameObject.AddComponent<CanvasGroup>();
            captionGroup.blocksRaycasts = false;
            captionGroup.interactable = false;

            // the next-page hint, the Preferred input's animated icon only
            RectTransform hint = NewRect("NextHint", plate);
            hint.anchorMin = hint.anchorMax = new Vector2(1f, 0.5f);
            hint.anchoredPosition = new Vector2(-HINT_FROM_RIGHT, 0f);
            hint.sizeDelta = new Vector2(HINT_SIZE, HINT_SIZE);
            CanvasGroup hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hintGroup.alpha = 0f;
            hintGroup.blocksRaycasts = false;
            HintIconWidget slot =
                hint.gameObject.AddComponent<HintIconWidget>();
            SerializedObject slotData = new SerializedObject(slot);
            slotData.FindProperty("library").objectReferenceValue =
                Load<HintIconLibrary>(HINT_LIBRARY);
            slotData.FindProperty("keyboardController").objectReferenceValue =
                Load<RuntimeAnimatorController>(SPACE_PRESS);
            slotData.FindProperty("touchController").objectReferenceValue =
                Load<RuntimeAnimatorController>(TOUCH_TAP);
            slotData.FindProperty("fitHeight").floatValue = HINT_SIZE;
            slotData.FindProperty("iconScale").floatValue = HINT_ICON_SCALE;
            slotData.ApplyModifiedPropertiesWithoutUndo();

            Button skip = UIStyleBuilder.MakeIntroSkipButton(
                stage, SKIP_SIZE, "Skip"
            );
            RectTransform skipRect = (RectTransform)skip.transform;
            skipRect.anchorMin = skipRect.anchorMax = Vector2.one;
            skipRect.pivot = Vector2.one;
            skipRect.anchoredPosition = new Vector2(-SKIP_MARGIN, -SKIP_MARGIN);

            IntroComicScreen screen = root.AddComponent<IntroComicScreen>();
            SerializedObject screenData = new SerializedObject(screen);
            screenData.FindProperty("config").objectReferenceValue = config;
            screenData.FindProperty("inputActions").objectReferenceValue =
                Load<InputActionAsset>(INPUT_ACTIONS);
            screenData.FindProperty("stage").objectReferenceValue = stage;
            screenData.FindProperty("captionPlate").objectReferenceValue =
                plate;
            screenData.FindProperty("captionText").objectReferenceValue = words;
            screenData.FindProperty("captionGroup").objectReferenceValue =
                captionGroup;
            screenData.FindProperty("captionPaddingY").floatValue =
                CAPTION_PADDING_Y;
            screenData.FindProperty("nextHint").objectReferenceValue =
                hintGroup;
            screenData.FindProperty("skipButton").objectReferenceValue = skip;
            screenData.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                root, PREFAB, out bool isSaved
            );
            return isSaved ? saved : null;
        } finally {
            Object.DestroyImmediate(root);
        }
    }

    // made once, like the other menu scenes: MenuCamera, EventSystem_Menu,
    // the screen, Analytics, ControlStyleManager; then right after the title
    // in the build, so it ships
    private static void BuildScene(GameObject screen) {
        if (!File.Exists(Path.Combine(ProjectRoot, SCENE))) {
            string back = SceneManager.GetActiveScene().path;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                Debug.LogWarning(
                    "IntroComicBuilder:\tscene not made, the open scene was "
                        + "not saved; run the menu again"
                );
                return;
            }
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single
            );
            foreach (string path in SHELL) {
                GameObject prefab = Load<GameObject>(path);
                if (prefab == null) {
                    Debug.LogWarning("IntroComicBuilder:\tmissing " + path);
                    continue;
                }
                PrefabUtility.InstantiatePrefab(prefab, scene);
            }
            PrefabUtility.InstantiatePrefab(screen, scene);
            EditorSceneManager.SaveScene(scene, SCENE);
            if (!string.IsNullOrEmpty(back)) {
                EditorSceneManager.OpenScene(back);
            }
        }

        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == SCENE)) {
            return;
        }
        int title = scenes.FindIndex(
            s => s.path.EndsWith("/" + SceneNames.TITLE + ".unity")
        );
        scenes.Insert(
            title >= 0 ? title + 1 : scenes.Count,
            new EditorBuildSettingsScene(SCENE, true)
        );
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static IntroComicConfig.Cue ToCue(
        CueEntry entry, int panel, string layer
    ) {
        IntroComicConfig.Cue cue = new IntroComicConfig.Cue();
        if (entry == null || string.IsNullOrEmpty(entry.kind)) {
            return cue;
        }
        if (!Enum.TryParse(entry.kind, out IntroComicConfig.CueKind kind)) {
            Debug.LogWarning(
                "IntroComicBuilder:\tpanel " + panel + " " + layer
                    + ": unknown cue " + entry.kind
            );
            return cue;
        }
        cue.kind = kind;
        cue.delayS = Mathf.Max(0f, entry.delay);
        cue.periodS = Mathf.Max(0.01f, entry.period);
        cue.amount = entry.amount;
        cue.start = ToOffset(entry.start);
        cue.end = ToOffset(entry.end);
        cue.startScale = entry.startScale;
        cue.endScale = entry.endScale;
        return cue;
    }

    private static IntroComicConfig.View ToView(ViewEntry entry) {
        return new IntroComicConfig.View {
            center = FromPage(entry.center[0], entry.center[1]),
            zoom = Mathf.Max(1f, entry.zoom),
        };
    }

    private static Vector2 FromPage(float x, float y) {
        return new Vector2(x - PAGE.x / 2f, PAGE.y / 2f - y);
    }

    private static Vector2 ToOffset(float[] offset) {
        return offset == null || offset.Length < 2 ? Vector2.zero
            : new Vector2(offset[0], -offset[1]);
    }

    private static string SpritePath(int panel, string layer) {
        return SPRITES + "/Panel" + panel + "_" + layer + ".png";
    }

    private static RectTransform NewRect(string name, Transform parent) {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = UI_LAYER;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static T Load<T>(string path) where T : Object {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static string ProjectRoot =>
        Path.GetDirectoryName(Application.dataPath);

    // panels.json, as make_panels.py writes it
    [Serializable]
    private class PanelsFile {
        public PanelEntry[] panels;
    }

    [Serializable]
    private class PanelEntry {
        public int index;
        public string caption;
        public LayerEntry[] layers;
        public MotionEntry motion;
    }

    [Serializable]
    private class LayerEntry {
        public string name;
        public string file;
        public float[] rect;
        public CueEntry cue;
    }

    [Serializable]
    private class CueEntry {
        public string kind;
        public float delay;
        public float period;
        public float amount;
        public float[] start;
        public float[] end;
        public float startScale;
        public float endScale;
    }

    [Serializable]
    private class MotionEntry {
        public float seconds;
        public ViewEntry start;
        public ViewEntry end;
    }

    [Serializable]
    private class ViewEntry {
        public float[] center;
        public float zoom;
    }

    // constants  ##############################################################
    private const int UI_LAYER = 5;
    private const string SOURCE = "tools/comic/panels";
    private const string SPRITES = "Assets/_Monkobra/Sprites/IntroComicScreen";
    private const string CONFIG =
        "Assets/_Monkobra/Settings/IntroComicScreen/IntroComicConfig.asset";
    private const string PREFAB =
        "Assets/_Monkobra/Prefabs/IntroComicScreen/IntroComicScreen.prefab";
    private const string SCENE =
        "Assets/_Monkobra/Scenes/" + SceneNames.INTRO_COMIC + ".unity";
    private const string INPUT_ACTIONS =
        "Assets/_Monkobra/Inputs/InputSystem_Actions.inputactions";
    private const string HINT_LIBRARY =
        "Assets/_Monkobra/Settings/ControlStyle/HintIconLibrary.asset";
    private const string SPACE_PRESS =
        "Assets/_Monkobra/Animations/InputIcons/SpacePress.controller";
    private const string TOUCH_TAP =
        "Assets/_Monkobra/Animations/InputIcons/TouchTap.controller";
    private static readonly string[] SHELL = {
        "Assets/_Monkobra/Prefabs/World/MenuCamera.prefab",
        "Assets/_Monkobra/Prefabs/World/EventSystem_Menu.prefab",
        "Assets/_Monkobra/Resources/Analytics.prefab",
        "Assets/_Monkobra/Resources/ControlStyleManager.prefab",
    };

    // the 1280x720 page, and the art at 2x squeezed to fit 2048, crunched,
    // so five full-screen backgrounds stay small in the WebGL build
    private static readonly Vector2 PAGE = new Vector2(1280f, 720f);
    private const int MAX_TEXTURE_SIZE = 2048;
    private const int CRUNCH_QUALITY = 60;

    // caption plate, as the previews of make_panels.py draw it
    private const float CAPTION_WIDTH = 1040f;
    private const float CAPTION_HEIGHT = 68f; // one line; it fits each caption
    private const float CAPTION_BOTTOM = 22f;
    private const float CAPTION_INSET_X = 112f; // clear of the hint icon
    private const float CAPTION_PADDING_Y = 14f;
    // the hint as big as the popups' (64 at 1.5) and tutorial's (56 at 1.35):
    // the Space keycap fills only the middle of its square icon
    private const float HINT_SIZE = 64f;
    private const float HINT_ICON_SCALE = 1.4f;
    private const float HINT_FROM_RIGHT = 64f;
    private static readonly Vector2 SKIP_SIZE = new Vector2(156f, 64f);
    private const float SKIP_MARGIN = 18f;
}
