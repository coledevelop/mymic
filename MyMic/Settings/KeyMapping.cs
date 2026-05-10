using System.Collections.Generic;
using Avalonia.Input;

namespace MyMic.Settings;

internal static class KeyMapping
{
    // Carbon modifier masks (HIToolbox/Events.h)
    public const uint CmdKey = 1u << 8;
    public const uint ShiftKey = 1u << 9;
    public const uint OptionKey = 1u << 11;
    public const uint ControlKey = 1u << 12;

    // Avalonia.Input.Key → macOS virtual key code (kVK_*)
    private static readonly Dictionary<Key, uint> AvKeyToMac = new()
    {
        // Letters
        [Key.A] = 0x00, [Key.S] = 0x01, [Key.D] = 0x02, [Key.F] = 0x03,
        [Key.H] = 0x04, [Key.G] = 0x05, [Key.Z] = 0x06, [Key.X] = 0x07,
        [Key.C] = 0x08, [Key.V] = 0x09, [Key.B] = 0x0B, [Key.Q] = 0x0C,
        [Key.W] = 0x0D, [Key.E] = 0x0E, [Key.R] = 0x0F, [Key.Y] = 0x10,
        [Key.T] = 0x11, [Key.O] = 0x1F, [Key.U] = 0x20, [Key.I] = 0x22,
        [Key.P] = 0x23, [Key.L] = 0x25, [Key.J] = 0x26, [Key.K] = 0x28,
        [Key.N] = 0x2D, [Key.M] = 0x2E,

        // Digits (top row)
        [Key.D1] = 0x12, [Key.D2] = 0x13, [Key.D3] = 0x14, [Key.D4] = 0x15,
        [Key.D5] = 0x17, [Key.D6] = 0x16, [Key.D7] = 0x1A, [Key.D8] = 0x1C,
        [Key.D9] = 0x19, [Key.D0] = 0x1D,

        // Symbols / punctuation
        [Key.OemMinus] = 0x1B,
        [Key.OemPlus] = 0x18,        // = key
        [Key.OemOpenBrackets] = 0x21,
        [Key.OemCloseBrackets] = 0x1E,
        [Key.OemSemicolon] = 0x29,
        [Key.OemQuotes] = 0x27,
        [Key.OemComma] = 0x2B,
        [Key.OemPeriod] = 0x2F,
        [Key.Oem2] = 0x2C,            // /
        [Key.OemPipe] = 0x2A,         // backslash
        [Key.OemTilde] = 0x32,        // `

        // Whitespace / control
        [Key.Space] = 0x31,
        [Key.Return] = 0x24,
        [Key.Enter] = 0x24,
        [Key.Tab] = 0x30,
        [Key.Back] = 0x33,
        [Key.Delete] = 0x75,
        [Key.Escape] = 0x35,

        // Arrows
        [Key.Left] = 0x7B, [Key.Right] = 0x7C, [Key.Down] = 0x7D, [Key.Up] = 0x7E,

        // Function keys
        [Key.F1] = 0x7A, [Key.F2] = 0x78, [Key.F3] = 0x63, [Key.F4] = 0x76,
        [Key.F5] = 0x60, [Key.F6] = 0x61, [Key.F7] = 0x62, [Key.F8] = 0x64,
        [Key.F9] = 0x65, [Key.F10] = 0x6D, [Key.F11] = 0x67, [Key.F12] = 0x6F,
        [Key.F13] = 0x69, [Key.F14] = 0x6B, [Key.F15] = 0x71,
        [Key.F16] = 0x6A, [Key.F17] = 0x40, [Key.F18] = 0x4F, [Key.F19] = 0x50,

        // Home/End/PageUp/PageDown
        [Key.Home] = 0x73, [Key.End] = 0x77,
        [Key.PageUp] = 0x74, [Key.PageDown] = 0x79,
    };

    public static bool TryGetMacKeyCode(Key key, out uint code) => AvKeyToMac.TryGetValue(key, out code);

    public static uint AvModifiersToMac(KeyModifiers mods)
    {
        uint result = 0;
        if ((mods & KeyModifiers.Meta) != 0) result |= CmdKey;
        if ((mods & KeyModifiers.Shift) != 0) result |= ShiftKey;
        if ((mods & KeyModifiers.Alt) != 0) result |= OptionKey;
        if ((mods & KeyModifiers.Control) != 0) result |= ControlKey;
        return result;
    }

    public static bool IsModifierOnly(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl
        or Key.LeftShift or Key.RightShift
        or Key.LeftAlt or Key.RightAlt
        or Key.LWin or Key.RWin
        or Key.System
        or Key.None;

    public static string FormatDisplay(Key key, KeyModifiers mods)
    {
        // Apple modifier glyph order: ⌃ ⌥ ⇧ ⌘
        var s = "";
        if ((mods & KeyModifiers.Control) != 0) s += "⌃";
        if ((mods & KeyModifiers.Alt) != 0) s += "⌥";
        if ((mods & KeyModifiers.Shift) != 0) s += "⇧";
        if ((mods & KeyModifiers.Meta) != 0) s += "⌘";
        s += FormatKey(key);
        return s;
    }

    private static string FormatKey(Key key) => key switch
    {
        Key.Space => "Space",
        Key.Return or Key.Enter => "⏎",
        Key.Tab => "⇥",
        Key.Back => "⌫",
        Key.Delete => "⌦",
        Key.Escape => "Esc",
        Key.Left => "←",
        Key.Right => "→",
        Key.Up => "↑",
        Key.Down => "↓",
        Key.Home => "↖",
        Key.End => "↘",
        Key.PageUp => "⇞",
        Key.PageDown => "⇟",
        Key.OemMinus => "-",
        Key.OemPlus => "=",
        Key.OemOpenBrackets => "[",
        Key.OemCloseBrackets => "]",
        Key.OemSemicolon => ";",
        Key.OemQuotes => "'",
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.Oem2 => "/",
        Key.OemPipe => "\\",
        Key.OemTilde => "`",
        Key.D0 => "0", Key.D1 => "1", Key.D2 => "2", Key.D3 => "3", Key.D4 => "4",
        Key.D5 => "5", Key.D6 => "6", Key.D7 => "7", Key.D8 => "8", Key.D9 => "9",
        _ => key.ToString(),
    };
}
