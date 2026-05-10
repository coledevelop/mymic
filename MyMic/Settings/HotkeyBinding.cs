namespace MyMic.Settings;

public sealed class HotkeyBinding
{
    /// <summary>macOS virtual key code (HIToolbox kVK_*)</summary>
    public uint MacKeyCode { get; set; }

    /// <summary>Carbon modifier mask (cmdKey | shiftKey | etc.)</summary>
    public uint MacModifiers { get; set; }

    /// <summary>Human-readable form for the UI, e.g. "⌘⇧M".</summary>
    public string Display { get; set; } = "";
}
