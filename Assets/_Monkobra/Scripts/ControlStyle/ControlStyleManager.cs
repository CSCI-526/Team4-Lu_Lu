using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Detects whether the player steers by keyboard or by touch and announces
/// every switch with <see cref="StyleChanged"/>. A touch press selects
/// <see cref="ControlStyle.Touch"/>, a real key press selects
/// <see cref="ControlStyle.Keyboard"/>; the mouse never switches, and the
/// virtual controls write to a virtual gamepad, so they never read as a key.
/// <para>
/// Kept across scene loads (<c>DontDestroyOnLoad</c>, a later duplicate
/// destroys itself), like <see cref="AnalyticsManager"/>. Readers never need
/// the instance: <see cref="Current"/> and the static
/// <see cref="StyleChanged"/> work from any scene.
/// </para>
/// </summary>
public class ControlStyleManager: MonoBehaviour {
    // Public Members  #########################################################
    public static ControlStyleManager I {
        get; private set;
    }

    /// <returns>the style in use; keyboard until a touch is seen</returns>
    public static ControlStyle Current {
        get; private set;
    } = ControlStyle.Keyboard;

    /// <returns>if the player steers by touch right now</returns>
    public static bool IsTouch => Current == ControlStyle.Touch;

    /// <summary>raised once per switch, with the new style</summary>
    public static event Action<ControlStyle> StyleChanged;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        if (I != null && I != this) {
            Debug.LogWarning(
                "ControlStyleManager:\tduplicate instance destroyed",
                this
            );
            Destroy(gameObject);
            return;
        }

        I = this;
        // one style spans the whole launch, across every scene load
        DontDestroyOnLoad(gameObject);
        SetStyle(PickStartStyle());
    }

    private void Update() {
        // a key wins a tie, since a touch device never reports one
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) {
            SetStyle(ControlStyle.Keyboard);
        }

        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.press.wasPressedThisFrame) {
            SetStyle(ControlStyle.Touch);
        }
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
            Current = ControlStyle.Keyboard;
        }
    }

    // Private Methods  ########################################################
    // a device with a touch screen and no keyboard starts as touch
    private static ControlStyle PickStartStyle() {
        return Keyboard.current == null && Touchscreen.current != null
            ? ControlStyle.Touch
            : ControlStyle.Keyboard;
    }

    private static void SetStyle(ControlStyle style) {
        if (Current == style) {
            return;
        }

        Current = style;
        if (Debug.isDebugBuild) {
            Debug.Log("ControlStyleManager:\tstyle is now " + style);
        }
        StyleChanged?.Invoke(style);
    }
}
