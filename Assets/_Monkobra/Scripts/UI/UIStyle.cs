using UnityEngine;

/// <summary>
/// the Monkobra UI palette, taken from the logo: every screen draws from these
/// colors only, see docs/ui-style-doc.md. Gameplay signals (boss health red,
/// warning red) are not UI style and keep their own colors.
/// </summary>
public static class UIStyle {
    // Public Members  #########################################################
    // the logo  ---------------------------------------------------------------
    public static readonly Color Background = Hex(0x0A1F12);
    public static readonly Color Gold = Hex(0xEFB64D);
    public static readonly Color Brown = Hex(0x352016);
    public static readonly Color Shadow = Hex(0x89501F);
    public static readonly Color Green = Hex(0x24A965);
    public static readonly Color Cream = Hex(0xFFF2D2);

    // derived states  ---------------------------------------------------------
    public static readonly Color GoldLight = Hex(0xFFD36E); // selected button
    public static readonly Color GoldDark = Hex(0xD99A35); // pressed button
    public static readonly Color LockedFill = Hex(0x9E8F78);
    public static readonly Color LockedInk = Hex(0x4A3A2C);
    public static readonly Color Panel = Hex(0x102C1B); // cards, popups
    public static readonly Color Divider = Hex(0x3B4C41);

    // private methods  ########################################################
    private static Color Hex(int rgb) {
        return new Color32(
            (byte)(rgb >> 16), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF),
            255
        );
    }
}
