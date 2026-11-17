/// <summary>
/// The device family the player is steering with right now, picked by
/// <see cref="ControlStyleManager"/> from the last input seen. Hint texts and
/// the on-screen touch controls follow it.
/// </summary>
public enum ControlStyle {
    /// <summary>a physical keyboard (with or without a mouse)</summary>
    Keyboard,

    /// <summary>a touch screen, with the virtual controls on screen</summary>
    Touch,
}
