using System;
using UnityEditor;
using UnityEngine;

/// <summary>Wires the optional config through Unity's prefab API.</summary>
public static class FirebaseLeaderboardSetup {
    // Public Methods  #########################################################
    [MenuItem("Monkobra/Leaderboard/Prepare Firebase Leaderboard")]
    public static void Prepare() {
        FirebaseLeaderboardConfig config =
            AssetDatabase.LoadAssetAtPath<FirebaseLeaderboardConfig>(CONFIG);
        if (config == null) {
            config = ScriptableObject.CreateInstance<FirebaseLeaderboardConfig>();
            AssetDatabase.CreateAsset(config, CONFIG);
        }
        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB);
        try {
            LeaderboardWidget widget =
                root.GetComponentInChildren<LeaderboardWidget>(true);
            if (widget == null) {
                throw new InvalidOperationException("LeaderboardWidget missing.");
            }
            SerializedObject serialized = new SerializedObject(widget);
            serialized.FindProperty("firebaseConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UIStyleBuilder.BuildLeaderboardNamesIn(root);
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
        }
        finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        Selection.activeObject = config;
        Debug.Log("Firebase leaderboard prepared; fill in the public settings "
            + "and enable it after publishing tools/firebase/firestore.rules.");
    }

    /// <summary>Batch-mode integration check against the demo emulators.</summary>
    public static async void VerifyEmulator() {
        FirebaseLeaderboardConfig config =
            ScriptableObject.CreateInstance<FirebaseLeaderboardConfig>();
        try {
            ResetEmulatorIdentity();
            SerializedObject serialized = new SerializedObject(config);
            serialized.FindProperty("useOnlineLeaderboard").boolValue = true;
            serialized.FindProperty("useEmulators").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            FirebaseLeaderboardClient client =
                new FirebaseLeaderboardClient(config);
            foreach (string invalid in new[] {
                "", "   ", "<b>Monkey</b>", "Monkey\nKing", "12345678901234567"
            }) {
                if (FirebaseLeaderboardClient.TryNormalizeName(invalid, out _)) {
                    throw new Exception("Invalid nickname accepted.");
                }
            }
            if (!client.RememberName("  Test Monkey  ")
                || client.PlayerName != "Test Monkey") {
                throw new Exception("Nickname normalization failed.");
            }
            long initial = client.RememberScore("normal", 0);
            long target = Math.Max(1234, initial + 1);
            FirebaseLeaderboardClient.Result result =
                await client.SubmitAndLoad("normal", target, 10);
            if (result.PersonalBest != target) {
                throw new Exception("Initial best did not persist.");
            }
            // A new client must refresh the same anonymous identity.
            client.RememberName("Renamed Monkey");
            result = await new FirebaseLeaderboardClient(config)
                .SubmitAndLoad("normal", 1, 10);
            if (result.PersonalBest != target) {
                throw new Exception("Lower score overwrote personal best.");
            }
            bool found = false;
            foreach (SessionLeaderboard.Entry entry in result.Entries) {
                if (entry.IsPlayer && entry.Score == target
                    && entry.Name == "Renamed Monkey") {
                    found = true;
                }
            }
            if (!found) {
                throw new Exception("Player identity not found in top scores.");
            }
            VerifyStatusLayout();
            Debug.Log("FIREBASE_INTEGRATION_PASS: sign-in, token refresh, "
                + "commit maximum, query, nickname validation and rename "
                + "without losing personal best.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
        finally {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [MenuItem("Monkobra/Leaderboard/Reset Local Emulator Identity")]
    public static void ResetEmulatorIdentity() {
        // Emulator Auth is empty after a restart unless explicitly exported.
        // Keep pending bests, but never reuse a vanished demo identity.
        PlayerPrefs.DeleteKey("Monkobra.Firebase.Emulator.demo-monkobra.refresh");
        PlayerPrefs.Save();
    }

    /// <summary>Offline visual check: renders sample rows without contacting Firebase.</summary>
    public static void RenderPreview() {
        UnityEngine.SceneManagement.Scene scene =
            UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        RenderTexture target = new RenderTexture(720, 620, 24);
        RenderTexture previous = RenderTexture.active;
        Texture2D image = null;
        try {
            GameObject canvasObject = new GameObject("Preview", typeof(Canvas));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
                canvasObject, scene
            );
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.GetComponent<RectTransform>().sizeDelta =
                new Vector2(720f, 620f);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            GameObject boardObject =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            boardObject.transform.SetParent(canvasObject.transform, false);
            boardObject.GetComponent<RectTransform>().anchoredPosition =
                Vector2.zero;
            LeaderboardRowWidget[] rows = boardObject
                .GetComponentsInChildren<LeaderboardRowWidget>(true);
            SessionLeaderboard.Entry[] sample = new SessionLeaderboard(
                rows.Length - 1, 1000, 20000,
                new[] { "MonkeyOne", "BananaKing", "TreeRunner" }, 7
            ).GetStandings(15455);
            int playerRank = 0;
            for (int i = 0; i < rows.Length; i++) {
                rows[i].gameObject.SetActive(true);
                rows[i].ShowEntry(sample[i], i + 1);
                if (sample[i].IsPlayer) {
                    playerRank = i + 1;
                    SerializedObject row = new SerializedObject(rows[i]);
                    (row.FindProperty("nameLabel").objectReferenceValue
                        as TMPro.TMP_Text).text = "Belle";
                }
            }
            boardObject.transform.Find("PlayerRank")
                .GetComponent<TMPro.TMP_Text>().text = "YOUR RANK  #" + playerRank;
            boardObject.GetComponentInChildren<TMPro.TMP_InputField>(true)
                .SetTextWithoutNotify("Belle");

            GameObject cameraObject = new GameObject("Camera", typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
                cameraObject, scene
            );
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.overrideSceneCullingMask =
                UnityEditor.SceneManagement.EditorSceneManager
                    .GetSceneCullingMask(scene);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 310f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UIStyle.Background;
            camera.targetTexture = target;
            canvas.worldCamera = camera;
            Canvas.ForceUpdateCanvases();
            foreach (TMPro.TMP_Text text in boardObject
                .GetComponentsInChildren<TMPro.TMP_Text>(true)) {
                text.ForceMeshUpdate(true);
            }
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(720, 620, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, 720f, 620f), 0, 0);
            image.Apply();
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllBytes("Logs/leaderboard-name-preview.png",
                image.EncodeToPNG());
            Debug.Log("LEADERBOARD_PREVIEW_READY");
        }
        finally {
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            if (image != null) {
                UnityEngine.Object.DestroyImmediate(image);
            }
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    // private members  ########################################################
    private const string CONFIG = "Assets/_Monkobra/Settings/Scoring/"
        + "FirebaseLeaderboardConfig.asset";
    private const string PREFAB = "Assets/_Monkobra/Prefabs/LoseScreen/"
        + "Leaderboard.prefab";

    private static void VerifyStatusLayout() {
        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB);
        try {
            LeaderboardWidget widget =
                root.GetComponentInChildren<LeaderboardWidget>(true);
            LeaderboardNameEditor nameEditor =
                root.GetComponentInChildren<LeaderboardNameEditor>(true);
            if (nameEditor == null) {
                throw new Exception("Nickname controls missing from prefab.");
            }
            SerializedObject nameFields = new SerializedObject(nameEditor);
            foreach (string field in new[] {
                "leaderboard", "nameInput", "saveButton", "hintLabel"
            }) {
                if (nameFields.FindProperty(field).objectReferenceValue == null) {
                    throw new Exception("Missing name editor reference: " + field);
                }
            }
            SerializedObject serialized = new SerializedObject(widget);
            TMPro.TMP_Text label = serialized.FindProperty("playerRankLabel")
                .objectReferenceValue as TMPro.TMP_Text;
            Canvas.ForceUpdateCanvases();
            string[] messages = {
                "CONNECTING...", "OFFLINE  |  SAVED LOCALLY",
                "BEST 123,456  |  OUTSIDE TOP 10", "PRACTICE  #10 / 10"
            };
            foreach (string message in messages) {
                float width = label.GetPreferredValues(message).x;
                if (width > label.rectTransform.rect.width) {
                    throw new Exception("Leaderboard footer does not fit: "
                        + message + " (" + width + " > "
                        + label.rectTransform.rect.width + ")");
                }
            }
        }
        finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
