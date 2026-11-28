using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Every tuned value of the boss fight HUD, grouped as the Inspector shows
/// them. Read-only at runtime, so a value changed in Play mode
/// persists in the asset.
/// One asset per boss: the Spider King adds its parry prompts.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Objects/BossFightHudConfig")]
public class BossFightHudConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>how long the entry flash takes to fade; s</returns>
    public float FlashS => flashS;

    /// <returns>how long a speech line stays up; s</returns>
    public float SpeechShowS => speechShowS;

    /// <returns>bubble offset over the head, in canvas units</returns>
    public Vector2 SpeechOffset => speechOffset;

    /// <returns>prompt offset over the banana, in canvas units</returns>
    public Vector2 PromptOffset => promptOffset;

    /// <returns>the keycap and finger entities the prompt shows</returns>
    public HintIconLibrary HintIconLibrary => hintIconLibrary;

    /// <returns>animates the prompt keycap while a keyboard is in use
    /// (Space hold)</returns>
    public RuntimeAnimatorController PromptKeyboardController =>
        promptKeyboardController;

    /// <returns>animates the prompt finger while touch is in use
    /// (hold)</returns>
    public RuntimeAnimatorController PromptTouchController =>
        promptTouchController;

    /// <returns>the monkey's heart image in the heart row; null draws a
    /// plain heart</returns>
    public Sprite HeartSprite => heartSprite;

    /// <returns>the gold armor image beside the heart row; null draws a
    /// plain gold diamond</returns>
    public Sprite ArmorSprite => armorSprite;

    /// <returns>tint of the health bar</returns>
    public Color HealthColor => healthColor;

    /// <returns>tint of a damage number</returns>
    public Color DamageColor => damageColor;

    /// <returns>tint of an attack warning</returns>
    public Color WarningColor => warningColor;

    /// <returns>warning sign offset over its zone, in canvas units</returns>
    public Vector2 WarningOffset => warningOffset;

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
    [Tooltip("the keycap and finger entities the prompt shows")]
    private HintIconLibrary hintIconLibrary;

    [SerializeField]
    [Tooltip("animates the prompt keycap while a keyboard is in use (Space "
        + "hold)")]
    private RuntimeAnimatorController promptKeyboardController;

    [SerializeField]
    [Tooltip("animates the prompt finger while touch is in use (hold)")]
    private RuntimeAnimatorController promptTouchController;

    [SerializeField]
    [Tooltip("the monkey's heart in the heart row; blank draws a plain heart")]
    private Sprite heartSprite;

    [SerializeField]
    [Tooltip("the gold armor beside the heart row; blank draws a diamond")]
    private Sprite armorSprite;

    [SerializeField]
    [Tooltip("tint of the health bar")]
    private Color healthColor = new Color(0.85f, 0.15f, 0.15f);

    [SerializeField]
    [Tooltip("tint of a damage number")]
    private Color damageColor = new Color(1f, 0.85f, 0.2f);

    [SerializeField]
    [Tooltip("tint of an attack warning")]
    private Color warningColor = new Color(0.95f, 0.15f, 0.15f);

    [SerializeField]
    [Tooltip("warning sign offset over its zone, in canvas units")]
    private Vector2 warningOffset = new Vector2(0f, 70f);
}
