using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space UI of the boss fight, built at runtime on the root Canvas
/// (below the pause, tutorial and lose screens): the entry flash, the Cobra
/// King's name and health bar, its speech bubble over the head, the first
/// throw's key prompt over a banana, damage numbers and the end banner.
/// Every piece is a plain uGUI Image or TMP label, no sprite needed.
/// </summary>
public class BossFightHud: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>Builds the UI and shows the health bar, full.</summary>
    public void Show(string bossName) {
        if (!TryBuild()) {
            return;
        }
        rootRect.gameObject.SetActive(true);
        nameLabel.text = bossName;
        SetHealth(1f);
    }

    /// <summary>Sets the health bar to <paramref name="fraction"/> 0~1.
    /// </summary>
    public void SetHealth(float fraction) {
        if (healthFill == null) {
            return;
        }
        healthFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    /// <summary>Flashes the whole screen white, fading out.</summary>
    public void Flash() {
        flashLeftS = flashS;
    }

    /// <summary>Shows <paramref name="line"/> in a bubble over the head.
    /// </summary>
    public void Say(string line) {
        if (speechLabel == null) {
            return;
        }
        speechLabel.text = line;
        speechLeftS = speechShowS;
    }

    /// <summary>Pops a rising damage number at a world point.</summary>
    public void PopDamage(string text, Vector3 worldPosition) {
        PopText(text, worldPosition, damageColor);
    }

    /// <summary>Pops a rising word at a world point, eg "Blocked!".
    /// </summary>
    public void PopText(string text, Vector3 worldPosition, Color color) {
        if (rootRect == null) {
            return;
        }
        TextMeshProUGUI label = CreateLabel(
            "Pop", rootRect, text, 46f, color
        );
        label.rectTransform.sizeDelta = new Vector2(160f, 60f);
        pops.Add(new Pop {
            label = label,
            worldPosition = worldPosition,
            ageS = 0f
        });
    }

    /// <summary>
    /// Shows the victory panel over the frozen game: the title, the final
    /// score and how to play again.
    /// </summary>
    public void ShowVictory(long score) {
        if (rootRect == null) {
            return;
        }
        SetPrompt(false, Vector3.zero);
        if (speechRect != null) {
            speechLeftS = 0f;
            speechRect.gameObject.SetActive(false);
        }

        RectTransform panel = CreateRect("Victory", rootRect);
        Stretch(panel);
        AddImage(panel, new Color(0f, 0f, 0f, 0.55f));
        AddCenterLabel(panel, "Victory!", 90f, damageColor, 140f);
        AddCenterLabel(
            panel, "You defeated the Cobra King!", 36f, Color.white, 50f
        );
        AddCenterLabel(panel, "Final Score: " + score, 36f, Color.white, -10f);
        AddCenterLabel(
            panel, "Press Space to Play Again", 32f, Color.white, -110f
        );
    }

    /// <summary>Shows a big banner in the screen center.</summary>
    public void ShowBanner(string text) {
        if (bannerLabel == null) {
            return;
        }
        bannerLabel.text = text;
        bannerLabel.gameObject.SetActive(true);
    }

    /// <summary>
    /// Keeps the key prompt over <paramref name="worldPosition"/>, or hides
    /// it when <paramref name="isShown"/> is false.
    /// </summary>
    public void SetPrompt(bool isShown, Vector3 worldPosition) {
        if (promptRect == null) {
            return;
        }
        promptRect.gameObject.SetActive(isShown);
        promptWorldPosition = worldPosition;
    }

    /// <summary>
    /// Shows an attack warning over <paramref name="worldPosition"/>: a red
    /// sign with "!" and <paramref name="word"/> under it, beating faster as
    /// <paramref name="progress"/> 0~1 nears the hit.
    /// </summary>
    /// <returns>the warning's id for <see cref="UpdateWarning"/></returns>
    public int AddWarning(Vector3 worldPosition, string word) {
        if (rootRect == null) {
            return -1;
        }
        RectTransform sign = CreateRect("Warning", rootRect);
        sign.sizeDelta = new Vector2(70f, 70f);

        // a red diamond with a white border & a big "!"
        RectTransform border = CreateRect("Border", sign);
        Stretch(border);
        border.localRotation = Quaternion.Euler(0f, 0f, 45f);
        border.sizeDelta = Vector2.zero;
        AddImage(border, Color.white);
        RectTransform face = CreateRect("Face", border);
        Stretch(face);
        face.offsetMin = new Vector2(5f, 5f);
        face.offsetMax = new Vector2(-5f, -5f);
        AddImage(face, warningColor);
        TextMeshProUGUI mark = CreateLabel("Mark", sign, "!", 56f, Color.white);
        Stretch(mark.rectTransform);
        mark.fontStyle = FontStyles.Bold;

        TextMeshProUGUI label = CreateLabel(
            "Word", sign, word, 30f, warningColor
        );
        label.fontStyle = FontStyles.Bold;
        label.outlineWidth = 0.25f;
        label.outlineColor = Color.white;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -14f);
        labelRect.sizeDelta = new Vector2(240f, 40f);

        warnings.Add(new Warning {
            id = nextWarningId,
            sign = sign,
            worldPosition = worldPosition,
            progress = 0f
        });
        return nextWarningId++;
    }

    /// <summary>Moves warning <paramref name="id"/> & sets its progress 0~1.
    /// </summary>
    public void UpdateWarning(int id, Vector3 worldPosition, float progress) {
        int index = warnings.FindIndex(w => w.id == id);
        if (index < 0) {
            return;
        }
        Warning warning = warnings[index];
        warning.worldPosition = worldPosition;
        warning.progress = Mathf.Clamp01(progress);
        warnings[index] = warning;
    }

    /// <summary>Removes every attack warning.</summary>
    public void ClearWarnings() {
        foreach (Warning warning in warnings) {
            if (warning.sign != null) {
                Destroy(warning.sign.gameObject);
            }
        }
        warnings.Clear();
    }

    /// <summary>World point the speech bubble follows, eg the king's head.
    /// </summary>
    public Vector3 SpeechAnchor {
        set => speechWorldPosition = value;
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("how long the entry flash takes to fade; s")]
    private float flashS = 0.8f;

    [SerializeField]
    [Tooltip("how long a speech line stays up; s")]
    private float speechShowS = 2.2f;

    [SerializeField]
    [Tooltip("bubble offset over the head, in canvas units")]
    private Vector2 speechOffset = new Vector2(0f, 90f);

    [SerializeField]
    [Tooltip("prompt offset over the banana, in canvas units")]
    private Vector2 promptOffset = new Vector2(0f, 80f);

    [SerializeField]
    private Color healthColor = new Color(0.85f, 0.15f, 0.15f);

    [SerializeField]
    private Color damageColor = new Color(1f, 0.85f, 0.2f);

    [SerializeField]
    private Color warningColor = new Color(0.95f, 0.15f, 0.15f);

    [SerializeField]
    [Tooltip("warning sign offset over its zone, in canvas units")]
    private Vector2 warningOffset = new Vector2(0f, 70f);

    // MonoBehaviour Lifecycle  ################################################
    private void Update() {
        if (rootRect == null) {
            return;
        }

        UpdateFlash();
        if (speechRect != null) {
            speechLeftS -= Time.deltaTime;
            bool isSpeaking = speechLeftS > 0f;
            speechRect.gameObject.SetActive(isSpeaking);
            if (isSpeaking) {
                Place(speechRect, speechWorldPosition, speechOffset);
            }
        }
        if (promptRect != null && promptRect.gameObject.activeSelf) {
            Place(promptRect, promptWorldPosition, promptOffset);
        }
        UpdatePops();
        UpdateWarnings();
    }

    // signs beat faster & grow as the hit nears
    private void UpdateWarnings() {
        foreach (Warning warning in warnings) {
            if (warning.sign == null) {
                continue;
            }
            Place(warning.sign, warning.worldPosition, warningOffset);
            float beatHz = Mathf.Lerp(2f, 9f, warning.progress);
            float beat = 0.5f + 0.5f * Mathf.Sin(Time.time * beatHz * 2f * Mathf.PI);
            float scale = Mathf.Lerp(0.9f, 1.3f, warning.progress)
                * (1f + 0.12f * beat);
            warning.sign.localScale = Vector3.one * scale;
        }
    }

    // private methods  ########################################################
    private bool TryBuild() {
        if (rootRect != null) {
            return true;
        }
        Canvas canvas = FindRootCanvas();
        if (canvas == null) {
            Debug.LogError("BossFightHud:\tno root Canvas found", this);
            return false;
        }
        canvasRect = (RectTransform)canvas.transform;
        canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        rootRect = CreateRect("BossFightHud", canvasRect);
        Stretch(rootRect);
        // under the screens, so pause, tutorial & lose panels cover it
        Transform screens = FindChildNamed(canvasRect, "Screens");
        if (screens != null) {
            rootRect.SetSiblingIndex(screens.GetSiblingIndex());
        }

        // name & health bar, top center
        RectTransform bar = CreateRect("HealthBar", rootRect);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = new Vector2(0f, -54f);
        bar.sizeDelta = new Vector2(440f, 22f);
        AddImage(bar, new Color(0f, 0f, 0f, 0.6f));
        healthFill = CreateRect("Fill", bar);
        Stretch(healthFill);
        healthFill.offsetMin = new Vector2(3f, 3f);
        healthFill.offsetMax = new Vector2(-3f, -3f);
        healthFill.anchorMax = new Vector2(1f, 1f);
        AddImage(healthFill, healthColor);
        nameLabel = CreateLabel("Name", rootRect, "", 28f, Color.white);
        RectTransform nameRect = nameLabel.rectTransform;
        nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -16f);
        nameRect.sizeDelta = new Vector2(440f, 36f);

        // speech bubble
        speechRect = CreateRect("Speech", rootRect);
        speechRect.sizeDelta = new Vector2(360f, 56f);
        AddImage(speechRect, new Color(0f, 0f, 0f, 0.75f));
        speechLabel = CreateLabel("Line", speechRect, "", 28f, Color.white);
        Stretch(speechLabel.rectTransform);
        speechRect.gameObject.SetActive(false);

        // key prompt: a keycap over the banana and how to use it
        promptRect = CreateRect("Prompt", rootRect);
        promptRect.sizeDelta = new Vector2(220f, 90f);
        RectTransform keycap = CreateRect("Keycap", promptRect);
        keycap.anchorMin = keycap.anchorMax = new Vector2(0.5f, 1f);
        keycap.pivot = new Vector2(0.5f, 1f);
        keycap.sizeDelta = new Vector2(150f, 44f);
        AddImage(keycap, Color.white);
        RectTransform keyFace = CreateRect("Face", keycap);
        Stretch(keyFace);
        keyFace.offsetMin = new Vector2(3f, 3f);
        keyFace.offsetMax = new Vector2(-3f, -3f);
        AddImage(keyFace, new Color(0.15f, 0.15f, 0.15f, 0.9f));
        TextMeshProUGUI keyLabel = CreateLabel(
            "Key", keyFace, "space", 24f, Color.white
        );
        Stretch(keyLabel.rectTransform);
        TextMeshProUGUI howLabel = CreateLabel(
            "How", promptRect, "hold, then release", 22f, Color.white
        );
        RectTransform howRect = howLabel.rectTransform;
        howRect.anchorMin = howRect.anchorMax = new Vector2(0.5f, 0f);
        howRect.pivot = new Vector2(0.5f, 0f);
        howRect.sizeDelta = new Vector2(260f, 36f);
        promptRect.gameObject.SetActive(false);

        // banner, screen center
        bannerLabel = CreateLabel(
            "Banner", rootRect, "", 80f, new Color(1f, 0.85f, 0.2f)
        );
        bannerLabel.rectTransform.sizeDelta = new Vector2(1000f, 120f);
        bannerLabel.rectTransform.anchoredPosition = new Vector2(0f, 120f);
        bannerLabel.gameObject.SetActive(false);

        // entry flash, on top of the rest of the hud
        RectTransform flash = CreateRect("Flash", rootRect);
        Stretch(flash);
        flashImage = AddImage(flash, new Color(1f, 1f, 1f, 0f));
        return true;
    }

    private void UpdateFlash() {
        if (flashImage == null) {
            return;
        }
        if (flashLeftS > 0f) {
            flashLeftS -= Time.unscaledDeltaTime;
        }
        float alpha = flashS > 0f ? Mathf.Clamp01(flashLeftS / flashS) : 0f;
        flashImage.color = new Color(1f, 1f, 1f, alpha);
        flashImage.enabled = alpha > 0f;
    }

    private void UpdatePops() {
        for (int i = pops.Count - 1; i >= 0; i--) {
            Pop pop = pops[i];
            pop.ageS += Time.deltaTime;
            float t = pop.ageS / POP_S;
            if (t >= 1f || pop.label == null) {
                if (pop.label != null) {
                    Destroy(pop.label.gameObject);
                }
                pops.RemoveAt(i);
                continue;
            }
            Place(
                pop.label.rectTransform,
                pop.worldPosition,
                new Vector2(0f, 40f + t * 80f)
            );
            pop.label.alpha = 1f - t * t;
            pops[i] = pop;
        }
    }

    // keeps a UI rect over a world point, hidden behind the camera
    private void Place(RectTransform target, Vector3 world, Vector2 offset) {
        Camera cam = Camera.main;
        if (cam == null || canvasRect == null) {
            return;
        }
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z < 0f) {
            target.anchoredPosition = new Vector2(0f, -10000f);
            return;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screen, canvasCamera, out Vector2 local
        );
        target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = local + offset;
    }

    private static Canvas FindRootCanvas() {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(
            FindObjectsSortMode.None
        )) {
            if (canvas.isRootCanvas
                && canvas.renderMode != RenderMode.WorldSpace) {
                return canvas;
            }
        }
        return null;
    }

    private static Transform FindChildNamed(Transform parent, string part) {
        foreach (Transform child in parent) {
            if (child.name.Contains(part)) {
                return child;
            }
        }
        return null;
    }

    private static RectTransform CreateRect(string rectName, Transform parent) {
        var go = new GameObject(rectName, typeof(RectTransform));
        go.layer = 5; // UI
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void AddCenterLabel(
        RectTransform parent,
        string text,
        float size,
        Color color,
        float y
    ) {
        TextMeshProUGUI label = CreateLabel("Line", parent, text, size, color);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);
        label.rectTransform.sizeDelta = new Vector2(1000f, size * 1.4f);
        label.rectTransform.anchoredPosition = new Vector2(0f, y);
    }

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Image AddImage(RectTransform rect, Color color) {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateLabel(
        string labelName,
        Transform parent,
        string text,
        float size,
        Color color
    ) {
        RectTransform rect = CreateRect(labelName, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    // private members  ########################################################
    private const float POP_S = 0.9f; // damage number lifetime; s

    private struct Pop {
        public TextMeshProUGUI label;
        public Vector3 worldPosition;
        public float ageS;
    }

    private struct Warning {
        public int id;
        public RectTransform sign;
        public Vector3 worldPosition;
        public float progress; // 0 when shown, 1 at the hit
    }

    private readonly List<Pop> pops = new List<Pop>();
    private readonly List<Warning> warnings = new List<Warning>();
    private int nextWarningId;
    private float flashLeftS;
    private float speechLeftS;
    private Vector3 speechWorldPosition;
    private Vector3 promptWorldPosition;

    // cached references  ------------------------------------------------------
    private RectTransform canvasRect;
    private Camera canvasCamera; // null for an overlay canvas
    private RectTransform rootRect;
    private RectTransform healthFill;
    private TextMeshProUGUI nameLabel;
    private RectTransform speechRect;
    private TextMeshProUGUI speechLabel;
    private RectTransform promptRect;
    private TextMeshProUGUI bannerLabel;
    private Image flashImage;
}
