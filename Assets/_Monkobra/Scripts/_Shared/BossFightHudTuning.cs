using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Every Inspector-tuned value of the Cobra King fight, as the Inspector
/// shows them. Read through the properties; no role writes one.
/// </summary>
[System.Serializable]
public sealed class BossFightHudTuning {
    // Public Members  #########################################################
    public float FlashS => flashS;
    public float SpeechShowS => speechShowS;
    public Vector2 SpeechOffset => speechOffset;
    public Vector2 PromptOffset => promptOffset;
    public HintIconLibrary HintIconLibrary => hintIconLibrary;
    public RuntimeAnimatorController PromptKeyboardController =>
        promptKeyboardController;
    public RuntimeAnimatorController PromptTouchController =>
        promptTouchController;
    public Color HealthColor => healthColor;
    public Color DamageColor => damageColor;
    public Color WarningColor => warningColor;
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
    private Color healthColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField]
    private Color damageColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField]
    private Color warningColor = new Color(0.95f, 0.15f, 0.15f);
    [SerializeField]
    [Tooltip("warning sign offset over its zone, in canvas units")]
    private Vector2 warningOffset = new Vector2(0f, 70f);
}
