using System;
using UnityEngine;

/// <summary>
/// The look of a popup: which of the 2 popup entities draws it. A
/// <see cref="Message"/> is a text on a plate; a <see cref="Fruit"/> adds a
/// fruit icon and a line to the fruit it talks about.
/// </summary>
public enum PopUpKind {
    Message,
    Fruit,
}

/// <summary>
/// Data of one popup: its text, look, timing, priority, flags, and hint icon.
/// <see cref="PopUpCatalog"/> holds one per <see cref="PopUpId"/>; the shared
/// <see cref="MessagePopUp"/> entity of its <see cref="Kind"/> shows it.
/// </summary>
[Serializable]
public class PopUpDefinition {
    // Public Members  #########################################################
    /// <returns>key callers use to show this popup</returns>
    public PopUpId Id => id;

    /// <returns>which popup entity draws it</returns>
    public PopUpKind Kind => kind;

    /// <returns>shown text, <c>{0}</c> is the value the caller passes
    /// </returns>
    public string Message => message;

    /// <returns>wording while touch is in use, blank keeps
    /// <see cref="Message"/></returns>
    public string TouchMessage => touchMessage;

    /// <returns>stay time, fade-out included; s, 0 stays until hidden
    /// </returns>
    public float ShowS => showS;

    /// <returns>fade-in time; s</returns>
    public float FadeInS => fadeInS;

    /// <returns>fade-out time; s</returns>
    public float FadeOutS => fadeOutS;

    /// <returns>alpha pulses per s while shown, 0 for none</returns>
    public float PulseHz => pulseHz;

    /// <returns>higher interrupts a showing popup, equal or lower waits
    /// </returns>
    public int Priority => priority;

    /// <returns>if it shows only while the run is live</returns>
    public bool IsRunOnly => isRunOnly;

    /// <returns>if it shows at most once per scene load</returns>
    public bool IsOncePerScene => isOncePerScene;

    /// <returns>text size; canvas units</returns>
    public float FontSize => fontSize;

    /// <returns>text color</returns>
    public Color TextColor => textColor;

    /// <returns>height of the popup's anchor on the screen; 0 bottom, 1 top
    /// </returns>
    public float AnchorY => anchorY;

    /// <returns>push from the anchor; canvas units, up is positive</returns>
    public float OffsetY => offsetY;

    /// <returns>fruit picture of a <see cref="PopUpKind.Fruit"/> popup, or
    /// null</returns>
    public Sprite Icon => icon;

    /// <returns>animated hint icon while a keyboard is in use, or null
    /// </returns>
    public GameObject KeyboardHintPrefab => keyboardHintPrefab;

    /// <returns>animated hint icon while touch is in use, or null</returns>
    public GameObject TouchHintPrefab => touchHintPrefab;

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("key callers use to show this popup")]
    private PopUpId id;

    [SerializeField]
    [Tooltip("which popup entity draws it")]
    private PopUpKind kind;

    [Header("Text")]
    [SerializeField]
    [TextArea]
    [Tooltip("shown text, {0} is the value the caller passes")]
    private string message = "";

    [SerializeField]
    [TextArea]
    [Tooltip("shown text while touch is in use, {0} as in message; blank "
        + "keeps message")]
    private string touchMessage = "";

    [SerializeField]
    [Min(1f)]
    [Tooltip("text size; canvas units")]
    private float fontSize = 30f;

    [SerializeField]
    [Tooltip("text color")]
    private Color textColor = Color.white;

    [Header("Timing")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("stays this long, fade-out included; 0 stays until hidden; s")]
    private float showS = 2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("fade-in time; s")]
    private float fadeInS = 0.15f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("fade-out time; s")]
    private float fadeOutS = 0.5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("alpha pulses per s while shown, 0 for none")]
    private float pulseHz;

    [Header("Rules")]
    [SerializeField]
    [Tooltip("higher interrupts a showing popup, equal or lower waits")]
    private int priority;

    [SerializeField]
    [Tooltip("shows only while the run is live, drops on a loss")]
    private bool isRunOnly = true;

    [SerializeField]
    [Tooltip("shows at most once per scene load")]
    private bool isOncePerScene;

    [Header("Place")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("height of the anchor on the screen; 0 bottom, 1 top")]
    private float anchorY = 0.5f;

    [SerializeField]
    [Tooltip("push from the anchor; canvas units, up is positive")]
    private float offsetY;

    [Header("Icons")]
    [SerializeField]
    [Tooltip("fruit picture of a Fruit popup; blank for none")]
    private Sprite icon;

    [SerializeField]
    [Tooltip("animated hint icon while a keyboard is in use; blank for none")]
    private GameObject keyboardHintPrefab;

    [SerializeField]
    [Tooltip("animated hint icon while touch is in use; blank for none")]
    private GameObject touchHintPrefab;
}
