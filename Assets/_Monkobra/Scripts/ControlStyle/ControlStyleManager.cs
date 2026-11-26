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
/// A component of the <see cref="GameController"/> prefab, kept across scene
/// loads with it, like <see cref="AnalyticsManager"/>. Readers never need
/// the instance: <see cref="Current"/> and the static
/// <see cref="StyleChanged"/> work from any scene.
/// </para>
/// </summary>
[DefaultExecutionOrder(-990)]
[RequireComponent(typeof(GameController))]
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
        // GameController destroys a duplicate before this runs; kept as a guard
        if (I != null && I != this) {
            return;
        }

        I = this;
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

    // private methods  ########################################################
    // a phone or tablet (a mobile browser too, which still reports a
    // keyboard device) and a touch screen without a keyboard start as touch,
    // so the very first tap already meets the touch controls
    private static ControlStyle PickStartStyle() {
        bool isTouchDevice = Application.isMobilePlatform
            || (Keyboard.current == null && Touchscreen.current != null);
        return isTouchDevice ? ControlStyle.Touch : ControlStyle.Keyboard;
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
